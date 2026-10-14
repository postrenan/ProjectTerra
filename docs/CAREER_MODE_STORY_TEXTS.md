# Modo Carreira — Textos dos Marcos + Campanha Completa do Pescador

> Complemento de [CAREER_MODE_PROGRESSIVE_STORY.md](CAREER_MODE_PROGRESSIVE_STORY.md).
> **Parte 1:** textos prontos para colar (títulos, descrições, falas dos contatos e
> valores) de todos os marcos. **Parte 2:** a campanha completa de uma carreira (Pescador),
> do Ato 0 ao Ato V, com a ordem adaptativa própria dela.

## Convenções

Cada marco segue o formato do `CommercialContract` que já existe no `MissionManager`.
Sugiro **dois campos novos** para suportar a estória (opcionais, com default seguro):

```csharp
public DriverLicense requiredLicense = DriverLicense.None; // portão de modalidade
public string storyId = "";   // ex.: "ACT1_FARM_01" — liga o marco ao CareerStoryManager
```

- **Fala do contato** = texto para o banner/console (reaproveita `completionBannerMessage`
  e o chat do console). Prefixo sugerido: `📞`, `📻` (rádio), `📱` (app), `📬` (carta).
- **Valores** estão calibrados na curva do doc principal (piso $18k–$32k no Ato I, ~1,5–2× por ato).
- Posições (`pickupLocation`/`deliveryLocation`) usam os pontos que o sandbox já gera:
  `starterBasePosition` e `townCenterPosition`. Marcos intermodais usam `airportPos`/`docksPos`.

---

# PARTE 1 — Textos de todos os marcos

## ATO 0 — Prólogo: A Herança

### 0.1 — As Chaves
- **requiredLicense:** `None`
- **cargo:** "Caixa de Pertences do Avô" · **peso:** 120 kg
- **reward:** $5.000 · **rep:** +10
- **title:** `As Chaves`
- **description:** `O tabelião entregou as chaves da velha base e um caderno de contatos.
  Leve a caixa de pertences do seu tio Mateus da base até o Mercado Municipal e conheça a cidade.`
- 📬 *Abertura (carta):* "Se você está lendo isto, o velho se foi — e deixou tudo pra você.
  Não é muito: uma base, umas dívidas e o nome Mendes. Faça valer."
- ✅ *Conclusão:* "Primeira entrega feita. O pessoal do Mercado já conhece o sobrenome.
  Abra o celular — tem gente querendo falar com você."

> Ao concluir: concede a **licença base da carreira** e destrava o Ato I.

---

## ATO I — Raízes (varia por carreira)

> O Ato I é a jogatina escolhida. Abaixo o **spine canônico (🌾 Fazendeiro)**.
> As variantes 🚛/🛩️/🎣 estão na Parte 2 (Pescador completo) e resumidas no doc principal.

### I.1 — Primeira Safra (🌾)
- **requiredLicense:** `Agriculture`
- **cargo:** "Soja em Grãos" · 1.200 kg · **reward:** $18.500 · **rep:** +15
- **title:** `Primeira Safra: Grãos para a Cooperativa`
- **description:** `Engate a carga de soja colhida no trator e leve até os Silos da
  Cooperativa Central da Cidade.`
- 📻 *Rádio rural:* "Cooperativa chamando o sítio Mendes — a balança abre às seis.
  Traga a primeira carga que a gente acerta na hora."
- ✅ *Conclusão:* "Safra pesada na balança, dinheiro na conta. O gerente perguntou se você
  dá conta de volume maior..."

### I.2 — Janela da Chuva (🌾)
- **requiredLicense:** `Agriculture`
- **cargo:** "Soja (lote 2 de 3)" · 1.400 kg · **reward:** $21.000 · **rep:** +18
- **title:** `Janela da Chuva`
- **description:** `O temporal chega em horas. Entregue os três lotes restantes à
  Cooperativa antes da chuva estragar o grão. Corra.`
- 📱 *App Clima:* "Alerta: tempestade severa se aproximando da região. Colheita exposta
  em risco."
- ✅ *Conclusão:* "Entrou tudo seco no silo, no limite. A Cooperativa tem uma proposta
  grande — grande demais pro seu trator sozinho."

### I.3 — O Contrato da Cooperativa (🌾)
- **requiredLicense:** `Agriculture`
- **cargo:** "Safra Consolidada" · 4.000 kg · **reward:** $28.000 · **rep:** +25
- **title:** `O Contrato da Cooperativa`
- **description:** `Volume alto demais para o trator em uma viagem. A Cooperativa indica
  um caminhão de aluguel no pátio. Faça a entrega em lotes ou alugue a carreta.`
- 📞 *Contato do caderno (Dona Íris, transportadora):* "Soube que você tá crescendo, menino.
  Grão bom não espera trator. Passa na minha garagem — tenho uma rota e um caminhão pra você."
- ✅ *Conclusão:* "A Cooperativa fechou contrato fixo. E a Dona Íris não sai da sua cabeça:
  talvez transportar seja onde o dinheiro de verdade está."

> **Fecho do Ato I:** `reputation >= 150` e `completedDeliveries >= 5` → destrava Ato II.

---

## ATO II — A Estrada (🚛 Transportador)

### II.1 — A Transportadora
- **requiredLicense:** `Standard`
- **cargo:** "Peças Industriais" · 2.500 kg · **reward:** $30.000 · **rep:** +20
- **title:** `A Transportadora`
- **description:** `Financie seu primeiro caminhão no app Finanças e faça o frete de
  estreia: peças industriais da base ao Centro de Distribuição Municipal.`
- 📞 *Dona Íris:* "O caminhão é seu — e a parcela também. Todo mês ela bate na porta.
  Rode certo que a estrada paga. Rode errado e o banco cobra com juros."
- ✅ *Conclusão:* "Primeiro frete pago, primeira parcela no horizonte. Agora você é
  transportador de verdade."

### II.2 — Cadeia de Suprimentos
- **requiredLicense:** `Heavy`
- **cargo:** "Suprimentos Consolidados" · 6.000 kg · **reward:** $45.000 · **rep:** +30
- **title:** `Cadeia de Suprimentos`
- **description:** `Uma viagem longa, três paradas: colete no entreposto, reabasteça no
  posto e entregue na cidade distante. Olho no combustível e no desgaste do caminhão.`
- 📻 *Despachante:* "Rota tripla hoje. Se o motor fritar no meio do nada, o reparo sai
  do seu bolso. Dirige com juízo."
- ✅ *Conclusão:* "Entregou tudo e trouxe o caminhão inteiro. Aprendeu que manutenção
  é custo — e que distância é dinheiro."

### II.3 — A Carga Impossível
- **requiredLicense:** `HighValue`
- **cargo:** "Equipamento Hospitalar" · 1.800 kg · **reward:** $60.000 · **rep:** +35
- **title:** `A Carga Impossível`
- **description:** `Equipamento caríssimo, duas cidades distantes, prazo curtíssimo. Por
  estrada você não chega a tempo. O cliente sugere: e se fosse de avião?`
- 📞 *Comandante Vega (piloto aposentado):* "Ouvi no rádio que você tem carga boa e
  pouco tempo. Estrada tem limite, garoto. O céu não. Me procura no campo de pouso."
- ✅ *Conclusão:* "Você rateou o frete com um avião e salvou o prazo. A conta fechou — e
  uma ideia não sai da cabeça: e se o avião fosse seu?"

> **Desbloqueio:** 1º empréstimo em dia + `reputation >= 250` → destrava Ato III.

---

## ATO III — O Céu (🛩️ Aviador)

### III.1 — Brevê
- **requiredLicense:** `None` → concede `Aviation` ao concluir
- **cargo:** "Instrutor de Voo (avaliação)" · 90 kg · **reward:** $0 (paga-se a licença) · **rep:** +15
- **title:** `Brevê`
- **description:** `Pague o curso no app Finanças e cumpra o voo de avaliação: decole,
  contorne a antena repetidora e pouse na pista. Aprovado, o brevê é seu.`
- 📞 *Comandante Vega:* "Sem brevê você não tira o avião do chão legalmente. Faz o voo de
  prova comigo. Suave nos controles, firme no pouso."
- ✅ *Conclusão:* "Licença de piloto no bolso. Agora o monomotor é ferramenta de trabalho."

### III.2 — Ponte Aérea
- **requiredLicense:** `Aviation`
- **cargo:** "Encomendas Expressas" · 300 kg · **reward:** $75.000 · **rep:** +30
- **title:** `Ponte Aérea`
- **description:** `Cargas leves e urgentes entre cidades distantes. Vigie a autonomia:
  reabasteça antes de cruzar. Se o tempo fechar (`/weather`), decida voar ou esperar.`
- 📻 *Torre:* "Vento de través na pista distante e teto baixo a caminho. Combustível conta
  cada litro. Boa sorte, aviador."
- ✅ *Conclusão:* "Entregas que a estrada levaria dias, feitas numa manhã. O negócio voa —
  literalmente."

### III.3 — O Elo Perdido
- **requiredLicense:** `Refrigerated` (a carga exige frio)
- **cargo:** "Pescado Fresco Premium" · 400 kg · **reward:** $110.000 · **rep:** +40
- **title:** `O Elo Perdido`
- **description:** `Um restaurante premium na capital quer frutos do mar frescos entregues
  por via aérea. A margem é enorme — mas está na origem. Quem controla a pesca, lucra duas vezes.`
- 📞 *Marina (mestra de traineira):* "Você voa o peixe, mas compra caro de terceiros. O ouro
  tá na água, não no ar. Vem comigo pro cais que eu te mostro."
- ✅ *Conclusão:* "O cliente premium virou fixo. E Marina plantou a semente: por que pagar
  pelo peixe se você pode pescá-lo?"

> **Desbloqueio:** operou avião + `reputation >= 400` + saldo p/ entrada do barco → Ato IV.

---

## ATO IV — A Água (🎣 Pescador / Marítimo)

> **Se a região não tem mar/rios navegáveis:** este ato vira **"A Terra" (Mineração)** —
> ver variante no fim desta Parte 1.

### IV.1 — De Volta ao Cais
- **requiredLicense:** `None` → concede `Maritime` ao concluir
- **cargo:** "Pescado Fresco" · 850 kg · **reward:** $40.000 · **rep:** +25
- **title:** `De Volta ao Cais`
- **description:** `Tire a licença náutica, financie a embarcação e faça a primeira
  captura: siga até a boia oceânica, pesque a cota e descarregue no Entreposto do Mercado.`
- 📞 *Marina:* "Mar não perdoa amador. Respeita a maré, respeita a cota. Traz a primeira
  captura que eu te apresento ao entreposto."
- ✅ *Conclusão:* "Primeira pesca vendida. O cheiro de maresia agora é o cheiro do lucro."

### IV.2 — Cadeia do Frescor
- **requiredLicense:** `Maritime` + `Refrigerated` + `Aviation` (intermodal!)
- **cargo:** "Lote Premium Refrigerado" · 1.100 kg · **reward:** $130.000 · **rep:** +45
- **title:** `Cadeia do Frescor`
- **description:** `O frescor vale ouro se chegar gelado e rápido. Pesque com o barco,
  transfira ao caminhão frigorífico até o aeroporto e despache no avião rumo à capital.
  Três veículos, uma carga, um relógio correndo.`
- 📱 *App Contrato:* "Rede de restaurantes exige entrega D+0. Quebra de frio = contrato
  cancelado. Coordene as três pontas."
- ✅ *Conclusão:* "Barco, caminhão e avião numa só corrida contra o tempo — e deu certo.
  Você não transporta mais carga dos outros: você controla a cadeia inteira."

### IV.3 — Tempestade no Mar
- **requiredLicense:** `Maritime`
- **cargo:** "Captura Recorde" · 1.600 kg · **reward:** $95.000 · **rep:** +50
- **title:** `Tempestade no Mar`
- **description:** `A maior cota da temporada, com frente fria chegando. Pesque e volte ao
  cais antes da tempestade virar a traineira. Alta recompensa, alto risco.`
- 📻 *Rádio costeiro:* "Aviso aos navegantes: mar grosso em duas horas. Quem tá fora,
  volta agora."
- ✅ *Conclusão:* "Voltou com a captura recorde e a embarcação inteira. O nome Mendes
  agora manda no cais, na estrada e no céu."

> **Desbloqueio:** `reputation >= 600` → destrava Ato V.

---

## ATO V — O Grupo (Intermodal)

### V.1 — Operação Intermodal
- **requiredLicense:** `Agriculture | Maritime | Refrigerated | Aviation` (todas)
- **cargo:** "Remessa Integrada Regional" · 5.000 kg · **reward:** $200.000 · **rep:** +80
- **title:** `Operação Intermodal`
- **description:** `O megacontrato da temporada só fecha com as quatro pontas rodando:
  colha o grão, pesque o lote, consolide tudo por estrada e despache por ar. Troque de
  veículo (Tab) e orquestre a operação do início ao fim.`
- 📞 *Dona Íris, Vega e Marina (conferência):* "Um ano atrás você era um moleque com um
  trator velho. Hoje a região inteira depende das suas rodas, cascos e asas. Fecha esse
  contrato e você entra pra história."
- ✅ *Conclusão:* "Quatro modalidades, uma só operação, um só nome. Você reergueu o império
  do velho — e foi além dele."

### V.2 — O Legado
- **requiredLicense:** `None`
- **cargo:** "Escritura do Grupo Mendes" · 0 kg · **reward:** $0 (quitação simbólica) · **rep:** +100
- **title:** `O Legado`
- **description:** `Quite todas as dívidas e registre o Grupo Logístico Mendes na
  prefeitura. O empreendimento falido do seu tio Mateus agora é o maior operador multimodal da região.`
- 📬 *Fecho (carta ao tio Mateus):* "Deu certo, tio. Não é mais 'umas dívidas e um nome'. É um
  grupo. E ainda tem mundo lá fora pra conquistar."
- ✅ *Conclusão (final aberto):* "Fim da campanha regional. Inicie uma Nova Carreira em
  outra região do globo — a reputação herdada escala o desafio."

---

## Variante de região sem água — ATO IV alternativo: "A Terra" (Mineração)

Usado quando `hasCoastline == false && hasRivers == false && hasLakes == false`
(desertos, platôs, interiores áridos). Mantém o papel de "voltar a produzir + escoar intermodal".

### IV-T.1 — A Concessão
- **requiredLicense:** `Heavy` · **cargo:** "Minério Bruto" · 8.000 kg · **reward:** $45.000 · **rep:** +25
- **title:** `A Concessão`
- **description:** `A região tem veios minerais inexplorados. Financie o caminhão basculante,
  carregue o minério na frente de lavra e leve ao pátio de beneficiamento.`
- 📞 *Engenheiro Salas:* "Aqui não tem mar, garoto — tem montanha. E montanha tem ouro
  cinza. Peso pesado, licença pesada. Topa?"

### IV-T.2 — Linha de Beneficiamento
- **requiredLicense:** `Heavy | Hazmat` · **cargo:** "Concentrado Mineral" · 7.000 kg · **reward:** $120.000 · **rep:** +45
- **title:** `Linha de Beneficiamento`
- **description:** `Extraia, transporte o concentrado por estrada e despache a amostra de
  alta pureza por avião ao comprador. A cadeia mineral intermodal em ação.`

### IV-T.3 — O Veio Principal
- **requiredLicense:** `Heavy` · **cargo:** "Carga Recorde de Minério" · 10.000 kg · **reward:** $95.000 · **rep:** +50
- **title:** `O Veio Principal`
- **description:** `O maior carregamento da mina, terreno difícil, prazo da fundição.
  Desça a serra carregado sem perder o eixo. Alto risco, alta recompensa.`

---

# PARTE 2 — Campanha Completa: 🎣 Pescador

Exemplo detalhado de uma carreira inteira, com a **ordem adaptativa** própria do Pescador:
ele já domina a água, então a *novidade* vem da estrada, do céu e — por último — da terra/agro.

```
Pescador:  Ato I (Água) → Ato II (Estrada) → Ato III (Céu) → Ato IV (Agro) → Ato V (Intermodal)
```

**Premissa:** você é **Roger Mendes** e herda a traineira enferrujada do **tio Mateus**,
atracada num cais decadente. O caderno dele tem três nomes rabiscados: *Íris, Vega, e um
tal "Sítio Aurora"*.

## Ato 0 — As Chaves (cais)
- **0.1 — O Casco Velho** · cargo "Rede e Pertences" 120 kg · $5.000 · +10
  - **desc:** `Leve os pertences do tio Mateus da traineira ao Mercado do Cais e conheça o
    entreposto. A velha Marujo I ainda flutua — por pouco.`
  - 📬 "A Marujo I é sua agora. Ela já viu mares piores que suas dúvidas. Trate-a bem."
  - *concede licença `Maritime` base.*

## Ato I — Raízes: A Água
- **I.1 — Primeira Pesca** · "Pescado Fresco" 850 kg · $21.000 · +18 · lic `Maritime`
  - **desc:** `Navegue até a boia oceânica, pesque a cota de pescada e descarregue no
    Entreposto do Mercado Municipal.`
  - 📻 "Marujo I, aqui é o entreposto. Boia azul, duas milhas a leste. Enche o porão e volta."
  - ✅ "Primeira cota vendida. O peixe tá bom — mas o preço do cais, nem tanto."
- **I.2 — Maré e Mercado** · "Pescado (lote premium)" 900 kg · $24.000 · +20 · lic `Maritime`
  - **desc:** `O preço despencou no cais. A cidade grande paga o dobro pelo mesmo peixe —
    mas fica longe, por terra. Entregue o lote fresco antes que perca o frescor.`
  - 📱 App Preços: "Cais local: baixa de 40%. Mercado Central (cidade): alta histórica."
  - ✅ "Vendido caro na cidade. Lição: pescar é metade; escoar é a outra metade."
- **I.3 — Captura Além da Cota** · "Supercaptura" 1.800 kg · $27.000 · +25 · lic `Maritime`
  - **desc:** `Cardume gigante apareceu. Sua traineira não escoa tudo a tempo sozinha —
    o peixe estraga no porão. Você precisa de rodas.`
  - 📞 *Dona Íris:* "Peixe parado é peixe perdido, pescador. Tenho um caminhão parado e uma
    garagem seca. Vamos conversar antes que esse cardume vire adubo."
  - ✅ "Salvou a captura rateando com um caminhão. A estrada te chamou — e você ouviu."
  - *fecho Ato I: `reputation >= 150` + `completedDeliveries >= 5`.*

## Ato II — A Estrada
- **II.1 — A Garagem da Íris** · "Pescado + Gelo" 2.500 kg · $32.000 · +20 · lic `Standard`
  - **desc:** `Financie seu primeiro caminhão e leve a captura gelada do cais ao Centro de
    Distribuição da cidade. A parcela vence todo mês — rode certo.`
  - 📞 *Íris:* "O caminhão é seu. O banco também acha que é um pouco dele. Não atrasa parcela
    que juros mordem mais que tubarão."
- **II.2 — Corrente Fria** · "Perecíveis Consolidados" 5.000 kg · $48.000 · +30 · lic `Refrigerated`
  - **desc:** `Tire a licença de carga frigorífica e faça a rota tripla: cais → entreposto →
    cidade distante, sem quebrar a cadeia de frio. Combustível e manutenção contam.`
  - 📻 Despachante: "Quebrou o frio, perdeu a carga. Simples assim. Olho no termômetro e no tanque."
- **II.3 — Carga que Não Espera** · "Lote Premium Urgente" 1.500 kg · $62.000 · +35 · lic `HighValue`
  - **desc:** `Restaurante estrelado na capital, prazo de horas, estrada longa demais. Por
    terra o peixe chega morto de velho. Tem que ser pelo ar.`
  - 📞 *Comandante Vega:* "Pescador virou caminhoneiro, e agora quer o céu? Me acha no campo
    de pouso. Carga que não espera é especialidade minha."
  - *desbloqueio Ato III: `reputation >= 250` + empréstimo em dia.*

## Ato III — O Céu
- **III.1 — Brevê do Mar pro Ar** · "Voo de avaliação" 90 kg · $0 · +15 · concede `Aviation`
  - **desc:** `Pague o curso e faça o voo de prova com o Vega: decole, contorne a antena e
    pouse suave. O brevê abre os céus pro seu peixe.`
  - 📞 *Vega:* "Pousar é como atracar: tudo nos últimos segundos. Firme."
- **III.2 — Ponte do Pescado** · "Frutos do Mar Expressos" 350 kg · $78.000 · +30 · lic `Aviation|Refrigerated`
  - **desc:** `Entregas aéreas de frutos do mar frescos entre o litoral e cidades do
    interior. Vigie autonomia e clima — frente fria fecha pista.`
  - 📻 Torre: "Teto baixo no interior, vento de través. Decide: voa ou espera o peixe azedar."
- **III.3 — O Cliente de Ouro** · "Pescado Premium Refrigerado" 420 kg · $115.000 · +40 · lic `Aviation|Refrigerated`
  - **desc:** `Uma rede de restaurantes quer fornecimento aéreo fixo. A margem é absurda —
    mas eles querem rastreabilidade: de onde vem o peixe, como chega. Falta você dominar a
    origem em terra também.`
  - 📞 *"Sítio Aurora" (o terceiro nome do caderno):* "O tio Mateus comprava nossos grãos
    pra isca e ração. A gente ainda planta. Se quer fechar a cadeia, vem ver a lavoura."
  - *desbloqueio Ato IV: `reputation >= 400`.*

## Ato IV — A Terra (Agro, como novidade final)
- **IV.1 — O Sítio Aurora** · "Grãos para Ração/Isca" 1.200 kg · $42.000 · +25 · concede `Agriculture`
  - **desc:** `Tire a licença agrícola e reative a parceria do seu tio Mateus com o Sítio Aurora:
    colha os grãos e leve à base. Fechar a origem reduz seu custo em toda a cadeia.`
  - 📞 *Sítio Aurora:* "O tio Mateus plantou essa parceria. Literalmente. Hora de colher."
- **IV.2 — Da Terra ao Prato** · "Insumo + Pescado Integrado" 3.500 kg · $135.000 · +45 · lic `Agriculture|Refrigerated|Aviation`
  - **desc:** `Primeira operação que une as quatro pontas da SUA cadeia: grão do sítio →
    ração no cais → peixe no porão → caminhão frigorífico → avião à capital. Prova de conceito
    do grupo.`
  - 📱 Contrato: "Fornecimento verticalizado: do solo ao mar ao prato, rastreado. D+0."
- **IV.3 — Safra e Maré** · "Dupla Colheita" 4.000 kg · $98.000 · +50 · lic `Agriculture`
  - **desc:** `Colheita e cota de pesca caem na mesma semana, com tempo virando no mar e
    chuva na lavoura. Orquestre as duas produções sem perder nenhuma. O teste final antes do grupo.`
  - 📻 "Dois fronts, um operador. Mostra que você aguenta o grupo inteiro."
  - *desbloqueio Ato V: `reputation >= 600`.*

## Ato V — O Grupo
- **V.1 — Operação Maré Cheia** · "Remessa Integrada Regional" 5.000 kg · $200.000 · +80 · lic `Agriculture|Maritime|Refrigerated|Aviation`
  - **desc:** `O megacontrato: colha no Aurora, pesque no mar, consolide por estrada,
    despache por ar — tudo num ciclo só. Troque de veículo (Tab) e comande as quatro pontas.`
  - 📞 *Íris, Vega, Aurora e a memória de Marina/tio Mateus:* "Começou com uma traineira
    enferrujada. Olha você agora, moleque."
- **V.2 — O Legado do tio Mateus** · "Escritura do Grupo" 0 kg · $0 · +100
  - **desc:** `Quite tudo e registre o Grupo Mendes. A traineira falida virou o maior
    operador multimodal do litoral.`
  - 📬 *Carta ao tio Mateus:* "A Marujo I ainda flutua, tio. Só que agora ela não pesca sozinha —
    tem uma frota atrás dela. Deu certo."
  - *final aberto: Nova Carreira em outra região, dificuldade escalada pela reputação.*

---

## Como ligar isto ao código (resumo)

1. Adicionar `requiredLicense` e `storyId` ao `CommercialContract`.
2. `CareerStoryManager` guarda `currentStoryId` no `RegionSaveData` e, em
   `CompleteActiveContract`, em vez de `GenerateNextContract` (aleatório), chama o próximo
   marco da tabela via `AcceptMarketContract(...)`, checando `HasLicense(requiredLicense)`.
3. Falas 📞/📻/📱/📬 entram no banner (`completionBannerMessage`) e/ou no log do console.
4. Marcos viram linhas de uma tabela de dados (JSON/ScriptableObject) — texto editável sem recompilar.
5. A ordem adaptativa (Parte 2) é só a sequência de `storyId` escolhida conforme
   `activeSave.starterCareer` na inicialização da campanha.
