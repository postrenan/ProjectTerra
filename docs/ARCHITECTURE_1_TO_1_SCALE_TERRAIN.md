# ARQUITETURA DE TERRENO GLOBAL EM ESCALA REAL (1:1)
## Projeto Terra: Engenharia de Topografia, Streaming e Simulação Planetária

---

### 1. O Desafio da Escala 1:1 e Dimensões Reais da Terra

| Parâmetro Geodésico | Valor Físico Real |
| :--- | :--- |
| **Raio Médio Terrestre ($R$)** | $6.371.000\text{ m}$ ($6.371\text{ km}$) |
| **Circunferência Equatorial** | $40.075\text{ km}$ |
| **Área Superficial Total** | $510.065.623\text{ km}^2$ |
| **Área Continental (Terras Emersas)** | $\approx 148.940.000\text{ km}^2$ ($29,2\%$) |
| **Variação Altimétrica Real** | $-11.034\text{ m}$ (Fossa das Marianas) a $+8.848\text{ m}$ (Monte Everest) |

#### Por que o download estático prévio de todo o planeta é inviável?
Se fôssemos armazenar todo o relevo terrestre em 1 metro de resolução (padrão LIDAR) no computador do jogador:
$$\text{Volume de Dados} = 510 \times 10^{12} \text{ pontos} \times 2\text{ bytes} \approx 1.020\text{ Terabytes} = 1\text{ Petabyte}$$
Mesmo em 30 metros de resolução (padrão NASA SRTM / Copernicus DEM), seriam mais de **250 Gigabytes** de arquivos de elevação estáticos.

#### A Solução do Projeto Terra: Arquitetura Híbrida de 3 Camadas
Assim como o *Microsoft Flight Simulator 2024* e o *Google Earth*, o Projeto Terra adota:
1. **Streaming Sob Demanda (Tile-on-Demand):** Apenas os quadrantes e setores onde a câmera do jogador está ou onde a partida foi iniciada são baixados.
2. **Cache Binário Local Persistente ($O(1)$):** Cada tile baixado é decodificado e armazenado localmente em formato binário bruto (`.bin`). Um estado inteiro como São Paulo, Baviera ou Iowa ocupa apenas **~30 a 50 Megabytes** no disco do jogador.
3. **Amplificação Procedural em GPU (Micro-Escala 30m $\to$ 1m):** Os dados reais de 30m fornecem os contornos geográficos e montanhas; o relevo submétrico (sulcos de tratores em plantações, ondulações de drenagem, leitos fluviais) é gerado proceduralmente na GPU sem consumir tráfego de rede.
4. **Modo 100% Offline Resiliente:** Um gerador fractal analítico calibrado por latitude e bioma garante que o jogo funcione perfeitamente sem qualquer conexão de internet.

---

### 2. Fontes de Dados Públicas, Gratuitas e de Alta Precisão

#### A. AWS Open Data Terrain Tiles (Formato Terrarium)
- **Provedor:** Amazon Web Services Open Data Registry (Parceria com Mapzen / USGS / NASA).
- **Acesso:** Totalmente público, gratuito e sem necessidade de chave de API.
- **Protocolo:** Requisições HTTP padrão para tiles Web Mercator (Slippy Map):
  $$\text{URL} = \text{https://s3.amazonaws.com/elevation-tiles-prod/terrarium/\{z\}/\{x\}/\{y\}.png}$$
- **Decodificação de Altura em Metros Reais:**
  Os canais RGB de cada pixel de 256x256 contêm a cota altimétrica precisa:
  $$\text{Elevação (m)} = \left(R \times 256 + G + \frac{B}{256}\right) - 32768$$
  *Permite registrar altitudes de $-32.768\text{ m}$ até $+32.767\text{ m}$ com precisão submétrica ($1/256 \approx 0,0039\text{ metros}$).*

#### B. Copernicus DEM GLO-30 (ESA / União Europeia)
- **Resolução:** 30 metros global (1 arco de segundo).
- **Cobertura:** 100% dos continentes, incluindo ilhas remotas e regiões polares.
- **Utilização:** Malha altimétrica primária para relevo continental e relevo de bacias hidrográficas.

#### C. OpenStreetMap (OSM) via Overpass API
- **Conteúdo Vetorial:** Limites de propriedades rurais, polígonos de plantações (`landuse=farmland / orchard`), traçados viários e hidrográficos.
- **Integração:** Consultas espaciais em formato GeoJSON/OverpassQL por Bounding Box (`bbox`) apenas para o setor ativo do jogador.

---

### 3. Pipeline de Renderização e Níveis de Detalhe (LOD)

```
[VISÃO ORBITAL (1.000 km - 150 km)]
  └─ CubeSpherePlanet (8K Daymap + Normal Map + 4K Linhas Políticas + Shader de Destaque 1:1)
       │
[TRANSIÇÃO MESO (150 km - 15 km)]
  └─ TerrainDataService (Download de Tiles DEM Zoom 7-10 + Cache Binário Float32)
       │
[VISÃO REGIONAL / MUNICIPAL (15 km - 2 km)]
  └─ Quadtree CDLOD (Geração de Malhas Locais de Terreno com Curvas de Nível)
       │
[VISÃO DE SOLO 1:1 (2 km - 1 metro)]
  └─ GPU Displacement Shader (Sulcos de lavoura, vegetação densa, estradas rurais e estruturas)
```

1. **Visão Orbital (LOD 0):** A Terra gira com iluminação física, reflexo especular nos oceanos e destaque nítido do território soberano selecionado. Custo de renderização mínimo (60+ FPS em 4K).
2. **Aproximação e Tela de Carregamento:** Ao iniciar a partida, a câmera foca nas coordenadas geodésicas exatas da região selecionada. A tela temática carrega e exibe o dossiê regional enquanto o `TerrainDataService` busca os tiles topográficos necessários.
3. **Escala 1:1 no Solo:** No chão, o terreno é renderizado em metros reais. O jogador tem a sensação física autêntica de estar em uma plantação ou metrópole com horizonte curvo e atmosfera realista.

---

### 4. Sistema de Coordenadas e Precisão Numérica de 64 bits

Na Unity, o motor gráfico opera com pontos flutuantes de 32 bits (`float`), que perdem precisão e causam trepidação (*jittering*) em distâncias superiores a $10.000\text{ metros}$ do ponto de origem `(0, 0, 0)`.

Para permitir a movimentação sem limites pela superfície da Terra ($40.000\text{ km}$):
- **Origem Flutuante (`FloatingOrigin.cs`):** O centro do setor ativo do jogador é posicionado em `(0, 0, 0)` no espaço da cena da Unity.
- **Coordenadas Reais em Dupla Precisão (`Vector3d`):** A posição global do jogador na Terra é registrada em `double` (64 bits), proporcionando precisão submétrica em escala planetária.

---

### 5. Resiliência e Desempenho Local

- **Zero Latência após o Primeiro Acesso:** Se o jogador jogar em São Paulo ou na Baviera hoje, os tiles de elevação são armazenados no cache binário local. Em inicializações futuras, o carregamento do relevo ocorre em **menos de 5 milissegundos**, lido diretamente do disco SSD.
- **Baixo Consumo de Disco:** Cada tile compactado ocupa apenas $256 \text{ KB}$. Uma província típica com 200 tiles consome menos de $50 \text{ MB}$.
- **Sintetizador Offline Automático:** Se não houver conexão de internet, o gerador procedural calcula o relevo baseado no relevo daquele bioma, permitindo que a partida transcorra perfeitamente.
