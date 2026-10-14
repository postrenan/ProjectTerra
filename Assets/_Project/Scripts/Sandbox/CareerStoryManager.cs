using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Orquestra a campanha progressiva do modo carreira. Substitui a geração aleatória de
    /// contratos por uma sequência scriptada de beats (contratos, ligações, SMS e conversas),
    /// avançando conforme reputação / entregas / licenças do save ativo.
    /// </summary>
    public class CareerStoryManager : MonoBehaviour
    {
        public static CareerStoryManager Instance { get; private set; }

        private CareerCampaign campaign;
        private RegionSaveData save;

        // Beat atualmente em consideração que ainda não resolveu (aguardando condição).
        // Enquanto um contrato/ligação bloqueia, fica null (retoma via callback explícito).
        private StoryBeat considered;
        private float consideredSince;   // Time.time em que o beat entrou em consideração (p/ AfterDelaySeconds)

        private void Awake() => Instance = this;

        public void BeginCampaign(RegionSaveData activeSave)
        {
            if (activeSave == null) return;
            save = activeSave;
            campaign = CampaignLibrary.LoadFor(save.starterCareer);
            if (campaign == null || campaign.beats == null || campaign.beats.Length == 0)
            {
                Debug.LogWarning("[Story] Campanha vazia para a carreira " + save.starterCareer);
                return;
            }
            Debug.Log($"[Story] Campanha iniciada ({save.starterCareer}) — beat {save.storyBeatIndex}/{campaign.beats.Length}.");
            AdvanceUntilBlocked();
        }

        /// <summary>Avança beats automáticos até parar num que exige ação do jogador ou condição.</summary>
        private void AdvanceUntilBlocked()
        {
            while (save != null && campaign != null && save.storyBeatIndex < campaign.beats.Length)
            {
                var beat = campaign.beats[save.storyBeatIndex];

                if (beat == null || (save.resolvedBeats != null && save.resolvedBeats.Contains(beat.id)))
                {
                    save.storyBeatIndex++;
                    continue;
                }

                // Marca quando este beat entrou em consideração (base do temporizador AfterDelaySeconds).
                if (!ReferenceEquals(considered, beat))
                {
                    considered = beat;
                    consideredSince = Time.time;
                }

                // Condição ainda não satisfeita: fica esperando (Update reavalia a cada frame).
                if (!IsTriggerSatisfied(beat)) return;

                bool blocks = ResolveBeat(beat);
                considered = null;

                // Contrato: NÃO avança o índice aqui. Só avança quando for entregue
                // (OnStoryContractCompleted). Assim um reload com contrato ativo o reemite.
                if (beat.type == StoryBeatType.Contract) return;

                MarkResolved(beat);
                if (blocks) return; // ligação/conversa: retoma via callback explícito
            }

            if (save != null && campaign != null && save.storyBeatIndex >= campaign.beats.Length)
                Debug.Log("[Story] Campanha concluída.");
        }

        /// <summary>Executa o beat. Retorna true se ele BLOQUEIA (espera o jogador agir).</summary>
        private bool ResolveBeat(StoryBeat b)
        {
            switch (b.type)
            {
                case StoryBeatType.TextMessage:
                    SandboxHUD.Instance?.ReceiveStorySms(b.dialogue, save);
                    return false;

                case StoryBeatType.PhoneCall:
                case StoryBeatType.NpcTalk:
                    if (SandboxHUD.Instance != null)
                    {
                        SandboxHUD.Instance.ShowConversation(b.dialogue, AdvanceUntilBlocked);
                        return true; // retoma no callback ao encerrar a conversa
                    }
                    return false; // sem HUD: não trava a campanha

                case StoryBeatType.GrantLicense:
                    if (save != null && b.grantLicense != DriverLicense.None)
                    {
                        save.GrantLicense(b.grantLicense);
                        var info = DriverLicenseCatalog.Get(b.grantLicense);
                        SandboxHUD.Instance?.ShowToast($"Licença obtida: {info.icon} {info.name}", 3.2f);
                        Persist();
                    }
                    return false;

                case StoryBeatType.UnlockAct:
                    SandboxHUD.Instance?.ShowToast($"★ {b.unlockActLabel}", 4f);
                    Persist();
                    return false;

                case StoryBeatType.Contract:
                    PushStoryContract(b);
                    return true; // espera CompleteActiveContract

                case StoryBeatType.Wait:
                    return false;
            }
            return false;
        }

        private void PushStoryContract(StoryBeat b)
        {
            var mm = MissionManager.Instance;
            if (mm == null || b.contract == null) return;

            Vector3 pickup = ResolveAnchor(b.contract.pickupAnchor);
            Vector3 delivery = ResolveAnchor(b.contract.deliveryAnchor);
            mm.AcceptStoryContract(b.contract, pickup, delivery, b.id);
        }

        private Vector3 ResolveAnchor(string anchor)
        {
            var m = RegionalSandboxManager.Instance;
            if (m == null) return Vector3.zero;
            return m.ResolveAnchor(anchor);
        }

        /// <summary>Chamado pelo MissionManager quando um contrato de estória é entregue.</summary>
        public void OnStoryContractCompleted(string storyId)
        {
            // Avança o índice para além do contrato entregue (não avançado em ResolveBeat).
            if (save != null && campaign != null && save.storyBeatIndex < campaign.beats.Length)
            {
                var b = campaign.beats[save.storyBeatIndex];
                if (b != null && b.type == StoryBeatType.Contract)
                    MarkResolved(b);
            }
            considered = null;
            AdvanceUntilBlocked();
            Persist(); // garante que o índice avançado (e novo contrato) fique salvo
        }

        private void Update()
        {
            // Só reavalia quando está esperando uma condição (considered != null).
            // Durante um contrato/ligação (bloqueio) considered é null e a retomada é por callback.
            if (considered == null) return;
            if (IsTriggerSatisfied(considered))
                AdvanceUntilBlocked();
        }

        private bool IsTriggerSatisfied(StoryBeat b)
        {
            if (save == null) return false;
            switch (b.trigger)
            {
                case BeatTrigger.Immediate:          return true;
                case BeatTrigger.OnContractComplete: return true; // avaliado só após o contrato resolver
                case BeatTrigger.OnReputation:       return save.reputation >= b.triggerValue;
                case BeatTrigger.OnDeliveries:       return save.completedDeliveries >= b.triggerValue;
                case BeatTrigger.OnLicense:          return save.HasLicense((DriverLicense)(int)b.triggerValue);
                case BeatTrigger.OnVehicleOwned:     return OwnsCategory((VehicleCategory)(int)b.triggerValue);
                case BeatTrigger.OnEnterZone:        return PlayerNearAnchor(b);
                case BeatTrigger.AfterDelaySeconds:  return (Time.time - consideredSince) >= b.triggerValue;
                default:                             return true;
            }
        }

        private bool OwnsCategory(VehicleCategory cat)
        {
            var vm = VehicleManager.Instance;
            if (vm == null || vm.possessedVehicles == null) return false;
            foreach (var v in vm.possessedVehicles)
                if (v != null && v.category == cat) return true;
            return false;
        }

        private bool PlayerNearAnchor(StoryBeat b)
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return false;
            Vector3 target = ResolveAnchor(b.contract != null ? b.contract.deliveryAnchor : "town");
            float radius = b.triggerValue > 0 ? b.triggerValue : 20f;
            return Vector3.Distance(player.transform.position, target) <= radius;
        }

        private void MarkResolved(StoryBeat b)
        {
            if (save == null) return;
            if (save.resolvedBeats == null) save.resolvedBeats = new System.Collections.Generic.List<string>();
            if (!save.resolvedBeats.Contains(b.id)) save.resolvedBeats.Add(b.id);
            save.storyBeatIndex++;
        }

        private void Persist()
        {
            if (save != null && SaveManager.Instance != null) SaveManager.Instance.SaveToFile(save);
        }
    }
}
