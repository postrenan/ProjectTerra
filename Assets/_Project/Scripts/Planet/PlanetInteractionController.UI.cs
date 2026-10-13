using UnityEngine;
using ProjectTerra.UI;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void InitStyles()
        {
            if (cardStyle != null) return;

            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.04f, 0.08f, 0.16f, 0.92f), new Color(0.25f, 0.60f, 0.90f, 0.75f), 1.5f, 8f, 32);
            cardStyle.border = new RectOffset(8, 8, 8, 8);

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
            UIThemeHelper.ApplyModernButtonSkin(buttonStyle);

            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            UIThemeHelper.ApplyPrimaryButtonSkin(primaryButtonStyle);
            primaryButtonStyle.normal.textColor = new Color(1f, 0.95f, 0.5f);

            dangerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            UIThemeHelper.ApplyDangerButtonSkin(dangerButtonStyle);
            dangerButtonStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            saveCardBoxStyle = new GUIStyle(GUI.skin.box);
            saveCardBoxStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.08f, 0.15f, 0.28f, 0.88f), new Color(0.25f, 0.60f, 0.90f, 0.75f), 1.5f, 8f, 32);
            saveCardBoxStyle.border = new RectOffset(8, 8, 8, 8);

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(8, 8, 4, 4)
            };
            textFieldStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.06f, 0.12f, 0.22f, 0.90f), new Color(0.25f, 0.55f, 0.85f, 0.70f), 1.5f, 6f, 32);

            recentBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyModernButtonSkin(recentBtnStyle);
            recentBtnStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
            recentBtnStyle.hover.textColor = Color.white;

            playBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyPrimaryButtonSkin(playBtnStyle);
            playBtnStyle.normal.textColor = new Color(1f, 0.92f, 0.45f);
            playBtnStyle.hover.textColor = Color.white;

            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0)
            };
            UIThemeHelper.ApplyDangerButtonSkin(closeButtonStyle);
            closeButtonStyle.normal.textColor = new Color(1f, 0.6f, 0.6f);
            closeButtonStyle.hover.textColor = Color.white;
        }

        private void OnGUI()
        {
            InitStyles();

            // Se o Menu Inicial estiver ativo na tela, não renderiza a UI secundária de exploração
            if (MainMenuController.Instance != null && MainMenuController.Instance.IsMenuOpen)
            {
                lastCardRect = Rect.zero;
                return;
            }

            // 1. Sempre renderiza o botão e modal retrátil de Saves Recentes (Modo Planeta)
            DrawRecentSavesUI();

            // 2. Botão de Play no canto inferior direito para pausar e retornar a rotação do planeta
            DrawPlanetRotationButtonUI();

            // 3. Balão de detalhes da região selecionada
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
            float height = currentCardMode == CardMode.RegionDetails ? 400f : (currentCardMode == CardMode.NewGamePrompt ? 610f : 390f);
            float x = Mathf.Clamp(screenPos.x + 25f, 20f, Screen.width - width - 20f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - height * 0.5f, 20f, Screen.height - height - 20f);

            Rect cardRect = new Rect(x, y, width, height);
            lastCardRect = cardRect;

            // Fundo translúcido espacial com cantos arredondados e borda suave
            UIThemeHelper.DrawRoundedBox(cardRect, new Color(0.02f, 0.05f, 0.12f, 0.94f), new Color(0.3f, 0.7f, 1.0f, 0.85f), 10f, 1.5f);

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

            // Botão de fechar fixo no canto superior direito do card (sempre visível)
            Rect closeRect = new Rect(x + width - 30f, y + 8f, 22f, 22f);
            if (GUI.Button(closeRect, "✖", closeButtonStyle))
            {
                CloseRegionDetails();
            }
        }

        private void DrawPlanetRotationButtonUI()
        {
            if (planet == null)
            {
                planet = CubeSpherePlanet.Instance ?? FindAnyObjectByType<CubeSpherePlanet>();
            }

            float btnWidth = 140f;
            float btnHeight = 38f;
            float btnX = Screen.width - btnWidth - 24f;
            float btnY = Screen.height - btnHeight - 24f;
            rotationButtonRect = new Rect(btnX, btnY, btnWidth, btnHeight);

            bool isPaused = planet != null && planet.IsRotationPaused;

            // 1. Botão estilizado com cantos arredondados procedurais
            Color prevCol = GUI.color;
            GUI.color = Color.white;

            // 3. Botão Rotacionar / Pausar (com ícone)
            string buttonLabel = isPaused ? "Rotacionar" : "Pausar";
            ProjectTerra.UI.TerraIcon buttonIcon = isPaused ? ProjectTerra.UI.TerraIcon.Compass : ProjectTerra.UI.TerraIcon.Pause;
            GUIStyle styleToUse = isPaused ? (playBtnStyle ?? primaryButtonStyle) : (recentBtnStyle ?? buttonStyle);

            if (IconButton(rotationButtonRect, buttonLabel, buttonIcon, styleToUse, "Pausar ou retornar a rotação do globo"))
            {
                TogglePlanetRotation();
            }
        }

        private static ProjectTerra.UI.TerraIcon GetCareerIcon(ProjectTerra.Gameplay.StarterCareer career)
        {
            switch (career)
            {
                case StarterCareer.Farmer: return ProjectTerra.UI.TerraIcon.Tractor;
                case StarterCareer.Trucker: return ProjectTerra.UI.TerraIcon.HeavyTruck;
                case StarterCareer.Aviator: return ProjectTerra.UI.TerraIcon.Airplane;
                default: return ProjectTerra.UI.TerraIcon.FishingBoat;
            }
        }

        private readonly System.Collections.Generic.Dictionary<GUIStyle, GUIStyle> iconPaddedStyles = new System.Collections.Generic.Dictionary<GUIStyle, GUIStyle>();

        private GUIStyle GetIconPaddedStyle(GUIStyle baseStyle)
        {
            if (!iconPaddedStyles.TryGetValue(baseStyle, out var padded))
            {
                padded = new GUIStyle(baseStyle);
                padded.padding = new RectOffset(baseStyle.padding.left + 20, baseStyle.padding.right, baseStyle.padding.top, baseStyle.padding.bottom);
                iconPaddedStyles[baseStyle] = padded;
            }
            return padded;
        }

        private void DrawIconInRect(Rect r, ProjectTerra.UI.TerraIcon icon, GUIStyle style)
        {
            float size = Mathf.Min(16f, r.height - 6f);
            Rect iconRect = new Rect(r.x + 8f, r.y + (r.height - size) * 0.5f, size, size);
            ProjectTerra.UI.TerraIcons.Draw(iconRect, icon, style.normal.textColor);
        }

        /// <summary>Botão IMGUI com ícone real (TerraIcons) à esquerda do texto.</summary>
        private bool IconButton(Rect rect, string text, ProjectTerra.UI.TerraIcon icon, GUIStyle style, string tooltip = "")
        {
            bool clicked = GUI.Button(rect, new GUIContent(text, tooltip), GetIconPaddedStyle(style));
            DrawIconInRect(rect, icon, style);
            return clicked;
        }

        private bool IconButtonLayout(string text, ProjectTerra.UI.TerraIcon icon, GUIStyle style, params GUILayoutOption[] options)
        {
            bool clicked = GUILayout.Button(text, GetIconPaddedStyle(style), options);
            if (Event.current.type == EventType.Repaint)
                DrawIconInRect(GUILayoutUtility.GetLastRect(), icon, style);
            return clicked;
        }
    }
}
