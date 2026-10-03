using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Uma cidade real (localidade povoada do Natural Earth) catalogada numa região.
    /// As até 10 maiores cidades de cada estado/província formam a base econômica da região.
    /// </summary>
    [Serializable]
    public class RegionCity
    {
        public string name;
        public float lat;
        public float lon;
        public int population;
        public int rank;            // 0 = maior cidade da região
        public bool isCapital;
        public float economicWeight; // 0.1..1.0 — peso relativo na economia regional
    }

    /// <summary>
    /// Banco de dados das 10 maiores cidades de cada região (StreamingAssets/regions_cities.bin, magic 'PTCY').
    /// Carregado por completo em memória (arquivo pequeno) e indexado por regionId.
    /// </summary>
    public static class RegionCitiesDatabase
    {
        private const uint MagicNumber = 0x59435450; // 'PTCY' little-endian
        private static Dictionary<int, List<RegionCity>> cache;

        public static void EnsureLoaded()
        {
            if (cache != null) return;
            cache = new Dictionary<int, List<RegionCity>>();

            string binPath = Path.Combine(Application.streamingAssetsPath, "regions_cities.bin");
            if (!File.Exists(binPath))
            {
                Debug.LogWarning("[RegionCities] regions_cities.bin não encontrado — rode Tools/GenerateRegionCities.py.");
                return;
            }

            try
            {
                using (var fs = new FileStream(binPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new BinaryReader(fs, Encoding.UTF8))
                {
                    if (reader.ReadUInt32() != MagicNumber)
                    {
                        Debug.LogWarning("[RegionCities] Magic inválido em regions_cities.bin.");
                        return;
                    }
                    reader.ReadUInt16(); // version
                    int regionCount = reader.ReadInt32();
                    for (int i = 0; i < regionCount; i++)
                    {
                        int regionId = reader.ReadInt32();
                        int cityCount = reader.ReadUInt16();
                        var list = new List<RegionCity>(cityCount);
                        for (int c = 0; c < cityCount; c++)
                        {
                            var city = new RegionCity();
                            ushort len = reader.ReadUInt16();
                            city.name = len > 0 ? Encoding.UTF8.GetString(reader.ReadBytes(len)) : string.Empty;
                            city.lat = reader.ReadSingle();
                            city.lon = reader.ReadSingle();
                            city.population = reader.ReadInt32();
                            city.rank = reader.ReadByte();
                            city.isCapital = reader.ReadByte() == 1;
                            city.economicWeight = reader.ReadSingle();
                            list.Add(city);
                        }
                        cache[regionId] = list;
                    }
                }
                Debug.Log($"[RegionCities] Base de cidades carregada: {cache.Count} regiões.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RegionCities] Falha ao carregar regions_cities.bin: {ex.Message}");
            }
        }

        public static List<RegionCity> GetCities(int regionId)
        {
            EnsureLoaded();
            return cache.TryGetValue(regionId, out var list) ? list : new List<RegionCity>();
        }
    }
}
