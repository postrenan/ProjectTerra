using System;
using System.IO;
using UnityEngine;

namespace ProjectTerra.Planet.TerrainStreaming
{
    /// <summary>
    /// Fábrica e gerador de texturas PBR fotorrealistas e splatmapping adaptativo do Unity Terrain.
    /// Carrega paleta expandida de 8 biomas (Grama, Selva, Savana, Solo, Areia, Cascalho, Rocha e Neve)
    /// e computa a distribuição com amostragem real da imagem de satélite da Terra combinada com
    /// relevo físico, mosaicos agrícolas de lavouras e mesclas micro-orgânicas de solo.
    /// </summary>
    public static partial class TerrainPBRFactory
    {
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> textureCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

        private enum TextureMapType { Albedo, Normal, MaskMap }

        public const int LayerCount = 16;
        public const int LayerGrass = 0;             // Grama Lush (Pradaria Verde)
        public const int LayerJungle = 1;            // Selva / Floresta Tropical
        public const int LayerSavanna = 2;           // Savana / Pasto Seco
        public const int LayerSoil = 3;              // Solo Arado / Lavoura
        public const int LayerSand = 4;              // Areia de Dunas / Praia
        public const int LayerGravel = 5;            // Cascalho de Rio / Britas
        public const int LayerRock = 6;              // Rocha de Encosta / Penhasco
        public const int LayerSnow = 7;              // Neve Alpina / Tundra
        // Camadas adicionais de pintura e engenharia civil:
        public const int LayerDirtSimple = 8;        // Terra Simples / Chão Batido
        public const int LayerConcrete = 9;          // Concreto Urbano / Cimento
        public const int LayerAsphalt = 10;          // Asfalto Pavimentado
        public const int LayerCobblestone = 11;      // Paralelepípedo / Calçada de Pedra
        public const int LayerWhiteGravel = 12;      // Pedrisco / Brita Fina
        public const int LayerMud = 13;              // Lama Úmida / Barro
        public const int LayerRedClay = 14;          // Argila Vermelha / Terra Roxa
        public const int LayerDeadGrass = 15;        // Grama Queimada / Feno Seco

        public static TerrainLayer[] CreateTerrainLayers()
        {
            var layers = new TerrainLayer[LayerCount];

            // 0: Grama Lush / Pradaria verde
            layers[LayerGrass] = CreateLayer("Grama_Lush", "Ground/Grass", new Vector2(14f, 14f), new Color(0.25f, 0.45f, 0.2f), smoothness: 0.08f);

            // 1: Selva / Floresta Densa Tropical
            layers[LayerJungle] = CreateLayer("Selva_Foliage", "Vegetation/Foliage", new Vector2(18f, 18f), new Color(0.18f, 0.48f, 0.15f), smoothness: 0.10f);

            // 2: Savana Seca / Estepe / Pasto Dourado
            layers[LayerSavanna] = CreateLayer("Savana_Seca", "Ground/DryGrass", new Vector2(16f, 16f), new Color(0.6f, 0.56f, 0.26f), smoothness: 0.06f);

            // 3: Solo Arável / Terra Fértil
            layers[LayerSoil] = CreateLayer("Solo_Arado", "Ground/Soil", new Vector2(12f, 12f), new Color(0.37f, 0.26f, 0.18f), smoothness: 0.06f);

            // 4: Areia de Dunas / Deserto / Praia
            layers[LayerSand] = CreateLayer("Areia_Dunas", "Ground/Sand", new Vector2(12f, 12f), new Color(0.76f, 0.68f, 0.5f), smoothness: 0.05f);

            // 5: Cascalho / Leito de Rios e Seixos
            layers[LayerGravel] = CreateLayer("Cascalho_Rio", "Ground/Gravel", new Vector2(10f, 10f), new Color(0.41f, 0.4f, 0.38f), smoothness: 0.10f);

            // 6: Rocha de Encosta / Penhasco
            layers[LayerRock] = CreateLayer("Rocha_Penhasco", "Ground/Rock", new Vector2(24f, 24f), new Color(0.47f, 0.45f, 0.43f), smoothness: 0.12f);

            // 7: Neve Alpina / Tundra Glacial
            layers[LayerSnow] = CreateLayer("Neve_Alpina", "Ground/Snow", new Vector2(16f, 16f), new Color(0.9f, 0.92f, 0.96f), smoothness: 0.15f);

            // 8: Terra Simples / Chão Batido (Solo compactado não-cultivado)
            layers[LayerDirtSimple] = CreateLayer("Terra_Simples", "Ground/Soil", new Vector2(8f, 8f), new Color(0.48f, 0.35f, 0.22f), smoothness: 0.05f);

            // 9: Concreto Urbano / Cimento Armado (Piso pavimentado moderno)
            layers[LayerConcrete] = CreateLayer("Concreto_Urbano", "Infrastructure/Concrete", new Vector2(6f, 6f), new Color(0.72f, 0.72f, 0.74f), smoothness: 0.28f);

            // 10: Asfalto Pavimentado (PBR rodoviário)
            layers[LayerAsphalt] = CreateLayer("Asfalto_Pavimento", "Infrastructure/Asphalt", new Vector2(6f, 6f), new Color(0.24f, 0.24f, 0.25f), smoothness: 0.22f);

            // 11: Paralelepípedo / Calçamento de Pedra Rústica
            layers[LayerCobblestone] = CreateLayer("Paralelepipedo_Pedra", "Infrastructure/Cobblestone", new Vector2(5f, 5f), new Color(0.52f, 0.51f, 0.49f), smoothness: 0.18f);

            // 12: Pedrisco Fino / Brita Branca de Paisagismo
            layers[LayerWhiteGravel] = CreateLayer("Pedrisco_Fino", "Ground/Gravel", new Vector2(5f, 5f), new Color(0.72f, 0.71f, 0.69f), smoothness: 0.12f);

            // 13: Lama Úmida / Barro Encharcado
            layers[LayerMud] = CreateLayer("Lama_Umida", "Ground/Mud", new Vector2(7f, 7f), new Color(0.25f, 0.18f, 0.11f), smoothness: 0.42f);

            // 14: Argila Vermelha / Terra Roxa
            layers[LayerRedClay] = CreateLayer("Argila_Vermelha", "Ground/RedClay", new Vector2(8f, 8f), new Color(0.62f, 0.27f, 0.15f), smoothness: 0.06f);

            // 15: Grama Queimada / Feno Seco
            layers[LayerDeadGrass] = CreateLayer("Grama_Seca_Palha", "Ground/DryGrass", new Vector2(12f, 12f), new Color(0.68f, 0.58f, 0.28f), smoothness: 0.05f);

            return layers;
        }

        private static TerrainLayer CreateLayer(string name, string subPath, Vector2 tileSize, Color fallbackColor, float smoothness = 0.08f, float metallic = 0.0f)
        {
            var layer = new TerrainLayer();
            layer.name = name;
            layer.tileSize = tileSize;
            layer.smoothness = smoothness;
            layer.metallic = metallic;
            layer.normalScale = 0.8f;
            layer.specular = new Color(0.08f, 0.08f, 0.08f, 1.0f);

            string baseDir = System.IO.Path.Combine(Application.dataPath, "_Project", "Textures", "PBR", subPath);
            layer.diffuseTexture = LoadPBRTexture(baseDir, subPath, "Albedo", TextureMapType.Albedo, fallbackColor);
            layer.normalMapTexture = LoadPBRTexture(baseDir, subPath, "Normal", TextureMapType.Normal, new Color(0.5f, 0.5f, 1.0f, 1.0f));

            bool isHdrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null && 
                          UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HD");
            if (isHdrp)
            {
                layer.maskMapTexture = LoadPBRTexture(baseDir, subPath, "MaskMap", TextureMapType.MaskMap, new Color(0.0f, 0.9f, 0.5f, 0.15f));
            }
            else
            {
                layer.maskMapTexture = null;
            }

            bool usedFallback = (layer.diffuseTexture != null && layer.diffuseTexture.width == 16 && layer.diffuseTexture.height == 16);
            Debug.Log($"[TerrainPBRFactory] Layer '{name}' ({subPath}): diffuse={(layer.diffuseTexture != null ? $"{layer.diffuseTexture.width}x{layer.diffuseTexture.height}" : "NULL")} {(usedFallback ? "[FALLBACK COLORIDO]" : "[TEXTURA REAL]")}");

            return layer;
        }

        private static Texture2D LoadPBRTexture(string baseDir, string subPath, string fileName, TextureMapType mapType, Color fallbackColor)
        {
            string cacheKey = $"{subPath}_{fileName}";
            if (textureCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;

#if UNITY_EDITOR
            string assetPath = $"Assets/_Project/Textures/PBR/{subPath}/{fileName}.png";
            var editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (editorTex == null)
            {
                assetPath = $"Assets/_Project/Textures/PBR/{subPath}/{fileName}.tga";
                editorTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            if (editorTex != null)
            {
                textureCache[cacheKey] = editorTex;
                return editorTex;
            }
#endif

            // Carregamento de PNG
            string pngPath = Path.Combine(baseDir, $"{fileName}.png");
            if (File.Exists(pngPath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(pngPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (tex.LoadImage(data))
                    {
                        tex.wrapMode = TextureWrapMode.Repeat;
                        tex.filterMode = FilterMode.Trilinear;
                        textureCache[cacheKey] = tex;
                        return tex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TerrainPBRFactory] Erro ao ler PNG {pngPath}: {ex.Message}");
                }
            }

            // Carregamento de TGA
            string tgaPath = Path.Combine(baseDir, $"{fileName}.tga");
            if (File.Exists(tgaPath))
            {
                try
                {
                    byte[] data = File.ReadAllBytes(tgaPath);
                    var tgaTex = DecodeTGA(data);
                    if (tgaTex != null)
                    {
                        tgaTex.wrapMode = TextureWrapMode.Repeat;
                        tgaTex.filterMode = FilterMode.Trilinear;
                        textureCache[cacheKey] = tgaTex;
                        return tgaTex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TerrainPBRFactory] Erro ao decodificar TGA {tgaPath}: {ex.Message}");
                }
            }

            var fallbackTex = GenerateFallbackTexture(mapType, fallbackColor);
            textureCache[cacheKey] = fallbackTex;
            return fallbackTex;
        }

        private static Texture2D DecodeTGA(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 18) return null;
            int idLength = bytes[0];
            int imageType = bytes[2];
            if (imageType != 2 && imageType != 10) return null;
            int width = bytes[12] | (bytes[13] << 8);
            int height = bytes[14] | (bytes[15] << 8);
            int bpp = bytes[16];
            if (width <= 0 || height <= 0 || (bpp != 24 && bpp != 32)) return null;

            bool topDown = (bytes[17] & 0x20) != 0;
            int bytesPerPixel = bpp / 8;
            int dataOffset = 18 + idLength;

            var tex = new Texture2D(width, height, bpp == 32 ? TextureFormat.RGBA32 : TextureFormat.RGB24, true);
            Color32[] colors = new Color32[width * height];

            if (imageType == 2)
            {
                for (int y = 0; y < height; y++)
                {
                    int row = topDown ? (height - 1 - y) : y;
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = dataOffset + (y * width + x) * bytesPerPixel;
                        if (srcIdx + bytesPerPixel <= bytes.Length)
                        {
                            byte b = bytes[srcIdx];
                            byte g = bytes[srcIdx + 1];
                            byte r = bytes[srcIdx + 2];
                            byte a = bytesPerPixel == 4 ? bytes[srcIdx + 3] : (byte)255;
                            colors[row * width + x] = new Color32(r, g, b, a);
                        }
                    }
                }
            }
            tex.SetPixels32(colors);
            tex.Apply(true);
            return tex;
        }

        private static Texture2D GenerateFallbackTexture(TextureMapType type, Color color)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, true);
            Color[] cols = new Color[16 * 16];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (type == TextureMapType.Normal)
                    {
                        cols[y * 16 + x] = new Color(0.5f, 0.5f, 1.0f, 1.0f);
                    }
                    else if (type == TextureMapType.MaskMap)
                    {
                        cols[y * 16 + x] = new Color(0.0f, 0.9f, 0.5f, 0.15f);
                    }
                    else
                    {
                        float noise = (Mathf.Sin(x * 1.5f) * Mathf.Cos(y * 1.5f)) * 0.04f;
                        cols[y * 16 + x] = new Color(
                            Mathf.Clamp01(color.r + noise),
                            Mathf.Clamp01(color.g + noise),
                            Mathf.Clamp01(color.b + noise),
                            0.12f
                        );
                    }
                }
            }
            tex.SetPixels(cols);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.Apply(true);
            return tex;
        }
    }
}
