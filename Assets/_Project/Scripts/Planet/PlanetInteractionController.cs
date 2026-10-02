using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;
using ProjectTerra.Planet.TerrainStreaming;
using ProjectTerra.Cameras;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Controla a seleção interativa de Estados/Províncias e Países ao clicar no globo.
    /// Interrompe a rotação do planeta, destaca o terreno selecionado via shader e
    /// exibe o painel de estatísticas com opções completas de Novo Jogo e Carregar Jogo.
    /// </summary>
    public class PlanetInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CubeSpherePlanet planet;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Material earthMaterial;

        [Header("Selected State")]
        [SerializeField] private bool hasSelection = false;
        [SerializeField] private RegionData selectedRegion;
        [SerializeField] private Vector3 selectedLocalNormal;
        [SerializeField] private Vector3 selectedWorldPoint;

        private RegionDatabase database;
        private byte[] regionIdMap;
        private const int MapWidth = 4096;
        private const int MapHeight = 2048;
        private bool isDatabaseLoaded = false;

        private Rect lastCardRect;
        private Vector2 mouseDownPos;
        private bool isMouseDownOnCard;

        // Modos da UI de Interação
        public enum CardMode { RegionDetails, NewGamePrompt, LoadGameList }
        private CardMode currentCardMode = CardMode.RegionDetails;

        // Estado do Formulário de Novo Jogo
        private string newGameSaveName = "";
        private long newGameBudget = 500000;
        private StarterCareer selectedCareer = StarterCareer.Farmer;
        private readonly long[] BudgetPresets = { 100000, 250000, 500000, 1000000, 2500000, 5000000 };
        private readonly string[] BudgetLabels = { "$100k", "$250k", "$500k", "$1M", "$2.5M", "$5M" };

        // Estado da Lista de Saves
        private Vector2 loadScrollPos = Vector2.zero;
        private string notificationMessage = "";
        private float notificationTimer = 0f;

        // UI Styling
        private GUIStyle cardStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle dangerButtonStyle;
        private GUIStyle saveCardBoxStyle;
        private GUIStyle textFieldStyle;
        private Texture2D whiteTex;

        public static PlanetInteractionController Instance { get; private set; }

        public bool IsPointerOverUI()
        {
            if (!hasSelection) return false;
            Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return lastCardRect.Contains(guiMouse);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (planet == null)
            {
                planet = FindAnyObjectByType<CubeSpherePlanet>();
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            EnsureServicesExist();
            ResolveEarthMaterial();
            LoadDatabase();
            CreateTextures();
        }

        private void EnsureServicesExist()
        {
            if (SaveManager.Instance == null && FindAnyObjectByType<SaveManager>() == null)
            {
                var go = new GameObject("SaveManager");
                go.AddComponent<SaveManager>();
            }
            if (TerrainDataService.Instance == null && FindAnyObjectByType<TerrainDataService>() == null)
            {
                var go = new GameObject("TerrainDataService");
                go.AddComponent<TerrainDataService>();
            }
            if (LoadingScreenController.Instance == null && FindAnyObjectByType<LoadingScreenController>() == null)
            {
                var go = new GameObject("LoadingScreenController");
                go.AddComponent<LoadingScreenController>();
            }
        }

        private void ResolveEarthMaterial()
        {
            if (earthMaterial == null && planet != null)
            {
                earthMaterial = planet.PlanetMaterial;
            }
        }

        private void CreateTextures()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }

        private void LoadDatabase()
        {
            string binDbPath = Path.Combine(Application.streamingAssetsPath, "regions_database.bin");
            string jsonDbPath = Path.Combine(Application.streamingAssetsPath, "regions_database.json");
            string binPath = Path.Combine(Application.streamingAssetsPath, "region_id_map.bin");

            if (File.Exists(binDbPath))
            {
                try
                {
                    database = RegionDatabase.LoadFromBinary(binDbPath);
                    isDatabaseLoaded = database != null && database.regions.Count > 0;
                    Debug.Log($"[PlanetInteraction] Base de dados binária carregada com {database.regions.Count} regiões ({new FileInfo(binDbPath).Length / 1024} KB).");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar regions_database.bin: {ex.Message}");
                }
            }
            else if (File.Exists(jsonDbPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonDbPath);
                    database = JsonUtility.FromJson<RegionDatabase>(json);
                    isDatabaseLoaded = database != null && database.regions.Count > 0;
                    Debug.Log($"[PlanetInteraction] Base de dados legada (JSON) carregada com {database.regions.Count} regiões.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar regions_database.json: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlanetInteraction] Arquivo de regiões não encontrado em {binDbPath} nem {jsonDbPath}");
            }

            if (File.Exists(binPath))
            {
                try
                {
                    regionIdMap = File.ReadAllBytes(binPath);
                    Debug.Log($"[PlanetInteraction] Mapa binário de IDs geográficos carregado ({regionIdMap.Length} bytes).");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar region_id_map.bin: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlanetInteraction] Arquivo region_id_map.bin não encontrado em {binPath}");
            }
        }

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f) notificationMessage = "";
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                isMouseDownOnCard = hasSelection && lastCardRect.Contains(guiMouse);
                mouseDownPos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (!isMouseDownOnCard)
                {
                    float dragDist = Vector2.Distance(mouseDownPos, (Vector2)Input.mousePosition);
                    if (dragDist < 12.0f)
                    {
                        TryRaycastPlanet(Input.mousePosition);
                    }
                }
                isMouseDownOnCard = false;
            }

            // Acompanha a rotação do planeta se houver ponto selecionado
            if (hasSelection && planet != null)
            {
                selectedWorldPoint = planet.transform.TransformPoint(selectedLocalNormal * (float)planet.PlanetRadius);
            }
        }

        private void TryRaycastPlanet(Vector2 screenPos)
        {
            if (planet == null)
            {
                planet = CubeSpherePlanet.Instance ?? FindAnyObjectByType<CubeSpherePlanet>();
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (planet == null || mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            Vector3 center = planet.transform.position;
            double radius = planet.PlanetRadius;

            // Interseção analítica raio-esfera de alta precisão (double precision)
            double ocX = ray.origin.x - center.x;
            double ocY = ray.origin.y - center.y;
            double ocZ = ray.origin.z - center.z;

            double dirX = ray.direction.x;
            double dirY = ray.direction.y;
            double dirZ = ray.direction.z;

            double b = ocX * dirX + ocY * dirY + ocZ * dirZ;
            double c = (ocX * ocX + ocY * ocY + ocZ * ocZ) - (radius * radius);
            double discriminant = b * b - c;

            if (discriminant >= 0.0)
            {
                double t = -b - Math.Sqrt(discriminant);
                if (t > 0.0)
                {
                    Vector3 worldHit = ray.origin + ray.direction * (float)t;
                    Vector3 localHit = planet.transform.InverseTransformPoint(worldHit);
                    selectedLocalNormal = localHit.normalized;
                    selectedWorldPoint = worldHit;

                    // Converter coordenadas locais para Latitude e Longitude
                    float lat = Mathf.Asin(Mathf.Clamp(selectedLocalNormal.y, -1f, 1f)) * Mathf.Rad2Deg;
                    float lon = Mathf.Atan2(selectedLocalNormal.x, -selectedLocalNormal.z) * Mathf.Rad2Deg;

                    SelectRegionAt(lat, lon);
                }
            }
        }

        private void SelectRegionAt(float lat, float lon)
        {
            if (!isDatabaseLoaded || database == null || database.regions.Count == 0)
            {
                LoadDatabase();
            }

            RegionData match = null;

            // 1. Amostragem direta no mapa binário de IDs geográficos
            if (regionIdMap != null && regionIdMap.Length == MapWidth * MapHeight * 2)
            {
                int px = Mathf.Clamp((int)((lon + 180.0f) / 360.0f * (MapWidth - 1)), 0, MapWidth - 1);
                int py = Mathf.Clamp((int)((90.0f - lat) / 180.0f * (MapHeight - 1)), 0, MapHeight - 1);

                ushort id = GetIdAt(px, py);

                // Se cair em água rasa, baía ou costa (id 0), verifica tolerância litorânea de até 6 pixels (~50km)
                if (id == 0)
                {
                    float bestDistSqr = float.MaxValue;
                    ushort nearestLandId = 0;

                    for (int r = 1; r <= 6; r++)
                    {
                        for (int dy = -r; dy <= r; dy++)
                        {
                            for (int dx = -r; dx <= r; dx++)
                            {
                                if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                                int nx = Mathf.Clamp(px + dx, 0, MapWidth - 1);
                                int ny = Mathf.Clamp(py + dy, 0, MapHeight - 1);
                                ushort candId = GetIdAt(nx, ny);
                                if (candId > 0 && database != null && candId <= database.regions.Count)
                                {
                                    float dSqr = dx * dx + dy * dy;
                                    if (dSqr < bestDistSqr)
                                    {
                                        bestDistSqr = dSqr;
                                        nearestLandId = candId;
                                    }
                                }
                            }
                        }
                        if (nearestLandId > 0 && r <= 4)
                        {
                            break;
                        }
                    }

                    if (nearestLandId > 0)
                    {
                        id = nearestLandId;
                    }
                }

                if (id > 0 && database != null && id <= database.regions.Count)
                {
                    match = database.regions[id - 1];
                }
            }

            if (match == null)
            {
                selectedRegion = GenerateOceanRegion(lat, lon);
                SetTerrainHighlight(0);
            }
            else
            {
                selectedRegion = match;
                SetTerrainHighlight(match.id);
            }

            // Reseta estado para a tela principal de detalhes da região selecionada
            currentCardMode = CardMode.RegionDetails;
            newGameSaveName = $"Governo de {selectedRegion.name}";
            newGameBudget = 500000;
            notificationMessage = "";

            hasSelection = true;
            Debug.Log($"[PlanetInteraction] Região Selecionada: {selectedRegion.name} ({selectedRegion.country}) [ID: {selectedRegion.id}] - Lat: {lat:F2}°, Lon: {lon:F2}°");

            // Pausa a rotação do planeta conforme solicitado pelo usuário
            if (planet != null)
            {
                planet.SetRotationPaused(true);
            }
        }

        private void SetTerrainHighlight(int regionId)
        {
            ResolveEarthMaterial();
            if (earthMaterial != null)
            {
                earthMaterial.SetFloat("_SelectedRegionId", (float)regionId);
            }
        }

        private ushort GetIdAt(int px, int py)
        {
            int idx = (py * MapWidth + px) * 2;
            return (ushort)(regionIdMap[idx] | (regionIdMap[idx + 1] << 8));
        }

        private RegionData GenerateOceanRegion(float lat, float lon)
        {
            string oceanName = "Oceano Global";
            if (lat > 65.0f)
            {
                oceanName = "Oceano Ártico";
            }
            else if (lat < -60.0f)
            {
                oceanName = "Oceano Antártico";
            }
            else if (lat >= 30.0f && lat <= 46.0f && lon >= -6.0f && lon <= 36.0f)
            {
                oceanName = "Mar Mediterrâneo";
            }
            else if (lat >= 53.0f && lat <= 66.0f && lon >= 10.0f && lon <= 30.0f)
            {
                oceanName = "Mar Báltico";
            }
            else if (lon >= -75.0f && lon <= 20.0f)
            {
                oceanName = lat >= 0 ? "Oceano Atlântico Norte" : "Oceano Atlântico Sul";
            }
            else if (lon > 20.0f && lon <= 100.0f && lat <= 30.0f)
            {
                oceanName = "Oceano Índico";
            }
            else
            {
                oceanName = lat >= 0 ? "Oceano Pacífico Norte" : "Oceano Pacífico Sul";
            }

            return new RegionData
            {
                id = 0,
                name = oceanName,
                country = "Águas Internacionais",
                type = "Zona Marítima",
                centerLat = lat,
                centerLon = lon,
                forestPercent = 0,
                mineralsPercent = 40,
                arablePercent = 0,
                waterPercent = 100
            };
        }

        public void ResumeRotation()
        {
            hasSelection = false;
            lastCardRect = Rect.zero;
            currentCardMode = CardMode.RegionDetails;
            SetTerrainHighlight(0);

            if (planet != null)
            {
                planet.SetRotationPaused(false);
            }
        }

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
        }

        private void OnGUI()
        {
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

            InitStyles();

            // Dimensões dinâmicas do card conforme a aba
            float width = currentCardMode == CardMode.RegionDetails ? 330f : (currentCardMode == CardMode.NewGamePrompt ? 420f : 390f);
            float height = currentCardMode == CardMode.RegionDetails ? 315f : (currentCardMode == CardMode.NewGamePrompt ? 520f : 380f);
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

        private void DrawRegionDetailsUI()
        {
            // Cabeçalho
            GUILayout.Label(selectedRegion.name, headerTitleStyle);
            GUILayout.Label($"{selectedRegion.country} • {selectedRegion.type}", headerSubtitleStyle);
            GUILayout.Space(6);

            // Divisória
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Barras de Recursos com percentuais
            DrawResourceBar("🌲 Cobertura Florestal", selectedRegion.forestPercent, new Color(0.2f, 0.85f, 0.3f));
            DrawResourceBar("⛏️ Potencial Mineral", selectedRegion.mineralsPercent, new Color(1.0f, 0.65f, 0.2f));
            DrawResourceBar("🌾 Terras Aráveis", selectedRegion.arablePercent, new Color(0.95f, 0.85f, 0.25f));
            DrawResourceBar("💧 Recursos Hídricos", selectedRegion.waterPercent, new Color(0.25f, 0.7f, 1.0f));

            GUILayout.Space(8);

            // Botões de Ação de Jogo (Apenas para regiões terrestres)
            if (selectedRegion.id > 0)
            {
                List<RegionSaveData> saves = SaveManager.Instance != null ?
                    SaveManager.Instance.GetSavesForRegion(selectedRegion.id) : new List<RegionSaveData>();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🎮 Novo Jogo", primaryButtonStyle, GUILayout.Height(28)))
                {
                    currentCardMode = CardMode.NewGamePrompt;
                    newGameSaveName = $"Governo de {selectedRegion.name}";
                }

                string loadBtnLabel = saves.Count > 0 ? $"💾 Carregar ({saves.Count})" : "💾 Carregar (0)";
                if (GUILayout.Button(loadBtnLabel, buttonStyle, GUILayout.Height(28)))
                {
                    currentCardMode = CardMode.LoadGameList;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);
            }

            // Notificação temporária se houver
            if (!string.IsNullOrEmpty(notificationMessage))
            {
                GUI.color = new Color(0.4f, 1f, 0.5f);
                GUILayout.Label(notificationMessage, labelStyle);
                GUI.color = Color.white;
                GUILayout.Space(2);
            }

            // Rodapé: Retomar rotação e Fechar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Retomar Rotação", buttonStyle, GUILayout.Height(24)))
            {
                ResumeRotation();
            }
            if (GUILayout.Button("✖ Fechar", buttonStyle, GUILayout.Width(70), GUILayout.Height(24)))
            {
                hasSelection = false;
                lastCardRect = Rect.zero;
                SetTerrainHighlight(0);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNewGameUI()
        {
            GUILayout.Label("🎮 Configurar Nova Partida", headerTitleStyle);
            GUILayout.Label($"Região: {selectedRegion.name} ({selectedRegion.country})", headerSubtitleStyle);
            GUILayout.Space(4);

            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // 1. Escolha da Carreira Inicial (Marco 1)
            GUILayout.Label("Escolha sua Primeira Jornada (Ofício Inicial):", labelStyle);
            GUILayout.BeginHorizontal();

            // Botão Agricultor
            bool isFarmer = selectedCareer == StarterCareer.Farmer;
            GUI.color = isFarmer ? new Color(0.3f, 1f, 0.4f) : Color.white;
            if (GUILayout.Button("🌾 Agricultor", buttonStyle, GUILayout.Height(26)))
            {
                selectedCareer = StarterCareer.Farmer;
            }

            // Botão Motorista
            bool isTrucker = selectedCareer == StarterCareer.Trucker;
            GUI.color = isTrucker ? new Color(1f, 0.85f, 0.3f) : Color.white;
            if (GUILayout.Button("🚛 Motorista", buttonStyle, GUILayout.Height(26)))
            {
                selectedCareer = StarterCareer.Trucker;
            }

            // Botão Aviador
            bool isAviator = selectedCareer == StarterCareer.Aviator;
            GUI.color = isAviator ? new Color(0.4f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button("🛩️ Aviador", buttonStyle, GUILayout.Height(26)))
            {
                selectedCareer = StarterCareer.Aviator;
            }

            // Botão Pescador
            bool isFisherman = selectedCareer == StarterCareer.Fisherman;
            GUI.color = isFisherman ? new Color(0.3f, 0.95f, 0.95f) : Color.white;
            if (GUILayout.Button("🎣 Pescador", buttonStyle, GUILayout.Height(26)))
            {
                selectedCareer = StarterCareer.Fisherman;
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Descrição da Carreira Selecionada
            GUI.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            string careerDesc = "";
            switch (selectedCareer)
            {
                case StarterCareer.Farmer:
                    careerDesc = "🌾 Comece em uma fazenda rural com um trator utilitário e campo cultivável para semear, colher e levar a safra à cooperativa.";
                    break;
                case StarterCareer.Trucker:
                    careerDesc = "🚛 Comece em um galpão logístico na cidade com caminhonete/caminhão de carga e cumpra contratos de frete rodoviário.";
                    break;
                case StarterCareer.Aviator:
                    careerDesc = "🛩️ Comece em uma pista de pouso com avião monomotor leve utilitário para missões aéreas, inspeção e entrega expressa.";
                    break;
                case StarterCareer.Fisherman:
                    careerDesc = "🎣 Comece no cais do porto costeiro com um barco pesqueiro para pescas em alto-mar e venda direta na lota da cidade.";
                    break;
            }
            GUILayout.Label(careerDesc, headerSubtitleStyle);
            GUI.color = Color.white;
            GUILayout.Space(6);

            // 2. Nome da Partida
            GUILayout.Label("Nome da Empresa / Partida:", labelStyle);
            newGameSaveName = GUILayout.TextField(newGameSaveName, 40, textFieldStyle, GUILayout.Height(24));
            GUILayout.Space(6);

            // 3. Orçamento Inicial
            GUILayout.BeginHorizontal();
            GUILayout.Label("Orçamento Inicial:", labelStyle);
            GUILayout.Label($"${newGameBudget:N0}", valueStyle);
            GUILayout.EndHorizontal();

            // Botões de Presets rápidos de orçamento
            GUILayout.BeginHorizontal();
            for (int i = 0; i < BudgetPresets.Length; i++)
            {
                bool isSelected = newGameBudget == BudgetPresets[i];
                GUI.color = isSelected ? new Color(1f, 0.9f, 0.3f) : Color.white;
                if (GUILayout.Button(BudgetLabels[i], buttonStyle, GUILayout.Height(20)))
                {
                    newGameBudget = BudgetPresets[i];
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            // Slider livre para ajuste fino
            newGameBudget = (long)(GUILayout.HorizontalSlider(newGameBudget, 50000, 10000000) / 10000) * 10000;
            GUILayout.Space(8);

            // 4. Botão Iniciar Partida
            if (GUILayout.Button("▶ Iniciar & Desembarcar na Região", primaryButtonStyle, GUILayout.Height(32)))
            {
                RegionSaveData save = null;
                if (SaveManager.Instance != null)
                {
                    save = SaveManager.Instance.CreateNewSave(selectedRegion, newGameSaveName, newGameBudget, selectedCareer);
                    SaveManager.Instance.ActiveSave = save;
                }
                currentCardMode = CardMode.RegionDetails;
                hasSelection = false;
                lastCardRect = Rect.zero;

                // Aciona o mergulho cinemático da câmera orbital antes de abrir a tela de carregamento
                OrbitCameraController orbitCam = mainCamera != null ? mainCamera.GetComponent<OrbitCameraController>() : null;
                if (orbitCam != null && selectedWorldPoint != Vector3.zero)
                {
                    orbitCam.DiveTowardsPoint(selectedWorldPoint, 1.8f, () =>
                    {
                        if (LoadingScreenController.Instance != null)
                        {
                            LoadingScreenController.Instance.Show(selectedRegion, save, () =>
                            {
                                SceneManager.LoadScene("RegionalSandboxScene");
                            });
                        }
                    });
                }
                else
                {
                    if (LoadingScreenController.Instance != null)
                    {
                        LoadingScreenController.Instance.Show(selectedRegion, save, () =>
                        {
                            SceneManager.LoadScene("RegionalSandboxScene");
                        });
                    }
                }
            }

            GUILayout.Space(4);
            if (GUILayout.Button("↩ Voltar aos Detalhes", buttonStyle, GUILayout.Height(22)))
            {
                currentCardMode = CardMode.RegionDetails;
            }
        }

        private void DrawLoadGameUI()
        {
            GUILayout.Label("💾 Carregar Partida Salva", headerTitleStyle);
            GUILayout.Label($"Região: {selectedRegion.name} ({selectedRegion.country})", headerSubtitleStyle);
            GUILayout.Space(6);

            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            List<RegionSaveData> saves = SaveManager.Instance != null ?
                SaveManager.Instance.GetSavesForRegion(selectedRegion.id) : new List<RegionSaveData>();

            if (saves.Count == 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("Nenhuma partida salva encontrada para esta região.", headerSubtitleStyle);
                GUILayout.Space(20);
            }
            else
            {
                loadScrollPos = GUILayout.BeginScrollView(loadScrollPos, GUILayout.Height(200));

                foreach (var save in saves)
                {
                    // Mini card para cada save
                    GUI.color = new Color(0.08f, 0.15f, 0.28f, 0.85f);
                    GUILayout.BeginVertical(saveCardBoxStyle);
                    GUI.color = Color.white;

                    // Título do save, Carreira e Verba
                    GUILayout.BeginHorizontal();
                    string icon = save.starterCareer == StarterCareer.Farmer ? "🌾" :
                                  save.starterCareer == StarterCareer.Trucker ? "🚛" :
                                  save.starterCareer == StarterCareer.Aviator ? "🛩️" : "🎣";
                    GUILayout.Label($"{icon} {save.saveName}", labelStyle);
                    GUILayout.Label(save.GetFormattedMoney(), valueStyle);
                    GUILayout.EndHorizontal();

                    // Data e Hora
                    GUI.color = new Color(0.7f, 0.85f, 1f, 0.8f);
                    GUILayout.Label($"📅 Salvo em: {save.lastSavedDate}", headerSubtitleStyle);

                    // Status / Alterações regionais
                    string statusSummary = save.GetStatChangesSummary();
                    GUI.color = save.IsStatsModified ? new Color(1f, 0.85f, 0.4f) : new Color(0.6f, 0.7f, 0.8f);
                    GUILayout.Label(statusSummary, headerSubtitleStyle);
                    GUI.color = Color.white;

                    // Botões Carregar e Excluir
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("▶ Carregar & Entrar", primaryButtonStyle, GUILayout.Height(22)))
                    {
                        if (SaveManager.Instance != null)
                        {
                            SaveManager.Instance.ActiveSave = save;
                        }
                        notificationMessage = $"Partida '{save.saveName}' carregada! Verba: {save.GetFormattedMoney()}";
                        notificationTimer = 4.0f;
                        currentCardMode = CardMode.RegionDetails;
                        hasSelection = false;
                        lastCardRect = Rect.zero;

                        // Mergulho e transição
                        OrbitCameraController orbitCam = mainCamera != null ? mainCamera.GetComponent<OrbitCameraController>() : null;
                        if (orbitCam != null && selectedWorldPoint != Vector3.zero)
                        {
                            orbitCam.DiveTowardsPoint(selectedWorldPoint, 1.8f, () =>
                            {
                                if (LoadingScreenController.Instance != null)
                                {
                                    LoadingScreenController.Instance.Show(selectedRegion, save, () =>
                                    {
                                        SceneManager.LoadScene("RegionalSandboxScene");
                                    });
                                }
                            });
                        }
                        else
                        {
                            if (LoadingScreenController.Instance != null)
                            {
                                LoadingScreenController.Instance.Show(selectedRegion, save, () =>
                                {
                                    SceneManager.LoadScene("RegionalSandboxScene");
                                });
                            }
                        }
                    }
                    if (GUILayout.Button("🗑 Excluir", dangerButtonStyle, GUILayout.Width(70), GUILayout.Height(22)))
                    {
                        if (SaveManager.Instance != null)
                        {
                            SaveManager.Instance.DeleteSave(save.saveId);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(6);
            if (GUILayout.Button("↩ Voltar aos Detalhes", buttonStyle, GUILayout.Height(26)))
            {
                currentCardMode = CardMode.RegionDetails;
            }
        }

        private void DrawResourceBar(string title, int percent, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle, GUILayout.Width(170));
            GUILayout.Label($"{percent}%", valueStyle);
            GUILayout.EndHorizontal();

            Rect barBg = GUILayoutUtility.GetRect(240, 6);
            GUI.color = new Color(0.15f, 0.2f, 0.3f, 0.6f);
            GUI.DrawTexture(barBg, whiteTex);

            float fillWidth = (barBg.width * Mathf.Clamp01(percent / 100f));
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barBg.x, barBg.y, fillWidth, barBg.height), whiteTex);
            GUI.color = Color.white;

            GUILayout.Space(2);
        }
    }
}
