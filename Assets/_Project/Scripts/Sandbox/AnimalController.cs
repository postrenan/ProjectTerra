using System;
using System.Collections.Generic;
using ProjectTerra.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProjectTerra.Sandbox
{
    public enum AnimalSpecies
    {
        Cow,
        Horse,
        Sheep,
        Fox,
        Wolf
    }

    public enum AnimalState
    {
        Idle,
        Graze,
        LookAround,
        Walk,
        Run
    }

    /// <summary>
    /// Controlador completo de animação e comportamento de animais do cenário:
    /// - Animação via Mecanim Animator e PlayableGraph (mescla suave de Idle, Walk, Run de clips FBX importados).
    /// - Camada orgânica procedural (respiração, inclinação de cabeça para pastar, olhar ao redor, balanço de cauda).
    /// - Movimentação e alinhamento com inclinação de terreno 1:1 (amostragem de relevo e normais do Terrain).
    /// - IA de pastagem e reação a aproximação de jogador e veículos (fuga quando assustado).
    /// - Suporte total a modelos FBX e fallback procedural para primitivos.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimalController : OriginRebasedBehaviour
    {
        [Header("Configuração da Espécie")]
        public AnimalSpecies species = AnimalSpecies.Cow;
        public AnimalState currentState = AnimalState.Idle;

        [Header("Parâmetros de Movimento")]
        public float walkSpeed = 1.3f;
        public float runSpeed = 3.5f;
        public float turnSpeed = 4.0f;
        public float roamRadius = 35f;

        [Header("Reatividade ao Jogador / Veículos")]
        public float fleeDistance = 8.0f;
        public float fleeDuration = 4.5f;

        [Header("Campos do Terreno")]
        private Terrain terrain;
        private Vector3 homePosition;
        private float groundHeightOffset = 0f;

        // Máquina de estados e temporizadores
        private float stateTimer = 0f;
        private Vector3 currentDestination;
        private float currentSpeed = 0f;
        private float fleeTimer = 0f;

        // Componentes de animação
        private Animator animator;
        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixerPlayable;
        private bool hasPlayableGraph = false;
        private AnimationClip idleClip;
        private AnimationClip walkClip;
        private AnimationClip runClip;

        // Ossos e articulações para animação procedural aditiva
        private Transform headBone;
        private Transform neckBone;
        private Transform tailBone;
        private Transform bodyBone;
        private readonly List<Transform> legBones = new List<Transform>();

        // Pose de repouso de cada perna, capturada em LocateBones().
        private readonly List<Quaternion> initialLegLocalRots = new List<Quaternion>();

        // Rotações e escalas locais iniciais dos ossos
        private Quaternion initialHeadLocalRot = Quaternion.identity;
        private Quaternion initialNeckLocalRot = Quaternion.identity;
        private Quaternion initialTailLocalRot = Quaternion.identity;
        private Vector3 initialBodyLocalScale = Vector3.one;

        // Pesos e transições procedurais suaves
        private float grazeWeight = 0f;
        private float targetGrazeWeight = 0f;
        private float lookYaw = 0f;
        private float currentLookYaw = 0f;
        private float breathPhase = 0f;
        private float strideCycle = 0f;

        // Cache do jogador e veículos para verificação de distância
        private float lastAwarenessCheckTime = 0f;
        private const float AWARENESS_CHECK_INTERVAL = 0.35f;

        public void Initialize(AnimalSpecies animalSpecies, Terrain sceneTerrain, string resourcePath, Vector3 home, float radius)
        {
            species = animalSpecies;
            terrain = sceneTerrain != null ? sceneTerrain : Terrain.activeTerrain;
            homePosition = home;
            roamRadius = radius > 5f ? radius : 35f;

            // Calcular offset vertical inicial em relação ao chão
            if (terrain != null)
            {
                float groundY = terrain.SampleHeight(transform.position) + terrain.transform.position.y;
                groundHeightOffset = transform.position.y - groundY;
            }

            ConfigureSpeciesAttributes();
            LocateBones();
            SetupAnimation(resourcePath);

            // Iniciar com um destino de repouso
            currentDestination = transform.position;
            SwitchToState(AnimalState.Idle, UnityEngine.Random.Range(3f, 8f));
        }

        /// <summary>
        /// Rebase do FloatingOrigin: homePosition e currentDestination são pontos world
        /// em cache. Sem deslocá-los, cada animal passava a caminhar para um destino
        /// ~25 km distante e nunca mais voltava ao pasto.
        /// </summary>
        protected override void OnOriginRebased(Vector3 offset)
        {
            homePosition -= offset;
            currentDestination -= offset;
        }

        private void ConfigureSpeciesAttributes()
        {
            breathPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            switch (species)
            {
                case AnimalSpecies.Cow:
                    walkSpeed = 1.2f;
                    runSpeed = 3.2f;
                    turnSpeed = 3.0f;
                    fleeDistance = 7.0f;
                    break;
                case AnimalSpecies.Horse:
                    walkSpeed = 2.0f;
                    runSpeed = 6.0f;
                    turnSpeed = 4.5f;
                    fleeDistance = 9.0f;
                    break;
                case AnimalSpecies.Sheep:
                    walkSpeed = 1.0f;
                    runSpeed = 3.2f;
                    turnSpeed = 3.8f;
                    fleeDistance = 7.5f;
                    break;
                case AnimalSpecies.Fox:
                    walkSpeed = 2.2f;
                    runSpeed = 6.2f;
                    turnSpeed = 5.5f;
                    fleeDistance = 16.0f;
                    break;
                case AnimalSpecies.Wolf:
                    walkSpeed = 2.4f;
                    runSpeed = 7.0f;
                    turnSpeed = 5.0f;
                    fleeDistance = 18.0f;
                    break;
            }
        }

        private void LocateBones()
        {
            headBone = FindBone(transform, "head", "cabeça", "003", "004");
            neckBone = FindBone(transform, "neck", "pescoço", "002");
            tailBone = FindBone(transform, "tail", "tail1", "rabo", "cauda", "007");
            bodyBone = FindBone(transform, "body", "spine", "hips", "root", "corpo", "001");

            if (headBone != null) initialHeadLocalRot = headBone.localRotation;
            if (neckBone != null) initialNeckLocalRot = neckBone.localRotation;
            if (tailBone != null) initialTailLocalRot = tailBone.localRotation;
            if (bodyBone != null) initialBodyLocalScale = bodyBone.localScale;

            // Localizar pernas para o fallback procedural
            FindBonesMatching(transform, legBones, "leg", "perna", "foot", "pata", "011", "014", "017", "019");

            // Cache das poses de repouso de cada perna. A animação procedural precisa
            // partir da pose do rig, não da pose do frame anterior.
            initialLegLocalRots.Clear();
            for (int i = 0; i < legBones.Count; i++)
            {
                initialLegLocalRots.Add(legBones[i] != null ? legBones[i].localRotation : Quaternion.identity);
            }
        }

        private Quaternion GetInitialLegLocalRot(int index)
        {
            if (index < 0 || index >= initialLegLocalRots.Count) return Quaternion.identity;
            return initialLegLocalRots[index];
        }

        private Transform FindBone(Transform root, params string[] keywords)
        {
            if (root == null) return null;
            string n = root.name.ToLower();
            foreach (var kw in keywords)
            {
                if (n.Contains(kw.ToLower())) return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindBone(root.GetChild(i), keywords);
                if (found != null) return found;
            }
            return null;
        }

        private void FindBonesMatching(Transform root, List<Transform> list, params string[] keywords)
        {
            if (root == null) return;
            string n = root.name.ToLower();
            foreach (var kw in keywords)
            {
                if (n.Contains(kw.ToLower()) && !list.Contains(root))
                {
                    list.Add(root);
                    break;
                }
            }
            for (int i = 0; i < root.childCount; i++)
            {
                FindBonesMatching(root.GetChild(i), list, keywords);
            }
        }

        private void SetupAnimation(string resourcePath)
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
            }

            // Tentar carregar clips de animação
            if (!string.IsNullOrEmpty(resourcePath))
            {
                var clips = Resources.LoadAll<AnimationClip>(resourcePath);
                if (clips != null && clips.Length > 0)
                {
                    foreach (var c in clips)
                    {
                        string cName = c.name.ToLower();
                        if (cName.StartsWith("__preview__")) continue;

                        if (idleClip == null && (cName.Contains("idle") || cName.Contains("repouso")))
                            idleClip = c;
                        else if (walkClip == null && (cName.Contains("walk") || cName.Contains("action") || cName.Contains("passo")))
                            walkClip = c;
                        else if (runClip == null && (cName.Contains("run") || cName.Contains("correr")))
                            runClip = c;
                    }

                    // Fallbacks para clips
                    if (idleClip == null && clips.Length > 0) idleClip = clips[0];
                    if (walkClip == null) walkClip = idleClip;
                    if (runClip == null) runClip = walkClip;
                }
            }

            // Se o Animator já possuir um Controller configurado, usar parâmetros nativos
            if (animator.runtimeAnimatorController != null)
            {
                return;
            }

            // Se não possuir Controller, construir um PlayableGraph dinâmico com AnimationMixerPlayable
            if (idleClip != null || walkClip != null)
            {
                BuildPlayableGraph();
            }
        }

        private void BuildPlayableGraph()
        {
            try
            {
                if (playableGraph.IsValid()) playableGraph.Destroy();

                playableGraph = PlayableGraph.Create($"{gameObject.name}_PlayableGraph");
                playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

                var output = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);
                mixerPlayable = AnimationMixerPlayable.Create(playableGraph, 3);
                output.SetSourcePlayable(mixerPlayable);

                int validInputs = 0;

                if (idleClip != null)
                {
                    var idlePlayable = AnimationClipPlayable.Create(playableGraph, idleClip);
                    playableGraph.Connect(idlePlayable, 0, mixerPlayable, 0);
                    mixerPlayable.SetInputWeight(0, 1.0f);
                    validInputs++;
                }

                if (walkClip != null)
                {
                    var walkPlayable = AnimationClipPlayable.Create(playableGraph, walkClip);
                    playableGraph.Connect(walkPlayable, 0, mixerPlayable, 1);
                    mixerPlayable.SetInputWeight(1, 0.0f);
                    validInputs++;
                }

                if (runClip != null && runClip != walkClip)
                {
                    var runPlayable = AnimationClipPlayable.Create(playableGraph, runClip);
                    playableGraph.Connect(runPlayable, 0, mixerPlayable, 2);
                    mixerPlayable.SetInputWeight(2, 0.0f);
                    validInputs++;
                }

                if (validInputs > 0)
                {
                    playableGraph.Play();
                    hasPlayableGraph = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AnimalController] Erro ao criar PlayableGraph para {gameObject.name}: {ex.Message}");
                hasPlayableGraph = false;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Otimização: se a câmera principal estiver muito distante (>350m), pausar IA ativa
            if (Camera.main != null)
            {
                float distToCamSqr = (transform.position - Camera.main.transform.position).sqrMagnitude;
                if (distToCamSqr > 350f * 350f)
                {
                    return;
                }
            }

            CheckSurroundingsForThreats();
            UpdateStateMachine(dt);
            UpdateMovementAndRotation(dt);
            UpdateAnimationWeights(dt);
        }

        private void CheckSurroundingsForThreats()
        {
            if (Time.time - lastAwarenessCheckTime < AWARENESS_CHECK_INTERVAL) return;
            lastAwarenessCheckTime = Time.time;

            if (currentState == AnimalState.Run) return; // Já está fugindo

            Vector3 myPos = transform.position;
            Vector3 threatPos = Vector3.zero;
            bool threatFound = false;

            // 1. Verificar proximidade do jogador
            var player = PlayerCharacterController.Instance;
            if (player != null && !player.isDriving)
            {
                float d = Vector3.Distance(myPos, player.transform.position);
                if (d < fleeDistance)
                {
                    threatPos = player.transform.position;
                    threatFound = true;
                }
            }

            // 2. Verificar veículos próximos
            if (!threatFound)
            {
                var vehicles = FindObjectsByType<VehicleController>();
                foreach (var v in vehicles)
                {
                    if (v != null)
                    {
                        float d = Vector3.Distance(myPos, v.transform.position);
                        if (d < fleeDistance * 1.5f && v.currentSpeedKmh > 5f)
                        {
                            threatPos = v.transform.position;
                            threatFound = true;
                            break;
                        }
                    }
                }
            }

            if (threatFound)
            {
                StartleAndFlee(threatPos);
            }
        }

        private void StartleAndFlee(Vector3 threatPos)
        {
            Vector3 fleeDir = (transform.position - threatPos).normalized;
            fleeDir.y = 0f;
            if (fleeDir.sqrMagnitude < 0.01f) fleeDir = transform.forward;

            // Escolher ponto de fuga
            Vector3 targetFlee = transform.position + fleeDir * UnityEngine.Random.Range(20f, 35f);

            // Garantir que não fuja infinitamente longe da área de origem
            if (Vector3.Distance(targetFlee, homePosition) > roamRadius * 1.6f)
            {
                Vector3 toHome = (homePosition - transform.position).normalized;
                fleeDir = (fleeDir + toHome * 0.7f).normalized;
                targetFlee = transform.position + fleeDir * 20f;
            }

            currentDestination = targetFlee;
            fleeTimer = fleeDuration;
            SwitchToState(AnimalState.Run, fleeDuration);
        }

        private void UpdateStateMachine(float dt)
        {
            stateTimer -= dt;

            switch (currentState)
            {
                case AnimalState.Idle:
                    targetGrazeWeight = 0f;
                    if (stateTimer <= 0f)
                    {
                        DecideNextIdleAction();
                    }
                    break;

                case AnimalState.Graze:
                    targetGrazeWeight = 1.0f;
                    if (stateTimer <= 0f)
                    {
                        DecideNextIdleAction();
                    }
                    break;

                case AnimalState.LookAround:
                    targetGrazeWeight = 0f;
                    if (stateTimer <= 0f)
                    {
                        DecideNextIdleAction();
                    }
                    break;

                case AnimalState.Walk:
                    targetGrazeWeight = 0f;
                    // Chegou ao destino ou tempo expirou
                    float distToDest = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                                        new Vector3(currentDestination.x, 0f, currentDestination.z));
                    if (distToDest < 1.5f || stateTimer <= 0f)
                    {
                        SwitchToState(AnimalState.Idle, UnityEngine.Random.Range(4f, 10f));
                    }
                    break;

                case AnimalState.Run:
                    targetGrazeWeight = 0f;
                    fleeTimer -= dt;
                    float distRun = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                                     new Vector3(currentDestination.x, 0f, currentDestination.z));
                    if (distRun < 2.0f || fleeTimer <= 0f)
                    {
                        SwitchToState(AnimalState.LookAround, UnityEngine.Random.Range(2.5f, 5.0f));
                    }
                    break;
            }
        }

        private void DecideNextIdleAction()
        {
            float roll = UnityEngine.Random.value;

            // Espécies de pasto (vaca, ovelha, cavalo) pastam frequentemente
            bool isGrazer = species == AnimalSpecies.Cow || species == AnimalSpecies.Sheep || species == AnimalSpecies.Horse;

            if (isGrazer && roll < 0.45f)
            {
                SwitchToState(AnimalState.Graze, UnityEngine.Random.Range(5f, 12f));
            }
            else if (roll < 0.70f)
            {
                // Olhar ao redor
                lookYaw = UnityEngine.Random.Range(-35f, 35f);
                SwitchToState(AnimalState.LookAround, UnityEngine.Random.Range(3f, 6f));
            }
            else
            {
                // Caminhar para um novo local dentro do raio de pasto
                PickNewWanderDestination();
                SwitchToState(AnimalState.Walk, UnityEngine.Random.Range(8f, 18f));
            }
        }

        private void PickNewWanderDestination()
        {
            Vector2 randCircle = UnityEngine.Random.insideUnitCircle * roamRadius;
            Vector3 dest = homePosition + new Vector3(randCircle.x, 0f, randCircle.y);

            if (terrain != null)
            {
                dest.y = terrain.SampleHeight(dest) + terrain.transform.position.y + groundHeightOffset;
            }

            currentDestination = dest;
        }

        private void SwitchToState(AnimalState newState, float duration)
        {
            currentState = newState;
            stateTimer = duration;

            if (newState == AnimalState.LookAround)
            {
                lookYaw = UnityEngine.Random.Range(-35f, 35f);
            }
        }

        private void UpdateMovementAndRotation(float dt)
        {
            bool isMoving = currentState == AnimalState.Walk || currentState == AnimalState.Run;
            float targetSpeed = 0f;

            if (currentState == AnimalState.Walk) targetSpeed = walkSpeed;
            else if (currentState == AnimalState.Run) targetSpeed = runSpeed;

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, dt * (isMoving ? 4f : 6f));

            if (currentSpeed > 0.05f)
            {
                Vector3 toDest = currentDestination - transform.position;
                toDest.y = 0f;

                if (toDest.sqrMagnitude > 0.05f)
                {
                    Vector3 moveDir = toDest.normalized;

                    // Alinhamento suave com a rotação de deslocamento e com a inclinação do terreno
                    Vector3 terrainNormal = Vector3.up;
                    if (terrain != null && terrain.terrainData != null)
                    {
                        Vector3 tPos = terrain.transform.position;
                        Vector3 tSize = terrain.terrainData.size;
                        float normX = (transform.position.x - tPos.x) / tSize.x;
                        float normZ = (transform.position.z - tPos.z) / tSize.z;
                        terrainNormal = terrain.terrainData.GetInterpolatedNormal(Mathf.Clamp01(normX), Mathf.Clamp01(normZ));
                    }

                    // Projetar direção no plano da normal do relevo
                    Vector3 projectedForward = Vector3.ProjectOnPlane(moveDir, terrainNormal).normalized;
                    if (projectedForward.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(projectedForward, terrainNormal);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * turnSpeed);
                    }

                    // Translação à frente
                    transform.position += transform.forward * (currentSpeed * dt);
                }
            }

            // Fixar altura no terreno de forma precisa
            if (terrain != null)
            {
                float targetY = terrain.SampleHeight(transform.position) + terrain.transform.position.y + groundHeightOffset;
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(pos.y, targetY, dt * 15f);
                transform.position = pos;
            }
        }

        private void UpdateAnimationWeights(float dt)
        {
            // 1. Se estiver usando Animator com Controller nativo
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat("Speed", currentSpeed);
                animator.SetBool("IsMoving", currentSpeed > 0.1f);
                animator.SetBool("IsRunning", currentSpeed > walkSpeed * 1.3f);
                animator.SetBool("IsGrazing", currentState == AnimalState.Graze);
                return;
            }

            // 2. Se estiver usando PlayableGraph dinâmico com AnimationMixerPlayable
            if (hasPlayableGraph && playableGraph.IsValid())
            {
                float normSpeed = currentSpeed / Mathf.Max(0.1f, runSpeed);
                float walkWeight = 0f;
                float runWeight = 0f;
                float idleWeight = 1.0f;

                if (normSpeed > 0.01f)
                {
                    if (normSpeed < 0.5f)
                    {
                        // Transição suave entre Idle e Walk
                        float t = normSpeed / 0.5f;
                        idleWeight = 1.0f - t;
                        walkWeight = t;
                    }
                    else
                    {
                        // Transição entre Walk e Run
                        float t = (normSpeed - 0.5f) / 0.5f;
                        idleWeight = 0.0f;
                        walkWeight = 1.0f - t;
                        runWeight = t;
                    }
                }

                mixerPlayable.SetInputWeight(0, idleWeight);
                mixerPlayable.SetInputWeight(1, walkWeight);
                mixerPlayable.SetInputWeight(2, runWeight);
            }
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Atualizar transições procedurais suaves
            grazeWeight = Mathf.MoveTowards(grazeWeight, targetGrazeWeight, dt * 2.2f);
            float targetLook = (currentState == AnimalState.LookAround) ? lookYaw : 0f;
            currentLookYaw = Mathf.MoveTowards(currentLookYaw, targetLook, dt * 45f);

            strideCycle += dt * currentSpeed * 4.5f;
            breathPhase += dt * (currentState == AnimalState.Run ? 4.5f : 1.8f);

            // 1. Procedural: Cabeça e Pescoço (Pastar / Olhar ao redor / Aceno do passo)
            if (headBone != null)
            {
                float grazePitch = grazeWeight * 38f;
                float nodPitch = (currentSpeed > 0.1f) ? Mathf.Sin(strideCycle) * 3.5f : 0f;

                Quaternion addRot = Quaternion.Euler(grazePitch + nodPitch, currentLookYaw, 0f);
                headBone.localRotation = initialHeadLocalRot * addRot;
            }

            if (neckBone != null)
            {
                float neckGrazePitch = grazeWeight * 18f;
                neckBone.localRotation = initialNeckLocalRot * Quaternion.Euler(neckGrazePitch, currentLookYaw * 0.5f, 0f);
            }

            // 2. Procedural: Cauda (balanço sinusoidal natural contra moscas e reatividade)
            if (tailBone != null)
            {
                float tailSpeedMult = (currentState == AnimalState.Run) ? 3.5f : 1.2f;
                float tailAmp = (currentState == AnimalState.Run) ? 22f : 12f;
                float tailAngle = Mathf.Sin(Time.time * 2.5f * tailSpeedMult) * tailAmp;

                tailBone.localRotation = initialTailLocalRot * Quaternion.Euler(0f, tailAngle, 0f);
            }

            // 3. Procedural: Respiração Orgânica no Corpo
            if (bodyBone != null)
            {
                float breathScale = 1.0f + Mathf.Sin(breathPhase) * 0.018f;
                bodyBone.localScale = new Vector3(initialBodyLocalScale.x * breathScale,
                                                  initialBodyLocalScale.y * (1f + (breathScale - 1f) * 0.5f),
                                                  initialBodyLocalScale.z);
            }

            // 4. Procedural: Passo das Pernas (Fallback para modelos sem animação esquelética ou primitivos)
            if (!hasPlayableGraph && (animator == null || animator.runtimeAnimatorController == null))
            {
                ApplyProceduralLocomotionFallback(dt);
            }
        }

        private void ApplyProceduralLocomotionFallback(float dt)
        {
            if (currentSpeed > 0.05f)
            {
                // Inclinação e balanço vertical de marcha quadrúpede
                float bob = Mathf.Sin(strideCycle * 2f) * 0.04f * (currentSpeed / walkSpeed);
                float roll = Mathf.Cos(strideCycle) * 2.0f * (currentSpeed / walkSpeed);

                transform.position += transform.up * bob;
                transform.Rotate(0f, 0f, roll, Space.Self);

                // Se houver pernas identificadas, aplicar oscilação frente/trás
                for (int i = 0; i < legBones.Count; i++)
                {
                    var leg = legBones[i];
                    if (leg != null)
                    {
                        float phaseOffset = (i % 2 == 0) ? 0f : Mathf.PI;
                        float legAngle = Mathf.Sin(strideCycle + phaseOffset) * 20f * (currentSpeed / walkSpeed);
                        // Precisa ser *atribuição* a partir da pose de repouso. Com *= a
                        // rotação acumulava ~20° por frame (~1200°/s) e as pernas viravam
                        // hélices; com = a cada frame pisava a pose original do rig.
                        leg.localRotation = GetInitialLegLocalRot(i) * Quaternion.Euler(legAngle, 0f, 0f);
                    }
                }
            }
            else
            {
                // Respiração sutil em repouso
                float idleBob = Mathf.Sin(breathPhase) * 0.005f;
                transform.position += transform.up * idleBob;
            }
        }

        private void OnDestroy()
        {
            if (hasPlayableGraph && playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(homePosition, roamRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentDestination);
            Gizmos.DrawWireSphere(currentDestination, 0.6f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fleeDistance);
        }
    }
}
