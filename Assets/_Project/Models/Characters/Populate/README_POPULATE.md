# Biblioteca de NPCs Civis e Pedestres (Populate Models)

Coleção de **61 Modelos Humanoides Civis** e **105 Variações de Texturas** fotorealistas para povoamento urbano, rural e rodoviário, cobrindo homens e mulheres com roupas civis modernas (calças jeans, bermudas, camisetas, polos, jaquetas, camisas de botão, vestidos e calçados casuais), em poses e rêmiges calibradas na escala métrica real (1.70m a 1.88m).

---

## 🎨 Visualização e Showroom

- **Showroom Panorâmico de Civis ([`populate_characters_showcase.png`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Characters/Populate/populate_characters_showcase.png))**:
  - Exibição de homens e mulheres em escala humana 1:1 com vestimentas do cotidiano.
- **Arquivo Master Blender ([`Populate_Characters_Master.blend`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Characters/Populate/Populate_Characters_Master.blend))**:
  - Cena completa com todos os 105 modelos, malhas higienizadas e texturas mapeadas no Blender 5.2.
- **Coleção de Bancos Públicos ([`Populate_Seats_Collection.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Characters/Populate/Populate_Seats_Collection.fbx))**:
  - Coleção de assentos e bancos de praça para descanso dos pedestres.

---

## 👥 Resumo Demográfico dos Modelos (105 Malhas FBX)

### 1. Personagens Femininos (52 Variações)
- **`Populate_Female000Skin_001` a `005`**: 5 variações de roupas casuais (blusão vinho, azul, jeans escuro, sapatos de passeio).
- **`Populate_Female001Skin_001` a `004`**: 4 variações de camisa social verde, azul-claro, calça de linho claro.
- **`Populate_Female002Skin_001` a `004`**: 4 variações de jaquetas esportivas e calças urbanas.
- **`Populate_Female003Skin_001` a `004`**: 4 variações de suéteres de outono e sapatilhas.
- **`Populate_Female004Skin_001` a `004`**: 4 variações de camisetas de verão e bermudas/jeans.
- **`Populate_Female005Skin_001` a `004`**: 4 variações de trajes de trabalho civil.
- **`Populate_Female006Skin_001` a `004`**: 4 variações de blazers e sobretudos leves.
- **`Populate_Female007Skin_001` a `Female030Skin_001`**: 23 modelos individuais adicionais com feições, penteados e tons de pele variados.

### 2. Personagens Masculinos (53 Variações)
- **`Populate_Male000Skin_001` a `005`**: 5 variações com camisa polo rosa, branca, azul, calça de sarja preta e sapatos sociais.
- **`Populate_Male001Skin_001` a `004`**: 4 variações com suéter roxo, marrom, verde e calça chino.
- **`Populate_Male002Skin_001` a `004`**: 4 variações com jaqueta de couro e jeans escuro.
- **`Populate_Male003Skin_001` a `003`**: 3 variações com camisa xadrez e traje de trabalho agrícola.
- **`Populate_Male004Skin_001` a `003`**: 3 variações esportivas com agasalho e tênis.
- **`Populate_Male005Skin_001` a `003`**: 3 variações de caminhoneiro/operário com jeans e botas rústicas.
- **`Populate_Male006Skin_001` a `003`**: 3 variações com blazer executivo e gravata.
- **`Populate_Male007Skin_001` a `002`**: 2 variações com colete utilitário.
- **`Populate_Male008Skin_001` a `002`**: 2 variações casuais urbanas.
- **`Populate_Male009Skin_001` a `002`**: 2 variações com t-shirt básica e bermuda cargo.
- **`Populate_Male010Skin_001` a `002`**: 2 variações de agasalho com capuz.
- **`Populate_Male011Skin_001` a `Male030Skin_001`**: 20 modelos individuais de pedestres adicionais.

### 3. Mobiliário Urbano e Assentos (12 Bancos)
- **`Populate_Seat001.fbx` a `Populate_Seat012.fbx`**: Bancos e assentos em madeira e metal para pontos de ônibus, praças e calçadas.

---

## 🛠️ Integração no Código

1. **`NpcPedestrianSpawner.cs`**:
   - Catálogo estendido `POPULATE_NPC_MODELS` integrado no spawner universal.
   - Povoa praças, calçadas, rodovias e vilas com pedestres e civis.
   - Aplicação dinâmica de materiais URP e texturas de alta definição via `ConfigureNpcVisuals`.
   - Sistema de IA de perambulação e repouso `NpcWandererAI`.
2. **`RegionalSandboxManager.cs`**:
   - Invoca `SceneObjectSpawner.PopulatePedestrians(starterBasePosition, count: 18, radius: 45f)` na inicialização da cena.
3. **`NpcImportSetup.cs`**:
   - Menu no Unity Editor: `Tools -> Characters -> Setup Populate Humanoids`.
