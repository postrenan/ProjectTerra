using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ProjectTerra.Planet
{
    [Serializable]
    public class RegionData
    {
        public int id;
        public string name;
        public string country;
        public string type;
        public float centerLat;
        public float centerLon;
        public float minLat;
        public float maxLat;
        public float minLon;
        public float maxLon;
        public int forestPercent;
        public int mineralsPercent;
        public int arablePercent;
        public int waterPercent;

        public int realWidthMeters;
        public int realLengthMeters;
        public int realAreaKm2;

        public float BoundingArea => (maxLat - minLat) * (maxLon - minLon);

        public bool Contains(float lat, float lon)
        {
            return lat >= minLat && lat <= maxLat && lon >= minLon && lon <= maxLon;
        }

        public float DistanceSqr(float lat, float lon)
        {
            float dLat = lat - centerLat;
            float dLon = lon - centerLon;
            return dLat * dLat + dLon * dLon;
        }
    }

    [Serializable]
    public class RegionDatabase
    {
        public List<RegionData> regions = new List<RegionData>();

        private const uint MagicNumber = 0x44525450; // 'PTRD' Little Endian ('P' | 'T'<<8 | 'R'<<16 | 'D'<<24)

        public static RegionDatabase LoadFromBinary(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fs, Encoding.UTF8))
            {
                uint magic = reader.ReadUInt32();
                if (magic != MagicNumber)
                {
                    throw new InvalidDataException($"Identificador mágico inválido no arquivo binário: {magic:X8}");
                }

                ushort version = reader.ReadUInt16();
                int count = reader.ReadInt32();

                var db = new RegionDatabase();
                db.regions = new List<RegionData>(count);

                for (int i = 0; i < count; i++)
                {
                    var r = new RegionData();
                    r.id = reader.ReadInt32();

                    ushort nameLen = reader.ReadUInt16();
                    r.name = Encoding.UTF8.GetString(reader.ReadBytes(nameLen));

                    ushort countryLen = reader.ReadUInt16();
                    r.country = Encoding.UTF8.GetString(reader.ReadBytes(countryLen));

                    ushort typeLen = reader.ReadUInt16();
                    r.type = Encoding.UTF8.GetString(reader.ReadBytes(typeLen));

                    r.centerLat = reader.ReadSingle();
                    r.centerLon = reader.ReadSingle();
                    r.minLat = reader.ReadSingle();
                    r.maxLat = reader.ReadSingle();
                    r.minLon = reader.ReadSingle();
                    r.maxLon = reader.ReadSingle();

                    r.forestPercent = reader.ReadInt32();
                    r.mineralsPercent = reader.ReadInt32();
                    r.arablePercent = reader.ReadInt32();
                    r.waterPercent = reader.ReadInt32();

                    r.realWidthMeters = reader.ReadInt32();
                    r.realLengthMeters = reader.ReadInt32();
                    r.realAreaKm2 = reader.ReadInt32();

                    db.regions.Add(r);
                }

                return db;
            }
        }

        public void SaveToBinary(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fs, Encoding.UTF8))
            {
                writer.Write(MagicNumber);
                writer.Write((ushort)1); // Version 1
                writer.Write(regions.Count);

                foreach (var r in regions)
                {
                    writer.Write(r.id);

                    byte[] nameBytes = Encoding.UTF8.GetBytes(r.name ?? "");
                    writer.Write((ushort)nameBytes.Length);
                    writer.Write(nameBytes);

                    byte[] countryBytes = Encoding.UTF8.GetBytes(r.country ?? "");
                    writer.Write((ushort)countryBytes.Length);
                    writer.Write(countryBytes);

                    byte[] typeBytes = Encoding.UTF8.GetBytes(r.type ?? "");
                    writer.Write((ushort)typeBytes.Length);
                    writer.Write(typeBytes);

                    writer.Write(r.centerLat);
                    writer.Write(r.centerLon);
                    writer.Write(r.minLat);
                    writer.Write(r.maxLat);
                    writer.Write(r.minLon);
                    writer.Write(r.maxLon);

                    writer.Write(r.forestPercent);
                    writer.Write(r.mineralsPercent);
                    writer.Write(r.arablePercent);
                    writer.Write(r.waterPercent);

                    writer.Write(r.realWidthMeters);
                    writer.Write(r.realLengthMeters);
                    writer.Write(r.realAreaKm2);
                }
            }
        }
    }
}
