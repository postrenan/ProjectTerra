using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;

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
        private double targetDistance = 18000000.0;

        private Vector3 rotationVelocity;
        private Vector2 leftMouseDownPos;
        private bool isLeftDragging = false;
        private const float DragThreshold = 8.0f;

        private void Start()
        {
            if (target == null)
            {
                var planet = FindAnyObjectByType<ProjectTerra.Planet.CubeSpherePlanet>();
                if (planet != null)
                {
                    target = planet.transform;
                }
            }

            Vector3 angles = transform.eulerAngles;
            currentYaw = targetYaw = angles.y;
            currentPitch = targetPitch = angles.x;
            targetDistance = currentDistance;
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraTransform();
        }

        private bool isDiving = false;

        private void HandleInput()
        {
            if (isDiving) return;

            double altitude = Mathf.Max((float)(currentDistance - planetRadius), 1.0f);

            // Adapta a velocidade de rotação à altitude: rápida no espaço, precisa e suave a 50m
            float altitudeFactor = Mathf.Clamp01((float)(altitude / 500000.0)); // 500 km
            float activeRotationSpeed = Mathf.Lerp(15.0f, baseRotationSpeed, altitudeFactor);

            bool isOverUI = ProjectTerra.Planet.PlanetInteractionController.Instance != null && 
                            ProjectTerra.Planet.PlanetInteractionController.Instance.IsPointerOverUI();

            // Gerenciar arrasto com botão esquerdo (para não conflitar com clique de seleção)
            if (TerraInput.GetMouseButtonDown(0))
            {
                leftMouseDownPos = TerraInput.MousePosition;
                isLeftDragging = false;
            }

            if (TerraInput.GetMouseButton(0) && !isOverUI)
            {
                if (!isLeftDragging && Vector2.Distance(leftMouseDownPos, TerraInput.MousePosition) > DragThreshold)
                {
                    isLeftDragging = true;
                }
            }

            if (TerraInput.GetMouseButtonUp(0))
            {
                isLeftDragging = false;
            }

            // Rotação orbital:
            // - Botão direito (1) ou meio (2) sempre orbitam livremente
            // - Botão esquerdo (0) só orbita se for explicitamente arrastado além de 8 pixels
            bool shouldOrbit = !isOverUI && (TerraInput.GetMouseButton(1) || TerraInput.GetMouseButton(2) || isLeftDragging);

            if (shouldOrbit)
            {
                float mouseX = TerraInput.GetAxis("Mouse X");
                float mouseY = TerraInput.GetAxis("Mouse Y");

                targetYaw += mouseX * activeRotationSpeed * Time.deltaTime * 50f;
                targetPitch -= mouseY * activeRotationSpeed * Time.deltaTime * 50f;
                targetPitch = Mathf.Clamp(targetPitch, -89.0f, 89.0f);
            }

            // Zoom exponencial com a roda de scroll do mouse (Google Earth style)
            float scroll = TerraInput.GetAxis("Mouse ScrollWheel");

            // Suporte adicional a teclas para zoom (+ / -, Numpad+, Numpad-, PageUp / PageDown)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.equalsKey.isPressed || kb.numpadPlusKey.isPressed || kb.pageUpKey.isPressed)
                    scroll += 2.5f * Time.deltaTime;
                if (kb.minusKey.isPressed || kb.numpadMinusKey.isPressed || kb.pageDownKey.isPressed)
                    scroll -= 2.5f * Time.deltaTime;
            }

            if (Mathf.Abs(scroll) > 0.0001f && !isOverUI)
            {
                double currentAlt = System.Math.Max(targetDistance - planetRadius, minAltitude);
                // Zoom multiplicativo exponencial:
                // exp(-scroll * zoomSensitivity) garante zoom perfeitamente simétrico, suave e proporcional à altitude
                double newAlt = currentAlt * System.Math.Exp(-scroll * zoomSensitivity);
                double minDistance = planetRadius + minAltitude;
                double maxDistance = planetRadius * 6.0; // ~38.000 km no espaço profundo

                targetDistance = System.Math.Clamp(planetRadius + newAlt, minDistance, maxDistance);
            }
        }

        private void UpdateCameraTransform()
        {
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref rotationVelocity.y, smoothTime);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref rotationVelocity.x, smoothTime);

            // Suavização contínua de distância em double (sem perda de precisão astronômica nem no solo)
            if (!isDiving)
            {
                double t = 1.0 - System.Math.Exp(-14.0 * Time.deltaTime);
                currentDistance += (targetDistance - currentDistance) * t;
            }

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
            targetDistance = currentDistance;
        }

        public void FocusOnPoint(Vector3 worldPoint, double desiredAltitude = -1)
        {
            if (isDiving) return;

            Vector3 center = target != null ? target.position : Vector3.zero;
            Vector3 dir = (worldPoint - center).normalized;
            if (dir.sqrMagnitude < 0.001f) return;

            // Alinha a câmera para olhar diretamente para o ponto centralizando a região na tela
            Quaternion rot = BuildLookRotation(-dir);
            Vector3 euler = rot.eulerAngles;

            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;
            targetPitch = Mathf.Clamp(pitch, -89f, 89f);
            targetYaw = euler.y;

            if (desiredAltitude > 0)
            {
                targetDistance = System.Math.Clamp(planetRadius + desiredAltitude, planetRadius + minAltitude, planetRadius * 6.0);
            }
        }

        /// <summary>
        /// Quaternion de look com vetor de referência seguro. Quaternion.LookRotation
        /// devolve identity e loga erro quando <paramref name="forward"/> é paralelo ao
        /// up — exatamente o caso de focar ou mergulhar para uma região polar (dir ~ ±up).
        /// A câmera então saltava para pitch 0 / yaw 0, ou seja, o equador. Aqui usa-se
        /// forward como referência quando dir está praticamente vertical.
        /// </summary>
        private static Quaternion BuildLookRotation(Vector3 forward)
        {
            Vector3 upRef = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.999f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(forward, upRef);
        }

        public void DiveTowardsPoint(Vector3 worldPoint, float duration, System.Action onComplete = null)
        {
            if (isDiving) return;
            StartCoroutine(DiveCoroutine(worldPoint, duration, onComplete));
        }

        private System.Collections.IEnumerator DiveCoroutine(Vector3 worldPoint, float duration, System.Action onComplete)
        {
            isDiving = true;
            Vector3 center = target != null ? target.position : Vector3.zero;
            Vector3 dir = (worldPoint - center).normalized;

            Quaternion rot = BuildLookRotation(-dir);
            Vector3 euler = rot.eulerAngles;
            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;
            float targetPitchDeg = Mathf.Clamp(pitch, -89f, 89f);
            float targetYawDeg = euler.y;

            double startDist = currentDistance;
            double endDist = planetRadius + 15000.0; // 15km acima da superfície

            float startPitch = targetPitch;
            float startYaw = targetYaw;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                targetPitch = Mathf.LerpAngle(startPitch, targetPitchDeg, t);
                targetYaw = Mathf.LerpAngle(startYaw, targetYawDeg, t);
                currentDistance = System.Math.Max(planetRadius + 1000.0, startDist + (endDist - startDist) * t);
                targetDistance = currentDistance;

                yield return null;
            }

            isDiving = false;
            onComplete?.Invoke();
        }
    }
}
