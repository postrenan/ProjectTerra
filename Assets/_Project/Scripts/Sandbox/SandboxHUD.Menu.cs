using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Gestão do Menu de Opções / Pausa

        public void ToggleOptionsMenu()
        {
            IsOptionsMenuOpen = !IsOptionsMenuOpen;
            if (IsOptionsMenuOpen)
            {
                if (IsRegionalMapOpen) IsRegionalMapOpen = false;
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f; // Pausa a simulação
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                isSettingsSubmenuOpen = false;
            }
            else
            {
                CloseOptionsMenu();
            }
        }

        public void CloseOptionsMenu()
        {
            IsOptionsMenuOpen = false;
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            RestoreGameplayCursor();
        }

        public void SaveCurrentGame()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                SaveManager.Instance.SaveToFile(SaveManager.Instance.ActiveSave);
                saveToastMessage = "✅ Partida salva com sucesso!";
                saveToastTimer = 3.5f;
            }
        }

        public void ReturnToGlobe()
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                SaveManager.Instance.SaveToFile(SaveManager.Instance.ActiveSave);
            }
            SceneManager.LoadScene("MainEarthScene");
        }

        #endregion

        #region Renderização Menu de Opções [Alt Esquerdo]

        private void DrawLeftAltOptionsMenu()
        {
            // Overlay de fundo escuro
            GUI.color = new Color(0f, 0f, 0f, 0.70f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);

            float w = 420f;
            float h = isSettingsSubmenuOpen ? 420f : 360f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);
            GUI.DrawTexture(modalRect, whiteTex);
            GUI.color = new Color(0.25f, 0.7f, 1f, 0.9f);
            GUI.DrawTexture(new Rect(modalRect.x, modalRect.y, w, 2), whiteTex);
            GUI.DrawTexture(new Rect(modalRect.x, modalRect.y + h - 2, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(modalRect.x + 25f, modalRect.y + 20f, w - 50f, h - 35f));

            if (!isSettingsSubmenuOpen)
            {
                // Cabeçalho Principal
                GUILayout.Label("⚙️ MENU DE OPÇÕES", modalTitleStyle);
                string reg = activeSave != null ? activeSave.regionName : "Região";
                string country = activeSave != null ? activeSave.countryName : "Mundo";
                GUILayout.Label($"Simulação Pausada  •  {reg}, {country}", subtitleStyle);
                GUILayout.Space(15);

                // Botão 1: Continuar
                if (GUILayout.Button("▶ Continuar Partida", menuButtonPrimaryStyle, GUILayout.Height(36)))
                {
                    CloseOptionsMenu();
                }
                GUILayout.Space(8);

                // Botão 2: Salvar Partida
                if (GUILayout.Button("💾 Salvar Jogo", menuButtonStyle, GUILayout.Height(34)))
                {
                    SaveCurrentGame();
                }
                GUILayout.Space(8);

                // Botão 3: Retornar ao Globo Terrestre
                if (GUILayout.Button("🌐 Retornar ao Globo Terrestre", menuButtonStyle, GUILayout.Height(34)))
                {
                    ReturnToGlobe();
                }
                GUILayout.Space(8);

                // Botão 4: Salvar e Voltar ao Menu Principal
                if (GUILayout.Button("🏠 Salvar e Sair para o Menu", menuButtonDangerStyle, GUILayout.Height(34)))
                {
                    ReturnToGlobe();
                }
                GUILayout.Space(8);

                // Botão 5: Configurações
                if (GUILayout.Button("🛠️ Configurações (Áudio / HUD / Gráficos)", menuButtonStyle, GUILayout.Height(34)))
                {
                    isSettingsSubmenuOpen = true;
                }
            }
            else
            {
                // Submenu de Configurações
                GUILayout.Label("🛠️ CONFIGURAÇÕES DO JOGO", modalTitleStyle);
                GUILayout.Space(10);

                // 1. Volume Master de Áudio
                GUILayout.Label($"🔊 Volume Geral: {Mathf.RoundToInt(AudioListener.volume * 100f)}%", subtitleStyle);
                AudioListener.volume = GUILayout.HorizontalSlider(AudioListener.volume, 0f, 1f);
                GUILayout.Space(10);

                // 2. Toggles de Elementos da HUD
                GUILayout.Label("Exibição da Interface (HUD):", subtitleStyle);
                GUILayout.BeginHorizontal();
                ShowCompass = GUILayout.Toggle(ShowCompass, "🧭 Bússola", GUILayout.Width(110));
                ShowMissionTracker = GUILayout.Toggle(ShowMissionTracker, "🎯 Missões", GUILayout.Width(110));
                ShowTelemetry = GUILayout.Toggle(ShowTelemetry, "🏎️ Telemetria", GUILayout.Width(110));
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                // 3. Qualidade Gráfica
                GUILayout.Label($"Qualidade Gráfica Atual: {QualitySettings.names[QualitySettings.GetQualityLevel()]}", subtitleStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Médio", menuButtonStyle, GUILayout.Height(24))) QualitySettings.SetQualityLevel(1, true);
                if (GUILayout.Button("Alto", menuButtonStyle, GUILayout.Height(24))) QualitySettings.SetQualityLevel(2, true);
                if (GUILayout.Button("Ultra", menuButtonStyle, GUILayout.Height(24))) QualitySettings.SetQualityLevel(3, true);
                GUILayout.EndHorizontal();
                GUILayout.Space(16);

                if (GUILayout.Button("◀ Voltar ao Menu de Pausa", menuButtonPrimaryStyle, GUILayout.Height(34)))
                {
                    isSettingsSubmenuOpen = false;
                }
            }

            GUILayout.EndArea();
        }

        #endregion
    }
}
