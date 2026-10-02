using System;
using UnityEngine;
using UnityEngine.Rendering;
using ProjectTerra.Gameplay;
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
    public class RegionalSandboxManager : MonoBehaviour
    {
        public static RegionalSandboxManager Instance { get; private set; }

        [Header("Dimensões Reais 1:1 do Território")]
        public float worldWidthMeters = 60000f;   // Leste-Oeste em metros reais
        public float worldLengthMeters = 80000f;  // Norte-Sul em metros reais
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
            SpawnPlayerAndVehicle();
            SetupInitialMission();
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
                    regionId = 264,
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

        #region 1. Atmosfera e Iluminação Fotorrealista HDRP

        private void BuildAtmosphereAndLighting()
        {
            var atmosObj = new GameObject("HDRP_AtmosphereSystem");
            atmosObj.AddComponent<HDRPAtmosphereController>();

            // Ajusta o alcance da câmera para visualizar o horizonte do território real
            if (Camera.main != null)
            {
                Camera.main.farClipPlane = 250000f; // 250 km de campo de visão
            }
        }

        #endregion

        #region 2. Terreno Nativo Unity & Relevo Real 1:1

        private void BuildRealisticTerrainAndHydrography()
        {
            // Carregar heightmap e metadados com as dimensões métricas 1:1 reais da região
            bool loaded = GeographicDataLoader.LoadRegionalHeightmap(activeSave.regionId, out float[,] heights, out activeHydroData);

            if (activeHydroData != null && activeHydroData.realWidthMeters > 5000)
            {
                worldWidthMeters = activeHydroData.realWidthMeters;
                worldLengthMeters = activeHydroData.realLengthMeters;
                realAreaKm2 = activeHydroData.realAreaKm2 > 0 ? activeHydroData.realAreaKm2 : (int)((worldWidthMeters * worldLengthMeters) / 1000000f);
            }
            else
            {
                // Dimensão padrão representativa de estado/província (75 km x 90 km)
                worldWidthMeters = 75000f;
                worldLengthMeters = 90000f;
                realAreaKm2 = 6750;
            }

            Debug.Log($"[RegionalSandbox] Território 1:1 Carregado: {activeSave.regionName} — {worldWidthMeters / 1000f:F1} km (Leste-Oeste) × {worldLengthMeters / 1000f:F1} km (Norte-Sul) | Área: {realAreaKm2:N0} km².");

            // Criar TerrainData na escala métrica real
            activeTerrainData = new TerrainData();
            activeTerrainData.heightmapResolution = 129;
            float elevRange = activeHydroData != null ? Mathf.Clamp(activeHydroData.elevationRange * 1.8f, 250f, 4800f) : maxElevationScale;
            activeTerrainData.size = new Vector3(worldWidthMeters, elevRange, worldLengthMeters);
            activeTerrainData.SetHeights(0, 0, heights);

            // Associar Camadas PBR (Grama, Terra, Rocha, Cascalho)
            activeTerrainData.terrainLayers = TerrainPBRFactory.CreateTerrainLayers();
            activeTerrainData.alphamapResolution = 128;
            float[,,] splat = TerrainPBRFactory.CalculateSplatmaps(activeTerrainData, activeHydroData, activeSave.arablePercent);
            activeTerrainData.SetAlphamaps(0, 0, splat);

            // Criar GameObject do Terreno centralizado na origem
            var terrainObj = Terrain.CreateTerrainGameObject(activeTerrainData);
            terrainObj.name = $"Terrain_{activeSave.regionName}_EscalaReal";
            terrainObj.transform.position = new Vector3(-worldWidthMeters * 0.5f, 0f, -worldLengthMeters * 0.5f);
            activeTerrain = terrainObj.GetComponent<Terrain>();
            activeTerrain.drawTreesAndFoliage = true;
            activeTerrain.heightmapPixelError = 5;

            // Criar corpos d'água de acordo com os dados hidrográficos
            BuildHydrographySurfaces();
        }

        private void BuildHydrographySurfaces()
        {
            bool isCoastal = (activeHydroData != null && activeHydroData.isCoastal) || activeSave.waterPercent >= 35 || activeSave.starterCareer == StarterCareer.Fisherman;

            if (isCoastal)
            {
                // Bacia marítima oceânica na costa real
                var oceanObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                oceanObj.name = "Water_OceanBasin";
                float seaY = activeHydroData != null ? activeHydroData.seaLevelNormalized * activeTerrainData.size.y + 0.5f : 1.5f;
                oceanObj.transform.position = new Vector3(0f, seaY, -worldLengthMeters * 0.38f);
                oceanObj.transform.localScale = new Vector3(worldWidthMeters / 10f, 1f, (worldLengthMeters * 0.35f) / 10f);
                var rend = oceanObj.GetComponent<Renderer>();
                rend.material.color = new Color(0.1f, 0.32f, 0.52f, 0.88f);
            }
            else if (activeSave.waterPercent > 15)
            {
                // Grande lago / represa hidroelétrica no vale interior
                var lakeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lakeObj.name = "Water_InlandLake";
                Vector3 lakePos = new Vector3(worldWidthMeters * 0.15f, 0f, worldLengthMeters * 0.1f);
                lakePos.y = GetTerrainHeight(lakePos) + 0.4f;
                lakeObj.transform.position = lakePos;
                lakeObj.transform.localScale = new Vector3(1800f, 0.2f, 1200f);
                Destroy(lakeObj.GetComponent<Collider>());
                lakeObj.GetComponent<Renderer>().material.color = new Color(0.12f, 0.38f, 0.48f, 0.88f);
            }
        }

        public float GetTerrainHeight(Vector3 worldPos)
        {
            if (activeTerrain != null)
            {
                return activeTerrain.SampleHeight(worldPos) + activeTerrain.transform.position.y;
            }
            return 0f;
        }

        #endregion

        #region 3. Construção da Cidade Inicial & Malha Viária

        private void BuildStarterTown()
        {
            var townRoot = new GameObject("Town_CidadeInicial");
            townCenterPosition.y = GetTerrainHeight(townCenterPosition);
            townRoot.transform.position = townCenterPosition;

            // Malha viária principal (Avenida Central de 1.2km)
            CreateRoad(townRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(30f, 0.2f, 1200f), "Avenida Central Asfaltada");
            CreateRoad(townRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(1200f, 0.2f, 30f), "Rua Comercial Transversal");

            // Edifício 1: Cooperativa Agrícola & Silos Regionais
            deliveryMarketPosition = townCenterPosition + new Vector3(80f, 0f, 120f);
            deliveryMarketPosition.y = GetTerrainHeight(deliveryMarketPosition);
            CreateBuilding(townRoot.transform, deliveryMarketPosition, new Vector3(38f, 22f, 48f), new Color(0.75f, 0.78f, 0.82f), "🏢 Cooperativa & Silos Centrais");

            for (int i = 0; i < 4; i++)
            {
                Vector3 siloPos = deliveryMarketPosition + new Vector3(32f, 0f, -25f + i * 18f);
                siloPos.y = GetTerrainHeight(siloPos);
                var silo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                silo.name = $"Silo_Graos_{i + 1}";
                silo.transform.SetParent(townRoot.transform);
                silo.transform.position = siloPos + Vector3.up * 14f;
                silo.transform.localScale = new Vector3(14f, 14f, 14f);
                silo.GetComponent<Renderer>().material.color = new Color(0.85f, 0.87f, 0.9f);
            }

            CreateDeliveryTriggerZone(deliveryMarketPosition + new Vector3(-24f, 0f, 0f));

            // Edifício 2: Centro Administrativo / Prefeitura
            Vector3 prefPos = townCenterPosition + new Vector3(-90f, 0f, 120f);
            prefPos.y = GetTerrainHeight(prefPos);
            CreateBuilding(townRoot.transform, prefPos, new Vector3(42f, 16f, 36f), new Color(0.92f, 0.90f, 0.85f), "🏛️ Centro Administrativo & Alvarás");

            // Edifício 3: Posto Rodoviário de Combustíveis & Pátio
            Vector3 gasPos = townCenterPosition + new Vector3(-85f, 0f, -120f);
            gasPos.y = GetTerrainHeight(gasPos);
            CreateBuilding(townRoot.transform, gasPos, new Vector3(34f, 10f, 38f), new Color(0.9f, 0.35f, 0.25f), "⛽ Posto Rodoviário & Oficina");

            // Edifício 4: Armazém Geral & Mercado Central
            Vector3 mktPos = townCenterPosition + new Vector3(90f, 0f, -120f);
            mktPos.y = GetTerrainHeight(mktPos);
            CreateBuilding(townRoot.transform, mktPos, new Vector3(44f, 12f, 32f), new Color(0.6f, 0.65f, 0.7f), "🏪 Mercado Central & Armazém");
        }

        private void CreateRoad(Transform parent, Vector3 pos, Vector3 size, string name)
        {
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = name;
            road.transform.SetParent(parent);
            float h = GetTerrainHeight(pos);
            road.transform.position = new Vector3(pos.x, h + 0.1f, pos.z);
            road.transform.localScale = size;
            var rend = road.GetComponent<Renderer>();
            rend.material.color = new Color(0.18f, 0.18f, 0.20f);
        }

        private void CreateBuilding(Transform parent, Vector3 pos, Vector3 size, Color color, string name)
        {
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = name;
            building.transform.SetParent(parent);
            float h = GetTerrainHeight(pos);
            building.transform.position = new Vector3(pos.x, h + (size.y * 0.5f), pos.z);
            building.transform.localScale = size;
            var rend = building.GetComponent<Renderer>();
            rend.material.color = color;
        }

        private void CreateDeliveryTriggerZone(Vector3 pos)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Trigger_PontoDeEntrega";
            float h = GetTerrainHeight(pos);
            marker.transform.position = new Vector3(pos.x, h + 0.1f, pos.z);
            marker.transform.localScale = new Vector3(25f, 0.05f, 25f);
            var rend = marker.GetComponent<Renderer>();
            rend.material.color = new Color(0.2f, 1.0f, 0.4f, 0.6f);
            Destroy(marker.GetComponent<Collider>());
        }

        #endregion

        #region 4. Infraestrutura da Carreira na Escala 1:1

        private void BuildStarterCareerInfrastructure()
        {
            StarterCareer career = activeSave.starterCareer;

            switch (career)
            {
                case StarterCareer.Farmer:
                    BuildFarmerBase();
                    break;
                case StarterCareer.Trucker:
                    BuildTruckerBase();
                    break;
                case StarterCareer.Aviator:
                    BuildAviatorBase();
                    break;
                case StarterCareer.Fisherman:
                    BuildFishermanBase();
                    break;
            }
        }

        private void BuildFarmerBase()
        {
            // Fazenda rural a 2.5 km a noroeste da cidade
            starterBasePosition = townCenterPosition + new Vector3(-1800f, 0f, 1600f);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var farmRoot = new GameObject("Base_FazendaInicial");
            farmRoot.transform.position = starterBasePosition;

            CreateRoad(farmRoot.transform, new Vector3(900f, 0f, -800f), new Vector3(2000f, 0.15f, 20f), "Estrada Rural Intermunicipal");
            CreateBuilding(farmRoot.transform, starterBasePosition + new Vector3(-35f, 0f, 35f), new Vector3(24f, 9f, 20f), new Color(0.85f, 0.75f, 0.65f), "🏡 Sede da Fazenda");
            CreateBuilding(farmRoot.transform, starterBasePosition + new Vector3(35f, 0f, 40f), new Vector3(36f, 14f, 26f), new Color(0.72f, 0.22f, 0.18f), "🚜 Galpão de Tratores e Colheitadeiras");

            // Lavoura agrícola de 800m x 600m
            var cropField = GameObject.CreatePrimitive(PrimitiveType.Plane);
            cropField.name = "Lavoura_Safra_EscalaReal";
            cropField.transform.SetParent(farmRoot.transform);
            float hField = GetTerrainHeight(starterBasePosition + new Vector3(0f, 0f, -400f));
            cropField.transform.position = new Vector3(starterBasePosition.x, hField + 0.12f, starterBasePosition.z - 400f);
            cropField.transform.localScale = new Vector3(80f, 1f, 60f); // 800m x 600m
            cropField.GetComponent<Renderer>().material.color = new Color(0.48f, 0.38f, 0.18f);

            Vector3 tratorPos = starterBasePosition + new Vector3(10f, 0f, 10f);
            tratorPos.y = GetTerrainHeight(tratorPos) + 0.8f;
            starterVehicle = VehicleBuilder.CreateTractor(tratorPos);
            starterVehicle.LoadCargo("Soja Colhida", 2000f);
        }

        private void BuildTruckerBase()
        {
            // Pátio intermodal logístico a 3.2 km a leste da cidade
            starterBasePosition = townCenterPosition + new Vector3(3200f, 0f, -1200f);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var depotRoot = new GameObject("Base_GalpaoLogistica");
            depotRoot.transform.position = starterBasePosition;

            CreateRoad(depotRoot.transform, new Vector3(-1600f, 0f, 600f), new Vector3(3400f, 0.15f, 28f), "Rodovia Estadual Principal");
            CreateBuilding(depotRoot.transform, starterBasePosition + new Vector3(0f, 0f, 50f), new Vector3(65f, 14f, 35f), new Color(0.35f, 0.45f, 0.6f), "🏭 Galpão de Cargas & Docas Intermodais");

            Vector3 truckPos = starterBasePosition;
            truckPos.y = GetTerrainHeight(truckPos) + 0.9f;
            starterVehicle = VehicleBuilder.CreateTruck(truckPos);
            starterVehicle.LoadCargo("Peças Industriais", 4500f);
        }

        private void BuildAviatorBase()
        {
            // Aeródromo Regional a 5 km ao norte da cidade
            starterBasePosition = townCenterPosition + new Vector3(2500f, 0f, 4500f);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var airfieldRoot = new GameObject("Base_AerodromoRegional");
            airfieldRoot.transform.position = starterBasePosition;

            // Pista de Pouso Asfaltada Real de 1.800 metros de extensão!
            CreateRoad(airfieldRoot.transform, starterBasePosition, new Vector3(45f, 0.15f, 1800f), "Pista de Pouso Regional 09/27 (1800m)");
            CreateBuilding(airfieldRoot.transform, starterBasePosition + new Vector3(60f, 0f, -120f), new Vector3(45f, 15f, 38f), new Color(0.75f, 0.8f, 0.85f), "✈️ Hangar Executivo & Carga Aérea");

            Vector3 planePos = starterBasePosition + new Vector3(0f, 0f, -750f);
            planePos.y = GetTerrainHeight(planePos) + 1.2f;
            starterVehicle = VehicleBuilder.CreatePlane(planePos);
            starterVehicle.LoadCargo("Malote Postal Expresso", 350f);
        }

        private void BuildFishermanBase()
        {
            // Porto Marítimo localizado na costa real sul do território
            float coastZ = -worldLengthMeters * 0.36f;
            starterBasePosition = new Vector3(0f, 0f, coastZ);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var pierRoot = new GameObject("Base_PortoPesqueiro");
            pierRoot.transform.position = starterBasePosition;

            var pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Trapiche_Madeira_1_1";
            pier.transform.SetParent(pierRoot.transform);
            float pierY = Mathf.Max(0.8f, starterBasePosition.y);
            pier.transform.position = new Vector3(0f, pierY, coastZ - 75f);
            pier.transform.localScale = new Vector3(25f, 1.2f, 180f);
            pier.GetComponent<Renderer>().material.color = new Color(0.48f, 0.35f, 0.25f);

            CreateBuilding(pierRoot.transform, starterBasePosition + new Vector3(40f, 0f, 25f), new Vector3(36f, 11f, 28f), new Color(0.45f, 0.65f, 0.75f), "🐟 Entreposto Portuário & Mercado de Pescados");

            Vector3 boatPos = new Vector3(-20f, 0.5f, coastZ - 100f);
            starterVehicle = VehicleBuilder.CreateBoat(boatPos);
            starterVehicle.LoadCargo("Pescado Fresco", 1800f);
        }

        #endregion

        #region 5. Spawn do Jogador e Missões

        private void SpawnPlayerAndVehicle()
        {
            var playerObj = new GameObject("PlayerCharacter");
            float playerY = GetTerrainHeight(starterBasePosition + new Vector3(5f, 0f, 5f)) + 1.0f;
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
