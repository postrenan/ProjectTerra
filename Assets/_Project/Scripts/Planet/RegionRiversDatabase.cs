using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>Uma polilinha de rio em coordenadas lat/lon reais, com largura visual em metros.</summary>
    public class RiverPolyline
    {
        public float width;
        public Vector2[] points; // x = lat, y = lon
    }

    /// <summary>
    /// Banco de dados da hidrografia linear (StreamingAssets/regions_rivers.bin, magic 'PTRV').
    /// Índice por região + leitura sob demanda, igual a <see cref="RegionRoadsDatabase"/>.
    /// </summary>
    public static class RegionRiversDatabase
    {
        private const uint MagicNumber = 0x56525450; // 'PTRV' little-endian

        private struct IndexEntry { public long offset; public int polyCount; }
        private static Dictionary<int, IndexEntry> index;
        private static string filePath;

        public static void EnsureLoaded()
        {
            if (index != null) return;
            index = new Dictionary<int, IndexEntry>();
            filePath = Path.Combine(Application.streamingAssetsPath, "regions_rivers.bin");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("[RegionRivers] regions_rivers.bin não encontrado — rode Tools/GenerateRegionRivers.py.");
                return;
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    if (reader.ReadUInt32() != MagicNumber)
                    {
                        Debug.LogWarning("[RegionRivers] Magic inválido em regions_rivers.bin.");
                        return;
                    }
                    reader.ReadUInt16(); // version
                    int regionCount = reader.ReadInt32();
                    for (int i = 0; i < regionCount; i++)
                    {
                        int regionId = reader.ReadInt32();
                        long offset = reader.ReadInt64();
                        int polyCount = reader.ReadUInt16();
                        index[regionId] = new IndexEntry { offset = offset, polyCount = polyCount };
                    }
                }
                Debug.Log($"[RegionRivers] Índice de hidrografia carregado: {index.Count} regiões.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegionRivers] Falha ao ler índice de regions_rivers.bin: {ex.Message}");
            }
        }

        public static List<RiverPolyline> GetRivers(int regionId)
        {
            EnsureLoaded();
            var result = new List<RiverPolyline>();
            if (index == null || !index.TryGetValue(regionId, out var entry)) return result;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    fs.Seek(entry.offset, SeekOrigin.Begin);
                    for (int p = 0; p < entry.polyCount; p++)
                    {
                        var river = new RiverPolyline { width = reader.ReadSingle() };
                        int ptCount = reader.ReadUInt16();
                        river.points = new Vector2[ptCount];
                        for (int k = 0; k < ptCount; k++)
                        {
                            float lat = reader.ReadSingle();
                            float lon = reader.ReadSingle();
                            river.points[k] = new Vector2(lat, lon);
                        }
                        result.Add(river);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegionRivers] Falha ao ler bloco da região {regionId}: {ex.Message}");
            }
            return result;
        }
    }
}
