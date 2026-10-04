using System;
using System.IO;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Planet.TerrainStreaming
{
    public static partial class TerrainPBRFactory
    {
        private static Texture2D cachedSatelliteDaymap;
        private static Color[] cachedSatPixels;
        private static int satWidth = 0;
        private static int satHeight = 0;

        private static void EnsureSatelliteDaymapLoaded()
        {
            if (cachedSatPixels != null && satWidth > 0 && satHeight > 0) return;

            string earthDir = Path.Combine(Application.dataPath, "_Project", "Textures", "Earth");
            string daymapPath = Path.Combine(earthDir, "2k_earth_daymap.jpg");
            if (!File.Exists(daymapPath))
            {
                daymapPath = Path.Combine(earthDir, "8k_earth_daymap.jpg");
            }

            if (File.Exists(daymapPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(daymapPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (tex.LoadImage(bytes))
                    {
                        cachedSatelliteDaymap = tex;
                        satWidth = tex.width;
                        satHeight = tex.height;
                        cachedSatPixels = tex.GetPixels();
                        Debug.Log($"[TerrainPBRFactory] Imagem de Satélite da Terra carregada com sucesso ({satWidth}x{satHeight}) para classificação fotorrealista de biomas.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TerrainPBRFactory] Falha ao carregar mapa de satélite {daymapPath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Computa o splatmap em 8 camadas combinando:
        /// 1. Amostragem de satélite real nas coordenadas geográficas da província.
        /// 2. Mosaicos agrícolas de lavoura e pastagem nas áreas cultivadas.
        /// 3. Mescla geológica de declividade (rochas em encostas, cascalho na base).
        /// 4. Zonas ribeirinhas e praias (areia e cascalho no contato com a água).
        /// 5. Micro-mescla orgânica de terra/solo exposto entre a vegetação, eliminando qualquer padrão de repetição (tiling).
        /// </summary>
        public static float[,,] CalculateSplatmaps(TerrainData terrainData, RegionalHydroData hydro, RegionData regionData, RegionSaveData save)
        {
            EnsureSatelliteDaymapLoaded();

            int alphaRes = terrainData.alphamapResolution;
            float[,,] splatmap = new float[alphaRes, alphaRes, LayerCount];

            float arableFactor = save != null ? (save.arablePercent / 100.0f) : 0.5f;
            float forestFactor = save != null ? (save.forestPercent / 100.0f) : 0.4f;
            float mineralFactor = save != null ? (save.mineralsPercent / 100.0f) : 0.3f;
            float seaLevel = hydro != null ? hydro.seaLevelNormalized : 0f;

            // Bounding box geográfica da região
            float minLon = regionData != null ? regionData.minLon : -48.0f;
            float maxLon = regionData != null ? regionData.maxLon : -46.0f;
            float minLat = regionData != null ? regionData.minLat : -24.5f;
            float maxLat = regionData != null ? regionData.maxLat : -22.5f;

            int regionId = save != null ? save.regionId : 1980;
            float seedX = (regionId % 100) * 17.31f;
            float seedY = (regionId / 100) * 23.47f;

            bool hasSatMap = cachedSatPixels != null && satWidth > 0 && satHeight > 0;

            for (int y = 0; y < alphaRes; y++)
            {
                float ny = y / (float)(alphaRes - 1);
                float curLat = Mathf.Lerp(minLat, maxLat, ny);

                for (int x = 0; x < alphaRes; x++)
                {
                    float nx = x / (float)(alphaRes - 1);
                    float curLon = Mathf.Lerp(minLon, maxLon, nx);

                    // 1. Amostragem da Imagem de Satélite na coordenada geográfica da província
                    Color satColor = new Color(0.32f, 0.42f, 0.22f); // Fallback verde-oliva
                    if (hasSatMap)
                    {
                        float u = Mathf.Clamp01((curLon + 180f) / 360f);
                        float v = Mathf.Clamp01((curLat + 90f) / 180f);
                        int px = Mathf.Clamp((int)(u * (satWidth - 1)), 0, satWidth - 1);
                        int py = Mathf.Clamp((int)(v * (satHeight - 1)), 0, satHeight - 1);
                        satColor = cachedSatPixels[py * satWidth + px];
                    }

                    // Análise espectral da imagem de satélite
                    float r = satColor.r;
                    float g = satColor.g;
                    float b = satColor.b;
                    float lum = 0.299f * r + 0.587f * g + 0.114f * b;
                    float maxC = Mathf.Max(r, Mathf.Max(g, b));
                    float minC = Mathf.Min(r, Mathf.Min(g, b));
                    float sat = maxC > 0.001f ? (maxC - minC) / maxC : 0f;

                    // 2. Classificação de Bioma por Satélite com Curvas de Contraste Nítidas
                    // A) Selva / Floresta Tropical Densa (Verde escuro saturado)
                    float jungleRaw = Mathf.Clamp01((g - r * 0.85f) * 4.0f) * Mathf.Clamp01(1.0f - (lum - 0.40f) * 3.5f);
                    float jungleAffinity = Mathf.Pow(jungleRaw, 1.4f) * (0.6f + forestFactor * 0.8f);

                    // B) Grama / Pradaria / Pasto Verdejante
                    float grassRaw = Mathf.Clamp01((g - b * 1.2f) * 3.2f) * Mathf.Clamp01(1.0f - Mathf.Abs(lum - 0.44f) * 2.8f);
                    float grassAffinity = Mathf.Pow(grassRaw, 1.3f);

                    // C) Savana Seca / Pasto Árido / Cerrado (Dourado/Oliva claro)
                    float savannaRaw = Mathf.Clamp01((r + g - b * 1.8f) * 2.0f) * Mathf.Clamp01((lum - 0.35f) * 3.0f);
                    float savannaAffinity = Mathf.Pow(savannaRaw, 1.5f);

                    // D) Areia / Deserto / Dunas (Alta luminosidade, tons ocre/bege quente)
                    float sandRaw = Mathf.Clamp01((lum - 0.50f) * 3.5f) * Mathf.Clamp01(1.0f - (g - r) * 3.0f);
                    float sandAffinity = Mathf.Pow(sandRaw, 1.6f);

                    // E) Neve Alpina / Tundra (Branco/gelo de alta luminosidade e baixa saturação)
                    float snowRaw = Mathf.Clamp01((lum - 0.70f) * 4.5f) * Mathf.Clamp01((0.22f - sat) * 5.0f);
                    float snowAffinity = Mathf.Pow(snowRaw, 1.5f);

                    // Gradiente polar em 3 zonas:
                    // • Boreal/Taiga (58°–72°): neve parcial — floresta boreal ainda predomina no satélite,
                    //   então apenas realçamos levemente a afinidade de neve para não engolir a vegetação.
                    // • Subártico (72°–80°): tundra majoritária, neve predominante.
                    // • Ártico / Polar (>80°): neve quase total (calota de gelo).
                    float absLat = Mathf.Abs(curLat);
                    if (absLat > 80f)
                        snowAffinity = Mathf.Max(snowAffinity, 0.85f);
                    else if (absLat > 72f)
                        snowAffinity = Mathf.Max(snowAffinity, Mathf.Lerp(0.45f, 0.85f, (absLat - 72f) / 8f));
                    else if (absLat > 58f)
                        snowAffinity = Mathf.Max(snowAffinity, Mathf.Lerp(0.10f, 0.45f, (absLat - 58f) / 14f));

                    // Amplificador equatorial de selva (−10° a +10°): garante cobertura densa independente
                    // da qualidade da imagem de satélite para florestas tropicais úmidas.
                    if (absLat <= 10f)
                    {
                        float equatorialBoost = Mathf.Lerp(0.55f, 0.75f, forestFactor) * Mathf.Clamp01(1.0f - absLat / 10f);
                        jungleAffinity = Mathf.Max(jungleAffinity, equatorialBoost);
                    }

                    // Amplificador de deserto para os cinturões áridos subtropicais (15°–35° N/S):
                    // Saara, Arábia, Austrália Central, Namíbia, Atacama, etc.
                    if (absLat >= 15f && absLat <= 35f)
                    {
                        // Só reforça se o satélite já indica alta luminosidade quente (não sobre o Mediterrâneo verde)
                        float desertLatBoost = Mathf.Lerp(0.0f, 0.30f, Mathf.Clamp01((sandRaw - 0.15f) / 0.35f));
                        sandAffinity = Mathf.Max(sandAffinity, sandAffinity + desertLatBoost);
                    }

                    // 3. Mosaico de Lavouras e Solo Agrícola (Talhões Rurais Realistas)
                    // Cria campos retangulares e quadrantes cultivados típicos de fazendas e propriedades rurais
                    float fieldAngle = 0.35f; // Rotação dos talhões agrícolas
                    float rotX = nx * Mathf.Cos(fieldAngle) - ny * Mathf.Sin(fieldAngle);
                    float rotY = nx * Mathf.Sin(fieldAngle) + ny * Mathf.Cos(fieldAngle);
                    float cellX = Mathf.Abs(Mathf.Sin(rotX * 110f + seedX));
                    float cellY = Mathf.Abs(Mathf.Sin(rotY * 110f + seedY));
                    float fieldPattern = Mathf.SmoothStep(0.25f, 0.75f, cellX * cellY);

                    // 4. Relevo Físico e Topografia
                    float steepness = terrainData.GetSteepness(nx, ny);
                    float heightNorm = terrainData.GetInterpolatedHeight(nx, ny) / Mathf.Max(1f, terrainData.size.y);

                    // Rocha de Encosta / Penhasco: domina conforme o declive aumenta
                    float rockWeight = 0f;
                    if (steepness > 18f)
                    {
                        rockWeight = Mathf.Clamp01((steepness - 18f) / 16f); // 1.0 aos 34 graus
                    }
                    if (heightNorm > 0.82f)
                    {
                        rockWeight = Mathf.Max(rockWeight, (heightNorm - 0.82f) * 5f);
                        if (absLat > 35f || mineralFactor > 0.5f)
                        {
                            snowAffinity = Mathf.Max(snowAffinity, (heightNorm - 0.80f) * 4f);
                        }
                    }

                    // Margens de água, leito de rio e praias: Areia e Cascalho
                    float sandWeight = sandAffinity;
                    float gravelWeight = 0.05f;
                    if (heightNorm <= seaLevel + 0.045f)
                    {
                        float waterDist = Mathf.Clamp01(1.0f - (heightNorm - seaLevel) / 0.045f);
                        sandWeight = Mathf.Max(sandWeight, waterDist * 0.90f);
                        gravelWeight = Mathf.Max(gravelWeight, waterDist * 0.50f);
                    }

                    // 5. Quebra Procedural de Tiling e Síntese de Mescla de Solo
                    // Macro-ruído (manchas de bioma de 2 a 4 km)
                    float macro1 = Mathf.PerlinNoise(nx * 7.5f + seedX, ny * 7.5f + seedY);
                    float macro2 = Mathf.PerlinNoise(nx * 16f + seedX + 50f, ny * 16f + seedY + 50f);
                    // Meso-ruído (talhões e clareiras de 400m a 800m)
                    float meso = Mathf.PerlinNoise(nx * 32f + seedX + 80f, ny * 32f + seedY + 80f);
                    // Micro-ruído (detalhes de solo e terra batida de 40m a 100m)
                    float micro = Mathf.PerlinNoise(nx * 65f + seedX + 120f, ny * 65f + seedY + 120f);

                    // Pesos das Camadas de Solo:
                    // A) Solo Arado / Terra Cultivada:
                    // Fortemente estimulado nas áreas planas com alta taxa arável (lavouras) e micro-veios em campos
                    float soilWeight = 0f;
                    if (arableFactor > 0.10f && steepness < 18f)
                    {
                        float arableBoost = arableFactor * 0.85f * fieldPattern;
                        float soilBase = Mathf.Clamp01((r - b * 1.1f) * 2.5f) * 0.4f;
                        soilWeight = (arableBoost + soilBase) * (0.6f + 0.8f * micro);
                    }
                    else
                    {
                        soilWeight = 0.12f * micro; // Terra residual sob a vegetação
                    }

                    // B) Grama Lush / Pastagem:
                    float grassWeight = (grassAffinity * 0.85f + 0.20f) * (0.6f + 0.8f * macro1);
                    // Se estiver em lavoura de terra arada, a grama cede espaço ao solo arado.
                    // Clamp01 é obrigatório: soilWeight chega a (0,85 + 0,4) * 1,4 = 1,75, e
                    // o fator cruza zero em 1,119 — sem o clamp o peso de grama ficava NEGATIVO
                    // e SetAlphamaps grava negativo no alphamap (ele não valida).
                    if (soilWeight > 0.35f)
                    {
                        grassWeight *= Mathf.Clamp01(1.0f - (soilWeight - 0.35f) * 1.3f);
                    }

                    // C) Selva / Floresta Densa:
                    float jungleWeight = jungleAffinity * (0.5f + 0.9f * macro2);

                    // D) Savana / Pasto Seco:
                    float savannaWeight = savannaAffinity * (0.6f + 0.8f * (1.0f - macro1));

                    // E) Cascalho:
                    // Seixos em encostas médias (12° a 24°) e caminhos rurais
                    if (steepness >= 12f && steepness < 26f)
                    {
                        gravelWeight += Mathf.Clamp01((steepness - 12f) / 14f) * 0.45f * meso;
                    }
                    gravelWeight += 0.08f * micro;

                    // F) Neve:
                    float snowWeight = snowAffinity * (0.8f + 0.4f * macro1);

                    // 6. Supressão de Vegetação em Paredões Rochosos
                    float vegFactor = Mathf.Clamp01(1.0f - rockWeight * 1.2f);
                    grassWeight *= vegFactor;
                    jungleWeight *= vegFactor;
                    savannaWeight *= vegFactor;
                    soilWeight *= vegFactor;

                    // 7. Normalização Perfeita dos 8 Pesos
                    float totalWeight = grassWeight + jungleWeight + savannaWeight + soilWeight +
                                        sandWeight + gravelWeight + rockWeight + snowWeight;

                    if (totalWeight > 0.0001f)
                    {
                        // Rede de segurança: os pesos nunca podem sair negativos nem estourar 1,
                        // senão o shader extrapola a cor da camada e o TerrainGroundPainter
                        // (que soma os "outros" pesos) passa a zerar todas as camadas do texel.
                        splatmap[y, x, LayerGrass] = Mathf.Max(0f, grassWeight / totalWeight);
                        splatmap[y, x, LayerJungle] = Mathf.Max(0f, jungleWeight / totalWeight);
                        splatmap[y, x, LayerSavanna] = Mathf.Max(0f, savannaWeight / totalWeight);
                        splatmap[y, x, LayerSoil] = Mathf.Max(0f, soilWeight / totalWeight);
                        splatmap[y, x, LayerSand] = Mathf.Max(0f, sandWeight / totalWeight);
                        splatmap[y, x, LayerGravel] = Mathf.Max(0f, gravelWeight / totalWeight);
                        splatmap[y, x, LayerRock] = Mathf.Max(0f, rockWeight / totalWeight);
                        splatmap[y, x, LayerSnow] = Mathf.Max(0f, snowWeight / totalWeight);
                    }
                    else
                    {
                        splatmap[y, x, LayerGrass] = 1.0f;
                    }
                }
            }

            Debug.Log($"[TerrainPBRFactory] Mescla de Solo 8-Camadas concluída com sucesso para Região #{regionId} ({minLon:F2}° a {maxLon:F2}° Lon, {minLat:F2}° a {maxLat:F2}° Lat). Lavouras, encostas e biomas mesclados organicamente.");
            return splatmap;
        }
    }
}
