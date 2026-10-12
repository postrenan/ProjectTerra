using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void DrawRegionDetailsUI()
        {
            var infra = RegionInfrastructureDatabase.GetInfrastructure(selectedRegion.id, selectedRegion);

            // Cabeçalho
            GUILayout.Label(selectedRegion.name, headerTitleStyle);
            GUILayout.Label($"{selectedRegion.country} - {selectedRegion.type}", headerSubtitleStyle);
            GUILayout.Space(4);

            // Divisória
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Barras de Recursos com percentuais
            DrawResourceBar("Cobertura Florestal", selectedRegion.forestPercent, new Color(0.2f, 0.85f, 0.3f), TerraIcon.Forest);
            DrawResourceBar("Potencial Mineral", selectedRegion.mineralsPercent, new Color(1.0f, 0.65f, 0.2f), TerraIcon.Mining);
            DrawResourceBar("Terras Aráveis", selectedRegion.arablePercent, new Color(0.95f, 0.85f, 0.25f), TerraIcon.Crops);
            DrawResourceBar("Recursos Hídricos", selectedRegion.waterPercent, new Color(0.25f, 0.7f, 1.0f), TerraIcon.WaterDrop);

            GUILayout.Space(6);

            // Painel de Infraestrutura e Condições Geográficas
            GUI.color = new Color(0.12f, 0.22f, 0.38f, 0.85f);
            GUILayout.BeginVertical(saveCardBoxStyle);
            GUI.color = Color.white;

            GUI.color = new Color(0.45f, 0.85f, 1.0f);
            DrawIconLabel(TerraIcon.LocationPin, $"Bioma: {infra.environmentCategory}", headerSubtitleStyle);
            GUI.color = new Color(0.85f, 0.92f, 1.0f);
            DrawIconLabel(TerraIcon.WaterDrop, $"Hidrografia: {infra.waterBodyName}", headerSubtitleStyle);
            DrawIconLabel(TerraIcon.Harbor, $"Portos: {infra.portName}", headerSubtitleStyle);
            DrawIconLabel(TerraIcon.Airport, $"Aeroportos: {infra.airportName}", headerSubtitleStyle);

            // Resumo de Aptidão (agricultura e pesca)
            GUILayout.BeginHorizontal();
            Color agriColor = infra.isAgricultureViable ? new Color(0.29f, 0.87f, 0.5f) : new Color(0.97f, 0.44f, 0.44f);
            Color fishColor = infra.isFishingViable ? new Color(0.29f, 0.87f, 0.5f) : new Color(0.97f, 0.44f, 0.44f);
            string agriTag = infra.isAgricultureViable ? "Agricultura Viável" : "Agricultura Inviável";
            string fishTag = infra.isFishingViable ? "Pesca Viável" : "Pesca Indisponível";

            Rect agriIconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            if (Event.current.type == EventType.Repaint)
                ProjectTerra.UI.TerraIcons.Draw(agriIconRect, TerraIcon.Crops, agriColor);
            GUI.color = agriColor;
            GUILayout.Label(agriTag, headerSubtitleStyle);
            GUILayout.Space(6);
            Rect fishIconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            if (Event.current.type == EventType.Repaint)
                ProjectTerra.UI.TerraIcons.Draw(fishIconRect, TerraIcon.Fish, fishColor);
            GUI.color = fishColor;
            GUILayout.Label(fishTag, headerSubtitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUI.color = Color.white;
            GUILayout.Space(6);

            // Botões de Ação de Jogo (Apenas para regiões terrestres)
            if (selectedRegion.id > 0)
            {
                List<RegionSaveData> saves = SaveManager.Instance != null ?
                    SaveManager.Instance.GetSavesForRegion(selectedRegion.id) : new List<RegionSaveData>();

                GUILayout.BeginHorizontal();
                if (IconButtonLayout("Novo Jogo", TerraIcon.Flag, primaryButtonStyle, GUILayout.Height(28)))
                {
                    currentCardMode = CardMode.NewGamePrompt;
                    newGameSaveName = $"Governo de {selectedRegion.name}";
                }

                string loadBtnLabel = saves.Count > 0 ? $"Carregar ({saves.Count})" : "Carregar (0)";
                if (IconButtonLayout(loadBtnLabel, TerraIcon.Historic, buttonStyle, GUILayout.Height(28)))
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

            // Rodapé: Fechar
            if (IconButtonLayout("Fechar", TerraIcon.CancelCross, buttonStyle, GUILayout.Height(26)))
            {
                CloseRegionDetails();
            }
        }

        /// <summary>Desenha um ícone TerraIcons seguido de um Label em linha.</summary>
        private void DrawIconLabel(TerraIcon icon, string text, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            Rect iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            if (Event.current.type == EventType.Repaint)
                ProjectTerra.UI.TerraIcons.Draw(iconRect, icon, style.normal.textColor);
            GUILayout.Label(text, style);
            GUILayout.EndHorizontal();
        }

        private void DrawResourceBar(string title, int percent, Color barColor, TerraIcon icon)
        {
            GUILayout.BeginHorizontal();
            Rect iconRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16), GUILayout.Height(16));
            if (Event.current.type == EventType.Repaint)
                ProjectTerra.UI.TerraIcons.Draw(iconRect, icon, barColor);
            GUILayout.Label(title, labelStyle, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
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
