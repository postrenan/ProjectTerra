using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Core;
using ProjectTerra.Planet;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Loja de terrenos moderna (GIS / Portal Imobiliário Clean):
    /// Visual contemporâneo com mapa satelital interativo, demarcação procedural de glebas
    /// e painel de dossiê cadastral com interface limpa e responsiva.
    ///
    /// As glebas NÃO são mais quadrados de tamanho fixo: um diagrama de Voronoi sobre
    /// sementes distribuídas por amostragem tipo Poisson (determinística por região) recorta
    /// o território real da província em lotes orgânicos de formatos e áreas variadas, cada um
    /// limitado ao contorno geográfico verdadeiro (nenhuma gleba invade água ou região vizinha).
    /// </summary>
    public partial class SandboxHUD
    {
        #region Estado da Loja de Terrenos

        // Resolução do mapa de células (parcelPixel). Alinhado 1:1 com a textura satelital.
        private const int LandParcelMapRes = 200;

        private int landParcelCount;
        private float[] landSeedU, landSeedV;      // sementes Voronoi (espaço normalizado 0..1)
        private int[] landParcelPixel;             // id da gleba por pixel (-1 = fora da província)
        private bool[] landParcelBoundary;         // pixel pertence à linha de divisa entre glebas
        private bool[] landForSale;
        private long[] landPrice;
        private float[] landAreaHa;
        private float[] landCentU, landCentV;      // centroide normalizado de cada gleba

        private int landCacheRegionId = int.MinValue;
        private float landCacheW, landCacheL;
        private int landSelected = -1;
        private int landHover = -1;
        private Vector2 landPanelScroll = Vector2.zero;
        private GUIStyle landCellStyle;

        // Overlay de glebas desenhado sobre o satélite; só é reconstruído quando o estado muda
        // (hover, seleção ou conjunto de glebas adquiridas), nunca a cada frame.
        private Texture2D landOverlayTex;
        private int landOverlayKey = int.MinValue;

        #endregion

        #region Geração Procedural da Malha Cadastral (Voronoi / Poisson)

        private void EnsureLandGrid()
        {
            var rsm = RegionalSandboxManager.Instance;
            var region = GetActiveRegion();
            float worldW = rsm != null ? rsm.worldWidthMeters : 60000f;
            float worldL = rsm != null ? rsm.worldLengthMeters : 60000f;
            int regId = region != null ? region.id : (activeSave != null ? activeSave.regionId : 0);

            if (landParcelPixel != null && landCacheRegionId == regId &&
                Mathf.Approximately(landCacheW, worldW) && Mathf.Approximately(landCacheL, worldL))
                return;

            landCacheRegionId = regId;
            landCacheW = worldW;
            landCacheL = worldL;
            landSelected = -1;
            landHover = -1;
            landOverlayKey = int.MinValue; // força reconstrução do overlay

            GenerateProceduralParcels(region, regId, worldW, worldL);
        }

        private void GenerateProceduralParcels(RegionData region, int regId, float worldW, float worldL)
        {
            int res = LandParcelMapRes;
            bool useMask = region != null && region.id > 0;
            if (useMask) RegionBoundaryService.EnsureLoaded();

            float dLat = useMask ? (region.maxLat - region.minLat) : 0f;
            float dLon = useMask ? (region.maxLon - region.minLon) : 0f;

            // Número-alvo de glebas conforme a área real da província (lotes maiores em
            // estados gigantes, poucos lotes em microterritórios). Média ~6,5 km²/gleba.
            float areaKm2 = (worldW / 1000f) * (worldL / 1000f);
            int target = Mathf.Clamp(Mathf.RoundToInt(areaKm2 / 6.5f), 18, 120);

            // Distribuição tipo Poisson (espaçamento métrico mínimo) → glebas de tamanhos
            // variados e orgânicos, sem o padrão regular de uma grade.
            float avgArea = (worldW * worldL) / Mathf.Max(1, target);
            float minSpacing = 0.62f * Mathf.Sqrt(avgArea);
            float minSpacingSq = minSpacing * minSpacing;

            var rng = new System.Random(unchecked(regId * 73856093) ^ 19349663);
            var sU = new List<float>(target);
            var sV = new List<float>(target);

            int attempts = target * 60;
            while (sU.Count < target && attempts-- > 0)
            {
                float u = (float)rng.NextDouble();
                float v = (float)rng.NextDouble();

                if (useMask)
                {
                    float lat = region.minLat + v * dLat;
                    float lon = region.minLon + u * dLon;
                    if (!RegionBoundaryService.IsInsideRegion(region.id, lat, lon)) continue;
                }

                bool ok = true;
                for (int i = 0; i < sU.Count; i++)
                {
                    float du = (u - sU[i]) * worldW;
                    float dv = (v - sV[i]) * worldL;
                    if (du * du + dv * dv < minSpacingSq) { ok = false; break; }
                }
                if (!ok) continue;

                sU.Add(u);
                sV.Add(v);
            }

            if (sU.Count == 0) { sU.Add(0.5f); sV.Add(0.5f); }

            int count = sU.Count;
            landParcelCount = count;
            landSeedU = sU.ToArray();
            landSeedV = sV.ToArray();
            landForSale = new bool[count];
            landPrice = new long[count];
            landAreaHa = new float[count];
            landCentU = new float[count];
            landCentV = new float[count];
            landParcelPixel = new int[res * res];
            landParcelBoundary = new bool[res * res];

            var pxCount = new int[count];
            var sumU = new double[count];
            var sumV = new double[count];

            // Atribuição Voronoi: cada pixel pertence à semente métrica mais próxima,
            // recortado pelo contorno real da província (mesma convenção da textura satelital).
            for (int y = 0; y < res; y++)
            {
                float ny = y / (float)(res - 1);
                float lat = region != null ? region.minLat + ny * dLat : 0f;
                int rowBase = y * res;
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);
                    int idx = rowBase + x;

                    if (useMask)
                    {
                        float lon = region.minLon + nx * dLon;
                        if (!RegionBoundaryService.IsInsideRegion(region.id, lat, lon))
                        {
                            landParcelPixel[idx] = -1;
                            continue;
                        }
                    }

                    int best = 0;
                    float bestD = float.MaxValue;
                    for (int i = 0; i < count; i++)
                    {
                        float du = (nx - landSeedU[i]) * worldW;
                        float dv = (ny - landSeedV[i]) * worldL;
                        float d = du * du + dv * dv;
                        if (d < bestD) { bestD = d; best = i; }
                    }

                    landParcelPixel[idx] = best;
                    pxCount[best]++;
                    sumU[best] += nx;
                    sumV[best] += ny;
                }
            }

            // Linhas de divisa (um pixel difere do vizinho à direita/abaixo, incluindo transição p/ fora).
            for (int y = 0; y < res; y++)
            {
                int rowBase = y * res;
                for (int x = 0; x < res; x++)
                {
                    int idx = rowBase + x;
                    int p = landParcelPixel[idx];
                    if (p < 0) continue;
                    bool bound = false;
                    if (x < res - 1 && landParcelPixel[idx + 1] != p) bound = true;
                    else if (y < res - 1 && landParcelPixel[idx + res] != p) bound = true;
                    else if (x > 0 && landParcelPixel[idx - 1] != p) bound = true;
                    else if (y > 0 && landParcelPixel[idx - res] != p) bound = true;
                    landParcelBoundary[idx] = bound;
                }
            }

            // Métricas por gleba: área real (hectares), centroide e preço.
            float pxAreaHa = (worldW / res) * (worldL / res) / 10000f;
            for (int i = 0; i < count; i++)
            {
                landForSale[i] = pxCount[i] > 0;
                landAreaHa[i] = pxCount[i] * pxAreaHa;
                landCentU[i] = pxCount[i] > 0 ? (float)(sumU[i] / pxCount[i]) : landSeedU[i];
                landCentV[i] = pxCount[i] > 0 ? (float)(sumV[i] / pxCount[i]) : landSeedV[i];

                float noise = Mathf.PerlinNoise(landCentU[i] * 6.3f + regId * 0.13f, landCentV[i] * 6.3f + 11.7f);
                float factor = Mathf.Lerp(0.7f, 1.45f, noise);
                double basePrice = 120000.0 + 8000.0 * Mathf.Sqrt(Mathf.Max(1f, landAreaHa[i]));
                long price = (long)(basePrice * factor);
                landPrice[i] = (price / 1000L) * 1000L;
            }
        }

        private static string ShortPrice(long p)
        {
            if (p >= 1000000L) return $"${p / 1000000f:0.0}M";
            return $"${p / 1000L}k";
        }

        #endregion

        #region Overlay das Glebas (reconstruído sob demanda)

        private int ComputeOverlayKey(RegionSaveData save)
        {
            int key = 17;
            key = key * 31 + landHover;
            key = key * 31 + landSelected;
            var owned = save != null ? save.ownedParcels : null;
            if (owned != null)
            {
                key = key * 31 + owned.Count;
                for (int i = 0; i < owned.Count; i++) key = key * 31 + owned[i];
            }
            return key;
        }

        private void EnsureLandOverlay(RegionSaveData save)
        {
            if (landParcelPixel == null) return;

            int key = ComputeOverlayKey(save);
            if (landOverlayTex != null && key == landOverlayKey) return;
            landOverlayKey = key;

            int res = LandParcelMapRes;
            if (landOverlayTex == null)
            {
                landOverlayTex = new Texture2D(res, res, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            var owned = save != null ? save.ownedParcels : null;
            Color outside = new Color(0f, 0f, 0f, 0.55f);          // fora da província: indisponível
            Color line = new Color(1f, 1f, 1f, 0.5f);              // divisa cadastral
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color colOwned = new Color(0.10f, 0.75f, 0.35f, 0.42f);
            Color colSel = new Color(0.25f, 0.65f, 1f, 0.45f);
            Color colHover = new Color(1f, 0.95f, 0.40f, 0.33f);

            var px = new Color[res * res];
            for (int i = 0; i < px.Length; i++)
            {
                int p = landParcelPixel[i];
                if (p < 0) { px[i] = outside; continue; }
                if (landParcelBoundary[i]) { px[i] = line; continue; }

                if (owned != null && owned.Contains(p)) px[i] = colOwned;
                else if (p == landSelected) px[i] = colSel;
                else if (p == landHover) px[i] = colHover;
                else px[i] = clear;
            }

            landOverlayTex.SetPixels(px);
            landOverlayTex.Apply(false);
        }

        #endregion

        #region Desenho da Página Terra & Cia Clean

        private void DrawLandMarket(Rect page)
        {
            EnsureMapTexture();
            EnsureLandGrid();

            if (landCellStyle == null)
            {
                landCellStyle = new GUIStyle(brSmallStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false
                };
                landCellStyle.normal.textColor = Color.white;
            }

            var save = CurrentSave;
            var region = GetActiveRegion();
            string regName = region != null ? region.name : (save != null ? save.regionName : "Região");

            float maxContainerW = Mathf.Min(1160f, page.width - 48f);
            float startX = page.x + (page.width - maxContainerW) * 0.5f;

            // Cabeçalho Clean
            Rect header = new Rect(startX, page.y + 14f, maxContainerW, 64f);
            Fill(header, UiCardBg);
            DrawOutline(header, UiBorderLight);

            Fill(new Rect(header.x, header.y, 4f, header.height), new Color(0.18f, 0.45f, 0.25f));

            GUI.Label(new Rect(header.x + 20f, header.y + 12f, header.width - 40f, 24f),
                $"Terra & Cia • Mapa Cadastral: {regName}", brPageH1Style);

            // Legenda moderna com caixas de cor procedurais limpas
            float lx = header.x + 20f;
            float ly = header.y + 38f;

            Fill(new Rect(lx, ly + 2f, 10f, 10f), new Color(0.10f, 0.75f, 0.35f));
            GUI.Label(new Rect(lx + 14f, ly, 75f, 18f), "Suas Terras", brSmallStyle);
            lx += 96f;

            Fill(new Rect(lx, ly + 2f, 10f, 10f), new Color(0.25f, 0.65f, 1f));
            GUI.Label(new Rect(lx + 14f, ly, 80f, 18f), "Selecionada", brSmallStyle);
            lx += 100f;

            Fill(new Rect(lx, ly + 2f, 10f, 10f), new Color(0.85f, 0.88f, 0.92f));
            GUI.Label(new Rect(lx + 14f, ly, 65f, 18f), "À Venda", brSmallStyle);
            lx += 86f;

            Fill(new Rect(lx, ly + 2f, 10f, 10f), new Color(0.95f, 0.20f, 0.20f));
            GUI.Label(new Rect(lx + 14f, ly, 80f, 18f), "Sua Posição", brSmallStyle);

            float contentY = header.yMax + 14f;
            float availH = page.yMax - contentY - 14f;

            if (maxContainerW >= 760f)
            {
                // Layout 2 Colunas (Mapa à Esquerda | Painel à Direita)
                float panelW = Mathf.Clamp(maxContainerW * 0.34f, 280f, 360f);
                float mapAreaW = maxContainerW - panelW - 16f;

                Rect mapArea = new Rect(startX, contentY, mapAreaW, availH);
                Rect panelArea = new Rect(mapArea.xMax + 16f, contentY, panelW, availH);

                DrawCleanLandMap(mapArea, save);
                DrawCleanLandPanel(panelArea, save);
            }
            else
            {
                // Layout Empilhado (Telas compactas)
                GUILayout.BeginArea(new Rect(startX, contentY, maxContainerW, availH));
                landPanelScroll = GUILayout.BeginScrollView(landPanelScroll);

                Rect mapArea = GUILayoutUtility.GetRect(maxContainerW, 320f);
                DrawCleanLandMap(mapArea, save);

                GUILayout.Space(14);

                Rect panelArea = GUILayoutUtility.GetRect(maxContainerW, 260f);
                DrawCleanLandPanel(panelArea, save);

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void DrawCleanLandMap(Rect area, RegionSaveData save)
        {
            // Fundo escuro suave do mapa
            Fill(area, new Color(0.10f, 0.12f, 0.16f));
            DrawOutline(area, UiBorderLight);

            float aspect = landCacheW / Mathf.Max(1f, landCacheL);
            float innerPad = 10f;
            float maxW = area.width - innerPad * 2f;
            float maxH = area.height - innerPad * 2f;

            float mw = maxW;
            float mh = mw / aspect;
            if (mh > maxH) { mh = maxH; mw = mh * aspect; }

            Rect map = new Rect(area.x + (area.width - mw) * 0.5f, area.y + (area.height - mh) * 0.5f, mw, mh);

            if (regionalMapTex != null) GUI.DrawTexture(map, regionalMapTex);
            else Fill(map, new Color(0.24f, 0.32f, 0.22f));

            // Hover / seleção determinados pelo pixel da gleba sob o cursor.
            int res = LandParcelMapRes;
            Vector2 mouse = Event.current.mousePosition;
            bool click = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            int newHover = -1;

            if (landParcelPixel != null && map.Contains(mouse))
            {
                float fx = (mouse.x - map.x) / map.width;
                float fy = (mouse.y - map.y) / map.height;
                // A textura é desenhada com GUI.DrawTexture (origem na base): o topo do
                // retângulo corresponde à última linha de pixels, daí (1 - fy).
                int tx = Mathf.Clamp((int)(fx * (res - 1)), 0, res - 1);
                int ty = Mathf.Clamp((int)((1f - fy) * (res - 1)), 0, res - 1);
                int p = landParcelPixel[ty * res + tx];
                if (p >= 0 && p < landForSale.Length && landForSale[p]) newHover = p;
            }

            if (newHover != landHover) landHover = newHover;

            // Overlay das glebas (orgânicas), reconstruído só quando o estado muda.
            EnsureLandOverlay(save);
            if (landOverlayTex != null) GUI.DrawTexture(map, landOverlayTex);

            if (click && landHover >= 0 && map.Contains(mouse))
            {
                landSelected = landHover;
                Event.current.Use();
            }

            // Jogador no mapa
            var player = PlayerCharacterController.Instance;
            if (player != null)
            {
                Vector2 pp = WorldToMapScreen(player.transform.position, landCacheW, landCacheL, map);
                if (map.Contains(pp))
                {
                    Fill(new Rect(pp.x - 5, pp.y - 5, 10, 10), new Color(0.95f, 0.20f, 0.20f));
                    GUI.Label(new Rect(pp.x + 7, pp.y - 9, 80, 18), "Você", landCellStyle);
                }
            }
        }

        private void DrawCleanLandPanel(Rect panel, RegionSaveData save)
        {
            Fill(panel, UiCardBg);
            DrawOutline(panel, UiBorderLight);

            int ownedCount = save != null && save.ownedParcels != null ? save.ownedParcels.Count : 0;
            string moneyStr = save != null ? save.GetFormattedMoney() : "$ --";

            GUILayout.BeginArea(new Rect(panel.x + 20f, panel.y + 20f, panel.width - 40f, panel.height - 40f));
            landPanelScroll = GUILayout.BeginScrollView(landPanelScroll);

            GUI.Label(new Rect(0, 0, panel.width - 40f, 22f), "Dossiê da Propriedade", brPageH2Style);
            GUI.Label(new Rect(0, 26f, panel.width - 40f, 18f), $"Suas terras: {ownedCount} glebas adquiridas", brBodyStyle);
            GUI.Label(new Rect(0, 48f, panel.width - 40f, 22f), $"Saldo: {moneyStr}", brPriceStyle);

            GUILayout.Space(82);

            if (landSelected < 0 || landForSale == null || landSelected >= landForSale.Length)
            {
                Rect tip = GUILayoutUtility.GetRect(panel.width - 40f, 74f);
                Fill(tip, new Color(0.96f, 0.97f, 0.99f));
                DrawOutline(tip, UiBorderLight);
                GUI.Label(new Rect(tip.x + 14f, tip.y + 16f, tip.width - 28f, 42f),
                    "Clique sobre qualquer gleba no mapa para consultar formato, área e valor de aquisição.", brSmallStyle);
            }
            else
            {
                int id = landSelected;
                bool owned = save != null && save.ownedParcels != null && save.ownedParcels.Contains(id);
                string code = $"Gleba Nº {id + 1:000}";

                Rect info = GUILayoutUtility.GetRect(panel.width - 40f, 144f);
                Fill(info, new Color(0.97f, 0.98f, 1f));
                DrawOutline(info, UiBorderLight);

                GUI.Label(new Rect(info.x + 16f, info.y + 14f, info.width - 32f, 24f), code, brPageH1Style);
                GUI.Label(new Rect(info.x + 16f, info.y + 42f, info.width - 32f, 20f), $"Área: {landAreaHa[id]:N0} hectares", brBodyStyle);
                GUI.Label(new Rect(info.x + 16f, info.y + 64f, info.width - 32f, 20f),
                    "Formato: gleba de contorno procedural (irregular)", brSmallStyle);
                GUI.Label(new Rect(info.x + 16f, info.y + 86f, info.width - 32f, 20f),
                    owned ? "Situação: Sua propriedade (Adquirida)" : "Situação: Disponível para compra", brSmallStyle);

                GUI.Label(new Rect(info.x + 16f, info.y + 110f, info.width - 32f, 26f), FormatPrice(landPrice[id]), brPriceStyle);

                GUILayout.Space(20);

                bool canAfford = save != null && save.currentMoney >= landPrice[id];
                GUI.enabled = !owned && canAfford;
                string label = owned ? "Já Adquirida" : (canAfford ? "Adquirir Gleba" : "Saldo Insuficiente");
                if (GUILayout.Button(label, brBuyPrimaryStyle, GUILayout.Height(42)))
                {
                    PurchaseLandParcel(id);
                }
                GUI.enabled = true;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void PurchaseLandParcel(int id)
        {
            var save = CurrentSave;
            if (save == null) { ShowToast("Sem partida ativa."); return; }
            if (landForSale == null || id < 0 || id >= landForSale.Length || !landForSale[id]) return;
            if (save.ownedParcels == null) save.ownedParcels = new List<int>();
            if (save.ownedParcels.Contains(id)) return;
            if (save.currentMoney < landPrice[id]) { ShowToast("Saldo insuficiente."); return; }

            save.currentMoney -= landPrice[id];
            save.ownedParcels.Add(id);
            save.reputation += 5;
            PersistSave();

            ShowToast($"Gleba Nº {id + 1:000} adquirida com sucesso! (+5 Rep)");
        }

        #endregion
    }
}
