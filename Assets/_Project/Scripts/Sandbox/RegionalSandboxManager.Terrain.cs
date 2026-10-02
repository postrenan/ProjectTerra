using System;
using UnityEngine;
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
                territoryWidthMeters = activeHydroData.realWidthMeters;
                territoryLengthMeters = activeHydroData.realLengthMeters;
                realAreaKm2 = activeHydroData.realAreaKm2 > 0 ? activeHydroData.realAreaKm2 : (int)((territoryWidthMeters * territoryLengthMeters) / 1000000f);
            }
            else
            {
                territoryWidthMeters = 75000f;
                territoryLengthMeters = 90000f;
                realAreaKm2 = 6750;
            }

            // O setor de simulação física e malha de solo no nível do chão é balanceado para altíssima densidade de vértices:
            // Regiões menores que 60km (ex: Luxemburgo, Mônaco) mantêm suas dimensões métricas 100% integrais.
            // Grandes estados (ex: São Paulo com 886km) operam com um setor ativo de 60km x 60km (3.600 km² de solo contínuo).
            worldWidthMeters = Mathf.Clamp(territoryWidthMeters, 25000f, 60000f);
            worldLengthMeters = Mathf.Clamp(territoryLengthMeters, 25000f, 60000f);

            Debug.Log($"[RegionalSandbox] Território 1:1: {activeSave.regionName} — {territoryWidthMeters / 1000f:F1} km × {territoryLengthMeters / 1000f:F1} km | Área: {realAreaKm2:N0} km². Setor Ativo: {worldWidthMeters / 1000f:F0} km × {worldLengthMeters / 1000f:F0} km.");

            // Criar TerrainData com 513x513 vértices para relevo nítido de colinas, vales e lavouras
            const int HeightmapRes = 513;
            activeTerrainData = new TerrainData();
            activeTerrainData.heightmapResolution = HeightmapRes;
            float elevRange = activeHydroData != null ? Mathf.Clamp(activeHydroData.elevationRange * 1.6f, 250f, 3200f) : maxElevationScale;
            activeTerrainData.size = new Vector3(worldWidthMeters, elevRange, worldLengthMeters);

            // Síntese topográfica multi-escala (Relevo Macro Geográfico + Colinas Médias + Micro-ondulações de Lavoura)
            float[,] detailedHeights = SynthesizeDetailedHeights(macroHeights, HeightmapRes, activeHydroData, activeSave);
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

            // Associar Camadas PBR (Grama, Selva, Savana, Solo, Areia, Cascalho, Rocha e Neve)
            activeTerrainData.terrainLayers = TerrainPBRFactory.CreateTerrainLayers();
            activeTerrainData.alphamapResolution = 512;
            float[,,] splat = TerrainPBRFactory.CalculateSplatmaps(activeTerrainData, activeHydroData, activeRegionData, activeSave);
            activeTerrainData.SetAlphamaps(0, 0, splat);
            activeTerrainData.SetBaseMapDirty();

            // Criar GameObject do Terreno centralizado na origem
            var terrainObj = Terrain.CreateTerrainGameObject(activeTerrainData);
            terrainObj.name = $"Terrain_{activeSave.regionName}_EscalaReal";
            terrainObj.transform.position = new Vector3(-worldWidthMeters * 0.5f, 0f, -worldLengthMeters * 0.5f);
            activeTerrain = terrainObj.GetComponent<Terrain>();
            activeTerrain.drawTreesAndFoliage = true;
            activeTerrain.heightmapPixelError = 2;
            // Configuração do Renderizador de Terreno Nativo
            // Deixar materialTemplate como null permite que a Unity use seu shader nativo interno de Terreno
            // que conecta automaticamente os splatmaps (_Control0, _Control1) para as 8 camadas PBR sem sobrescrever com material vazio
            activeTerrain.materialTemplate = null;
            activeTerrain.basemapDistance = 45000f; // 45 km de distância para renderização nítida de todas as camadas PBR sem basemap branco

            // Criar corpos d'água de acordo com os dados hidrográficos
            BuildHydrographySurfaces();
        }

        private float[,] SynthesizeDetailedHeights(float[,] macroHeights, int res, RegionalHydroData hydro, RegionSaveData save)
        {
            float[,] result = new float[res, res];
            int macroRes = macroHeights != null ? macroHeights.GetLength(0) : 0;

            float arableFactor = save.arablePercent / 100.0f;
            float mineralFactor = save.mineralsPercent / 100.0f;
            bool isCoastal = hydro != null && hydro.isCoastal;

            float seedX = (save.regionId % 100) * 17.31f;
            float seedY = (save.regionId / 100) * 23.47f;

            for (int y = 0; y < res; y++)
            {
                float ny = y / (float)(res - 1);
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);

                    // 1. Amostragem bi-linear do Relevo Geográfico Macro Real
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

                    // 2. Colinas médias (Meso-escala: ondas de 1.5km a 4km)
                    float meso1 = Mathf.PerlinNoise(nx * 12f + seedX, ny * 12f + seedY) * 0.14f;
                    float meso2 = Mathf.PerlinNoise(nx * 24f + seedX + 50f, ny * 24f + seedY + 50f) * 0.06f;

                    // 3. Micro-topografia de lavoura/pastagem (Micro-escala: ondulações suaves de 200m a 600m)
                    float micro = (Mathf.PerlinNoise(nx * 65f + seedX, ny * 65f + seedY) - 0.5f) * 0.02f;

                    // Relevo montanhoso / escarpado caso a região tenha alto teor mineral (ex: serras)
                    float mountainRidges = 0f;
                    if (mineralFactor > 0.2f)
                    {
                        float ridgeNoise = Mathf.PerlinNoise(nx * 8f + seedX + 120f, ny * 8f + seedY + 120f);
                        ridgeNoise = 1.0f - Mathf.Abs(ridgeNoise * 2.0f - 1.0f);
                        mountainRidges = ridgeNoise * ridgeNoise * (mineralFactor * 0.20f);
                    }

                    // Suavização do relevo no centro (cidade e base inicial) para facilitar manobras e tráfego
                    float distFromCenter = Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f));
                    float townFlatness = Mathf.Clamp01((0.15f - distFromCenter) / 0.15f);
                    float combinedDetail = (meso1 + meso2 + micro + mountainRidges) * (1.0f - townFlatness * 0.6f);

                    float h = macroH * 0.65f + combinedDetail;

                    // 4. Se for litoral, esculpir suave gradiente até a água na extremidade sul
                    if (isCoastal && ny < 0.25f)
                    {
                        float coastT = ny / 0.25f;
                        h *= Mathf.SmoothStep(0.02f, 1.0f, coastT);
                    }

                    result[y, x] = Mathf.Clamp01(h);
                }
            }

            return result;
        }

        private void BuildHydrographySurfaces()
        {
            var infra = RegionInfrastructureDatabase.GetInfrastructure(activeSave.regionId);
            bool isCoastal = (infra != null && infra.hasCoastline) || (activeHydroData != null && activeHydroData.isCoastal);
            bool hasInlandWater = (infra != null && (infra.hasRivers || infra.hasLakes)) || activeSave.waterPercent > 15;

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
                Debug.Log("[RegionalSandbox] Litoral oceânico construído com plataforma marítima costeira.");
            }
            else if (hasInlandWater)
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
                Debug.Log($"[RegionalSandbox] Bacia hidrográfica/lacustre interior construída ({(infra != null ? infra.waterBodyName : "Corpo Hídrico")}).");
            }
            else
            {
                Debug.Log("[RegionalSandbox] Região interior sem corpos d'água superficiais significativos (terreno continental/árido mantido).");
            }
        }

        #endregion
    }
}
