using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Nó de Quadtree para subdivisão dinâmica do CubeSphere por nível de detalhe (LOD).
    /// Garante que o planeta permaneça perfeitamente estanque (sem furos ou lacunas pretas) em todas as altitudes.
    /// </summary>
    public class PlanetQuadTreeNode
    {
        public CubeSpherePlanet Planet { get; private set; }
        public PlanetQuadTreeNode Parent { get; private set; }
        public PlanetQuadTreeNode[] Children { get; private set; }

        public Vector3 LocalUp { get; private set; }
        public Vector3 AxisA { get; private set; }
        public Vector3 AxisB { get; private set; }

        public Vector2 CubeCenter { get; private set; }
        public float CubeSize { get; private set; }
        public int Depth { get; private set; }

        public Vector3 SphereCenter { get; private set; }
        public float BoundingRadius { get; private set; }

        public GameObject GameObject { get; private set; }
        public MeshFilter MeshFilter { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        public Mesh Mesh { get; private set; }

        public bool IsSplit { get; private set; }
        public bool IsMeshConstructed { get; private set; }

        public PlanetQuadTreeNode(
            CubeSpherePlanet planet,
            PlanetQuadTreeNode parent,
            Transform parentTransform,
            Vector3 localUp,
            Vector3 axisA,
            Vector3 axisB,
            Vector2 cubeCenter,
            float cubeSize,
            int depth,
            Material material)
        {
            Planet = planet;
            Parent = parent;
            LocalUp = localUp;
            AxisA = axisA;
            AxisB = axisB;
            CubeCenter = cubeCenter;
            CubeSize = cubeSize;
            Depth = depth;

            // Ponto central projetado na esfera
            Vector3 pointOnUnitCube = LocalUp + CubeCenter.x * AxisA + CubeCenter.y * AxisB;
            Vector3 sphereNormal = CubeToSphericalNormalized(pointOnUnitCube);
            double radius = Planet.PlanetRadius;
            SphereCenter = sphereNormal * (float)radius;

            // Raio delimitador aproximado da calota coberta por este quadrante
            BoundingRadius = (float)(radius * CubeSize * 0.707f);

            string nodeName = $"Node_D{depth}_{cubeCenter.x:F3}_{cubeCenter.y:F3}";
            GameObject = new GameObject(nodeName);
            GameObject.transform.parent = parentTransform;
            GameObject.transform.localPosition = Vector3.zero;
            GameObject.transform.localRotation = Quaternion.identity;
            GameObject.transform.localScale = Vector3.one;

            MeshFilter = GameObject.AddComponent<MeshFilter>();
            MeshRenderer = GameObject.AddComponent<MeshRenderer>();
            MeshRenderer.sharedMaterial = material;
            MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            Mesh = new Mesh
            {
                name = nodeName + "_Mesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            MeshFilter.sharedMesh = Mesh;

            ConstructMesh();
        }

        public void UpdateLOD(Vector3 localCameraPos, ref int meshBuildBudget)
        {
            float distanceToCamera = Vector3.Distance(localCameraPos, SphereCenter);

            // Culling de horizonte esférico
            Vector3 camDir = localCameraPos.normalized;
            Vector3 nodeDir = SphereCenter.normalized;
            float horizonDot = Vector3.Dot(camDir, nodeDir);

            // Se o nó estiver atrás do planeta e a câmera estiver distante, evita subdividir
            bool behindHorizon = horizonDot < -0.35f && distanceToCamera > BoundingRadius * 1.5f;

            bool shouldSplit = !behindHorizon &&
                               (distanceToCamera < BoundingRadius * Planet.SplitFactor) &&
                               (Depth < Planet.MaxDepth);

            if (shouldSplit)
            {
                if (Children == null)
                {
                    if (meshBuildBudget <= 0)
                    {
                        // Mantém o nó pai visível se não houver cota para criar os filhos neste frame
                        MeshRenderer.enabled = true;
                        return;
                    }

                    CreateChildren();
                    meshBuildBudget--;
                }

                // Recursão nos filhos
                if (Children != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (Children[i] != null)
                        {
                            Children[i].UpdateLOD(localCameraPos, ref meshBuildBudget);
                        }
                    }

                    // Prevenção de buracos pretos: só oculta a malha pai quando TODOS os 4 filhos estiverem prontos
                    bool allChildrenReady = Children[0] != null && Children[0].IsMeshConstructed &&
                                            Children[1] != null && Children[1].IsMeshConstructed &&
                                            Children[2] != null && Children[2].IsMeshConstructed &&
                                            Children[3] != null && Children[3].IsMeshConstructed;

                    if (allChildrenReady)
                    {
                        MeshRenderer.enabled = false;
                        IsSplit = true;
                    }
                    else
                    {
                        MeshRenderer.enabled = true;
                    }
                }
            }
            else
            {
                if (Children != null)
                {
                    CollapseChildren();
                    IsSplit = false;
                }

                MeshRenderer.enabled = true;
            }
        }

        private void CreateChildren()
        {
            Children = new PlanetQuadTreeNode[4];
            float childSize = CubeSize * 0.5f;
            float offset = CubeSize * 0.25f;

            Vector2[] childOffsets = new Vector2[]
            {
                new Vector2(CubeCenter.x - offset, CubeCenter.y - offset),
                new Vector2(CubeCenter.x + offset, CubeCenter.y - offset),
                new Vector2(CubeCenter.x - offset, CubeCenter.y + offset),
                new Vector2(CubeCenter.x + offset, CubeCenter.y + offset)
            };

            for (int i = 0; i < 4; i++)
            {
                Children[i] = new PlanetQuadTreeNode(
                    Planet,
                    this,
                    GameObject.transform,
                    LocalUp,
                    AxisA,
                    AxisB,
                    childOffsets[i],
                    childSize,
                    Depth + 1,
                    Planet.PlanetMaterial
                );
            }
        }

        private void CollapseChildren()
        {
            if (Children == null) return;

            for (int i = 0; i < 4; i++)
            {
                if (Children[i] != null)
                {
                    Children[i].Destroy();
                    Children[i] = null;
                }
            }
            Children = null;
        }

        public void ConstructMesh()
        {
            int res = Planet.ChunkResolution;
            int numVertices = (res + 1) * (res + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            Vector4[] tangents = new Vector4[numVertices];
            int[] triangles = new int[res * res * 6];

            int triIndex = 0;
            double radius = Planet.PlanetRadius;

            float minU = CubeCenter.x - CubeSize * 0.5f;
            float minV = CubeCenter.y - CubeSize * 0.5f;

            for (int y = 0; y <= res; y++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int i = x + y * (res + 1);
                    float px = (float)x / res;
                    float py = (float)y / res;

                    float u = minU + px * CubeSize;
                    float v = minV + py * CubeSize;

                    Vector3 pointOnUnitCube = LocalUp + u * AxisA + v * AxisB;
                    Vector3 sphereNormal = CubeToSphericalNormalized(pointOnUnitCube);

                    vertices[i] = sphereNormal * (float)radius;
                    normals[i] = sphereNormal;
                    uvs[i] = CalculateEquirectangularUV(sphereNormal);

                    Vector3 tangent3D = Vector3.Cross(Vector3.up, sphereNormal);
                    if (tangent3D.sqrMagnitude < 0.001f)
                        tangent3D = Vector3.Cross(Vector3.forward, sphereNormal);
                    tangent3D.Normalize();
                    tangents[i] = new Vector4(tangent3D.x, tangent3D.y, tangent3D.z, 1.0f);

                    if (x < res && y < res)
                    {
                        triangles[triIndex] = i;
                        triangles[triIndex + 1] = i + res + 2;
                        triangles[triIndex + 2] = i + res + 1;

                        triangles[triIndex + 3] = i;
                        triangles[triIndex + 4] = i + 1;
                        triangles[triIndex + 5] = i + res + 2;

                        triIndex += 6;
                    }
                }
            }

            // Prevenção de quebra de UV no antimeridiano (180° de longitude):
            // Quando um nó da Quadtree cruza o meridiano de 180°, ajusta os vértices com u < 0.5f somando 1.0f
            // eliminando o estiramento diagonal pelo globo.
            bool hasLowU = false;
            bool hasHighU = false;
            for (int k = 0; k < numVertices; k++)
            {
                if (uvs[k].x < 0.25f) hasLowU = true;
                if (uvs[k].x > 0.75f) hasHighU = true;
            }

            if (hasLowU && hasHighU)
            {
                for (int k = 0; k < numVertices; k++)
                {
                    if (uvs[k].x < 0.5f)
                    {
                        uvs[k].x += 1.0f;
                    }
                }
            }

            Mesh.Clear();
            Mesh.vertices = vertices;
            Mesh.triangles = triangles;
            Mesh.normals = normals;
            Mesh.uv = uvs;
            Mesh.tangents = tangents;
            Mesh.RecalculateBounds();

            IsMeshConstructed = true;
        }

        public void Destroy()
        {
            if (Children != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Children[i] != null)
                    {
                        Children[i].Destroy();
                        Children[i] = null;
                    }
                }
                Children = null;
            }

            if (Mesh != null)
            {
                SafeDestroy(Mesh);
                Mesh = null;
            }

            if (GameObject != null)
            {
                SafeDestroy(GameObject);
                GameObject = null;
            }
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }

        public void SetMaterial(Material mat)
        {
            if (MeshRenderer != null)
                MeshRenderer.sharedMaterial = mat;

            if (Children != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Children[i] != null)
                        Children[i].SetMaterial(mat);
                }
            }
        }

        private Vector3 CubeToSphericalNormalized(Vector3 p)
        {
            float x2 = p.x * p.x;
            float y2 = p.y * p.y;
            float z2 = p.z * p.z;

            float sx = p.x * Mathf.Sqrt(Mathf.Max(0f, 1f - (y2 * 0.5f) - (z2 * 0.5f) + (y2 * z2 / 3f)));
            float sy = p.y * Mathf.Sqrt(Mathf.Max(0f, 1f - (z2 * 0.5f) - (x2 * 0.5f) + (z2 * x2 / 3f)));
            float sz = p.z * Mathf.Sqrt(Mathf.Max(0f, 1f - (x2 * 0.5f) - (y2 * 0.5f) + (x2 * y2 / 3f)));

            return new Vector3(sx, sy, sz).normalized;
        }

        private Vector2 CalculateEquirectangularUV(Vector3 normal)
        {
            float longitude = Mathf.Atan2(normal.x, -normal.z);
            float latitude = Mathf.Asin(Mathf.Clamp(normal.y, -1f, 1f));

            float u = (longitude / (2f * Mathf.PI)) + 0.5f;
            float v = (latitude / Mathf.PI) + 0.5f;

            return new Vector2(u, v);
        }
    }
}
