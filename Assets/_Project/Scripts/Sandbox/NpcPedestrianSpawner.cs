using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public enum NpcCultureRegion
    {
        GlobalMixed,
        EastAsia,
        EgyptAndMiddleEast,
        Americas,
        EuropeAndNordic
    }

    /// <summary>
    /// Gerenciador dinâmico de pedestres e NPCs para popular vilas, cidades e estradas.
    /// Suporta seleção por região cultural e distribuição demográfica realista.
    /// </summary>
    public class NpcPedestrianSpawner : MonoBehaviour
    {
        [Header("Configurações de Povoamento")]
        public int npcCount = 20;
        public float spawnRadius = 80.0f;
        public NpcCultureRegion cultureRegion = NpcCultureRegion.GlobalMixed;

        [Header("Controlador de Animação")]
        public RuntimeAnimatorController npcAnimatorController;

        // Controller humanoide carregado de Resources quando nenhum é atribuído no inspector.
        // Gerado por Tools/Animations/Setup Humanoid Animations em Resources/Animations/.
        private const string NPC_CONTROLLER_RESOURCE = "Animations/HumanoidLocomotion";
        private static RuntimeAnimatorController cachedNpcController;
        private static bool triedLoadController;

        // Catálogo dos NPCs Civis e Pedestres do Populate Models
        public static readonly string[] ALL_NPC_MODELS = new string[]
        {
            "Models/Characters/Populate/Populate_Female000Skin_001",
            "Models/Characters/Populate/Populate_Female000Skin_002",
            "Models/Characters/Populate/Populate_Female001Skin_001",
            "Models/Characters/Populate/Populate_Female001Skin_002",
            "Models/Characters/Populate/Populate_Female002Skin_001",
            "Models/Characters/Populate/Populate_Female002Skin_002",
            "Models/Characters/Populate/Populate_Female003Skin_001",
            "Models/Characters/Populate/Populate_Female004Skin_001",
            "Models/Characters/Populate/Populate_Female005Skin_001",
            "Models/Characters/Populate/Populate_Female006Skin_001",
            "Models/Characters/Populate/Populate_Female007Skin_001",
            "Models/Characters/Populate/Populate_Female010Skin_001",
            "Models/Characters/Populate/Populate_Female015Skin_001",
            "Models/Characters/Populate/Populate_Female020Skin_001",
            "Models/Characters/Populate/Populate_Female025Skin_001",
            "Models/Characters/Populate/Populate_Male000Skin_001",
            "Models/Characters/Populate/Populate_Male000Skin_002",
            "Models/Characters/Populate/Populate_Male001Skin_001",
            "Models/Characters/Populate/Populate_Male001Skin_002",
            "Models/Characters/Populate/Populate_Male002Skin_001",
            "Models/Characters/Populate/Populate_Male003Skin_001",
            "Models/Characters/Populate/Populate_Male004Skin_001",
            "Models/Characters/Populate/Populate_Male005Skin_001",
            "Models/Characters/Populate/Populate_Male006Skin_001",
            "Models/Characters/Populate/Populate_Male007Skin_001",
            "Models/Characters/Populate/Populate_Male008Skin_001",
            "Models/Characters/Populate/Populate_Male009Skin_001",
            "Models/Characters/Populate/Populate_Male010Skin_001",
            "Models/Characters/Populate/Populate_Male015Skin_001",
            "Models/Characters/Populate/Populate_Male020Skin_001",
            "Models/Characters/Populate/Populate_Male025Skin_001",
            "Models/Characters/Populate/Populate_Male030Skin_001"
        };

        private readonly List<GameObject> activeNpcs = new List<GameObject>();

        void Start()
        {
            SpawnPedestrians();
        }

        public void SpawnPedestrians()
        {
            Vector3 center = transform.position;
            Terrain terrain = Terrain.activeTerrain;

            for (int i = 0; i < npcCount; i++)
            {
                string modelPath = PickModelForRegion(cultureRegion);
                GameObject prefab = Resources.Load<GameObject>(modelPath);
                if (prefab == null) continue;

                // Posição aleatória ao redor do centro
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = new Vector3(center.x + randomCircle.x, 0, center.z + randomCircle.y);

                if (terrain != null)
                {
                    spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                }
                else
                {
                    if (Physics.Raycast(spawnPos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
                    {
                        spawnPos = hit.point;
                    }
                }

                GameObject npcInstance = Instantiate(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360f), 0), transform);
                npcInstance.name = $"Pedestrian_{i:00}_{prefab.name}";

                // Configurar materiais URP e texturas para os personagens
                ConfigureNpcVisuals(npcInstance, prefab.name);

                // Adicionar colisor e controlador de locomoção
                var col = npcInstance.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = npcInstance.AddComponent<CapsuleCollider>();
                    col.center = new Vector3(0, 0.9f, 0);
                    col.height = 1.8f;
                    col.radius = 0.35f;
                }

                var wanderer = npcInstance.AddComponent<NpcWandererAI>();
                wanderer.homePosition = spawnPos;
                wanderer.patrolRadius = Random.Range(15f, 40f);

                // Configurar Animator: usa o controller atribuído no inspector ou,
                // na ausência dele (NPCs criados por código), o controller humanoide de Resources.
                var anim = npcInstance.GetComponentInChildren<Animator>(true);
                var controller = ResolveNpcController();
                if (anim != null && controller != null)
                {
                    anim.runtimeAnimatorController = controller;
                    anim.applyRootMotion = false; // Locomoção feita via transform no NpcWandererAI
                }

                activeNpcs.Add(npcInstance);
            }

            Debug.Log($"[NpcPedestrianSpawner] {activeNpcs.Count} pedestres gerados na região '{cultureRegion}' ao redor de {center}.");
        }

        /// <summary>
        /// Retorna o controller humanoide a usar: o atribuído no inspector tem prioridade;
        /// caso contrário carrega (uma única vez) o gerado em Resources/Animations/.
        /// </summary>
        private RuntimeAnimatorController ResolveNpcController()
        {
            if (npcAnimatorController != null) return npcAnimatorController;

            if (!triedLoadController)
            {
                triedLoadController = true;
                cachedNpcController = Resources.Load<RuntimeAnimatorController>(NPC_CONTROLLER_RESOURCE);
                if (cachedNpcController == null)
                {
                    Debug.LogWarning($"[NpcPedestrianSpawner] Controller '{NPC_CONTROLLER_RESOURCE}' não encontrado em Resources. " +
                                     "Rode Tools/Animations/Setup Humanoid Animations para gerá-lo; sem ele os NPCs ficam sem animação de caminhada.");
                }
            }
            return cachedNpcController;
        }

        private string PickModelForRegion(NpcCultureRegion region)
        {
            if (ALL_NPC_MODELS == null || ALL_NPC_MODELS.Length == 0) return null;
            return ALL_NPC_MODELS[Random.Range(0, ALL_NPC_MODELS.Length)];
        }

        private void ConfigureNpcVisuals(GameObject npcInstance, string prefabName)
        {
            if (npcInstance == null) return;
            ProjectTerra.Core.URPMaterialHelper.UpgradeRenderersToURP(npcInstance);

            if (prefabName.StartsWith("Populate_"))
            {
                string cleanName = prefabName.Substring("Populate_".Length);
                string texPath = $"Textures/Characters/Populate/{cleanName}_Color";
                var tex = Resources.Load<Texture2D>(texPath);
                if (tex != null)
                {
                    var renderers = npcInstance.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r != null && r.sharedMaterial != null)
                        {
                            var mat = new Material(ProjectTerra.Core.URPMaterialHelper.URPLitShader);
                            mat.name = $"M_{prefabName}";
                            ProjectTerra.Core.URPMaterialHelper.SetupTextures(mat, tex);
                            r.sharedMaterial = mat;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// IA leve de caminhada e repouso (Wander / Idle) para simulação de multidões vivas.
    /// </summary>
    public class NpcWandererAI : MonoBehaviour
    {
        public Vector3 homePosition;
        public float patrolRadius = 25f;
        public float moveSpeed = 1.5f;

        private Vector3 currentTarget;
        private bool isWaiting = false;
        private float waitTimer = 0f;
        private Animator anim;

        void Start()
        {
            anim = GetComponentInChildren<Animator>(true);
            PickNewTarget();
            SetMoving(true);
        }

        // Atualiza os parâmetros do controller humanoide (Idle<->Walk via "IsMoving"/"Speed").
        private void SetMoving(bool moving)
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;
            anim.SetBool("IsMoving", moving);
            anim.SetFloat("Speed", moving ? 1.0f : 0.0f);
        }

        void Update()
        {
            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    PickNewTarget();
                    SetMoving(true);
                }
                return;
            }

            Vector3 direction = currentTarget - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude < 1.0f)
            {
                // Chegou ao destino, repousar por alguns segundos
                isWaiting = true;
                waitTimer = Random.Range(3f, 8f);
                SetMoving(false);
                return;
            }

            // Mover e rotacionar suavemente em direção ao destino
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 4.0f);
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);

            // Ajustar altura no terreno
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                Vector3 p = transform.position;
                p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                transform.position = p;
            }
        }

        private void PickNewTarget()
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            currentTarget = new Vector3(homePosition.x + r.x, homePosition.y, homePosition.z + r.y);
        }
    }
}
