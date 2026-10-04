using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Planet;
using ProjectTerra.Planet.TerrainStreaming;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Camadas e Controles do Mapa Tático

        private bool showRoadsLayer = true;
        private bool showRiversLayer = true;
        private bool showCitiesLayer = true;
        private bool showDivisasLayer = true;
        private bool showContoursLayer = true;
        private bool showGridLayer = true;
        private bool showLandmarksLayer = true;

        private class NeighborMapLabel
        {
            public int regionId;
            public string displayName;
            public string subtitle;
            public float normX;
            public float normY;
            public int pixelCount;
            public float sumNx;
            public float sumNy;
            public bool isEdgeBadge;
        }

        private List<NeighborMapLabel> neighborMapLabels;

        private List<string> borderLabels;
        private bool bordersComputed = false;

        private bool mapTextureGenerated = false;
        private int lastGeneratedRegionId = -1;

        // Limites de Zoom
        private const float MinMapZoom = 0.35f;
        private const float MaxMapZoom = 25.0f;

        #endregion

        #region Gestão do Mapa Regional

        public void ToggleRegionalMap()
        {
            IsRegionalMapOpen = !IsRegionalMapOpen;
            if (IsRegionalMapOpen)
            {
                // Simétrico ao console: abrir o mapa fecha o console pelo mesmo motivo.
                if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpen)
                {
                    InGameCommandConsole.Instance.CloseConsole();
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                EnsureMapTexture();
            }
            else
            {
                RestoreGameplayCursor();
            }
        }

        public void CloseRegionalMap()
        {
            IsRegionalMapOpen = false;
            RestoreGameplayCursor();
        }

        private RegionData GetActiveRegion()
        {
            var rsm = RegionalSandboxManager.Instance;
            if (rsm != null && rsm.activeRegionData != null) return rsm.activeRegionData;

            if (activeSave != null)
            {
                string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "regions_database.bin");
                if (System.IO.File.Exists(dbPath))
                {
                    try
                    {
                        var db = RegionDatabase.LoadFromBinary(dbPath);
                        if (db != null && db.regions != null)
                        {
                            return db.regions.Find(r => r.id == activeSave.regionId);
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        private void EnsureMapTexture()
        {
            var rsm = RegionalSandboxManager.Instance;
            var region = GetActiveRegion();
            int currentRegId = region != null ? region.id : (activeSave != null ? activeSave.regionId : 0);

            if (regionalMapTex == null || !mapTextureGenerated || lastGeneratedRegionId != currentRegId)
            {
                if (rsm != null && rsm.activeTerrainData != null)
                {
                    GenerateRegionalMapTexture();
                }
            }
        }

        #endregion

        #region Síntese Realista da Textura do Mapa no Formato da Província

        private void GenerateRegionalMapTexture()
        {
            int res = 512;
            regionalMapTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            Color[] cols = new Color[res * res];

            var rsm = RegionalSandboxManager.Instance;
            var region = GetActiveRegion();
            TerrainData tData = rsm != null ? rsm.activeTerrainData : null;
            RegionalHydroData hydro = rsm != null ? rsm.activeHydroData : null;
            float seaLvl = hydro != null ? hydro.seaLevelNormalized : 0.05f;
            float worldW = rsm != null ? rsm.worldWidthMeters : 60000f;
            float worldL = rsm != null ? rsm.worldLengthMeters : 60000f;

            // 1. Extração da Máscara Geográfica Exata da Província (Silhueta Real)
            bool[,] insideMask = new bool[res, res];
            ushort[,] pixelRegionIds = new ushort[res, res];
            bool hasRegionMask = false;
            int insideCount = 0;

            var neighborStats = new Dictionary<ushort, NeighborMapLabel>();

            if (region != null && region.id > 0)
            {
                RegionBoundaryService.EnsureLoaded();
                float dLat = region.maxLat - region.minLat;
                float dLon = region.maxLon - region.minLon;

                for (int y = 0; y < res; y++)
                {
                    float ny = y / (float)(res - 1);
                    float lat = region.minLat + ny * dLat;
                    for (int x = 0; x < res; x++)
                    {
                        float nx = x / (float)(res - 1);
                        float lon = region.minLon + nx * dLon;
                        ushort pid = RegionBoundaryService.GetRegionIdAt(lat, lon);
                        pixelRegionIds[y, x] = pid;
                        bool inside = RegionBoundaryService.IsInsideRegion(region.id, lat, lon);
                        insideMask[y, x] = inside;
                        if (inside)
                        {
                            insideCount++;
                        }
                        else if (pid > 0 && pid != region.id)
                        {
                            if (!neighborStats.TryGetValue(pid, out var stat))
                            {
                                stat = new NeighborMapLabel { regionId = pid };
                                neighborStats[pid] = stat;
                            }
                            stat.pixelCount++;
                            stat.sumNx += nx;
                            stat.sumNy += ny;
                        }
                    }
                }

                if (insideCount > 30)
                {
                    hasRegionMask = true;
                }
            }

            // Agrupa e prepara rótulos para as divisas e municípios vizinhos
            var groupedLabels = new Dictionary<string, NeighborMapLabel>();
            foreach (var kvp in neighborStats)
            {
                var stat = kvp.Value;
                if (stat.pixelCount < 60) continue;

                var nReg = RegionNeighbors.GetRegionById(stat.regionId);
                if (nReg == null) continue;

                bool isForeign = region != null && nReg.country != region.country;
                string label = isForeign ? nReg.country.ToUpper() : nReg.name.ToUpper();
                string sub = isForeign ? nReg.name.ToUpper() : "DIVISA";

                if (!groupedLabels.TryGetValue(label, out var grp))
                {
                    grp = new NeighborMapLabel
                    {
                        displayName = label,
                        subtitle = sub,
                        regionId = stat.regionId
                    };
                    groupedLabels[label] = grp;
                }
                grp.pixelCount += stat.pixelCount;
                grp.sumNx += stat.sumNx;
                grp.sumNy += stat.sumNy;
            }

            neighborMapLabels = new List<NeighborMapLabel>();
            var labeledNames = new HashSet<string>();
            foreach (var grp in groupedLabels.Values)
            {
                if (grp.pixelCount >= 60)
                {
                    grp.normX = grp.sumNx / grp.pixelCount;
                    grp.normY = grp.sumNy / grp.pixelCount;
                    neighborMapLabels.Add(grp);
                    labeledNames.Add(grp.displayName);
                }
            }

            // Garante que vizinhos de fronteira importantes tenham rótulos mesmo se tocam a borda externa
            if (region != null)
            {
                var borderNeighbors = RegionNeighbors.GetDetailedBorderNeighbors(region);
                foreach (var b in borderNeighbors)
                {
                    if (labeledNames.Contains(b.displayName)) continue;

                    float bx = 0.5f + b.directionX * 0.42f;
                    float by = 0.5f + b.directionY * 0.42f;
                    bx = Mathf.Clamp(bx, 0.08f, 0.92f);
                    by = Mathf.Clamp(by, 0.08f, 0.92f);

                    neighborMapLabels.Add(new NeighborMapLabel
                    {
                        regionId = b.region.id,
                        displayName = b.displayName,
                        subtitle = b.isForeign ? "FRONTEIRA INTERNACIONAL" : "DIVISA",
                        normX = bx,
                        normY = by,
                        pixelCount = 100,
                        isEdgeBadge = true
                    });
                    labeledNames.Add(b.displayName);
                }
            }

            // 2. Detecção de Bordas / Contorno da Província com Glow Halo
            bool[,] isBorder = new bool[res, res];
            bool[,] isOuterGlow = new bool[res, res];

            if (hasRegionMask)
            {
                for (int y = 1; y < res - 1; y++)
                {
                    for (int x = 1; x < res - 1; x++)
                    {
                        bool cur = insideMask[y, x];
                        if (cur)
                        {
                            if (!insideMask[y, x - 1] || !insideMask[y, x + 1] ||
                                !insideMask[y - 1, x] || !insideMask[y + 1, x])
                            {
                                isBorder[y, x] = true;
                            }
                        }
                        else
                        {
                            // Só é "borda externa" quando o pixel está junto da região na horizontal
                            // (há vizinho dentro) e longe dela na vertical (vizinho fora).
                            // A condição anterior (A || B || !C || !D) era verdadeira para
                            // praticamente todo pixel fora da região — basta um vizinho
                            // vertical fora para o || curto-circuitar — e deixava os ramos
                            // de oceano, terra cinza e linha de divisa inalcançáveis.
                            if ((insideMask[y, x - 1] || insideMask[y, x + 1]) &&
                                (!insideMask[y - 1, x] || !insideMask[y + 1, x]))
                            {
                                isOuterGlow[y, x] = true;
                            }
                        }
                    }
                }
            }

            // 3. Renderização Topográfica Multi-Camadas (Relevo Suíço, Biomas, Curvas de Nível)
            float delta = 1f / (res - 1);
            Vector3 lightDir = new Vector3(-0.55f, 0.707f, 0.45f).normalized; // Noroeste 315° / 45° elevação

            for (int y = 0; y < res; y++)
            {
                float ny = y / (float)(res - 1);
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);

                    float h = 0.5f;
                    float steep = 0f;
                    float elevMeters = 0f;
                    if (tData != null)
                    {
                        float rawH = tData.GetInterpolatedHeight(nx, ny);
                        h = rawH / Mathf.Max(1f, tData.size.y);
                        steep = tData.GetSteepness(nx, ny);
                        elevMeters = rawH;
                    }

                    // Sombreador analítico de relevo (Hillshading topográfico)
                    float hL = tData != null ? tData.GetInterpolatedHeight(Mathf.Max(0f, nx - delta), ny) : h;
                    float hR = tData != null ? tData.GetInterpolatedHeight(Mathf.Min(1f, nx + delta), ny) : h;
                    float hD = tData != null ? tData.GetInterpolatedHeight(nx, Mathf.Max(0f, ny - delta)) : h;
                    float hU = tData != null ? tData.GetInterpolatedHeight(nx, Mathf.Min(1f, ny + delta)) : h;

                    float slopeX = (hR - hL) / (delta * 2f * worldW);
                    float slopeZ = (hU - hD) / (delta * 2f * worldL);
                    Vector3 normal = new Vector3(-slopeX * 4f, 1f, -slopeZ * 4f).normalized;
                    float nDotL = Mathf.Clamp01(Vector3.Dot(normal, lightDir));
                    float hillshade = Mathf.Lerp(0.58f, 1.28f, nDotL);

                    // Curvas de nível topográficas (isolinhas a cada 50m / 200m)
                    float contourInterval = 50f;
                    float contourDist = Mathf.Abs(elevMeters % contourInterval);
                    bool isContour = showContoursLayer && (contourDist < 1.8f || contourDist > contourInterval - 1.8f);
                    bool isMajorContour = isContour && (Mathf.Abs(elevMeters % 200f) < 2.5f || Mathf.Abs(elevMeters % 200f) > 197.5f);

                    // Paleta realista por elevação e bioma
                    Color terrainCol;
                    if (h <= seaLvl + 0.012f)
                    {
                        // Água / Bacia litorânea ou lacustre
                        float depthNorm = Mathf.Clamp01((seaLvl + 0.012f - h) / 0.04f);
                        Color shallow = new Color(0.16f, 0.48f, 0.72f);
                        Color deep = new Color(0.06f, 0.18f, 0.42f);
                        terrainCol = Color.Lerp(shallow, deep, depthNorm) * hillshade;
                    }
                    else if (h <= seaLvl + 0.035f)
                    {
                        // Praia / Orla arenosa
                        terrainCol = new Color(0.80f, 0.75f, 0.56f) * hillshade;
                    }
                    else if (steep > 26f)
                    {
                        // Encostas rochosas / Penhascos
                        float rockFactor = Mathf.Clamp01((steep - 26f) / 18f);
                        Color rock = new Color(0.48f, 0.46f, 0.43f);
                        Color darkRock = new Color(0.32f, 0.30f, 0.28f);
                        terrainCol = Color.Lerp(rock, darkRock, rockFactor) * hillshade;
                    }
                    else
                    {
                        // Gradação altimétrica de vegetação e lavoura
                        float vegNoise = Mathf.PerlinNoise(nx * 7.5f + 14f, ny * 7.5f + 28f);
                        Color lushValley = new Color(0.23f, 0.46f, 0.22f); // Vale verdejante
                        Color agricultural = new Color(0.44f, 0.53f, 0.27f); // Campo / lavoura
                        Color highlands = new Color(0.56f, 0.51f, 0.32f); // Planalto / cerrado
                        Color subAlpine = new Color(0.50f, 0.46f, 0.40f); // Serras elevadas

                        float relElev = Mathf.Clamp01((h - seaLvl) / (1f - seaLvl));
                        Color baseCol;
                        if (relElev < 0.28f)
                            baseCol = Color.Lerp(lushValley, agricultural, relElev / 0.28f);
                        else if (relElev < 0.65f)
                            baseCol = Color.Lerp(agricultural, highlands, (relElev - 0.28f) / 0.37f);
                        else
                            baseCol = Color.Lerp(highlands, subAlpine, (relElev - 0.65f) / 0.35f);

                        terrainCol = Color.Lerp(baseCol, lushValley, vegNoise * 0.25f) * hillshade;

                        // Neve nos cumes mais altos
                        if (h > 0.86f)
                        {
                            terrainCol = Color.Lerp(terrainCol, new Color(0.94f, 0.96f, 1.0f), (h - 0.86f) / 0.14f);
                        }
                    }

                    // Aplica escurecimento sutil das curvas de nível
                    if (isContour && h > seaLvl + 0.02f)
                    {
                        terrainCol *= (isMajorContour ? 0.70f : 0.84f);
                    }

                    // 4. Aplicação da Silhueta no Formato da Província
                    Color finalCol;
                    if (hasRegionMask)
                    {
                        if (isBorder[y, x])
                        {
                            // Traçado da Linha de Fronteira Tática (Ciano tático brilhante)
                            finalCol = new Color(0.15f, 0.95f, 1.0f, 1.0f);
                        }
                        else if (isOuterGlow[y, x])
                        {
                            // Halo / Glow exterior suave da fronteira
                            finalCol = new Color(0.12f, 0.75f, 0.95f, 0.55f);
                        }
                        else if (insideMask[y, x])
                        {
                            // TERRITÓRIO DA PROVÍNCIA: Totalmente visível, nítido e colorido
                            finalCol = new Color(terrainCol.r, terrainCol.g, terrainCol.b, 1.0f);
                        }
                        else
                        {
                            // FORA DA PROVÍNCIA:
                            // Azul APENAS onde for água (pid == 0); cinza onde forem outros municípios/províncias (pid > 0)!
                            ushort pid = pixelRegionIds[y, x];
                            if (pid == 0)
                            {
                                // ÁGUA / OCEANO ABERTO: Azul cartográfico com batimetria e profundidade
                                float depthNorm = Mathf.Clamp01((seaLvl + 0.02f - h) / 0.05f);
                                Color shallowOcean = new Color(0.12f, 0.38f, 0.62f);
                                Color deepOcean = new Color(0.04f, 0.15f, 0.30f);
                                finalCol = Color.Lerp(shallowOcean, deepOcean, depthNorm) * Mathf.Clamp(hillshade, 0.75f, 1.25f);
                            }
                            else
                            {
                                // OUTROS MUNICÍPIOS / PROVÍNCIAS: Terreno em tom cinza neutro elegante
                                // com relevo / hillshading topográfico
                                float baseGray = 0.35f;
                                float grayShaded = Mathf.Clamp(baseGray * hillshade, 0.22f, 0.52f);

                                // Se for divisa entre dois municípios/províncias vizinhos distintos, desenha linha divisória sutil
                                bool isNeighborBorder = false;
                                if (x < res - 1 && pixelRegionIds[y, x + 1] > 0 && pixelRegionIds[y, x + 1] != pid && !insideMask[y, x + 1])
                                    isNeighborBorder = true;
                                else if (y < res - 1 && pixelRegionIds[y + 1, x] > 0 && pixelRegionIds[y + 1, x] != pid && !insideMask[y + 1, x])
                                    isNeighborBorder = true;

                                if (isNeighborBorder)
                                {
                                    finalCol = new Color(0.18f, 0.18f, 0.20f, 1.0f);
                                }
                                else
                                {
                                    finalCol = new Color(grayShaded, grayShaded, grayShaded, 1.0f);
                                }
                            }
                        }
                    }
                    else
                    {
                        finalCol = terrainCol;
                    }

                    cols[y * res + x] = finalCol;
                }
            }

            regionalMapTex.SetPixels(cols);
            regionalMapTex.wrapMode = TextureWrapMode.Clamp;
            regionalMapTex.filterMode = FilterMode.Bilinear;
            regionalMapTex.Apply();

            mapTextureGenerated = true;
            lastGeneratedRegionId = region != null ? region.id : (activeSave != null ? activeSave.regionId : 0);
        }

        #endregion

        #region Renderização do Mapa Tático GIS Fullscreen [M]

        private void DrawInteractiveRegionalMap()
        {
            EnsureMapTexture();

            var rsm = RegionalSandboxManager.Instance;
            var region = GetActiveRegion();
            float worldW = rsm != null ? rsm.worldWidthMeters : 60000f;
            float worldL = rsm != null ? rsm.worldLengthMeters : 60000f;
            float aspect = worldW / Mathf.Max(1f, worldL);

            // 1. Tela Inteira (Fullscreen GIS Command Canvas)
            Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);
            lastMapRect = screenRect;

            // Fundo tático de centro de comando
            GUI.color = new Color(0.02f, 0.04f, 0.07f, 0.97f);
            GUI.DrawTexture(screenRect, whiteTex);
            GUI.color = Color.white;

            // 2. Cálculo do Enquadramento e Proporção do Terreno com Zoom & Pan
            float availW = Screen.width * 0.88f;
            float availH = (Screen.height - 90f) * 0.88f;
            float baseW, baseH;
            if (availW / availH > aspect)
            {
                baseH = availH;
                baseW = baseH * aspect;
            }
            else
            {
                baseW = availW;
                baseH = baseW / aspect;
            }

            float drawW = baseW * mapZoom;
            float drawH = baseH * mapZoom;
            Vector2 mapCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + mapPanOffset;
            Rect mapImageRect = new Rect(mapCenter.x - drawW * 0.5f, mapCenter.y - drawH * 0.5f, drawW, drawH);

            // 3. Interação de Pan & Zoom com Atalhos e Roda do Mouse
            HandleMapInteraction(mapImageRect, worldW, worldL, baseW, baseH);

            // 4. Renderização do Terreno e Malha Cartográfica no Canvas Clipado
            GUI.BeginGroup(screenRect);

            // Textura do Terreno (com contorno real da província)
            if (regionalMapTex != null)
            {
                GUI.DrawTexture(mapImageRect, regionalMapTex);
            }

            // Grade Tática MGRS / UTM (se habilitada)
            if (showGridLayer)
            {
                float gridSpacing = 80f * mapZoom;
                while (gridSpacing < 50f) gridSpacing *= 2f;
                while (gridSpacing > 140f) gridSpacing *= 0.5f;

                GUI.color = new Color(0.18f, 0.65f, 1f, 0.10f);
                float startX = (mapCenter.x % gridSpacing);
                for (float gx = startX; gx < Screen.width; gx += gridSpacing)
                    GUI.DrawTexture(new Rect(gx, 44f, 1, Screen.height - 80f), whiteTex);

                float startY = (mapCenter.y % gridSpacing);
                for (float gy = startY; gy < Screen.height - 36f; gy += gridSpacing)
                {
                    if (gy >= 44f)
                        GUI.DrawTexture(new Rect(0, gy, Screen.width, 1), whiteTex);
                }
                GUI.color = Color.white;
            }

            // 5. Camadas Vetoriais: Hidrografia (Rios), Malha Viária, Cidades e Marcos
            DrawInfrastructureOnMap(mapImageRect, worldW, worldL);

            // 5.1. Nomes e Rótulos das Divisas / Municípios Vizinhos na Área Cinza
            DrawNeighborDivisasOnMap(mapImageRect, region);

            // Marcos Geográficos (Montanhas, Vulcões, Chapadas)
            if (showLandmarksLayer && LandmarkManager.Placed != null)
            {
                foreach (var lm in LandmarkManager.Placed)
                {
                    Vector2 lmScreen = WorldToMapScreen(lm.Key, worldW, worldL, mapImageRect);
                    if (lmScreen.x >= -60 && lmScreen.x <= Screen.width + 60 && lmScreen.y >= 20 && lmScreen.y <= Screen.height + 20)
                    {
                        GUI.color = new Color(0.85f, 0.72f, 1f, 0.85f);
                        GUI.Label(new Rect(lmScreen.x - 7f, lmScreen.y - 7f, 14f, 14f), "▲", titleStyle);
                        if (mapZoom > 1.3f)
                        {
                            DrawTextWithShadow(new Rect(lmScreen.x + 8f, lmScreen.y - 8f, 150f, 16f), lm.Value, subtitleStyle, new Color(0.9f, 0.8f, 1f), Color.black);
                        }
                        GUI.color = Color.white;
                    }
                }
            }

            // 6. Alvo de Missão Ativa
            if (MissionManager.Instance != null && MissionManager.Instance.hasActiveContract && !MissionManager.Instance.currentContract.isCompleted)
            {
                Vector2 targetPos = WorldToMapScreen(MissionManager.Instance.deliveryTargetPosition, worldW, worldL, mapImageRect);
                GUI.color = new Color(1f, 0.85f, 0.2f);
                GUI.Label(new Rect(targetPos.x - 12f, targetPos.y - 14f, 24f, 24f), "🎯", titleStyle);
                DrawTextWithShadow(new Rect(targetPos.x + 12f, targetPos.y - 10f, 140f, 20f), "ALVO DA MISSÃO", subtitleStyle, new Color(1f, 0.85f, 0.2f), Color.black);
                GUI.color = Color.white;
            }

            // 7. Marcador Customizado de Waypoint com Linha até o Jogador
            var player = PlayerCharacterController.Instance;
            if (CustomWaypoint.HasValue)
            {
                Vector2 wScreen = WorldToMapScreen(CustomWaypoint.Value, worldW, worldL, mapImageRect);

                if (player != null)
                {
                    Vector2 pScreen = WorldToMapScreen(player.transform.position, worldW, worldL, mapImageRect);
                    GuiLine(pScreen, wScreen, 2f, new Color(1f, 0.25f, 0.95f, 0.75f));
                    float distKm = Vector3.Distance(player.transform.position, CustomWaypoint.Value) / 1000f;
                    Vector2 mid = (pScreen + wScreen) * 0.5f;
                    DrawTextWithShadow(new Rect(mid.x - 35f, mid.y - 10f, 70f, 20f), $"{distKm:F1} km", subtitleStyle, new Color(1f, 0.4f, 1f), Color.black);
                }

                GUI.color = new Color(1f, 0.25f, 0.95f);
                GUI.Label(new Rect(wScreen.x - 10f, wScreen.y - 14f, 24f, 24f), "🚩", titleStyle);
                DrawTextWithShadow(new Rect(wScreen.x + 10f, wScreen.y - 10f, 120f, 20f), "DESTINO", subtitleStyle, new Color(1f, 0.45f, 1f), Color.black);
                GUI.color = Color.white;
            }

            // 8. Veículo Inicial
            if (rsm != null && rsm.starterVehicle != null)
            {
                DrawMapMarker(rsm.starterVehicle.transform.position, worldW, worldL, mapImageRect, "🚜 Veículo", new Color(1f, 0.65f, 0.25f));
            }

            // 9. Jogador com Radar Pulsante e Seta de Rotação 360°
            if (player != null)
            {
                Vector2 pScreen = WorldToMapScreen(player.transform.position, worldW, worldL, mapImageRect);

                // Ping de radar animado
                float pulse = (Mathf.Sin(Time.realtimeSinceStartup * 4f) + 1f) * 0.5f;
                float r = 16f + pulse * 10f;
                GUI.color = new Color(0.2f, 1f, 0.45f, 0.35f - pulse * 0.22f);
                GUI.DrawTexture(new Rect(pScreen.x - r * 0.5f, pScreen.y - r * 0.5f, r, r), whiteTex);

                // Seta de direção em rotação analógica
                Matrix4x4 savedMat = GUI.matrix;
                float heading = player.transform.eulerAngles.y;
                GUIUtility.RotateAroundPivot(heading, pScreen);
                GUI.color = new Color(0.2f, 1f, 0.45f);
                GUI.Label(new Rect(pScreen.x - 12f, pScreen.y - 12f, 24f, 24f), "▲", titleStyle);
                GUI.matrix = savedMat;

                GUI.color = Color.white;
                DrawTextWithShadow(new Rect(pScreen.x + 12f, pScreen.y - 10f, 80f, 20f), "VOCÊ", subtitleStyle, new Color(0.3f, 1f, 0.5f), Color.black);
            }

            GUI.EndGroup();

            // 10. Barras e Painéis Flutuantes HUD Fullscreen
            DrawFullscreenTopBar(mapImageRect, worldW, worldL, region);
            DrawFloatingLayerPanel();
            DrawFloatingNavigationPanel(worldW, worldL, baseW, baseH);
            DrawFullscreenBottomBar(mapImageRect, worldW);
        }

        #endregion

        #region Painéis Flutuantes e HUD Fullscreen

        private void DrawFullscreenTopBar(Rect mapRect, float worldW, float worldL, RegionData region)
        {
            float barH = 46f;
            Rect topRect = new Rect(0, 0, Screen.width, barH);

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);
            GUI.DrawTexture(topRect, whiteTex);
            GUI.color = new Color(0.18f, 0.75f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(0, barH - 2f, Screen.width, 2f), whiteTex);
            GUI.color = Color.white;

            // Informações da Província / País
            string regName = region != null ? region.name : (activeSave != null ? activeSave.regionName : "Região");
            string country = region != null ? region.country : (activeSave != null ? activeSave.countryName : "Planeta Terra");
            string type = region != null ? region.type : "Estado / Província";
            int area = region != null && region.realAreaKm2 > 0 ? region.realAreaKm2 : (int)((worldW * worldL) / 1000000f);

            GUI.Label(new Rect(16f, 6f, 420f, 20f), $"🗺️ {regName.ToUpper()}, {country.ToUpper()}", titleStyle);
            GUI.Label(new Rect(16f, 24f, 450f, 18f), $"{type}  •  Área: {area:N0} km²  •  Dimensão: {worldW / 1000f:F0} km × {worldL / 1000f:F0} km", subtitleStyle);

            // Telemetria Geográfica em Tempo Real sob o Cursor do Mouse
            Vector2 mouse = Event.current.mousePosition;
            if (mapRect.Contains(mouse) && mapRect.width > 0 && mapRect.height > 0)
            {
                float u = (mouse.x - mapRect.x) / mapRect.width;
                float v = 1.0f - ((mouse.y - mapRect.y) / mapRect.height);

                float cursorLat = region != null ? Mathf.Lerp(region.minLat, region.maxLat, v) : 0f;
                float cursorLon = region != null ? Mathf.Lerp(region.minLon, region.maxLon, u) : 0f;

                float wx = (u - 0.5f) * worldW;
                float wz = (v - 0.5f) * worldL;
                var rsm = RegionalSandboxManager.Instance;
                float altM = (rsm != null && rsm.activeTerrain != null) ? rsm.activeTerrain.SampleHeight(new Vector3(wx, 0, wz)) : 0f;

                var player = PlayerCharacterController.Instance;
                float distKm = player != null ? Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(wx, wz)) / 1000f : 0f;

                string latStr = cursorLat >= 0 ? $"{cursorLat:F2}° N" : $"{-cursorLat:F2}° S";
                string lonStr = cursorLon >= 0 ? $"{cursorLon:F2}° L" : $"{-cursorLon:F2}° O";

                ushort hoveredId = RegionBoundaryService.GetRegionIdAt(cursorLat, cursorLon);
                string locStr = "";
                if (hoveredId == (region != null ? region.id : 0))
                {
                    locStr = regName;
                }
                else if (hoveredId > 0)
                {
                    var hReg = RegionNeighbors.GetRegionById(hoveredId);
                    locStr = hReg != null ? $"Divisa: {hReg.name} ({hReg.country})" : "Outro Município / Província";
                }
                else
                {
                    locStr = "Oceano / Águas Abertas";
                }

                GUI.color = new Color(0.25f, 0.85f, 1f);
                GUI.Label(new Rect(Screen.width * 0.36f, 13f, 560f, 22f), $"🌐 [{locStr}]  |  LAT: {latStr}  |  LON: {lonStr}  |  ALT: {altM:F0}m  |  DIST: {distKm:F1} km", subtitleStyle);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.6f, 0.75f, 0.9f);
                GUI.Label(new Rect(Screen.width * 0.42f, 13f, 400f, 22f), "🌐 SISTEMA CARTOGRÁFICO MILITAR GIS 1:1 • TEMPO REAL", subtitleStyle);
                GUI.color = Color.white;
            }

            // Botão Fechar [✖]
            if (GUI.Button(new Rect(Screen.width - 48f, 8f, 36f, 30f), "✖", menuButtonDangerStyle))
            {
                CloseRegionalMap();
            }
        }

        private void DrawFloatingLayerPanel()
        {
            float panelW = 160f;
            float panelH = 198f;
            Rect panelRect = new Rect(14f, 58f, panelW, panelH);

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            GUI.DrawTexture(panelRect, whiteTex);
            GUI.color = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 1), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(6);
            GUILayout.Label("  📡 CAMADAS GIS", titleStyle);
            GUILayout.Space(4);

            showRoadsLayer = GUILayout.Toggle(showRoadsLayer, "  🛣️ Rodovias / Trilhos");
            showRiversLayer = GUILayout.Toggle(showRiversLayer, "  🌊 Hidrografia / Rios");
            showCitiesLayer = GUILayout.Toggle(showCitiesLayer, "  🏛️ Cidades & Sedes");
            showDivisasLayer = GUILayout.Toggle(showDivisasLayer, "  🧭 Divisas / Vizinhos");
            showContoursLayer = GUILayout.Toggle(showContoursLayer, "  ⛰️ Curvas de Nível");
            showGridLayer = GUILayout.Toggle(showGridLayer, "  🌐 Grade Tática");
            showLandmarksLayer = GUILayout.Toggle(showLandmarksLayer, "  ▲ Marcos e Serras");

            GUILayout.EndArea();
        }

        private void DrawFloatingNavigationPanel(float worldW, float worldL, float baseW, float baseH)
        {
            float navW = 54f;
            float navH = 250f;
            Rect navRect = new Rect(Screen.width - navW - 14f, 58f, navW, navH);

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.92f);
            GUI.DrawTexture(navRect, whiteTex);
            GUI.color = new Color(0.2f, 0.6f, 0.9f, 0.7f);
            GUI.DrawTexture(new Rect(navRect.x, navRect.y, navRect.width, 1), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(navRect);
            GUILayout.Space(6);

            // Zoom In [+]
            if (GUILayout.Button("➕", menuButtonStyle, GUILayout.Height(32)))
            {
                ZoomAt(mapZoom * 1.3f, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            }

            // Indicador de Nível de Zoom
            GUILayout.Space(2);
            GUI.color = new Color(0.35f, 0.85f, 1f);
            GUILayout.Label($"{(int)(mapZoom * 100)}%", compassLabelStyle);
            GUI.color = Color.white;
            GUILayout.Space(2);

            // Zoom Out [-]
            if (GUILayout.Button("➖", menuButtonStyle, GUILayout.Height(32)))
            {
                ZoomAt(mapZoom * 0.77f, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            }

            GUILayout.Space(8);

            // Centralizar no Jogador [🎯]
            if (GUILayout.Button("🎯", menuButtonStyle, GUILayout.Height(32)))
            {
                CenterOnPlayer(worldW, worldL, baseW, baseH);
            }

            // Centralizar na Capital [★]
            if (GUILayout.Button("★", menuButtonStyle, GUILayout.Height(32)))
            {
                CenterOnCapital(worldW, worldL, baseW, baseH);
            }

            // Resetar Câmera [⟲]
            if (GUILayout.Button("⟲", menuButtonStyle, GUILayout.Height(32)))
            {
                mapPanOffset = Vector2.zero;
                mapZoom = 1.0f;
            }

            if (CustomWaypoint.HasValue)
            {
                GUILayout.Space(4);
                if (GUILayout.Button("❌", menuButtonDangerStyle, GUILayout.Height(28)))
                {
                    ClearCustomWaypoint();
                }
            }

            GUILayout.EndArea();
        }

        private void DrawFullscreenBottomBar(Rect mapRect, float worldW)
        {
            float barH = 34f;
            Rect btmRect = new Rect(0, Screen.height - barH, Screen.width, barH);

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);
            GUI.DrawTexture(btmRect, whiteTex);
            GUI.color = new Color(0.18f, 0.75f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(0, btmRect.y, Screen.width, 2f), whiteTex);
            GUI.color = Color.white;

            // 1. Divisas / Vizinhos de Fronteira
            if (!bordersComputed)
            {
                bordersComputed = true;
                var region = GetActiveRegion();
                borderLabels = RegionNeighbors.GetBorderLabels(region);
            }
            string divisas = (borderLabels != null && borderLabels.Count > 0)
                ? string.Join("  •  ", borderLabels)
                : "Sem divisas terrestres imediatas (Zona Costeira/Oceano)";

            GUI.color = new Color(0.85f, 0.95f, 1f);
            GUI.Label(new Rect(14f, btmRect.y + 7f, Screen.width * 0.45f, 20f), $"🧭 Divisas: {divisas}", subtitleStyle);
            GUI.color = Color.white;

            // 2. Dicas de Navegação
            GUI.color = new Color(0.7f, 0.8f, 0.9f);
            GUI.Label(new Rect(Screen.width * 0.42f, btmRect.y + 7f, 380f, 20f), "💡 Scroll: Zoom  •  Arraste: Mover  •  Clique: Destino  •  F: Focar Jogador", subtitleStyle);
            GUI.color = Color.white;

            // 3. Barra de Escala Dinâmica (Metric Scale Bar)
            if (mapRect.width > 0)
            {
                float pixelsPerMeter = mapRect.width / worldW;
                float[] metricSteps = { 200f, 500f, 1000f, 2000f, 5000f, 10000f, 20000f, 50000f, 100000f, 200000f };
                float bestStep = 10000f;

                foreach (var step in metricSteps)
                {
                    float px = step * pixelsPerMeter;
                    if (px >= 60f && px <= 160f)
                    {
                        bestStep = step;
                        break;
                    }
                }

                float scaleBarPx = bestStep * pixelsPerMeter;
                float scaleX = Screen.width - scaleBarPx - 24f;
                float scaleY = btmRect.y + 16f;
                string scaleLabel = bestStep >= 1000f ? $"{(bestStep / 1000f):F0} km" : $"{bestStep:F0} m";

                GUI.color = Color.white;
                // Barra e marcadores
                GUI.DrawTexture(new Rect(scaleX, scaleY, scaleBarPx, 2f), whiteTex);
                GUI.DrawTexture(new Rect(scaleX, scaleY - 4f, 2f, 8f), whiteTex);
                GUI.DrawTexture(new Rect(scaleX + scaleBarPx * 0.5f, scaleY - 2f, 1f, 5f), whiteTex);
                GUI.DrawTexture(new Rect(scaleX + scaleBarPx - 2f, scaleY - 4f, 2f, 8f), whiteTex);
                GUI.Label(new Rect(scaleX - 20f, scaleY - 16f, scaleBarPx + 40f, 16f), scaleLabel, compassLabelStyle);
                GUI.color = Color.white;
            }
        }

        #endregion

        #region Interação Cartográfica, Pan & Zoom Preciso

        private void ZoomAt(float newZoom, Vector2 pivot)
        {
            newZoom = Mathf.Clamp(newZoom, MinMapZoom, MaxMapZoom);
            if (Mathf.Abs(newZoom - mapZoom) < 0.001f) return;

            // Fórmula GIS: mantém a coordenada geográfica sob o cursor perfeitamente estática durante o zoom
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 oldMapCenter = screenCenter + mapPanOffset;
            Vector2 rel = pivot - oldMapCenter;
            float ratio = newZoom / mapZoom;
            Vector2 newMapCenter = pivot - rel * ratio;

            mapPanOffset = newMapCenter - screenCenter;
            mapZoom = newZoom;
        }

        private void CenterOnPlayer(float worldW, float worldL, float baseW, float baseH)
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            float u = (player.transform.position.x + worldW * 0.5f) / worldW;
            float v = (player.transform.position.z + worldL * 0.5f) / worldL;

            float drawW = baseW * mapZoom;
            float drawH = baseH * mapZoom;
            float relX = (u - 0.5f) * drawW;
            float relY = -(v - 0.5f) * drawH;

            mapPanOffset = new Vector2(-relX, -relY);
        }

        private void CenterOnCapital(float worldW, float worldL, float baseW, float baseH)
        {
            if (mapCities != null)
            {
                var cap = mapCities.Find(c => c.isCapital);
                if (cap == null && mapCities.Count > 0) cap = mapCities[0];

                if (cap != null && mapCityPos != null && mapCityPos.Count > 0)
                {
                    int idx = mapCities.IndexOf(cap);
                    if (idx >= 0 && idx < mapCityPos.Count)
                    {
                        Vector3 capPos = mapCityPos[idx];
                        float u = (capPos.x + worldW * 0.5f) / worldW;
                        float v = (capPos.z + worldL * 0.5f) / worldL;

                        float drawW = baseW * mapZoom;
                        float drawH = baseH * mapZoom;
                        float relX = (u - 0.5f) * drawW;
                        float relY = -(v - 0.5f) * drawH;

                        mapPanOffset = new Vector2(-relX, -relY);
                    }
                }
            }
        }

        private bool IsMouseOverUI(Vector2 mouse)
        {
            // Barra superior
            if (mouse.y <= 48f) return true;
            // Barra inferior
            if (mouse.y >= Screen.height - 36f) return true;
            // Painel esquerdo de camadas
            if (new Rect(14f, 58f, 160f, 175f).Contains(mouse)) return true;
            // Painel direito de navegação
            if (new Rect(Screen.width - 68f, 58f, 54f, 250f).Contains(mouse)) return true;

            return false;
        }

        private void HandleMapInteraction(Rect mapImageRect, float worldW, float worldL, float baseW, float baseH)
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            bool overUI = IsMouseOverUI(mouse);

            // Zoom suave com a roda do mouse centralizado no cursor
            if (e.type == EventType.ScrollWheel && !overUI)
            {
                float factor = e.delta.y < 0f ? 1.18f : 0.847f;
                ZoomAt(mapZoom * factor, mouse);
                e.Use();
            }

            // Atalhos de teclado no modo mapa
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus || e.keyCode == KeyCode.Equals)
                {
                    ZoomAt(mapZoom * 1.25f, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus || e.keyCode == KeyCode.Underscore)
                {
                    ZoomAt(mapZoom * 0.80f, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                    e.Use();
                }
                else if (e.keyCode == KeyCode.F)
                {
                    CenterOnPlayer(worldW, worldL, baseW, baseH);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Home)
                {
                    mapPanOffset = Vector2.zero;
                    mapZoom = 1.0f;
                    e.Use();
                }
            }

            // Movimentação suave contínua por teclado (WASD ou Setas)
            float panStep = 500f * Time.unscaledDeltaTime;
            if (Keyboard.current != null)
            {
                Vector2 panDir = Vector2.zero;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) panDir.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) panDir.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) panDir.x += 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) panDir.x -= 1f;
                if (panDir != Vector2.zero)
                {
                    mapPanOffset += panDir.normalized * panStep;
                }
            }

            // Arraste do Mapa (Pan com botão esquerdo ou direito)
            if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1 || e.button == 2) && !overUI)
            {
                isDraggingMap = true;
                dragStartMouse = mouse;
                dragStartPan = mapPanOffset;
                e.Use();
            }

            if (isDraggingMap)
            {
                if (e.type == EventType.MouseDrag)
                {
                    mapPanOffset = dragStartPan + (mouse - dragStartMouse);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    isDraggingMap = false;
                    float clickDist = Vector2.Distance(mouse, dragStartMouse);

                    // Duplo clique = Zoom In rápido no ponto
                    if (e.clickCount == 2 && clickDist < 6f && !overUI)
                    {
                        ZoomAt(mapZoom * 1.8f, mouse);
                    }
                    // Clique simples com botão esquerdo = Cria Waypoint
                    else if (e.button == 0 && clickDist < 6f && !overUI)
                    {
                        float u = (mouse.x - mapImageRect.x) / mapImageRect.width;
                        float v = 1.0f - ((mouse.y - mapImageRect.y) / mapImageRect.height);

                        if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                        {
                            var rsm = RegionalSandboxManager.Instance;
                            float wx = (u - 0.5f) * worldW;
                            float wz = (v - 0.5f) * worldL;
                            float wy = rsm != null && rsm.activeTerrain != null ? rsm.activeTerrain.SampleHeight(new Vector3(wx, 0, wz)) : 0f;
                            SetCustomWaypoint(new Vector3(wx, wy, wz));
                        }
                    }
                    e.Use();
                }
            }
        }

        #endregion

        #region Malha Viária, Hidrografia & Cidades no Mapa Tático

        private struct MapPolyline { public Planet.RoadClass cls; public Vector3[] pts; }
        private struct MapRiverPoly { public float width; public Vector3[] pts; }

        private List<MapPolyline> mapRoads;
        private List<MapRiverPoly> mapRivers;
        private List<Planet.RegionCity> mapCities;
        private List<Vector3> mapCityPos;
        private int infraCacheRegionId = -1;
        private const int MaxMapRoadSegments = 2500;
        private const int MaxMapRiverSegments = 1600;

        private void EnsureInfraMapCache(float worldW, float worldL)
        {
            var region = GetActiveRegion();
            if (region == null) return;
            if (infraCacheRegionId == region.id && mapRoads != null && mapRivers != null) return;

            infraCacheRegionId = region.id;

            // 1. Rodovias e Ferrovias
            mapRoads = new List<MapPolyline>();
            foreach (var poly in Planet.RegionRoadsDatabase.GetRoads(region.id))
            {
                if (poly.points == null || poly.points.Length < 2) continue;
                var pts = new Vector3[poly.points.Length];
                for (int i = 0; i < pts.Length; i++)
                    pts[i] = Planet.RegionGeoProjection.ToLocal(poly.points[i].x, poly.points[i].y, region, worldW, worldL);
                mapRoads.Add(new MapPolyline { cls = poly.roadClass, pts = pts });
            }

            // 2. Hidrografia e Rios Reais
            mapRivers = new List<MapRiverPoly>();
            foreach (var r in Planet.RegionRiversDatabase.GetRivers(region.id))
            {
                if (r.points == null || r.points.Length < 2) continue;
                var pts = new Vector3[r.points.Length];
                for (int i = 0; i < pts.Length; i++)
                    pts[i] = Planet.RegionGeoProjection.ToLocal(r.points[i].x, r.points[i].y, region, worldW, worldL);
                mapRivers.Add(new MapRiverPoly { width = r.width, pts = pts });
            }

            // 3. Cidades e Polos Econômicos
            mapCities = Planet.RegionCitiesDatabase.GetCities(region.id);
            mapCityPos = new List<Vector3>();
            if (mapCities != null)
            {
                foreach (var c in mapCities)
                    mapCityPos.Add(Planet.RegionGeoProjection.ToLocal(c.lat, c.lon, region, worldW, worldL));
            }
        }

        private void DrawInfrastructureOnMap(Rect mapRect, float worldW, float worldL)
        {
            EnsureInfraMapCache(worldW, worldL);

            // 1. Desenho de Rios Reais (Hidrografia)
            if (showRiversLayer && mapRivers != null)
            {
                int riverBudget = MaxMapRiverSegments;
                Color riverCol = new Color(0.15f, 0.68f, 1.0f, 0.88f);
                foreach (var river in mapRivers)
                {
                    if (riverBudget <= 0) break;
                    float rw = Mathf.Clamp(river.width * 0.12f * mapZoom, 1.8f, 6.0f);
                    for (int i = 0; i < river.pts.Length - 1 && riverBudget > 0; i++, riverBudget--)
                    {
                        Vector2 a = WorldToMapScreen(river.pts[i], worldW, worldL, mapRect);
                        Vector2 b = WorldToMapScreen(river.pts[i + 1], worldW, worldL, mapRect);
                        if ((a.x < 0 && b.x < 0) || (a.x > Screen.width && b.x > Screen.width) ||
                            (a.y < 0 && b.y < 0) || (a.y > Screen.height && b.y > Screen.height))
                            continue;
                        GuiLine(a, b, rw, riverCol);
                    }
                }
            }

            // 2. Desenho de Rodovias e Ferrovias
            if (showRoadsLayer && mapRoads != null)
            {
                int roadBudget = MaxMapRoadSegments;
                foreach (var poly in mapRoads)
                {
                    if (roadBudget <= 0) break;
                    Color col = RoadMapColor(poly.cls);
                    float w = RoadMapWidth(poly.cls) * Mathf.Clamp(mapZoom * 0.75f, 0.8f, 2.8f);
                    for (int i = 0; i < poly.pts.Length - 1 && roadBudget > 0; i++, roadBudget--)
                    {
                        Vector2 a = WorldToMapScreen(poly.pts[i], worldW, worldL, mapRect);
                        Vector2 b = WorldToMapScreen(poly.pts[i + 1], worldW, worldL, mapRect);
                        if ((a.x < 0 && b.x < 0) || (a.x > Screen.width && b.x > Screen.width) ||
                            (a.y < 0 && b.y < 0) || (a.y > Screen.height && b.y > Screen.height))
                            continue;
                        GuiLine(a, b, w, col);
                    }
                }
            }

            // 3. Desenho de Cidades e Capitais
            if (showCitiesLayer && mapCities != null)
            {
                for (int i = 0; i < mapCities.Count; i++)
                {
                    var c = mapCities[i];
                    Vector2 p = WorldToMapScreen(mapCityPos[i], worldW, worldL, mapRect);
                    if (p.x < -100 || p.x > Screen.width + 100 || p.y < -50 || p.y > Screen.height + 50) continue;

                    if (c.isCapital)
                    {
                        float r = 11f;
                        GUI.color = new Color(1f, 0.85f, 0.2f, 1f);
                        GUI.DrawTexture(new Rect(p.x - r * 0.5f, p.y - r * 0.5f, r, r), whiteTex);
                        GUI.color = Color.white;
                        DrawTextWithShadow(new Rect(p.x + 8f, p.y - 10f, 160f, 20f), "★ " + c.name.ToUpper(), titleStyle, new Color(1f, 0.9f, 0.3f), Color.black);
                    }
                    else
                    {
                        if (c.rank <= 2 || mapZoom > 1.25f)
                        {
                            float r = c.rank <= 2 ? 7f : 5f;
                            Color col = c.rank <= 2 ? new Color(0.35f, 0.88f, 1f) : new Color(0.7f, 0.85f, 0.95f, 0.8f);
                            GUI.color = col;
                            GUI.DrawTexture(new Rect(p.x - r * 0.5f, p.y - r * 0.5f, r, r), whiteTex);
                            GUI.color = Color.white;
                            if (c.rank <= 2 || mapZoom > 2.0f)
                            {
                                DrawTextWithShadow(new Rect(p.x + 6f, p.y - 8f, 140f, 16f), c.name, subtitleStyle, Color.white, Color.black);
                            }
                        }
                    }
                }
            }
        }

        private void GuiLine(Vector2 a, Vector2 b, float width, Color col)
        {
            float len = Vector2.Distance(a, b);
            if (len < 0.5f) return;
            Matrix4x4 saved = GUI.matrix;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            GUI.color = col;
            GUIUtility.RotateAroundPivot(angle, a);
            GUI.DrawTexture(new Rect(a.x, a.y - width * 0.5f, len, width), whiteTex);
            GUI.matrix = saved;
            GUI.color = Color.white;
        }

        private void DrawTextWithShadow(Rect r, string text, GUIStyle style, Color textColor, Color shadowColor)
        {
            GUI.color = shadowColor;
            GUI.Label(new Rect(r.x + 1f, r.y + 1f, r.width, r.height), text, style);
            GUI.color = textColor;
            GUI.Label(r, text, style);
            GUI.color = Color.white;
        }

        private static Color RoadMapColor(Planet.RoadClass c)
        {
            switch (c)
            {
                case Planet.RoadClass.Expressway: return new Color(1f, 0.55f, 0.15f, 0.95f);
                case Planet.RoadClass.MajorHighway: return new Color(1f, 0.82f, 0.25f, 0.95f);
                case Planet.RoadClass.SecondaryHighway: return new Color(0.92f, 0.92f, 0.94f, 0.85f);
                case Planet.RoadClass.Railroad: return new Color(0.55f, 0.78f, 1f, 0.90f);
                default: return new Color(0.4f, 0.9f, 0.5f, 0.7f);
            }
        }

        private static float RoadMapWidth(Planet.RoadClass c)
        {
            switch (c)
            {
                case Planet.RoadClass.Expressway: return 3.2f;
                case Planet.RoadClass.MajorHighway: return 2.6f;
                case Planet.RoadClass.SecondaryHighway: return 1.6f;
                case Planet.RoadClass.Railroad: return 2.2f;
                default: return 1.5f;
            }
        }

        #endregion

        #region Projeções de Coordenadas e Marcadores

        private Vector2 WorldToMapScreen(Vector3 worldPos, float worldW, float worldL, Rect mapRect)
        {
            float u = (worldPos.x + worldW * 0.5f) / worldW;
            float v = (worldPos.z + worldL * 0.5f) / worldL;
            float px = mapRect.x + u * mapRect.width;
            float py = mapRect.y + (1.0f - v) * mapRect.height;
            return new Vector2(px, py);
        }

        private void DrawMapMarker(Vector3 worldPos, float worldW, float worldL, Rect mapRect, string label, Color color)
        {
            Vector2 pt = WorldToMapScreen(worldPos, worldW, worldL, mapRect);
            if (pt.x < -60 || pt.x > Screen.width + 60 || pt.y < -30 || pt.y > Screen.height + 30) return;

            GUI.color = color;
            GUI.Label(new Rect(pt.x - 8f, pt.y - 8f, 16f, 16f), "●", titleStyle);
            DrawTextWithShadow(new Rect(pt.x + 8f, pt.y - 8f, 100f, 18f), label, subtitleStyle, color, Color.black);
            GUI.color = Color.white;
        }

        #endregion

        #region Divisas e Municípios/Províncias Vizinhos no Mapa Tático

        private void DrawNeighborDivisasOnMap(Rect mapRect, RegionData currentRegion)
        {
            if (!showDivisasLayer || neighborMapLabels == null || neighborMapLabels.Count == 0) return;

            foreach (var lbl in neighborMapLabels)
            {
                float sx = mapRect.x + lbl.normX * mapRect.width;
                float sy = mapRect.y + (1.0f - lbl.normY) * mapRect.height;

                if (sx < -200 || sx > Screen.width + 200 || sy < -100 || sy > Screen.height + 100) continue;

                float badgeW = Mathf.Clamp(lbl.displayName.Length * 9.5f + 32f, 110f, 260f);
                float badgeH = string.IsNullOrEmpty(lbl.subtitle) ? 24f : 36f;
                Rect badgeRect = new Rect(sx - badgeW * 0.5f, sy - badgeH * 0.5f, badgeW, badgeH);

                // Fundo do badge tático cinza-ardósia escuro com borda metálica
                GUI.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);
                GUI.DrawTexture(badgeRect, whiteTex);

                GUI.color = new Color(0.48f, 0.54f, 0.62f, 0.85f);
                GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 1), whiteTex);
                GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y + badgeRect.height - 1, badgeRect.width, 1), whiteTex);
                GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, 1, badgeRect.height), whiteTex);
                GUI.DrawTexture(new Rect(badgeRect.x + badgeRect.width - 1, badgeRect.y, 1, badgeRect.height), whiteTex);

                // Título em cinza claro brilhante / prata
                GUI.color = Color.white;
                string icon = lbl.isEdgeBadge ? "🧭 " : "🏛️ ";
                DrawTextWithShadow(new Rect(badgeRect.x + 6f, badgeRect.y + 3f, badgeRect.width - 12f, 18f),
                    $"{icon}{lbl.displayName}", titleStyle, new Color(0.92f, 0.94f, 0.98f), Color.black);

                if (!string.IsNullOrEmpty(lbl.subtitle))
                {
                    DrawTextWithShadow(new Rect(badgeRect.x + 6f, badgeRect.y + 19f, badgeRect.width - 12f, 14f),
                        lbl.subtitle, subtitleStyle, new Color(0.68f, 0.74f, 0.82f), Color.black);
                }
            }
        }

        #endregion
    }
}
