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
            if (isLoaded && idMapData != null) return;

            lock (lockObj)
            {
                if (isLoaded && idMapData != null) return;

                string path = Path.Combine(Application.streamingAssetsPath, "region_id_map.bin");
                if (File.Exists(path))
                {
                    try
                    {
                        idMapData = File.ReadAllBytes(path);
                        isLoaded = idMapData.Length == MapWidth * MapHeight * 2;
                        Debug.Log($"[RegionBoundaryService] Mapa binário de fronteiras geográficas carregado ({idMapData.Length / (1024 * 1024)} MB).");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RegionBoundaryService] Falha ao carregar region_id_map.bin: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[RegionBoundaryService] Arquivo region_id_map.bin não encontrado em: {path}");
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
            if (idMapData == null) return true;

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
