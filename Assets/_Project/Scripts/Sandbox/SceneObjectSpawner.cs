using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Gerenciador que povoa o cenário 1:1 com objetos 3D de alta qualidade importados:
    /// - Natureza: Árvores (coníferas, carvalhos) e Rochas (penhascos, pedregulhos).
    /// - Fauna: Animais de fazenda (vacas, cavalos, ovelhas) e selvagens (lobos, raposas).
    /// - Estruturas: Prédios urbanos, silos e casas rurais.
    /// - Veículos: Modelos 3D de tratores, caminhões e utilitários.
    /// </summary>
    public static class SceneObjectSpawner
    {
        private static readonly Dictionary<string, GameObject> cachedPrefabs = new Dictionary<string, GameObject>();

        public static GameObject LoadModel(string path)
        {
            if (cachedPrefabs.TryGetValue(path, out var cached) && cached != null)
            {
                return cached;
            }

            var loaded = Resources.Load<GameObject>(path);
            if (loaded != null)
            {
                cachedPrefabs[path] = loaded;
            }
            return loaded;
        }

        /// <summary>
        /// Popula a região com árvores, rochedos e animais de acordo com o bioma e save da região.
        /// </summary>
        public static void PopulateNatureAndFauna(Terrain terrain, RegionSaveData save, Vector3 starterBasePos, Vector3 townPos)
        {
            if (terrain == null || terrain.terrainData == null) return;

            var rootObj = new GameObject("Environment_SceneObjects");
            var treeRoot = new GameObject("Trees_Clusters").transform;
            treeRoot.SetParent(rootObj.transform);
            var rockRoot = new GameObject("Rocks_Formations").transform;
            rockRoot.SetParent(rootObj.transform);
            var animalRoot = new GameObject("Fauna_Animals").transform;
            animalRoot.SetParent(rootObj.transform);

            Vector3 terrainPos = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;

            // Carregar prefabs 3D de recursos
            GameObject treeDefault = LoadModel("Models/Nature/Trees/tree_default");
            GameObject treeDetailed = LoadModel("Models/Nature/Trees/tree_detailed");
            GameObject treeCone = LoadModel("Models/Nature/Trees/tree_cone");

            GameObject rockLarge = LoadModel("Models/Nature/Rocks/rock_largeA");
            GameObject rockSmall = LoadModel("Models/Nature/Rocks/rock_smallA");
            GameObject rockTall = LoadModel("Models/Nature/Rocks/rock_tallA");

            GameObject cowModel = LoadModel("Models/Animals/Farm/Cow");
            GameObject horseModel = LoadModel("Models/Animals/Farm/Horse");
            GameObject sheepModel = LoadModel("Models/Animals/Farm/Sheep");
            GameObject foxModel = LoadModel("Models/Animals/Wild/Red Fox");
            GameObject wolfModel = LoadModel("Models/Animals/Wild/Wolf");

            // Seed determinística por região
            int seed = save != null ? save.regionId : 42;
            Random.InitState(seed);

            // 1. Árvores em torno das estradas e campos (amostragem em raio de 3km do centro de operações)
            int treeCount = Mathf.Clamp((save != null ? save.forestPercent : 35) * 8, 80, 500);
            for (int i = 0; i < treeCount; i++)
            {
                // Espalha árvores ao redor da base e cidade, evitando colisão direta com o centro urbano
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(180f, 3500f);
                Vector3 baseCenter = (starterBasePos + townPos) * 0.5f;
                Vector3 spawnPos = baseCenter + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                spawnPos.y = terrain.SampleHeight(spawnPos) + terrainPos.y;

                // Não spawnar dentro de corpos d'água muito baixos
                if (spawnPos.y < 1.0f) continue;

                GameObject treePrefab = (i % 3 == 0) ? treeDetailed : (i % 2 == 0 ? treeCone : treeDefault);
                if (treePrefab != null)
                {
                    var tree = Object.Instantiate(treePrefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), treeRoot);
                    float scale = Random.Range(3.5f, 6.0f);
                    tree.transform.localScale = Vector3.one * scale;
                }
            }

            // 2. Rochas em encostas e relevos
            int rockCount = 60;
            for (int i = 0; i < rockCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(250f, 4000f);
                Vector3 spawnPos = starterBasePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrainPos.y;

                if (spawnPos.y < 1.0f) continue;

                GameObject rockPrefab = (i % 3 == 0) ? rockTall : (i % 2 == 0 ? rockLarge : rockSmall);
                if (rockPrefab != null)
                {
                    var rock = Object.Instantiate(rockPrefab, spawnPos, Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f)), rockRoot);
                    float scale = Random.Range(4.0f, 8.5f);
                    rock.transform.localScale = Vector3.one * scale;
                }
            }

            // 3. Animais de Fazenda (Cavalos, Vacas, Ovelhas) em pasto perto da base do agricultor
            if (cowModel != null || sheepModel != null || horseModel != null)
            {
                SpawnPastureAnimals(animalRoot, starterBasePos, terrain, cowModel, horseModel, sheepModel);
            }

            // 4. Animais Selvagens (Raposas, Lobos) nas áreas florestais
            if (foxModel != null || wolfModel != null)
            {
                SpawnWildAnimals(animalRoot, starterBasePos + new Vector3(800f, 0f, 900f), terrain, foxModel, wolfModel);
            }

            Debug.Log($"[SceneObjectSpawner] Povoamento concluído: {treeCount} árvores, {rockCount} rochas e grupos de animais posicionados.");
        }

        private static void SpawnPastureAnimals(Transform parent, Vector3 basePos, Terrain terrain, GameObject cow, GameObject horse, GameObject sheep)
        {
            Vector3 pastureCenter = basePos + new Vector3(120f, 0f, 80f);

            // Grupo de vacas
            if (cow != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 pos = pastureCenter + new Vector3(Random.Range(-35f, 35f), 0f, Random.Range(-35f, 35f));
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    var obj = Object.Instantiate(cow, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Vaca_{i + 1}";
                    obj.transform.localScale = Vector3.one * 1.5f;
                }
            }

            // Grupo de cavalos
            if (horse != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 pos = pastureCenter + new Vector3(Random.Range(50f, 110f), 0f, Random.Range(-25f, 25f));
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    var obj = Object.Instantiate(horse, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Cavalo_{i + 1}";
                    obj.transform.localScale = Vector3.one * 1.6f;
                }
            }

            // Grupo de ovelhas
            if (sheep != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector3 pos = pastureCenter + new Vector3(Random.Range(-70f, -20f), 0f, Random.Range(30f, 75f));
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    var obj = Object.Instantiate(sheep, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Ovelha_{i + 1}";
                    obj.transform.localScale = Vector3.one * 1.3f;
                }
            }
        }

        private static void SpawnWildAnimals(Transform parent, Vector3 forestPos, Terrain terrain, GameObject fox, GameObject wolf)
        {
            if (fox != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 pos = forestPos + new Vector3(Random.Range(-50f, 50f), 0f, Random.Range(-50f, 50f));
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    var obj = Object.Instantiate(fox, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Raposa_{i + 1}";
                    obj.transform.localScale = Vector3.one * 1.2f;
                }
            }

            if (wolf != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = forestPos + new Vector3(Random.Range(100f, 200f), 0f, Random.Range(-40f, 40f));
                    pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                    var obj = Object.Instantiate(wolf, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Lobo_{i + 1}";
                    obj.transform.localScale = Vector3.one * 1.4f;
                }
            }
        }

        /// <summary>
        /// Spawna um edifício 3D importado caso disponível no caminho, ou invoca o fallback procedural.
        /// </summary>
        public static GameObject SpawnBuilding(Transform parent, Vector3 position, Vector3 scale, string resourcePath, string name)
        {
            GameObject prefab = LoadModel(resourcePath);
            if (prefab != null)
            {
                var obj = Object.Instantiate(prefab, position, Quaternion.identity, parent);
                obj.name = name;
                obj.transform.localScale = scale;
                return obj;
            }
            return null;
        }
    }
}
