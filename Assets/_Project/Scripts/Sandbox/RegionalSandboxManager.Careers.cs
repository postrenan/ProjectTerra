using UnityEngine;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    public partial class RegionalSandboxManager
    {
        #region Infraestrutura da Carreira na Escala 1:1

        private void BuildStarterCareerInfrastructure()
        {
            StarterCareer career = activeSave.starterCareer;

            // Validação geográfica da carreira: algumas carreiras exigem características regionais específicas.
            // Se a região não suporta a carreira escolhida, faz fallback com aviso claro no console.
            career = ValidateCareerForRegion(career);

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

        /// <summary>
        /// Valida se a carreira escolhida é compatível com a região atual.
        /// Retorna a carreira original se for compatível, ou uma carreira alternativa com aviso.
        /// </summary>
        private StarterCareer ValidateCareerForRegion(StarterCareer career)
        {
            if (career != StarterCareer.Fisherman) return career;

            var infra = RegionInfrastructureDatabase.GetInfrastructure(activeSave.regionId);
            bool hasCoast  = (infra != null && infra.hasCoastline) || (activeHydroData != null && activeHydroData.isCoastal);
            bool hasWater  = (infra != null && (infra.hasRivers || infra.hasLakes)) || activeSave.waterPercent > 5;

            if (!hasCoast && !hasWater)
            {
                // Região sem qualquer corpo d'água — Pescador não tem sentido aqui.
                // Exemplos de regiões problemáticas: Saara (Algeria, Líbia), Gobi, interior do Namibe,
                // platôs do Tibet, desertos da Arábia central.
                Debug.LogWarning($"[RegionalSandbox] AVISO DE CARREIRA: '{activeSave.regionName}' não possui litoral, rios ou lagos. " +
                                 $"A carreira 'Pescador' foi substituída por 'Caminhoneiro' para esta região árida/continental. " +
                                 $"Altere a carreira inicial do save para evitar este fallback.");
                return StarterCareer.Trucker;
            }

            return career;
        }

        private void BuildFarmerBase()
        {
            var infra = RegionInfrastructureDatabase.GetInfrastructure(activeSave.regionId);
            bool isHighland = infra != null && infra.environmentCategory.Contains("Andes");
            string farmTitle = isHighland ? "Cooperativa Andina de Altitude" : "Sede da Fazenda Regional";
            string cropCargo = isHighland ? "Quinoa e Tubérculos Andinos" : "Soja Colhida";

            // Fazenda rural a 2.5 km a noroeste da cidade
            starterBasePosition = townCenterPosition + new Vector3(-1800f, 0f, 1600f);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var farmRoot = new GameObject("Base_FazendaInicial");
            farmRoot.transform.position = starterBasePosition;

            CreateRoad(farmRoot.transform, new Vector3(900f, 0f, -800f), new Vector3(2000f, 0.15f, 20f), "Estrada Rural Intermunicipal");
            // [removido] sede e galpao (casas/predios) nao relevantes para a cena atual

            Vector3[] fencePosts = new Vector3[] {
                new Vector3(-400f, 0f, -100f),
                new Vector3(400f, 0f, -100f),
                new Vector3(-400f, 0f, -700f),
                new Vector3(400f, 0f, -700f)
            };
            for (int p = 0; p < fencePosts.Length; p++)
            {
                Vector3 postPos = starterBasePosition + fencePosts[p];
                postPos.y = GetTerrainHeight(postPos) + 1.5f;
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = $"Marco_Perimetro_Lavoura_{p + 1}";
                post.transform.SetParent(farmRoot.transform);
                post.transform.position = postPos;
                post.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
                post.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial(new Color(0.85f, 0.45f, 0.15f));
                Destroy(post.GetComponent<Collider>());
            }

            Vector3 tratorPos = starterBasePosition + new Vector3(10f, 0f, 10f);
            tratorPos.y = GetTerrainHeight(tratorPos) + 0.8f;
            starterVehicle = VehicleBuilder.CreateTractor(tratorPos);
            starterVehicle.LoadCargo(cropCargo, 2000f);
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
            var infra = RegionInfrastructureDatabase.GetInfrastructure(activeSave.regionId);
            string airportName = infra != null && !string.IsNullOrEmpty(infra.airportName) ? infra.airportName : "Aeródromo Regional";

            // Aeródromo Regional a 5 km ao norte da cidade
            starterBasePosition = townCenterPosition + new Vector3(2500f, 0f, 4500f);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var airfieldRoot = new GameObject($"Base_{airportName}");
            airfieldRoot.transform.position = starterBasePosition;

            // Pista de Pouso Asfaltada Real de 1.800 metros de extensão!
            CreateRoad(airfieldRoot.transform, starterBasePosition, new Vector3(45f, 0.15f, 1800f), $"Pista 09/27 - {airportName} (1800m)");
            CreateBuilding(airfieldRoot.transform, starterBasePosition + new Vector3(60f, 0f, -120f), new Vector3(45f, 15f, 38f), new Color(0.75f, 0.8f, 0.85f), $"✈️ Hangar Operacional ({airportName})");

            Vector3 planePos = starterBasePosition + new Vector3(0f, 0f, -750f);
            planePos.y = GetTerrainHeight(planePos) + 1.2f;
            starterVehicle = VehicleBuilder.CreatePlane(planePos);
            starterVehicle.LoadCargo("Malote Postal Expresso", 350f);
        }

        private void BuildFishermanBase()
        {
            var infra = RegionInfrastructureDatabase.GetInfrastructure(activeSave.regionId);
            bool isCoastal = (infra != null && infra.hasCoastline) || (activeHydroData != null && activeHydroData.isCoastal);
            string portTitle = infra != null && !string.IsNullOrEmpty(infra.portName) ? infra.portName : (isCoastal ? "Porto Marítimo Litorâneo" : "Entreposto Fluvial");

            // Se for costeiro, ancora no litoral sul; se for hidrográfico/lacustre, ancora no lago/rio interior
            float waterZ = isCoastal ? -worldLengthMeters * 0.36f : worldLengthMeters * 0.1f;
            float waterX = isCoastal ? 0f : worldWidthMeters * 0.15f;
            starterBasePosition = new Vector3(waterX, 0f, waterZ);
            starterBasePosition.y = GetTerrainHeight(starterBasePosition);
            var pierRoot = new GameObject($"Base_Porto_{portTitle}");
            pierRoot.transform.position = starterBasePosition;

            var pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Trapiche_Madeira_1_1";
            pier.transform.SetParent(pierRoot.transform);
            float pierY = Mathf.Max(0.8f, starterBasePosition.y);
            pier.transform.position = new Vector3(waterX, pierY, waterZ - 75f);
            pier.transform.localScale = new Vector3(25f, 1.2f, 180f);
            pier.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial(new Color(0.48f, 0.35f, 0.25f));

            CreateBuilding(pierRoot.transform, starterBasePosition + new Vector3(40f, 0f, 25f), new Vector3(36f, 11f, 28f), new Color(0.45f, 0.65f, 0.75f), $"🐟 {portTitle} & Mercado de Pescados");

            Vector3 boatPos = new Vector3(waterX - 20f, 0.5f, waterZ - 100f);
            starterVehicle = VehicleBuilder.CreateBoat(boatPos);
            starterVehicle.LoadCargo("Pescado Fresco", 1800f);
        }

        #endregion
    }
}
