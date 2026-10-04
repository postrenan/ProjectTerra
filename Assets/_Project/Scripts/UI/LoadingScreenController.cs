using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Planet;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.UI
{
    public enum LoadingTheme
    {
        Plantation,
        Metropolis,
        Coastal
    }

    /// <summary>
    /// Tela de carregamento cinematográfica e temática adaptada ao tipo de região selecionada
    /// (Plantação / Agrícola, Metrópole / Urbana, Costeira / Marítima).
    /// Simula e orquestra o streaming de topografia 1:1, dados orçamentários e parâmetros regionais.
    /// </summary>
    public partial class LoadingScreenController : MonoBehaviour
    {
        public static LoadingScreenController Instance { get; private set; }

        [Header("Estado do Carregamento")]
        private bool isVisible = false;
        private float loadProgress = 0f;
        private bool isFinishedLoading = false;
        private float screenAlpha = 1f;
        private bool isFadingOut = false;

        private RegionData currentRegion;
        private RegionSaveData currentSave;
        private Action onCompleteCallback;

        private LoadingTheme currentTheme = LoadingTheme.Plantation;
        private Texture2D backgroundTexture;
        private Texture2D whiteTexture;

        // Texturas em cache
        private Texture2D plantationTex;
        private Texture2D metropolisTex;
        private Texture2D coastalTex;

        // Animação de Ken Burns (zoom suave da imagem de fundo)
        private float animationTimer = 0f;

        // Mensagens técnicas de carregamento de relevo 1:1
        private readonly string[] LoadingStages = new string[]
        {
            "Inicializando projeção geodésica WGS84 e quadtree planetária...",
            "Conectando ao catálogo digital de elevação global (Copernicus DEM 30m / AWS Terrarium)...",
            "Transmitindo malha de relevo 1:1 e gerando curvas de nível topográficas...",
            "Instanciando parcelas agrícolas, biomas de vegetação e redes hidrográficas locais...",
            "Compilando dados orçamentários e estabelecendo soberania regional...",
            "Pronto! Território carregado com sucesso em escala real (1:1)."
        };
        private string currentStatusMessage = "";

        // UI Styles
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusStyle;
        private GUIStyle percentStyle;
        private GUIStyle quoteStyle;
        private GUIStyle dossierCardStyle;
        private GUIStyle enterButtonStyle;
        private bool stylesInitialized = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CreateSolidTextures();
                PreloadBackgrounds();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CreateSolidTextures()
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        private void PreloadBackgrounds()
        {
            string baseDir = Path.Combine(Application.dataPath, "_Project", "Textures", "Loading");
            plantationTex = LoadTextureFromFile(Path.Combine(baseDir, "plantation_bg.jpg"));
            metropolisTex = LoadTextureFromFile(Path.Combine(baseDir, "metropolis_bg.jpg"));
            coastalTex = LoadTextureFromFile(Path.Combine(baseDir, "coastal_bg.jpg"));
        }

        private Texture2D LoadTextureFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (tex.LoadImage(fileData))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        return tex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LoadingScreen] Erro ao carregar imagem {filePath}: {ex.Message}");
                }
            }
            return null;
        }

        public void Show(RegionData region, RegionSaveData save, Action onFinished = null)
        {
            currentRegion = region;
            currentSave = save;
            onCompleteCallback = onFinished;

            // Determina o tema conforme o bioma / dados da região
            DetermineTheme(region);

            isVisible = true;
            isFinishedLoading = false;
            isFadingOut = false;
            loadProgress = 0f;
            screenAlpha = 1f;
            animationTimer = 0f;
            currentStatusMessage = LoadingStages[0];

            // Inicia simulação e pré-carregamento de topografia 1:1
            StartCoroutine(LoadingSimulationCoroutine());
        }

        private void DetermineTheme(RegionData region)
        {
            if (region == null)
            {
                currentTheme = LoadingTheme.Plantation;
                backgroundTexture = plantationTex;
                return;
            }

            // 1. Água / Marítimo / Costeiro
            if (region.waterPercent >= 45 || region.id == 0 || region.name.Contains("Oceano") || region.name.Contains("Mar") || region.name.Contains("Ilha") || region.type.Contains("Ilha"))
            {
                currentTheme = LoadingTheme.Coastal;
                backgroundTexture = coastalTex;
            }
            // 2. Metrópole / Centro Urbano (São Paulo, Berlim, Tóquio, Nova York, capitais)
            else if (region.mineralsPercent >= 38 && region.arablePercent < 55 ||
                     region.type.Contains("Federal") || region.type.Contains("Metropolitana") || region.type.Contains("Capital") ||
                     region.name.Contains("São Paulo") || region.name.Contains("Berlin") || region.name.Contains("Tokyo") || region.name.Contains("New York") || region.name.Contains("Paris") || region.name.Contains("London"))
            {
                currentTheme = LoadingTheme.Metropolis;
                backgroundTexture = metropolisTex;
            }
            // 3. Plantação / Agrícola (Padrão para terras aráveis abundantes)
            else
            {
                currentTheme = LoadingTheme.Plantation;
                backgroundTexture = plantationTex;
            }

            // Fallback se uma imagem específica não carregou
            if (backgroundTexture == null)
            {
                backgroundTexture = plantationTex ?? metropolisTex ?? coastalTex;
            }
        }

        private IEnumerator LoadingSimulationCoroutine()
        {
            float targetDuration = 3.6f;
            float elapsed = 0f;

            // Inicia solicitação de tile de elevação 1:1 na coordenada da região
            if (TerrainDataService.Instance != null && currentRegion != null)
            {
                Vector2Int tile = TerrainDataService.LatLonToTile(currentRegion.centerLat, currentRegion.centerLon, 9);
                TerrainDataService.Instance.RequestElevationTile(9, tile.x, tile.y, (heights) =>
                {
                    Debug.Log($"[LoadingScreen] Topografia 1:1 pré-carregada para {currentRegion.name}: 256x256 pontos de elevação prontos.");
                });
            }

            while (elapsed < targetDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / targetDuration);
                // Curva de progresso suave com aceleração e desaceleração orgânica
                loadProgress = Mathf.SmoothStep(0f, 1f, t);

                // Alterna mensagens de estágio
                int stageIdx = Mathf.Clamp((int)(loadProgress * LoadingStages.Length), 0, LoadingStages.Length - 1);
                currentStatusMessage = LoadingStages[stageIdx];

                yield return null;
            }

            loadProgress = 1.0f;
            currentStatusMessage = LoadingStages[LoadingStages.Length - 1];
            isFinishedLoading = true;
        }

        private void Update()
        {
            if (!isVisible) return;

            animationTimer += Time.deltaTime;

            if (isFinishedLoading && !isFadingOut)
            {
                // Tecla Enter ou Espaço para entrar imediatamente
                if (TerraInput.GetKeyDown(Key.Enter) || TerraInput.GetKeyDown(Key.NumpadEnter) || TerraInput.GetKeyDown(Key.Space))
                {
                    StartFadeOut();
                }
            }

            if (isFadingOut)
            {
                screenAlpha -= Time.deltaTime * 2.2f;
                if (screenAlpha <= 0f)
                {
                    screenAlpha = 0f;
                    isVisible = false;
                    isFadingOut = false;
                    onCompleteCallback?.Invoke();
                }
            }
        }

        public void StartFadeOut()
        {
            if (!isFadingOut)
            {
                isFadingOut = true;
            }
        }
    }
}
