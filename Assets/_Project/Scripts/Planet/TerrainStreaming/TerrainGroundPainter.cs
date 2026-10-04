using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Sandbox;

namespace ProjectTerra.Planet.TerrainStreaming
{
    /// <summary>
    /// Metadados de uma camada de pintura de solo.
    /// </summary>
    public struct GroundTextureInfo
    {
        public int layerIndex;
        public string displayName;
        public string technicalName;
        public string description;
        public Color swatchColor;
        public bool isPaved; // Se for pavimento (concreto, asfalto, etc.) ou terra batida, suprime a grama alta
    }

    /// <summary>
    /// Gerenciador e executor de pintura dinâmica de texturas no terreno (Splatmap) em tempo real.
    /// Permite pintar qualquer área com Concreto, Terra Simples, Asfalto, Brita, Grama, Paralelepípedo, etc.
    /// </summary>
    public static class TerrainGroundPainter
    {
        public static readonly GroundTextureInfo[] AvailableTextures = new GroundTextureInfo[]
        {
            new GroundTextureInfo { layerIndex = 0, displayName = "Grama Lush", technicalName = "Grama_Lush", description = "Pradaria verde viçosa natural", swatchColor = new Color(0.28f, 0.55f, 0.22f), isPaved = false },
            new GroundTextureInfo { layerIndex = 1, displayName = "Selva / Folhagem", technicalName = "Selva_Foliage", description = "Mata densa tropical úmida", swatchColor = new Color(0.18f, 0.44f, 0.16f), isPaved = false },
            new GroundTextureInfo { layerIndex = 2, displayName = "Savana Dourada", technicalName = "Savana_Seca", description = "Pasto seco e estepe semiárido", swatchColor = new Color(0.68f, 0.62f, 0.28f), isPaved = false },
            new GroundTextureInfo { layerIndex = 3, displayName = "Solo Arado", technicalName = "Solo_Arado", description = "Terra fértil preparada para lavoura", swatchColor = new Color(0.38f, 0.27f, 0.18f), isPaved = false },
            new GroundTextureInfo { layerIndex = 4, displayName = "Areia de Dunas", technicalName = "Areia_Dunas", description = "Areia dourada de praia e deserto", swatchColor = new Color(0.78f, 0.70f, 0.52f), isPaved = false },
            new GroundTextureInfo { layerIndex = 5, displayName = "Cascalho de Rio", technicalName = "Cascalho_Rio", description = "Seixos e britas arredondadas", swatchColor = new Color(0.48f, 0.47f, 0.45f), isPaved = true },
            new GroundTextureInfo { layerIndex = 6, displayName = "Rocha de Encosta", technicalName = "Rocha_Penhasco", description = "Pedra maciça de penhasco", swatchColor = new Color(0.50f, 0.48f, 0.46f), isPaved = true },
            new GroundTextureInfo { layerIndex = 7, displayName = "Neve Alpina", technicalName = "Neve_Alpina", description = "Gelo e neve compacta de altitude", swatchColor = new Color(0.92f, 0.94f, 0.98f), isPaved = false },
            new GroundTextureInfo { layerIndex = 8, displayName = "Terra Simples / Chão Batido", technicalName = "Terra_Simples", description = "Solo compactado não-cultivado (pátios, estradas vicinais)", swatchColor = new Color(0.50f, 0.36f, 0.22f), isPaved = true },
            new GroundTextureInfo { layerIndex = 9, displayName = "Concreto Urbano", technicalName = "Concreto_Urbano", description = "Cimento armado e piso industrial liso", swatchColor = new Color(0.72f, 0.73f, 0.75f), isPaved = true },
            new GroundTextureInfo { layerIndex = 10, displayName = "Asfalto Pavimentado", technicalName = "Asfalto_Pavimento", description = "Massa asfáltica escura de rodovia", swatchColor = new Color(0.24f, 0.24f, 0.26f), isPaved = true },
            new GroundTextureInfo { layerIndex = 11, displayName = "Paralelepípedo / Calçada", technicalName = "Paralelepipedo_Pedra", description = "Pedra calçada histórica e pavimentação rústica", swatchColor = new Color(0.55f, 0.54f, 0.52f), isPaved = true },
            new GroundTextureInfo { layerIndex = 12, displayName = "Pedrisco Fino / Brita Branca", technicalName = "Pedrisco_Fino", description = "Brita de paisagismo e cascalho claro", swatchColor = new Color(0.74f, 0.73f, 0.70f), isPaved = true },
            new GroundTextureInfo { layerIndex = 13, displayName = "Lama Úmida / Barro", technicalName = "Lama_Umida", description = "Solo encharcado, lamaçal e atoleiros", swatchColor = new Color(0.26f, 0.18f, 0.12f), isPaved = false },
            new GroundTextureInfo { layerIndex = 14, displayName = "Argila Vermelha / Terra Roxa", technicalName = "Argila_Vermelha", description = "Solo argiloso rico brasileiro (terra roxa)", swatchColor = new Color(0.65f, 0.28f, 0.16f), isPaved = false },
            new GroundTextureInfo { layerIndex = 15, displayName = "Grama Seca / Feno", technicalName = "Grama_Seca_Palha", description = "Palha dourada e vegetação seca", swatchColor = new Color(0.70f, 0.60f, 0.30f), isPaved = false }
        };

        /// <summary>
        /// Aplica a textura selecionada em um raio esférico/circular no mundo ao redor de worldPos.
        /// </summary>
        public static bool PaintGroundAtWorldPos(
            Terrain terrain,
            Vector3 worldPos,
            int layerIndex,
            float radiusMeters,
            float strength = 1.0f,
            bool updateGrass = true)
        {
            if (terrain == null || terrain.terrainData == null) return false;

            var tData = terrain.terrainData;
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = tData.size;
            int alphaRes = tData.alphamapResolution;

            float nx = (worldPos.x - tPos.x) / tSize.x;
            float nz = (worldPos.z - tPos.z) / tSize.z;
            if (nx < 0f || nx > 1f || nz < 0f || nz > 1f) return false;

            int centerPx = Mathf.RoundToInt(nx * (alphaRes - 1));
            int centerPz = Mathf.RoundToInt(nz * (alphaRes - 1));
            int rPx = Mathf.Max(1, Mathf.RoundToInt((radiusMeters / tSize.x) * (alphaRes - 1)));

            int minX = Mathf.Clamp(centerPx - rPx, 0, alphaRes - 1);
            int maxX = Mathf.Clamp(centerPx + rPx, 0, alphaRes - 1);
            int minZ = Mathf.Clamp(centerPz - rPx, 0, alphaRes - 1);
            int maxZ = Mathf.Clamp(centerPz + rPx, 0, alphaRes - 1);

            int width = maxX - minX + 1;
            int height = maxZ - minZ + 1;
            if (width <= 0 || height <= 0) return false;

            float[,,] alphas = tData.GetAlphamaps(minX, minZ, width, height);
            int totalLayers = alphas.GetLength(2);
            if (layerIndex < 0 || layerIndex >= totalLayers) return false;

            float rPxSq = rPx * rPx;

            for (int y = 0; y < height; y++)
            {
                int curPz = minZ + y;
                float dz = curPz - centerPz;
                float dzSq = dz * dz;

                for (int x = 0; x < width; x++)
                {
                    int curPx = minX + x;
                    float dx = curPx - centerPx;
                    float distSq = dx * dx + dzSq;
                    if (distSq > rPxSq) continue;

                    float distNorm = Mathf.Sqrt(distSq) / rPx;
                    // Curva de decaimento suave (SmoothStep)
                    float falloff = Mathf.SmoothStep(1f, 0f, distNorm);
                    float blend = Mathf.Clamp01(falloff * strength);

                    float currentTarget = alphas[y, x, layerIndex];
                    float newTarget = Mathf.Lerp(currentTarget, 1f, blend);
                    alphas[y, x, layerIndex] = newTarget;

                    // Normalizar os outros pesos mantendo a soma total estritamente em 1.0
                    float remaining = 1f - newTarget;
                    float otherSum = 0f;
                    for (int l = 0; l < totalLayers; l++)
                    {
                        if (l != layerIndex) otherSum += alphas[y, x, l];
                    }

                    if (otherSum > 0.0001f)
                    {
                        float scale = remaining / otherSum;
                        for (int l = 0; l < totalLayers; l++)
                        {
                            if (l != layerIndex) alphas[y, x, l] *= scale;
                        }
                    }
                    else
                    {
                        for (int l = 0; l < totalLayers; l++)
                        {
                            if (l != layerIndex) alphas[y, x, l] = 0f;
                        }
                    }
                }
            }

            tData.SetAlphamaps(minX, minZ, alphas);

            if (updateGrass && PlayerFollowGrass.Instance != null)
            {
                PlayerFollowGrass.Instance.InvalidateSplatCache();
            }

            return true;
        }

        /// <summary>
        /// Localiza o índice da camada pelo termo digitado (ex: "concreto", "terra", "terra simples", "asfalto", etc.)
        /// </summary>
        public static int FindLayerIndex(string query, int defaultIndex = 9)
        {
            if (string.IsNullOrEmpty(query)) return defaultIndex;

            if (int.TryParse(query, out int idx) && idx >= 0 && idx < AvailableTextures.Length)
            {
                return idx;
            }

            string q = query.ToLower().Trim();

            // Prioridade para termos específicos
            if (q.Contains("concreto") || q.Contains("concrete") || q.Contains("cimento") || q.Contains("piso"))
                return TerrainPBRFactory.LayerConcrete; // 9

            if (q.Contains("simples") || q.Contains("batid") || q.Contains("batida") || q.Contains("chao") || q.Contains("chão"))
                return TerrainPBRFactory.LayerDirtSimple; // 8

            if (q.Contains("arado") || q.Contains("lavoura") || q.Contains("cultiv"))
                return TerrainPBRFactory.LayerSoil; // 3

            if (q.Contains("terra") || q.Contains("dirt") || q.Contains("solo"))
                return TerrainPBRFactory.LayerDirtSimple; // 8 (padrão de terra simples)

            if (q.Contains("asfalto") || q.Contains("asphalt") || q.Contains("piche"))
                return TerrainPBRFactory.LayerAsphalt; // 10

            if (q.Contains("paralelepipedo") || q.Contains("pedra") || q.Contains("calcada") || q.Contains("calçada") || q.Contains("cobble"))
                return TerrainPBRFactory.LayerCobblestone; // 11

            if (q.Contains("pedrisco") || q.Contains("britafina") || q.Contains("branca"))
                return TerrainPBRFactory.LayerWhiteGravel; // 12

            if (q.Contains("brita") || q.Contains("cascalho") || q.Contains("gravel"))
                return TerrainPBRFactory.LayerGravel; // 5

            if (q.Contains("lama") || q.Contains("barro") || q.Contains("mud") || q.Contains("brejo"))
                return TerrainPBRFactory.LayerMud; // 13

            if (q.Contains("argila") || q.Contains("vermelha") || q.Contains("roxa") || q.Contains("clay"))
                return TerrainPBRFactory.LayerRedClay; // 14

            if (q.Contains("grama") || q.Contains("grass") || q.Contains("verde") || q.Contains("pastagem"))
                return TerrainPBRFactory.LayerGrass; // 0

            if (q.Contains("feno") || q.Contains("palha") || q.Contains("seca"))
                return TerrainPBRFactory.LayerDeadGrass; // 15

            if (q.Contains("areia") || q.Contains("sand") || q.Contains("praia") || q.Contains("duna"))
                return TerrainPBRFactory.LayerSand; // 4

            if (q.Contains("rocha") || q.Contains("rock") || q.Contains("penhasco"))
                return TerrainPBRFactory.LayerRock; // 6

            if (q.Contains("neve") || q.Contains("snow") || q.Contains("gelo"))
                return TerrainPBRFactory.LayerSnow; // 7

            if (q.Contains("selva") || q.Contains("floresta") || q.Contains("folha"))
                return TerrainPBRFactory.LayerJungle; // 1

            return defaultIndex;
        }
    }
}
