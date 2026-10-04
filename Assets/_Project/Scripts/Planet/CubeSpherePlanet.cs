using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Gerenciador do planeta Terra com subdivisão contínua por Quadtree LOD.
    /// Permite transição contínua e sem furos da órbita espacial (20.000 km) até escala de solo (50 m).
    /// </summary>
    [SelectionBase]
    public class CubeSpherePlanet : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlanetConfig planetConfig;

        [Header("LOD Quadtree Settings")]
        [Tooltip("Resolução de subdivisão de cada chunk (16x16 quads por chunk)")]
        [Range(8, 32)]
        [SerializeField] private int chunkResolution = 16;

        [Tooltip("Profundidade máxima de subdivisão (LOD 15 permite resolução < 20 metros no solo)")]
        [Range(1, 18)]
        [SerializeField] private int maxDepth = 15;

        [Tooltip("Multiplicador de distância para subdivisão (1.3 garante que o planeta fique estável e inteiro no espaço)")]
        [Range(1.0f, 3.0f)]
        [SerializeField] private float splitFactor = 1.35f;

        [Tooltip("Limite de geração de malhas por frame para garantir 60 FPS")]
        [Range(1, 32)]
        [SerializeField] private int maxChunkGenerationsPerFrame = 8;

        [Header("Rendering")]
        [SerializeField] private Material planetMaterial;
        [SerializeField] private Camera targetCamera;

        [Header("Planetary Rotation")]
        [SerializeField] private bool rotateInPlayMode = true;
        [Tooltip("Multiplicador de velocidade temporal (1x = tempo real, 360x = 1 hora a cada 10s)")]
        [SerializeField] private float timeMultiplier = 360.0f;

        // Propriedades públicas acessíveis pelos nós
        public double PlanetRadius => planetConfig != null ? planetConfig.GetEffectiveRadius() : 6371000.0;
        public int ChunkResolution => chunkResolution;
        public int MaxDepth => maxDepth;
        public float SplitFactor => splitFactor;
        public Material PlanetMaterial => planetMaterial;

        private PlanetQuadTreeNode[] rootNodes;
        private Transform[] faceParents;

        private static readonly Vector3[] FaceDirections = {
            Vector3.up,      // 0: +Y (Polo Norte)
            Vector3.down,    // 1: -Y (Polo Sul)
            Vector3.left,    // 2: -X (Pacífico / Oceania)
            Vector3.right,   // 3: +X (Índico / Ásia Oriental)
            Vector3.forward, // 4: +Z (Europa / África / Atlântico)
            Vector3.back     // 5: -Z (Américas / Pacífico)
        };

        // Eixo A do plano local do cubo (Horizontal u)
        private static readonly Vector3[] FaceAxesA = {
            Vector3.forward, // Up (+Y)
            Vector3.forward, // Down (-Y)
            Vector3.up,      // Left (-X)
            Vector3.up,      // Right (+X)
            Vector3.up,      // Forward (+Z)
            Vector3.up       // Back (-Z)
        };

        // Eixo B do plano local do cubo (Vertical v)
        // Garante rigorosamente que AxisA x AxisB = FaceDirection (vetor normal apontando para FORA)
        private static readonly Vector3[] FaceAxesB = {
            Vector3.right,   // Up:      (0,0,1) x (1,0,0) = (0,1,0) = Up
            Vector3.left,    // Down:    (0,0,1) x (-1,0,0) = (0,-1,0) = Down
            Vector3.back,    // Left:    (0,1,0) x (0,0,-1) = (-1,0,0) = Left
            Vector3.forward, // Right:   (0,1,0) x (0,0,1) = (1,0,0) = Right
            Vector3.left,    // Forward: (0,1,0) x (-1,0,0) = (0,0,1) = Forward
            Vector3.right    // Back:    (0,1,0) x (1,0,0) = (0,0,-1) = Back
        };

        private static readonly string[] FaceNames = {
            "Face_North (+Y)",
            "Face_South (-Y)",
            "Face_West (-X)",
            "Face_East (+X)",
            "Face_Forward (+Z)",
            "Face_Back (-Z)"
        };

        public static CubeSpherePlanet Instance { get; private set; }

        [Header("Runtime State")]
        [SerializeField] private bool isRotationPaused = false;
        public bool IsRotationPaused => isRotationPaused;

        public void SetRotationPaused(bool paused)
        {
            isRotationPaused = paused;
        }

        public void ToggleRotation()
        {
            isRotationPaused = !isRotationPaused;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializePlanet();
        }

        private void Start()
        {
            if (rootNodes == null || rootNodes.Length != 6)
            {
                InitializePlanet();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (rootNodes == null || rootNodes.Length != 6)
            {
                InitializePlanet();
            }
        }

        private void OnDisable()
        {
            CleanupTree();
        }

        private void Update()
        {
            // O LOD dinâmico e a rotação operam exclusivamente em Play Mode para não sujar o Editor
            if (!Application.isPlaying) return;

            if (rotateInPlayMode && !isRotationPaused)
            {
                double daySecs = planetConfig != null ? planetConfig.siderealDaySeconds : 86164.0905;
                float degreesPerSecond = (float)(360.0 / daySecs) * timeMultiplier;
                transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.Self);
            }

            UpdatePlanetLOD();
        }

        public void InitializePlanet()
        {
            CleanupTree();

            if (faceParents == null || faceParents.Length != 6)
            {
                faceParents = new Transform[6];
            }

            rootNodes = new PlanetQuadTreeNode[6];

            for (int i = 0; i < 6; i++)
            {
                Transform child = transform.Find(FaceNames[i]);
                if (child == null)
                {
                    var go = new GameObject(FaceNames[i]);
                    go.transform.parent = transform;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    child = go.transform;
                }
                faceParents[i] = child;

                // Remove renderers legados do objeto pai da face para não duplicar geometria
                if (child.TryGetComponent<MeshRenderer>(out var mr))
                    SafeDestroy(mr);
                if (child.TryGetComponent<MeshFilter>(out var mf))
                    SafeDestroy(mf);

                // Limpa filhos residuais anteriores
                for (int c = child.childCount - 1; c >= 0; c--)
                {
                    SafeDestroy(child.GetChild(c).gameObject);
                }

                // Cria o nó raiz de cada uma das 6 faces (Depth 0, Center (0,0), Size 2.0)
                rootNodes[i] = new PlanetQuadTreeNode(
                    this,
                    null,
                    faceParents[i],
                    FaceDirections[i],
                    FaceAxesA[i],
                    FaceAxesB[i],
                    Vector2.zero,
                    2.0f,
                    0,
                    planetMaterial
                );
            }

            // Inclinação axial real da Terra (23,44°)
            if (planetConfig != null)
            {
                transform.localRotation = Quaternion.Euler(planetConfig.axialTiltDegrees, 0f, 0f);
            }
        }

        private void UpdatePlanetLOD()
        {
            if (rootNodes == null) return;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            Vector3 camWorldPos = targetCamera.transform.position;
            Vector3 localCamPos = transform.InverseTransformPoint(camWorldPos);

            int budget = maxChunkGenerationsPerFrame;

            for (int i = 0; i < 6; i++)
            {
                if (rootNodes[i] != null)
                {
                    rootNodes[i].UpdateLOD(localCamPos, ref budget);
                }
            }
        }

        private void CleanupTree()
        {
            if (rootNodes != null)
            {
                for (int i = 0; i < rootNodes.Length; i++)
                {
                    if (rootNodes[i] != null)
                    {
                        rootNodes[i].Destroy();
                        rootNodes[i] = null;
                    }
                }
                rootNodes = null;
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

        [ContextMenu("Regenerate Planet LOD")]
        public void RegenerateLOD()
        {
            InitializePlanet();
        }

        public void SetMaterial(Material mat)
        {
            planetMaterial = mat;
            if (rootNodes != null)
            {
                for (int i = 0; i < rootNodes.Length; i++)
                {
                    if (rootNodes[i] != null)
                        rootNodes[i].SetMaterial(mat);
                }
            }
        }
    }
}
