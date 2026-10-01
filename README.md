# Project Terra 🌍

Simulação de globo terrestre 3D interativo em escala 1:1 desenvolvido na Unity 6 (6000.6.3f1).

## 🚀 Funcionalidades

- **Globo Terrestre CubeSphere com LOD Dinâmico (Quadtree):**
  - Subdivisão esférica contínua do espaço profundo até a altitude de 50 metros.
  - Correção de costura no antimeridiano (180° de longitude) sem estiramento de texturas.
  - Inclinação axial real da Terra ($23,44^\circ$) e rotação sideral configurável.

- **Fronteiras e Divisões Políticas Mundiais:**
  - Camada vetorial com países (Admin 0) e estados/províncias (Admin 1) globais.
  - Transição contínua com fade-in proporcional à altitude da câmera.

- **Interação Geográfica e Seleção Regional:**
  - Raycast analítico de alta precisão (dupla precisão) contra a esfera planetária.
  - Conversão exata de clique para latitude e longitude reais.
  - Banco de dados geográfico integrado (`StreamingAssets/regions_database.json`) cobrindo 530+ países, estados e províncias com caixas delimitadoras e centroides reais.
  - Balão de interface (*glassmorphism*) exibindo indicadores de recursos:
    - 🌲 Cobertura Florestal (%)
    - ⛏️ Potencial Mineral (%)
    - 🌾 Terras Aráveis (%)
    - 💧 Recursos Hídricos (%)
  - Pausa automática do giro do planeta ao selecionar uma região e botão para retomar a rotação.

- **Câmera Orbital e Shader Multi-Escala:**
  - Navegação orbital suave com zoom exponencial adaptativo à altitude.
  - Síntese de micro-relevo procedural no terreno, florestas, zonas áridas e marolas oceânicas com specular reflexivo (*sun glint*).

## 🎮 Controles

- **Botão Esquerdo do Mouse:**
  - Clique: Seleciona estado/país/região e abre o painel de estatísticas.
  - Arrastar (> 8px): Rotaciona a câmera orbital.
- **Botão Direito / Botão do Meio do Mouse:**
  - Rotaciona a câmera orbital livremente.
- **Roda do Mouse (Scroll):**
  - Zoom contínuo do espaço até 50 metros da superfície.
