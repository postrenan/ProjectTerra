# Project Terra 🌍

Simulação e jogo de exploração e gestão global em escala real 1:1 desenvolvido na **Unity 6 (6000.6.3f1)** com pipeline **HDRP (High Definition Render Pipeline)**.

O **Project Terra** combina a observação do globo terrestre a partir do espaço com a descida contínua para uma simulação territorial regional detalhada, permitindo ao jogador gerenciar territórios, explorar topografia real, administrar carreiras econômicas e conduzir veículos em escala métrica 1:1.

---

## 🌟 Principais Funcionalidades

### 1. 🪐 Globo Terrestre Planetário em Alta Definição
- **CubeSphere com LOD Dinâmico (Quadtree):**
  - Subdivisão esférica procedural em 6 faces normalizadas com transição de malha contínua do espaço profundo até níveis de detalhe locais.
  - Correção analítica da costura no antimeridiano (180° de longitude) sem estiramento ou artefatos texturais.
  - Inclinação axial real da Terra ($23,44^\circ$) e rotação sideral configurável.
- **Seleção Territorial Precisa com Texel Snapping:**
  - Mapa de ID binário em resolução 4K cobrindo soberanias globais e divisões subnacionais (países e estados).
  - Raycast analítico de alta precisão contra a esfera planetária convertendo coordenadas polares em latitude/longitude exatas.
  - Shader de destaque territorial (*highlight*) com texel-snapping e efeito Fresnel, evitando sangramento de bordas em geometrias curvas.
- **Câmera Orbital Cinemática:**
  - Navegação orbital suave com zoom exponencial adaptativo à altitude.
  - Efeito de **Mergulho Orbital Cinemático** (*Dive towards region*): interpolação dinâmica e aceleração em direção ao território selecionado no momento da confirmação de desembarque.

### 2. ⚡ Banco de Dados Geográfico Binário Otimizado
- Migração de arquivo textual JSON para formato binário de baixo footprint (`StreamingAssets/regions_database.bin`) com cabeçalho mágico de validação `PTRD`.
- Cobertura de **530+ países, estados e províncias mundiais**, contendo:
  - Centroides e caixas delimitadoras (*Bounding Boxes*).
  - **Dimensões métricas reais 1:1**: cálculo de largura ($m$), comprimento ($m$) e área territorial ($km^2$) geodésica.
  - Indicadores e potenciais econômicos: Cobertura Florestal (🌲), Potencial Mineral (⛏️), Terras Aráveis (🌾) e Recursos Hídricos (💧).

### 3. 💼 Sistema de Saves, Carreiras e Economia Inicial
- Gerenciador de partidas e saves locais por território (`SaveManager` & `RegionSaveData`).
- Configuração de orçamento inicial customizável (presets rápidos ou slider fino de $ \$50.000 $ a $ \$10.000.000 $).
- Escolha da **Carreira Inicial de Empreendimento**:
  - 🌾 **Fazendeiro / Agropecuária:** Foco em produção agrícola, colheita e silos de grãos.
  - 🚛 **Transportador / Logística:** Foco em rotas terrestres, frete de suprimentos e centros de distribuição.
  - 🛩️ **Aviador / Aviação:** Foco em transporte aéreo rápido, pistas de pouso e rotas de longa distância.
  - 🎣 **Pescador / Marítimo:** Foco em operações fluviais e costeiras, docas e comércio marítimo.
- Telas de carregamento temáticas com artes geradas proceduralmente e dicas contextuais adaptadas à carreira e bioma regional selecionados.

### 4. 🏞️ Sandbox Regional em Escala Real 1:1 (`RegionalSandboxScene`)
Ao confirmar a entrada na região, o jogo instancia o ambiente sandbox em escala métrica 1:1 nativo:
- **Terreno Real e Topografia:**
  - Geração métrica do terreno Unity ajustado nas dimensões geodésicas reais da região selecionada (largura, comprimento e altimetria).
  - Multi-camadas de texturas PBR de alta definição (Grama, Solo/Terra, Rocha e Cascalho) sintetizadas via `TerrainPBRFactory`.
  - Hidrografia regional com simulação de rios e corpos d'água dinâmicos.
- **Atmosfera e Iluminação HDRP:**
  - Ciclo Dia/Noite com iluminação solar física e posicionamento celeste georreferenciado.
  - Nevoeiro volumétrico (*Volumetric Fog*) e densidade atmosférica calculada via `HDRPAtmosphereController`.
- **Personagem em Terceira/Primeira Pessoa:**
  - Controlador de personagem (`PlayerCharacterController`) com caminhada, corrida, salto, física suave e sistema de câmera orbital livre.
  - Transição contínua para embarque/desembarque de veículos através da tecla `[E]`.
- **Veículos Físicos Conduzíveis (`VehicleController` & `VehicleBuilder`):**
  - Veículo inicial construído dinamicamente com base na carreira selecionada (Trator, Caminhão de Carga, Aeronave ou Embarcação).
  - Simulação física de tração nas rodas, direção responsiva, marchas e telemetria de velocidade ($km/h$).
- **Sistema de Missões e Contratos Econômicos (`MissionManager`):**
  - Geração de objetivos contratuais com marcadores no mundo 3D (ex: coleta e entrega de cargas entre bases da empresa e o mercado central da cidade).
  - Recompensas financeiras creditadas diretamente no saldo da partida ao completar missões.
- **HUD Integrado & Telemetria (`SandboxHUD`):**
  - Bússola e minimapa com coordenadas métricas relativas.
  - Painel de telemetria veicular (velocímetro analógico/digital, combustível e status do motor).
  - Informações do save, saldo bancário, horário do dia e lista de missões ativas.
  - Suporte a **Floating Origin** para viagens de dezenas e centenas de quilômetros com precisão matemática estável.

---

## 🛠️ Ferramentas e Pipelines (`Tools/`)

Scripts auxiliares em Python integrados ao fluxo de desenvolvimento:
- `Tools/MigrateToBinary.py`: Converte o banco de dados de regiões de JSON para o formato binário otimizado (`.bin`), acelerando o tempo de carregamento em mais de 10x.
- `Tools/UpdateRealWorldDimensions.py`: Calcula analiticamente a extensão física (largura, comprimento em metros e área em $km^2$) de cada fronteira para calibração 1:1.
- `Tools/BatchGeographicDataProcessor.py`: Pipeline para download, conversão de tiles e processamento de relevo SRTM / Terrarium e dados hidrológicos.
- `Tools/ImportPBRTextures.py`: Importador e configurador automatizado de texturas e mapas de normais PBR para o terreno.

---

## 🎮 Controles

### Modo Globo Planetário
| Ação | Comando |
| :--- | :--- |
| **Rotacionar Câmera Orbital** | Segurar e arrastar `Botão Esquerdo` (arrasto longo) ou `Botão Direito` / `Botão do Meio` |
| **Zoom Espacial** | `Roda do Mouse (Scroll)` (do espaço profundo até 50m) |
| **Selecionar Território** | `Clique com Botão Esquerdo` sobre qualquer país ou estado |
| **Gerenciar Partidas & Carreira** | Painel lateral interativo da região selecionada |

### Modo Sandbox Regional
| Ação | Comando |
| :--- | :--- |
| **Movimento do Personagem** | `W`, `A`, `S`, `D` |
| **Correr** | Segurar `Shift Esquerdo` |
| **Pular** | `Espaço` |
| **Olhar / Câmera** | `Movimento do Mouse` |
| **Entrar / Sair do Veículo** | `E` (próximo à porta do veículo) |
| **Acelerar Veículo** | `W` |
| **Frear / Ré do Veículo** | `S` |
| **Curva / Direção** | `A` / `D` |
| **Freio de Mão** | `Espaço` |
| **Alternar Câmera no Veículo** | `C` ou `V` (3ª Pessoa Perto, Panorâmica, Cockpit e Capô) |
| **Lanterna / Faróis Dianteiros** | `L` (Lanterna a pé ou Faróis no veículo) |
| **Alternar Veículo em Posse** | `Tab` (Troca direta entre os veículos do jogador) |
| **Abrir Chat / Console de Comandos** | `/` (ou Teclado Numérico `/`) |
| **Mapa Regional Tático GIS** | `M` |
| **Pausar / Menu de Opções** | `Esc` ou `Alt Esquerdo` |

---

### 💬 Console de Comandos In-Game (`/`)

Pressione a tecla `/` para abrir o chat de comandos com auto-complete (`Tab`) e histórico (`Setas Cima/Baixo`):

| Comando | Descrição | Exemplo |
| :--- | :--- | :--- |
| `/help [comando]` | Exibe o manual ou ajuda específica | `/help spawn` |
| `/spawn <tipo>` | Cria veículos na sua posse | `/spawn truck`, `/spawn car`, `/spawn plane`, `/spawn police`, `/spawn all` |
| `/time <hora/preset>` | Altera o horário solar (0 a 24) ou presets | `/time 14`, `/time day`, `/time night`, `/time sunset`, `/time cycle on` |
| `/timescale <mult>` | Altera a velocidade da simulação | `/timescale 2`, `/timescale 0.5` |
| `/weather <clima>` | Altera condições atmosféricas e neblina | `/weather clear`, `/weather fog`, `/weather storm`, `/weather overcast` |
| `/tp <local/coords>` | Teleporta jogador e veículo | `/tp town`, `/tp base`, `/tp airport`, `/tp 1500 -800` |
| `/speed <mult>` | Multiplicador de velocidade a pé | `/speed 3`, `/speed 1` |
| `/fly` | Alterna modo voo livre (Noclip) | `/fly` (WASD + Espaço/Ctrl) |
| `/god` | Combustível infinito e invulnerabilidade | `/god` |
| `/money <quantia>` | Adiciona fundos ao saldo da partida | `/money 500000` |
| `/setmoney <quantia>` | Define o saldo monetário exato | `/setmoney 1000000` |
| `/refuel` | Abastece o tanque a 100% | `/refuel` |
| `/repair` | Repara e desvira o veículo atual | `/repair` |
| `/unflip` | Alinha o veículo capotado ao solo | `/unflip` |
| `/cargo <load/unload>` | Gerencia a carga do veículo | `/cargo load Soja`, `/cargo unload` |
| `/mission <complete/new>` | Conclui ou gera contratos | `/mission complete` |
| `/vehicles` | Lista toda a frota com distâncias | `/vehicles` |
| `/switch [índice]` | Troca para veículo específico | `/switch 2` |
| `/clearvehicles` | Remove veículos adicionais criados | `/clearvehicles` |
| `/light` | Alterna lanterna ou faróis | `/light` |
| `/camera <modo>` | Altera visão da câmera veicular | `/camera cockpit`, `/camera hood`, `/camera far` |
| `/coords` | Exibe coordenadas X, Y, Z e rumo | `/coords` |
| `/save` | Salva a partida imediatamente | `/save` |
| `/clear` | Limpa o log do chat | `/clear` |

---

## 📂 Estrutura do Projeto

```
testejogo/
├── Assets/
│   ├── _Project/
│   │   ├── Scenes/
│   │   │   ├── MainEarthScene.unity           # Cena orbital do globo terrestre
│   │   │   └── RegionalSandboxScene.unity     # Cena de exploração e gestão 1:1
│   │   ├── Scripts/
│   │   │   ├── Camera/                        # Controladores orbitais e cinemáticos
│   │   │   ├── Core/                          # Sistemas de precisão e floating origin
│   │   │   ├── Gameplay/                      # Gerenciador de saves e economia
│   │   │   ├── Planet/                        # CubeSphere, LOD, banco geográfico e shaders
│   │   │   │   └── TerrainStreaming/          # Download e processamento de elevação
│   │   │   └── Sandbox/                       # Personagem, veículos, atmosfera e missões
│   │   ├── Shaders/                           # Shaders de atmosfera, relevo e highlight
│   │   └── Textures/                          # Texturas PBR, ID maps e relevo
│   └── StreamingAssets/
│       ├── GeographicData/                    # Tiles e dados topográficos locais
│       └── regions_database.bin               # Banco de dados binário de países/estados
├── ProjectSettings/                           # Configurações do projeto e Build Settings
└── Tools/                                     # Ferramentas Python para processamento de dados
```

---

## ⚙️ Tecnologias Utilizadas

- **Engine:** Unity 6 (6000.6.3f1)
- **Render Pipeline:** High Definition Render Pipeline (HDRP)
- **Linguagens:** C# (.NET Standard 2.1) & Python 3
- **Datasets Geográficos:** Natural Earth Data 10m (Admin 0 & Admin 1), Mapzen/AWS Terrarium Elevation
