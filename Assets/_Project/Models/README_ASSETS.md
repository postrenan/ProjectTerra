# Catálogo de Modelos 3D de Alta Qualidade (CC0 / Domínio Público)

Este diretório contém a biblioteca de ativos 3D importada para o projeto, cobrindo todos os requisitos visuais para o ambiente, cidades, veículos e vida animal em escala real.

## 📜 Licença e Termos de Uso
Todos os ativos incluídos nesta biblioteca foram disponibilizados sob a licença **Creative Commons Zero (CC0 1.0 Universal - Public Domain Dedication)** por seus respectivos criadores:
- **Kenney (Kenney.nl)**: *Nature Kit, Car Kit, City Commercial Kit, City Suburban Kit, Pirate Kit, Space Kit.*
- **Quaternius (Quaternius.com)**: *Farm Animals Pack, Wild Animals Pack Vol 1 & Vol 2.*

> [!NOTE]
> A licença CC0 permite uso comercial e não-comercial irrestrito, modificação, redistribuição e inclusão no projeto sem necessidade de royalties.

---

## 📁 Estrutura de Diretórios e Ativos Importados

### 1. 🌲 Natureza (`Assets/_Project/Models/Nature/`)
- **Árvores (`Trees/`)** (183 arquivos):
  - Árvores coníferas/pinheiros (`tree_cone.fbx`), carvalhos (`tree_default.fbx`), modelos detalhados (`tree_detailed.fbx`), blocos estilizados (`tree_blocks.fbx`), variações outonais e escuras (`tree_*_fall.fbx`, `tree_*_dark.fbx`).
  - Formatos: FBX e OBJ com arquivos de materiais MTL.
- **Rochas e Escarpas (`Rocks/`)** (172 arquivos):
  - Pedregulhos grandes (`rock_largeA` a `rock_largeF`), rochas médias e pequenas (`rock_smallA` a `rock_smallI`), formações rochosas altas/pilares (`rock_tallA` a `rock_tallJ`), penhascos e blocos de encosta (`cliff_block_rock`, `cliff_cave_rock`).
- **Vegetação Rasteira (`Foliage/`)** (58 arquivos):
  - Arbustos (`plant_bush`, `plant_bushDetailed`, `plant_bushLarge`), plantas rasteiras, flores, cogumelos e troncos caídos (`log_*`).

---

### 2. 🐾 Fauna e Animais (`Assets/_Project/Models/Animals/`)
- **Animais de Fazenda (`Farm/`)** (21 arquivos):
  - 🐮 Vaca (`Cow.fbx`)
  - 🐴 Cavalo (`Horse.fbx`)
  - 🐑 Ovelha (`Sheep.fbx`)
  - 🐷 Porco (`Pig.fbx`)
  - 🦙 Lhama (`Llama.fbx`)
  - 🐶 Pug / Cão Pastor (`Pug.fbx`)
  - 🦓 Zebra (`Zebra.fbx`)
- **Animais Selvagens e Aquáticos (`Wild/`)** (30 arquivos):
  - 🐺 Lobo (`Wolf.fbx`)
  - 🦊 Raposa Vermelha (`Red Fox.fbx`)
  - 🦅 Águia (`Eagle.fbx`)
  - 🐦 Pássaro e Pintinho (`bird.fbx`, `Chick.fbx`)
  - 🐱 Gato doméstico (`Cat.fbx`)
  - 🐕 Cão de caça (`Dog.fbx`)
  - 🐟 Peixe e Piranha (`Fish.fbx`, `Piranha.fbx`)
  - 🐋 Baleia (`Whale.fbx`)

---

### 3. 🚗 Veículos (`Assets/_Project/Models/Vehicles/`)
- **Agrícolas (`Agricultural/`)**:
  - Trator clássico (`tractor.fbx`), Trator com pá carregadeira (`tractor-shovel.fbx`), rodas traseiras e dianteiras articuladas.
- **Pesados e Transporte (`Heavy/`)**:
  - Caminhão de carga (`truck.fbx`), Caminhão prancha (`truck-flat.fbx`), Furgão de entrega (`delivery.fbx`, `delivery-flat.fbx`), Caminhão de lixo e Bombeiros.
- **Civis e Passeio (`Civilian/`)**:
  - Sedans (`sedan.fbx`, `sedan-sports.fbx`), SUVs (`suv.fbx`, `suv-luxury.fbx`), Perua/Van (`van.fbx`), Táxi (`taxi.fbx`), Carros de corrida.
- **Emergência (`Emergency/`)**:
  - Ambulância (`ambulance.fbx`), Viatura Policial (`police.fbx`).
- **Marítimos (`Maritime/`)**:
  - Barcos a remo (`boat-row-large.fbx`), Traineiras e navios de pequeno/médio porte (`ship-small.fbx`, `ship-medium.fbx`).
- **Aéreos (`Aviation/`)**:
  - Aerodeslizadores utilitários e cargueiros leves (`craft_cargoA.fbx`, `craft_speederA.fbx`).
- **Texturas**: Atlas otimizado `colormap.png` para rendering de drawcall único.

---

### 4. 🏢 Estruturas e Edifícios (`Assets/_Project/Models/Structures/`)
- **Comerciais e Urbanos (`Commercial/`)** (19 arquivos):
  - Prédios comerciais (`building-a` a `building-n`), arranha-céus e edifícios administrativos (`building-skyscraper-a` a `building-skyscraper-e`).
- **Suburbanos e Rurais (`Suburban/`)** (21 arquivos):
  - Casas residenciais, chalés, garagens, armazéns e estruturas agrícolas (`building-type-a` a `building-type-s`).
- **Downtown City MegaKit (`DowntownCity/`)** (153 arquivos FBX + 33 texturas PBR):
  - Paredes modulares (`Metal_FirstFloor_Wall`, `Metal_FirstFloor_Window`), cornijas e cantos (`Trim_Corner`, `Trim_FirstFloor_Wall`).
  - Entradas e escadarias (`Door_1`, `Door_2`, `Entrance_Concrete_2x1`, `Stairs_Entrance_Concrete`).
  - Vias e calçadas modulares (`Street_2Lane`, `Street_4Lane`, `Sidewalk_Straight_3m`, `Sidewalk_Planter`).
  - Mobiliário e adereços urbanos (`Prop_Bollard`, `Prop_Planter_Single`, `Prop_ACUnit`, `Prop_ManholeCover`, `Prop_Drain`).
  - Texturas PBR completas (Albedo/BaseColor, Normal Maps e ORM).

---

### 5. 🌲 Cenário Florestal Realista (`Assets/_Project/Models/Nature/RealisticForest/`)
- **Coníferas Realistas (`Conifers/`)**: Modelos fotorrealistas *Book of the Dead* (`PF Conifer Tall BOTD`, `PF Conifer Medium BOTD`, `PF Conifer Small BOTD`, `PF Conifer Bare BOTD`) com LODs e shaders com balanço ao vento.
- **Formações Rochosas (`Rocks/`)**: Rocha e pedregulhos 4K/2K (*Rocks and Boulders 2*) com LOD groups (`Rock1A` a `Rock1E`, `Rock2`, `Rock3`, `Rock4A`, `Rock6A`).
- **Vegetação Rasteira e Arbustos (`Bushes/`)**: Arbustos de alta densidade (*Yughues Free Bushes 2018*) com meshes e texturas (`P_Bush01` a `P_Bush05`).
- **Camadas e Texturas de Terreno (`TerrainLayers/`, `Textures/`, `Materials/`)**.
- **Cena Demonstrativa (`DemoForestScene/`)**: Layout de level design de floresta tutorial.

---

### 6. 🏃 Universal Animation Library (`Assets/_Project/Animations/UniversalAnimationLibrary/`)
- **Animações Humanoides Padrão (CC0 Quaternius)**:
  - `UAL1_Standard.fbx`: Conjunto completo de animações (Idle, Walk, Run, Sprint, Jump, Swim, Drive, Crouch, Turn, etc.) sem Root Motion.
  - `UAL1_Standard_RM.fbx`: Conjunto com Root Motion pré-calculado.
  - Controlador gerado via menu `Tools/Animations/Setup Humanoid Animations` (`PlayerHumanoidController.controller`).

---

## 🛠️ Integração no Código do Jogo
1. **`SceneObjectSpawner.cs`**:
   - Povoa a natureza alternando árvores folhosas e pinheiros fotorrealistas *Book of the Dead* (`PF Conifer Tall/Medium/Small BOTD`).
   - Espalha rochas e pedregulhos com sombreamento PBR realista (`Rock1A`, `Rock2`, `Rock3`, etc.).
   - Espalha arbustos e vegetação rasteira fotorrealista (`P_Bush01` a `P_Bush05`) em orlas de mata e margens de estrada.
   - Posiciona pastos de gado (vacas, cavalos, ovelhas) próximos à sede rural.
   - Posiciona fauna selvagem (raposas, lobos) em clareiras florestais.
2. **`DowntownBuildingBuilder.cs` & `RegionalSandboxManager.Town.cs`**:
   - `CreateBuilding` monta prédios municipais e comerciais da cidade inicial usando peças modulares do Downtown City MegaKit (`Metal_FirstFloor_Wall`, `Metal_FirstFloor_Window`, `Trim_Corner`, `Roof_Slate_Center`, `Door_1`).
   - Instala unidades de ar condicionado (`Prop_ACUnit`) nos telhados e postes balizadores (`Prop_Bollard`) e floreiras (`Prop_Planter_Single`) ao longo das calçadas.
3. **`PlayerCharacterController.cs` & `HumanoidAnimationSetup.cs`**:
   - Suporte nativo a `playerAnimator`, atualizando dinamicamente estados `Speed`, `IsMoving`, `IsSprinting`, `IsGrounded`, `IsSwimming` e `IsDriving`.
   - Gera o controlador Mecanim para o jogador com animações da Universal Animation Library.
4. **`VehicleBuilder.cs`**:
   - Acopla as malhas 3D detalhadas de tratores (`tractor.fbx`) e caminhões (`truck.fbx`) preservando o sistema de física `Rigidbody` e os colliders de colisão já calibrados.
