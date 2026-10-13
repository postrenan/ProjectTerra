using UnityEngine;
using ProjectTerra.Core;
using ProjectTerra.Planet;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Loja de terrenos moderna (GIS / Portal Imobiliário Clean):
    /// Visual contemporâneo com mapa satelital interativo, demarcação precisa de glebas
    /// e painel de dossiê cadastral com interface limpa e responsiva.
    /// </summary>
    public partial class SandboxHUD
    {
        #region Estado da Loja de Terrenos

        private const int LandGridCols = 16;

        private int landCols, landRows;
        private bool[] landForSale;
        private long[] landPrice;
        private float[] landAreaHa;
        private int landCacheRegionId = int.MinValue;
        private float landCacheW, landCacheL;
        private int landSelected = -1;
        private Vector2 landPanelScroll = Vector2.zero;
        private GUIStyle landCellStyle;

        #endregion

        #region Cálculo da Grade Cadastral

        private void EnsureLandGrid()
        {
            var rsm = RegionalSandboxManager.Instance;
            var region = GetActiveRegion();
            float worldW = rsm != null ? rsm.worldWidthMeters : 60000f;
            float worldL = rsm != null ? rsm.worldLengthMeters : 60000f;
            int regId = region != null ? region.id : (activeSave != null ? activeSave.regionId : 0);

            if (landForSale != null && landCacheRegionId == regId &&
                Mathf.Approximately(landCacheW, worldW) && Mathf.Approximately(landCacheL, worldL))
                return;

            landCacheRegionId = regId;
            landCacheW = worldW;
            landCacheL = worldL;
            landSelected = -1;

            float aspect = worldW / Mathf.Max(1f, worldL);
            landCols = LandGridCols;
            landRows = Mathf.Clamp(Mathf.RoundToInt(landCols / Mathf.Max(0.2f, aspect)), 6, 28);

            int n = landCols * landRows;
            landForSale = new bool[n];
            landPrice = new long[n];
            landAreaHa = new float[n];

            float cellW = worldW / landCols;
            float cellL = worldL / landRows;
            float areaHa = cellW * cellL / 10000f;

            bool useMask = region != null && region.id > 0;
            if (useMask) RegionBoundaryService.EnsureLoaded();

            for (int r = 0; r < landRows; r++)
            {
                for (int c = 0; c < landCols; c++)
                {
                    int id = r * landCols + c;

                    float insideFrac = 1f;
                    if (useMask)
                    {
                        int hit = 0, total = 0;
                        float dLat = region.maxLat - region.minLat;
                        float dLon = region.maxLon - region.minLon;
                        for (int sy = 0; sy < 4; sy++)
                        {
                            for (int sx = 0; sx < 4; sx++)
                            {
                                float u = (c + (sx + 0.5f) / 4f) / landCols;
                                float v = 1f - (r + (sy + 0.5f) / 4f) / landRows;
                                float lat = region.minLat + v * dLat;
                                float lon = region.minLon + u * dLon;
                                total++;
                                if (RegionBoundaryService.IsInsideRegion(region.id, lat, lon)) hit++;
                            }
                        }
                        insideFrac = hit / (float)total;
                    }

                    landForSale[id] = insideFrac >= 0.4f;
                    landAreaHa[id] = areaHa * insideFrac;

                    float noise = Mathf.PerlinNoise(c * 0.37f + regId * 0.13f, r * 0.37f + 11.7f);
                    float factor = Mathf.Lerp(0.7f, 1.45f, noise);
                    double basePrice = 120000.0 + 8000.0 * Mathf.Sqrt(Mathf.Max(1f, landAreaHa[id]));
                    long price = (long)(basePrice * factor);
                    landPrice[id] = (price / 1000L) * 1000L;
                }
            }
        }

        private static string ShortPrice(long p)
        {
            if (p >= 1000000L) return $"${p / 1000000f:0.0}M";
            return $"${p / 1000L}k";
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

            float cw = map.width / landCols;
            float ch = map.height / landRows;
            Vector2 mouse = Event.current.mousePosition;
            int hover = -1;
            bool click = Event.current.type == EventType.MouseDown && Event.current.button == 0;

            for (int r = 0; r < landRows; r++)
            {
                for (int c = 0; c < landCols; c++)
                {
                    int id = r * landCols + c;
                    Rect cell = new Rect(map.x + c * cw, map.y + r * ch, cw, ch);
                    bool owned = save != null && save.ownedParcels != null && save.ownedParcels.Contains(id);

                    if (!landForSale[id])
                    {
                        Fill(cell, new Color(0f, 0f, 0f, 0.55f));
                        continue;
                    }

                    bool isHover = cell.Contains(mouse);
                    if (isHover) hover = id;

                    if (owned) Fill(cell, new Color(0.10f, 0.75f, 0.35f, 0.45f));
                    else if (isHover) Fill(cell, new Color(1f, 0.95f, 0.40f, 0.35f));

                    GUI.color = new Color(1f, 1f, 1f, 0.40f);
                    DrawOutline(cell, Color.white);
                    GUI.color = Color.white;

                    if (cw > 38f && ch > 16f)
                        GUI.Label(cell, owned ? "SUA" : ShortPrice(landPrice[id]), landCellStyle);
                }
            }

            // Destaque da Seleção
            if (landSelected >= 0 && landSelected < landForSale.Length)
            {
                int sr = landSelected / landCols, sc = landSelected % landCols;
                Rect sel = new Rect(map.x + sc * cw, map.y + sr * ch, cw, ch);
                GUI.color = new Color(0.25f, 0.65f, 1f, 1f);
                DrawOutline(sel, new Color(0.25f, 0.65f, 1f));
                GUI.color = Color.white;
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

            if (click && hover >= 0 && map.Contains(mouse))
            {
                landSelected = hover;
                Event.current.Use();
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
            GUI.Label(new Rect(0, 26f, panel.width - 40f, 18f), $"Suas terras: {ownedCount} parcelas adquiridas", brBodyStyle);
            GUI.Label(new Rect(0, 48f, panel.width - 40f, 22f), $"Saldo: {moneyStr}", brPriceStyle);

            GUILayout.Space(82);

            if (landSelected < 0 || landSelected >= landForSale.Length)
            {
                Rect tip = GUILayoutUtility.GetRect(panel.width - 40f, 74f);
                Fill(tip, new Color(0.96f, 0.97f, 0.99f));
                DrawOutline(tip, UiBorderLight);
                GUI.Label(new Rect(tip.x + 14f, tip.y + 16f, tip.width - 28f, 42f),
                    "Clique sobre qualquer parcela no mapa para consultar dimensão, área e valor de aquisição.", brSmallStyle);
            }
            else
            {
                int id = landSelected;
                int row = id / landCols, col = id % landCols;
                bool owned = save != null && save.ownedParcels != null && save.ownedParcels.Contains(id);
                string code = $"Gleba {(char)('A' + Mathf.Min(col, 25))}-{row + 1}";

                Rect info = GUILayoutUtility.GetRect(panel.width - 40f, 144f);
                Fill(info, new Color(0.97f, 0.98f, 1f));
                DrawOutline(info, UiBorderLight);

                GUI.Label(new Rect(info.x + 16f, info.y + 14f, info.width - 32f, 24f), code, brPageH1Style);
                GUI.Label(new Rect(info.x + 16f, info.y + 42f, info.width - 32f, 20f), $"Área: {landAreaHa[id]:N0} hectares", brBodyStyle);
                GUI.Label(new Rect(info.x + 16f, info.y + 64f, info.width - 32f, 20f),
                    $"Dimensões: {landCacheW / landCols / 1000f:0.0} x {landCacheL / landRows / 1000f:0.0} km", brSmallStyle);
                GUI.Label(new Rect(info.x + 16f, info.y + 86f, info.width - 32f, 20f),
                    owned ? "Situação: Sua propriedade (Adquirida)" : "Situação: Disponível para compra", brSmallStyle);

                GUI.Label(new Rect(info.x + 16f, info.y + 110f, info.width - 32f, 26f), FormatPrice(landPrice[id]), brPriceStyle);

                GUILayout.Space(20);

                bool canAfford = save != null && save.currentMoney >= landPrice[id];
                GUI.enabled = !owned && canAfford;
                string label = owned ? "Já Adquirida" : (canAfford ? "Adquirir Parcela" : "Saldo Insuficiente");
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
            if (id < 0 || id >= landForSale.Length || !landForSale[id]) return;
            if (save.ownedParcels == null) save.ownedParcels = new System.Collections.Generic.List<int>();
            if (save.ownedParcels.Contains(id)) return;
            if (save.currentMoney < landPrice[id]) { ShowToast("Saldo insuficiente."); return; }

            save.currentMoney -= landPrice[id];
            save.ownedParcels.Add(id);
            save.reputation += 5;
            PersistSave();

            string code = $"{(char)('A' + Mathf.Min(id % landCols, 25))}{id / landCols + 1}";
            ShowToast($"Gleba {code} adquirida com sucesso! (+5 Rep)");
        }

        #endregion
    }
}
