# CareerStoryManager — Esboço de Implementação

> **Status: IMPLEMENTADO no jogo.** Os 4 atos das 4 carreiras estão em código e rodando.
> Mapa design → arquivos:
> - Modelo de beats → `Assets/_Project/Scripts/Gameplay/StoryBeat.cs`
> - Os atos (conteúdo das 4 campanhas) → `Assets/_Project/Scripts/Gameplay/CampaignLibrary.cs`
> - Orquestrador → `Assets/_Project/Scripts/Sandbox/CareerStoryManager.cs`
> - Save v4 (progresso + inbox SMS) → `Assets/_Project/Scripts/Gameplay/RegionSaveData.cs`
> - Contrato de estória + desvio → `Assets/_Project/Scripts/Sandbox/MissionManager.cs`
> - Celular: app Mensagens + overlay de ligação/conversa → `Assets/_Project/Scripts/Sandbox/SandboxHUD.Story.cs` (+ enum/grade em `SandboxHUD.Menu.cs`)
> - Início da campanha + âncoras → `Assets/_Project/Scripts/Sandbox/RegionalSandboxManager.cs`
>
> Decisão de robustez: a progressão é **sequencial por conclusão de contrato**
> (`OnContractComplete`), não por limiar de reputação/entregas — isso evita qualquer
> deadlock (um ato nunca fica esperando uma meta que os contratos da estória não atingem).
> Os fretes livres do mercado seguem rodando em paralelo para renda extra.


> Orquestrador da campanha progressiva do **Project Terra**. Lê uma tabela de **beats**
> (marcos + interações), dispara **contratos**, **ligações 📞**, **mensagens de texto 📱**
> e **conversas com NPCs 💬** ao longo da jornada do **Roger Mendes**, e avança de ato
> conforme reputação / entregas / licenças.
>
> Baseado em: [CAREER_MODE_PROGRESSIVE_STORY.md](CAREER_MODE_PROGRESSIVE_STORY.md) ·
> [CAREER_MODE_STORY_TEXTS.md](CAREER_MODE_STORY_TEXTS.md).

## Visão geral da arquitetura

```
                 ┌─────────────────────────────────────────────┐
                 │           CareerStoryManager                │
                 │  (lê campaign.json → fila de StoryBeats)     │
                 └───────┬───────────────┬─────────────────────┘
          dispara beat   │               │  escuta eventos
                 ┌───────▼──────┐  ┌──────▼───────────────┐
                 │  PhoneOS     │  │ MissionManager        │
                 │ (SMS/Call UI)│  │ (contrato ativo)      │
                 └───────┬──────┘  └──────┬───────────────┘
                 ┌───────▼──────┐  ┌──────▼───────────────┐
                 │ SandboxHUD   │  │ RegionSaveData        │
                 │ (apps/toast) │  │ (storyId, reputation) │
                 └──────────────┘  └───────────────────────┘
```

**Princípio:** o `CareerStoryManager` substitui o `GenerateNextContract()` aleatório do
`MissionManager`. Em vez de sortear, ele avança uma **fila de beats scriptados**. Cada beat
pode ser um contrato, uma ligação, uma mensagem, uma conversa de NPC, uma concessão de
licença, ou um gatilho de desbloqueio.

---

## 1. Modelo de dados

### Tipos de beat

```csharp
namespace ProjectTerra.Gameplay
{
    public enum StoryBeatType
    {
        Contract,     // gera um contrato comercial (missão jogável)
        PhoneCall,    // ligação recebida (overlay com retrato + áudio/voz)
        TextMessage,  // SMS no app Mensagens
        NpcTalk,      // conversa com NPC no mundo (gatilho por proximidade)
        GrantLicense, // concede uma DriverLicense ao jogador
        UnlockAct,    // marca avanço de ato / salva checkpoint
        Wait          // pausa até uma condição (reputação, entregas, veículo possuído)
    }

    public enum BeatTrigger
    {
        Immediate,          // dispara assim que o beat anterior resolve
        OnContractComplete, // quando o contrato-pai é entregue
        OnReputation,       // quando reputation >= value
        OnDeliveries,       // quando completedDeliveries >= value
        OnLicense,          // quando o jogador obtém a licença X
        OnVehicleOwned,     // quando possui veículo da categoria X
        OnEnterZone,        // quando entra num raio de um ponto (base, cais, aeroporto)
        AfterDelaySeconds   // tempo real, p/ encadear ligação logo após uma entrega
    }
}
```

### Beat e campanha

```csharp
[Serializable]
public class StoryBeat
{
    public string id;                // "ACT1_FISH_03"
    public StoryBeatType type;
    public BeatTrigger trigger = BeatTrigger.Immediate;
    public float triggerValue;       // reputação-alvo, delay, raio, etc.
    public string requiresBeat;      // id de beat que precisa ter resolvido antes

    // --- Conteúdo (preenchido conforme o type) ---
    public ContractData contract;    // type == Contract
    public DialogueData dialogue;    // PhoneCall / TextMessage / NpcTalk
    public DriverLicense grantLicense = DriverLicense.None; // GrantLicense
    public string unlockActLabel;    // UnlockAct (ex.: "Ato II — A Estrada")
}

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
    public string pickupAnchor = "base";   // "base" | "town" | "docks" | "airport"
    public string deliveryAnchor = "town"; // resolve p/ posição real no sandbox
}

[Serializable]
public class DialogueData
{
    public string contactId;   // "iris", "vega", "aurora", "mateus", "system"
    public string contactName; // "Dona Íris"
    public string portrait;    // chave de ícone/retrato (emoji ou sprite)
    public Channel channel;    // Call | Sms | WorldTalk
    [TextArea] public string[] lines; // falas em sequência (balões/parágrafos)
    public bool urgent;        // toca som diferente / badge vermelho
}

public enum Channel { Call, Sms, WorldTalk }

[Serializable]
public class CareerCampaign
{
    public StarterCareer career;   // a qual carreira esta sequência pertence
    public StoryBeat[] beats;      // ordem lógica da campanha (ordem adaptativa já aplicada)
}
```

> **Por que JSON/ScriptableObject:** os 15 marcos + interações viram dados editáveis sem
> recompilar. Um `CareerCampaign` por carreira (4 arquivos) resolve a **ordem adaptativa**
> do doc principal — cada arquivo já traz os beats na ordem certa daquela carreira.

---

## 2. Persistência (campos novos em RegionSaveData)

```csharp
// --- Campanha (save v4) ---
public int storyBeatIndex = 0;          // próximo beat a resolver na campanha
public List<string> resolvedBeats = new List<string>(); // idempotência
public List<SmsRecord> inbox = new List<SmsRecord>();    // histórico de mensagens

[Serializable]
public class SmsRecord
{
    public string contactName;
    public string body;
    public long gameDay;    // quando chegou
    public bool read;
}
```

Isso mantém a campanha **salva e retomável** e dá um histórico real de SMS no celular.

---

## 3. CareerStoryManager (esqueleto)

```csharp
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public class CareerStoryManager : MonoBehaviour
    {
        public static CareerStoryManager Instance { get; private set; }

        private CareerCampaign campaign;     // carregado conforme a carreira do save
        private RegionSaveData save;
        private StoryBeat pendingTriggerBeat; // beat aguardando condição (Wait/OnReputation...)

        private void Awake() => Instance = this;

        public void BeginCampaign(RegionSaveData activeSave)
        {
            save = activeSave;
            campaign = CampaignLibrary.LoadFor(save.starterCareer); // lê o JSON certo
            AdvanceUntilBlocked();
        }

        // Avança beats automáticos (mensagens, ligações, licenças) até parar num
        // beat que exige ação do jogador (Contract) ou numa condição (Wait/OnReputation).
        private void AdvanceUntilBlocked()
        {
            while (save.storyBeatIndex < campaign.beats.Length)
            {
                var beat = campaign.beats[save.storyBeatIndex];

                if (save.resolvedBeats.Contains(beat.id)) { save.storyBeatIndex++; continue; }

                // Beat condicional ainda não satisfeito → guarda e para.
                if (!IsTriggerSatisfied(beat)) { pendingTriggerBeat = beat; return; }

                bool blocks = ResolveBeat(beat);  // executa o beat
                MarkResolved(beat);
                if (blocks) return;               // Contract: espera a entrega
            }
            Debug.Log("[Story] Campanha concluída.");
        }

        // Retorna true se o beat BLOQUEIA o avanço (espera o jogador agir).
        private bool ResolveBeat(StoryBeat b)
        {
            switch (b.type)
            {
                case StoryBeatType.TextMessage:
                    PhoneOS.Instance.ReceiveSms(b.dialogue, save);
                    return false;

                case StoryBeatType.PhoneCall:
                    PhoneOS.Instance.IncomingCall(b.dialogue,
                        onFinished: () => { AdvanceUntilBlocked(); });
                    return true; // a ligação toma a tela; retoma no callback

                case StoryBeatType.NpcTalk:
                    NpcDirector.Instance.QueueWorldTalk(b.dialogue);
                    return false;

                case StoryBeatType.GrantLicense:
                    save.GrantLicense(b.grantLicense);
                    PhoneOS.Instance.ShowToast($"Licença obtida: {DriverLicenseCatalog.Get(b.grantLicense).name}");
                    return false;

                case StoryBeatType.UnlockAct:
                    SandboxHUD.Instance.ShowChapterBanner(b.unlockActLabel);
                    SaveManager.Instance.SaveToFile(save);
                    return false;

                case StoryBeatType.Contract:
                    PushStoryContract(b.contract);
                    return true; // espera CompleteActiveContract

                case StoryBeatType.Wait:
                    return true; // só sai via IsTriggerSatisfied no próximo tick
            }
            return false;
        }

        private void PushStoryContract(ContractData c)
        {
            var mm = MissionManager.Instance;
            // Resolve âncoras ("base"/"town"/"docks"/"airport") p/ posições reais do sandbox.
            Vector3 pickup = RegionalSandboxManager.Instance.ResolveAnchor(c.pickupAnchor);
            Vector3 delivery = RegionalSandboxManager.Instance.ResolveAnchor(c.deliveryAnchor);
            mm.AcceptStoryContract(c, pickup, delivery); // variante de AcceptMarketContract
        }

        // Chamado pelo MissionManager quando o contrato da estória é entregue.
        public void OnStoryContractCompleted(string contractTitle)
        {
            AdvanceUntilBlocked();
        }

        // Verificação por frame dos beats condicionais pendentes.
        private void Update()
        {
            if (pendingTriggerBeat != null && IsTriggerSatisfied(pendingTriggerBeat))
            {
                pendingTriggerBeat = null;
                AdvanceUntilBlocked();
            }
        }

        private bool IsTriggerSatisfied(StoryBeat b)
        {
            switch (b.trigger)
            {
                case BeatTrigger.Immediate:          return true;
                case BeatTrigger.OnReputation:       return save.reputation >= b.triggerValue;
                case BeatTrigger.OnDeliveries:       return save.completedDeliveries >= b.triggerValue;
                case BeatTrigger.OnLicense:          return save.HasLicense((DriverLicense)(int)b.triggerValue);
                case BeatTrigger.OnVehicleOwned:     return VehicleManager.Instance.OwnsCategory((VehicleCategory)(int)b.triggerValue);
                case BeatTrigger.OnEnterZone:        return PlayerNearAnchor(b);
                case BeatTrigger.OnContractComplete: return true; // encadeado via callback
                default:                             return true;
            }
        }

        private void MarkResolved(StoryBeat b)
        {
            if (!save.resolvedBeats.Contains(b.id)) save.resolvedBeats.Add(b.id);
            save.storyBeatIndex++;
        }

        private bool PlayerNearAnchor(StoryBeat b) { /* distância ao âncora <= triggerValue */ return false; }
    }
}
```

**Gancho no MissionManager** (mudança mínima): em `CompleteActiveContract()`, trocar o
`Invoke(nameof(GenerateNextContract), 3f)` por:

```csharp
if (currentContract.isStoryContract && CareerStoryManager.Instance != null)
    CareerStoryManager.Instance.OnStoryContractCompleted(currentContract.title);
else
    Invoke(nameof(GenerateNextContract), 3f); // fretes livres continuam aleatórios
```

Assim os **fretes do mercado** (celular) seguem aleatórios e **paralelos** à campanha —
o jogador pode fazer dinheiro extra entre os marcos da estória.

---

## 4. PhoneOS — ligações 📞 e mensagens 📱

Encaixa no `SandboxHUD` existente (que já tem `PhoneApp`, `OpenApp`, `ShowToast`).

### 4.1 App Mensagens (novo PhoneApp)

Adicionar `Messages` ao enum `PhoneApp` e um `DrawPhoneMessages(content)` espelhando
`DrawPhoneContacts`. A inbox vem de `save.inbox` (persistida).

```csharp
// SandboxHUD.Menu.cs — enum PhoneApp: ... , Messages, ...
// Home: adicionar "Mensagens" à HomeLabels + HomeTargets, com badge de não-lidas.

private void DrawPhoneMessages(Rect content)
{
    DrawAppHeader(content, "Mensagens");
    var save = CurrentSave;
    // lista cada SmsRecord: contactName em negrito, body, "há X dias", badge se !read
    foreach (var sms in save.inbox) { /* desenha balão; marca read ao abrir */ }
}
```

### 4.2 Recepção de SMS

```csharp
public void ReceiveSms(DialogueData d, RegionSaveData save)
{
    foreach (var line in d.lines)
        save.inbox.Add(new SmsRecord {
            contactName = d.contactName, body = line,
            gameDay = save.gameDay, read = false
        });
    ShowToast($"📱 {d.contactName}: nova mensagem"); // usa o toast que já existe
    PlaySms();                                        // som curto
}
```

### 4.3 Ligação recebida (overlay)

Overlay modal que **pausa a condução** e mostra retrato + falas com botão "Atender".
Pode reaproveitar o mesmo renderer de balão do console/HUD.

```csharp
public void IncomingCall(DialogueData d, System.Action onFinished)
{
    callQueue = new Queue<string>(d.lines);
    callContact = d; callActive = true; callOnFinished = onFinished;
    RingTone(d.urgent);
    // UI: "📞 Dona Íris está ligando…  [Atender] [Recusar]"
    // Ao atender, avança falas (clique/Enter); ao fim, callOnFinished()
}
```

> Sem dublagem no MVP: ligação = overlay de texto com retrato e toque. Dá pra plugar
> `fal` depois (skill `fal-assets`) p/ gerar vozes das linhas e virar áudio real.

---

## 5. NpcDirector — conversas no mundo 💬

Para falas presenciais (chegar na base da Íris, no cais, no hangar do Vega). Dois modos:

1. **Balão de proximidade:** ao entrar no raio de um NPC âncora, abre um balão com as
   `lines` (avança com `[F]`/clique). Reaproveita o gatilho de raio que o `MissionManager`
   já usa para entrega.
2. **Fila do diretor:** `QueueWorldTalk(dialogue)` enfileira uma conversa que dispara no
   próximo encontro relevante (ex.: Vega só fala quando o Roger pousa no hangar).

```csharp
public class NpcDirector : MonoBehaviour
{
    public static NpcDirector Instance { get; private set; }
    private readonly Queue<DialogueData> pending = new();

    public void QueueWorldTalk(DialogueData d) => pending.Add(d);

    private void Update()
    {
        // Se há conversa pendente e o jogador está perto do NPC contactId → abre balão.
        // Ao fechar, chama CareerStoryManager.AdvanceUntilBlocked() se o beat exigia.
    }
}
```

---

## 6. Exemplo de dados — início da campanha do Roger (Pescador)

`campaign_fisherman.json` (trecho — Ato 0 → início do Ato I):

```json
{
  "career": "Fisherman",
  "beats": [
    { "id": "F_A0_LETTER", "type": "TextMessage", "trigger": "Immediate",
      "dialogue": { "contactId": "mateus", "contactName": "Tio Mateus (rascunho)",
        "channel": "Sms", "portrait": "⚓",
        "lines": [ "Roger, se você tá lendo isso, eu já era.",
                   "A Marujo I é sua. Ela já viu mares piores que suas dúvidas.",
                   "Procura a Íris, o Vega e o pessoal do Sítio Aurora. Eles te devem favores." ] } },

    { "id": "F_A0_KEYS", "type": "Contract", "trigger": "Immediate",
      "contract": { "title": "As Chaves", "description": "Leve os pertences do tio Mateus da traineira ao Mercado do Cais.",
        "cargoType": "Rede e Pertences", "cargoWeightKg": 120, "rewardMoney": 5000, "rewardReputation": 10,
        "requiredLicense": "None", "pickupAnchor": "docks", "deliveryAnchor": "town" } },

    { "id": "F_A0_GRANT", "type": "GrantLicense", "trigger": "OnContractComplete",
      "grantLicense": "Maritime" },

    { "id": "F_A0_IRIS_SMS", "type": "TextMessage", "trigger": "AfterDelaySeconds", "triggerValue": 4,
      "dialogue": { "contactId": "iris", "contactName": "Dona Íris", "channel": "Sms", "portrait": "🚚",
        "lines": [ "Oi Roger. Soube do Mateus. Sinto muito.",
                   "Quando quiser escoar peixe de verdade, me liga. Tenho caminhão parado." ] } },

    { "id": "F_A1_FIRST", "type": "Contract", "trigger": "Immediate",
      "contract": { "title": "Primeira Pesca", "description": "Navegue até a boia oceânica, pesque a cota e descarregue no Entreposto.",
        "cargoType": "Pescado Fresco", "cargoWeightKg": 850, "rewardMoney": 21000, "rewardReputation": 18,
        "requiredLicense": "Maritime", "pickupAnchor": "docks", "deliveryAnchor": "town" } },

    { "id": "F_A1_IRIS_CALL", "type": "PhoneCall", "trigger": "OnContractComplete",
      "dialogue": { "contactId": "iris", "contactName": "Dona Íris", "channel": "Call", "portrait": "🚚", "urgent": false,
        "lines": [ "Roger! Vi que você encheu o porão hoje.",
                   "Peixe parado é peixe perdido, menino.",
                   "Aparece na minha garagem — tenho uma rota e um caminhão pra você." ] } },

    { "id": "F_A1_CARDUME", "type": "Contract", "trigger": "OnReputation", "triggerValue": 120,
      "contract": { "title": "Captura Além da Cota", "description": "Cardume gigante. Sua traineira não escoa tudo a tempo — você precisa de rodas.",
        "cargoType": "Supercaptura", "cargoWeightKg": 1800, "rewardMoney": 27000, "rewardReputation": 25,
        "requiredLicense": "Maritime", "pickupAnchor": "docks", "deliveryAnchor": "town" } },

    { "id": "F_A2_UNLOCK", "type": "UnlockAct", "trigger": "OnDeliveries", "triggerValue": 5,
      "unlockActLabel": "Ato II — A Estrada" },

    { "id": "F_A2_IRIS_TALK", "type": "NpcTalk", "trigger": "OnEnterZone", "triggerValue": 15,
      "dialogue": { "contactId": "iris", "contactName": "Dona Íris", "channel": "WorldTalk", "portrait": "🚚",
        "lines": [ "Então é você o sobrinho do Mateus. Cara dele.",
                   "O caminhão tá ali. A parcela vence todo mês, não esquece.",
                   "Rode certo que a estrada paga." ] } }
  ]
}
```

Os textos completos de **todos os atos** (para colar nos JSONs das 4 carreiras) estão em
[CAREER_MODE_STORY_TEXTS.md](CAREER_MODE_STORY_TEXTS.md) — basta mapear cada marco para um
beat `Contract` e intercalar `PhoneCall`/`TextMessage`/`NpcTalk` com as falas 📞/📱/💬 já escritas.

---

## 7. Elenco de contatos (recorrentes)

| contactId | Nome | Retrato | Papel na jornada do Roger |
| :--- | :--- | :--- | :--- |
| `mateus` | Tio Mateus | ⚓ | Voz do passado (cartas/rascunhos no celular), abre a campanha |
| `iris` | Dona Íris | 🚚 | Mentora da **estrada**; empresta o 1º caminhão |
| `vega` | Comandante Vega | ✈️ | Mentor do **céu**; piloto aposentado, dá o brevê |
| `marina` | Marina | 🎣 | Mestra de traineira (mentora da **água** nas carreiras não-pescador) |
| `aurora` | Sítio Aurora | 🌾 | Parceria do tio no **agro**; fecha a cadeia produtiva |
| `salas` | Eng. Salas | ⛏️ | Mentor da **mineração** (regiões sem água) |
| `system` | Central/Prefeitura | 🏛️ | Avisos neutros (desbloqueios, licenças, prazos) |

Reaproveitar o **app Contatos** que já existe: cada `contactId` vira uma entrada, e tocar
nela mostra o histórico de SMS daquele contato (filtra `save.inbox`).

---

## 8. Ordem de implementação sugerida (incremental)

1. **Dados:** `StoryBeat`/`ContractData`/`DialogueData` + `CampaignLibrary.LoadFor()` lendo
   um JSON por carreira (começa só com o `campaign_fisherman.json` acima).
2. **Save v4:** `storyBeatIndex`, `resolvedBeats`, `inbox` + migração (default vazio).
3. **Contrato de estória:** flag `isStoryContract` em `CommercialContract` + `AcceptStoryContract`
   e o desvio no `CompleteActiveContract`.
4. **CareerStoryManager:** máquina de beats (seção 3) chamada em `BeginCampaign` na
   inicialização do `RegionalSandboxManager`.
5. **PhoneOS SMS:** app Mensagens + `ReceiveSms` + badge de não-lidas (seção 4.1/4.2).
6. **PhoneOS Call:** overlay de ligação (seção 4.3).
7. **NpcDirector:** balão de proximidade (seção 5).
8. **Áudio/voz (opcional):** gerar toques e vozes via skill `fal-assets`.

Cada passo é jogável por si: até o passo 4 já existe uma campanha com contratos scriptados;
5–7 adicionam a camada narrativa de celular e mundo.
