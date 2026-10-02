using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Banco de dados de carregamento de alta performance para a infraestrutura de todas as 4.838 regiões do globo.
    /// Carrega do arquivo binário otimizado StreamingAssets/regions_infrastructure.bin em milissegundos
    /// e possui avaliador geográfico algorítmico de contingência para regiões dinâmicas e oceânicas.
    /// </summary>
    public static class RegionInfrastructureDatabase
    {
        private const uint MagicNumber = 0x4E495450; // 'PTIN' Little Endian
        private static Dictionary<int, RegionInfrastructure> cache = new Dictionary<int, RegionInfrastructure>();
        private static bool isLoaded = false;

        public static void EnsureLoaded()
        {
            if (isLoaded && cache.Count > 0) return;

            string binPath = Path.Combine(Application.streamingAssetsPath, "regions_infrastructure.bin");
            string jsonPath = Path.Combine(Application.streamingAssetsPath, "regions_infrastructure.json");

            if (File.Exists(binPath))
            {
                try
                {
                    using (var fs = new FileStream(binPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new BinaryReader(fs, Encoding.UTF8))
                    {
                        uint magic = reader.ReadUInt32();
                        if (magic == MagicNumber)
                        {
                            ushort version = reader.ReadUInt16();
                            int count = reader.ReadInt32();

                            cache = new Dictionary<int, RegionInfrastructure>(count);

                            for (int i = 0; i < count; i++)
                            {
                                var item = new RegionInfrastructure();
                                item.regionId = reader.ReadInt32();

                                byte b0 = reader.ReadByte();
                                byte b1 = reader.ReadByte();
                                byte b2 = reader.ReadByte();
                                byte b3 = reader.ReadByte();
                                byte b4 = reader.ReadByte();
                                byte b5 = reader.ReadByte();
                                byte b6 = reader.ReadByte();
                                byte b7 = reader.ReadByte();
                                byte b8 = reader.ReadByte();
                                byte b9 = reader.ReadByte();

                                item.hasCoastline = b0 == 1;
                                item.hasRivers = b1 == 1;
                                item.hasLakes = b2 == 1;
                                item.hasPort = b3 == 1;
                                item.hasAirport = b4 == 1;
                                item.hasRoadNetwork = b5 == 1;
                                item.isAgricultureViable = b6 == 1;
                                item.isFishingViable = b7 == 1;
                                item.isAviationViable = b8 == 1;
                                item.isTruckingViable = b9 == 1;

                                item.waterBodyName = ReadLengthPrefixedString(reader);
                                item.portName = ReadLengthPrefixedString(reader);
                                item.airportName = ReadLengthPrefixedString(reader);
                                item.environmentCategory = ReadLengthPrefixedString(reader);
                                item.agricultureReason = ReadLengthPrefixedString(reader);
                                item.fishingReason = ReadLengthPrefixedString(reader);
                                item.aviationReason = ReadLengthPrefixedString(reader);
                                item.truckingReason = ReadLengthPrefixedString(reader);

                                cache[item.regionId] = item;
                            }

                            isLoaded = true;
                            Debug.Log($"[RegionInfrastructure] Carregada base binária de infraestrutura com {cache.Count} territórios.");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RegionInfrastructure] Falha ao carregar regions_infrastructure.bin: {ex.Message}");
                }
            }

            // Fallback JSON se binário não existir
            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    var wrapper = JsonUtility.FromJson<RegionInfrastructureCollection>(json);
                    if (wrapper != null && wrapper.regions != null)
                    {
                        cache = new Dictionary<int, RegionInfrastructure>(wrapper.regions.Count);
                        foreach (var item in wrapper.regions)
                        {
                            cache[item.regionId] = item;
                        }
                        isLoaded = true;
                        Debug.Log($"[RegionInfrastructure] Carregada base JSON de infraestrutura com {cache.Count} territórios.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RegionInfrastructure] Falha ao carregar regions_infrastructure.json: {ex.Message}");
                }
            }
        }

        private static string ReadLengthPrefixedString(BinaryReader reader)
        {
            ushort len = reader.ReadUInt16();
            if (len == 0) return string.Empty;
            byte[] bytes = reader.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        public static RegionInfrastructure GetInfrastructure(int regionId, RegionData fallbackRegion = null)
        {
            EnsureLoaded();

            if (cache.TryGetValue(regionId, out var infra))
            {
                if (fallbackRegion != null)
                {
                    infra.regionName = fallbackRegion.name;
                    infra.countryName = fallbackRegion.country;
                }
                return infra;
            }

            // Se for região marítima ou não catalogada, avaliar proceduralmente
            if (fallbackRegion != null)
            {
                var generated = EvaluateProcedural(fallbackRegion);
                cache[regionId] = generated;
                return generated;
            }

            return CreateGenericLandInfrastructure(regionId);
        }

        public static RegionInfrastructure EvaluateProcedural(RegionData region)
        {
            var infra = new RegionInfrastructure
            {
                regionId = region.id,
                regionName = region.name ?? "Região",
                countryName = region.country ?? "Território"
            };

            // Região Oceânica (ID 0)
            if (region.id == 0 || region.type == "Zona Marítima")
            {
                infra.hasCoastline = true;
                infra.hasRivers = false;
                infra.hasLakes = false;
                infra.waterBodyName = region.name;
                infra.hasPort = true;
                infra.portName = "Terminal Flutuante / Plataforma Marítima";
                infra.hasAirport = false;
                infra.airportName = "Heliponto Marítimo";
                infra.hasRoadNetwork = false;

                infra.isAgricultureViable = false;
                infra.agricultureReason = "Águas oceânicas abertas sem solo cultivável terrestre.";

                infra.isFishingViable = true;
                infra.fishingReason = "Pesca oceânica em alto-mar plenamente autorizada.";

                infra.isAviationViable = false;
                infra.aviationReason = "Águas abertas sem pista de pouso terrestre para aviões de carga.";

                infra.isTruckingViable = false;
                infra.truckingReason = "Zona marítima sem malha viária rodoviária contínua.";

                infra.environmentCategory = "Águas Oceânicas Globais";
                return infra;
            }

            float lat = region.centerLat;
            float lon = region.centerLon;
            string country = region.country ?? "";
            string name = region.name ?? "";

            // Detecção Geográfica do Deserto do Saara
            bool isSahara = (country == "Algeria" || country == "Libya" || country == "Chad" || country == "Niger" ||
                             country == "Mali" || country == "Mauritania" || country == "Sudan" || country == "Egypt" || country == "Western Sahara")
                            && (lat >= 14f && lat <= 32f) && (lon >= -17f && lon <= 35f);

            // Detecção da Cordilheira dos Andes
            bool isAndes = (country == "Bolivia" || country == "Peru" || country == "Ecuador" ||
                            country == "Colombia" || country == "Argentina" || country == "Chile")
                           && (lat >= -55f && lat <= 11f) && (lon >= -80f && lon <= -64f);

            // Detecção de Extremos Polares
            bool isPolar = (country.ToLower().Contains("antarctica") || name.ToLower().Contains("antarctica") || lat < -60f ||
                           (country.ToLower().Contains("greenland") && lat > 66f) || Mathf.Abs(lat) > 76f);

            infra.hasCoastline = region.waterPercent >= 35;
            infra.hasRivers = region.waterPercent >= 20 && !isSahara && !isAndes;
            infra.hasLakes = region.waterPercent >= 30;
            infra.hasPort = infra.hasCoastline || infra.hasRivers || infra.hasLakes;
            infra.hasAirport = !isPolar;
            infra.hasRoadNetwork = !isPolar;

            // 1. Agricultura: Viável em quase todas as regiões exceto extremos
            if (isPolar)
            {
                infra.isAgricultureViable = false;
                infra.agricultureReason = "Calota glacial polar permanente com permafrost congelado.";
            }
            else if (isSahara && region.waterPercent < 15 && region.arablePercent < 15)
            {
                infra.isAgricultureViable = false;
                infra.agricultureReason = "Dunas hiperáridas do Deserto do Saara sem recursos hídricos para irrigação.";
            }
            else
            {
                infra.isAgricultureViable = true;
                infra.agricultureReason = "Solos cultiváveis e regime climático favoráveis à atividade agrícola.";
            }

            // 2. Pesca: Inviável no Saara ou nos Andes (salvo com costa/lagos confirmados)
            if (isSahara)
            {
                infra.isFishingViable = false;
                infra.fishingReason = "Indisponível no Deserto do Saara: Região árida sem litoral, rios ou lagos navegáveis.";
            }
            else if (isAndes && !infra.hasCoastline && !infra.hasLakes)
            {
                infra.isFishingViable = false;
                infra.fishingReason = "Indisponível na Cordilheira dos Andes: Relevo montanhoso de alta altitude sem corpos hídricos navegáveis.";
            }
            else if (!infra.hasCoastline && !infra.hasRivers && !infra.hasLakes)
            {
                infra.isFishingViable = false;
                infra.fishingReason = "Indisponível: Província interior sem acesso a litoral marítimo, rios ou lagos navegáveis.";
            }
            else
            {
                infra.isFishingViable = true;
                infra.fishingReason = "Corpos hídricos navegáveis disponíveis para pesca comercial.";
            }

            // 3. Aviação
            infra.isAviationViable = !isPolar;
            infra.aviationReason = infra.isAviationViable ? "Aeródromo regional pavimentado em operação." : "Inviável na calota polar extrema sem aeródromo.";

            // 4. Rodoviário
            infra.isTruckingViable = !isPolar;
            infra.truckingReason = infra.isTruckingViable ? "Malha rodoviária pavimentada intermunicipal ativa." : "Ausência de malha rodoviária sobre a calota polar.";

            infra.waterBodyName = infra.hasCoastline ? "Costa Oceânica" : (infra.hasRivers ? "Bacia Hidrográfica" : (infra.hasLakes ? "Lago Regional" : "Sem corpo hídrico"));
            infra.portName = infra.hasCoastline ? "Terminal Portuário Marítimo" : (infra.isFishingViable ? "Entreposto Fluvial" : "Sem Instalação Portuária");
            infra.airportName = infra.isAviationViable ? "Aeródromo Regional Pavimentado" : "Sem Pista";

            if (isPolar) infra.environmentCategory = "Gelo Polar & Tundra Ártica";
            else if (isSahara) infra.environmentCategory = "Deserto do Saara";
            else if (isAndes) infra.environmentCategory = "Cordilheira dos Andes";
            else if (infra.hasCoastline) infra.environmentCategory = "Zona Costeira Litorânea";
            else infra.environmentCategory = "Planície Continental";

            return infra;
        }

        private static RegionInfrastructure CreateGenericLandInfrastructure(int regionId)
        {
            return new RegionInfrastructure
            {
                regionId = regionId,
                regionName = "Território Regional",
                countryName = "País",
                hasCoastline = true,
                hasRivers = true,
                hasLakes = false,
                hasPort = true,
                portName = "Porto Regional",
                hasAirport = true,
                airportName = "Aeroporto Regional",
                hasRoadNetwork = true,
                isAgricultureViable = true,
                agricultureReason = "Terras aráveis disponíveis para cultivo agrícola.",
                isFishingViable = true,
                fishingReason = "Águas costeiras e estuarinas abertas para atividade pesqueira.",
                isAviationViable = true,
                aviationReason = "Aeródromo e pista pavimentada operacionais.",
                isTruckingViable = true,
                truckingReason = "Malha rodoviária asfaltada interligada.",
                environmentCategory = "Zona Continental Padrão"
            };
        }
    }
}
