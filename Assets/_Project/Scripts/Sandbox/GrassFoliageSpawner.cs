using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Povoa o cenário com vegetação adaptada organicamente ao bioma:
    ///  - GRAMA: campo denso ou esparso que acompanha o jogador (<see cref="PlayerFollowGrass"/>),
    ///    com densidade e coloração adaptadas: verdejante em vales/planícies, dourada em savanas,
    ///    e MUITO ESPARSA e seca em desertos/dunas.
    ///  - FOLHAGEM: arbustos e moitas espalhados como GameObjects perto da base (renderizados
    ///    a curta distância pelo culling de <see cref="WorldStreaming"/>), reduzidos e ressecados no deserto.
    /// </summary>
    public static class GrassFoliageSpawner
    {
        public static void PopulateGrassAndFoliage(Terrain terrain, RegionSaveData save, Vector3 starterBasePos, Vector3 townPos)
        {
            if (terrain == null || terrain.terrainData == null) return;

            int seed = save != null ? save.regionId + 7 : 49;
            Random.InitState(seed);

            var regionData = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.activeRegionData : null;
            bool isDesert = PlayerFollowGrass.CheckIfDesert(save, regionData);

            // Campo de grama adaptativo seguindo o jogador.
            PlayerFollowGrass.Create(terrain, save, regionData);
            Debug.Log($"[GrassFoliageSpawner] Campo de grama (follow-player) ativado. Bioma desértico/árido: {isDesert}.");

            try
            {
                SpawnFoliageClusters(terrain, save, starterBasePos, townPos, isDesert);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GrassFoliageSpawner] Falha ao espalhar folhagem: {ex.Message}");
            }
        }

        #region Folhagem (arbustos e moitas espalhados)

        private static void SpawnFoliageClusters(Terrain terrain, RegionSaveData save, Vector3 starterBasePos, Vector3 townPos, bool isDesert)
        {
            var root = new GameObject("Environment_Foliage").transform;

            // Modelos opcionais importados; caso ausentes, usamos arbustos procedurais.
            GameObject bushA = SceneObjectSpawner.LoadModel("Models/Nature/Bushes/bush_default");
            GameObject bushB = SceneObjectSpawner.LoadModel("Models/Nature/Bushes/bush_detailed");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 baseCenter = (starterBasePos + townPos) * 0.5f;

            // Em biomas de deserto, a contagem de arbustos é drasticamente menor (apenas arbustos áridos esparsos)
            int foliageCount;
            if (isDesert)
            {
                foliageCount = Mathf.Clamp((save != null ? save.forestPercent : 0) * 2, 8, 28);
            }
            else
            {
                foliageCount = Mathf.Clamp((save != null ? save.forestPercent : 35) * 6, 120, 420);
            }

            int spawned = 0;

            for (int i = 0; i < foliageCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(40f, isDesert ? 1800f : 2600f);
                Vector3 pos = baseCenter + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                pos.y = terrain.SampleHeight(pos) + terrainPos.y;

                if (pos.y < 1.0f) continue; // evita água/leito

                GameObject prefab = (i % 2 == 0) ? bushA : bushB;
                if (prefab != null)
                {
                    var bush = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), root);
                    float sc = isDesert ? Random.Range(0.8f, 1.8f) : Random.Range(1.2f, 2.6f);
                    bush.transform.localScale = Vector3.one * sc;

                    if (isDesert)
                    {
                        // Tingir modelo importado com tom árido
                        Color desertTint = new Color(0.68f, 0.62f, 0.40f);
                        foreach (var rend in bush.GetComponentsInChildren<Renderer>())
                        {
                            if (rend != null && rend.material != null)
                            {
                                rend.material.color = desertTint;
                            }
                        }
                    }
                }
                else
                {
                    SpawnProceduralBush(root, pos, isDesert ? Random.Range(0.6f, 1.5f) : Random.Range(0.8f, 2.2f), isDesert);
                }
                spawned++;
            }

            // Folhagem renderiza só de perto via culling
            WorldStreaming.Apply(root.gameObject, WorldObjectCategory.Foliage);

            Debug.Log($"[GrassFoliageSpawner] Folhagem posicionada: {spawned} arbustos/moitas (árido/deserto={isDesert}, render até {WorldStreaming.DistNear:F0}m).");
        }

        /// <summary>
        /// Arbusto procedural com coloração e formato adaptados ao bioma (verdejante ou árido/desértico).
        /// </summary>
        private static void SpawnProceduralBush(Transform parent, Vector3 pos, float size, bool isDesert)
        {
            var bush = new GameObject(isDesert ? "Bush_DesertScrub" : "Bush_Procedural");
            bush.transform.SetParent(parent);
            bush.transform.position = pos;

            int blobs = isDesert ? Random.Range(2, 3) : Random.Range(2, 4);

            Color leaf;
            if (isDesert)
            {
                // Coloração seca de deserto: palha, ocre e oliva queimado pelo sol
                float tone = Random.Range(0.48f, 0.62f);
                leaf = new Color(tone * 1.15f, tone * 0.98f, tone * 0.58f);
            }
            else
            {
                float green = Random.Range(0.30f, 0.46f);
                leaf = new Color(0.13f, green, 0.12f);
            }

            for (int i = 0; i < blobs; i++)
            {
                var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blob.name = "Folhagem";
                blob.transform.SetParent(bush.transform);
                Vector3 off = new Vector3(Random.Range(-size, size) * 0.5f, 0f, Random.Range(-size, size) * 0.5f);
                blob.transform.localPosition = off + Vector3.up * (size * 0.45f);

                if (isDesert)
                {
                    // Arbusto achatado e espalhado, típico de vegetação rasteira de deserto
                    blob.transform.localScale = new Vector3(size * Random.Range(1.2f, 1.8f), size * Random.Range(0.35f, 0.55f), size * Random.Range(1.2f, 1.8f));
                }
                else
                {
                    blob.transform.localScale = new Vector3(size * Random.Range(0.9f, 1.4f), size * Random.Range(0.5f, 0.8f), size * Random.Range(0.9f, 1.4f));
                }

                blob.GetComponent<Renderer>().material.color = leaf;
                Object.Destroy(blob.GetComponent<Collider>());
            }
        }

        #endregion
    }
}
