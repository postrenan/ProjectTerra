using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Executor no mundo físico para o sistema de construção:
    /// - Spawna estruturas procedurais ou baseadas em prefabs
    /// - Altera o relevo físico do terreno (elevar, rebaixar, nivelar platô para pistas de pouso)
    /// - Aplica dragagem e lagos na hidrografia
    /// - Aplica pincéis de texturas e vegetação
    /// </summary>
    public class ConstructionPlacementManager : MonoBehaviour
    {
        public static ConstructionPlacementManager Instance { get; private set; }

        private Transform constructionParent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }

            EnsureParent();
        }

        private void EnsureParent()
        {
            if (constructionParent == null)
            {
                var go = GameObject.Find("World_PlayerConstructions");
                if (go == null) go = new GameObject("World_PlayerConstructions");
                constructionParent = go.transform;
            }
        }

        /// <summary>
        /// Executa a ação da construção selecionada na posição do jogador ou ponto de mira.
        /// isAutomatic = true: construção instantânea terminada.
        /// isAutomatic = false: posiciona o fantasma / canteiro de obras no mapa para entrega de materiais.
        /// </summary>
        public bool ExecuteBuild(BuildItemDefinition item, Vector3 buildPos, Quaternion rotation, bool isAutomatic, out string feedback)
        {
            EnsureParent();
            feedback = "";

            if (item == null)
            {
                feedback = "Nenhum item selecionado.";
                return false;
            }

            // Para modificações de terreno/água/solo/vegetação (Paisagismo)
            if (item.actionType != BuildActionType.StructureObject)
            {
                switch (item.actionType)
                {
                    case BuildActionType.TerrainTerraform:
                        return ApplyTerrainTerraform(item.actionPayload, buildPos, out feedback);
                    case BuildActionType.WaterTerraform:
                        return ApplyWaterTerraform(item.actionPayload, buildPos, out feedback);
                    case BuildActionType.GroundPaint:
                        return ApplyGroundPaint(item.actionPayload, buildPos, out feedback);
                    case BuildActionType.VegetationPaint:
                        return ApplyVegetationPaint(item.actionPayload, buildPos, out feedback);
                }
            }

            // Para estruturas físicas:
            if (isAutomatic)
            {
                var spawned = SpawnStructureFinal(item, buildPos, rotation);
                if (spawned != null)
                {
                    feedback = $"Construção Automática Concluída: {item.name}!";
                    return true;
                }
                feedback = "Falha ao instanciar a estrutura.";
                return false;
            }
            else
            {
                // Modo Manual: posiciona o fantasma / canteiro de obras holográfico
                var ghostObj = SpawnStructureGhost(item, buildPos, rotation);
                if (ghostObj != null)
                {
                    feedback = $"Canteiro de Obras iniciado! Fantasma posicionado no local. Entregue os recursos para finalizar.";
                    return true;
                }
                feedback = "Falha ao criar o canteiro de obras.";
                return false;
            }
        }

        #region Spawn de Estruturas Físicas

        public GameObject SpawnStructureFinal(BuildItemDefinition item, Vector3 buildPos, Quaternion rotation)
        {
            var rsm = RegionalSandboxManager.Instance;
            Terrain terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;
            if (terrain != null)
            {
                buildPos.y = terrain.SampleHeight(buildPos) + terrain.transform.position.y;
            }

            GameObject spawned = null;
            string payload = item.actionPayload;

            GameObject prefab = LoadStructurePrefab(payload);
            if (prefab != null)
            {
                spawned = Instantiate(prefab, buildPos, rotation, constructionParent);
                EnsureColliders(spawned);
            }
            else
            {
                spawned = BuildProceduralStructure(item, buildPos, rotation);
            }

            if (spawned != null)
            {
                spawned.name = $"PlayerBuild_{item.id}_{DateTime.Now.Ticks % 10000}";
            }

            return spawned;
        }

        public GameObject SpawnStructureGhost(BuildItemDefinition item, Vector3 buildPos, Quaternion rotation)
        {
            var rsm = RegionalSandboxManager.Instance;
            Terrain terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;
            if (terrain != null)
            {
                buildPos.y = terrain.SampleHeight(buildPos) + terrain.transform.position.y;
            }

            GameObject ghostObj = null;
            string payload = item.actionPayload;

            GameObject prefab = LoadStructurePrefab(payload);
            if (prefab != null)
            {
                ghostObj = Instantiate(prefab, buildPos, rotation, constructionParent);
            }
            else
            {
                ghostObj = BuildProceduralStructure(item, buildPos, rotation);
            }

            if (ghostObj != null)
            {
                ghostObj.name = $"GhostBuild_{item.id}_{DateTime.Now.Ticks % 10000}";

                // Anexar componente de canteiro de obras / fantasma
                var ghostComp = ghostObj.AddComponent<ConstructionSiteGhost>();
                var reqs = item.GetResourceRequirements();
                ghostComp.Setup(item, reqs);
            }

            return ghostObj;
        }

        private GameObject BuildProceduralStructure(BuildItemDefinition item, Vector3 pos, Quaternion rot)
        {
            var root = new GameObject($"Build_{item.name}");
            root.transform.SetParent(constructionParent);
            root.transform.position = pos;
            root.transform.rotation = rot;

            string id = item.id;

            if (id.Contains("hangar"))
            {
                // Hangares modulares
                float w = id.EndsWith("lg") ? 45f : (id.EndsWith("md") ? 28f : 16f);
                float h = id.EndsWith("lg") ? 14f : (id.EndsWith("md") ? 9f : 6f);
                float l = id.EndsWith("lg") ? 40f : (id.EndsWith("md") ? 24f : 15f);

                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                box.name = "Hangar_Body";
                box.transform.SetParent(root.transform);
                box.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
                box.transform.localScale = new Vector3(w, h, l);
                box.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.85f, 0.88f, 0.92f), 0.35f);

                // Portão frontal contrastante
                var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
                door.name = "Hangar_Door";
                door.transform.SetParent(root.transform);
                door.transform.localPosition = new Vector3(0f, (h - 1f) * 0.5f, -l * 0.5f - 0.1f);
                door.transform.localScale = new Vector3(w * 0.75f, h - 1f, 0.3f);
                door.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.2f, 0.35f, 0.55f), 0.2f);
            }
            else if (id.Contains("silo"))
            {
                // Silo cilíndrico
                var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "Silo_Body";
                cyl.transform.SetParent(root.transform);
                cyl.transform.localPosition = new Vector3(0f, 9f, 0f);
                cyl.transform.localScale = new Vector3(9f, 9f, 9f);
                cyl.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.78f, 0.82f, 0.85f), 0.5f);

                var cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cone.name = "Silo_Cap";
                cone.transform.SetParent(root.transform);
                cone.transform.localPosition = new Vector3(0f, 18.5f, 0f);
                cone.transform.localScale = new Vector3(9.2f, 0.8f, 9.2f);
                cone.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.25f, 0.45f, 0.75f), 0.3f);
            }
            else if (id.Contains("torre"))
            {
                // Torre de controle de tráfego aéreo
                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Tower_Shaft";
                shaft.transform.SetParent(root.transform);
                shaft.transform.localPosition = new Vector3(0f, 15f, 0f);
                shaft.transform.localScale = new Vector3(6f, 15f, 6f);
                shaft.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.9f, 0.9f, 0.92f), 0.2f);

                var cab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cab.name = "Tower_Cab";
                cab.transform.SetParent(root.transform);
                cab.transform.localPosition = new Vector3(0f, 32f, 0f);
                cab.transform.localScale = new Vector3(10f, 3f, 10f);
                cab.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.15f, 0.3f, 0.5f), 0.8f);
            }
            else if (id.Contains("pier") || id.Contains("marina"))
            {
                // Píer ou marina
                var pier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pier.name = "Pier_Deck";
                pier.transform.SetParent(root.transform);
                pier.transform.localPosition = new Vector3(0f, 0.4f, 12f);
                pier.transform.localScale = new Vector3(5f, 0.8f, 25f);
                Color c = id.Contains("wood") ? new Color(0.48f, 0.32f, 0.18f) : new Color(0.72f, 0.74f, 0.76f);
                pier.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(c, 0.15f);
            }
            else if (id.Contains("cerca") || id.Contains("alambrado") || id.Contains("muro"))
            {
                // Cerca / Muro modular de 15m
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Fence_Segment";
                wall.transform.SetParent(root.transform);
                wall.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                wall.transform.localScale = new Vector3(14f, 2.4f, 0.4f);
                Color c = id.Contains("rustica") ? new Color(0.55f, 0.38f, 0.22f) :
                          id.Contains("alambrado") ? new Color(0.70f, 0.72f, 0.75f) : new Color(0.45f, 0.45f, 0.48f);
                wall.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(c, 0.15f);
            }
            else
            {
                // Estrutura genérica padrão tipo galpão / fábrica / entreposto
                var main = GameObject.CreatePrimitive(PrimitiveType.Cube);
                main.name = "Structure_Volume";
                main.transform.SetParent(root.transform);
                main.transform.localPosition = new Vector3(0f, 3.5f, 0f);
                main.transform.localScale = new Vector3(14f, 7f, 18f);

                Color c = new Color(0.80f, 0.82f, 0.85f);
                if (item.category == BuildCategory.BiologiaCultivo) c = new Color(0.68f, 0.45f, 0.28f);
                else if (item.category == BuildCategory.OperacoesProducao) c = new Color(0.35f, 0.42f, 0.52f);
                else if (item.category == BuildCategory.Decoracao) c = new Color(0.85f, 0.65f, 0.25f);

                main.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(c, 0.25f);
            }

            return root;
        }

        private Material CreateURPLitMat(Color col, float smoothness)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            var m = new Material(s);
            m.color = col;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            return m;
        }

        #endregion

        #region Modificação do Relevo (Esculpir Terra)

        public bool ApplyTerrainTerraform(string mode, Vector3 worldPos, out string feedback)
        {
            var rsm = RegionalSandboxManager.Instance;
            Terrain terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;

            if (terrain == null || terrain.terrainData == null)
            {
                feedback = "Nenhum terreno ativo para esculpir.";
                return false;
            }

            var tData = terrain.terrainData;
            int res = tData.heightmapResolution;
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = tData.size;

            float normX = (worldPos.x - tPos.x) / tSize.x;
            float normZ = (worldPos.z - tPos.z) / tSize.z;

            if (normX < 0f || normX > 1f || normZ < 0f || normZ > 1f)
            {
                feedback = "Posição fora dos limites do terreno.";
                return false;
            }

            int cx = Mathf.RoundToInt(normX * (res - 1));
            int cz = Mathf.RoundToInt(normZ * (res - 1));

            float radiusMeters = 35f;
            int rPx = Mathf.Max(2, Mathf.RoundToInt((radiusMeters / tSize.x) * (res - 1)));

            int x0 = Mathf.Clamp(cx - rPx, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rPx, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rPx, 0, res - 1);
            int z1 = Mathf.Clamp(cz + rPx, 0, res - 1);

            int width = x1 - x0 + 1;
            int height = z1 - z0 + 1;

            float[,] heights = tData.GetHeights(x0, z0, width, height);
            float targetHeightNorm = (worldPos.y - tPos.y) / tSize.y;

            float heightDelta = 3.5f / tSize.y; // elevação por pincelada

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int gx = x0 + x;
                    int gz = z0 + z;
                    float dist = Vector2.Distance(new Vector2(gx, gz), new Vector2(cx, cz));
                    if (dist > rPx) continue;

                    float factor = Mathf.SmoothStep(1f, 0f, dist / rPx);

                    if (mode == "raise")
                    {
                        heights[z, x] += heightDelta * factor;
                    }
                    else if (mode == "lower")
                    {
                        heights[z, x] -= heightDelta * factor;
                    }
                    else if (mode == "smooth")
                    {
                        // Suaviza e nivela em direção à altura alvo (útil para pista de pouso)
                        heights[z, x] = Mathf.Lerp(heights[z, x], targetHeightNorm, factor * 0.75f);
                    }
                }
            }

            tData.SetHeights(x0, z0, heights);

            string actionName = mode == "raise" ? "Elevação" : (mode == "lower" ? "Rebaixamento" : "Nivelamento");
            feedback = $"{actionName} de terreno concluído com sucesso!";
            return true;
        }

        #endregion

        #region Modificação de Água (Esculpir Água)

        public bool ApplyWaterTerraform(string mode, Vector3 worldPos, out string feedback)
        {
            var rsm = RegionalSandboxManager.Instance;
            Terrain terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;

            if (terrain == null || terrain.terrainData == null)
            {
                feedback = "Terreno não encontrado para esculpir água.";
                return false;
            }

            // Para dragagem ou lago artificial, escava a bacia no terreno
            var tData = terrain.terrainData;
            int res = tData.heightmapResolution;
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = tData.size;

            float normX = (worldPos.x - tPos.x) / tSize.x;
            float normZ = (worldPos.z - tPos.z) / tSize.z;

            int cx = Mathf.RoundToInt(normX * (res - 1));
            int cz = Mathf.RoundToInt(normZ * (res - 1));

            float radiusMeters = mode == "createlake" ? 60f : 40f;
            int rPx = Mathf.Max(2, Mathf.RoundToInt((radiusMeters / tSize.x) * (res - 1)));

            int x0 = Mathf.Clamp(cx - rPx, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rPx, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rPx, 0, res - 1);
            int z1 = Mathf.Clamp(cz + rPx, 0, res - 1);

            int width = x1 - x0 + 1;
            int height = z1 - z0 + 1;

            float[,] heights = tData.GetHeights(x0, z0, width, height);
            float excavationDepth = mode == "createlake" ? (8f / tSize.y) : (4.5f / tSize.y);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int gx = x0 + x;
                    int gz = z0 + z;
                    float dist = Vector2.Distance(new Vector2(gx, gz), new Vector2(cx, cz));
                    if (dist > rPx) continue;

                    float factor = Mathf.SmoothStep(1f, 0f, dist / rPx);
                    heights[z, x] -= excavationDepth * factor;
                }
            }

            tData.SetHeights(x0, z0, heights);

            // Se for lago artificial, instanciar espelho d'água plano
            if (mode == "createlake")
            {
                var lakeMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lakeMesh.name = $"Player_ArtificialLake_{DateTime.Now.Ticks % 10000}";
                lakeMesh.transform.SetParent(constructionParent);
                lakeMesh.transform.position = new Vector3(worldPos.x, worldPos.y - 1.2f, worldPos.z);
                lakeMesh.transform.localScale = new Vector3(radiusMeters * 1.8f, 0.1f, radiusMeters * 1.8f);

                Shader ws = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(ws);
                mat.color = new Color(0.12f, 0.45f, 0.70f, 0.85f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);
                lakeMesh.GetComponent<Renderer>().sharedMaterial = mat;
                Destroy(lakeMesh.GetComponent<Collider>());
            }

            feedback = mode == "createlake" ? "Lago artificial criado com sucesso!" : "Área dragada e aprofundada com sucesso!";
            return true;
        }

        #endregion

        #region Pintura de Solo

        public bool ApplyGroundPaint(string payload, Vector3 worldPos, out string feedback)
        {
            if (int.TryParse(payload, out int layerIdx))
            {
                var rsm = RegionalSandboxManager.Instance;
                Terrain terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;
                if (terrain != null)
                {
                    TerrainGroundPainter.PaintGroundAtWorldPos(terrain, worldPos, layerIdx, 28f, 1.0f, updateGrass: true);
                    feedback = "Solo pintado com a textura selecionada (Raio: 28m).";
                    return true;
                }
            }

            feedback = "Falha ao aplicar textura de solo.";
            return false;
        }

        #endregion

        #region Pintura de Vegetação

        public bool ApplyVegetationPaint(string type, Vector3 worldPos, out string feedback)
        {
            var rsm = RegionalSandboxManager.Instance;
            Terrain terrain = rsm != null ? rsm.activeTerrain : Terrain.activeTerrain;

            var cluster = new GameObject($"VegCluster_{type}_{DateTime.Now.Ticks % 10000}");
            cluster.transform.SetParent(constructionParent);
            cluster.transform.position = worldPos;

            int count = type == "trees" ? 6 : (type == "bushes" ? 14 : 20);
            float radius = 22f;

            for (int i = 0; i < count; i++)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 p = worldPos + new Vector3(rnd.x, 0f, rnd.y);
                if (terrain != null) p.y = terrain.SampleHeight(p) + terrain.transform.position.y;

                if (type == "trees")
                {
                    var tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tree.name = "Tree_Trunk";
                    tree.transform.SetParent(cluster.transform);
                    tree.transform.position = p + Vector3.up * 2.5f;
                    tree.transform.localScale = new Vector3(0.6f, 2.5f, 0.6f);
                    tree.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.4f, 0.25f, 0.15f), 0.1f);

                    var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    top.name = "Tree_Foliage";
                    top.transform.SetParent(cluster.transform);
                    top.transform.position = p + Vector3.up * 6.5f;
                    top.transform.localScale = new Vector3(4.2f, 4.2f, 4.2f);
                    top.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(new Color(0.18f, 0.52f, 0.18f), 0.1f);
                }
                else
                {
                    // Arbusto / Moita / Alga
                    var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bush.name = "Foliage_Cluster";
                    bush.transform.SetParent(cluster.transform);
                    bush.transform.position = p + Vector3.up * 0.6f;
                    bush.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);

                    Color c = type == "aquatic" ? new Color(0.12f, 0.55f, 0.45f) : new Color(0.24f, 0.58f, 0.22f);
                    bush.GetComponent<Renderer>().sharedMaterial = CreateURPLitMat(c, 0.1f);
                }
            }

            feedback = "Vegetação plantada com sucesso!";
            return true;
        }

        #endregion

        #region Utilitários de Carregamento e Colisão

        private GameObject LoadStructurePrefab(string payload)
        {
            if (string.IsNullOrEmpty(payload)) return null;

            // Se for caminho direto com Models/
            if (payload.StartsWith("Models/"))
            {
                var direct = SceneObjectSpawner.LoadModel(payload);
                if (direct != null) return direct;
            }

            // Busca nas pastas modulares de Fazenda e Cidade
            var farm = SceneObjectSpawner.LoadModel($"Models/ModularBuildings/Farm/{payload}");
            if (farm != null) return farm;

            var city = SceneObjectSpawner.LoadModel($"Models/ModularBuildings/City/{payload}");
            if (city != null) return city;

            // Busca em Structures geral
            var struc = SceneObjectSpawner.LoadModel($"Models/Structures/{payload}");
            if (struc != null) return struc;

            // Busca nas ferramentas agrícolas e adereços
            var tools = SceneObjectSpawner.LoadModel($"Models/Props/FarmTools/{payload}");
            if (tools != null) return tools;

            // Busca nos personagens e mobiliário Populate
            var pop = SceneObjectSpawner.LoadModel($"Models/Characters/Populate/{payload}");
            if (pop != null) return pop;

            // Busca na raiz de Models
            return SceneObjectSpawner.LoadModel($"Models/{payload}");
        }

        private void EnsureColliders(GameObject root)
        {
            if (root == null) return;
            var colliders = root.GetComponentsInChildren<Collider>(true);
            if (colliders != null && colliders.Length > 0) return;

            // Se o modelo importado não tiver colisor, adiciona BoxCollider baseado nos renderers
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }

                var box = root.AddComponent<BoxCollider>();
                box.center = root.transform.InverseTransformPoint(b.center);
                Vector3 ls = root.transform.lossyScale;
                box.size = new Vector3(
                    Mathf.Abs(ls.x) > 1e-4f ? b.size.x / Mathf.Abs(ls.x) : b.size.x,
                    Mathf.Abs(ls.y) > 1e-4f ? b.size.y / Mathf.Abs(ls.y) : b.size.y,
                    Mathf.Abs(ls.z) > 1e-4f ? b.size.z / Mathf.Abs(ls.z) : b.size.z);
            }
        }

        #endregion
    }
}
