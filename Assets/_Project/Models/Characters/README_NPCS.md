# Coleção de NPCs Civis Humanoides (Estilo Styloo Asset Pack)

Coleção completa de **12 Personagens NPCs Humanoides Civis** modelados e gerados no **Blender 5.2** diretamente a partir das malhas estilizadas e texturizadas à mão de `"The company Asset Pack Styloo"`.

O conjunto atende com rigor aos requisitos de **diversidade étnica e cultural humana**, tons de pele reais (Escala Fitzpatrick I a VI), remoção completa de armamentos de combate para atuação como pedestres civis, humanização anatômica das orelhas e feições faciais exclusivas.

---

## 🎨 Visualização e Renders de Showroom

- **Parada Panorâmica do Showroom ([`npc_characters_showcase.png`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Characters/npc_characters_showcase.png))**:
  - Exibição de estúdio com todos os 12 NPCs dispostos em duas fileiras escalonadas, com iluminação de 3 pontos (Key Sun, Soft Fill e Rim Backlight).
- **Close-up de Retratos Culturais ([`npc_portraits_showcase.png`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Characters/npc_portraits_showcase.png))**:
  - Detalhe em alta definição dos rostos, olhos, formatos de nariz e queixo, barbas e tecidos.
- **Arquivo Master Blender ([`NPC_Characters_Collection.blend`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Characters/NPC_Characters_Collection.blend))**:
  - Cena completa com todos os modelos, armatures, iluminação de estúdio e materiais opacos prontos para edição no Blender 5.2.

---

## 👥 Catálogo dos 12 NPCs Criados

| # | Identificador FBX | Base Styloo | Etnia / Arquétipo | Tom de Pele (Fitzpatrick) | Características Faciais & Cabelo | Vestimenta Civil |
|---|---|---|---|---|---|---|
| **01** | `NPC_Asian_Female_Silk` | `elfexport.fbx` | Leste Asiático | Tipo III: Dourado Quente | Olhos amendoados inclinados, maçãs do rosto altas, cabelo sedoso escuro, orelhas humanizadas | Túnica de seda carmesim e brocado dourado |
| **02** | `NPC_Asian_Male_Scholar` | `knightclean4.fbx` | Leste Asiático | Tipo III: Dourado Claro | Olhos amendoados, traços suaves, gorro e cabelo preto | Túnica de linho verde-jade e botas douradas |
| **03** | `NPC_Egyptian_Male_Elder` | `mageclean4.fbx` | Egípcio / Norte da África | Tipo IV/V: Bronze Dourado | Nariz aquilino pronunciado, barba branca esculpida de sábio, olhos expressivos | Galabeya de linho off-white/ametista com detalhes a ouro e livro de estudos |
| **04** | `NPC_Egyptian_Female_Dignitary` | `elfexport.fbx` | Egípcia / Árabe | Tipo IV/V: Bronze Dourado | Maçãs esculpidas, olhos com kohl egípcio marcante, orelhas humanizadas | Vestido real em azul-petróleo (teal) com bordados solares |
| **05** | `NPC_European_Male_Knight` | `knightclean4.fbx` | Europeu | Tipo II: Pêssego Claro | Ponte nasal reta, queixo angular limpo, cota de malha leve sob túnica | Túnica de lã azul-safira real e botas de montaria |
| **06** | `NPC_European_Female_Noble` | `elfexport.fbx` | Europeia Nórdica | Tipo I: Pálido Claro | Queixo delicado, cabelos loiros platinados, orelhas humanizadas | Vestido longo de veludo verde-esmeralda com corset estilizado |
| **07** | `NPC_Nordic_Male_Blacksmith` | `nain4export.fbx` | Nórdico / Ferreiro | Tipo I: Pálido Rosado | Barba e bigode ruivos volumosos, olhos nórdicos, fisionomia robusta | Avental e couraça de couro e cobre para forja |
| **08** | `NPC_American_Male_Explorer` | `knightclean4.fbx` | Americano | Tipo II/III: Bronzeado | Mandíbula quadrada atlética, expressão amigável, cabelos castanhos | Túnica índigo denim com cinturão de viagem em couro |
| **09** | `NPC_American_Female_Adventurer` | `elfexport.fbx` | Latina / Americana | Tipo IV: Caramelo Quente | Olhos amendoados, feições expressivas, cabelos castanho-escuros | Traje de exploradora em tons cáqui e couro terra |
| **10** | `NPC_African_Male_Champion` | `nain4export.fbx` | Afrodescendente | Tipo VI: Ébano Profundo | Mandíbula atlética forte, cavanhaque escuro e bigode bem desenhado | Armadura cerimonial em bronze polido e ouro |
| **11** | `NPC_African_Female_Merchant` | `elfexport.fbx` | Afrodescendente | Tipo VI: Ébano Luminoso | Lábios desenhados, traços nobres africanos, orelhas humanizadas | Vestido tradicional em tons ouro-açafrão e oliva |
| **12** | `NPC_MiddleEastern_Magistrate` | `mageclean4.fbx` | Médio Oriente | Tipo IV: Oliva Bronze | Barba aparada nobre, feições expressivas do mediterrâneo oriental | Roupagem de magistrado em púrpura real com gema de ametista no cinturão |

---

## 🛠️ Modificações Aplicadas às Bases Originais

1. **Higienização de Armas de Combate (`sanitize_civilian_mesh`)**:
   - Foram removidos via grupos de vértices de ossos (`DEF-bow*`, `DEF-arrow*`, `DEF-staff*`, `DEF-weapon*`, `DEF-shield*`) arcos, flechas flutuantes, cajados de batalha e martelos gigantes, transformando os personagens em **civis e pedestres** autênticos.
2. **Humanização Anatômica das Orelhas**:
   - Os modelos com base élfica (`elfexport.fbx`) tiveram as orelhas pontudas retraídas e arredondadas para proporções humanas naturais por meio de deformação vetorial localizada nos vértices ponderados pelo osso da orelha (`DEF-ear`).
3. **Escultura Óssea Facial por Etnia (`apply_ethnic_facial_morph`)**:
   - Variação real dos ossos DEF faciais (arco zigomático/bochechas, inclinação amendoada dos olhos, largura e altura nasal, queixo e mandíbula).
4. **Materiais Exclusivos e Texturas Embutidas**:
   - Cada modelo conta com materiais independentes (`M_<ID>_1` e `M_<ID>_2`) e texturas difusas exclusivas de 2048x2048 salvas em `Assets/_Project/Textures/Characters/` e embutidas nos arquivos FBX (`embed_textures=True`).

---

## 🚀 Como Utilizar no Unity

### Spawner Dinâmico de Pedestres
O script `NpcPedestrianSpawner.cs` permite instanciar e distribuir multidões de pedestres por região:
- **`GlobalMixed`**: Distribui os 12 NPCs aleatoriamente pela cena.
- **`EastAsia`**: Prioriza NPCs asiáticos.
- **`EgyptAndMiddleEast`**: Prioriza NPCs egípcios e do Médio Oriente.
- **`Americas`**: Prioriza NPCs americanos, latino-americanos e africanos.
- **`EuropeAndNordic`**: Prioriza NPCs europeus e nórdicos.

```csharp
// Exemplo: popular uma praça com 25 pedestres
SceneObjectSpawner.PopulatePedestrians(spawnCenter, count: 25, radius: 60f);
```
