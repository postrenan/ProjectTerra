using UnityEngine;

namespace ProjectTerra.Cameras
{
    /// <summary>
    /// Câmera orbital astronômica contínua com zoom exponencial e rotação adaptativa à altitude.
    /// Permite navegar suavemente de 20.000 km (espaço) até 50 metros (superfície da Terra).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCameraController : MonoBehaviour
    {
        [Header("Target & Focus")]
        [SerializeField] private Transform target;

        [Header("Orbit Controls")]
        [SerializeField] private float baseRotationSpeed = 120.0f;
        [SerializeField] private float smoothTime = 0.08f;

        [Header("Distance & Zoom (Escala 50m)")]
        [Tooltip("Altitude mínima da superfície em metros (50m para inspeção detalhada do solo)")]
        [SerializeField] private double minAltitude = 50.0;
        [SerializeField] private double planetRadius = 6371000.0;
        [SerializeField] private double currentDistance = 18000000.0; // 18.000 km inicial
        [SerializeField] private float zoomSensitivity = 0.28f;

        private float currentYaw = 0.0f;
        private float currentPitch = 20.0f;
        private float targetYaw = 0.0f;
        private float targetPitch = 20.0f;

        private Vector3 rotationVelocity;
        private Vector2 leftMouseDownPos;
        private bool isLeftDragging = false;
        private const float DragThreshold = 8.0f;

        private void Start()
        {
            if (target == null)
            {
                var planet = FindFirstObjectByType<ProjectTerra.Planet.CubeSpherePlanet>();
                if (planet != null)
                {
                    target = planet.transform;
                }
            }

            Vector3 angles = transform.eulerAngles;
            currentYaw = targetYaw = angles.y;
            currentPitch = targetPitch = angles.x;
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private void HandleInput()
        {
            double altitude = Mathf.Max((float)(currentDistance - planetRadius), 1.0f);

            // Adapta a velocidade de rotação à altitude: rápida no espaço, precisa e suave a 50m
            float altitudeFactor = Mathf.Clamp01((float)(altitude / 500000.0)); // 500 km
            float activeRotationSpeed = Mathf.Lerp(15.0f, baseRotationSpeed, altitudeFactor);

            bool isOverUI = ProjectTerra.Planet.PlanetInteractionController.Instance != null && 
                            ProjectTerra.Planet.PlanetInteractionController.Instance.IsPointerOverUI();

            // Gerenciar arrasto com botão esquerdo (para não conflitar com clique de seleção)
            if (Input.GetMouseButtonDown(0))
            {
                leftMouseDownPos = Input.mousePosition;
                isLeftDragging = false;
            }

            if (Input.GetMouseButton(0) && !isOverUI)
            {
                if (!isLeftDragging && Vector2.Distance(leftMouseDownPos, (Vector2)Input.mousePosition) > DragThreshold)
                {
                    isLeftDragging = true;
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isLeftDragging = false;
            }

            // Rotação orbital:
            // - Botão direito (1) ou meio (2) sempre orbitam livremente
            // - Botão esquerdo (0) só orbita se for explicitamente arrastado além de 8 pixels
            bool shouldOrbit = !isOverUI && (Input.GetMouseButton(1) || Input.GetMouseButton(2) || isLeftDragging);

            if (shouldOrbit)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                targetYaw += mouseX * activeRotationSpeed * Time.deltaTime * 50f;
                targetPitch -= mouseY * activeRotationSpeed * Time.deltaTime * 50f;
                targetPitch = Mathf.Clamp(targetPitch, -89.0f, 89.0f);
            }

            // Zoom exponencial com a roda de scroll do mouse (Google Earth style)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                // Velocidade de zoom proporcional à altitude atual (passo mínimo de 5m a 50m de altitude)
                double zoomStep = Mathf.Max((float)(altitude * zoomSensitivity), 5.0f);
                currentDistance -= scroll * zoomStep;

                double minDistance = planetRadius + minAltitude;
                double maxDistance = planetRadius * 6.0; // ~38.000 km no espaço profundo
                if (currentDistance < minDistance)
                    currentDistance = minDistance;
                else if (currentDistance > maxDistance)
                    currentDistance = maxDistance;
            }
        }

        private void UpdateCameraTransform()
        {
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref rotationVelocity.y, smoothTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref rotationVelocity.x, smoothTime);

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 targetPosition = target != null ? target.position : Vector3.zero;

            Vector3 direction = rotation * -Vector3.forward;
            transform.position = targetPosition + direction * (float)currentDistance;
            transform.rotation = rotation;
        }

        public void SetTarget(Transform newTarget, double radius)
        {
            target = newTarget;
            planetRadius = radius;
            currentDistance = radius * 2.5;
        }
    }
}
