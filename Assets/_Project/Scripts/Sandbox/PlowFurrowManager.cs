using System.Collections.Generic;
using ProjectTerra.Core;
using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Gerenciador em tempo real das leivas e sulcos de solo arado gerados pelo arado agrícola.
    /// Constrói fitas de malha 3D conformatórias ao relevo com textura PBR de terra fértil revolvida,
    /// suprimindo simultaneamente a grama nativa sobre a faixa arada através da máscara de vias.
    /// </summary>
    public class PlowFurrowManager : OriginRebasedBehaviour
    {
        private static Material furrowMaterial;

        [Header("Configurações do Sulco")]
        public float furrowWidth = 2.6f;
        public float segmentDistance = 0.85f;
        public float terrainOffsetHeight = 0.035f;

        private Transform plowTransform;
        private Transform discGang;
        private Vector3 lastRecordedCenter;
        private Vector3 lastLeftVertex;
        private Vector3 lastRightVertex;
        private bool isCurrentlyTracing = false;
        private float totalDistanceAccumulated = 0f;

        // Malha atual sendo expandida
        private GameObject currentRibbonObj;
        private Mesh currentMesh;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private const int MaxVerticesPerMesh = 600; // ~300 metros por malha antes de fechar e criar novo trecho

        private static Transform furrowsContainer;

        public void Initialize(Transform plowRoot, Transform discs, float width)
        {
            plowTransform = plowRoot;
            discGang = discs;
            furrowWidth = width;
            EnsureMaterial();
        }

        /// <summary>
        /// Rebase do FloatingOrigin: os três pontos do sulco anterior são vertices
        /// world em cache. Sem deslocá-los, o primeiro passo após um rebase emitia um
        /// quad (e um RoadMaskSegment) atravessando os 25 km do salto.
        /// </summary>
        protected override void OnOriginRebased(Vector3 offset)
        {
            lastRecordedCenter -= offset;
            lastLeftVertex -= offset;
            lastRightVertex -= offset;
        }

        private static void EnsureMaterial()
        {
            if (furrowMaterial != null) return;

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            furrowMaterial = new Material(shader)
            {
                name = "Mat_PlowedSoil_Furrow",
                color = new Color(0.32f, 0.22f, 0.14f) // Tom escuro de terra fértil revolvida e úmida
            };

            // Tenta carregar texturas PBR de Solo
            string basePath = System.IO.Path.Combine(Application.dataPath, "_Project", "Textures", "PBR", "Ground", "Soil");
            string albedoPath = System.IO.Path.Combine(basePath, "Albedo.png");
            string normalPath = System.IO.Path.Combine(basePath, "Normal.png");

            if (System.IO.File.Exists(albedoPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(albedoPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Repeat;
                        furrowMaterial.mainTexture = tex;
                    }
                }
                catch {}
            }

            if (System.IO.File.Exists(normalPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(normalPath);
                    var normTex = new Texture2D(2, 2, TextureFormat.RGBA32, true, true);
                    if (normTex.LoadImage(bytes))
                    {
                        normTex.wrapMode = TextureWrapMode.Repeat;
                        furrowMaterial.EnableKeyword("_NORMALMAP");
                        furrowMaterial.SetTexture("_BumpMap", normTex);
                        furrowMaterial.SetFloat("_BumpScale", 1.2f);
                    }
                }
                catch {}
            }

            if (furrowMaterial.HasProperty("_Glossiness")) furrowMaterial.SetFloat("_Glossiness", 0.08f);
            if (furrowMaterial.HasProperty("_Metallic")) furrowMaterial.SetFloat("_Metallic", 0.02f);
        }

        public void StartFurrow(Vector3 center)
        {
            EnsureMaterial();
            EnsureContainer();

            isCurrentlyTracing = true;
            lastRecordedCenter = center;

            Vector3 right = (plowTransform != null ? plowTransform.right : Vector3.right) * (furrowWidth * 0.5f);
            lastLeftVertex = SampleGroundVertex(center - right);
            lastRightVertex = SampleGroundVertex(center + right);

            StartNewRibbon();
        }

        public void AddFurrowStep(Vector3 center)
        {
            if (!isCurrentlyTracing || currentMesh == null)
            {
                StartFurrow(center);
                return;
            }

            float dist = Vector3.Distance(new Vector3(center.x, 0f, center.z), new Vector3(lastRecordedCenter.x, 0f, lastRecordedCenter.z));
            if (dist < segmentDistance) return;

            Vector3 right = (plowTransform != null ? plowTransform.right : Vector3.right) * (furrowWidth * 0.5f);
            Vector3 curLeft = SampleGroundVertex(center - right);
            Vector3 curRight = SampleGroundVertex(center + right);

            // Adiciona quad na fita
            int baseIdx = vertices.Count;

            vertices.Add(lastLeftVertex);
            vertices.Add(lastRightVertex);
            vertices.Add(curLeft);
            vertices.Add(curRight);

            // UVs ao longo da extensão
            float v0 = totalDistanceAccumulated * 0.4f;
            float v1 = (totalDistanceAccumulated + dist) * 0.4f;
            uvs.Add(new Vector2(0f, v0));
            uvs.Add(new Vector2(1f, v0));
            uvs.Add(new Vector2(0f, v1));
            uvs.Add(new Vector2(1f, v1));

            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);
            normals.Add(Vector3.up);

            // 2 Triângulos (frente e verso para evitar clipping com inclinação de terreno)
            triangles.Add(baseIdx);
            triangles.Add(baseIdx + 2);
            triangles.Add(baseIdx + 1);

            triangles.Add(baseIdx + 1);
            triangles.Add(baseIdx + 2);
            triangles.Add(baseIdx + 3);

            // Aplica na malha
            currentMesh.SetVertices(vertices);
            currentMesh.SetUVs(0, uvs);
            currentMesh.SetNormals(normals);
            currentMesh.SetTriangles(triangles, 0);
            currentMesh.RecalculateBounds();

            // Registra segmento para suprimir grama alta no caminho arado
            if (RegionalSandboxManager.Instance != null)
            {
                RegionalSandboxManager.Instance.RoadMaskSegments.Add(new RoadMaskSegment
                {
                    a = new Vector2(lastRecordedCenter.x, lastRecordedCenter.z),
                    b = new Vector2(center.x, center.z),
                    clearance = furrowWidth * 0.55f
                });
            }

            // Notifica o sistema de grama para atualizar suavemente a área arada
            PlayerFollowGrass.Instance?.RequestGrassPruning();

            totalDistanceAccumulated += dist;
            lastRecordedCenter = center;
            lastLeftVertex = curLeft;
            lastRightVertex = curRight;

            // Se atingir o limite de vértices, inicia nova fita contínua
            if (vertices.Count >= MaxVerticesPerMesh)
            {
                StartNewRibbon();
            }
        }

        public void StopFurrow()
        {
            isCurrentlyTracing = false;
            currentRibbonObj = null;
            currentMesh = null;
        }

        private void StartNewRibbon()
        {
            EnsureContainer();

            currentRibbonObj = new GameObject($"Sulco_Arado_{System.DateTime.Now.Ticks % 10000}");
            currentRibbonObj.transform.SetParent(furrowsContainer, true);

            var mf = currentRibbonObj.AddComponent<MeshFilter>();
            var mr = currentRibbonObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = furrowMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            currentMesh = new Mesh { name = "Mesh_FurrowRibbon" };
            currentMesh.MarkDynamic();
            mf.sharedMesh = currentMesh;

            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            normals.Clear();
        }

        private Vector3 SampleGroundVertex(Vector3 pos)
        {
            float groundY = pos.y;
            if (RegionalSandboxManager.Instance != null)
            {
                groundY = RegionalSandboxManager.Instance.GetTerrainHeight(pos);
            }
            return new Vector3(pos.x, groundY + terrainOffsetHeight, pos.z);
        }

        private static void EnsureContainer()
        {
            if (furrowsContainer == null)
            {
                var go = new GameObject("Lavoura_SulcosArados");
                furrowsContainer = go.transform;
            }
        }
    }
}
