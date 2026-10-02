using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectTerra.Core
{
    /// <summary>
    /// Mantém o objeto de foco (câmera ou jogador) próximo da origem (0,0,0) da Unity.
    /// Quando o foco se afasta mais que o limite configurado (Threshold),
    /// todos os objetos raiz da cena são transladados, eliminando jittering de float32 em escala 1:1.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class FloatingOrigin : MonoBehaviour
    {
        public static FloatingOrigin Instance { get; private set; }

        [Header("Focus Target")]
        [SerializeField] private Transform focusTarget;

        [Header("Threshold Settings")]
        [Tooltip("Distância máxima da origem antes de rebater o mundo (em metros)")]
        [SerializeField] private float threshold = 25000.0f; // 25 km

        [Header("Orbit Mode Bypass")]
        [Tooltip("Quando a câmera está em órbita astronômica (> minOrbitAltitude), o rebaseamento é suspenso para manter o centro do planeta na origem.")]
        [SerializeField] private bool bypassInOrbit = true;
        [SerializeField] private float minOrbitAltitude = 50000.0f; // 50 km

        [Header("Global Origin Tracking")]
        [SerializeField] private Vector3d globalOriginOffset = Vector3d.zero;

        public Vector3d GlobalOriginOffset => globalOriginOffset;

        public delegate void OriginRebasedAction(Vector3 offset);
        public static event OriginRebasedAction OnOriginRebased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (focusTarget == null && Camera.main != null)
            {
                focusTarget = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (focusTarget == null)
            {
                if (Camera.main != null)
                    focusTarget = Camera.main.transform;
                else
                    return;
            }

            Vector3 focusPos = focusTarget.position;

            if (bypassInOrbit && focusPos.magnitude > minOrbitAltitude)
            {
                return;
            }

            if (focusPos.sqrMagnitude > threshold * threshold)
            {
                Rebase(focusPos);
            }
        }

        public void SetFocusTarget(Transform newTarget)
        {
            focusTarget = newTarget;
        }

        private void Rebase(Vector3 offset)
        {
            globalOriginOffset += new Vector3d(offset);

            // Desativa temporariamente CharacterControllers para evitar que o PhysX os desloque ou corrompa colisões
            var charControllers = Object.FindObjectsByType<CharacterController>();
            for (int i = 0; i < charControllers.Length; i++)
            {
                if (charControllers[i] != null) charControllers[i].enabled = false;
            }

            // Transladar todos os GameObjects na raiz da cena ativa
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i] != null)
                {
                    rootObjects[i].transform.position -= offset;
                }
            }

            // Força a árvore de física do Unity a sincronizar as novas posições imediatamente
            Physics.SyncTransforms();

            for (int i = 0; i < charControllers.Length; i++)
            {
                if (charControllers[i] != null) charControllers[i].enabled = true;
            }

            // Notificar sistemas que precisam de ajuste (partículas, trilhas, física)
            OnOriginRebased?.Invoke(offset);
        }

        /// <summary>
        /// Converte posição local do Unity para coordenadas globais de dupla precisão.
        /// </summary>
        public Vector3d LocalToGlobal(Vector3 localPosition)
        {
            return globalOriginOffset + new Vector3d(localPosition);
        }

        /// <summary>
        /// Converte coordenadas globais de dupla precisão para posição local no espaço da Unity.
        /// </summary>
        public Vector3 GlobalToLocal(Vector3d globalPosition)
        {
            return (globalPosition - globalOriginOffset).ToVector3();
        }
    }
}
