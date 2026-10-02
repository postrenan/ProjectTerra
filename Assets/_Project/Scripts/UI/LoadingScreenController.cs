using System;
using System.Collections;
using System.IO;
using UnityEngine;
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
    public class LoadingScreenController : MonoBehaviour
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
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
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

        private void InitStyles()
        {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            subtitleStyle.normal.textColor = new Color(0.35f, 0.85f, 1.0f);

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            statusStyle.normal.textColor = new Color(0.9f, 0.95f, 1.0f);

            percentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            percentStyle.normal.textColor = new Color(1.0f, 0.9f, 0.35f);

            quoteStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            quoteStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f, 0.85f);

            dossierCardStyle = new GUIStyle(GUI.skin.box);

            enterButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            enterButtonStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible || screenAlpha <= 0f) return;

            InitStyles();
            GUI.depth = -1000; // Desenha sobre todos os outros elementos da interface

            float sw = Screen.width;
            float sh = Screen.height;

            // Salva cor da GUI com fade alpha
            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            // 1. Imagem de Fundo Panorâmica com Efeito Ken Burns (Zoom e Pan suave)
            if (backgroundTexture != null)
            {
                float zoom = 1.05f + 0.05f * Mathf.Sin(animationTimer * 0.15f);
                float panX = Mathf.Sin(animationTimer * 0.08f) * 20.0f;
                float panY = Mathf.Cos(animationTimer * 0.06f) * 10.0f;

                float bw = sw * zoom;
                float bh = sh * zoom;
                float bx = (sw - bw) * 0.5f + panX;
                float by = (sh - bh) * 0.5f + panY;

                GUI.DrawTexture(new Rect(bx, by, bw, bh), backgroundTexture, ScaleMode.ScaleAndCrop);
            }
            else
            {
                // Fallback de gradiente escuro elegante
                Color topBg = currentTheme == LoadingTheme.Plantation ? new Color(0.12f, 0.16f, 0.08f) :
                              (currentTheme == LoadingTheme.Metropolis ? new Color(0.08f, 0.1f, 0.18f) : new Color(0.06f, 0.14f, 0.18f));
                GUI.color = topBg * screenAlpha;
                GUI.DrawTexture(new Rect(0, 0, sw, sh), whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }

            // 2. Vinheta e Gradientes Escuros de Legibilidade
            // Gradiente Superior
            GUI.color = new Color(0f, 0f, 0f, 0.75f * screenAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, 180), whiteTexture);
            // Gradiente Inferior
            GUI.color = new Color(0f, 0f, 0f, 0.85f * screenAlpha);
            GUI.DrawTexture(new Rect(0, sh - 220, sw, 220), whiteTexture);
            // Painel Lateral de Relevo
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.70f * screenAlpha);
            GUI.DrawTexture(new Rect(sw - 480, 0, 480, sh), whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            // 3. Cabeçalho Principal (Top-Left)
            GUILayout.BeginArea(new Rect(40, 30, sw - 540, 130));
            string themeTag = currentTheme == LoadingTheme.Plantation ? "🌾 ZONA DE PRODUÇÃO AGRÍCOLA & PLANTAÇÕES GLOBAIS" :
                             (currentTheme == LoadingTheme.Metropolis ? "🏙️ METRÓPOLE GLOBAL & CENTRO ECONÔMICO-FINANCEIRO" : "⚓ POLO LOGÍSTICO PORTUÁRIO & LITORAL");
            GUILayout.Label(themeTag, subtitleStyle);
            GUILayout.Space(2);
            string displayTitle = currentSave != null && !string.IsNullOrEmpty(currentSave.saveName) ?
                currentSave.saveName.ToUpper() : $"GOVERNO DE {(currentRegion != null ? currentRegion.name.ToUpper() : "TERRA")}";
            GUILayout.Label(displayTitle, titleStyle);
            GUILayout.Space(4);
            string locationDesc = currentRegion != null ?
                $"{currentRegion.country.ToUpper()} • {currentRegion.type} • Coordenadas: Lat {currentRegion.centerLat:F2}° | Lon {currentRegion.centerLon:F2}° (Escala 1:1 Datum WGS84)" :
                "TERRITÓRIO GLOBAL • ESCALA 1:1";
            GUILayout.Label(locationDesc, quoteStyle);
            GUILayout.EndArea();

            // 4. Painel de Dossiê Regional e Econômico (Top-Right Glassmorphism)
            GUILayout.BeginArea(new Rect(sw - 450, 40, 410, sh - 280));
            GUI.color = new Color(0.05f, 0.1f, 0.18f, 0.88f * screenAlpha);
            GUILayout.BeginVertical(dossierCardStyle, GUILayout.ExpandHeight(true));
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            GUILayout.Label("📊 DOSSIÊ ESTRATÉGICO REGIONAL", subtitleStyle);
            GUILayout.Space(6);

            // Divisor
            GUI.color = new Color(0.3f, 0.7f, 1f, 0.35f * screenAlpha);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            GUILayout.Space(8);

            // Orçamento Inicial
            string budgetStr = currentSave != null ? currentSave.GetFormattedMoney() : "$500,000";
            GUILayout.Label("💰 Orçamento de Tesouro Inicial:", quoteStyle);
            GUI.color = new Color(0.3f, 1.0f, 0.5f);
            GUILayout.Label(budgetStr, percentStyle);
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            GUILayout.Space(12);

            // Indicadores de Recursos
            if (currentRegion != null)
            {
                DrawDossierStat("🌾 Terras Aráveis & Plantações", currentRegion.arablePercent, new Color(1f, 0.88f, 0.3f));
                DrawDossierStat("🌲 Cobertura Florestal", currentRegion.forestPercent, new Color(0.2f, 0.85f, 0.35f));
                DrawDossierStat("⛏️ Potencial Mineral e Geológico", currentRegion.mineralsPercent, new Color(1f, 0.65f, 0.25f));
                DrawDossierStat("💧 Recursos Hídricos e Aquíferos", currentRegion.waterPercent, new Color(0.25f, 0.75f, 1f));
            }

            GUILayout.Space(16);
            GUILayout.Label("💡 DIRETRIZ DE GESTÃO DO TERRITÓRIO:", subtitleStyle);
            GUILayout.Space(4);
            string tipText = currentTheme == LoadingTheme.Plantation ?
                "O cultivo sustentável em grande escala garante a segurança alimentar e insumos industriais vitais. O relevo suave e os aquíferos locais facilitam irrigação pivotante de alta produtividade." :
                (currentTheme == LoadingTheme.Metropolis ?
                "Áreas urbanas concentram os maiores mercados consumidores e pólos tecnológicos do planeta. Gerencie a matriz energética e zoneamentos industriais para maximizar a arrecadação fiscal." :
                "A infraestrutura portuária conecta a produção agrícola e mineral aos mercados mundiais. Preserve os ecossistemas marinhos enquanto expande a capacidade de escoamento logístico.");
            GUILayout.Label(tipText, quoteStyle);

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 5. Barra de Progresso e Notificação Técnica de Topografia 1:1 (Rodapé)
            GUILayout.BeginArea(new Rect(50, sh - 170, sw - 540, 140));

            // Status e Porcentagem
            GUILayout.BeginHorizontal();
            GUILayout.Label(currentStatusMessage, statusStyle, GUILayout.Width(sw - 660));
            GUILayout.Label($"{Mathf.RoundToInt(loadProgress * 100f)}%", percentStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // Fundo da Barra de Progresso
            Rect progressBgRect = GUILayoutUtility.GetRect(sw - 540, 12);
            GUI.color = new Color(0.12f, 0.15f, 0.22f, 0.95f * screenAlpha);
            GUI.DrawTexture(progressBgRect, whiteTexture);

            // Preenchimento com Brilho
            float barWidth = (progressBgRect.width - 4) * loadProgress;
            Rect progressFillRect = new Rect(progressBgRect.x + 2, progressBgRect.y + 2, barWidth, progressBgRect.height - 4);
            Color barColor = isFinishedLoading ?
                new Color(0.35f, 0.95f, 0.5f, 1f * screenAlpha) :
                new Color(0.3f, 0.75f, 1f, 1f * screenAlpha);
            GUI.color = barColor;
            GUI.DrawTexture(progressFillRect, whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            GUILayout.Space(14);

            // 6. Botão de Entrada Interativa / Conclusão
            if (isFinishedLoading)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 5.0f);
                GUI.color = new Color(0.2f, 0.8f, 0.4f, pulse * screenAlpha);
                if (GUILayout.Button("▶ ENTRAR NO TERRITÓRIO [ENTER]", enterButtonStyle, GUILayout.Height(40), GUILayout.Width(340)))
                {
                    StartFadeOut();
                }
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }
            else
            {
                GUI.color = new Color(0.6f, 0.75f, 0.9f, 0.6f * screenAlpha);
                GUILayout.Label("Pressione [ESC] para cancelar se desejar retornar à órbita.", quoteStyle);
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }

            GUILayout.EndArea();

            GUI.color = prevColor;
        }

        private void DrawDossierStat(string label, float percent, Color barColor)
        {
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, quoteStyle, GUILayout.Width(240));
            GUILayout.Label($"{percent:F0}%", quoteStyle);
            GUILayout.EndHorizontal();

            Rect bgRect = GUILayoutUtility.GetRect(380, 6);
            GUI.color = new Color(0.1f, 0.15f, 0.22f, 0.8f);
            GUI.DrawTexture(bgRect, whiteTexture);

            Rect fillRect = new Rect(bgRect.x, bgRect.y, bgRect.width * (percent / 100f), bgRect.height);
            GUI.color = barColor;
            GUI.DrawTexture(fillRect, whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(2);
        }
    }
}
