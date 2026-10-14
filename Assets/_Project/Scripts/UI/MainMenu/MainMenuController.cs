using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Core;
using ProjectTerra.Planet;
using ProjectTerra.Gameplay;
using ProjectTerra.Cameras;

namespace ProjectTerra.UI
{
    /// <summary>
    /// Controlador principal do Menu Inicial do Project Terra.
    /// Renderiza uma interface moderna e translúcida em primeiro plano,
    /// mantendo o globo terrestre majestoso centralizado no fundo com rotação cinemática.
    /// Fornece opções de Novo Jogo, Continuar, Multiplayer, Configurações e ModHub.
    /// </summary>
    public partial class MainMenuController : MonoBehaviour
    {
        public static MainMenuController Instance { get; private set; }

        public enum MenuScreen
        {
            None,
            Main,
            NewGame,
            SavesList,
            Multiplayer,
            Settings,
            ModHub
        }

        [Header("State")]
        [SerializeField] private MenuScreen currentScreen = MenuScreen.Main;
        [SerializeField] private bool isMenuOpen = true;

        [Header("Cinematic Orbit")]
        [SerializeField] private bool autoRotateGlobe = true;
#pragma warning disable CS0414
        [SerializeField] private float globeRotationSpeed = 1.8f; // Graus por segundo no menu inicial
#pragma warning restore CS0414

        // Animação e Opacidade
        private float screenAlpha = 1.0f;
        private float targetScreenAlpha = 1.0f;

        // Dimensões da UI
        private Rect mainPanelRect;
        private Rect subModalRect;
        private Rect floatingReturnBtnRect;

        // Referências locais
        private CubeSpherePlanet planetRef;
        private Camera cameraRef;
        private OrbitCameraController orbitCamRef;

        public bool IsMenuOpen => isMenuOpen && currentScreen != MenuScreen.None;
        public MenuScreen CurrentScreen => currentScreen;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Garante que o jogo rode em taxa suave no menu
            Application.targetFrameRate = PlayerPrefs.GetInt("Terra_TargetFPS", 120);
        }

        private void Start()
        {
            FindSceneReferences();
            LoadSettings();
            InitModHub();
            InitMultiplayer();
            // InitStyles() é chamado em OnGUI (GUI.skin só é acessível lá)

            // Centraliza o foco inicial na Terra
            if (orbitCamRef != null && planetRef != null)
            {
                orbitCamRef.SetTarget(planetRef.transform, planetRef.PlanetRadius);
            }
        }

        private void FindSceneReferences()
        {
            if (planetRef == null)
            {
                planetRef = FindAnyObjectByType<CubeSpherePlanet>();
            }
            if (cameraRef == null)
            {
                cameraRef = Camera.main;
            }
            if (cameraRef != null && orbitCamRef == null)
            {
                orbitCamRef = cameraRef.GetComponent<OrbitCameraController>();
            }
        }

        private void Update()
        {
            // Transição suave de opacidade da UI
            targetScreenAlpha = (isMenuOpen && currentScreen != MenuScreen.None) ? 1.0f : 0.0f;
            screenAlpha = Mathf.MoveTowards(screenAlpha, targetScreenAlpha, Time.deltaTime * 5.0f);

            // Garante que o planeta continue girando cinematicamente no fundo do menu inicial
            if (isMenuOpen && planetRef != null)
            {
                if (planetRef.IsRotationPaused != !autoRotateGlobe)
                {
                    planetRef.SetRotationPaused(!autoRotateGlobe);
                }
            }

            // Tecla ESC para voltar um nível no menu ou abrir o menu se estiver explorando o globo
            if (TerraInput.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }

            UpdateMultiplayerTimers();
            UpdateModHubDownloads();
        }

        private void HandleEscapeKey()
        {
            if (!isMenuOpen || currentScreen == MenuScreen.None)
            {
                OpenMainMenu();
            }
            else if (currentScreen != MenuScreen.Main)
            {
                currentScreen = MenuScreen.Main;
            }
        }

        public bool IsPointerOverUI()
        {
            if (!isMenuOpen || screenAlpha <= 0.05f)
            {
                // Verifica botão flutuante de voltar ao menu
                Vector2 guiMouse = new Vector2(TerraInput.MousePosition.x, Screen.height - TerraInput.MousePosition.y);
                return floatingReturnBtnRect.Contains(guiMouse);
            }

            Vector2 mousePos = new Vector2(TerraInput.MousePosition.x, Screen.height - TerraInput.MousePosition.y);
            if (mainPanelRect.Contains(mousePos)) return true;
            if (currentScreen != MenuScreen.Main && subModalRect.Contains(mousePos)) return true;
            if (floatingReturnBtnRect.Contains(mousePos)) return true;

            return false;
        }

        public void OpenMainMenu()
        {
            isMenuOpen = true;
            currentScreen = MenuScreen.Main;
            notificationMessage = "";
        }

        public void OpenScreen(MenuScreen screen)
        {
            isMenuOpen = true;
            currentScreen = screen;
            notificationMessage = "";
        }

        public void ExploreGlobeFreely()
        {
            // Oculta a interface para permitir rotação livre de câmera e cliques no planeta
            isMenuOpen = false;
            currentScreen = MenuScreen.None;
            if (PlanetInteractionController.Instance != null)
            {
                PlanetInteractionController.Instance.ClearSelection();
            }
        }

        public void CloseSubModal()
        {
            currentScreen = MenuScreen.Main;
        }
    }
}
