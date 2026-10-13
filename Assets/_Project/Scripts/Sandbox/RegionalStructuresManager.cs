using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Gerenciador de geração e ciclo de vida de estruturas funcionais do mapa:
    /// Concessionárias, Silos, Postos de Estrada, Fábricas, Hangares, Estaleiros e Mercados.
    /// Garante fidelidade geográfica absoluta às características do território (bioma, hidrografia, relevo).
    /// </summary>
    public class RegionalStructuresManager : MonoBehaviour
    {
        public static RegionalStructuresManager Instance { get; private set; }

        private static readonly List<RegionalGameStructure> activeStructures = new List<RegionalGameStructure>();

        public static IReadOnlyList<RegionalGameStructure> ActiveStructures => activeStructures;

        public static void RegisterStructure(RegionalGameStructure s)
        {
            if (s != null && !activeStructures.Contains(s))
            {
                activeStructures.Add(s);
            }
        }

        public static void UnregisterStructure(RegionalGameStructure s)
        {
            if (s != null)
            {
                activeStructures.Remove(s);
            }
        }

        public static RegionalGameStructure GetNearestStructure(Vector3 position, out float distance)
        {
            distance = float.MaxValue;
            RegionalGameStructure nearest = null;

            for (int i = 0; i < activeStructures.Count; i++)
            {
                var s = activeStructures[i];
                if (s == null) continue;

                Vector3 sPos = s.interactionPoint != null ? s.interactionPoint.position : s.transform.position;
                float d = Vector3.Distance(position, sPos);
                if (d < distance)
                {
                    distance = d;
                    nearest = s;
                }
            }

            return nearest;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Gera as estruturas no mapa com base nos dados geográficos da região.
        /// </summary>
        public void GenerateRegionalStructures(
            RegionData region,
            RegionInfrastructure infra,
            Vector3 townCenter,
            float worldWidth,
            float worldLength,
            Func<Vector3, float> getTerrainHeight)
        {
            activeStructures.Clear();

            var existingRoot = GameObject.Find("World_RegionalStructures");
            if (existingRoot != null) Destroy(existingRoot);

            var rootObj = new GameObject("World_RegionalStructures");

            int generatedCount = 0;
            int rejectedCount = 0;

            var allDefs = GameStructureCatalog.GetAll();

            // Sementes determinísticas para posicionamento coerente em relação à cidade e relevo
            int seed = region != null ? region.id * 31 + 4099 : 54321;
            var rng = new System.Random(seed);

            // Determinar coordenadas de água para pesqueiros (se houver)
            bool isCoastal = (infra != null && infra.hasCoastline) || (region != null && region.waterPercent >= 35);
            float waterZ = isCoastal ? -worldLength * 0.35f : worldLength * 0.12f;
            float waterX = isCoastal ? 0f : worldWidth * 0.18f;

            // Coordenadas rurais para agropecuária
            Vector3 ruralCenter = townCenter + new Vector3(-2500f, 0f, 2200f);

            // Coordenadas rodoviárias e anel logístico para transporte pesado
            Vector3 highwayHubCenter = townCenter + new Vector3(3400f, 0f, -1400f);

            // Coordenadas aeronáuticas para aeródromo / pistas
            Vector3 airfieldCenter = townCenter + new Vector3(2200f, 0f, 4800f);

            // Coordenadas urbanas para comércio varejista e padarias
            Vector3 urbanCenter = townCenter;

            foreach (var def in allDefs)
            {
                // 1. Validação Geográfica & Bioma
                if (!def.IsCompatibleWithRegion(region, infra))
                {
                    rejectedCount++;
                    continue;
                }

                // 2. Determinar ponto de implantação no terreno
                Vector3 spawnPos = CalculateStructurePosition(def, rng, townCenter, ruralCenter, highwayHubCenter, airfieldCenter, urbanCenter, waterX, waterZ, worldWidth, worldLength);
                spawnPos.y = getTerrainHeight != null ? getTerrainHeight(spawnPos) : 0f;

                // 3. Montar a instalação 3D completa
                var structureObj = BuildStructureComplex(rootObj.transform, def, spawnPos, rng);
                generatedCount++;
            }

            Debug.Log($"[RegionalStructures] Geração concluída para '{region?.name ?? "Mundo"}': {generatedCount} estruturas implantadas com sucesso. {rejectedCount} bloqueadas por restrições geográficas de bioma/relevo.");
        }

        private Vector3 CalculateStructurePosition(
            GameStructureDefinition def,
            System.Random rng,
            Vector3 townCenter,
            Vector3 ruralCenter,
            Vector3 highwayHubCenter,
            Vector3 airfieldCenter,
            Vector3 urbanCenter,
            float waterX,
            float waterZ,
            float worldWidth,
            float worldLength)
        {
            Vector3 pos = Vector3.zero;

            switch (def.targetCareer)
            {
                case StarterCareer.Farmer:
                    if (def.group == StructureGroup.RetailTrade)
                    {
                        // Supermercado / Mercado de Fazendeiros fica no centro urbano
                        pos = urbanCenter + new Vector3(NextFloat(rng, 120f, 450f), 0f, NextFloat(rng, -200f, 200f));
                    }
                    else if (def.type == GameStructureType.LogisticsTrainStation)
                    {
                        // Estação de Trem fica na saída do anel logístico
                        pos = highwayHubCenter + new Vector3(NextFloat(rng, 600f, 1100f), 0f, NextFloat(rng, 400f, 800f));
                    }
                    else if (def.type == GameStructureType.SawmillBiomassPlant)
                    {
                        // Serraria na direção da mata
                        pos = ruralCenter + new Vector3(NextFloat(rng, -1800f, -800f), 0f, NextFloat(rng, 1000f, 2200f));
                    }
                    else
                    {
                        // Estruturas agrícolas no cinturão rural
                        float ox = NextFloat(rng, -1400f, 1400f);
                        float oz = NextFloat(rng, -1200f, 1600f);
                        pos = ruralCenter + new Vector3(ox, 0f, oz);
                    }
                    break;

                case StarterCareer.Trucker:
                    if (def.type == GameStructureType.QuarryConstructionYard)
                    {
                        // Pedreira / Obras em áreas mais afastadas e rochosas
                        pos = townCenter + new Vector3(NextFloat(rng, -3800f, -2200f), 0f, NextFloat(rng, -3200f, -1800f));
                    }
                    else
                    {
                        // Centros de distribuição, Truck Stop e Borracharia ao longo do corredor logístico
                        float ox = NextFloat(rng, -1000f, 1200f);
                        float oz = NextFloat(rng, -900f, 1100f);
                        pos = highwayHubCenter + new Vector3(ox, 0f, oz);
                    }
                    break;

                case StarterCareer.Fisherman:
                    // Todas as estruturas de pescador se fixam nas margens d'água
                    float wox = NextFloat(rng, -900f, 900f);
                    float woz = NextFloat(rng, -350f, 350f);
                    pos = new Vector3(waterX + wox, 0f, waterZ + woz);
                    break;

                case StarterCareer.Aviator:
                    if (def.type == GameStructureType.RescueHelipadHospital)
                    {
                        // Hospital com Heliponto fica próximo à malha urbana
                        pos = urbanCenter + new Vector3(NextFloat(rng, -600f, -250f), 0f, NextFloat(rng, 250f, 650f));
                    }
                    else if (def.type == GameStructureType.AgroSprayingBase)
                    {
                        // Base de pulverização fica na transição entre o campo e o aeródromo
                        pos = (ruralCenter + airfieldCenter) * 0.5f + new Vector3(NextFloat(rng, -300f, 300f), 0f, NextFloat(rng, -300f, 300f));
                    }
                    else
                    {
                        // Hangares, FBO, Aeroclube e Terminais Aéreos no complexo do aeroporto
                        float ox = NextFloat(rng, -800f, 800f);
                        float oz = NextFloat(rng, -600f, 900f);
                        pos = airfieldCenter + new Vector3(ox, 0f, oz);
                    }
                    break;
            }

            // Clamping defensivo dentro do limite do setor sandbox
            float limitX = worldWidth * 0.42f;
            float limitZ = worldLength * 0.42f;
            pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
            pos.z = Mathf.Clamp(pos.z, -limitZ, limitZ);

            return pos;
        }

        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private GameObject BuildStructureComplex(Transform parent, GameStructureDefinition def, Vector3 position, System.Random rng)
        {
            var complexObj = new GameObject($"Estrutura_{def.type}_{def.shortName}");
            complexObj.transform.SetParent(parent);
            complexObj.transform.position = position;

            // 1. Adicionar componente de interação
            var structureComp = complexObj.AddComponent<RegionalGameStructure>();
            structureComp.structureType = def.type;
            structureComp.customName = def.title;
            structureComp.interactionRadius = 35f;

            // Ponto de interação
            var interactPoint = new GameObject("PontoDeInteracao");
            interactPoint.transform.SetParent(complexObj.transform);
            interactPoint.transform.position = position;
            structureComp.interactionPoint = interactPoint.transform;

            // 2. Zona de Parada no Solo (Floor Decal / Disco)
            BuildInteractionPad(complexObj.transform, position, def.themeColor);

            // 3. Baliza Vertical Luminosa (Pylon)
            BuildBeaconPylon(complexObj.transform, position, def.themeColor, def.shortName);

            // 4. Edificação Principal & Elementos Temáticos
            BuildThematicGeometry(complexObj.transform, def, position, rng);

            return complexObj;
        }

        private void BuildInteractionPad(Transform parent, Vector3 pos, Color themeColor)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "ZonaDeParada_Pad";
            pad.transform.SetParent(parent);
            pad.transform.position = new Vector3(pos.x, pos.y + 0.12f, pos.z);
            pad.transform.localScale = new Vector3(32f, 0.08f, 32f);

            Color padColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.75f);
            var padMat = URPMaterialHelper.CreateURPParticlesLitMaterial("StructurePad_Mat", padColor, 0.2f, 0.0f);
            URPMaterialHelper.SetupTransparentBlend(padMat, 3000);
            pad.GetComponent<Renderer>().sharedMaterial = padMat;
            Destroy(pad.GetComponent<Collider>());
        }

        private void BuildBeaconPylon(Transform parent, Vector3 pos, Color themeColor, string label)
        {
            // Baliza vertical de alta visibilidade
            var pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pylon.name = $"Baliza_{label}";
            pylon.transform.SetParent(parent);
            pylon.transform.position = new Vector3(pos.x, pos.y + 35f, pos.z);
            pylon.transform.localScale = new Vector3(2.4f, 35f, 2.4f);

            var pylonMat = URPMaterialHelper.CreateURPLitMaterial("Beacon_Mat", themeColor, 0.6f, 0.3f);
            pylon.GetComponent<Renderer>().sharedMaterial = pylonMat;
            Destroy(pylon.GetComponent<Collider>());

            // Cúpula iluminada no topo
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orb_Topo";
            orb.transform.SetParent(parent);
            orb.transform.position = new Vector3(pos.x, pos.y + 70f, pos.z);
            orb.transform.localScale = new Vector3(6f, 6f, 6f);
            orb.GetComponent<Renderer>().sharedMaterial = pylonMat;
            Destroy(orb.GetComponent<Collider>());
        }

        private void BuildThematicGeometry(Transform parent, GameStructureDefinition def, Vector3 pos, System.Random rng)
        {
            // Construção personalizada do corpo da edificação conforme a profissão e especialidade
            Vector3 buildingSize = new Vector3(42f, 16f, 36f);
            Color buildingColor = def.themeColor * 0.85f + new Color(0.2f, 0.2f, 0.2f, 1f);

            switch (def.targetCareer)
            {
                case StarterCareer.Farmer:
                    if (def.type == GameStructureType.GrainElevatorCoop || def.type == GameStructureType.InputSupplyStation || def.type == GameStructureType.GrainAndOilMills)
                    {
                        // Galpão central + 4 silos metálicos cilíndricos com cúpula
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 25f), new Vector3(45f, 20f, 32f), buildingColor, def.title);
                        BuildSilosRow(parent, pos + new Vector3(36f, 0f, 25f), 4, 12f, 24f);
                    }
                    else if (def.type == GameStructureType.SawmillBiomassPlant)
                    {
                        // Serraria: galpão aberto e pilhas de toras de madeira
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 25f), new Vector3(50f, 14f, 30f), new Color(0.48f, 0.35f, 0.22f), def.title);
                        BuildWoodPiles(parent, pos + new Vector3(-35f, 0f, 20f));
                    }
                    else if (def.type == GameStructureType.LivestockMarket)
                    {
                        // Currais de madeira para animais
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 30f), new Vector3(38f, 12f, 26f), new Color(0.6f, 0.45f, 0.3f), def.title);
                        BuildLivestockPens(parent, pos + new Vector3(30f, 0f, 25f));
                    }
                    else
                    {
                        // Galpão agro comercial
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 25f), buildingSize, buildingColor, def.title);
                    }
                    break;

                case StarterCareer.Trucker:
                    if (def.type == GameStructureType.HighwayTruckStop)
                    {
                        // Posto de Estrada: Loja de conveniência + Cobertura de bombas de alta vazão
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 35f), new Vector3(38f, 12f, 28f), new Color(0.9f, 0.35f, 0.25f), "Conveniencia");
                        BuildGasStationCanopy(parent, pos + new Vector3(0f, 0f, 5f));
                    }
                    else if (def.type == GameStructureType.QuarryConstructionYard)
                    {
                        // Pedreira: escritório rústico + pilhas de brita e blocos
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 30f), new Vector3(32f, 12f, 24f), new Color(0.5f, 0.5f, 0.55f), def.title);
                        BuildGravelMounds(parent, pos + new Vector3(35f, 0f, 20f));
                    }
                    else if (def.type == GameStructureType.DryPortContainerTerminal)
                    {
                        // Terminal de Contêineres: pilhas de contêineres coloridos
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 40f), new Vector3(45f, 18f, 30f), buildingColor, def.title);
                        BuildContainerStacks(parent, pos + new Vector3(-35f, 0f, 25f));
                    }
                    else
                    {
                        // Galpões industriais de cross-docking / concessionária
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 25f), new Vector3(60f, 18f, 40f), buildingColor, def.title);
                    }
                    break;

                case StarterCareer.Fisherman:
                    // Estruturas marítimas / fluviais com trapiche de madeira
                    DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 25f), new Vector3(40f, 14f, 30f), new Color(0.2f, 0.5f, 0.7f), def.title);
                    BuildWoodenPier(parent, pos + new Vector3(0f, 0f, -20f));
                    break;

                case StarterCareer.Aviator:
                    if (def.type == GameStructureType.RescueHelipadHospital)
                    {
                        // Hospital com heliponto circular elevado
                        DowntownBuildingBuilder.BuildBuilding(parent, pos + new Vector3(0f, 0f, 25f), new Vector3(48f, 24f, 42f), new Color(0.95f, 0.95f, 0.95f), "Hospital");
                        BuildHelipad(parent, pos + new Vector3(-35f, 0f, 20f));
                    }
                    else
                    {
                        // Hangar amplo com vão livre
                        BuildAviationHangar(parent, pos + new Vector3(0f, 0f, 25f), def.title, buildingColor);
                    }
                    break;
            }
        }

        // Elementos auxiliares temáticos 3D
        private void BuildSilosRow(Transform parent, Vector3 basePos, int count, float radius, float height)
        {
            var steelMat = URPMaterialHelper.CreateURPLitMaterial("Silo_Steel", new Color(0.85f, 0.88f, 0.92f), 0.5f, 0.8f);

            for (int i = 0; i < count; i++)
            {
                Vector3 sPos = basePos + new Vector3(0f, 0f, (i - count * 0.5f) * (radius * 2.2f));
                var silo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                silo.name = $"Silo_{i + 1}";
                silo.transform.SetParent(parent);
                silo.transform.position = sPos + Vector3.up * (height * 0.5f);
                silo.transform.localScale = new Vector3(radius, height * 0.5f, radius);
                silo.GetComponent<Renderer>().sharedMaterial = steelMat;

                var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = "Cupula";
                dome.transform.SetParent(silo.transform);
                dome.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                dome.transform.localScale = new Vector3(1.02f, 0.45f, 1.02f);
                dome.GetComponent<Renderer>().sharedMaterial = steelMat;
                Destroy(dome.GetComponent<Collider>());
            }
        }

        private void BuildGasStationCanopy(Transform parent, Vector3 center)
        {
            var canopyMat = URPMaterialHelper.CreateURPLitMaterial("Canopy_Mat", new Color(0.95f, 0.35f, 0.20f), 0.4f, 0.2f);

            // Cobertura horizontal
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Cobertura_Posto";
            roof.transform.SetParent(parent);
            roof.transform.position = center + Vector3.up * 8.5f;
            roof.transform.localScale = new Vector3(32f, 1.2f, 22f);
            roof.GetComponent<Renderer>().sharedMaterial = canopyMat;

            // Pilares de sustentação
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillar.transform.SetParent(parent);
                    pillar.transform.position = center + new Vector3(x * 12f, 4.25f, z * 7f);
                    pillar.transform.localScale = new Vector3(1.2f, 4.25f, 1.2f);
                    pillar.GetComponent<Renderer>().sharedMaterial = canopyMat;
                }
            }
        }

        private void BuildAviationHangar(Transform parent, Vector3 center, string title, Color color)
        {
            var hangarMat = URPMaterialHelper.CreateURPLitMaterial("Hangar_Mat", color, 0.35f, 0.4f);

            var hangar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hangar.name = $"Hangar_{title}";
            hangar.transform.SetParent(parent);
            hangar.transform.position = center + Vector3.up * 10f;
            hangar.transform.localScale = new Vector3(56f, 20f, 48f);
            hangar.GetComponent<Renderer>().sharedMaterial = hangarMat;
        }

        private void BuildHelipad(Transform parent, Vector3 center)
        {
            var padMat = URPMaterialHelper.CreateURPLitMaterial("Helipad_Mat", new Color(0.2f, 0.22f, 0.25f), 0.2f, 0.0f);
            var helipad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            helipad.name = "Heliponto_Pouso";
            helipad.transform.SetParent(parent);
            helipad.transform.position = center + Vector3.up * 0.2f;
            helipad.transform.localScale = new Vector3(22f, 0.2f, 22f);
            helipad.GetComponent<Renderer>().sharedMaterial = padMat;

            // Cruz Vermelha Hospitalar central
            var crossMat = URPMaterialHelper.CreateURPLitMaterial("Cross_Mat", new Color(0.9f, 0.15f, 0.15f), 0.1f, 0.0f);
            var hCross1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hCross1.transform.SetParent(helipad.transform);
            hCross1.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            hCross1.transform.localScale = new Vector3(0.18f, 0.2f, 0.65f);
            hCross1.GetComponent<Renderer>().sharedMaterial = crossMat;
            Destroy(hCross1.GetComponent<Collider>());

            var hCross2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hCross2.transform.SetParent(helipad.transform);
            hCross2.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            hCross2.transform.localScale = new Vector3(0.65f, 0.2f, 0.18f);
            hCross2.GetComponent<Renderer>().sharedMaterial = crossMat;
            Destroy(hCross2.GetComponent<Collider>());
        }

        private void BuildWoodenPier(Transform parent, Vector3 center)
        {
            var woodMat = URPMaterialHelper.CreateURPLitMaterial("Pier_Wood", new Color(0.48f, 0.35f, 0.24f), 0.1f, 0.0f);
            var pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pier.name = "Trapiche_Atracacao";
            pier.transform.SetParent(parent);
            pier.transform.position = center + Vector3.up * 0.8f;
            pier.transform.localScale = new Vector3(18f, 1.2f, 50f);
            pier.GetComponent<Renderer>().sharedMaterial = woodMat;
        }

        private void BuildContainerStacks(Transform parent, Vector3 center)
        {
            Color[] colors = { new Color(0.85f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.85f), new Color(0.25f, 0.65f, 0.35f), new Color(0.9f, 0.65f, 0.15f) };
            for (int i = 0; i < 6; i++)
            {
                var contMat = URPMaterialHelper.CreateURPLitMaterial($"Container_{i}", colors[i % colors.Length], 0.4f, 0.3f);
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.SetParent(parent);
                float y = (i % 2) * 3.2f + 1.6f;
                c.transform.position = center + new Vector3((i / 2) * 7f, y, (i % 2) * 2f);
                c.transform.localScale = new Vector3(6f, 3.0f, 14f);
                c.GetComponent<Renderer>().sharedMaterial = contMat;
            }
        }

        private void BuildWoodPiles(Transform parent, Vector3 center)
        {
            var logMat = URPMaterialHelper.CreateURPLitMaterial("Logs_Mat", new Color(0.42f, 0.30f, 0.20f), 0.1f, 0.0f);
            for (int i = 0; i < 4; i++)
            {
                var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.transform.SetParent(parent);
                log.transform.position = center + new Vector3(i * 4f, 1.8f, 0f);
                log.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                log.transform.localScale = new Vector3(2.5f, 8f, 2.5f);
                log.GetComponent<Renderer>().sharedMaterial = logMat;
            }
        }

        private void BuildGravelMounds(Transform parent, Vector3 center)
        {
            var gravelMat = URPMaterialHelper.CreateURPLitMaterial("Gravel_Mat", new Color(0.6f, 0.6f, 0.62f), 0.1f, 0.0f);
            for (int i = 0; i < 3; i++)
            {
                var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mound.transform.SetParent(parent);
                mound.transform.position = center + new Vector3(i * 12f, 2.5f, 0f);
                mound.transform.localScale = new Vector3(14f, 5f, 14f);
                mound.GetComponent<Renderer>().sharedMaterial = gravelMat;
            }
        }

        private void BuildLivestockPens(Transform parent, Vector3 center)
        {
            var fenceMat = URPMaterialHelper.CreateURPLitMaterial("Fence_Mat", new Color(0.55f, 0.42f, 0.28f), 0.05f, 0.0f);
            var pen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pen.name = "Curral_Pecuario";
            pen.transform.SetParent(parent);
            pen.transform.position = center + Vector3.up * 0.9f;
            pen.transform.localScale = new Vector3(28f, 1.8f, 28f);
            pen.GetComponent<Renderer>().sharedMaterial = fenceMat;
        }
    }
}
