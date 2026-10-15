using System;
using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Gameplay;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        private string notificationMessage = "";
        private float notificationTimer = 0f;
        private bool isQuitConfirmOpen = false;

        private void OnGUI()
        {
            InitStyles();

            // 1. Caso o menu esteja fechado (modo exploração livre do globo)
            if (!isMenuOpen || currentScreen == MenuScreen.None)
            {
                DrawFloatingReturnButton();
                return;
            }

            if (screenAlpha <= 0.01f) return;

            // Salva a matriz de cores anterior
            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            // 2. Vinheta de fundo suave (escurece sutilmente as laterais sem tampar o globo no centro)
            if (vignetteTex != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f * screenAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTex, ScaleMode.StretchToFill);
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }

            // 3. Barra Lateral Esquerda - Menu Principal
            DrawMainNavigationSidebar();

            // 4. Painel Submodal Central/Direito (Novo Jogo, Multiplayer, Settings, ModHub, Saves)
            if (currentScreen != MenuScreen.Main)
            {
                DrawSubModalWindow();
            }

            // 5. Diálogo de Confirmação de Saída
            if (isQuitConfirmOpen)
            {
                DrawQuitConfirmDialog();
            }

            // 6. Notificações temporárias da UI
            if (!string.IsNullOrEmpty(notificationMessage))
            {
                DrawNotificationBadge();
            }

            GUI.color = prevColor;
        }

        private void DrawFloatingReturnButton()
        {
            float btnW = 180f;
            float btnH = 38f;
            float btnX = 24f;
            float btnY = 24f;
            floatingReturnBtnRect = new Rect(btnX, btnY, btnW, btnH);

            Color prev = GUI.color;
            GUI.color = Color.white;

            if (GUI.Button(floatingReturnBtnRect, "MENU PRINCIPAL", floatingBackBtnStyle))
            {
                OpenMainMenu();
            }
            GUI.color = prev;
        }

        private void DrawMainNavigationSidebar()
        {
            float sidebarWidth = 360f;
            float sidebarHeight = Screen.height - 48f;
            float sidebarX = 24f;
            float sidebarY = 24f;
            mainPanelRect = new Rect(sidebarX, sidebarY, sidebarWidth, sidebarHeight);

            // Painel lateral em vidro escuro translúcido com bordas arredondadas e glow ciano
            UIThemeHelper.DrawRoundedBox(mainPanelRect, new Color(0.02f, 0.05f, 0.12f, 0.92f * screenAlpha), new Color(0.20f, 0.55f, 0.85f, 0.70f * screenAlpha), 12f, 1.5f);

            // Área de conteúdo
            GUILayout.BeginArea(new Rect(sidebarX + 20f, sidebarY + 20f, sidebarWidth - 40f, sidebarHeight - 40f));

            // CABEÇALHO DO JOGO
            GUILayout.BeginVertical();
            GUILayout.Label("PROJECT TERRA", titleMainStyle);
            GUILayout.Label("SIMULAÇÃO PLANETÁRIA GLOBAL EM ESCALA REAL", titleSubStyle);
            GUILayout.Space(2);

            // Divisor com gradiente
            GUI.color = new Color(0.35f, 0.75f, 1.0f, 0.35f * screenAlpha);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.EndVertical();

            GUILayout.Space(14);

            // BOTÕES DE NAVEGAÇÃO PRINCIPAL
            DrawMenuNavButton("NOVO JOGO", "Selecione a região de início diretamente no globo", MenuScreen.NewGame, () =>
            {
                // Entra direto no modo de seleção no globo 3D
                ExploreGlobeFreely();
            });

            // CONTINUAR
            RegionSaveData latestSave = GetMostRecentSave();
            string continueSubtext = latestSave != null
                ? $"Continuar: {latestSave.saveName} ({latestSave.regionName})"
                : "Nenhuma partida salva existente";

            DrawContinueButton(latestSave, continueSubtext);

            // MULTIPLAYER
            DrawMenuNavButton("MULTIPLAYER", "Lobby online, co-op regional e servidores", MenuScreen.Multiplayer, () =>
            {
                OpenScreen(MenuScreen.Multiplayer);
            });

            // MODHUB
            DrawMenuNavButton("MODHUB", "Oficina de mods, veículos, alfaias e mapas", MenuScreen.ModHub, () =>
            {
                OpenScreen(MenuScreen.ModHub);
            });

            // CONFIGURAÇÕES
            DrawMenuNavButton("CONFIGURAÇÕES", "Vídeo, áudio, jogabilidade e controles", MenuScreen.Settings, () =>
            {
                OpenScreen(MenuScreen.Settings);
            });

            GUILayout.FlexibleSpace();

            // ÁREA INFERIOR DA BARRA LATERAL
            GUI.color = new Color(0.25f, 0.55f, 0.85f, 0.25f * screenAlpha);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(6);

            // Botão para explorar o globo livremente
            GUI.color = new Color(0.12f, 0.30f, 0.55f, 0.90f);
            if (IconBtn(TerraIcon.Compass, "EXPLORAR GLOBO 3D", actionBtnStyle, GUILayout.Height(34)))
            {
                ExploreGlobeFreely();
            }
            GUI.color = Color.white;

            GUILayout.Space(4);

            // Botão Sair do Jogo
            GUI.color = new Color(0.50f, 0.15f, 0.15f, 0.85f);
            if (GUILayout.Button("SAIR DO JOGO", dangerActionBtnStyle, GUILayout.Height(30)))
            {
                isQuitConfirmOpen = true;
            }
            GUI.color = Color.white;

            GUILayout.Space(8);

            // Informações de Versão & Status
            GUILayout.BeginHorizontal();
            GUILayout.Label("Serviços Conectados", versionBadgeStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("v1.0.0 Alpha", versionBadgeStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawMenuNavButton(string title, string subtitle, MenuScreen screenTarget, Action onClick)
        {
            bool isActive = currentScreen == screenTarget;
            GUIStyle btnStyle = isActive ? activeMenuBtnStyle : menuBtnStyle;

            GUILayout.BeginVertical();
            if (GUILayout.Button(title, btnStyle, GUILayout.Height(36)))
            {
                onClick?.Invoke();
            }

            GUILayout.Space(-4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14);
            GUILayout.Label(subtitle, menuBtnSubtextStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6);
        }

        private void DrawContinueButton(RegionSaveData latestSave, string subtext)
        {
            bool canContinue = latestSave != null;
            bool isActive = currentScreen == MenuScreen.SavesList;

            GUILayout.BeginVertical();

            if (!canContinue)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
                GUILayout.Button("CONTINUAR", menuBtnStyle, GUILayout.Height(36));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(0.2f, 0.85f, 0.45f, 1.0f);
                if (GUILayout.Button($"CONTINUAR ({latestSave.saveName})", isActive ? activeMenuBtnStyle : menuBtnStyle, GUILayout.Height(36)))
                {
                    ContinueGame(latestSave);
                }
                GUI.color = Color.white;
            }

            GUILayout.Space(-4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(14);
            GUILayout.Label(subtext, menuBtnSubtextStyle);
            if (canContinue)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Todos Saves »", labelRegularStyle, GUILayout.Width(80)))
                {
                    OpenScreen(MenuScreen.SavesList);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        private void DrawSubModalWindow()
        {
            float modalX = mainPanelRect.x + mainPanelRect.width + 20f;
            float modalY = 24f;
            float modalWidth = Screen.width - modalX - 24f;
            float modalHeight = Screen.height - 48f;

            // Limita a largura máxima do painel para manter o globo visível no centro
            modalWidth = Mathf.Clamp(modalWidth, 480f, 760f);

            subModalRect = new Rect(modalX, modalY, modalWidth, modalHeight);

            // Fundo escuro fosco de vidro com bordas arredondadas e acento ciano
            UIThemeHelper.DrawRoundedBox(subModalRect, new Color(0.02f, 0.05f, 0.12f, 0.94f * screenAlpha), new Color(0.30f, 0.65f, 0.95f, 0.75f * screenAlpha), 12f, 1.5f);

            // Conteúdo específico da tela atual
            GUILayout.BeginArea(new Rect(modalX + 24f, modalY + 20f, modalWidth - 48f, modalHeight - 40f));

            switch (currentScreen)
            {
                case MenuScreen.NewGame:
                    DrawNewGameScreen();
                    break;
                case MenuScreen.SavesList:
                    DrawSavesListScreen();
                    break;
                case MenuScreen.Multiplayer:
                    DrawMultiplayerScreen();
                    break;
                case MenuScreen.ModHub:
                    DrawModHubScreen();
                    break;
                case MenuScreen.Settings:
                    DrawSettingsScreen();
                    break;
            }

            GUILayout.EndArea();
        }

        private void DrawModalHeader(string title, string subtitle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label(title, modalHeaderTitleStyle);
            GUILayout.Label(subtitle, modalHeaderSubStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUI.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);
            if (IconBtn(TerraIcon.CancelCross, "Fechar", actionBtnStyle, GUILayout.Width(80), GUILayout.Height(28)))
            {
                CloseSubModal();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUI.color = new Color(0.35f, 0.75f, 1.0f, 0.25f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(10);
        }

        private void DrawQuitConfirmDialog()
        {
            float dlgW = 380f;
            float dlgH = 160f;
            float dlgX = (Screen.width - dlgW) * 0.5f;
            float dlgY = (Screen.height - dlgH) * 0.5f;
            Rect dlgRect = new Rect(dlgX, dlgY, dlgW, dlgH);

            // Fundo escuro modal com bordas arredondadas de alerta
            UIThemeHelper.DrawRoundedBox(dlgRect, new Color(0.03f, 0.05f, 0.10f, 0.98f), new Color(1f, 0.35f, 0.35f, 0.90f), 12f, 2f);

            GUILayout.BeginArea(new Rect(dlgX + 20f, dlgY + 20f, dlgW - 40f, dlgH - 40f));
            GUILayout.Label("Encerrar o Jogo?", modalHeaderTitleStyle);
            GUILayout.Label("Deseja realmente fechar o Project Terra e sair para o desktop?", labelRegularStyle);
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancelar", actionBtnStyle, GUILayout.Height(32)))
            {
                isQuitConfirmOpen = false;
            }
            GUILayout.Space(10);
            GUI.color = new Color(0.85f, 0.20f, 0.20f);
            if (GUILayout.Button("Sair do Jogo", dangerActionBtnStyle, GUILayout.Height(32)))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawNotificationBadge()
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f)
            {
                notificationMessage = "";
                return;
            }

            // Margem interna para o texto não encostar na borda do balão.
            const float padX = 20f;
            const float padY = 12f;

            var content = new GUIContent(notificationMessage);
            var msgStyle = new GUIStyle(labelBoldStyle)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };

            float maxBadgeW = Mathf.Min(560f, Screen.width - 40f);
            float innerMaxW = maxBadgeW - padX * 2f;
            // CalcSize ignora wordWrap (largura de uma linha); limita à largura máxima
            // e deixa o CalcHeight quebrar o texto em mais linhas quando necessário.
            float innerW = Mathf.Min(innerMaxW, msgStyle.CalcSize(content).x);
            float innerH = msgStyle.CalcHeight(content, innerW);

            float badgeW = innerW + padX * 2f;
            float badgeH = Mathf.Max(40f, innerH + padY * 2f);
            float badgeX = (Screen.width - badgeW) * 0.5f;
            float badgeY = 24f;
            Rect badgeRect = new Rect(badgeX, badgeY, badgeW, badgeH);

            UIThemeHelper.DrawRoundedBox(badgeRect, new Color(0.04f, 0.18f, 0.32f, 0.95f), new Color(0.40f, 0.90f, 1.0f, 0.90f), 8f, 1.5f);

            Rect textRect = new Rect(badgeRect.x + padX, badgeRect.y + padY, badgeRect.width - padX * 2f, badgeRect.height - padY * 2f);
            GUI.Label(textRect, content, msgStyle);
        }

        public void ShowNotification(string message, float duration = 3.5f)
        {
            notificationMessage = message;
            notificationTimer = duration;
        }

        private RegionSaveData GetMostRecentSave()
        {
            if (SaveManager.Instance == null) return null;
            var list = SaveManager.Instance.GetAllSaves();
            return (list != null && list.Count > 0) ? list[0] : null;
        }

        private void ContinueGame(RegionSaveData save)
        {
            if (save == null) return;
            if (PlanetInteractionController.Instance != null)
            {
                isMenuOpen = false;
                PlanetInteractionController.Instance.LoadSaveGame(save);
            }
        }
    }
}
