using System;
using UnityEngine;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet;
using ProjectTerra.Planet.TerrainStreaming;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Orquestrador mestre da Cena Sandbox Regional em Escala Real 1:1.
    /// Constrói o terreno nativo da Unity esculpido nas dimensões métricas exatas do estado ou país (ex: 60km de Luxemburgo ou 500km do Paraná),
    /// aplica multi-camadas de texturas PBR (grama, terra, rochas, cascalho), gera hidrografia real (rios e oceanos),
    /// posiciona a Cidade Inicial, ergue a infraestrutura da carreira do jogador e gerencia o Floating Origin para precisão infinita.
    /// </summary>
    public partial class RegionalSandboxManager : MonoBehaviour
    {
        public static RegionalSandboxManager Instance { get; private set; }

        [Header("Dimensões Reais 1:1 do Território")]
        public float worldWidthMeters = 60000f;   // Leste-Oeste do setor sandbox em metros
        public float worldLengthMeters = 60000f;  // Norte-Sul do setor sandbox em metros
        public float territoryWidthMeters = 886000f;  // Dimensão geográfica total do estado/país
        public float territoryLengthMeters = 606000f;
        public int realAreaKm2 = 4800;
        public float maxElevationScale = 450f;

        [Header("Referências")]
        public Terrain activeTerrain;
        public TerrainData activeTerrainData;
        public RegionalHydroData activeHydroData;

        [Header("Locais Chave (Coordenadas Reais)")]
        public Vector3 townCenterPosition = Vector3.zero;
        public Vector3 starterBasePosition;
        public Vector3 deliveryMarketPosition;

        [Header("Veículo e Jogador")]
        public VehicleController starterVehicle;
        public PlayerCharacterController playerCharacter;

        private RegionSaveData activeSave;
        public RegionData activeRegionData { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            EnsureActiveSave();
            EnsureFloatingOrigin();
            BuildAtmosphereAndLighting();
            BuildRealisticTerrainAndHydrography();
            BuildStarterTown();
            BuildStarterCareerInfrastructure();
            SceneObjectSpawner.PopulateNatureAndFauna(activeTerrain, activeSave, starterBasePosition, townCenterPosition);
            SpawnPlayerAndVehicle();
            EnsureVehicleManager();
            EnsureCommandConsole();
            SetupInitialMission();
        }

        private void EnsureVehicleManager()
        {
            if (FindAnyObjectByType<VehicleManager>() == null)
            {
                var vmObj = new GameObject("VehicleManager");
                var vm = vmObj.AddComponent<VehicleManager>();
                if (starterVehicle != null)
                {
                    vm.RegisterVehicle(starterVehicle);
                }
            }
        }

        private void EnsureCommandConsole()
        {
            if (FindAnyObjectByType<InGameCommandConsole>() == null)
            {
                var consoleObj = new GameObject("InGameCommandConsole");
                consoleObj.AddComponent<InGameCommandConsole>();
            }
        }

        private void EnsureActiveSave()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                activeSave = SaveManager.Instance.ActiveSave;
            }
            else
            {
                activeSave = new RegionSaveData
                {
                    saveId = "demo_editor",
                    saveName = "Fazenda Esperança",
                    regionId = 1980,
                    regionName = "São Paulo",
                    countryName = "Brasil",
                    regionType = "Estado",
                    starterCareer = StarterCareer.Farmer,
                    startingMoney = 500000,
                    currentMoney = 500000,
                    forestPercent = 45,
                    mineralsPercent = 30,
                    arablePercent = 65,
                    waterPercent = 40,
                    reputation = 100
                };
            }
        }

        private void EnsureFloatingOrigin()
        {
            if (FindAnyObjectByType<FloatingOrigin>() == null)
            {
                var foObj = new GameObject("FloatingOriginManager");
                foObj.AddComponent<FloatingOrigin>();
                Debug.Log("[RegionalSandbox] Sistema FloatingOrigin ativado para escala real 1:1 contínua.");
            }
        }

        #region Atmosfera e Iluminação Fotorrealista HDRP

        private void BuildAtmosphereAndLighting()
        {
            // Destruir a câmera estática da cena IMEDIATAMENTE (DestroyImmediate) ANTES de criar o jogador.
            // Isso garante que Camera.main == null quando PlayerCharacterController.Start() rodar,
            // forçando-o a criar e controlar sua própria SandboxMainCamera corretamente.
            // Nota: DestroyImmediate é seguro aqui pois estamos em Start(), fora do ciclo de renderização.
            var sceneStaticCamera = Camera.main;
            if (sceneStaticCamera != null)
            {
                Debug.Log($"[RegionalSandbox] Destruindo câmera estática '{sceneStaticCamera.gameObject.name}' (pos: {sceneStaticCamera.transform.position}) para liberar controle de câmera ao PlayerCharacterController.");
                DestroyImmediate(sceneStaticCamera.gameObject);
            }

            var atmosObj = new GameObject("HDRP_AtmosphereSystem");
            atmosObj.AddComponent<HDRPAtmosphereController>();
        }

        #endregion

        #region Auxiliar de Altura do Terreno

        public float GetTerrainHeight(Vector3 worldPos)
        {
            if (activeTerrain != null)
            {
                return activeTerrain.SampleHeight(worldPos) + activeTerrain.transform.position.y;
            }
            return 0f;
        }

        #endregion

        #region Spawn do Jogador e Missões

        private void SpawnPlayerAndVehicle()
        {
            var playerObj = new GameObject("PlayerCharacter");
            float playerY = GetTerrainHeight(starterBasePosition + new Vector3(5f, 0f, 5f)) + 1.2f;
            playerObj.transform.position = new Vector3(starterBasePosition.x + 5f, playerY, starterBasePosition.z + 5f);

            var charController = playerObj.AddComponent<CharacterController>();
            charController.height = 1.8f;
            charController.radius = 0.4f;
            charController.center = new Vector3(0f, 0.9f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Visual_Corpo";
            body.transform.SetParent(playerObj.transform);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().material.color = new Color(0.15f, 0.45f, 0.85f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Visual_Capacete";
            head.transform.SetParent(playerObj.transform);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0.1f);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().material.color = new Color(0.95f, 0.85f, 0.2f);

            var camPoint = new GameObject("CameraFollowPoint");
            camPoint.transform.SetParent(playerObj.transform);
            camPoint.transform.localPosition = new Vector3(0f, 1.65f, 0.15f);

            playerCharacter = playerObj.AddComponent<PlayerCharacterController>();
            playerCharacter.cameraFollowPoint = camPoint.transform;

            var hudObj = new GameObject("SandboxHUD");
            hudObj.AddComponent<SandboxHUD>();
        }

        private void SetupInitialMission()
        {
            var missionObj = new GameObject("MissionManager");
            var mm = missionObj.AddComponent<MissionManager>();
            mm.SetupInitialContract(activeSave.starterCareer, starterBasePosition, deliveryMarketPosition);
        }

        #endregion
    }
}
