using System;
using UnityEngine;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;
using ProjectTerra.Cameras;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Controla a seleção interativa de Estados/Províncias e Países ao clicar no globo.
    /// Interrompe a rotação do planeta, destaca o terreno selecionado via shader e
    /// exibe o painel de estatísticas com opções completas de Novo Jogo e Carregar Jogo.
    /// </summary>
    public partial class PlanetInteractionController : MonoBehaviour
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
        private GUIStyle recentBtnStyle;
        private Texture2D whiteTex;

        // Estado do Modal Retrátil de Saves Recentes
        private bool isRecentSavesOpen = false;
        private float recentSavesAnimProgress = 0f;
        private Rect recentSavesButtonRect;
        private Rect recentSavesModalRect;
        private string recentSavesSearchQuery = "";
        private Vector2 recentSavesScrollPos = Vector2.zero;
        private string saveIdConfirmDelete = null;
        private float deleteConfirmTimer = 0f;

        public static PlanetInteractionController Instance { get; private set; }

        public bool IsPointerOverUI()
        {
            Vector2 guiMouse = new Vector2(TerraInput.MousePosition.x, Screen.height - TerraInput.MousePosition.y);
            if (hasSelection && lastCardRect.Contains(guiMouse)) return true;
            if (recentSavesButtonRect.Contains(guiMouse)) return true;
            if (recentSavesAnimProgress > 0.01f && recentSavesModalRect.Contains(guiMouse)) return true;
            return false;
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

        private void Update()
        {
            if (notificationTimer > 0f)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0f) notificationMessage = "";
            }

            if (deleteConfirmTimer > 0f)
            {
                deleteConfirmTimer -= Time.deltaTime;
                if (deleteConfirmTimer <= 0f) saveIdConfirmDelete = null;
            }

            // Animação de transição suave do modal retrátil
            float targetProgress = isRecentSavesOpen ? 1.0f : 0.0f;
            recentSavesAnimProgress = Mathf.MoveTowards(recentSavesAnimProgress, targetProgress, Time.deltaTime * 6.0f);

            if (TerraInput.GetMouseButtonDown(0))
            {
                Vector2 guiMouse = new Vector2(TerraInput.MousePosition.x, Screen.height - TerraInput.MousePosition.y);
                isMouseDownOnCard = (hasSelection && lastCardRect.Contains(guiMouse)) ||
                                    recentSavesButtonRect.Contains(guiMouse) ||
                                    (recentSavesAnimProgress > 0.01f && recentSavesModalRect.Contains(guiMouse));
                mouseDownPos = TerraInput.MousePosition;
            }

            if (TerraInput.GetMouseButtonUp(0))
            {
                if (!isMouseDownOnCard)
                {
                    float dragDist = Vector2.Distance(mouseDownPos, TerraInput.MousePosition);
                    if (dragDist < 12.0f)
                    {
                        TryRaycastPlanet(TerraInput.MousePosition);
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

        public void ToggleRecentSavesModal()
        {
            isRecentSavesOpen = !isRecentSavesOpen;
            if (isRecentSavesOpen)
            {
                saveIdConfirmDelete = null;
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.LoadAllSaves();
                }
            }
        }

        public void CloseRecentSavesModal()
        {
            isRecentSavesOpen = false;
            saveIdConfirmDelete = null;
        }

        public RegionData GetRegionById(int id)
        {
            if (!isDatabaseLoaded || database == null || database.regions.Count == 0)
            {
                LoadDatabase();
            }
            if (database == null || database.regions == null) return null;

            if (id > 0 && id <= database.regions.Count)
            {
                var r = database.regions[id - 1];
                if (r.id == id) return r;
            }
            return database.regions.Find(r => r.id == id);
        }

        public void FocusAndSelectRegion(int regionId, bool openDetails = true)
        {
            var region = GetRegionById(regionId);
            if (region == null)
            {
                Debug.LogWarning($"[PlanetInteraction] Região com ID {regionId} não encontrada no banco de dados.");
                return;
            }

            SelectRegion(region, openDetails);
        }

        public void SelectRegion(RegionData region, bool openDetails = true)
        {
            if (region == null) return;

            selectedRegion = region;
            float latRad = region.centerLat * Mathf.Deg2Rad;
            float lonRad = region.centerLon * Mathf.Deg2Rad;
            float cosLat = Mathf.Cos(latRad);
            selectedLocalNormal = new Vector3(
                cosLat * Mathf.Sin(lonRad),
                Mathf.Sin(latRad),
                -cosLat * Mathf.Cos(lonRad)
            ).normalized;

            if (planet == null)
            {
                planet = CubeSpherePlanet.Instance ?? FindAnyObjectByType<CubeSpherePlanet>();
            }

            if (planet != null)
            {
                selectedWorldPoint = planet.transform.TransformPoint(selectedLocalNormal * (float)planet.PlanetRadius);
                planet.SetRotationPaused(true);
            }

            SetTerrainHighlight(region.id);

            currentCardMode = CardMode.RegionDetails;
            newGameSaveName = $"Governo de {selectedRegion.name}";
            newGameBudget = 500000;
            notificationMessage = "";
            hasSelection = openDetails;

            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null && selectedWorldPoint != Vector3.zero)
            {
                var orbitCam = mainCamera.GetComponent<OrbitCameraController>();
                if (orbitCam != null)
                {
                    orbitCam.FocusOnPoint(selectedWorldPoint, 3200000.0);
                }
            }
        }

        public void LoadSaveGame(RegionSaveData save)
        {
            if (save == null) return;

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ActiveSave = save;
            }

            RegionData region = GetRegionById(save.regionId);
            if (region == null)
            {
                region = new RegionData
                {
                    id = save.regionId,
                    name = save.regionName,
                    country = save.countryName,
                    type = save.regionType,
                    forestPercent = save.forestPercent,
                    mineralsPercent = save.mineralsPercent,
                    arablePercent = save.arablePercent,
                    waterPercent = save.waterPercent
                };
            }

            hasSelection = false;
            lastCardRect = Rect.zero;
            isRecentSavesOpen = false;

            if (planet == null) planet = CubeSpherePlanet.Instance ?? FindAnyObjectByType<CubeSpherePlanet>();
            if (mainCamera == null) mainCamera = Camera.main;

            float latRad = region.centerLat * Mathf.Deg2Rad;
            float lonRad = region.centerLon * Mathf.Deg2Rad;
            float cosLat = Mathf.Cos(latRad);
            Vector3 localNorm = new Vector3(
                cosLat * Mathf.Sin(lonRad),
                Mathf.Sin(latRad),
                -cosLat * Mathf.Cos(lonRad)
            ).normalized;
            Vector3 targetWorld = planet != null ? planet.transform.TransformPoint(localNorm * (float)planet.PlanetRadius) : Vector3.zero;

            OrbitCameraController orbitCam = mainCamera != null ? mainCamera.GetComponent<OrbitCameraController>() : null;
            if (orbitCam != null && targetWorld != Vector3.zero)
            {
                orbitCam.DiveTowardsPoint(targetWorld, 1.8f, () =>
                {
                    if (LoadingScreenController.Instance != null)
                    {
                        LoadingScreenController.Instance.Show(region, save, () =>
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene("RegionalSandboxScene");
                        });
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("RegionalSandboxScene");
                    }
                });
            }
            else
            {
                if (LoadingScreenController.Instance != null)
                {
                    LoadingScreenController.Instance.Show(region, save, () =>
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("RegionalSandboxScene");
                    });
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("RegionalSandboxScene");
                }
            }
        }
    }
}
