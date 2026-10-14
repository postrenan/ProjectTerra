# Modo Carreira — Estórias Progressivas (4 Jogatinas)

> Design narrativo do modo carreira do **Project Terra**. Objetivo: uma campanha
> **progressiva** que, ao longo do crescimento de um pequeno empreendimento regional
> até um **grupo logístico multimodal**, leva o jogador a experimentar as **4 jogatinas**:
> 🌾 Fazendeiro · 🚛 Transportador · 🛩️ Aviador · 🎣 Pescador.

## Princípio de design

O jogador **escolhe uma carreira inicial** (como já acontece hoje), mas a estória o
**obriga a diversificar**: cada ato abre uma nova modalidade por necessidade econômica,
não por menu. No fim, as quatro jogatinas convivem numa mesma operação intermodal.

Tudo abaixo se apoia em sistemas que **já existem** no código:

| Sistema existente | Papel na estória |
| :--- | :--- |
| `StarterCareer` (Farmer/Trucker/Aviator/Fisherman) | Define o Ato 1 e o tom de abertura |
| `DriverLicense` (bitflags) | **Portão de progressão** entre modalidades |
| `CommercialContract` (reward money + reputation) | Unidade de missão de cada marco |
| `reputation` (começa em 100), `completedDeliveries` | Gatilhos de desbloqueio de ato |
| `BankLoan` / app Finanças (empréstimos parcelados) | Tensão econômica e "aposta" para expandir |
| Mercado de fretes no celular (`AcceptMarketContract`) | Entrega os contratos de cada marco |
| `gameDay` + multa por atraso | Pressão de prazo nos empréstimos |
| Região 1:1 real (litoral, rios, arável, mineral, floresta) | Define quais modalidades a região suporta |

**Regra de adaptação regional:** a campanha lê os indicadores reais da região
(`forestPercent`, `arablePercent`, `waterPercent`, `hasCoastline`, `hasRivers`…).
Se a região não tem litoral/rios, o Ato Marítimo vira **fluvial/lacustre** ou é
substituído por um Ato de Mineração/Carga Pesada — nunca trava a campanha
(reaproveita o `ValidateCareerForRegion` que já existe).

---

## Estrutura geral: 5 Atos, 15 Marcos

```
ATO 0  — Prólogo: A Herança            (tutorial, 1 marco)
ATO I  — Raízes      [carreira inicial] (3 marcos)   ← jogatina escolhida
ATO II — A Estrada   [Transportador]    (3 marcos)   ← conecta tudo
ATO III— O Céu       [Aviador]          (3 marcos)   ← alcance e urgência
ATO IV — A Água      [Pescador/Marítimo](3 marcos)   ← nova cadeia produtiva
ATO V  — O Grupo     [Intermodal]       (2 marcos)   ← as 4 juntas
```

A **ordem dos Atos II–IV rotaciona** conforme a carreira inicial, para que a estória
sempre apresente algo *novo* depois do que o jogador já domina (ver "Ordem adaptativa").

---

## ATO 0 — Prólogo: A Herança

**Gancho:** o jogador herda do tio Mateus um pequeno empreendimento falido na região escolhida
(uma fazenda, uma transportadora, um hangar ou uma traineira, conforme a `StarterCareer`).
O cartório entrega as chaves, uma dívida pequena e um caderno com "contatos".

- **Marco 0.1 — "As Chaves":** tutorial disfarçado. Dirigir o veículo inicial da base
  até a cidade, conhecer o Mercado Municipal (ponto de entrega padrão) e o app de fretes.
  - Recompensa: ~$5.000 + licença base da carreira (`Standard` ou a específica).
  - Ensina: condução, `[E]` embarque, `[F]` entrega, abrir o celular.

---

## ATO I — Raízes (carreira inicial)

Consolida a jogatina escolhida. É o "arroz com feijão" que o jogador domina antes de
ser desafiado. Três marcos de dificuldade crescente, com reputação como moeda.

### Variante 🌾 Fazendeiro
- **I.1 — Primeira Safra:** colher e levar soja/quinoa à Cooperativa (o contrato que já existe). +$18.500, +15 rep.
- **I.2 — Janela da Chuva:** prazo apertado (`gameDay` conta) — entregar 3 cargas antes do temporal (`/weather storm`). Falhar = perder parte da safra.
- **I.3 — O Contrato da Cooperativa:** volume grande que **excede a capacidade do trator** → a Cooperativa sugere alugar um caminhão. **Gancho para o Ato II.**

### Variante 🚛 Transportador
- **I.1 — Frete Rodoviário:** o contrato de peças industriais existente. +$24.000, +20 rep.
- **I.2 — Rota Dupla:** dois pontos de coleta, uma entrega — introduz planejamento de rota no mapa tático (`M`).
- **I.3 — Carga Perecível:** exige licença `Refrigerated`; cliente é uma **fazenda** que precisa escoar produção → **gancho para conhecer a origem agrícola (Ato I cruza com Fazendeiro)**.

### Variante 🛩️ Aviador
- **I.1 — Voo de Inspeção:** o contrato postal aéreo existente. +$32.000, +25 rep.
- **I.2 — Resgate de Suprimentos:** entregar remédios a um ponto isolado (sem estrada) — justifica o avião.
- **I.3 — Combustível Caro:** a conta do hangar aperta; voar sozinho não paga. Precisa de **carga terrestre barata para complementar renda** → **gancho para o Ato II**.

### Variante 🎣 Pescador
- **I.1 — Pesca Inicial:** o contrato de pescado existente. +$21.000, +18 rep.
- **I.2 — Maré e Mercado:** o preço do peixe no cais cai; vale a pena levar a captura a uma cidade maior → **precisa de transporte rodoviário**.
- **I.3 — Captura Além da Cota:** volume que a traineira não escoa a tempo → **gancho para o Ato II**.

> **Fecho do Ato I (comum):** reputação ~150 e `completedDeliveries >= 5` destravam o Ato II.
> Um contato do caderno do tio Mateus liga: *"Soube que você tá crescendo. Tenho uma rota pra você."*

---

## ATO II — A Estrada (Transportador)

A modalidade que **conecta** todas as outras. Mesmo o fazendeiro vira transportador aqui.

- **II.1 — A Transportadora:** comprar/financiar o primeiro caminhão (app Finanças, `BankLoan`
  parcelado). Primeira **aposta**: a parcela cobra todo mês (`gameDay`), com multa por atraso.
  Licença alvo: `Standard` → `Heavy`.
- **II.2 — Cadeia de Suprimentos:** encadear 3 entregas (origem → entreposto → cidade) numa
  única viagem longa, exercitando **Floating Origin** e o mapa tático. Introduz **manutenção/reparo** do veículo (`/repair` como recurso pago).
- **II.3 — A Carga Impossível:** um contrato de **alto valor** com prazo curtíssimo entre
  duas cidades distantes. Por estrada não dá no prazo → a oferta sugere **fretar um avião**.
  **Gancho direto para o Ato III.** Exige licença `HighValue`.

> **Desbloqueio:** quitar (ou estar em dia com) o 1º empréstimo + rep ~250.

---

## ATO III — O Céu (Aviador)

Velocidade e alcance. Resolve o que a estrada não alcança.

- **III.1 — Brevê:** tirar a licença `Aviation` (custa dinheiro + um "voo de avaliação"
  como missão). Comprar/financiar o monomotor.
- **III.2 — Ponte Aérea:** cargas leves e urgentes entre regiões/cidades distantes;
  introduz **combustível como restrição real** (autonomia, pouso para reabastecer).
  Clima (`/weather`) vira risco de verdade.
- **III.3 — O Elo Perdido:** um grande cliente quer **frutos do mar frescos** entregues por
  via aérea a um mercado premium no interior. O jogador descobre que a margem está na
  **origem** — pescar barato e vender caro longe. **Gancho para o Ato IV.**

> **Desbloqueio:** rep ~400 + ter operado avião + saldo para a entrada do barco.

---

## ATO IV — A Água (Pescador / Marítimo)

Fecha o ciclo produtivo: o jogador deixa de só **transportar** e passa a **produzir** de novo
(como no Ato I agrícola), agora no mar/rio.

- **IV.1 — De Volta ao Cais:** licença `Maritime`, comprar a embarcação. Primeira captura
  e venda no entreposto.
- **IV.2 — Cadeia do Frescor:** a captura só vale muito se chegar **gelada e rápida** →
  **combina as 3 modalidades anteriores**: barco pesca → caminhão frigorífico leva ao
  aeroporto (`Refrigerated`) → avião entrega na capital. Primeiro **contrato intermodal** real.
- **IV.3 — Tempestade no Mar:** missão-clímax da modalidade com clima adverso e prazo;
  recompensa alta e a reputação que abre o Ato final.

> **Variante sem litoral:** se a região não tem mar/rios navegáveis, o Ato IV vira
> **"A Terra" (Mineração/Carga Pesada)** — extrair minério (`mineralsPercent`) e escoar
> com caminhão `Heavy` + avião, mantendo a lógica "produzir + escoar intermodal".

---

## ATO V — O Grupo (Intermodal)

As 4 jogatinas numa só operação. O jogador agora **orquestra uma frota** (`/vehicles`, `Tab`).

- **V.1 — Operação Intermodal:** um megacontrato que **só fecha se as 4 pontas rodarem**:
  colher (🌾) → pescar (🎣) → consolidar e levar por estrada (🚛) → despachar por ar (🛩️).
  Pode ser cumprido pelo jogador trocando de veículo (`Tab`) ou — se/quando houver IA de
  frota — delegando pontas.
- **V.2 — O Legado:** quitar todas as dívidas, atingir reputação máxima e reerguer o nome
  do tio Mateus como **grupo logístico regional**. Final aberto: a mesma campanha pode recomeçar
  em **outra região do globo** (Nova Carreira), com a dificuldade escalada pela reputação herdada.

---

## Ordem adaptativa dos Atos

Para a estória sempre introduzir algo novo depois do que o jogador domina:

| Carreira inicial | Ato I | Ato II | Ato III | Ato IV |
| :--- | :--- | :--- | :--- | :--- |
| 🌾 Fazendeiro | Raízes (agro) | Estrada | Céu | Água |
| 🚛 Transportador | Raízes (estrada) | Água¹ | Céu | Agro² |
| 🛩️ Aviador | Raízes (céu) | Estrada | Água | Agro² |
| 🎣 Pescador | Raízes (água) | Estrada | Céu | Agro² |

¹ para o transportador, a *novidade* é produzir (pescar); ² o agro entra como ato produtivo
alternativo. O importante: **cada jogador cruza as 4 modalidades**, só muda a ordem de descoberta.

---

## Curva econômica sugerida (ancorada nos valores atuais)

| Fase | Recompensa/contrato | Dívida típica | Reputação |
| :--- | :--- | :--- | :--- |
| Ato 0 | ~$5k | — | 100 → 110 |
| Ato I | $18k–$32k | pequena (herança) | → 150 |
| Ato II | $30k–$60k | 1º caminhão financiado | → 250 |
| Ato III | $50k–$120k | avião financiado | → 400 |
| Ato IV | $40k–$150k | barco financiado | → 600 |
| Ato V | $200k+ (megacontrato) | quitação final | máx |

Mantém os prêmios atuais ($18.5k–$32k) como piso do Ato I e escala ~1,5–2× por ato.

---

## Ganchos de implementação (mínimo viável)

Para rodar a campanha sem reescrever sistemas, bastaria:

1. **`CareerStoryManager`** (novo): guarda `currentAct`/`currentMarco` no `RegionSaveData`
   e escolhe o próximo contrato via `MissionManager.AcceptMarketContract(...)` em vez do
   `GenerateNextContract` aleatório.
2. **Gatilhos de ato:** checar `reputation` + `completedDeliveries` + licença/veículo possuído
   ao concluir contrato (já há o hook em `CompleteActiveContract`).
3. **Diálogos/telemetria:** reaproveitar o banner de conclusão e o chat/console para as
   falas dos "contatos" (texto simples, sem sistema de diálogo novo).
4. **Marcos como `CommercialContract` nomeados** com `title`/`description` fixos + a licença
   exigida (checar `HasLicense`). A licença virar **pré-requisito do marco** cria o portão
   natural entre modalidades.

> Próximo passo natural: transformar os 15 marcos acima numa tabela de dados
> (JSON/ScriptableObject) que o `CareerStoryManager` lê — mantém a narrativa editável
> sem recompilar.
