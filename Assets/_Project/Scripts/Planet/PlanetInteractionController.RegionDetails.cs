using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void DrawRegionDetailsUI()
        {
            var infra = RegionInfrastructureDatabase.GetInfrastructure(selectedRegion.id, selectedRegion);

            // Cabeçalho
            GUILayout.Label(selectedRegion.name, headerTitleStyle);
            GUILayout.Label($"{selectedRegion.country} • {selectedRegion.type}", headerSubtitleStyle);
            GUILayout.Space(4);

            // Divisória
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Barras de Recursos com percentuais
            DrawResourceBar("🌲 Cobertura Florestal", selectedRegion.forestPercent, new Color(0.2f, 0.85f, 0.3f));
            DrawResourceBar("⛏️ Potencial Mineral", selectedRegion.mineralsPercent, new Color(1.0f, 0.65f, 0.2f));
            DrawResourceBar("🌾 Terras Aráveis", selectedRegion.arablePercent, new Color(0.95f, 0.85f, 0.25f));
            DrawResourceBar("💧 Recursos Hídricos", selectedRegion.waterPercent, new Color(0.25f, 0.7f, 1.0f));

            GUILayout.Space(6);

            // Painel de Infraestrutura e Condições Geográficas
            GUI.color = new Color(0.12f, 0.22f, 0.38f, 0.85f);
            GUILayout.BeginVertical(saveCardBoxStyle);
            GUI.color = Color.white;

            GUI.color = new Color(0.45f, 0.85f, 1.0f);
            GUILayout.Label($"📍 Bioma: {infra.environmentCategory}", headerSubtitleStyle);
            GUI.color = new Color(0.85f, 0.92f, 1.0f);
            GUILayout.Label($"🌊 Hidrografia: {infra.waterBodyName}", headerSubtitleStyle);
            GUILayout.Label($"⚓ Portos: {infra.portName}", headerSubtitleStyle);
            GUILayout.Label($"🛫 Aeroportos: {infra.airportName}", headerSubtitleStyle);

            // Resumo de Aptidão
            string fishTag = infra.isFishingViable ? "<color=#4ade80>🎣 Pesca Viável</color>" : "<color=#f87171>🎣 Pesca Indisponível</color>";
            string agriTag = infra.isAgricultureViable ? "<color=#4ade80>🌾 Agricultura Viável</color>" : "<color=#f87171>🌾 Agricultura Inviável</color>";
            GUILayout.Label($"{agriTag}  •  {fishTag}", headerSubtitleStyle);

            GUILayout.EndVertical();
            GUI.color = Color.white;
            GUILayout.Space(6);

            // Botões de Ação de Jogo (Apenas para regiões terrestres)
            if (selectedRegion.id > 0)
            {
                List<RegionSaveData> saves = SaveManager.Instance != null ?
                    SaveManager.Instance.GetSavesForRegion(selectedRegion.id) : new List<RegionSaveData>();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🎮 Novo Jogo", primaryButtonStyle, GUILayout.Height(28)))
                {
                    currentCardMode = CardMode.NewGamePrompt;
                    newGameSaveName = $"Governo de {selectedRegion.name}";
                }

                string loadBtnLabel = saves.Count > 0 ? $"💾 Carregar ({saves.Count})" : "💾 Carregar (0)";
                if (GUILayout.Button(loadBtnLabel, buttonStyle, GUILayout.Height(28)))
                {
                    currentCardMode = CardMode.LoadGameList;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            // Notificação temporária se houver
            if (!string.IsNullOrEmpty(notificationMessage))
            {
                GUI.color = new Color(0.4f, 1f, 0.5f);
                GUILayout.Label(notificationMessage, labelStyle);
                GUI.color = Color.white;
                GUILayout.Space(2);
            }

            // Rodapé: Retomar rotação e Fechar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Retomar Rotação", buttonStyle, GUILayout.Height(24)))
            {
                ResumeRotation();
            }
            if (GUILayout.Button("✖ Fechar", buttonStyle, GUILayout.Width(70), GUILayout.Height(24)))
            {
                hasSelection = false;
                lastCardRect = Rect.zero;
                SetTerrainHighlight(0);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawResourceBar(string title, int percent, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle, GUILayout.Width(170));
            GUILayout.Label($"{percent}%", valueStyle);
            GUILayout.EndHorizontal();

            Rect barBg = GUILayoutUtility.GetRect(240, 6);
            GUI.color = new Color(0.15f, 0.2f, 0.3f, 0.6f);
            GUI.DrawTexture(barBg, whiteTex);

            float fillWidth = (barBg.width * Mathf.Clamp01(percent / 100f));
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barBg.x, barBg.y, fillWidth, barBg.height), whiteTex);
            GUI.color = Color.white;

            GUILayout.Space(2);
        }
    }
}
