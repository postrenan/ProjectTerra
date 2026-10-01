using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Controla a seleção interativa de Estados/Províncias e Países ao clicar no globo.
    /// Interrompe a rotação do planeta e exibe um balão com estatísticas regionais.
    /// </summary>
    public class PlanetInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CubeSpherePlanet planet;
        [SerializeField] private Camera mainCamera;

        [Header("Selected State")]
        [SerializeField] private bool hasSelection = false;
        [SerializeField] private RegionData selectedRegion;
        [SerializeField] private Vector3 selectedLocalNormal;
        [SerializeField] private Vector3 selectedWorldPoint;

        private RegionDatabase database;
        private byte[] regionIdMap;
        private const int MapWidth = 2048;
        private const int MapHeight = 1024;
        private bool isDatabaseLoaded = false;

        private Rect lastCardRect;
        private Vector2 mouseDownPos;
        private bool isMouseDownOnCard;

        // UI Styling
        private GUIStyle cardStyle;
        private GUIStyle headerTitleStyle;
        private GUIStyle headerSubtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle buttonStyle;
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

            LoadDatabase();
            CreateTextures();
        }

        private void CreateTextures()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }

        private void LoadDatabase()
        {
            string dbPath = Path.Combine(Application.streamingAssetsPath, "regions_database.json");
            string binPath = Path.Combine(Application.streamingAssetsPath, "region_id_map.bin");

            if (File.Exists(dbPath))
            {
                try
                {
                    string json = File.ReadAllText(dbPath);
                    database = JsonUtility.FromJson<RegionDatabase>(json);
                    isDatabaseLoaded = database != null && database.regions.Count > 0;
                    Debug.Log($"[PlanetInteraction] Base de dados carregada com {database.regions.Count} regiões.");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar regions_database.json: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlanetInteraction] Arquivo de regiões não encontrado em {dbPath}");
            }

            if (File.Exists(binPath))
            {
                try
                {
                    regionIdMap = File.ReadAllBytes(binPath);
                    Debug.Log($"[PlanetInteraction] Mapa binário de IDs geográficos carregado ({regionIdMap.Length} bytes).");
                }
                catch (System.Exception ex)
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
                    // Tolerância confortável para clique (não arrasto)
                    if (dragDist < 12.0f)
                    {
                        TryRaycastPlanet(Input.mousePosition);
                    }
                }
                isMouseDownOnCard = false;
            }

            // Atualiza posição mundial do ponto selecionado acompanhando a rotação do planeta
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
                double t = -b - System.Math.Sqrt(discriminant);
                if (t > 0.0)
                {
                    Vector3 worldHit = ray.origin + ray.direction * (float)t;
                    Vector3 localHit = planet.transform.InverseTransformPoint(worldHit);
                    selectedLocalNormal = localHit.normalized;
                    selectedWorldPoint = worldHit;

                    // Converter coordenadas locais para Latitude e Longitude (sincronizado com a projeção da Terra)
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

            // 1. Amostragem direta de altíssima precisão no mapa binário de IDs geográficos (2048 x 1024)
            if (regionIdMap != null && regionIdMap.Length == MapWidth * MapHeight * 2)
            {
                int px = Mathf.Clamp((int)((lon + 180.0f) / 360.0f * (MapWidth - 1)), 0, MapWidth - 1);
                int py = Mathf.Clamp((int)((90.0f - lat) / 180.0f * (MapHeight - 1)), 0, MapHeight - 1);

                ushort id = GetIdAt(px, py);

                // Se cair em água rasa / costa costeira com id 0, faz uma pequena tolerância litorânea de 2 pixels
                if (id == 0)
                {
                    int[] offsets = { -1, 1, -2, 2 };
                    foreach (int ox in offsets)
                    {
                        foreach (int oy in offsets)
                        {
                            int nx = Mathf.Clamp(px + ox, 0, MapWidth - 1);
                            int ny = Mathf.Clamp(py + oy, 0, MapHeight - 1);
                            ushort neighborId = GetIdAt(nx, ny);
                            if (neighborId > 0 && database != null && neighborId <= database.regions.Count)
                            {
                                float nLon = (nx / (float)(MapWidth - 1)) * 360.0f - 180.0f;
                                float nLat = 90.0f - (ny / (float)(MapHeight - 1)) * 180.0f;
                                float dLat = lat - nLat;
                                float dLon = lon - nLon;
                                // Só aceita se estiver a menos de ~0.2 graus da borda litorânea
                                if (dLat * dLat + dLon * dLon < 0.05f)
                                {
                                    id = neighborId;
                                    break;
                                }
                            }
                        }
                        if (id > 0) break;
                    }
                }

                if (id > 0 && database != null && id <= database.regions.Count)
                {
                    match = database.regions[id - 1];
                }
            }

            // 2. Se o ID for 0 (ou nenhuma terra encontrada), é 100% GARANTIDO OCEANO!
            if (match == null)
            {
                selectedRegion = GenerateOceanRegion(lat, lon);
            }
            else
            {
                selectedRegion = match;
            }

            hasSelection = true;
            Debug.Log($"[PlanetInteraction] Região Selecionada: {selectedRegion.name} ({selectedRegion.country}) - Lat: {lat:F2}°, Lon: {lon:F2}°");

            // Pausa a rotação do planeta conforme solicitado pelo usuário
            if (planet != null)
            {
                planet.SetRotationPaused(true);
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

        private RegionData GenerateFallbackRegion(float lat, float lon)
        {
            return new RegionData
            {
                name = $"Região ({lat:F1}°, {lon:F1}°)",
                country = "Planeta Terra",
                type = "Área Geográfica",
                centerLat = lat,
                centerLon = lon,
                forestPercent = 45,
                mineralsPercent = 50,
                arablePercent = 35,
                waterPercent = 60
            };
        }

        public void ResumeRotation()
        {
            hasSelection = false;
            lastCardRect = Rect.zero;
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
        }

        private void OnGUI()
        {
            if (!hasSelection || selectedRegion == null || mainCamera == null)
            {
                lastCardRect = Rect.zero;
                return;
            }

            // Verificar se o ponto selecionado está virado para a câmera (não está do outro lado do globo)
            Vector3 camToPoint = selectedWorldPoint - mainCamera.transform.position;
            Vector3 pointNormal = selectedWorldPoint.normalized;
            if (Vector3.Dot(camToPoint.normalized, pointNormal) > 0.1f)
            {
                // Ponto está oculto atrás da curvatura da Terra
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

            // Dimensões do balão de informações
            float width = 310f;
            float height = 240f;
            float x = Mathf.Clamp(screenPos.x + 25f, 20f, Screen.width - width - 20f);
            float y = Mathf.Clamp(Screen.height - screenPos.y - height * 0.5f, 20f, Screen.height - height - 20f);

            Rect cardRect = new Rect(x, y, width, height);
            lastCardRect = cardRect;

            // 1. Sombra e Fundo translúcido (Glassmorphism azul escuro espacial)
            GUI.color = new Color(0.02f, 0.05f, 0.12f, 0.92f);
            GUI.DrawTexture(cardRect, whiteTex);

            // 2. Borda externa suave
            GUI.color = new Color(0.3f, 0.65f, 1.0f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, width, 2), whiteTex);
            GUI.DrawTexture(new Rect(x, y + height - 2, width, 2), whiteTex);
            GUI.DrawTexture(new Rect(x, y, 2, height), whiteTex);
            GUI.DrawTexture(new Rect(x + width - 2, y, 2, height), whiteTex);

            // 3. Indicador de Pino na posição do clique
            GUI.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            GUI.DrawTexture(new Rect(screenPos.x - 4, Screen.height - screenPos.y - 4, 8, 8), whiteTex);

            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(x + 14, y + 10, width - 28, height - 20));

            // Título: Estado / Província e País
            GUILayout.Label(selectedRegion.name, headerTitleStyle);
            GUILayout.Label($"{selectedRegion.country} • {selectedRegion.type}", headerSubtitleStyle);
            GUILayout.Space(6);

            // Linha divisória
            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(6);

            // Barras de Recursos com percentuais
            DrawResourceBar("🌲 Cobertura Florestal", selectedRegion.forestPercent, new Color(0.2f, 0.85f, 0.3f));
            DrawResourceBar("⛏️ Potencial Mineral", selectedRegion.mineralsPercent, new Color(1.0f, 0.65f, 0.2f));
            DrawResourceBar("🌾 Terras Aráveis", selectedRegion.arablePercent, new Color(0.95f, 0.85f, 0.25f));
            DrawResourceBar("💧 Recursos Hídricos", selectedRegion.waterPercent, new Color(0.25f, 0.7f, 1.0f));

            GUILayout.Space(8);

            // Botão para retomar rotação do planeta
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("▶ Retomar Rotação", buttonStyle, GUILayout.Height(26)))
            {
                ResumeRotation();
            }
            if (GUILayout.Button("✖ Fechar", buttonStyle, GUILayout.Width(70), GUILayout.Height(26)))
            {
                hasSelection = false;
                lastCardRect = Rect.zero;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawResourceBar(string title, int percent, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, labelStyle, GUILayout.Width(170));
            GUILayout.Label($"{percent}%", valueStyle);
            GUILayout.EndHorizontal();

            Rect barBg = GUILayoutUtility.GetRect(240, 6);
            // Fundo escuro da barra
            GUI.color = new Color(0.15f, 0.2f, 0.3f, 0.6f);
            GUI.DrawTexture(barBg, whiteTex);

            // Preenchimento colorido da barra
            float fillWidth = (barBg.width * Mathf.Clamp01(percent / 100f));
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barBg.x, barBg.y, fillWidth, barBg.height), whiteTex);
            GUI.color = Color.white;

            GUILayout.Space(2);
        }
    }
}
