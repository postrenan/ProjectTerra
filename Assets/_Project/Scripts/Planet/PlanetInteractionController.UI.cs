using UnityEngine;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void InitStyles()
        {
            if (cardStyle != null) return;

            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.background = whiteTex;

            headerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerTitleStyle.normal.textColor = new Color(0.95f, 0.95f, 1f);

            headerSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            headerSubtitleStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            valueStyle.normal.textColor = new Color(1f, 0.9f, 0.4f);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            primaryButtonStyle.normal.textColor = new Color(1f, 0.95f, 0.5f);

            dangerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            dangerButtonStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            saveCardBoxStyle = new GUIStyle(GUI.skin.box);
            saveCardBoxStyle.normal.background = whiteTex;

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            recentBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            recentBtnStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
            recentBtnStyle.hover.textColor = Color.white;
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Sempre renderiza o botão e modal retrátil de Saves Recentes (Modo Planeta)
            DrawRecentSavesUI();

            // 2. Balão de detalhes da região selecionada
            if (!hasSelection || selectedRegion == null || mainCamera == null)
            {
                lastCardRect = Rect.zero;
                return;
            }

            // Oculta balão se o ponto estiver do lado oposto do globo
            Vector3 camToPoint = selectedWorldPoint - mainCamera.transform.position;
            Vector3 pointNormal = selectedWorldPoint.normalized;
            if (Vector3.Dot(camToPoint.normalized, pointNormal) > 0.1f)
            {
                lastCardRect = Rect.zero;
                return;
            }

            Vector3 screenPos = mainCamera.WorldToScreenPoint(selectedWorldPoint);
            if (screenPos.z < 0)
            {
                lastCardRect = Rect.zero;
                return;
            }

            // Dimensões dinâmicas do card conforme a aba
            float width = currentCardMode == CardMode.RegionDetails ? 360f : (currentCardMode == CardMode.NewGamePrompt ? 460f : 400f);
            float height = currentCardMode == CardMode.RegionDetails ? 430f : (currentCardMode == CardMode.NewGamePrompt ? 610f : 390f);
            float x = Mathf.Clamp(screenPos.x + 25f, 20f, Screen.width - width - 20f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - height * 0.5f, 20f, Screen.height - height - 20f);

            Rect cardRect = new Rect(x, y, width, height);
            lastCardRect = cardRect;

            // 1. Fundo translúcido espacial
            GUI.color = new Color(0.02f, 0.05f, 0.12f, 0.94f);
            GUI.DrawTexture(cardRect, whiteTex);

            // 2. Borda externa suave
            GUI.color = new Color(0.3f, 0.7f, 1.0f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, width, 2), whiteTex);
            GUI.DrawTexture(new Rect(x, y + height - 2, width, 2), whiteTex);
            GUI.DrawTexture(new Rect(x, y, 2, height), whiteTex);
            GUI.DrawTexture(new Rect(x + width - 2, y, 2, height), whiteTex);

            // 3. Pino indicador na posição do clique
            GUI.color = new Color(1f, 0.85f, 0.3f, 0.95f);
            GUI.DrawTexture(new Rect(screenPos.x - 4, Screen.height - screenPos.y - 4, 8, 8), whiteTex);

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(x + 14, y + 10, width - 28, height - 20));

            // Renderiza o conteúdo do modo ativo
            switch (currentCardMode)
            {
                case CardMode.RegionDetails:
                    DrawRegionDetailsUI();
                    break;
                case CardMode.NewGamePrompt:
                    DrawNewGameUI();
                    break;
                case CardMode.LoadGameList:
                    DrawLoadGameUI();
                    break;
            }

            GUILayout.EndArea();
        }
    }
}
