using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// HUD e Gerenciador de Interface da Cena Sandbox Regional:
    /// 1. Layout UI/UX limpo e moderno (Status Pill no topo-esquerdo, Bússola 360° no topo-central, Rastreador no topo-direito e Telemetria no canto inferior).
    /// 2. Atalho [M]: Abre o Mapa Regional Tático GIS interativo com Pan, Zoom, marcadores e criação de waypoints customizados ao clicar.
    /// 3. Atalho [Alt Esquerdo]: O personagem saca o celular (canto inferior direito, estilo GTA V) com apps: Mapa, Internet, Contatos, Missões, Ajustes e Jogo (Salvar/Sair). O mundo não pausa.
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

        // Estilos do Celular (estilo GTA V)
        private GUIStyle phoneStatusStyle;
        private GUIStyle phoneHeaderStyle;
        private GUIStyle phoneIconStyle;
        private GUIStyle phoneAppLabelStyle;
        private GUIStyle phoneBackStyle;
        private GUIStyle phoneUrlStyle;
        private GUIStyle phoneSectionStyle;
        private GUIStyle phoneListButtonStyle;
        private GUIStyle phoneLinkStyle;
        private GUIStyle phoneBodyStyle;
        private GUIStyle phoneSmallStyle;
        private GUIStyle phoneLockStyle;

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

            // Anima o celular deslizando para cima/baixo (independente do timeScale).
            float phoneTarget = IsOptionsMenuOpen ? 1f : 0f;
            phoneAnim = Mathf.MoveTowards(phoneAnim, phoneTarget, Time.unscaledDeltaTime * 6.5f);

            // Regeneração adiada do mercado de fretes (fora do OnGUI).
            TickJobMarket();

            if (saveToastTimer > 0f)
            {
                saveToastTimer -= Time.unscaledDeltaTime;
                if (saveToastTimer <= 0f) saveToastMessage = "";
            }

            UpdateWaypointVisualMarker();
        }

        private void HandleKeyInputs()
        {
            // Não processa atalhos do HUD se o console de comandos estiver aberto ou tiver fechado neste frame
            if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpenOrJustClosed)
            {
                return;
            }

            // Navegador da loja em tela cheia: consome o input e bloqueia os demais atalhos.
            if (HandleBrowserInput())
            {
                return;
            }

            // Atalho [M]: Abre/Fecha o Mapa Regional Tático
            if (TerraInput.GetKeyDown(Key.M))
            {
                if (IsOptionsMenuOpen)
                {
                    CloseOptionsMenu();
                }
                ToggleRegionalMap();
            }

            // Atalho [Alt Esquerdo]: Saca/guarda o celular. Com o celular aberto dentro
            // de um app, o atalho volta primeiro para a tela inicial (como o botão home).
            if (TerraInput.GetKeyDown(Key.LeftAlt) || (TerraInput.GetKeyDown(Key.Escape) && !IsRegionalMapOpen))
            {
                PhoneBack();
            }
            else if (TerraInput.GetKeyDown(Key.Escape) && IsRegionalMapOpen)
            {
                CloseRegionalMap();
            }

            // Atalho [H]: Alterna visibilidade do HUD
            if (TerraInput.GetKeyDown(Key.H))
            {
                IsHudVisible = !IsHudVisible;
            }

            // Time Warp via [ e ] (Apenas se não estiver no menu de pausa).
            // Os aliases , e . foram removidos de propósito: eles passaram a ser o
            // leme de cauda do avião (VehicleController.Physics). Com as duas
            // formas ativas, virar o leme também mudava a velocidade do tempo.
            if (!IsOptionsMenuOpen)
            {
                if (TerraInput.GetKeyDown(Key.RightBracket))
                {
                    SetTimeWarp(currentTimeWarpIdx + 1);
                }
                else if (TerraInput.GetKeyDown(Key.LeftBracket))
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

            // 0. Navegador da loja em tela cheia (por cima de tudo).
            if (IsBrowserOpen)
            {
                DrawBrowser();
                DrawToastNotification();
                return;
            }

            // 1. Mapa Regional Tático GIS [M] (tela cheia)
            if (IsRegionalMapOpen)
            {
                DrawInteractiveRegionalMap();
                DrawToastNotification();
                return;
            }

            bool phoneActive = IsOptionsMenuOpen || phoneAnim > 0.001f;

            // 2. HUD In-Game Moderno (se visível via [H]). O celular ocupa apenas o
            //    canto inferior direito, então o HUD do topo permanece. Os elementos
            //    do rodapé/direita são ocultados para não colidir com o aparelho.
            if (IsHudVisible)
            {
                DrawTopLeftStatusPill();
                if (ShowCompass) DrawTopCompassTape();
                if (ShowMissionTracker) DrawTopRightMissionCard();
                if (!phoneActive)
                {
                    if (ShowTelemetry) DrawVehicleTelemetry();
                    DrawContextualHints();
                    DrawSubtleKeyShortcuts();
                }
            }

            // 3. Celular do personagem [Alt Esquerdo] por cima do HUD.
            if (phoneActive)
            {
                DrawPhone();
            }

            DrawToastNotification();
        }
    }
}
