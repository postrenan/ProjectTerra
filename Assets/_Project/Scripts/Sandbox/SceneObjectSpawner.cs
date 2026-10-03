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

            // Carregar prefabs 3D de recursos — caminhos relativos à pasta Resources/Models/
            // Árvores: disponíveis em Resources/Models/Nature/Trees/
            GameObject treeDefault  = LoadModel("Models/Nature/Trees/tree_default");   // Árvore folhosa genérica
            GameObject treeDetailed = LoadModel("Models/Nature/Trees/tree_detailed");  // Árvore folhosa detalhada
            GameObject treeCone     = LoadModel("Models/Nature/Trees/tree_cone");      // Pinheiro cônico

            // Rochas: disponíveis em Resources/Models/Nature/Rocks/
            GameObject rockLarge = LoadModel("Models/Nature/Rocks/rock_largeA");
            GameObject rockSmall = LoadModel("Models/Nature/Rocks/rock_smallA");
            GameObject rockTall  = LoadModel("Models/Nature/Rocks/rock_tallA");

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
            int treesSpawned = 0;
            for (int i = 0; i < treeCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(180f, 3500f);
                Vector3 baseCenter = (starterBasePos + townPos) * 0.5f;
                Vector3 spawnPos = baseCenter + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrainPos.y;

                if (spawnPos.y < 1.0f) continue;

                GameObject[] treeModels = { treeDefault, treeCone, treeDetailed };
                GameObject treePrefab = treeModels[i % treeModels.Length];
                if (treePrefab != null)
                {
                    var tree = Object.Instantiate(treePrefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), treeRoot);
                    // Proporcoes variadas: mais alto que largo, com variacao por individuo.
                    float baseS = Random.Range(2.6f, 5.6f);
                    float w = baseS * Random.Range(0.82f, 1.18f);
                    float hgt = baseS * Random.Range(1.05f, 1.65f);
                    tree.transform.localScale = new Vector3(w, hgt, w);
                    // Variacao de tom de folhagem e tronco.
                    float g = Random.Range(0.34f, 0.56f);
                    Color leaf = new Color(0.10f + Random.Range(0f, 0.12f), g, 0.10f + Random.Range(0f, 0.08f));
                    Color bark = new Color(0.34f + Random.Range(0f, 0.12f), 0.23f + Random.Range(0f, 0.08f), 0.13f);
                    ApplyModelColors(tree, leaf, bark);
                }
                else
                {
                    // Fallback procedural: tronco (cilindro) + copa (esfera) em escala realista (5-12m)
                    SpawnProceduralTree(treeRoot, spawnPos, Random.Range(5f, 12f));
                }
                treesSpawned++;
            }

            // 2. Rochas em encostas e relevos
            int rockCount = 120;
            int rocksSpawned = 0;
            for (int i = 0; i < rockCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(250f, 4000f);
                Vector3 spawnPos = starterBasePos + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrainPos.y;

                if (spawnPos.y < 1.0f) continue;

                GameObject[] rockModels = { rockSmall, rockLarge, rockTall };
                GameObject rockPrefab = rockModels[i % rockModels.Length];
                if (rockPrefab != null)
                {
                    var rock = Object.Instantiate(rockPrefab, spawnPos, Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(0f, 360f), Random.Range(-14f, 14f)), rockRoot);
                    // Formas irregulares: escala nao-uniforme + faixa ampla de tamanho.
                    float baseS = Random.Range(1.8f, 7.0f);
                    Vector3 s = new Vector3(baseS * Random.Range(0.7f, 1.4f), baseS * Random.Range(0.5f, 1.1f), baseS * Random.Range(0.7f, 1.4f));
                    rock.transform.localScale = s;
                    float gray = Random.Range(0.36f, 0.54f);
                    ApplyModelColors(rock, new Color(gray, gray, gray + 0.03f), new Color(gray - 0.05f, gray - 0.05f, gray - 0.03f));
                }
                else
                {
                    // Fallback: cubo distorcido como rocha (1.5-5m)
                    SpawnProceduralRock(rockRoot, spawnPos, Random.Range(1.5f, 5.0f));
                }
                rocksSpawned++;
            }

            // 3. Animais de Fazenda — sempre spawnados (com prefab ou fallback procedural)
            SpawnPastureAnimals(animalRoot, starterBasePos, terrain, cowModel, horseModel, sheepModel);

            // 4. Animais Selvagens — sempre spawnados
            SpawnWildAnimals(animalRoot, starterBasePos + new Vector3(800f, 0f, 900f), terrain, foxModel, wolfModel);

            Debug.Log($"[SceneObjectSpawner] Povoamento concluído: {treesSpawned} árvores, {rocksSpawned} rochas e grupos de animais posicionados.");
        }

        private static void SpawnProceduralTree(Transform parent, Vector3 pos, float height)
        {
            var treeRoot = new GameObject("Tree_Procedural");
            treeRoot.transform.SetParent(parent);
            treeRoot.transform.position = pos;

            // Tronco: cilindro marrom (0.3m raio, altura variável)
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Tronco";
            trunk.transform.SetParent(treeRoot.transform);
            float trunkH = height * 0.55f;
            trunk.transform.localPosition = new Vector3(0f, trunkH * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(0.28f, trunkH * 0.5f, 0.28f);
            SetPrimitiveColor(trunk.GetComponent<Renderer>(), new Color(0.35f, 0.22f, 0.12f));
            Object.Destroy(trunk.GetComponent<Collider>());

            // Copa: esfera verde (varia por tipo)
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Copa";
            canopy.transform.SetParent(treeRoot.transform);
            float canopyR = height * 0.45f;
            canopy.transform.localPosition = new Vector3(0f, trunkH + canopyR * 0.7f, 0f);
            canopy.transform.localScale = new Vector3(canopyR, canopyR * 0.85f, canopyR);
            float green = Random.Range(0.28f, 0.48f);
            SetPrimitiveColor(canopy.GetComponent<Renderer>(), new Color(0.15f, green, 0.12f));
            Object.Destroy(canopy.GetComponent<Collider>());
        }

        private static void SpawnProceduralRock(Transform parent, Vector3 pos, float size)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock_Procedural";
            rock.transform.SetParent(parent);
            rock.transform.position = pos + Vector3.up * (size * 0.5f);
            rock.transform.rotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
            rock.transform.localScale = new Vector3(size * Random.Range(0.8f, 1.3f), size * Random.Range(0.5f, 0.9f), size * Random.Range(0.8f, 1.2f));
            float gray = Random.Range(0.38f, 0.52f);
            SetPrimitiveColor(rock.GetComponent<Renderer>(), new Color(gray, gray - 0.03f, gray - 0.05f));
        }

        private static void SpawnPastureAnimals(Transform parent, Vector3 basePos, Terrain terrain, GameObject cow, GameObject horse, GameObject sheep)
        {
            Vector3 pastureCenter = basePos + new Vector3(120f, 0f, 80f);

            // Grupo de vacas (6 animais, ~1.5m de altura, marrom/branco)
            for (int i = 0; i < 6; i++)
            {
                Vector3 pos = pastureCenter + new Vector3(Random.Range(-35f, 35f), 0f, Random.Range(-35f, 35f));
                pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                if (cow != null)
                {
                    var obj = Object.Instantiate(cow, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Vaca_{i + 1}";
                    ApplyModelColors(obj, new Color(0.55f, 0.35f, 0.22f), new Color(0.92f, 0.90f, 0.88f));
                    FitAnimalModel(obj, 2.4f, pos.y);
                }
                else
                {
                    SpawnAnimalPrimitive(parent, pos, $"Animal_Vaca_{i + 1}", new Vector3(0.85f, 1.5f, 1.6f), new Color(0.55f, 0.35f, 0.22f));
                }
            }

            // Grupo de cavalos (4 animais, ~1.6m de altura)
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = pastureCenter + new Vector3(Random.Range(50f, 110f), 0f, Random.Range(-25f, 25f));
                pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                if (horse != null)
                {
                    var obj = Object.Instantiate(horse, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Cavalo_{i + 1}";
                    ApplyModelColors(obj, new Color(0.32f, 0.20f, 0.12f), new Color(0.15f, 0.10f, 0.08f));
                    FitAnimalModel(obj, 2.5f, pos.y);
                }
                else
                {
                    SpawnAnimalPrimitive(parent, pos, $"Animal_Cavalo_{i + 1}", new Vector3(0.7f, 1.6f, 1.9f), new Color(0.25f, 0.18f, 0.12f));
                }
            }

            // Grupo de ovelhas (8 animais, ~0.7m de altura, branco-creme)
            for (int i = 0; i < 8; i++)
            {
                Vector3 pos = pastureCenter + new Vector3(Random.Range(-70f, -20f), 0f, Random.Range(30f, 75f));
                pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                if (sheep != null)
                {
                    var obj = Object.Instantiate(sheep, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Ovelha_{i + 1}";
                    ApplyModelColors(obj, new Color(0.92f, 0.90f, 0.85f), new Color(0.20f, 0.18f, 0.18f));
                    FitAnimalModel(obj, 1.2f, pos.y);
                }
                else
                {
                    SpawnAnimalPrimitive(parent, pos, $"Animal_Ovelha_{i + 1}", new Vector3(0.6f, 0.7f, 0.9f), new Color(0.92f, 0.90f, 0.88f));
                }
            }
        }

        private static void SpawnWildAnimals(Transform parent, Vector3 forestPos, Terrain terrain, GameObject fox, GameObject wolf)
        {
            // Raposas (4 animais, ~0.4m, laranja)
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = forestPos + new Vector3(Random.Range(-50f, 50f), 0f, Random.Range(-50f, 50f));
                pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                if (fox != null)
                {
                    var obj = Object.Instantiate(fox, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Raposa_{i + 1}";
                    ApplyModelColors(obj, new Color(0.85f, 0.42f, 0.12f), new Color(0.95f, 0.95f, 0.95f));
                    FitAnimalModel(obj, 0.9f, pos.y);
                }
                else
                {
                    SpawnAnimalPrimitive(parent, pos, $"Animal_Raposa_{i + 1}", new Vector3(0.3f, 0.4f, 0.7f), new Color(0.80f, 0.42f, 0.10f));
                }
            }

            // Lobos (3 animais, ~0.8m, cinza escuro)
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = forestPos + new Vector3(Random.Range(100f, 200f), 0f, Random.Range(-40f, 40f));
                pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
                if (wolf != null)
                {
                    var obj = Object.Instantiate(wolf, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
                    obj.name = $"Animal_Lobo_{i + 1}";
                    ApplyModelColors(obj, new Color(0.35f, 0.35f, 0.38f), new Color(0.20f, 0.20f, 0.22f));
                    FitAnimalModel(obj, 1.5f, pos.y);
                }
                else
                {
                    SpawnAnimalPrimitive(parent, pos, $"Animal_Lobo_{i + 1}", new Vector3(0.45f, 0.8f, 1.1f), new Color(0.30f, 0.30f, 0.32f));
                }
            }
        }

        /// <summary>
        /// Cria um primitivo Capsule representando um animal, com escala e cor realistas.
        /// bodySize = (width, height, length) em metros.
        /// </summary>
        private static void SpawnAnimalPrimitive(Transform parent, Vector3 pos, string name, Vector3 bodySize, Color color)
        {
            var animal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            animal.name = name;
            animal.transform.SetParent(parent);
            animal.transform.position = pos + Vector3.up * (bodySize.y * 0.5f);
            animal.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f); // Capsule deitada = corpo horizontal
            // Capsule padrão tem raio 0.5 e altura 2 (em unidades), então scale x=width, y=half_length, z=width
            animal.transform.localScale = new Vector3(bodySize.x, bodySize.z * 0.5f, bodySize.x);
            SetPrimitiveColor(animal.GetComponent<Renderer>(), color);
            // Mantém o CapsuleCollider do primitivo para que o animal tenha colisão sólida.
        }

        /// <summary>
        /// Normaliza o tamanho de um modelo FBX de animal para um comprimento real em metros (medindo
        /// os bounds dos renderers), adiciona um BoxCollider ajustado para colisão sólida e assenta o
        /// modelo sobre o chão (base dos bounds na altura do terreno informada).
        /// </summary>
        private static void FitAnimalModel(GameObject obj, float targetLengthMeters, float groundY)
        {
            if (obj == null) return;
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            // Mede os bounds com rotação zerada (alinhado aos eixos do modelo) para escala/colisor corretos.
            Quaternion origRot = obj.transform.rotation;
            obj.transform.rotation = Quaternion.identity;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float longest = Mathf.Max(b.size.x, b.size.z);
            if (longest < 1e-4f) longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (longest > 1e-4f)
            {
                obj.transform.localScale *= targetLengthMeters / longest;
                // Recomputa bounds após a escala
                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            }

            // BoxCollider ajustado aos bounds (em espaço local do objeto).
            var box = obj.GetComponent<BoxCollider>();
            if (box == null) box = obj.AddComponent<BoxCollider>();
            box.center = obj.transform.InverseTransformPoint(b.center);
            Vector3 ls = obj.transform.lossyScale;
            box.size = new Vector3(
                Mathf.Abs(ls.x) > 1e-4f ? b.size.x / Mathf.Abs(ls.x) : b.size.x,
                Mathf.Abs(ls.y) > 1e-4f ? b.size.y / Mathf.Abs(ls.y) : b.size.y,
                Mathf.Abs(ls.z) > 1e-4f ? b.size.z / Mathf.Abs(ls.z) : b.size.z);

            // Restaura a rotação e assenta a base do modelo no chão.
            obj.transform.rotation = origRot;
            b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            obj.transform.position += Vector3.up * (groundY - b.min.y);
        }

        /// <summary>
        /// Atribui uma cor a um renderer criando um novo Material com o shader Standard do Unity.
        /// NUNCA modifique .material.color diretamente em primitivos — isso corrompe o Default-Material
        /// compartilhado entre todos os objetos, causando que todos fiquem brancos.
        /// </summary>
        private static void SetPrimitiveColor(Renderer renderer, Color color)
        {
            // Busca o shader Standard (Built-in) ou Unlit/Color como fallback
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Diffuse");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
                renderer.sharedMaterial = mat;
            }
            else
            {
                // Último recurso: instanciar o material atual para não poluir o compartilhado
                renderer.material = Object.Instantiate(renderer.sharedMaterial);
                renderer.material.color = color;
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

        /// <summary>
        /// Aplica materiais com shader Standard e cores naturais aos submeshes de modelos FBX importados,
        /// garantindo que árvores (tronco e folhas), rochas e animais tenham cores vivas e sombreamento solar no Built-in Pipeline.
        /// </summary>
        private static void ApplyModelColors(GameObject instance, Color primaryColor, Color secondaryColor)
        {
            if (instance == null) return;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var matPrimary = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            matPrimary.name = $"{instance.name}_MatPrimary";
            matPrimary.color = primaryColor;
            if (matPrimary.HasProperty("_Glossiness")) matPrimary.SetFloat("_Glossiness", 0.15f);
            if (matPrimary.HasProperty("_Metallic")) matPrimary.SetFloat("_Metallic", 0.0f);

            var matSecondary = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            matSecondary.name = $"{instance.name}_MatSecondary";
            matSecondary.color = secondaryColor;
            if (matSecondary.HasProperty("_Glossiness")) matSecondary.SetFloat("_Glossiness", 0.15f);
            if (matSecondary.HasProperty("_Metallic")) matSecondary.SetFloat("_Metallic", 0.0f);

            var matAccent = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            matAccent.name = $"{instance.name}_MatAccent";
            matAccent.color = new Color(0.88f, 0.55f, 0.58f); // Rosa para focinho/orelhas
            if (matAccent.HasProperty("_Glossiness")) matAccent.SetFloat("_Glossiness", 0.18f);
            if (matAccent.HasProperty("_Metallic")) matAccent.SetFloat("_Metallic", 0.0f);

            string instNameLower = instance.name.ToLower();
            bool isTree = instNameLower.Contains("tree") || instNameLower.Contains("arvore");
            bool isCow = instNameLower.Contains("vaca") || instNameLower.Contains("cow");

            for (int r = 0; r < renderers.Length; r++)
            {
                var rend = renderers[r];
                var sharedMats = rend.sharedMaterials;
                if (sharedMats == null || sharedMats.Length == 0)
                {
                    rend.sharedMaterial = matPrimary;
                    continue;
                }

                var newMats = new Material[sharedMats.Length];
                for (int m = 0; m < sharedMats.Length; m++)
                {
                    string matName = sharedMats[m] != null ? sharedMats[m].name.ToLower() : "";

                    if (isTree)
                    {
                        // Para árvores: tronco marrom (secondary) vs copa folhosa verde (primary)
                        bool isTrunk = false;
                        if (matName.Contains("wood") || matName.Contains("bark") || matName.Contains("trunk"))
                        {
                            isTrunk = true;
                        }
                        else if (matName.Contains("leaf") || matName.Contains("foliage") || matName.Contains("green"))
                        {
                            isTrunk = false;
                        }
                        else
                        {
                            // Detecção posicional nos FBX da Kenney:
                            // tree_detailed: slot 1 é tronco; tree_cone e tree_default: slot 0 é tronco
                            isTrunk = instNameLower.Contains("detailed") ? (m == 1) : (m == 0);
                        }
                        newMats[m] = isTrunk ? matSecondary : matPrimary;
                    }
                    else if (isCow)
                    {
                        // Para vacas com 3 submeshes (Preto/Marrom manchas, Rosa focinho, Branco corpo)
                        if (matName.Contains("pink") || matName.Contains("snout") || (sharedMats.Length >= 3 && m == 1))
                        {
                            newMats[m] = matAccent;
                        }
                        else if (matName.Contains("black") || matName.Contains("dark") || matName.Contains("spot") || (sharedMats.Length >= 3 && m == 0))
                        {
                            newMats[m] = matPrimary; // Manchas escuras / marrons
                        }
                        else
                        {
                            newMats[m] = matSecondary; // Corpo claro
                        }
                    }
                    else
                    {
                        // Animais genéricos, rochas e estruturas
                        if (matName.Contains("wood") || matName.Contains("bark") || matName.Contains("dark") ||
                            matName.Contains("hoof") || matName.Contains("horn") || matName.Contains("black") ||
                            matName.Contains("mane") || (sharedMats.Length > 1 && m % 2 != 0))
                        {
                            newMats[m] = matSecondary;
                        }
                        else
                        {
                            newMats[m] = matPrimary;
                        }
                    }
                }
                rend.sharedMaterials = newMats;
            }
        }
    }
}
