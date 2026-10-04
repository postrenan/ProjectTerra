using System;
using UnityEngine;
using UnityEngine.Rendering;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    public partial class RegionalSandboxManager
    {
        #region Terreno Nativo Unity & Relevo Real 1:1

        private void BuildRealisticTerrainAndHydrography()
        {
            // Carregar heightmap e metadados com as dimensões métricas 1:1 reais da região
            bool loaded = GeographicDataLoader.LoadRegionalHeightmap(activeSave.regionId, out float[,] macroHeights, out activeHydroData);

            if (activeHydroData != null && activeHydroData.realWidthMeters > 5000)
            {
                // Território padrão: usa dimensões reais 1:1 do heightmap
                territoryWidthMeters = activeHydroData.realWidthMeters;
                territoryLengthMeters = activeHydroData.realLengthMeters;
                realAreaKm2 = activeHydroData.realAreaKm2 > 0 ? activeHydroData.realAreaKm2 : (int)((territoryWidthMeters * territoryLengthMeters) / 1000000f);
            }
            else if (activeHydroData != null && activeHydroData.realWidthMeters > 0)
            {
                // Micro-ilha / território minúsculo (≤ 5 km): usa dimensões reais sem impor defaults gigantes.
                // Exemplos: Mônaco (2 km²), Maldivas (atóis de 1-2 km), São Bartolomeu, Nauru.
                // O setor ativo fica igual às dimensões reais da ilha — sem expansão artificial.
                territoryWidthMeters = activeHydroData.realWidthMeters;
                territoryLengthMeters = activeHydroData.realLengthMeters;
                realAreaKm2 = activeHydroData.realAreaKm2 > 0 ? activeHydroData.realAreaKm2 : 1;
                Debug.LogWarning($"[RegionalSandbox] Micro-ilha detectada: {activeSave.regionName} — {territoryWidthMeters / 1000f:F2} km × {territoryLengthMeters / 1000f:F2} km. Dimensões reais mantidas sem expansão.");
            }
            else
            {
                // Fallback puro: sem dados de heightmap nem hidro. Usa 75 km × 90 km genérico.
                territoryWidthMeters = 75000f;
                territoryLengthMeters = 90000f;
                realAreaKm2 = 6750;
            }

            // O setor de simulação física e malha de solo no nível do chão é balanceado para altíssima densidade de vértices:
            // • Micro-ilhas e territórios minúsculos (≤ 5 km): dimensões reais, sem expansão mínima artificial.
            // • Regiões normais (ex: Luxemburgo, Mônaco): mantêm suas dimensões métricas 100% integrais.
            // • Grandes estados (ex: São Paulo com 886 km): setor ativo limitado a 60 km × 60 km (3.600 km²).
            // 1:1 REAL: sem teto de tamanho — a região tem seu tamanho verdadeiro (RS, Amazonas, Kansas...).
            // Mínimo de 500 m para micro-ilhas; o FloatingOrigin cuida da precisão em escala continental.
            worldWidthMeters = Mathf.Max(territoryWidthMeters, 500f);
            worldLengthMeters = Mathf.Max(territoryLengthMeters, 500f);

            Debug.Log($"[RegionalSandbox] Território 1:1: {activeSave.regionName} — {territoryWidthMeters / 1000f:F1} km × {territoryLengthMeters / 1000f:F1} km | Área: {realAreaKm2:N0} km². Setor Ativo: {worldWidthMeters / 1000f:F0} km × {worldLengthMeters / 1000f:F0} km.");

            // Resolução do heightmap escala com o tamanho real da região (mais vértices em estados gigantes),
            // mantendo ~300 m por vértice, limitada a 2049 para não estourar memória/tempo.
            int HeightmapRes = ChooseHeightmapResolution(worldWidthMeters);
            activeTerrainData = new TerrainData();
            activeTerrainData.heightmapResolution = HeightmapRes;
            // elevRange: escala vertical do terreno em metros.
            // Mínimo 40m (não 250m!) para respeitar regiões genuinamente planas como Holanda,
            // Bangladesh, Polônia, Amazônia baixa — que têm elevationRange real < 50m.
            // 250m mínimo criava montanhas artificiais onde existia planície.
            float rawElevRange = activeHydroData != null ? activeHydroData.elevationRange * 1.6f : maxElevationScale;
            float elevRange = Mathf.Clamp(rawElevRange, 40f, 3200f);
            // Para regiões com heightmap sintético (fallback), usar um valor razoável de 180m
            if (!loaded && activeHydroData == null) elevRange = 180f;
            activeTerrainData.size = new Vector3(worldWidthMeters, elevRange, worldLengthMeters);

            // Síntese topográfica multi-escala (Relevo Macro Geográfico + Colinas Médias + Micro-ondulações de Lavoura)
            float[,] detailedHeights = SynthesizeDetailedHeights(macroHeights, HeightmapRes, activeHydroData, activeSave, elevRange);
            activeTerrainData.SetHeights(0, 0, detailedHeights);

            // Carregar metadados geográficos da região (bounding box e coordenadas reais)
            string dbPath = System.IO.Path.Combine(Application.streamingAssetsPath, "regions_database.bin");
            if (System.IO.File.Exists(dbPath))
            {
                try
                {
                    var db = RegionDatabase.LoadFromBinary(dbPath);
                    if (db != null && db.regions != null)
                    {
                        activeRegionData = db.regions.Find(r => r.id == activeSave.regionId);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RegionalSandbox] Falha ao carregar metadados geográficos: {ex.Message}");
                }
            }

            // IMPORTANTE: alphamapResolution deve ser definido ANTES de atribuir terrainLayers e calcular splatmap
            activeTerrainData.alphamapResolution = 512;

            // Associar Camadas PBR (Grama, Selva, Savana, Solo, Areia, Cascalho, Rocha e Neve)
            activeTerrainData.terrainLayers = TerrainPBRFactory.CreateTerrainLayers();
            float[,,] splat = TerrainPBRFactory.CalculateSplatmaps(activeTerrainData, activeHydroData, activeRegionData, activeSave);
            activeTerrainData.SetAlphamaps(0, 0, splat);

            // Criar GameObject do Terreno centralizado na origem
            var terrainObj = Terrain.CreateTerrainGameObject(activeTerrainData);
            terrainObj.name = $"Terrain_{activeSave.regionName}_EscalaReal";
            terrainObj.transform.position = new Vector3(-worldWidthMeters * 0.5f, 0f, -worldLengthMeters * 0.5f);
            activeTerrain = terrainObj.GetComponent<Terrain>();
            activeTerrain.drawTreesAndFoliage = true;
            activeTerrain.heightmapPixelError = 5;

            // Garantir que o colisor do terreno está ativo
            var tc = terrainObj.GetComponent<Collider>();
            if (tc != null) tc.enabled = true;

            // Garantir que o terreno possui um material e shader válidos
            EnsureTerrainMaterial(activeTerrain);

            // Criar corpos d'água de acordo com os dados hidrográficos
            BuildHydrographySurfaces();
        }

        /// <summary>
        /// Configura o material do terreno para URP.
        /// No URP, usamos o shader "Universal Render Pipeline/Terrain/Lit" que suporta
        /// TerrainLayers com splat mapping e normal maps.
        /// </summary>
        private void EnsureTerrainMaterial(Terrain terrain)
        {
            // URP Terrain Lit shader
            Shader s = Shader.Find("Universal Render Pipeline/Terrain/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Terrain/SimpleLit")
                    ?? Shader.Find("Nature/Terrain/Standard") // Fallback Built-in
                    ?? Shader.Find("Nature/Terrain/Diffuse");

            if (s != null)
            {
                var mat = new Material(s);
                mat.name = "Terrain_URP_Lit";
                terrain.materialTemplate = mat;
                Debug.Log($"[RegionalSandbox] Terreno URP: material '{s.name}' (splat das TerrainLayers injetado pelo Unity).");
            }
            else
            {
                Debug.LogError("[RegionalSandbox] Shader de terreno URP 'Universal Render Pipeline/Terrain/Lit' não encontrado.");
            }

            // Renderiza as camadas PBR reais em todo o setor ativo (até 60 km), sem cair no basemap de baixa-res.
            terrain.basemapDistance = 40000f;
            terrain.heightmapPixelError = 5;
        }

        private int ChooseHeightmapResolution(float widthMeters)
        {
            // ~300 m por vértice, limitado às resoluções válidas do Unity (2^n + 1).
            float target = widthMeters / 300f;
            if (target <= 512f) return 513;
            if (target <= 1024f) return 1025;
            return 2049; // teto: estados gigantes (ex.: Amazonas) ~900 m/vértice
        }

        private float[,] SynthesizeDetailedHeights(float[,] macroHeights, int res, RegionalHydroData hydro, RegionSaveData save, float elevRange)
        {
            float[,] result = new float[res, res];
            int macroRes = macroHeights != null ? macroHeights.GetLength(0) : 0;

            float mineralFactor = save.mineralsPercent / 100.0f;
            float seedX = (save.regionId % 100) * 17.31f;
            float seedY = (save.regionId / 100) * 23.47f;

            // Amplitudes em METROS convertidas para o espaço normalizado (0..1) do heightmap.
            float invElev = elevRange > 1f ? 1f / elevRange : 0f;

            // Frequências em CICLOS ao longo da região, mantendo o comprimento de onda REAL constante
            // independentemente do tamanho do estado (colinas ~3 km, ondulações ~700 m, serras ~14 km).
            float mesoCycles = Mathf.Max(2f, worldWidthMeters / 3000f);
            float microCycles = Mathf.Max(4f, worldWidthMeters / 700f);
            float ridgeCycles = Mathf.Max(1f, worldWidthMeters / 14000f);
            float flatRadiusM = 2500f; // raio aplainado no centro (base/cidade)

            for (int y = 0; y < res; y++)
            {
                float ny = y / (float)(res - 1);
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);

                    // 1. Relevo macro REAL (DEM), bilinear — peso total (1:1 vertical).
                    float macroH = 0.5f;
                    if (macroHeights != null && macroRes > 1)
                    {
                        float mx = nx * (macroRes - 1);
                        float my = ny * (macroRes - 1);
                        int x0 = Mathf.FloorToInt(mx);
                        int y0 = Mathf.FloorToInt(my);
                        int x1 = Mathf.Min(x0 + 1, macroRes - 1);
                        int y1 = Mathf.Min(y0 + 1, macroRes - 1);
                        float tx = mx - x0;
                        float ty = my - y0;
                        float h00 = macroHeights[y0, x0];
                        float h10 = macroHeights[y0, x1];
                        float h01 = macroHeights[y1, x0];
                        float h11 = macroHeights[y1, x1];
                        macroH = Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), ty);
                    }

                    // 2. Rugosidade sintética PEQUENA (em metros), só para quebrar a interpolação do DEM.
                    float meso = (Mathf.PerlinNoise(nx * mesoCycles + seedX, ny * mesoCycles + seedY) - 0.5f) * 2f * 25f * invElev;
                    float micro = (Mathf.PerlinNoise(nx * microCycles + seedX + 50f, ny * microCycles + seedY + 50f) - 0.5f) * 2f * 7f * invElev;

                    // 3. Serras extras só onde há alto teor mineral (modesto, em metros).
                    float ridges = 0f;
                    if (mineralFactor > 0.2f)
                    {
                        float rn = Mathf.PerlinNoise(nx * ridgeCycles + seedX + 120f, ny * ridgeCycles + seedY + 120f);
                        rn = 1.0f - Mathf.Abs(rn * 2.0f - 1.0f);
                        ridges = rn * rn * (mineralFactor * 120f) * invElev;
                    }

                    // 4. Aplaina o centro (base/cidade) num raio absoluto em metros.
                    float dxm = (nx - 0.5f) * worldWidthMeters;
                    float dym = (ny - 0.5f) * worldLengthMeters;
                    float distM = Mathf.Sqrt(dxm * dxm + dym * dym);
                    float townFlatness = Mathf.Clamp01((flatRadiusM - distM) / flatRadiusM);

                    float detail = (meso + micro + ridges) * (1.0f - townFlatness * 0.8f);
                    result[y, x] = Mathf.Clamp01(macroH + detail);
                }
            }

            return result;
        }

        private void BuildHydrographySurfaces()
        {
            var waterSys = WaterSystem.EnsureInstance();
            waterSys.ClearAll();

            var infra = RegionInfrastructureDatabase.GetInfrastructure(activeSave.regionId);
            bool isCoastal = (infra != null && infra.hasCoastline) || (activeHydroData != null && activeHydroData.isCoastal);
            bool hasInlandWater = (infra != null && (infra.hasRivers || infra.hasLakes)) || activeSave.waterPercent > 15;

            if (isCoastal)
            {
                // Bacia marítima oceânica na costa real
                var oceanObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                oceanObj.name = "Water_OceanBasin";
                float seaY = activeHydroData != null ? activeHydroData.seaLevelNormalized * activeTerrainData.size.y + 0.5f : 1.5f;
                Vector3 oceanPos = new Vector3(0f, seaY, -worldLengthMeters * 0.38f);
                Vector3 oceanScale = new Vector3(worldWidthMeters / 10f, 1f, (worldLengthMeters * 0.35f) / 10f);
                oceanObj.transform.position = oceanPos;
                oceanObj.transform.localScale = oceanScale;

                // O plano nasce com MeshCollider. Se ele permanecer, vira uma barreira
                // invisivel no nivel do mar: o CharacterController nao consegue descer
                // abaixo da superficie (Ctrl-mergulho e bloqueado pela despenetracao) e
                // o casco do barco ancora em vez de boiar. O lago abaixo remove o dele.
                var oceanCollider = oceanObj.GetComponent<Collider>();
                if (oceanCollider != null) Destroy(oceanCollider);

                var rend = oceanObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = WaterSystem.CreateWaterMaterial(isRiver: false);
                }

                // O Y do Bounds precisa cobrir toda a coluna d'água, não uma faixa de 200 m:
                // Bounds.Contains é 3D, então um casco afundado mais de 100 m abaixo de
                // seaY era classificado como "sem água" e perdia o empuxo.
                Bounds oceanBounds = new Bounds(oceanPos, new Vector3(worldWidthMeters, activeTerrainData.size.y + Mathf.Abs(seaY) * 2f, worldLengthMeters * 0.5f));
                waterSys.RegisterOcean(seaY, oceanBounds, infra != null && !string.IsNullOrEmpty(infra.waterBodyName) ? infra.waterBodyName : "Oceano Costeiro");
                Debug.Log($"[RegionalSandbox] Litoral oceânico construído com plataforma marítima costeira e ondas dinâmicas.");
            }
            else if (hasInlandWater)
            {
                // Grande lago / represa hidroelétrica no vale interior
                var lakeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lakeObj.name = "Water_InlandLake";
                // O terreno e posicionado em (-W*0.5, 0, -L*0.5) com tamanho (W, h, L),
                // logo ocupa x in [-W, 0] e z in [-L, 0]. Lake/SampleHeight devolvem 0
                // fora desse retangulo, entao o lago precisa ser calculado em coordenadas
                // relativas a origem do terreno, nunca em coordenadas 0-based.
                Vector3 lakePos = new Vector3(-worldWidthMeters * 0.35f, 0f, -worldLengthMeters * 0.4f);
                lakePos.y = GetTerrainHeight(lakePos) + 0.4f;
                lakeObj.transform.position = lakePos;
                lakeObj.transform.localScale = new Vector3(1800f, 0.2f, 1200f);
                Destroy(lakeObj.GetComponent<Collider>());

                var rend = lakeObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.sharedMaterial = WaterSystem.CreateWaterMaterial(isRiver: false);
                }

                string lakeName = infra != null && !string.IsNullOrEmpty(infra.waterBodyName) ? infra.waterBodyName : "Lago/Represa Regional";
                waterSys.RegisterLake(lakePos, 900f, 600f, lakePos.y, lakeName);
                Debug.Log($"[RegionalSandbox] Bacia hidrográfica/lacustre interior construída ({lakeName}) com física e dinâmica ativas.");
            }
            else
            {
                Debug.Log("[RegionalSandbox] Região interior sem bacia de mar aberto (terreno continental com rios).");
            }

            // Inicializar rede de rios georreferenciados da região
            if (activeRegionData != null && activeTerrain != null)
            {
                RiverManager.Create(activeTerrain, activeRegionData);
            }
        }

        #endregion
    }
}
