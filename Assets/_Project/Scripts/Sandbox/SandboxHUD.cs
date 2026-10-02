using System;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// HUD e Gerenciador de Interface da Cena Sandbox Regional:
    /// 1. Layout UI/UX limpo e moderno (Status Pill no topo-esquerdo, Bússola 360° no topo-central, Rastreador no topo-direito e Telemetria no canto inferior).
    /// 2. Atalho [M]: Abre o Mapa Regional Tático GIS interativo com Pan, Zoom, marcadores e criação de waypoints customizados ao clicar.
    /// 3. Atalho [Alt Esquerdo]: Abre o Menu de Opções / Pausa com Salvar, Retornar ao Globo Terrestre, Salvar e Voltar ao Menu, e Ajustes de Som/Gráficos.
    /// 4. Atalho [H]: Alterna visibilidade total do HUD para imersão e capturas de tela.
    /// </summary>
    public partial class SandboxHUD : MonoBehaviour
    {
        public static SandboxHUD Instance { get; private set; }

        private RegionSaveData activeSave;
        private Texture2D whiteTex;
        private Texture2D regionalMapTex;

        // Estados de Telas
        public bool IsRegionalMapOpen { get; private set; } = false;
        public bool IsOptionsMenuOpen { get; private set; } = false;
        public bool IsHudVisible { get; private set; } = true;
        public bool ShowCompass { get; private set; } = true;
        public bool ShowMissionTracker { get; private set; } = true;
        public bool ShowTelemetry { get; private set; } = true;

        // Waypoint Customizado
        public Vector3? CustomWaypoint { get; private set; }
        private GameObject waypointWorldMarker;

        // Controle do Mapa Regional (Pan & Zoom)
        private float mapZoom = 1.0f;
        private Vector2 mapPanOffset = Vector2.zero;
        private bool isDraggingMap = false;
        private Vector2 dragStartMouse;
        private Vector2 dragStartPan;
        private Rect lastMapRect;

        // Submenu de Configurações no Menu Alt
        private bool isSettingsSubmenuOpen = false;
        private string saveToastMessage = "";
        private float saveToastTimer = 0f;

        // Time Warp
        private readonly int[] timeWarpPresets = { 1, 2, 4, 8, 16 };
        private int currentTimeWarpIdx = 0;
        private float previousTimeScale = 1.0f;

        // GUIStyles
        private GUIStyle pillStyle;
        private GUIStyle titleStyle;
        private GUIStyle moneyStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle timeWarpStyle;
        private GUIStyle missionCardStyle;
        private GUIStyle missionTitleStyle;
        private GUIStyle missionDescStyle;
        private GUIStyle telemetryBoxStyle;
        private GUIStyle speedValueStyle;
        private GUIStyle speedUnitStyle;
        private GUIStyle hintBoxStyle;
        private GUIStyle compassLabelStyle;
        private GUIStyle modalBoxStyle;
        private GUIStyle modalTitleStyle;
        private GUIStyle menuButtonStyle;
        private GUIStyle menuButtonPrimaryStyle;
        private GUIStyle menuButtonDangerStyle;
        private GUIStyle toastStyle;
        private bool stylesReady = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();

            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                activeSave = SaveManager.Instance.ActiveSave;
            }

            GenerateRegionalMapTexture();
        }

        private void Update()
        {
            HandleKeyInputs();

            if (saveToastTimer > 0f)
            {
                saveToastTimer -= Time.unscaledDeltaTime;
                if (saveToastTimer <= 0f) saveToastMessage = "";
            }

            UpdateWaypointVisualMarker();
        }

        private void HandleKeyInputs()
        {
            // Não processa atalhos do HUD se o console de comandos estiver aberto digitando
            if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpen)
            {
                return;
            }

            // Atalho [M]: Abre/Fecha o Mapa Regional Tático
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (IsOptionsMenuOpen)
                {
                    CloseOptionsMenu();
                }
                ToggleRegionalMap();
            }

            // Atalho [Alt Esquerdo]: Abre/Fecha o Menu de Opções do Jogo
            if (Input.GetKeyDown(KeyCode.LeftAlt) || (Input.GetKeyDown(KeyCode.Escape) && !IsRegionalMapOpen))
            {
                ToggleOptionsMenu();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && IsRegionalMapOpen)
            {
                CloseRegionalMap();
            }

            // Atalho [H]: Alterna visibilidade do HUD
            if (Input.GetKeyDown(KeyCode.H))
            {
                IsHudVisible = !IsHudVisible;
            }

            // Time Warp via [ e ] ou , e . (Apenas se não estiver no menu de pausa)
            if (!IsOptionsMenuOpen)
            {
                if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period))
                {
                    SetTimeWarp(currentTimeWarpIdx + 1);
                }
                else if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma))
                {
                    SetTimeWarp(currentTimeWarpIdx - 1);
                }
            }
        }


        public void SetTimeWarp(int newIdx)
        {
            currentTimeWarpIdx = Mathf.Clamp(newIdx, 0, timeWarpPresets.Length - 1);
            int multiplier = timeWarpPresets[currentTimeWarpIdx];
            Time.timeScale = multiplier;
            Time.fixedDeltaTime = 0.02f * multiplier;
            previousTimeScale = Time.timeScale;
            Debug.Log($"[TimeWarp] Simulação acelerada: {multiplier}x");
        }

        private void RestoreGameplayCursor()
        {
            if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (!IsRegionalMapOpen && !IsOptionsMenuOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Menu de Opções [Alt Esquerdo] (Sobrepõe tudo se aberto)
            if (IsOptionsMenuOpen)
            {
                DrawLeftAltOptionsMenu();
                DrawToastNotification();
                return;
            }

            // 2. Mapa Regional Tático GIS [M]
            if (IsRegionalMapOpen)
            {
                DrawInteractiveRegionalMap();
                DrawToastNotification();
                return;
            }

            // 3. HUD In-Game Moderno (se visível via [H])
            if (IsHudVisible)
            {
                DrawTopLeftStatusPill();
                if (ShowCompass) DrawTopCompassTape();
                if (ShowMissionTracker) DrawTopRightMissionCard();
                if (ShowTelemetry) DrawVehicleTelemetry();
                DrawContextualHints();
                DrawSubtleKeyShortcuts();
            }

            DrawToastNotification();
        }
    }
}
