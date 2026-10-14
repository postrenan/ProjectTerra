using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Componente unificado para objetos interativos dos edifícios modulares (portas, janelas, portões).
    /// Suporta rotação suave sobre a dobradiça (pivot de origem configurado no Blender),
    /// detecção por aproximação do jogador, indicação visual em tela e acionamento por tecla [E] ou [F].
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class InteractiveModularElement : MonoBehaviour
    {
        public enum InteractionType
        {
            HingeDoor,         // Porta com dobradiça lateral
            DoubleCommercial,  // Porta comercial dupla de vidro
            HingeWindow,       // Janela basculante/pivotante
            SlidingGate        // Portão corrediço
        }

        [Header("Configuração de Interação")]
        [SerializeField] private InteractionType elementType = InteractionType.HingeDoor;
        [SerializeField] private string elementName = "Porta";
        [SerializeField] private float openAngle = 85.0f;
        [SerializeField] private Vector3 rotationAxis = Vector3.up; // Eixo Y no Unity (Z no Blender)
        [SerializeField] private float animationSpeed = 4.0f;
        [SerializeField] private float interactionDistance = 2.8f;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private KeyCode alternateKey = KeyCode.F;

        [Header("Estado Inicial")]
        [SerializeField] private bool startOpen = false;

        [Header("Áudio Opcional")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioSource audioSource;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool isOpen = false;
        private Coroutine activeTransition;
        private Transform playerTransform;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1.0f; // 3D sound
                    audioSource.maxDistance = 15f;
                }
            }

            if (startOpen)
            {
                isOpen = true;
                transform.localRotation = openRotation;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= interactionDistance)
            {
                // Verificar input (InputSystem moderno ou KeyCode legado)
                bool pressed = false;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
                        pressed = true;
                }
                else
                {
                    if (Input.GetKeyDown(interactionKey) || Input.GetKeyDown(alternateKey))
                        pressed = true;
                }

                if (pressed)
                {
                    ToggleState();
                }
            }
        }

        private void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }

        public void ToggleState()
        {
            SetState(!isOpen);
        }

        public void SetState(bool open)
        {
            if (isOpen == open && activeTransition == null) return;

            isOpen = open;
            if (activeTransition != null)
            {
                StopCoroutine(activeTransition);
            }
            activeTransition = StartCoroutine(AnimateRotation(isOpen ? openRotation : closedRotation));

            // Tocar som
            if (audioSource != null)
            {
                var clip = isOpen ? openSound : closeSound;
                if (clip != null)
                {
                    audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(clip);
                }
            }
        }

        private IEnumerator AnimateRotation(Quaternion targetRotation)
        {
            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * animationSpeed);
                yield return null;
            }
            transform.localRotation = targetRotation;
            activeTransition = null;
        }

        private void OnGUI()
        {
            if (playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= interactionDistance)
            {
                // Renderizar aviso flutuante / prompt
                Vector3 screenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.0f) : Vector3.zero;
                if (screenPos.z > 0)
                {
                    string actionText = isOpen ? "Fechar" : "Abrir";
                    string prompt = $"[E] {actionText} {elementName}";

                    float width = 220;
                    float height = 36;
                    float x = screenPos.x - width / 2;
                    float y = Screen.height - screenPos.y - height / 2;

                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 0.9f);
                    GUI.Box(new Rect(x, y, width, height), "");

                    var labelStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 13,
                        fontStyle = FontStyle.Bold
                    };
                    labelStyle.normal.textColor = Color.white;

                    GUI.Label(new Rect(x, y, width, height), prompt, labelStyle);
                    GUI.backgroundColor = oldColor;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);

            // Indicador de arco de abertura
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            Vector3 openDir = Quaternion.AngleAxis(openAngle, rotationAxis) * forward;
            Gizmos.DrawRay(origin, forward * 1.0f);
            Gizmos.DrawRay(origin, openDir * 1.0f);
        }
    }
}
