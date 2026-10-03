using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Povoa o cenário com vegetação:
    ///  - GRAMA: campo denso que acompanha o jogador (<see cref="PlayerFollowGrass"/>), via instancing,
    ///    com raio e densidade fixos — independente do tamanho do terreno 1:1.
    ///  - FOLHAGEM: arbustos/moitas espalhados como GameObjects perto da base/cidade (layer "perto",
    ///    renderizados só a curta distância pelo culling de <see cref="WorldStreaming"/>).
    /// </summary>
    public static class GrassFoliageSpawner
    {
        public static void PopulateGrassAndFoliage(Terrain terrain, RegionSaveData save, Vector3 starterBasePos, Vector3 townPos)
        {
            if (terrain == null || terrain.terrainData == null) return;

            int seed = save != null ? save.regionId + 7 : 49;
            Random.InitState(seed);

            // Campo de grama denso seguindo o jogador.
            PlayerFollowGrass.Create(terrain);
            Debug.Log("[GrassFoliageSpawner] Campo de grama (follow-player) ativado.");

            try
            {
                SpawnFoliageClusters(terrain, save, starterBasePos, townPos);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GrassFoliageSpawner] Falha ao espalhar folhagem: {ex.Message}");
            }
        }

        #region Folhagem (arbustos e moitas espalhados)

        private static void SpawnFoliageClusters(Terrain terrain, RegionSaveData save, Vector3 starterBasePos, Vector3 townPos)
        {
            var root = new GameObject("Environment_Foliage").transform;

            // Modelos opcionais importados; caso ausentes, usamos arbustos procedurais.
            GameObject bushA = SceneObjectSpawner.LoadModel("Models/Nature/Bushes/bush_default");
            GameObject bushB = SceneObjectSpawner.LoadModel("Models/Nature/Bushes/bush_detailed");

            Vector3 terrainPos = terrain.transform.position;
            Vector3 baseCenter = (starterBasePos + townPos) * 0.5f;

            int foliageCount = Mathf.Clamp((save != null ? save.forestPercent : 35) * 6, 120, 420);
            int spawned = 0;

            for (int i = 0; i < foliageCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(60f, 2600f);
                Vector3 pos = baseCenter + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                pos.y = terrain.SampleHeight(pos) + terrainPos.y;

                if (pos.y < 1.0f) continue; // evita água/leito

                GameObject prefab = (i % 2 == 0) ? bushA : bushB;
                if (prefab != null)
                {
                    var bush = Object.Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), root);
                    bush.transform.localScale = Vector3.one * Random.Range(1.2f, 2.6f);
                }
                else
                {
                    SpawnProceduralBush(root, pos, Random.Range(0.8f, 2.2f));
                }
                spawned++;
            }

            // Folhagem renderiza só de perto.
            WorldStreaming.Apply(root.gameObject, WorldObjectCategory.Foliage);

            Debug.Log($"[GrassFoliageSpawner] Folhagem posicionada: {spawned} arbustos/moitas (render até {WorldStreaming.DistNear:F0}m).");
        }

        /// <summary>
        /// Arbusto procedural: 2-3 esferas achatadas verdes agrupadas, sem colisor.
        /// </summary>
        private static void SpawnProceduralBush(Transform parent, Vector3 pos, float size)
        {
            var bush = new GameObject("Bush_Procedural");
            bush.transform.SetParent(parent);
            bush.transform.position = pos;

            int blobs = Random.Range(2, 4);
            float green = Random.Range(0.30f, 0.46f);
            Color leaf = new Color(0.13f, green, 0.12f);

            for (int i = 0; i < blobs; i++)
            {
                var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blob.name = "Folhagem";
                blob.transform.SetParent(bush.transform);
                Vector3 off = new Vector3(Random.Range(-size, size) * 0.5f, 0f, Random.Range(-size, size) * 0.5f);
                blob.transform.localPosition = off + Vector3.up * (size * 0.45f);
                blob.transform.localScale = new Vector3(size * Random.Range(0.9f, 1.4f), size * Random.Range(0.5f, 0.8f), size * Random.Range(0.9f, 1.4f));
                blob.GetComponent<Renderer>().material.color = leaf;
                Object.Destroy(blob.GetComponent<Collider>());
            }
        }

        #endregion
    }
}
