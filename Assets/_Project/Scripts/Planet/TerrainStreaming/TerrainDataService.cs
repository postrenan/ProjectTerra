using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectTerra.Planet.TerrainStreaming
{
    /// <summary>
    /// Serviço de transmissão e cache de dados topográficos em escala real (1:1).
    /// Suporta streaming assíncrono de modelos digitais de elevação (DEM) globais (Copernicus DEM 30m / AWS Terrarium),
    /// decodificação de altura em metros reais, armazenamento local em binário puro e síntese procedural para modo offline.
    /// </summary>
    public class TerrainDataService : MonoBehaviour
    {
        public static TerrainDataService Instance { get; private set; }

        private const string AwsTerrariumUrlTemplate = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{0}/{1}/{2}.png";
        private const int TileResolution = 256;

        private string cacheDirectory;
        private readonly Dictionary<string, float[]> memoryTileCache = new Dictionary<string, float[]>();
        private readonly HashSet<string> activeDownloads = new HashSet<string>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCache();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeCache()
        {
            cacheDirectory = Path.Combine(Application.persistentDataPath, "TerrainCache");
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
            Debug.Log($"[TerrainDataService] Diretório de cache de topografia 1:1 pronto em: {cacheDirectory}");
        }

        /// <summary>
        /// Converte coordenadas de Latitude e Longitude (graus WGS84) para coordenadas de tile Web Mercator (Slippy Map).
        /// </summary>
        public static Vector2Int LatLonToTile(double lat, double lon, int zoom)
        {
            lat = Math.Clamp(lat, -85.05112878, 85.05112878);
            lon = Math.Clamp(lon, -180.0, 180.0);

            int n = 1 << zoom;
            int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);

            return new Vector2Int(Math.Clamp(x, 0, n - 1), Math.Clamp(y, 0, n - 1));
        }

        /// <summary>
        /// Converte coordenadas de tile Slippy Map para Latitude e Longitude do canto noroeste do tile.
        /// </summary>
        public static Vector2 TileToLatLon(int x, int y, int zoom)
        {
            int n = 1 << zoom;
            float lon = (float)(x / (double)n * 360.0 - 180.0);
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * y / (double)n)));
            float lat = (float)(latRad * 180.0 / Math.PI);
            return new Vector2(lat, lon);
        }

        /// <summary>
        /// Solicita assincronamente um tile de elevação para uma dada coordenada e nível de zoom.
        /// Retorna o array de alturas de 256x256 pontos em metros reais acima do nível do mar.
        /// </summary>
        public void RequestElevationTile(int zoom, int tileX, int tileY, Action<float[]> onComplete)
        {
            string tileKey = $"{zoom}_{tileX}_{tileY}";

            // 1. Memória RAM
            if (memoryTileCache.TryGetValue(tileKey, out float[] cachedData))
            {
                onComplete?.Invoke(cachedData);
                return;
            }

            // 2. Disco Local (Binário Bruto)
            string localBinPath = Path.Combine(cacheDirectory, $"{tileKey}.bin");
            if (File.Exists(localBinPath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(localBinPath);
                    if (bytes.Length == TileResolution * TileResolution * sizeof(float))
                    {
                        float[] heights = new float[TileResolution * TileResolution];
                        Buffer.BlockCopy(bytes, 0, heights, 0, bytes.Length);
                        memoryTileCache[tileKey] = heights;
                        onComplete?.Invoke(heights);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TerrainDataService] Erro ao carregar tile de cache local {tileKey}: {ex.Message}");
                }
            }

            // 3. Download da Nuvem (AWS Open Data Terrarium)
            if (!activeDownloads.Contains(tileKey))
            {
                StartCoroutine(DownloadTerrariumTileCoroutine(zoom, tileX, tileY, tileKey, localBinPath, onComplete));
            }
            else
            {
                // Se já estiver em download, gera elevação sintética de resposta imediata
                float[] proceduralHeights = GenerateProceduralTile(zoom, tileX, tileY);
                onComplete?.Invoke(proceduralHeights);
            }
        }

        private IEnumerator DownloadTerrariumTileCoroutine(int zoom, int tileX, int tileY, string tileKey, string localBinPath, Action<float[]> onComplete)
        {
            activeDownloads.Add(tileKey);
            string url = string.Format(AwsTerrariumUrlTemplate, zoom, tileX, tileY);

            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                uwr.timeout = 10;
                yield return uwr.SendWebRequest();

                activeDownloads.Remove(tileKey);

                if (uwr.result == UnityWebRequest.Result.Success && uwr.downloadHandler != null && uwr.downloadHandler.data != null)
                {
                    Texture2D downloadedTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (downloadedTex.LoadImage(uwr.downloadHandler.data))
                    {
                        Color32[] pixels = downloadedTex.GetPixels32();
                        float[] heights = new float[TileResolution * TileResolution];

                        // Decodificação oficial do formato Terrarium da AWS:
                        // Altura em metros = (R * 256 + G + B / 256) - 32768
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            Color32 c = pixels[i];
                            float h = (c.r * 256.0f + c.g + c.b / 256.0f) - 32768.0f;
                            heights[i] = h;
                        }

                        // Salva no cache de RAM
                        memoryTileCache[tileKey] = heights;

                        // Salva no disco em formato binário float32 para carregamento instantâneo subsequente
                        try
                        {
                            byte[] byteBuffer = new byte[heights.Length * sizeof(float)];
                            Buffer.BlockCopy(heights, 0, byteBuffer, 0, byteBuffer.Length);
                            File.WriteAllBytes(localBinPath, byteBuffer);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[TerrainDataService] Erro ao gravar cache binário do tile {tileKey}: {ex.Message}");
                        }

                        Destroy(downloadedTex);
                        onComplete?.Invoke(heights);
                        yield break;
                    }
                }
            }

            // Fallback: se offline ou falha de conexão, sintetiza relevo fractal geologicamente consistente
            float[] fallbackHeights = GenerateProceduralTile(zoom, tileX, tileY);
            memoryTileCache[tileKey] = fallbackHeights;
            onComplete?.Invoke(fallbackHeights);
        }

        /// <summary>
        /// Sintetizador procedural fractal de alta fidelidade para relevo terrestre quando o usuário estiver offline.
        /// </summary>
        public float[] GenerateProceduralTile(int zoom, int tileX, int tileY)
        {
            float[] heights = new float[TileResolution * TileResolution];
            Vector2 baseLatLon = TileToLatLon(tileX, tileY, zoom);

            float freqBase = 0.05f * (1 << Math.Min(zoom, 10));
            float seedX = (tileX * 137.15f) % 1000.0f;
            float seedY = (tileY * 269.45f) % 1000.0f;

            // Determina a amplitude básica conforme a latitude (cordilheiras, planaltos, pampas)
            float baseAmplitude = 350.0f;
            if (Math.Abs(baseLatLon.x) > 30.0f && Math.Abs(baseLatLon.x) < 55.0f)
            {
                baseAmplitude = 600.0f; // Zonas temperadas / cadeias montanhosas
            }

            for (int y = 0; y < TileResolution; y++)
            {
                float ny = (float)y / TileResolution;
                for (int x = 0; x < TileResolution; x++)
                {
                    float nx = (float)x / TileResolution;

                    float px = (seedX + nx) * freqBase;
                    float py = (seedY + ny) * freqBase;

                    // Relevo fractal com 4 oitavas (Macro relevo, colinas e micro ondulações de plantio)
                    float n1 = Mathf.PerlinNoise(px, py) * 1.0f;
                    float n2 = Mathf.PerlinNoise(px * 2.2f, py * 2.2f) * 0.45f;
                    float n3 = Mathf.PerlinNoise(px * 5.1f, py * 5.1f) * 0.18f;
                    float n4 = Mathf.PerlinNoise(px * 12.0f, py * 12.0f) * 0.06f;

                    float combined = (n1 + n2 + n3 + n4) / 1.69f;
                    float finalHeight = Mathf.Pow(combined, 1.4f) * baseAmplitude + 45.0f;

                    heights[y * TileResolution + x] = finalHeight;
                }
            }

            return heights;
        }

        /// <summary>
        /// Obtém a elevação instantânea aproximada em metros para qualquer ponto do globo.
        /// </summary>
        public float SampleElevationDirect(double lat, double lon)
        {
            int zoom = 8;
            Vector2Int tile = LatLonToTile(lat, lon, zoom);
            string key = $"{zoom}_{tile.x}_{tile.y}";

            if (memoryTileCache.TryGetValue(key, out float[] heights))
            {
                Vector2 nw = TileToLatLon(tile.x, tile.y, zoom);
                Vector2 se = TileToLatLon(tile.x + 1, tile.y + 1, zoom);

                float u = Mathf.Clamp01((float)((lon - nw.y) / (se.y - nw.y)));
                float v = Mathf.Clamp01((float)((nw.x - lat) / (nw.x - se.x)));

                int px = Mathf.Clamp((int)(u * (TileResolution - 1)), 0, TileResolution - 1);
                int py = Mathf.Clamp((int)(v * (TileResolution - 1)), 0, TileResolution - 1);

                return heights[py * TileResolution + px];
            }

            // Fallback sintético
            float fx = (float)((lon + 180.0) * 0.25f);
            float fy = (float)((lat + 90.0) * 0.25f);
            return Mathf.PerlinNoise(fx, fy) * 450.0f + 30.0f;
        }
    }
}
