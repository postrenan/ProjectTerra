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

        // --- Prazo e risco (Fase 2) ---
        // Horas de jogo acumuladas (GameClock) em que o frete expira. 0 = sem prazo.
        public float deadlineGameHours = 0f;
        // Reputação perdida se a entrega atrasar.
        public int penaltyReputation = 0;
        // Carga delicada: colisões/trancos abatem a integridade e, por consequência, o pagamento.
        public bool fragile = false;
        public float cargoIntegrity = 100f;
        // Frete urgente (prazo apertado, recompensa maior).
        public bool urgent = false;
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

        #region Prazos, destinos e risco (Fase 2/3)

        private RegionSaveData ActiveSave => SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : null;
        private bool AdvancedEconomy => ActiveSave != null && ActiveSave.advancedEconomy;

        private int lastHubIndex = -1;

        /// <summary>Escolhe um destino entre os hubs do mundo, evitando repetir o último.</summary>
        private bool TryPickDestinationHub(out Vector3 pos, out string name)
        {
            pos = Vector3.zero; name = null;
            var rsm = RegionalSandboxManager.Instance;
            if (rsm == null || rsm.deliveryHubs == null || rsm.deliveryHubs.Count == 0) return false;

            int count = rsm.deliveryHubs.Count;
            int idx = UnityEngine.Random.Range(0, count);
            if (count > 1 && idx == lastHubIndex) idx = (idx + 1) % count;
            lastHubIndex = idx;

            pos = rsm.deliveryHubs[idx].position;
            name = rsm.deliveryHubs[idx].name;
            return true;
        }

        /// <summary>Define o prazo de entrega e a penalidade, conforme distância, carreira e dificuldade.</summary>
        private void ApplyDeadline(CommercialContract c)
        {
            if (!AdvancedEconomy)
            {
                c.deadlineGameHours = 0f; // sem prazo em partidas antigas
                return;
            }

            var save = ActiveSave;
            float tightness = save.DifficultyConfig.deadlineTightness;

            float distKm = Vector3.Distance(c.pickupLocation, c.deliveryLocation) / 1000f;
            float avgKmh = save.starterCareer == StarterCareer.Aviator ? 140f :
                           save.starterCareer == StarterCareer.Fisherman ? 28f : 42f;
            float travelH = distKm / Mathf.Max(5f, avgKmh);

            float slack = Mathf.Lerp(3.0f, 1.3f, tightness);
            float buffer = Mathf.Lerp(8f, 2f, tightness);
            float allow = travelH * slack + buffer;
            if (c.urgent) allow *= 0.55f;

            c.deadlineGameHours = GameClock.TotalGameHoursNow + allow;
            c.penaltyReputation = Mathf.CeilToInt(Mathf.Lerp(5f, 22f, tightness)) + (c.urgent ? 8 : 0);
        }

        public float GetHoursRemaining()
        {
            if (currentContract == null || currentContract.deadlineGameHours <= 0f) return float.PositiveInfinity;
            return currentContract.deadlineGameHours - GameClock.TotalGameHoursNow;
        }

        public bool IsLate()
        {
            if (currentContract == null || currentContract.deadlineGameHours <= 0f) return false;
            return GameClock.TotalGameHoursNow > currentContract.deadlineGameHours;
        }

        /// <summary>Chamado pelo veículo em colisões: abate a integridade da carga frágil em transporte.</summary>
        public void ReportCargoShock(float vehicleDamage)
        {
            if (!hasActiveContract || currentContract == null || currentContract.isCompleted) return;
            if (!currentContract.fragile) return;
            // O tranco na carga é proporcional ao dano sofrido pelo veículo.
            currentContract.cargoIntegrity = Mathf.Max(0f, currentContract.cargoIntegrity - vehicleDamage * 1.6f);
        }

        #endregion

        public void SetupInitialContract(StarterCareer career, Vector3 starterBasePos, Vector3 townCenterPos)
        {
            hasActiveContract = true;
            currentContract = new CommercialContract();

            switch (career)
            {
                case StarterCareer.Farmer:
                    currentContract.title = "Primeira Safra: Grãos para a Cooperativa";
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
                    currentContract.title = "Frete Rodoviário: Carga de Maquinário";
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
                    currentContract.title = "Voo de Inspeção & Encomenda Aérea";
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
                    currentContract.title = "Pesca Inicial: Peixes Frescos do Litoral";
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

            ApplyDeadline(currentContract);
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

            // Creditar recompensa no save ativo (com prazo e integridade na economia avançada)
            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                var save = SaveManager.Instance.ActiveSave;
                long basePay = currentContract.rewardMoney;

                if (save.advancedEconomy)
                {
                    bool late = IsLate();

                    // Carga frágil paga proporcional à integridade (mínimo 40%).
                    float integrityFactor = currentContract.fragile
                        ? Mathf.Lerp(0.4f, 1f, currentContract.cargoIntegrity / 100f) : 1f;
                    long pay = (long)(basePay * integrityFactor);

                    string bonusLine;
                    if (!late)
                    {
                        long bonus = (long)(basePay * 0.15f);
                        pay += bonus;
                        save.reputation += currentContract.rewardReputation;
                        bonusLine = $"No prazo: +${bonus:N0} bônus • +{currentContract.rewardReputation} Reputação";
                    }
                    else
                    {
                        int penalty = currentContract.penaltyReputation;
                        save.reputation = Mathf.Max(0, save.reputation - penalty);
                        save.lateDeliveries++;
                        bonusLine = $"[Atrasado] -{penalty} Reputação";
                    }

                    save.currentMoney += pay;
                    save.completedDeliveries++;
                    save.lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    SaveManager.Instance.SaveToFile(save);

                    string fragLine = currentContract.fragile ? $"\nIntegridade da carga: {currentContract.cargoIntegrity:F0}%" : "";
                    completionBannerMessage = $"CONTRATO CONCLUÍDO!\n+${pay:N0} creditados\n{bonusLine}{fragLine}";
                    Debug.Log($"[MissionManager] Concluído ({(late ? "ATRASADO" : "no prazo")}). Pago ${pay:N0}. Saldo: {save.GetFormattedMoney()}.");
                }
                else
                {
                    // Comportamento clássico (partidas antigas sem economia avançada).
                    save.currentMoney += basePay;
                    save.reputation += currentContract.rewardReputation;
                    save.completedDeliveries++;
                    save.lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    SaveManager.Instance.SaveToFile(save);
                    completionBannerMessage = $"CONTRATO CONCLUÍDO!\n+${basePay:N0} creditados • +{currentContract.rewardReputation} Reputação";
                }
            }

            completionBannerTimer = 5.0f;

            // Gerar próximo contrato após 3 segundos
            Invoke(nameof(GenerateNextContract), 3.0f);

            isCompletingContract = false;
        }

        private void GenerateNextContract()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.ActiveSave == null) return;
            var save = SaveManager.Instance.ActiveSave;

            // Ponto de coleta: onde o jogador está agora (ou o último ponto de coleta).
            Vector3 pickup = PlayerCharacterController.Instance != null
                ? PlayerCharacterController.Instance.transform.position
                : currentContract.pickupLocation;

            // Destino: um hub variado do mundo (fallback para o destino anterior).
            Vector3 delivery = currentContract.deliveryLocation;
            string dest = currentContract.destinationName;
            if (TryPickDestinationHub(out var hubPos, out var hubName))
            {
                delivery = hubPos;
                dest = hubName;
            }

            currentContract = new CommercialContract
            {
                isCompleted = false,
                title = $"Contrato Comercial #{save.completedDeliveries + 1}",
                description = $"Transporte de remessa regional até {dest}.",
                cargoType = "Carga Comercial",
                cargoWeightKg = UnityEngine.Random.Range(800f, 3000f),
                rewardMoney = UnityEngine.Random.Range(20000, 45000),
                rewardReputation = UnityEngine.Random.Range(10, 25),
                pickupLocation = pickup,
                deliveryLocation = delivery,
                destinationName = dest
            };

            ApplyDeadline(currentContract);
            hasActiveContract = true;
            Debug.Log($"[MissionManager] Novo contrato gerado: {currentContract.title} → {dest}");
        }

        /// <summary>
        /// Aceita um frete escolhido no mercado do celular (apps Frete). Reaproveita os
        /// pontos de coleta/entrega do contrato atual para continuar compatível com a
        /// lógica de gatilho por raio já existente.
        /// </summary>
        public void AcceptMarketContract(string title, string description, string cargo, float weightKg, long reward, int reputation, bool fragile = false, bool urgent = false)
        {
            Vector3 playerPos = PlayerCharacterController.Instance != null ? PlayerCharacterController.Instance.transform.position : Vector3.zero;
            Vector3 pickup = playerPos;

            // Destino: um hub variado do mundo; fallback para o destino atual.
            Vector3 delivery = currentContract != null ? currentContract.deliveryLocation : playerPos;
            string dest = currentContract != null && !string.IsNullOrEmpty(currentContract.destinationName)
                ? currentContract.destinationName
                : "Centro de Distribuição Municipal";
            if (TryPickDestinationHub(out var hubPos, out var hubName))
            {
                delivery = hubPos;
                dest = hubName;
            }

            currentContract = new CommercialContract
            {
                title = title,
                description = $"Transportar {cargo} até {dest}.",
                cargoType = cargo,
                cargoWeightKg = weightKg,
                rewardMoney = reward,
                rewardReputation = reputation,
                pickupLocation = pickup,
                deliveryLocation = delivery,
                destinationName = dest,
                isCompleted = false,
                fragile = fragile,
                urgent = urgent,
                cargoIntegrity = 100f
            };
            ApplyDeadline(currentContract);
            hasActiveContract = true;
            isCompletingContract = false;

            Debug.Log($"[MissionManager] Frete aceito via celular: '{title}' (${reward:N0}){(urgent ? " [URGENTE]" : "")}.");
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
