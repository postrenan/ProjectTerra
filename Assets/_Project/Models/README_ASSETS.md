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
- **Texturas**: Atlas de mapeamento de cores `colormap_commercial.png` e `colormap_suburban.png`.

---

## 🛠️ Integração no Código do Jogo
1. **`SceneObjectSpawner.cs`**:
   - Povoa a natureza (densidade de árvores proporcional ao `forestPercent` do save da região).
   - Espalha rochas pelas elevações e encostas.
   - Posiciona pastos de gado (vacas, cavalos, ovelhas) próximos à sede rural.
   - Posiciona fauna selvagem (raposas, lobos) em clareiras florestais.
2. **`RegionalSandboxManager.cs`**:
   - `CreateBuilding`: Instancia modelos 3D dos prédios comerciais e residenciais para a cidade inicial e silos.
   - Integração com `SceneObjectSpawner.PopulateNatureAndFauna`.
3. **`VehicleBuilder.cs`**:
   - Acopla as malhas 3D detalhadas de tratores (`tractor.fbx`) e caminhões (`truck.fbx`) preservando o sistema de física `Rigidbody` e os colliders de colisão já calibrados.
