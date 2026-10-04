using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    [Serializable]
    public class CommercialContract
    {
        public string title;
        public string description;
        public string cargoType;
        public float cargoWeightKg;
        public long rewardMoney;
        public int rewardReputation;
        public Vector3 pickupLocation;
        public Vector3 deliveryLocation;
        public string destinationName;
        public bool isCompleted;
    }

    /// <summary>
    /// Gerencia o ciclo comercial e contratos de frete/entrega para o Marco 1.
    /// Monitora pontos de coleta, balizas de entrega e bonifica o jogador, salvando no RegionSaveData.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Contrato Ativo")]
        public CommercialContract currentContract;
        public bool hasActiveContract = false;

        [Header("Gatilhos de Missão")]
        public Transform deliveryPointTrigger;
        public float deliveryRadius = 8.0f;

        public Vector3 deliveryTargetPosition => (currentContract != null) ? currentContract.deliveryLocation : (deliveryPointTrigger != null ? deliveryPointTrigger.position : Vector3.zero);

        // Feedback de Sucesso
        public string completionBannerMessage = "";
        public float completionBannerTimer = 0f;

        // Lock para evitar reentrância em CompleteActiveContract
        private bool isCompletingContract = false;

        private void Awake()
        {
            Instance = this;
        }

        public void SetupInitialContract(StarterCareer career, Vector3 starterBasePos, Vector3 townCenterPos)
        {
            hasActiveContract = true;
            currentContract = new CommercialContract();

            switch (career)
            {
                case StarterCareer.Farmer:
                    currentContract.title = "🌾 Primeira Safra: Grãos para a Cooperativa";
                    currentContract.description = "Engate a carga de soja colhida no trator e transporte até os Silos da Cooperativa Central da Cidade.";
                    currentContract.cargoType = "Soja em Grãos";
                    currentContract.cargoWeightKg = 1200f;
                    currentContract.rewardMoney = 18500;
                    currentContract.rewardReputation = 15;
                    currentContract.pickupLocation = starterBasePos;
                    currentContract.deliveryLocation = townCenterPos;
                    currentContract.destinationName = "Silos da Cooperativa Agrícola";
                    break;

                case StarterCareer.Trucker:
                    currentContract.title = "🚛 Frete Rodoviário: Carga de Maquinário";
                    currentContract.description = "Carregue o palete de peças industriais no seu caminhão e faça a entrega rápida no Centro Comercial da Cidade.";
                    currentContract.cargoType = "Peças Industriais";
                    currentContract.cargoWeightKg = 2500f;
                    currentContract.rewardMoney = 24000;
                    currentContract.rewardReputation = 20;
                    currentContract.pickupLocation = starterBasePos;
                    currentContract.deliveryLocation = townCenterPos;
                    currentContract.destinationName = "Centro de Distribuição Municipal";
                    break;

                case StarterCareer.Aviator:
                    currentContract.title = "🛩️ Voo de Inspeção & Encomenda Aérea";
                    currentContract.description = "Decole com o monomotor, sobrevoe a antena repetidora da cidade e pouse na pista para entregar a mala postal expressa.";
                    currentContract.cargoType = "Malote Postal Expresso";
                    currentContract.cargoWeightKg = 200f;
                    currentContract.rewardMoney = 32000;
                    currentContract.rewardReputation = 25;
                    currentContract.pickupLocation = starterBasePos;
                    currentContract.deliveryLocation = starterBasePos; // Pouso e entrega
                    currentContract.destinationName = "Hangar de Carga Aérea";
                    break;

                case StarterCareer.Fisherman:
                    currentContract.title = "🎣 Pesca Inicial: Peixes Frescos do Litoral";
                    currentContract.description = "Navegue pelo canal até a boia oceânica, capture a cota de pescada e descarregue no Entreposto do Mercado Municipal.";
                    currentContract.cargoType = "Pescado Fresco";
                    currentContract.cargoWeightKg = 850f;
                    currentContract.rewardMoney = 21000;
                    currentContract.rewardReputation = 18;
                    currentContract.pickupLocation = starterBasePos;
                    currentContract.deliveryLocation = townCenterPos;
                    currentContract.destinationName = "Mercado do Cais da Cidade";
                    break;
            }

            Debug.Log($"[MissionManager] Contrato inicial gerado: '{currentContract.title}'. Recompensa: ${currentContract.rewardMoney:N0}");
        }

        private void Update()
        {
            if (completionBannerTimer > 0f)
            {
                completionBannerTimer -= Time.deltaTime;
            }

            if (!hasActiveContract || currentContract == null || currentContract.isCompleted) return;

            // Posição de referência do jogador ou veículo
            Vector3 playerPos = PlayerCharacterController.Instance != null ?
                (PlayerCharacterController.Instance.isDriving && PlayerCharacterController.Instance.currentVehicle != null ?
                 PlayerCharacterController.Instance.currentVehicle.transform.position :
                 PlayerCharacterController.Instance.transform.position) : Vector3.zero;

            float distToDelivery = Vector3.Distance(playerPos, currentContract.deliveryLocation);

            // Verificar se o jogador chegou ao ponto de entrega
            if (distToDelivery <= deliveryRadius)
            {
                if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpenOrJustClosed)
                    return;

                // Se o jogador pressionar F ou Retorno, entrega a carga
                if (TerraInput.GetKeyDown(Key.F) || TerraInput.GetKeyDown(Key.Enter))
                {
                    CompleteActiveContract();
                }
            }
        }

        public void CompleteActiveContract()
        {
            // Evitar reentrância (spam de F/Enter no mesmo frame)
            if (isCompletingContract) return;
            if (!hasActiveContract || currentContract == null || currentContract.isCompleted) return;

            isCompletingContract = true;
            currentContract.isCompleted = true;

            // Descarregar veículo se estiver conduzindo
            if (PlayerCharacterController.Instance != null && PlayerCharacterController.Instance.currentVehicle != null)
            {
                PlayerCharacterController.Instance.currentVehicle.UnloadCargo();
            }

            // Creditar recompensa no save ativo
            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                var save = SaveManager.Instance.ActiveSave;
                save.currentMoney += currentContract.rewardMoney;
                save.reputation += currentContract.rewardReputation;
                save.completedDeliveries++;
                save.lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                SaveManager.Instance.SaveToFile(save);
                Debug.Log($"[MissionManager] Contrato concluído! Saldo atual: {save.GetFormattedMoney()}. Entregas: {save.completedDeliveries}");
            }

            completionBannerMessage = $"🎉 CONTRATO CONCLUÍDO!\n+{currentContract.rewardMoney:C0} creditados • +{currentContract.rewardReputation} Reputação";
            completionBannerTimer = 5.0f;

            // Gerar próximo contrato após 3 segundos
            Invoke(nameof(GenerateNextContract), 3.0f);

            isCompletingContract = false;
        }

        private void GenerateNextContract()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.ActiveSave == null) return;
            var career = SaveManager.Instance.ActiveSave.starterCareer;

            currentContract = new CommercialContract
            {
                isCompleted = false,
                title = $"📦 Contrato Comercial #{SaveManager.Instance.ActiveSave.completedDeliveries + 1}",
                description = "Transporte de remessa regional de alta prioridade.",
                cargoType = "Carga Comercial",
                cargoWeightKg = UnityEngine.Random.Range(800f, 3000f),
                rewardMoney = UnityEngine.Random.Range(20000, 45000),
                rewardReputation = UnityEngine.Random.Range(10, 25),
                pickupLocation = currentContract.pickupLocation,
                deliveryLocation = currentContract.deliveryLocation,
                destinationName = currentContract.destinationName
            };

            hasActiveContract = true;
            Debug.Log($"[MissionManager] Novo contrato gerado: {currentContract.title}");
        }

        public float GetDistanceToDestination()
        {
            if (!hasActiveContract || currentContract == null || currentContract.isCompleted) return 0f;

            Vector3 playerPos = PlayerCharacterController.Instance != null ?
                (PlayerCharacterController.Instance.isDriving && PlayerCharacterController.Instance.currentVehicle != null ?
                 PlayerCharacterController.Instance.currentVehicle.transform.position :
                 PlayerCharacterController.Instance.transform.position) : Vector3.zero;

            return Vector3.Distance(playerPos, currentContract.deliveryLocation);
        }
    }
}
