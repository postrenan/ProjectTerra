using System;
using System.IO;
using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Serviço de alta performance para consulta e extração do formato geométrico real
    /// de Estados e Províncias mundiais a partir do mapa binário geográfico (region_id_map.bin).
    /// Permite recorte do contorno exato da província no mapa tático GIS e no globo.
    /// </summary>
    public static class RegionBoundaryService
    {
        public const int MapWidth = 4096;
        public const int MapHeight = 2048;
        private static byte[] idMapData;
        private static bool isLoaded = false;
        private static bool loadAttempted = false;
        private static readonly object lockObj = new object();

        public static byte[] Data
        {
            get
            {
                EnsureLoaded();
                return idMapData;
            }
        }

        public static bool IsLoaded => isLoaded;

        /// <summary>
        /// Carrega os 16 MB do mapa raster global de IDs se ainda não estiver em memória.
        /// Thread-safe e reutilizado em todas as cenas.
        /// </summary>
        public static void EnsureLoaded()
        {
            // IsLoaded sozinho não bastava: ele só era setado no caminho de sucesso,
            // então um ficheiro ausente/truncado fazia EnsureLoaded ser um no-op e
            // TODO chamador reentrava no lock, refazia File.Exists + Log e relia os
            // 16 MB. O mapa tático chama isto 2x por pixel (512x512 = 524.288 vezes por
            // abertura), o que travava o jogo por minutos e inundava o console.
            // A tentativa é marcada como "feita" antes de ler: o ficheiro em
            // StreamingAssets é imutável em runtime, então não há ganho em repetir.
            if (loadAttempted) return;

            lock (lockObj)
            {
                if (loadAttempted) return;
                loadAttempted = true;

                string path = Path.Combine(Application.streamingAssetsPath, "region_id_map.bin");
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[RegionBoundaryService] Arquivo region_id_map.bin não encontrado em: {path}");
                    return;
                }

                try
                {
                    byte[] raw = File.ReadAllBytes(path);
                    int expected = MapWidth * MapHeight * 2;
                    if (raw.Length != expected)
                    {
                        Debug.LogWarning($"[RegionBoundaryService] region_id_map.bin com tamanho inesperado ({raw.Length} bytes, esperado {expected}). Contorno de província indisponível.");
                        return;
                    }

                    idMapData = raw;
                    isLoaded = true;
                    Debug.Log($"[RegionBoundaryService] Mapa binário de fronteiras geográficas carregado ({raw.Length / (1024 * 1024)} MB).");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RegionBoundaryService] Falha ao carregar region_id_map.bin: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Obtém o ID da região/estado contido na coordenada geográfica informada.
        /// 0 = Águas abertas / Marítimo internacional.
        /// </summary>
        public static ushort GetRegionIdAt(float lat, float lon)
        {
            EnsureLoaded();
            if (idMapData == null) return 0;

            int px = Mathf.Clamp((int)((lon + 180f) / 360f * (MapWidth - 1)), 0, MapWidth - 1);
            int py = Mathf.Clamp((int)((90f - lat) / 180f * (MapHeight - 1)), 0, MapHeight - 1);
            int idx = (py * MapWidth + px) * 2;
            if (idx + 1 >= idMapData.Length) return 0;
            return (ushort)(idMapData[idx] | (idMapData[idx + 1] << 8));
        }

        /// <summary>
        /// Verifica se a coordenada cai dentro dos limites geográficos da província alvo,
        /// incluindo suporte a tolerância litorânea imediata (baías, enseadas e golfos da região).
        /// </summary>
        public static bool IsInsideRegion(int targetRegionId, float lat, float lon)
        {
            if (targetRegionId <= 0) return true;
            EnsureLoaded();

            // Sem o mapa não há como provar que o ponto está dentro, então a resposta
            // é "fora". Devolver true aqui marcava os 262.144 pixels do retângulo da
            // província como interiores, o contorno preenchia o mapa inteiro e o
            // hasRegionMask do HUD desligava o fallback de linha de costa. A
            // out-of-range logo abaixo já devolvia false — esta era a inconsistência.
            if (idMapData == null) return false;

            int px = Mathf.Clamp((int)((lon + 180f) / 360f * (MapWidth - 1)), 0, MapWidth - 1);
            int py = Mathf.Clamp((int)((90f - lat) / 180f * (MapHeight - 1)), 0, MapHeight - 1);
            int idx = (py * MapWidth + px) * 2;
            if (idx + 1 >= idMapData.Length) return false;

            ushort id = (ushort)(idMapData[idx] | (idMapData[idx + 1] << 8));
            if (id == targetRegionId) return true;

            // Se for água costeira imediata (ID 0), verifica se é água adjacente a esta província (até 2px ~ 15km)
            if (id == 0)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    int ny = py + dy;
                    if (ny < 0 || ny >= MapHeight) continue;
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = px + dx;
                        if (nx < 0 || nx >= MapWidth) continue;
                        int cIdx = (ny * MapWidth + nx) * 2;
                        ushort candId = (ushort)(idMapData[cIdx] | (idMapData[cIdx + 1] << 8));
                        if (candId == targetRegionId) return true;
                    }
                }
            }

            return false;
        }
    }
}
