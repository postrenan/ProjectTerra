using System;
using System.IO;
using UnityEngine;

namespace ProjectTerra.Planet.TerrainStreaming
{
    /// <summary>
    /// Fábrica e configurador de texturas PBR fotorrealistas e splatmapping do Unity Terrain.
    /// Carrega mapas PBR (Albedo, Normal, MaskMap) e computa distribuição geológica natural
    /// baseada na declividade (rochas em encostas, terra arada em vales, grama e cascalho de rios).
    /// </summary>
    public static class TerrainPBRFactory
    {
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> textureCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

        public static TerrainLayer[] CreateTerrainLayers()
        {
            var layers = new TerrainLayer[4];

            // Camada 0: Grama / Vegetação Campestre
            layers[0] = CreateLayer("Grama_Lush", "Ground/Grass", new Vector2(15f, 15f));

            // Camada 1: Solo Arável / Terra Fértil
            layers[1] = CreateLayer("Solo_Arado", "Ground/Soil", new Vector2(12f, 12f));

            // Camada 2: Rocha de Encosta / Penhasco
            layers[2] = CreateLayer("Rocha_Penhasco", "Ground/Rock", new Vector2(25f, 25f));

            // Camada 3: Areia e Cascalho de Leito de Rio
            layers[3] = CreateLayer("Areia_Cascalho", "Ground/Sand", new Vector2(10f, 10f));

            return layers;
        }

        private static TerrainLayer CreateLayer(string name, string subPath, Vector2 tileSize)
        {
            var layer = new TerrainLayer();
            layer.name = name;
            layer.tileSize = tileSize;

            string baseDir = Path.Combine(Application.dataPath, "_Project", "Textures", "PBR", subPath);
            layer.diffuseTexture = LoadTexture(Path.Combine(baseDir, "Albedo.tga"));
            layer.normalMapTexture = LoadTexture(Path.Combine(baseDir, "Normal.tga"));
            layer.maskMapTexture = LoadTexture(Path.Combine(baseDir, "MaskMap.tga"));

            return layer;
        }

        private static Texture2D LoadTexture(string path)
        {
            if (textureCache.TryGetValue(path, out var cached)) return cached;

            if (File.Exists(path))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2, TextureFormat.RGB24, true);
                    if (tex.LoadImage(data))
                    {
                        tex.wrapMode = TextureWrapMode.Repeat;
                        tex.filterMode = FilterMode.Trilinear;
                        textureCache[path] = tex;
                        return tex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TerrainPBRFactory] Erro ao carregar textura {path}: {ex.Message}");
                }
            }

            // Fallback para textura sólida se não encontrar
            var solidTex = new Texture2D(4, 4, TextureFormat.RGB24, false);
            Color solidColor = path.Contains("Grass") ? new Color(0.25f, 0.45f, 0.2f) :
                               path.Contains("Rock") ? new Color(0.45f, 0.45f, 0.45f) :
                               path.Contains("Soil") ? new Color(0.35f, 0.25f, 0.15f) : new Color(0.7f, 0.65f, 0.5f);
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = solidColor;
            solidTex.SetPixels(cols);
            solidTex.Apply();
            return solidTex;
        }

        /// <summary>
        /// Computa o splatmap em 4 camadas de acordo com as curvas de nível, inclinação e hidrografia real.
        /// </summary>
        public static float[,,] CalculateSplatmaps(TerrainData terrainData, RegionalHydroData hydro, int arablePercent)
        {
            int alphaRes = terrainData.alphamapResolution;
            float[,,] splatmap = new float[alphaRes, alphaRes, 4];

            float arableFactor = arablePercent / 100.0f;
            float seaLevel = hydro != null ? hydro.seaLevelNormalized : 0f;

            for (int y = 0; y < alphaRes; y++)
            {
                float ny = y / (float)(alphaRes - 1);
                for (int x = 0; x < alphaRes; x++)
                {
                    float nx = x / (float)(alphaRes - 1);

                    // Inclinação do terreno (declive em graus)
                    float steepness = terrainData.GetSteepness(nx, ny);
                    float heightNorm = terrainData.GetInterpolatedHeight(nx, ny) / terrainData.size.y;

                    float grassWeight = 0f;
                    float soilWeight = 0f;
                    float rockWeight = 0f;
                    float sandWeight = 0f;

                    // 1. Encostas íngremes (> 28 graus) viram Rocha pura
                    if (steepness > 28f)
                    {
                        rockWeight = Mathf.Clamp01((steepness - 28f) / 18f);
                    }

                    // 2. Margens de água / praias / vales baixos viram Areia/Cascalho
                    if (heightNorm <= seaLevel + 0.04f)
                    {
                        sandWeight = Mathf.Clamp01(1.0f - (heightNorm - seaLevel) / 0.04f);
                    }

                    // 3. Planícies e platôs: equilíbrio entre Grama e Solo Arável
                    float remaining = Mathf.Max(0f, 1.0f - (rockWeight + sandWeight));
                    soilWeight = remaining * (arableFactor * 0.75f);
                    grassWeight = remaining - soilWeight;

                    // Normalização do total para 1.0
                    float total = grassWeight + soilWeight + rockWeight + sandWeight;
                    if (total > 0f)
                    {
                        splatmap[y, x, 0] = grassWeight / total; // Grama
                        splatmap[y, x, 1] = soilWeight / total;  // Solo Arável
                        splatmap[y, x, 2] = rockWeight / total;  // Rocha
                        splatmap[y, x, 3] = sandWeight / total;  // Areia/Cascalho
                    }
                    else
                    {
                        splatmap[y, x, 0] = 1.0f;
                    }
                }
            }

            return splatmap;
        }
    }
}
