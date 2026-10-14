using System;
using UnityEngine;

namespace ProjectTerra.Gameplay
{
    /// <summary>Tipo de um beat (evento) da campanha do modo carreira.</summary>
    public enum StoryBeatType
    {
        Contract,     // gera um contrato comercial jogável (missão)
        PhoneCall,    // ligação recebida (overlay com retrato + falas)
        TextMessage,  // SMS no app Mensagens do celular
        NpcTalk,      // conversa presencial com um NPC (overlay)
        GrantLicense, // concede uma DriverLicense ao jogador
        UnlockAct,    // marca o avanço de ato (banner + save)
        Wait          // pausa até uma condição ser satisfeita
    }

    /// <summary>Condição que libera o disparo de um beat.</summary>
    public enum BeatTrigger
    {
        Immediate,          // assim que o beat anterior resolve
        OnContractComplete, // quando o contrato de estória anterior é entregue
        OnReputation,       // quando reputation >= triggerValue
        OnDeliveries,       // quando completedDeliveries >= triggerValue
        OnLicense,          // quando o jogador possui a licença em triggerValue
        OnVehicleOwned,     // quando possui veículo da categoria em triggerValue
        OnEnterZone,        // quando entra no raio triggerValue de deliveryAnchor
        AfterDelaySeconds   // tempo real decorrido (encadear falas após uma entrega)
    }

    public enum DialogueChannel { Call, Sms, WorldTalk }

    /// <summary>Dados de um contrato de estória (vira um CommercialContract no MissionManager).</summary>
    [Serializable]
    public class ContractData
    {
        public string title;
        public string description;
        public string cargoType;
        public float cargoWeightKg;
        public long rewardMoney;
        public int rewardReputation;
        public DriverLicense requiredLicense = DriverLicense.None;
        public string pickupAnchor = "base";    // "base" | "town"/"market" | "docks" | "airport"
        public string deliveryAnchor = "town";
    }

    /// <summary>Falas de uma interação (ligação, SMS ou conversa no mundo).</summary>
    [Serializable]
    public class DialogueData
    {
        public string contactId;    // "iris", "vega", "aurora", "mateus", "system"
        public string contactName;  // "Dona Íris"
        public string portrait;     // emoji/ícone de retrato
        public DialogueChannel channel = DialogueChannel.Call;
        public string[] lines;      // falas em sequência
        public bool urgent;         // badge/toque diferenciado
    }

    /// <summary>Um beat da campanha: o que acontece e quando é liberado.</summary>
    [Serializable]
    public class StoryBeat
    {
        public string id;
        public StoryBeatType type;
        public BeatTrigger trigger = BeatTrigger.Immediate;
        public float triggerValue;

        public ContractData contract;
        public DialogueData dialogue;
        public DriverLicense grantLicense = DriverLicense.None;
        public string unlockActLabel;
    }

    /// <summary>A sequência completa de beats de uma carreira (ordem adaptativa já aplicada).</summary>
    [Serializable]
    public class CareerCampaign
    {
        public StarterCareer career;
        public StoryBeat[] beats;
    }
}
