using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Componente do implemento agrícola: Arado Subsolador de Discos.
    /// Permite engatar e desengatar [G] da traseira do trator,
    /// levantar e abaixar os discos [X] (Modo Transporte vs Modo Aração),
    /// e arar o terreno em tempo real (pintando a camada "Solo Arado" e suprimindo grama alta)
    /// com partículas de terra e telemetria de área arada.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlowImplement : OriginRebasedBehaviour
    {
        public static readonly List<PlowImplement> AllPlows = new List<PlowImplement>();

        [Header("Estado do Engate")]
        public bool isHitched = false;
        public VehicleController hitchedVehicle;
        public float hitchDistanceThreshold = 4.2f;
        public float towBarLength = 2.4f;

        [Header("Estado dos Discos")]
        public bool isLowered = false; // true = discos cravados no solo arando; false = levantado (transporte)
        public float plowWorkingWidthMeters = 2.6f;
        public float totalAreaPlowedM2 = 0f;

        [Header("Referências Visuais")]
        public Transform tongueCouplerPoint;   // Ponta do olhal de engate triangular
        public Transform discGangPivot;        // Sub-estrutura que tomba os discos para baixo
        public Transform[] discBlades;         // Lâminas de disco que giram ao rodar
        public ParticleSystem dirtParticles;   // Partículas de poeira e torrões de terra revolvida

        private Rigidbody rb;
        private Collider mainCollider;
        private PlowFurrowManager furrowManager;
        private float lastPlowTime = 0f;
        private Vector3 lastPlowPos;
        private float currentPivotAngle = 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            mainCollider = GetComponent<Collider>();
            if (!AllPlows.Contains(this)) AllPlows.Add(this);
        }

        private void OnDestroy()
        {
            AllPlows.Remove(this);
        }

        private void Start()
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 850f;
            rb.linearDamping = 0.8f;
            rb.angularDamping = 3.0f;
            lastPlowPos = transform.position;

            furrowManager = GetComponent<PlowFurrowManager>();
            if (furrowManager == null) furrowManager = gameObject.AddComponent<PlowFurrowManager>();
            furrowManager.Initialize(transform, discGangPivot, plowWorkingWidthMeters);
        }

        /// <summary>
        /// Rebase do FloatingOrigin: lastPlowPos é uma posição world em cache. Sem
        /// deslocá-la, o rebase de 25 km fazia distSinceLast ≈ 25000 m e somava
        /// ~65.000 m² de área arada falsa num único passo, visível no HUD.
        /// </summary>
        protected override void OnOriginRebased(Vector3 offset)
        {
            lastPlowPos -= offset;
        }

        private void Update()
        {
            HandleInputs();
            UpdateVisuals();
        }

        private void HandleInputs()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            // Bloqueio de entrada se console ou mapas abertos
            if ((InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpenOrJustClosed) ||
                (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen)))
            {
                return;
            }

            // Caso 1: Jogador está dirigindo o trator
            if (player.isDriving && player.currentVehicle != null && player.currentVehicle.category == VehicleCategory.Tractor)
            {
                var tractor = player.currentVehicle;

                if (isHitched && hitchedVehicle == tractor)
                {
                    // Tecla G: Desengatar arado
                    if (TerraInput.GetKeyDown(Key.G))
                    {
                        Unhitch();
                    }

                    // Tecla X: Levantar / Abaixar arado
                    if (TerraInput.GetKeyDown(Key.X))
                    {
                        ToggleLower();
                    }
                }
                else if (!isHitched)
                {
                    // Trator próximo do engate deste arado
                    Vector3 tractorHitchPos = GetTractorHitchAnchor(tractor);
                    Vector3 myTonguePos = tongueCouplerPoint != null ? tongueCouplerPoint.position : transform.position + transform.forward * 1.8f;
                    float dist = Vector3.Distance(tractorHitchPos, myTonguePos);

                    if (dist <= hitchDistanceThreshold)
                    {
                        if (TerraInput.GetKeyDown(Key.G))
                        {
                            HitchTo(tractor);
                        }
                    }
                }
            }
            // Caso 2: Jogador a pé próximo do engate
            else if (!player.isDriving)
            {
                Vector3 playerPos = player.transform.position;
                Vector3 myTonguePos = tongueCouplerPoint != null ? tongueCouplerPoint.position : transform.position;
                float distToPlayer = Vector3.Distance(playerPos, myTonguePos);

                if (distToPlayer <= 3.8f)
                {
                    if (isHitched)
                    {
                        if (TerraInput.GetKeyDown(Key.G))
                        {
                            Unhitch();
                        }
                    }
                    else
                    {
                        // Procura trator próximo
                        var nearestTractor = FindNearbyTractor(myTonguePos, hitchDistanceThreshold);
                        if (nearestTractor != null && TerraInput.GetKeyDown(Key.G))
                        {
                            HitchTo(nearestTractor);
                        }
                    }
                }
            }
        }

        public void HitchTo(VehicleController tractor)
        {
            if (tractor == null) return;

            isHitched = true;
            hitchedVehicle = tractor;
            rb.isKinematic = true; // No reboque controlado, movimento cinemático garante estabilidade absoluta sem oscilação
            if (mainCollider != null) mainCollider.enabled = false;

            SandboxHUD.Instance?.ShowToast("🚜 Arado ENGATADO ao trator! Pressione [X] para abaixar os discos e arar.", 3.0f);
            Debug.Log($"[PlowImplement] Arado engatado com sucesso ao trator '{tractor.vehicleName}'.");
        }

        public void Unhitch()
        {
            if (!isHitched) return;

            isHitched = false;
            hitchedVehicle = null;
            isLowered = false; // Ao desengatar, recolhe
            rb.isKinematic = false;
            if (mainCollider != null) mainCollider.enabled = true;

            if (dirtParticles != null && dirtParticles.isPlaying)
            {
                dirtParticles.Stop();
            }
            furrowManager?.StopFurrow();

            SandboxHUD.Instance?.ShowToast("🚜 Arado DESENGATADO no local.", 2.5f);
            Debug.Log("[PlowImplement] Arado desengatado.");
        }

        public void ToggleLower()
        {
            if (!isHitched) return;

            isLowered = !isLowered;

            if (isLowered)
            {
                SandboxHUD.Instance?.ShowToast("🌱 Arado ABAIXADO — Modo de Aração ATIVADO! Ande com o trator para arar o solo.", 3.0f);
            }
            else
            {
                SandboxHUD.Instance?.ShowToast("🚜 Arado LEVANTADO — Modo de Transporte.", 2.2f);
                if (dirtParticles != null && dirtParticles.isPlaying)
                {
                    dirtParticles.Stop();
                }
            }
        }

        private void FixedUpdate()
        {
            if (isHitched && hitchedVehicle != null)
            {
                FixedUpdateTowedMotion();
                FixedUpdatePlowing();
            }
        }

        private void FixedUpdateTowedMotion()
        {
            Vector3 tractorHitchPos = GetTractorHitchAnchor(hitchedVehicle);

            // Vetor de tração do arado apontando para o ponto de engate do trator
            Vector3 toHitch = tractorHitchPos - transform.position;
            toHitch.y = 0f;

            if (toHitch.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toHitch.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 14f);
            }

            // Posição desejada fixando a barra de reboque atrás do engate
            Vector3 targetPos = tractorHitchPos - transform.forward * towBarLength;

            // Clamping preciso na superfície do terreno
            if (RegionalSandboxManager.Instance != null)
            {
                float groundY = RegionalSandboxManager.Instance.GetTerrainHeight(targetPos);
                targetPos.y = groundY + 0.15f;
            }

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.fixedDeltaTime * 28f);
        }

        private void FixedUpdatePlowing()
        {
            if (!isLowered || hitchedVehicle == null)
            {
                if (dirtParticles != null && dirtParticles.isPlaying) dirtParticles.Stop();
                furrowManager?.StopFurrow();
                return;
            }

            float speedKmh = hitchedVehicle.currentSpeedKmh;

            // Para arar, o trator deve estar em movimento a mais de 1.2 km/h
            if (speedKmh < 1.2f)
            {
                if (dirtParticles != null && dirtParticles.isPlaying) dirtParticles.Stop();
                furrowManager?.StopFurrow();
                return;
            }

            // Ativa partículas de terra
            if (dirtParticles != null && !dirtParticles.isPlaying)
            {
                dirtParticles.Play();
            }

            Vector3 plowCenter = discGangPivot != null ? discGangPivot.position : transform.position;

            // Gera faixas e sulcos 3D de terra fértil revolvida e suprime grama nativa
            furrowManager?.AddFurrowStep(plowCenter);

            // Incrementa área arada (largura de trabalho * distância percorrida)
            float distSinceLast = Vector3.Distance(transform.position, lastPlowPos);
            if (distSinceLast >= 0.75f)
            {
                totalAreaPlowedM2 += plowWorkingWidthMeters * distSinceLast;
                lastPlowPos = transform.position;
            }
        }

        private void UpdateVisuals()
        {
            // Animação hidráulica do basculamento dos discos (Levantado vs Abaixado)
            float targetAngle = isLowered ? 20f : 0f;
            currentPivotAngle = Mathf.MoveTowards(currentPivotAngle, targetAngle, Time.deltaTime * 35f);

            if (discGangPivot != null)
            {
                // Os discos ficam em Y local negativo dentro do pivô (cerca de -0.47).
                // Uma rotação de +currentPivotAngle em torno de +X levanta um ponto em
                // -Y, então o sinal estava invertido e "abaixar os discos" erguia o
                // conjunto em ~3 cm. O ângulo precisa ser negativo.
                discGangPivot.localRotation = Quaternion.Euler(-currentPivotAngle, 0f, 0f);
            }

            // Rotação contínua dos discos quando o implemento roda
            float speed = isHitched && hitchedVehicle != null ? hitchedVehicle.currentSpeedKmh : rb.linearVelocity.magnitude * 3.6f;
            if (speed > 0.4f && discBlades != null)
            {
                float rotAmount = speed * 180f * Time.deltaTime;
                for (int i = 0; i < discBlades.Length; i++)
                {
                    if (discBlades[i] != null)
                    {
                        discBlades[i].Rotate(Vector3.right, rotAmount, Space.Self);
                    }
                }
            }
        }

        public static Vector3 GetTractorHitchAnchor(VehicleController tractor)
        {
            if (tractor == null) return Vector3.zero;
            if (tractor.rearHitchPoint != null) return tractor.rearHitchPoint.position;
            return tractor.transform.position - tractor.transform.forward * 2.1f + tractor.transform.up * 0.45f;
        }

        public static VehicleController FindNearbyTractor(Vector3 position, float maxRadius = 4.5f)
        {
            if (VehicleManager.Instance != null)
            {
                foreach (var v in VehicleManager.Instance.possessedVehicles)
                {
                    if (v != null && v.category == VehicleCategory.Tractor)
                    {
                        Vector3 anchor = GetTractorHitchAnchor(v);
                        if (Vector3.Distance(position, anchor) <= maxRadius)
                            return v;
                    }
                }
            }

            // Fallback caso não esteja na frota
            var tractors = UnityEngine.Object.FindObjectsByType<VehicleController>();
            foreach (var v in tractors)
            {
                if (v != null && v.category == VehicleCategory.Tractor)
                {
                    Vector3 anchor = GetTractorHitchAnchor(v);
                    if (Vector3.Distance(position, anchor) <= maxRadius)
                        return v;
                }
            }
            return null;
        }

        public static PlowImplement GetHitchedPlow(VehicleController tractor)
        {
            if (tractor == null) return null;
            for (int i = 0; i < AllPlows.Count; i++)
            {
                var p = AllPlows[i];
                if (p != null && p.isHitched && p.hitchedVehicle == tractor)
                    return p;
            }
            return null;
        }

        public static PlowImplement FindNearestPlow(Vector3 position, float maxRadius = 15f)
        {
            PlowImplement nearest = null;
            float minDist = float.MaxValue;

            for (int i = 0; i < AllPlows.Count; i++)
            {
                var p = AllPlows[i];
                if (p == null) continue;
                float d = Vector3.Distance(position, p.transform.position);
                if (d < minDist && d <= maxRadius)
                {
                    minDist = d;
                    nearest = p;
                }
            }
            return nearest;
        }
    }
}
