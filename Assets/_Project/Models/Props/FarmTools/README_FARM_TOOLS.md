# Coleção de Ferramentas Agrícolas Rústicas (Rusted Farm Tools)

Biblioteca completa de **5 Ferramentas Agrícolas Tradicionais** modeladas em 3D, com texturas PBR completas (Albedo, Normal Map, Ambient Occlusion e Height Map) e acabamento rústico envelhecido (ferro forjado com oxidação natural e cabos de madeira nobre de freixo/nogueira).

---

## 🎨 Visualização e Showcase

- **Painel de Exibição das Ferramentas ([`farm_tools_showcase.png`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/farm_tools_showcase.png))**:
  - Render de estúdio demonstrando as 5 ferramentas alinhadas lado a lado em escala humana real (1.35m a 1.55m de altura).
- **Arquivo Master Blender ([`FarmEquipment_Collection.blend`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/FarmEquipment_Collection.blend))**:
  - Cena mestre com todas as malhas, pivots calibrados na base e materiais PBR configurados no Blender 5.2.
- **Arquivo Master FBX ([`FarmEquipment_Collection.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/FarmEquipment_Collection.fbx))**:
  - Coleção completa para visualização rápida no Unity.

---

## 🛠️ Catálogo das 5 Ferramentas Agrícolas

| # | Identificador FBX | Nome em Português | Altura Real | Descrição & Uso no Campo |
|---|---|---|---|---|
| **01** | [`Tool_Farm_ShovelSquare.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/Tool_Farm_ShovelSquare.fbx) | **Pá Reta de Corte** | 1.55m | Lâmina retangular em chapa de ferro forjado para corte de leivas, bordas de canteiros e valas de irrigação. |
| **02** | [`Tool_Farm_Spade.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/Tool_Farm_Spade.fbx) | **Pá de Bico / Concha** | 1.55m | Pá de ponta arredondada/ogival ideal para movimentação de terra solta, adubo orgânico, cascalho e escavação de covas. |
| **03** | [`Tool_Farm_Pitchfork.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/Tool_Farm_Pitchfork.fbx) | **Forcado / Garfo de Feno** | 1.55m | Garfo rústico de 4 dentes curvos temperados para manipulação de fardos de feno, palha e silagem de gado. |
| **04** | [`Tool_Farm_Hoe.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/Tool_Farm_Hoe.fbx) | **Enxada Agrícola Tradicional** | 1.35m | Lâmina transversal afiada com fixação por colar reforçado para capina de ervas daninhas e aiveca manual. |
| **05** | [`Tool_Farm_Rake.fbx`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Models/Props/FarmTools/Tool_Farm_Rake.fbx) | **Ancinho / Rastelo de Dentes** | 1.45m | Pente transversal de múltiplos dentes de aço para nivelamento de canteiros, aeração de solo e limpeza de folhagem. |

---

## 🌿 Texturas e Shading PBR

Localizadas em [`Assets/_Project/Textures/Props/FarmTools/`](file:///E:/Backup-C/Projetos/testejogo/Assets/_Project/Textures/Props/FarmTools/):
- **`FarmTools_ALB.png`**: Mapa difuso/albedo em alta resolução com pátina de oxidação e veios de madeira.
- **`FarmTools_NML.png`**: Mapa de normais tangente para relevo de corrosão do metal e ranhuras do cabo.
- **`FarmTools_AO.png`**: Oclusão de ambiente para sombreamento nas junções do metal com o cabo de madeira.
- **`FarmTools_HGT.png`**: Mapa de relevo e altura para detalhamento geométrico.

---

## 🎮 Integração no Jogo

1. **`FarmToolProp.cs`**:
   - Componente acoplado automaticamente com enum de tipo, cálculo dinâmico de `BoxCollider` e aplicação de shader URP Lit.
2. **`RegionalSandboxManager.Careers.cs` (`BuildFarmerBase`)**:
   - Spawna automaticamente o kit com as 5 ferramentas ao lado da sede/trator na base do fazendeiro.
3. **`ConstructionCatalog.cs` & `ConstructionPlacementManager.cs`**:
   - Disponíveis para compra e posicionamento pelo Celular de Construção na aba **Decoração -> Ferramentas Agrícolas**.
