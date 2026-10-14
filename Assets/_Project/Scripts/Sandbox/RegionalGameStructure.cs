using System;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Componente anexado a cada estrutura interativa no mundo 3D.
    /// Monitora a proximidade do jogador (a pé ou dirigindo) e expõe os serviços
    /// comerciais e operacionais da instalação.
    /// </summary>
    public class RegionalGameStructure : MonoBehaviour
    {
        [Header("Definição")]
        public GameStructureType structureType;
        public string customName;
        public float interactionRadius = 35f;

        [Header("Ponto de Interação")]
        public Transform interactionPoint;

        public GameStructureDefinition Definition => GameStructureCatalog.Get(structureType);

        public bool IsPlayerNearby { get; private set; } = false;

        private void OnEnable()
        {
            RegionalStructuresManager.RegisterStructure(this);
        }

        private void OnDisable()
        {
            RegionalStructuresManager.UnregisterStructure(this);
        }

        private void Update()
        {
            CheckPlayerDistance();
        }

        private void CheckPlayerDistance()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null)
            {
                IsPlayerNearby = false;
                return;
            }

            Vector3 checkPos = player.isDriving && player.currentVehicle != null
                ? player.currentVehicle.transform.position
                : player.transform.position;

            Vector3 myPos = interactionPoint != null ? interactionPoint.position : transform.position;

            // Medir distância no plano XZ
            float dx = checkPos.x - myPos.x;
            float dz = checkPos.z - myPos.z;
            float distSqr = dx * dx + dz * dz;

            bool wasNearby = IsPlayerNearby;
            IsPlayerNearby = distSqr <= (interactionRadius * interactionRadius);

            if (IsPlayerNearby && !wasNearby)
            {
                // Entrou no raio
            }
        }

        /// <summary>
        /// Retorna a distância atual do jogador a esta estrutura.
        /// </summary>
        public float GetDistanceToPlayer()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return float.MaxValue;

            Vector3 checkPos = player.isDriving && player.currentVehicle != null
                ? player.currentVehicle.transform.position
                : player.transform.position;

            Vector3 myPos = interactionPoint != null ? interactionPoint.position : transform.position;
            return Vector3.Distance(checkPos, myPos);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;
            Gizmos.DrawWireSphere(center, interactionRadius);
        }
    }
}
