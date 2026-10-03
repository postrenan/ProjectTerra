using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Planet.TerrainStreaming;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Gestão do Mapa Regional

        public void ToggleRegionalMap()
        {
            IsRegionalMapOpen = !IsRegionalMapOpen;
            if (IsRegionalMapOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
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

        #endregion

        #region Textura do Mapa Regional Tático

        private void GenerateRegionalMapTexture()
        {
            int res = 256;
            regionalMapTex = new Texture2D(res, res, TextureFormat.RGB24, false);
            Color[] cols = new Color[res * res];

            var rsm = RegionalSandboxManager.Instance;
            TerrainData tData = rsm != null ? rsm.activeTerrainData : null;
            RegionalHydroData hydro = rsm != null ? rsm.activeHydroData : null;
            float seaLvl = hydro != null ? hydro.seaLevelNormalized : 0.05f;

            for (int y = 0; y < res; y++)
            {
                float ny = y / (float)(res - 1);
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);

                    float h = 0.5f;
                    float steep = 0f;
                    if (tData != null)
                    {
                        h = tData.GetInterpolatedHeight(nx, ny) / Mathf.Max(1f, tData.size.y);
                        steep = tData.GetSteepness(nx, ny);
                    }

                    // Sombreador de relevo (Hillshading topográfico)
                    float shade = 1.0f - Mathf.Clamp01(steep / 60f) * 0.4f;

                    Color pxCol;
                    if (h <= seaLvl + 0.015f)
                    {
                        // Água / Lago / Oceano
                        pxCol = new Color(0.12f, 0.35f, 0.65f) * shade;
                    }
                    else if (steep > 26f)
                    {
                        // Rocha
                        pxCol = new Color(0.48f, 0.46f, 0.44f) * shade;
                    }
                    else if (h <= seaLvl + 0.04f)
                    {
                        // Areia / Praia
                        pxCol = new Color(0.75f, 0.70f, 0.52f) * shade;
                    }
                    else
                    {
                        // Gradiente de vegetação natural
                        float vegNoise = Mathf.PerlinNoise(nx * 6f, ny * 6f);
                        Color lush = new Color(0.24f, 0.45f, 0.22f);
                        Color dry = new Color(0.55f, 0.52f, 0.28f);
                        pxCol = Color.Lerp(lush, dry, vegNoise) * shade;
                    }

                    cols[y * res + x] = pxCol;
                }
            }

            regionalMapTex.SetPixels(cols);
            regionalMapTex.wrapMode = TextureWrapMode.Clamp;
            regionalMapTex.filterMode = FilterMode.Bilinear;
            regionalMapTex.Apply();
        }

        #endregion

        #region Renderização e Interação do Mapa Tático GIS [M]

        private void DrawInteractiveRegionalMap()
        {
            var rsm = RegionalSandboxManager.Instance;
            float mapSize = Mathf.Min(Screen.width * 0.82f, Screen.height * 0.82f);
            float mapX = (Screen.width - mapSize) * 0.5f;
            float mapY = (Screen.height - mapSize) * 0.5f;
            Rect frameRect = new Rect(mapX, mapY, mapSize, mapSize);
            lastMapRect = frameRect;

            // Fundo escuro modal de radar militar GIS
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);

            GUI.color = new Color(0.04f, 0.07f, 0.12f, 0.96f);
            GUI.DrawTexture(frameRect, whiteTex);
            GUI.color = new Color(0.2f, 0.7f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(frameRect.x, frameRect.y, frameRect.width, 2), whiteTex);
            GUI.DrawTexture(new Rect(frameRect.x, frameRect.y + frameRect.height - 2, frameRect.width, 2), whiteTex);
            GUI.color = Color.white;

            // Barra de Título do Mapa
            string regName = activeSave != null ? activeSave.regionName : "Província";
            string country = activeSave != null ? activeSave.countryName : "Mundo";
            GUI.Label(new Rect(mapX + 15f, mapY + 8f, 400f, 24f), $"🗺️ MAPA TÁTICO REGIONAL: {regName}, {country}", titleStyle);

            if (GUI.Button(new Rect(mapX + mapSize - 35f, mapY + 6f, 26f, 24f), "✖", menuButtonDangerStyle))
            {
                CloseRegionalMap();
                return;
            }

            // Área de Visualização do Mapa
            Rect mapCanvasRect = new Rect(mapX + 15f, mapY + 36f, mapSize - 30f, mapSize - 75f);
            GUI.color = new Color(0.08f, 0.12f, 0.18f);
            GUI.DrawTexture(mapCanvasRect, whiteTex);
            GUI.color = Color.white;

            // Interação de Pan & Zoom
            HandleMapInteraction(mapCanvasRect);

            // Renderizar Mapa Texturizado dentro do Canvas com Clip
            GUI.BeginGroup(mapCanvasRect);
            Vector2 mapCenter = new Vector2(mapCanvasRect.width * 0.5f, mapCanvasRect.height * 0.5f) + mapPanOffset;
            float drawW = mapCanvasRect.width * mapZoom;
            float drawH = mapCanvasRect.height * mapZoom;
            Rect mapImageRect = new Rect(mapCenter.x - drawW * 0.5f, mapCenter.y - drawH * 0.5f, drawW, drawH);

            if (regionalMapTex != null)
            {
                GUI.DrawTexture(mapImageRect, regionalMapTex);
            }

            // Linhas de Grade Tática (Grid)
            GUI.color = new Color(0.2f, 0.5f, 0.8f, 0.18f);
            for (float gx = 0; gx <= mapCanvasRect.width; gx += 50f * mapZoom)
            {
                GUI.DrawTexture(new Rect(gx + (mapPanOffset.x % (50f * mapZoom)), 0, 1, mapCanvasRect.height), whiteTex);
            }
            for (float gy = 0; gy <= mapCanvasRect.height; gy += 50f * mapZoom)
            {
                GUI.DrawTexture(new Rect(0, gy + (mapPanOffset.y % (50f * mapZoom)), mapCanvasRect.width, 1), whiteTex);
            }
            GUI.color = Color.white;

            // Marcadores no Mapa:
            float worldW = rsm != null ? rsm.worldWidthMeters : 60000f;
            float worldL = rsm != null ? rsm.worldLengthMeters : 60000f;

            // 1. Centro da Cidade / Base
            Vector3 townPos = rsm != null ? rsm.townCenterPosition : Vector3.zero;
            DrawMapMarker(townPos, worldW, worldL, mapImageRect, "🏛️ Cidade", new Color(0.4f, 0.8f, 1f));

            // 2. Destino de Missão
            if (MissionManager.Instance != null && MissionManager.Instance.hasActiveContract && !MissionManager.Instance.currentContract.isCompleted)
            {
                DrawMapMarker(MissionManager.Instance.deliveryTargetPosition, worldW, worldL, mapImageRect, "🎯 Destino", new Color(1f, 0.85f, 0.2f));
            }

            // 3. Marcador Customizado (Waypoint)
            if (CustomWaypoint.HasValue)
            {
                DrawMapMarker(CustomWaypoint.Value, worldW, worldL, mapImageRect, "🚩 Waypoint", new Color(0.95f, 0.25f, 1.0f));
            }

            // 4. Veículo Inicial
            if (rsm != null && rsm.starterVehicle != null)
            {
                DrawMapMarker(rsm.starterVehicle.transform.position, worldW, worldL, mapImageRect, "🚜 Veículo", new Color(1f, 0.6f, 0.2f));
            }

            // 5. Jogador Atual com Seta de Rotação
            var player = PlayerCharacterController.Instance;
            if (player != null)
            {
                Vector2 pScreen = WorldToMapScreen(player.transform.position, worldW, worldL, mapImageRect);
                GUI.color = new Color(0.2f, 1f, 0.5f);
                GUI.Label(new Rect(pScreen.x - 12f, pScreen.y - 12f, 24f, 24f), "▲", titleStyle);
                GUI.color = Color.white;
                GUI.Label(new Rect(pScreen.x + 8f, pScreen.y - 8f, 80f, 20f), "Você", subtitleStyle);
            }

            GUI.EndGroup();

            // Contorno (bounding) da região + vizinhos de fronteira
            DrawRegionBorders(mapCanvasRect);

            // Rodapé do Mapa com Controles e Legenda
            GUILayout.BeginArea(new Rect(mapX + 15f, mapY + mapSize - 35f, mapSize - 30f, 30f));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("🎯 Centralizar no Jogador", menuButtonStyle, GUILayout.Width(170), GUILayout.Height(24)))
            {
                mapPanOffset = Vector2.zero;
                mapZoom = 1.0f;
            }

            if (CustomWaypoint.HasValue)
            {
                if (GUILayout.Button("❌ Limpar Waypoint", menuButtonDangerStyle, GUILayout.Width(140), GUILayout.Height(24)))
                {
                    ClearCustomWaypoint();
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("💡 Clique no mapa para definir destino  •  Scroll para Zoom  •  Arraste para Mover", subtitleStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private System.Collections.Generic.List<string> borderLabels;
        private bool bordersComputed;

        private void DrawRegionBorders(Rect canvasRect)
        {
            if (!bordersComputed)
            {
                bordersComputed = true;
                var rsm = RegionalSandboxManager.Instance;
                var me = rsm != null ? rsm.activeRegionData : null;
                borderLabels = RegionNeighbors.GetBorderLabels(me);
            }

            // Moldura (bounding) da regiao no mapa
            GUI.color = new Color(0.25f, 0.8f, 1f, 0.9f);
            float t = 2f;
            GUI.DrawTexture(new Rect(canvasRect.x, canvasRect.y, canvasRect.width, t), whiteTex);
            GUI.DrawTexture(new Rect(canvasRect.x, canvasRect.yMax - t, canvasRect.width, t), whiteTex);
            GUI.DrawTexture(new Rect(canvasRect.x, canvasRect.y, t, canvasRect.height), whiteTex);
            GUI.DrawTexture(new Rect(canvasRect.xMax - t, canvasRect.y, t, canvasRect.height), whiteTex);
            GUI.color = Color.white;

            string divisas = (borderLabels != null && borderLabels.Count > 0)
                ? string.Join("  \u2022  ", borderLabels)
                : "Sem divisas terrestres (litoral/oceano)";
            var panel = new Rect(canvasRect.x + 6f, canvasRect.y + 6f, canvasRect.width - 12f, 22f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(panel, whiteTex);
            GUI.color = new Color(0.85f, 0.95f, 1f);
            GUI.Label(new Rect(panel.x + 6f, panel.y, panel.width - 10f, panel.height), "\U0001F9ED Divisas: " + divisas, subtitleStyle);
            GUI.color = Color.white;
        }

        private void HandleMapInteraction(Rect canvasRect)
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;

            if (canvasRect.Contains(mouse))
            {
                // Zoom com scroll do mouse
                if (e.type == EventType.ScrollWheel)
                {
                    float oldZoom = mapZoom;
                    mapZoom = Mathf.Clamp(mapZoom - e.delta.y * 0.15f, 0.8f, 4.0f);
                    e.Use();
                }

                // Iniciar arraste ou clique de waypoint
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    isDraggingMap = true;
                    dragStartMouse = mouse;
                    dragStartPan = mapPanOffset;
                    e.Use();
                }
            }

            if (isDraggingMap)
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    mapPanOffset = dragStartPan + (mouse - dragStartMouse);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    isDraggingMap = false;
                    // Se foi apenas um clique rápido sem arrastar, posiciona o waypoint!
                    if (Vector2.Distance(mouse, dragStartMouse) < 5f && canvasRect.Contains(mouse))
                    {
                        var rsm = RegionalSandboxManager.Instance;
                        float worldW = rsm != null ? rsm.worldWidthMeters : 60000f;
                        float worldL = rsm != null ? rsm.worldLengthMeters : 60000f;

                        Vector2 mapCenter = new Vector2(canvasRect.width * 0.5f, canvasRect.height * 0.5f) + mapPanOffset;
                        float drawW = canvasRect.width * mapZoom;
                        float drawH = canvasRect.height * mapZoom;
                        Rect mapImageRect = new Rect(mapCenter.x - drawW * 0.5f, mapCenter.y - drawH * 0.5f, drawW, drawH);

                        Vector2 localMouse = mouse - new Vector2(canvasRect.x, canvasRect.y);
                        float u = (localMouse.x - mapImageRect.x) / mapImageRect.width;
                        float v = 1.0f - ((localMouse.y - mapImageRect.y) / mapImageRect.height);

                        if (u >= 0f && u <= 1f && v >= 0f && v <= 1f)
                        {
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

        private Vector2 WorldToMapScreen(Vector3 worldPos, float worldW, float worldL, Rect mapRect)
        {
            float u = Mathf.Clamp01((worldPos.x + worldW * 0.5f) / worldW);
            float v = Mathf.Clamp01((worldPos.z + worldL * 0.5f) / worldL);
            float px = mapRect.x + u * mapRect.width;
            float py = mapRect.y + (1.0f - v) * mapRect.height;
            return new Vector2(px, py);
        }

        private void DrawMapMarker(Vector3 worldPos, float worldW, float worldL, Rect mapRect, string label, Color color)
        {
            Vector2 pt = WorldToMapScreen(worldPos, worldW, worldL, mapRect);
            GUI.color = color;
            GUI.Label(new Rect(pt.x - 8f, pt.y - 8f, 16f, 16f), "●", titleStyle);
            GUI.Label(new Rect(pt.x + 8f, pt.y - 8f, 90f, 18f), label, subtitleStyle);
            GUI.color = Color.white;
        }

        #endregion
    }
}
