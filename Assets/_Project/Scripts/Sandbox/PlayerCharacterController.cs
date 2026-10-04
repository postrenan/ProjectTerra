using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public enum VehicleCameraMode
    {
        ThirdPersonClose,
        ThirdPersonFar,
        FirstPersonCockpit,
        BumperOrHood
    }

    /// <summary>
    /// Controlador do jogador em 1ª Pessoa a pé e sistema híbrido (1ª e 3ª Pessoa) em veículos.
    /// Permite caminhar, correr, pular, interagir e alternar visões em veículos [V/C].
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public partial class PlayerCharacterController : MonoBehaviour
    {
        public static PlayerCharacterController Instance { get; private set; }

        [Header("Movimentação a Pé (1ª Pessoa)")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 8.0f;
        public float jumpHeight = 1.2f;
        public float gravity = -20f;

        [Header("Controle de Visão do Mouse")]
        public float mouseSensitivity = 2.0f;
        public float minPitchOnFoot = -85f;
        public float maxPitchOnFoot = 85f;

        [Header("Câmera do Veículo (1ª e 3ª Pessoa)")]
        [Tooltip("Modo de câmera ao dirigir: Cockpit, Capô, 3ª Pessoa Perto/Far. Alternável via [V] ou [C]")]
        public VehicleCameraMode vehicleCameraMode = VehicleCameraMode.ThirdPersonClose;
        public float vehicleThirdPersonDistance = 6.0f;

        [Header("Lanterna do Personagem [L]")]
        public bool isFlashlightOn = false;
        private Light flashlight;

        [Header("Ponto da Câmera")]
        public Transform cameraFollowPoint;

        [Header("Interação com Veículos")]
        public float interactionRadius = 4.0f;
        public LayerMask vehicleLayer = ~0;

        [Header("Estado Atual")]
        public bool isDriving = false;
        public VehicleController currentVehicle;

        [Header("Dinâmica de Água e Natação")]
        public bool isSwimming { get; private set; } = false;
        public bool isWading { get; private set; } = false;
        public float swimSpeed = 3.6f;
        public float swimFastSpeed = 5.2f;
        public float waterBuoyancy = 8.5f;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;

        // Ângulos a pé (1ª pessoa)
        private float yaw = 0f;
        private float pitch = 0f;

        // Ângulos em veículos
        private float vehicleYawLook = 0f;      // Livre visão em 1ª pessoa no cockpit
        private float vehiclePitchLook = 0f;
        private float thirdPersonYaw = 0f;      // Órbita em 3ª pessoa
        private float thirdPersonPitch = 15f;

        private Camera activeCamera;

        public VehicleController nearbyVehicle { get; private set; }

        private void Awake()
        {
            Instance = this;
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            activeCamera = Camera.main;
            if (activeCamera == null)
            {
                var camObj = new GameObject("SandboxMainCamera");
                camObj.tag = "MainCamera";
                activeCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                Debug.Log($"[PlayerCharacterController] Nova SandboxMainCamera criada. Jogador em: {transform.position}");
            }
            else
            {
                Debug.Log($"[PlayerCharacterController] Usando Camera.main existente: '{activeCamera.name}' em {activeCamera.transform.position}. Jogador em: {transform.position}");
            }

            activeCamera.nearClipPlane = 0.75f;
            activeCamera.farClipPlane = 45000f;

            yaw = transform.eulerAngles.y;
            pitch = 0f;

            // Informar o FloatingOrigin para rastrear o jogador ao invés da câmera
            var fo = ProjectTerra.Core.FloatingOrigin.Instance;
            if (fo != null)
            {
                fo.SetFocusTarget(transform);
                Debug.Log("[PlayerCharacterController] FloatingOrigin atualizado para rastrear o jogador.");
            }

            // Posicionar câmera imediatamente nos olhos do jogador (evitar frame azul na abertura)
            if (cameraFollowPoint != null)
            {
                activeCamera.transform.position = cameraFollowPoint.position;
                activeCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                Debug.Log($"[PlayerCharacterController] Câmera posicionada imediatamente em: {activeCamera.transform.position}");
            }

            // Configurar malhas do jogador para modo 1ª pessoa (somente sombras para não obstruir visão)
            SetupFirstPersonMeshes();
        }

        private void SetupFirstPersonMeshes()
        {
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                rend.enabled = true;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }

        public bool IsInputBlocked()
        {
            if (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen))
                return true;
            if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpenOrJustClosed)
                return true;
            return false;
        }

        private void Update()
        {
            HandleCursorLock();

            // Interrompe movimentação e atalhos se o Menu de Opções, Mapa Regional ou Console de Comandos estiver aberto
            if (IsInputBlocked())
            {
                return;
            }

            if (isDriving)
            {
                UpdateInVehicle();
            }
            else
            {
                UpdateOnFoot();
            }
        }

        private void LateUpdate()
        {
            // Não rotaciona a câmera se menus, mapa ou console estiverem com foco
            if (IsInputBlocked())
            {
                return;
            }

            UpdateCamera();
        }

        private void HandleCursorLock()
        {
            // Se o Console, Mapa Regional ou Menu de Opções estiver aberto, mantém o cursor liberado
            if (IsInputBlocked())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (TerraInput.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (TerraInput.GetKeyDown(Key.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void UpdateOnFoot()
        {
            // Consultar água e dinâmicas
            bool inWater = false;
            WaterInfo waterInfo = default;
            if (WaterSystem.Instance != null && WaterSystem.Instance.GetWaterInfo(transform.position, out waterInfo))
            {
                inWater = waterInfo.isInWater;
            }

            float submersion = inWater ? (waterInfo.surfaceY - transform.position.y) : 0f;
            bool wasSwimming = isSwimming;
            isSwimming = inWater && submersion > 1.25f;
            isWading = inWater && submersion > 0.15f && !isSwimming;

            if (isSwimming && !wasSwimming)
            {
                SandboxHUD.Instance?.ShowToast("🏊 Nadando na água [Espaço: Subir / Ctrl: Mergulhar]", 3.0f);
            }

            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0 && !isSwimming)
            {
                velocity.y = -2f;
            }

            // Entrada do Mouse para Visão em 1ª Pessoa
            float mouseX = TerraInput.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = TerraInput.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitchOnFoot, maxPitchOnFoot);

            // O corpo do jogador rotaciona diretamente com o yaw da câmera
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Entradas de movimentação
            float horizontal = TerraInput.GetAxis("Horizontal");
            float vertical = TerraInput.GetAxis("Vertical");

            if (isSwimming)
            {
                // MODO NATAÇÃO: Movimentação tridimensional suave no fluido
                Vector3 camForward = activeCamera != null ? activeCamera.transform.forward : transform.forward;
                Vector3 camRight = activeCamera != null ? activeCamera.transform.right : transform.right;

                Vector3 swimDir = camForward * vertical + camRight * horizontal;

                // Nado vertical: Espaço para dar braçadas para cima, Ctrl/C para mergulhar
                float verticalSwim = 0f;
                if (TerraInput.GetButton("Jump") || TerraInput.GetKey(Key.Space)) verticalSwim += 1.0f;
                if (TerraInput.GetKey(Key.LeftCtrl) || TerraInput.GetKey(Key.C)) verticalSwim -= 1.0f;

                swimDir.y += verticalSwim * 0.9f;
                if (swimDir.magnitude > 1f) swimDir.Normalize();

                float currentSwimSpeed = TerraInput.GetKey(Key.LeftShift) ? swimFastSpeed : swimSpeed;
                controller.Move(swimDir * (currentSwimSpeed * Time.deltaTime));

                // Empuxo natural de flutuação hidrostática
                float targetWaterSurfaceY = waterInfo.surfaceY - 1.25f; // Cabeça do jogador na superfície
                float heightDelta = targetWaterSurfaceY - transform.position.y;

                if (heightDelta > 0.05f)
                {
                    // Abaixo da superfície ideal: empuxo empurra o jogador para cima
                    velocity.y = Mathf.Lerp(velocity.y, Mathf.Clamp(heightDelta * waterBuoyancy, 0.4f, 4.5f), Time.deltaTime * 3.5f);
                }
                else if (heightDelta < -0.2f)
                {
                    // Acima da água: desce suavemente para a linha d'água
                    velocity.y = Mathf.Lerp(velocity.y, -3.5f, Time.deltaTime * 3.0f);
                }
                else
                {
                    // Treading water: oscilação suave acompanhando a onda
                    velocity.y = Mathf.Lerp(velocity.y, 0f, Time.deltaTime * 6.0f);
                }

                // Deslocamento por correnteza fluvial ou marítima
                Vector3 totalMove = (velocity + waterInfo.flowVelocity) * Time.deltaTime;
                controller.Move(totalMove);
            }
            else
            {
                // MODO TERRESTRE / VADEAMENTO
                Vector3 moveDir = transform.forward * vertical + transform.right * horizontal;
                if (moveDir.magnitude > 1f) moveDir.Normalize();

                float speed = TerraInput.GetKey(Key.LeftShift) ? sprintSpeed : walkSpeed;
                if (isWading)
                {
                    // Arrasto hidrodinâmico desacelera o caminhar na água rasa
                    float wadingDrag = Mathf.Lerp(0.85f, 0.45f, Mathf.Clamp01(submersion / 1.25f));
                    speed *= wadingDrag;
                }

                controller.Move(moveDir * (speed * Time.deltaTime));

                // Pulo
                if (TerraInput.GetButtonDown("Jump") && isGrounded)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }

                // Gravidade
                velocity.y += gravity * Time.deltaTime;
                Vector3 totalMove = velocity;
                if (isWading)
                {
                    totalMove += waterInfo.flowVelocity * (submersion / 1.25f);
                }
                controller.Move(totalMove * Time.deltaTime);
            }

            // Proteção contra queda no vazio / perda de colisão do terreno
            float groundH = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.GetTerrainHeight(transform.position) : 0f;
            if (transform.position.y < groundH - 1.5f)
            {
                controller.enabled = false;
                transform.position = new Vector3(transform.position.x, groundH + 1.2f, transform.position.z);
                velocity.y = 0f;
                controller.enabled = true;
            }

            // Verificar veículos próximos
            CheckNearbyVehicles();

            // Tecla L para alternar lanterna
            if (TerraInput.GetKeyDown(Key.L))
            {
                ToggleFlashlight();
            }

            // Tecla E para embarcar
            if (TerraInput.GetKeyDown(Key.E) && nearbyVehicle != null)
            {
                EnterVehicle(nearbyVehicle);
            }
        }

        private void CheckNearbyVehicles()
        {
            nearbyVehicle = null;
            Collider[] hits = Physics.OverlapSphere(transform.position, interactionRadius, vehicleLayer);
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var vehicle = hit.GetComponentInParent<VehicleController>();
                if (vehicle != null)
                {
                    float dist = Vector3.Distance(transform.position, vehicle.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        nearbyVehicle = vehicle;
                    }
                }
            }
        }

        public void EnterVehicle(VehicleController vehicle)
        {
            if (vehicle == null) return;

            currentVehicle = vehicle;
            isDriving = true;
            vehicle.isPlayerDriven = true;

            // Alinhar câmera do veículo
            vehicleYawLook = 0f;
            vehiclePitchLook = 0f;
            thirdPersonYaw = vehicle.transform.eulerAngles.y;
            thirdPersonPitch = 15f;

            // Distância adequada ao tipo de veículo para 3ª pessoa
            switch (vehicle.category)
            {
                case VehicleCategory.Tractor: vehicleThirdPersonDistance = 6.0f; break;
                case VehicleCategory.Truck: vehicleThirdPersonDistance = 8.5f; break;
                case VehicleCategory.Plane: vehicleThirdPersonDistance = 11.0f; break;
                case VehicleCategory.Boat: vehicleThirdPersonDistance = 8.5f; break;
            }

            // Oculta e desativa o collider do personagem
            controller.enabled = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }

            transform.SetParent(vehicle.transform);
            transform.localPosition = vehicle.driverSeatPoint != null ? vehicle.driverSeatPoint.localPosition : Vector3.zero;

            Debug.Log($"[Player] Embarcou no veículo: {vehicle.vehicleName}");
        }

        public void ExitVehicle()
        {
            if (currentVehicle == null) return;

            Vector3 exitPos = currentVehicle.exitPoint != null ?
                currentVehicle.exitPoint.position : currentVehicle.transform.position + currentVehicle.transform.right * -2.5f + Vector3.up * 0.5f;

            currentVehicle.isPlayerDriven = false;
            transform.SetParent(null);
            transform.position = exitPos;

            // Retomar orientação da câmera a pé a partir da direção do veículo
            yaw = currentVehicle.transform.eulerAngles.y;
            pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            SetupFirstPersonMeshes();
            controller.enabled = true;

            isDriving = false;
            currentVehicle = null;

            Debug.Log("[Player] Desembarcou do veículo.");
        }

        #region Lanterna do Personagem [L]

        public void ToggleFlashlight()
        {
            SetFlashlight(!isFlashlightOn);
        }

        public void SetFlashlight(bool state)
        {
            isFlashlightOn = state;
            if (flashlight == null)
            {
                CreateFlashlight();
            }
            if (flashlight != null)
            {
                flashlight.enabled = isFlashlightOn;
            }
            SandboxHUD.Instance?.ShowToast(isFlashlightOn ? "🔦 Lanterna: Ligada" : "🔦 Lanterna: Desligada", 1.8f);
        }

        private void CreateFlashlight()
        {
            var obj = new GameObject("Player_Flashlight");
            Transform parent = cameraFollowPoint != null ? cameraFollowPoint : transform;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = new Vector3(0.25f, -0.1f, 0.2f);
            obj.transform.localRotation = Quaternion.identity;

            flashlight = obj.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.spotAngle = 55f;
            flashlight.range = 55f;
            flashlight.color = new Color(1f, 0.98f, 0.92f);
            flashlight.intensity = 2.4f;
            flashlight.shadows = LightShadows.Soft;
            flashlight.enabled = isFlashlightOn;
        }

        #endregion

        #region Teletransporte / Reposicionamento

        public void TeleportTo(Vector3 targetPosition)
        {
            if (isDriving)
            {
                ExitVehicle();
            }

            if (controller != null)
            {
                controller.enabled = false;
                transform.position = targetPosition;
                velocity = Vector3.zero;
                controller.enabled = true;
            }
            else
            {
                transform.position = targetPosition;
            }

            if (activeCamera != null && cameraFollowPoint != null)
            {
                activeCamera.transform.position = cameraFollowPoint.position;
            }
        }

        #endregion
    }
}
