using System;
using UnityEngine;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        public enum SettingsTab { Video, Audio, Gameplay }
        private SettingsTab currentSettingsTab = SettingsTab.Video;

        // Opções de Vídeo
        private int qualityLevel = 2; // 0..4
        private int selectedResolutionIndex = 0;
        private int windowModeIndex = 0; // 0=Fullscreen, 1=Borderless, 2=Windowed
        private int vsyncIndex = 1; // 0=Off, 1=60Hz, 2=120Hz
        private int targetFPS = 120;
        private bool rotateGlobeSetting = true;

        // Opções de Áudio
        private float masterVolume = 1.0f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 1.0f;
        private float ambientVolume = 0.9f;

        // Opções de Jogabilidade
        private float mouseSensitivity = 1.0f;
        private bool invertYAxis = false;
        private float fieldOfView = 65.0f;
        private int languageIndex = 0; // 0=Português, 1=English

        private Vector2 settingsScrollPos = Vector2.zero;

        private void LoadSettings()
        {
            qualityLevel = PlayerPrefs.GetInt("Terra_QualityLevel", QualitySettings.GetQualityLevel());
            windowModeIndex = PlayerPrefs.GetInt("Terra_WindowMode", 0);
            vsyncIndex = PlayerPrefs.GetInt("Terra_VSync", 1);
            targetFPS = PlayerPrefs.GetInt("Terra_TargetFPS", 120);
            rotateGlobeSetting = PlayerPrefs.GetInt("Terra_RotateGlobe", 1) == 1;
            autoRotateGlobe = rotateGlobeSetting;

            masterVolume = PlayerPrefs.GetFloat("Terra_MasterVol", 1.0f);
            musicVolume = PlayerPrefs.GetFloat("Terra_MusicVol", 0.8f);
            sfxVolume = PlayerPrefs.GetFloat("Terra_SfxVol", 1.0f);
            ambientVolume = PlayerPrefs.GetFloat("Terra_AmbientVol", 0.9f);

            mouseSensitivity = PlayerPrefs.GetFloat("Terra_MouseSens", 1.0f);
            invertYAxis = PlayerPrefs.GetInt("Terra_InvertY", 0) == 1;
            fieldOfView = PlayerPrefs.GetFloat("Terra_FOV", 65.0f);
            languageIndex = PlayerPrefs.GetInt("Terra_Lang", 0);

            ApplySettings();
        }

        private void ApplySettings()
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1), true);
            QualitySettings.vSyncCount = vsyncIndex;
            Application.targetFrameRate = targetFPS > 0 ? targetFPS : -1;
            autoRotateGlobe = rotateGlobeSetting;

            AudioListener.volume = masterVolume;

            if (cameraRef != null)
            {
                cameraRef.fieldOfView = fieldOfView;
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("Terra_QualityLevel", qualityLevel);
            PlayerPrefs.SetInt("Terra_WindowMode", windowModeIndex);
            PlayerPrefs.SetInt("Terra_VSync", vsyncIndex);
            PlayerPrefs.SetInt("Terra_TargetFPS", targetFPS);
            PlayerPrefs.SetInt("Terra_RotateGlobe", rotateGlobeSetting ? 1 : 0);

            PlayerPrefs.SetFloat("Terra_MasterVol", masterVolume);
            PlayerPrefs.SetFloat("Terra_MusicVol", musicVolume);
            PlayerPrefs.SetFloat("Terra_SfxVol", sfxVolume);
            PlayerPrefs.SetFloat("Terra_AmbientVol", ambientVolume);

            PlayerPrefs.SetFloat("Terra_MouseSens", mouseSensitivity);
            PlayerPrefs.SetInt("Terra_InvertY", invertYAxis ? 1 : 0);
            PlayerPrefs.SetFloat("Terra_FOV", fieldOfView);
            PlayerPrefs.SetInt("Terra_Lang", languageIndex);

            PlayerPrefs.Save();
            ApplySettings();
            ShowNotification("Configurações salvas e aplicadas com sucesso!");
        }

        private void ResetDefaultSettings()
        {
            qualityLevel = 3;
            windowModeIndex = 0;
            vsyncIndex = 1;
            targetFPS = 120;
            rotateGlobeSetting = true;

            masterVolume = 1.0f;
            musicVolume = 0.8f;
            sfxVolume = 1.0f;
            ambientVolume = 0.9f;

            mouseSensitivity = 1.0f;
            invertYAxis = false;
            fieldOfView = 65.0f;
            languageIndex = 0;

            SaveSettings();
            ShowNotification("Configurações restauradas para os padrões de fábrica.");
        }

        private void DrawSettingsScreen()
        {
            DrawModalHeader("CONFIGURAÇÕES DO SISTEMA", "Ajuste os parâmetros gráficos, acústicos e ergonômicos de simulação");

            // Barra de abas
            GUILayout.BeginHorizontal();
            if (IconBtn(TerraIcon.Settings, "Vídeo & Gráficos", currentSettingsTab == SettingsTab.Video ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentSettingsTab = SettingsTab.Video;
            }
            if (IconBtn(TerraIcon.Volume, "Áudio & Som", currentSettingsTab == SettingsTab.Audio ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentSettingsTab = SettingsTab.Audio;
            }
            if (IconBtn(TerraIcon.Wrench, "Jogabilidade & Câmera", currentSettingsTab == SettingsTab.Gameplay ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentSettingsTab = SettingsTab.Gameplay;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            settingsScrollPos = GUILayout.BeginScrollView(settingsScrollPos);

            switch (currentSettingsTab)
            {
                case SettingsTab.Video:
                    DrawVideoSettings();
                    break;
                case SettingsTab.Audio:
                    DrawAudioSettings();
                    break;
                case SettingsTab.Gameplay:
                    DrawGameplaySettings();
                    break;
            }

            GUILayout.Space(16);

            // Botões de Ação
            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 0.85f, 0.45f);
            if (IconBtn(TerraIcon.Checkmark, "SALVAR E APLICAR ALTERAÇÕES", primaryActionBtnStyle, GUILayout.Height(36)))
            {
                SaveSettings();
            }
            GUI.color = Color.white;

            GUILayout.Space(10);
            if (GUILayout.Button("Restaurar Padrões", actionBtnStyle, GUILayout.Width(150), GUILayout.Height(36)))
            {
                ResetDefaultSettings();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        private void DrawVideoSettings()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("FIDELIDADE GRÁFICA & DESEMPENHO", labelBoldStyle);
            GUILayout.Space(6);

            // Nível de Qualidade
            GUILayout.Label("Predefinição de Qualidade Geral:", labelRegularStyle);
            GUILayout.BeginHorizontal();
            string[] qualities = { "Muito Baixa", "Baixa", "Média", "Alta", "Ultra" };
            for (int i = 0; i < qualities.Length; i++)
            {
                bool isCur = qualityLevel == i;
                GUI.color = isCur ? new Color(0.3f, 0.75f, 1f) : Color.white;
                if (GUILayout.Button(qualities[i], isCur ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(26)))
                {
                    qualityLevel = i;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Modo de Janela
            GUILayout.Label("Modo de Exibição de Janela:", labelRegularStyle);
            GUILayout.BeginHorizontal();
            string[] windowModes = { "Tela Cheia Exclusiva", "Janela Sem Bordas", "Em Janela" };
            for (int w = 0; w < windowModes.Length; w++)
            {
                bool isCur = windowModeIndex == w;
                GUI.color = isCur ? new Color(0.3f, 0.75f, 1f) : Color.white;
                if (GUILayout.Button(windowModes[w], isCur ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(26)))
                {
                    windowModeIndex = w;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // V-Sync
            GUILayout.Label("Sincronização Vertical (V-Sync):", labelRegularStyle);
            GUILayout.BeginHorizontal();
            string[] vsyncModes = { "Desligado", "Ligado (60 Hz)", "Adaptativo (120 Hz)" };
            for (int v = 0; v < vsyncModes.Length; v++)
            {
                bool isCur = vsyncIndex == v;
                GUI.color = isCur ? new Color(0.3f, 0.75f, 1f) : Color.white;
                if (GUILayout.Button(vsyncModes[v], isCur ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(26)))
                {
                    vsyncIndex = v;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Limite de FPS
            GUILayout.BeginHorizontal();
            GUILayout.Label("Taxa de Quadros Alvo (FPS):", labelRegularStyle);
            GUILayout.Label(targetFPS > 0 ? $"{targetFPS} FPS" : "Ilimitado", valueHighlightStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            int[] fpsPresets = { 30, 60, 120, 144, 240, -1 };
            string[] fpsLabels = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "240 FPS", "Ilimitado" };
            for (int f = 0; f < fpsPresets.Length; f++)
            {
                bool isCur = targetFPS == fpsPresets[f];
                GUI.color = isCur ? new Color(1f, 0.9f, 0.35f) : Color.white;
                if (GUILayout.Button(fpsLabels[f], actionBtnStyle, GUILayout.Height(22)))
                {
                    targetFPS = fpsPresets[f];
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Rotação Automática da Terra no Menu
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rotação Cinemática da Terra de Fundo:", labelRegularStyle);
            rotateGlobeSetting = GUILayout.Toggle(rotateGlobeSetting, rotateGlobeSetting ? " Ativado" : " Desativado");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawAudioSettings()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("PAINEL ACÚSTICO & MIXAGEM DE ÁUDIO", labelBoldStyle);
            GUILayout.Space(8);

            // Master
            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume Principal (Master):", labelRegularStyle);
            GUILayout.Label($"{(int)(masterVolume * 100)}%", valueHighlightStyle);
            GUILayout.EndHorizontal();
            masterVolume = GUILayout.HorizontalSlider(masterVolume, 0f, 1f);
            GUILayout.Space(8);

            // Música
            GUILayout.BeginHorizontal();
            GUILayout.Label("Trilha Sonora & Música:", labelRegularStyle);
            GUILayout.Label($"{(int)(musicVolume * 100)}%", valueHighlightStyle);
            GUILayout.EndHorizontal();
            musicVolume = GUILayout.HorizontalSlider(musicVolume, 0f, 1f);
            GUILayout.Space(8);

            // SFX
            GUILayout.BeginHorizontal();
            GUILayout.Label("Efeitos Sonoros (Motores, Máquinas, Cliques):", labelRegularStyle);
            GUILayout.Label($"{(int)(sfxVolume * 100)}%", valueHighlightStyle);
            GUILayout.EndHorizontal();
            sfxVolume = GUILayout.HorizontalSlider(sfxVolume, 0f, 1f);
            GUILayout.Space(8);

            // Ambiente
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ambiente & Natureza (Vento, Pássaros, Chuva):", labelRegularStyle);
            GUILayout.Label($"{(int)(ambientVolume * 100)}%", valueHighlightStyle);
            GUILayout.EndHorizontal();
            ambientVolume = GUILayout.HorizontalSlider(ambientVolume, 0f, 1f);

            GUILayout.EndVertical();
        }

        private void DrawGameplaySettings()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("JOGABILIDADE, CÂMERA & ERGONOMIA", labelBoldStyle);
            GUILayout.Space(8);

            // Sensibilidade
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sensibilidade de Órbita da Câmera:", labelRegularStyle);
            GUILayout.Label($"{mouseSensitivity:F2}x", valueHighlightStyle);
            GUILayout.EndHorizontal();
            mouseSensitivity = GUILayout.HorizontalSlider(mouseSensitivity, 0.2f, 3.0f);
            GUILayout.Space(8);

            // Inverter Y
            GUILayout.BeginHorizontal();
            GUILayout.Label("Inverter Eixo Vertical (Eixo Y):", labelRegularStyle);
            invertYAxis = GUILayout.Toggle(invertYAxis, invertYAxis ? " Invertido" : " Normal");
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            // FOV
            GUILayout.BeginHorizontal();
            GUILayout.Label("Campo de Visão (Field of View):", labelRegularStyle);
            GUILayout.Label($"{(int)fieldOfView}°", valueHighlightStyle);
            GUILayout.EndHorizontal();
            fieldOfView = GUILayout.HorizontalSlider(fieldOfView, 50f, 95f);
            GUILayout.Space(8);

            // Idioma
            GUILayout.Label("Idioma da Interface:", labelRegularStyle);
            GUILayout.BeginHorizontal();
            string[] langs = { "Português (Brasil)", "English (US)" };
            for (int l = 0; l < langs.Length; l++)
            {
                bool isCur = languageIndex == l;
                GUI.color = isCur ? new Color(0.3f, 0.75f, 1f) : Color.white;
                if (GUILayout.Button(langs[l], isCur ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(26)))
                {
                    languageIndex = l;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
