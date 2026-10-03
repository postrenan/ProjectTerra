using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Planet
{
    public enum RoadClass : byte
    {
        MajorHighway = 0,
        SecondaryHighway = 1,
        Expressway = 2,
        Railroad = 3,
        Connector = 4
    }

    /// <summary>Uma polilinha de via (rodovia/ferrovia/conector) em coordenadas lat/lon reais.</summary>
    public class RoadPolyline
    {
        public RoadClass roadClass;
        public Vector2[] points; // x = lat, y = lon
    }

    /// <summary>
    /// Banco de dados da malha de transporte (StreamingAssets/regions_roads.bin, magic 'PTRO').
    /// Lê a tabela de índice no início (seek por região) e carrega sob demanda apenas o
    /// bloco da região ativa — o arquivo global de rodovias/ferrovias é grande (~9 MB).
    /// </summary>
    public static class RegionRoadsDatabase
    {
        private const uint MagicNumber = 0x4F525450; // 'PTRO' little-endian

        private struct IndexEntry { public long offset; public int polyCount; }
        private static Dictionary<int, IndexEntry> index;
        private static string filePath;

        public static void EnsureLoaded()
        {
            if (index != null) return;
            index = new Dictionary<int, IndexEntry>();
            filePath = Path.Combine(Application.streamingAssetsPath, "regions_roads.bin");
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("[RegionRoads] regions_roads.bin não encontrado — rode Tools/GenerateRegionRoads.py.");
                return;
            }

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    if (reader.ReadUInt32() != MagicNumber)
                    {
                        Debug.LogWarning("[RegionRoads] Magic inválido em regions_roads.bin.");
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
                Debug.Log($"[RegionRoads] Índice de malha viária carregado: {index.Count} regiões.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegionRoads] Falha ao ler índice de regions_roads.bin: {ex.Message}");
            }
        }

        public static List<RoadPolyline> GetRoads(int regionId)
        {
            EnsureLoaded();
            var result = new List<RoadPolyline>();
            if (index == null || !index.TryGetValue(regionId, out var entry)) return result;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    fs.Seek(entry.offset, SeekOrigin.Begin);
                    for (int p = 0; p < entry.polyCount; p++)
                    {
                        var poly = new RoadPolyline { roadClass = (RoadClass)reader.ReadByte() };
                        int ptCount = reader.ReadUInt16();
                        poly.points = new Vector2[ptCount];
                        for (int k = 0; k < ptCount; k++)
                        {
                            float lat = reader.ReadSingle();
                            float lon = reader.ReadSingle();
                            poly.points[k] = new Vector2(lat, lon);
                        }
                        result.Add(poly);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegionRoads] Falha ao ler bloco da região {regionId}: {ex.Message}");
            }
            return result;
        }
    }
}
