# World Traffic Signs & Highway Distance System

Coleção procedural de placas de trânsito de padrões mundiais e placas especiais rodoviárias de distância até cidades, geradas para Blender 5.2 e integradas diretamente ao Unity.

## 1. Arquivos Master do Blender
- **`World_Traffic_Signs_Collection.blend`** (na raiz do projeto e em `Assets/_Project/Models/Structures/TrafficSigns/`)
  - Organizado por coleções com materiais PBR retrorefletivos, showroom completo, palco de asfalto, iluminação de estúdio (Key/Fill/Rim) e câmera panorâmica.
- **`traffic_signs_showcase.png`**: Render em alta resolução (1920x1080) de toda a frota de placas em exibição.

---

## 2. Modelos FBX Exportados (Prontos para Unity)
Pivô na base (Z=0 / Y=0) com eixos calibrados (`-Z` forward, `Y` up):

### Padrão Internacional & Europeu (Convenção de Viena)
- `Sign_EU_Stop_Octagon.fbx`: Placa octogonal internacional de parada obrigatória com ferragens e abraçadeiras.
- `Sign_EU_Yield_Triangle.fbx`: Placa triangular invertida de dê a preferência / ceda o passo.
- `Sign_EU_Warning_Curve_Triangle.fbx`: Placa triangular de advertência de curva perigosa.
- `Sign_EU_Speed_80_Circle.fbx`: Placa circular de limite de velocidade (80 km/h).
- `Sign_EU_No_Entry_Circle.fbx`: Placa circular de sentido proibido (fundo vermelho com faixa horizontal).
- `Sign_EU_Mandatory_Roundabout.fbx`: Placa circular azul de sentido giratório obrigatório (rotatória).
- `Sign_EU_Motorway_Entry.fbx`: Placa retangular de início de rodovia/autoestrada com viaduto em perspectiva.

### Padrão Américas & Mercosul (MUTCD EUA & CONTRAN Brasil)
- `Sign_US_Warning_Curve_Diamond.fbx`: Losango amarelo âmbar de advertência de curva com poste perfurado Unistrut/Telespar e parafusos.
- `Sign_US_Speed_Limit_65.fbx`: Retângulo vertical americano "SPEED LIMIT 65".
- `Sign_US_One_Way_Arrow.fbx`: Retângulo horizontal com seta larga "ONE WAY".
- `Sign_US_Interstate_Shield.fbx`: Escudo recortado oficial da Interstate 95 (azul e vermelho).
- `Sign_BR_Warning_Lombada_Diamond.fbx`: Losango padrão Mercosul/Brasil de advertência de lombada (ondulação transversal).
- `Sign_BR_Regulatory_Pare.fbx`: Placa octogonal brasileira de regulamentação "PARE".
- `Sign_BR_Highway_Shield.fbx`: Escudo rodoviário federal DNIT/Mercosul "BR-101".

### Padrão Ásia & Commonwealth (Japão, Reino Unido, Austrália)
- `Sign_JP_Stop_Inverted_Triangle.fbx`: Placa japonesa triangular invertida bilíngue ("止まれ" / "STOP") com manga listrada zebra amarela/preta na base do poste.
- `Sign_UK_National_Speed_Limit.fbx`: Círculo branco com faixa diagonal preta (Limite Nacional de Velocidade do Reino Unido).
- `Sign_AU_Wildlife_Kangaroo.fbx`: Losango amarelo australiano com silhueta de canguru (passagem de animais selvagens).

---

## 3. Placas Especiais Rodoviárias & Distância até Cidades (Mileage / Destination Boards)
- **`Sign_Special_Distance_Paris_Lyon.fbx`**: Placa monumental de confirmação de distância rodoviária europeia (A6):
  - `PARIS 45 km | LYON 285 km | MARSEILLE 520 km`
  - Postes duplos em viga I galvanizada pesada, nervuras traseiras em canal C e sapatas de concreto.
- **`Sign_Special_Distance_NewYork_DC.fbx`**: Placa rodoviária americana Interstate (I-95):
  - `NEW YORK 35 | PHILADELPHIA 110 | WASHINGTON 235` (milhas)
  - Postes duplos com articulação break-away de segurança.
- **`Sign_Special_Distance_SaoPaulo_Campinas.fbx`**: Placa rodoviária Mercosul / Brasil (SP-330 Anhanguera):
  - `SÃO PAULO 90 km | CAMPINAS 180 km | RIBEIRÃO PRETO 370 km`
  - Postes duplos em tubo quadrado de aço e sapatas de fundação.
- **`Sign_Special_Overhead_Highway_Gantry.fbx`**: Grande pórtico rodoviário em treliça metálica espacial (~10m de vão, 6.6m de altura):
  - Duas placas aéreas direcionais suspensas (`CENTRO / DOWNTOWN - NEXT EXIT 1 km` com seta para baixo, `AEROPORTO / AIRPORT - 12 km` com seta reta e escudo de rodovia).
  - Tab superior de saída amarela `EXIT 14B / SAÍDA 14B`.
  - Passarela de manutenção com piso gradeado, guarda-corpos e luminárias projetadas.
- **`Sign_Special_Cantilever_Exit.fbx`**: Semipórtico tipo bandeira em balanço:
  - Mastro tubular de 6m com braço de 4.4m sobre a pista.
  - Placa suspensa `CIDADE INDUSTRIAL / SAÍDA A 500 METROS` com seta inclinada.
- **`Sign_Special_Kilometre_Milestone.fbx`**: Marco quilométrico sólido de concreto usinado à beira da rodovia (`BR-101 / KM 250`).
- **`Sign_Special_Paddle_Mileage_Marker.fbx`**: Marcador fino de poste delimitador de milha/quilômetro (`MILE 142`) com olho-de-gato refletivo.
- **`Sign_Special_LED_Variable_Message.fbx`**: Painel Eletrônico de Mensagem Variável (PMV / VMS) com visor LED âmbar emissivo:
  - `[ ! ] CIDADE X: A 25 KM / TRÂNSITO LENTO - REDUZA`.
  - Estrobos laranjas intermitentes no topo e armário controlador de eletrônica na base.
- **`Sign_Special_City_Welcome_Monument.fbx`**: Monumento arquitetônico de boas-vindas na entrada de cidade:
  - Base e pilares em alvenaria de pedra (`SANTA MARIA / BEM-VINDO - ELEV: 450m - POP: 280.000`) com vigas de madeira nobre e holofotes de chão.

---

## 4. Texturas PBR
Localizadas em `Assets/_Project/Textures/TrafficSigns/`:
- Texturas de alta resolução com desenho vetorial preciso, fontes Highway/Arial Bold, cores calibradas pelas normas internacionais e alpha channel transparente para corte das lâminas de alumínio.
