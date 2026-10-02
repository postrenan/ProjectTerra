using System;
using UnityEngine;

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
                activeCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            activeCamera.nearClipPlane = 0.2f;

            yaw = transform.eulerAngles.y;
            pitch = 0f;

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

        private void Update()
        {
            HandleCursorLock();

            // Interrompe movimentação se o Menu de Opções ou Mapa Regional estiver aberto
            if (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen))
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
            // Não rotaciona a câmera com o mouse se o cursor estiver livre para o Mapa ou Menu
            if (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen))
            {
                return;
            }

            UpdateCamera();
        }

        private void HandleCursorLock()
        {
            // Se o Mapa Regional ou Menu de Opções estiver aberto, mantém o cursor liberado
            if (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void UpdateOnFoot()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Entrada do Mouse para Visão em 1ª Pessoa
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitchOnFoot, maxPitchOnFoot);

            // O corpo do jogador rotaciona diretamente com o yaw da câmera
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Movimentação em 1ª Pessoa (relativa à direção que o jogador está olhando)
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector3 moveDir = transform.forward * vertical + transform.right * horizontal;
            if (moveDir.magnitude > 1f) moveDir.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            controller.Move(moveDir * (speed * Time.deltaTime));

            // Pulo
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Gravidade
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Verificar veículos próximos
            CheckNearbyVehicles();

            // Tecla L para alternar lanterna
            if (Input.GetKeyDown(KeyCode.L))
            {
                ToggleFlashlight();
            }

            // Tecla E para embarcar
            if (Input.GetKeyDown(KeyCode.E) && nearbyVehicle != null)
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
    }
}
