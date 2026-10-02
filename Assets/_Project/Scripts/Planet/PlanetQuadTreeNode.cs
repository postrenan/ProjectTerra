using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Nó de Quadtree para subdivisão dinâmica do CubeSphere por nível de detalhe (LOD).
    /// Garante que o planeta permaneça perfeitamente estanque (sem furos ou lacunas pretas) em todas as altitudes.
    /// </summary>
    public partial class PlanetQuadTreeNode
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
    }
}
