using System;
using System.IO;
using UnityEngine;

namespace ProjectTerra.Planet.TerrainStreaming
{
    [Serializable]
    public class RegionalHydroData
    {
        public int regionId;
        public string name;
        public string country;
        public float minElevation;
        public float maxElevation;
        public float elevationRange;
        public bool isCoastal;
        public float seaLevelNormalized;
        public int waterPercent;
        public bool hasRivers;
        public int realWidthMeters;
        public int realLengthMeters;
        public int realAreaKm2;
    }

    /// <summary>
    /// Carregador de alta performance para os datasets topográficos (16-bit RAW) e hidrográficos (JSON)
    /// gerados para cada estado/província do planeta em StreamingAssets/GeographicData.
    /// </summary>
    public static class GeographicDataLoader
    {
        private const int Resolution = 129; // 129x129 grid (TerrainData standard)

        public static bool LoadRegionalHeightmap(int regionId, out float[,] heights, out RegionalHydroData hydro)
        {
            heights = new float[Resolution, Resolution];
            hydro = null;

            string geoDir = Path.Combine(Application.streamingAssetsPath, "GeographicData");
            string rawPath = Path.Combine(geoDir, $"heightmap_{regionId}.raw");
            string jsonPath = Path.Combine(geoDir, $"hydro_{regionId}.json");

            // Carregar Dataset Binário Unificado
            if (File.Exists(rawPath))
            {
                try
                {
                    byte[] rawBytes = File.ReadAllBytes(rawPath);

                    // Formato Binário Unificado (Magic: 'HMAP')
                    if (rawBytes.Length > 36 && rawBytes[0] == (byte)'H' && rawBytes[1] == (byte)'M' && rawBytes[2] == (byte)'A' && rawBytes[3] == (byte)'P')
                    {
                        using (var ms = new MemoryStream(rawBytes))
                        using (var reader = new BinaryReader(ms, System.Text.Encoding.UTF8))
                        {
                            reader.ReadUInt32(); // Pular Magic
                            ushort version = reader.ReadUInt16();
                            ushort headerSize = reader.ReadUInt16();
                            ushort res = reader.ReadUInt16();

                            hydro = new RegionalHydroData
                            {
                                regionId = reader.ReadInt32(),
                                minElevation = reader.ReadSingle(),
                                maxElevation = reader.ReadSingle(),
                                elevationRange = reader.ReadSingle(),
                                seaLevelNormalized = reader.ReadSingle(),
                                isCoastal = reader.ReadBoolean(),
                                hasRivers = reader.ReadBoolean(),
                                waterPercent = reader.ReadInt16(),
                                realWidthMeters = reader.ReadInt32(),
                                realLengthMeters = reader.ReadInt32(),
                                realAreaKm2 = reader.ReadInt32()
                            };

                            ushort nameLen = reader.ReadUInt16();
                            hydro.name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLen));

                            ushort countryLen = reader.ReadUInt16();
                            hydro.country = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(countryLen));

                            // Resolução vem do cabeçalho (varia por região, escala do ETOPO).
                            // O magic HMAP casa, então este ficheiro TEM cabeçalho: se o
                            // tamanho não fechar, o ficheiro está truncado/incompleto e
                            // NÃO é um RAW puro. Sem um return/else aqui o código caía no
                            // fallback legado, que relia os MESMOS bytes a partir do offset 0
                            // — ou seja, o magic, os floats de elevação e o nome UTF-8 da
                            // região viravam ushort de altura — e devolvia true, o que
                            // desligava o ProceduralFallback e pintava o setor com espetos.
                            if (res < 2 || rawBytes.Length < headerSize + res * res * 2)
                            {
                                Debug.LogWarning($"[GeographicDataLoader] heightmap_{regionId}.raw tem cabeçalho HMAP mas está truncado (res={res}, {rawBytes.Length} bytes). A usar fallback procedural.");
                                GenerateProceduralFallback(out heights);
                                return false;
                            }

                            {
                                heights = new float[res, res];
                                int byteIdx = headerSize;
                                for (int y = 0; y < res; y++)
                                {
                                    for (int x = 0; x < res; x++)
                                    {
                                        ushort rawVal = (ushort)(rawBytes[byteIdx] | (rawBytes[byteIdx + 1] << 8));
                                        heights[y, x] = rawVal / 65535.0f;
                                        byteIdx += 2;
                                    }
                                }
                                return true;
                            }
                        }
                    }

                    // Fallback legada para arquivos RAW puros sem cabeçalho
                    if (File.Exists(jsonPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(jsonPath);
                            hydro = JsonUtility.FromJson<RegionalHydroData>(json);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[GeographicDataLoader] Falha ao ler metadados hidrográficos legados {jsonPath}: {ex.Message}");
                        }
                    }

                    if (rawBytes.Length >= Resolution * Resolution * 2)
                    {
                        int byteIdx = 0;
                        for (int y = 0; y < Resolution; y++)
                        {
                            for (int x = 0; x < Resolution; x++)
                            {
                                ushort rawVal = (ushort)(rawBytes[byteIdx] | (rawBytes[byteIdx + 1] << 8));
                                heights[y, x] = rawVal / 65535.0f;
                                byteIdx += 2;
                            }
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GeographicDataLoader] Falha ao carregar dataset binário {rawPath}: {ex.Message}");
                }
            }

            // Fallback sintético procedural caso a região específica ainda não esteja gerada
            GenerateProceduralFallback(out heights);
            return false;
        }

        private static void GenerateProceduralFallback(out float[,] heights)
        {
            heights = new float[Resolution, Resolution];
            for (int y = 0; y < Resolution; y++)
            {
                float ny = y / (float)(Resolution - 1);
                for (int x = 0; x < Resolution; x++)
                {
                    float nx = x / (float)(Resolution - 1);
                    float h = Mathf.Sin(nx * Mathf.PI * 2.2f) * Mathf.Cos(ny * Mathf.PI * 2.0f) * 0.35f + 0.5f;
                    heights[y, x] = Mathf.Clamp01(h);
                }
            }
        }
    }
}
