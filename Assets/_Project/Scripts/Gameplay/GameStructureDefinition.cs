using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Identificador das 27 estruturas comerciais, industriais e de apoio
    /// organizadas pelas 4 categorias de jogadores: Fazendeiro, Motorista, Pescador e Piloto.
    /// </summary>
    public enum GameStructureType
    {
        // ==========================================
        // 1. FAZENDEIRO (Agricultura, Pecuária e Silvicultura)
        // ==========================================
        // Maquinário e Suprimentos
        FarmVehicleDealership = 101,     // Concessionária de Veículos Agrícolas
        InputSupplyStation = 102,        // Estação de Insumos (Silos/Tanques granel)

        // Comércio de Matéria-Prima
        GrainElevatorCoop = 103,         // Elevadores de Grãos e Cooperativas
        LogisticsTrainStation = 104,     // Estação de Trem (exportação granel)
        LivestockMarket = 105,           // Comerciante de Animais
        SawmillBiomassPlant = 106,       // Serraria e Usina de Biomassa
        BaleSellPoint = 107,             // Ponto de Venda de Fardos

        // Processamento e Indústria
        DairyAndSpinningMill = 108,      // Laticínios e Fiação
        GrainAndOilMills = 109,          // Moinhos (Grãos e Óleo)
        SugarAndBiogasPlants = 110,      // Usinas (Açúcar e Biogás)
        CarpentryAndBakery = 111,        // Carpintaria e Padaria

        // Varejo
        FarmersMarketSupermarket = 112,  // Mercado de Fazendeiros e Supermercados

        // ==========================================
        // 2. MOTORISTA (Logística e Transporte Rodoviário)
        // ==========================================
        // Maquinário e Suprimentos
        TruckDealership = 201,           // Concessionária de Caminhões
        HighwayTruckStop = 202,          // Posto de Estrada (Truck Stop)
        HeavyRepairWorkshop = 203,       // Oficina Especializada (Borracharia/Molas)

        // Contratos e Hubs Logísticos
        CrossDockingHub = 204,           // Centro de Distribuição (Cross-Docking)
        DryPortContainerTerminal = 205,  // Porto Seco / Terminal de Contêineres
        QuarryConstructionYard = 206,    // Pátio de Obras / Pedreira

        // ==========================================
        // 3. PESCADOR (Extração e Aquicultura)
        // ==========================================
        // Maquinário e Suprimentos
        ShipyardMarina = 301,            // Estaleiro e Marina
        NauticalSupplyShop = 302,        // Loja de Artigos Náuticos
        IceFactory = 303,                // Fábrica de Gelo

        // Comércio e Indústria
        FishMarket = 304,                // Mercado de Peixes
        CanningFactory = 305,            // Fábrica de Enlatados
        FishMealPlant = 306,             // Indústria de Farinha de Peixe

        // Varejo
        SeafoodRestaurant = 307,         // Restaurantes de Frutos do Mar

        // ==========================================
        // 4. PILOTO (Aviação Civil, Carga e Agro)
        // ==========================================
        // Maquinário e Suprimentos
        AviationHangarSales = 401,       // Hangar de Vendas & Customização
        FBO_AirServices = 402,           // FBO (Fixed-Base Operator)
        FlyingClub = 403,                // Aeroclube Regional

        // Contratos e Terminais
        PassengerAirTerminal = 404,      // Terminal de Passageiros (Táxi Aéreo)
        AirCargoTerminal = 405,          // Terminal de Cargas Aéreas

        // Serviços Especializados
        AgroSprayingBase = 406,          // Base de Pulverização Aérea
        RescueHelipadHospital = 407      // Heliponto de Resgate / Hospital
    }

    /// <summary>
    /// Grupo funcional da estrutura.
    /// </summary>
    public enum StructureGroup
    {
        MachineryAndSupplies,  // Maquinário e Suprimentos
        RawMaterialTrade,      // Comércio de Matéria-Prima
        ProcessingIndustry,    // Processamento e Indústria
        RetailTrade,           // Varejo
        LogisticsHubs,         // Contratos e Hubs Logísticos
        SpecializedServices    // Serviços Especializados
    }

    /// <summary>
    /// Serviços que uma estrutura pode prestar ao jogador.
    /// </summary>
    [Flags]
    public enum StructureServices
    {
        None = 0,
        VehicleSales = 1 << 0,       // Compra/aluguel de veículos
        VehicleRepair = 1 << 1,      // Oficina / manutenção / borracharia
        FuelRefuel = 1 << 2,         // Abastecimento de combustível
        SuppliesStore = 1 << 3,      // Sementes, fertilizantes, ração, kits náuticos, gelo
        CargoSell = 1 << 4,          // Compra de safras, fardos, toras, pescados, minérios
        IndustrialProcessing = 1 << 5, // Transformação industrial agregada
        RetailSales = 1 << 6,        // Venda final no varejo
        LogisticsContracts = 1 << 7, // Fretes fechados, táxi aéreo, contêineres, obras
        FlightSchool = 1 << 8,       // Licenças e instrução de voo
        LivestockTrading = 1 << 9    // Compra e venda de animais vivos
    }

    /// <summary>
    /// Definição completa e metadados de uma estrutura do jogo, incluindo
    /// regras de validação geográfica para garantir fidelidade ao bioma e região.
    /// </summary>
    [Serializable]
    public class GameStructureDefinition
    {
        public GameStructureType type;
        public string title;
        public string shortName;
        public string description;
        public StarterCareer targetCareer;
        public StructureGroup group;
        public StructureServices services;
        public Color themeColor;
        public string iconSymbol;

        // --- Critérios de Validação Geográfica & Bioma ---
        public bool requiresCoastline;    // Exige litoral marítimo (ex: estaleiro oceânico)
        public bool requiresRivers;       // Exige rios navegáveis (ex: porto fluvial)
        public bool requiresWaterBody;    // Exige qualquer corpo d'água (litoral, rios ou lagos)
        public bool forbiddenInDesert;    // Proibido em desertos e zonas hiperáridas (ex: porto fluvial no deserto)
        public bool requiresAgriculture;  // Exige relevo e clima favoráveis à agricultura
        public bool requiresForest;       // Exige cobertura vegetal/florestal (silvicultura)
        public bool requiresMinerals;     // Exige jazidas minerais ou relevo montanhoso
        public bool requiresRoadNetwork;  // Exige malha rodoviária viável
        public bool requiresRailways;     // Exige conexão ferroviária / intermodal
        public bool requiresAviation;     // Exige viabilidade de aviação
        public bool forbiddenInPolar;     // Proibido em extremos polares / geleiras

        public GameStructureDefinition(
            GameStructureType type,
            string title,
            string shortName,
            string description,
            StarterCareer targetCareer,
            StructureGroup group,
            StructureServices services,
            Color themeColor,
            string iconSymbol)
        {
            this.type = type;
            this.title = title;
            this.shortName = shortName;
            this.description = description;
            this.targetCareer = targetCareer;
            this.group = group;
            this.services = services;
            this.themeColor = themeColor;
            this.iconSymbol = iconSymbol;
        }

        /// <summary>
        /// Valida se esta estrutura pode existir na região indicada,
        /// respeitando rigorosamente o bioma, relevo e recursos hídricos.
        /// Exemplo clássico proibido: porto fluvial ou estaleiro no Deserto do Saara.
        /// </summary>
        public bool IsCompatibleWithRegion(RegionData region, RegionInfrastructure infra)
        {
            if (region == null) return true;

            float lat = region.centerLat;
            float lon = region.centerLon;
            string country = region.country ?? "";
            string name = region.name ?? "";

            // 1. Detecção Geográfica de Desertos Hiperáridos (Saara, Arábia, Gobi, etc.)
            bool isSahara = (country == "Algeria" || country == "Libya" || country == "Chad" || country == "Niger" ||
                             country == "Mali" || country == "Mauritania" || country == "Sudan" || country == "Egypt" || country == "Western Sahara")
                            && (lat >= 14f && lat <= 32f) && (lon >= -17f && lon <= 35f);

            bool isDesert = isSahara || (region.waterPercent < 5 && region.forestPercent < 5 && region.arablePercent < 10);

            // 2. Detecção de Extremos Polares
            bool isPolar = (country.ToLower().Contains("antarctica") || name.ToLower().Contains("antarctica") || lat < -60f ||
                           (country.ToLower().Contains("greenland") && lat > 66f) || Mathf.Abs(lat) > 76f);

            // 3. Hidrografia real da região
            bool hasCoast = (infra != null && infra.hasCoastline) || region.waterPercent >= 35;
            bool hasRivers = (infra != null && infra.hasRivers) || (region.waterPercent >= 18 && !isDesert);
            bool hasLakes = (infra != null && infra.hasLakes) || region.waterPercent >= 25;
            bool hasWater = hasCoast || hasRivers || hasLakes || region.waterPercent > 8;

            // REGRA FUNDAMENTAL: Nada fluvial, marítimo ou pesqueiro no deserto ou regiões áridas sem corpos hídricos!
            if (forbiddenInDesert && isDesert && !hasCoast)
            {
                return false;
            }

            if (requiresCoastline && !hasCoast)
            {
                return false;
            }

            if (requiresRivers && (!hasRivers || isDesert))
            {
                return false;
            }

            if (requiresWaterBody && !hasWater)
            {
                return false;
            }

            // REGRA POLAR: Calotas de gelo permanentes não comportam lavoura a céu aberto nem silvicultura
            if (forbiddenInPolar && isPolar)
            {
                return false;
            }

            // REGRA DE AGRICULTURA: Solos aráveis e clima viável
            if (requiresAgriculture)
            {
                if (infra != null && !infra.isAgricultureViable) return false;
                if (isPolar) return false;
                if (isDesert && region.waterPercent < 15 && region.arablePercent < 15) return false;
            }

            // REGRA DE SILVICULTURA: Exige floresta representativa
            if (requiresForest)
            {
                if (region.forestPercent < 12) return false;
                if (isDesert || isPolar) return false;
            }

            // REGRA DE MINERAÇÃO: Exige ocorrência mineral ou relevo rochoso
            if (requiresMinerals)
            {
                if (region.mineralsPercent < 8 && region.forestPercent > 70) return false;
            }

            // REGRA DE AVIAÇÃO
            if (requiresAviation)
            {
                if (infra != null && !infra.isAviationViable) return false;
            }

            // REGRA DE RODOVIA
            if (requiresRoadNetwork)
            {
                if (infra != null && !infra.hasRoadNetwork && isPolar) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Catálogo mestre contendo as definições de todas as 27 estruturas solicitadas.
    /// </summary>
    public static class GameStructureCatalog
    {
        private static Dictionary<GameStructureType, GameStructureDefinition> catalog;

        public static GameStructureDefinition Get(GameStructureType type)
        {
            EnsureCatalog();
            return catalog.TryGetValue(type, out var def) ? def : null;
        }

        public static IEnumerable<GameStructureDefinition> GetAll()
        {
            EnsureCatalog();
            return catalog.Values;
        }

        public static List<GameStructureDefinition> GetByCareer(StarterCareer career)
        {
            EnsureCatalog();
            var list = new List<GameStructureDefinition>();
            foreach (var def in catalog.Values)
            {
                if (def.targetCareer == career) list.Add(def);
            }
            return list;
        }

        private static void EnsureCatalog()
        {
            if (catalog != null) return;

            catalog = new Dictionary<GameStructureType, GameStructureDefinition>();

            // =========================================================================
            // 1. FAZENDEIRO (Agricultura, Pecuária e Silvicultura)
            // =========================================================================
            Register(new GameStructureDefinition(
                GameStructureType.FarmVehicleDealership,
                "Concessionária de Veículos Agrícolas",
                "Conc. Agrícola",
                "Compra, aluguel, manutenção e customização de tratores, colheitadeiras e implementos agrícolas.",
                StarterCareer.Farmer,
                StructureGroup.MachineryAndSupplies,
                StructureServices.VehicleSales | StructureServices.VehicleRepair,
                new Color(0.25f, 0.72f, 0.28f),
                "🚜"
            ) { requiresAgriculture = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.InputSupplyStation,
                "Estação de Insumos & Silos",
                "Estação Insumos",
                "Silos e tanques para compra a granel de sementes, fertilizantes, calcário, herbicida e ração.",
                StarterCareer.Farmer,
                StructureGroup.MachineryAndSupplies,
                StructureServices.SuppliesStore,
                new Color(0.40f, 0.75f, 0.25f),
                "🌱"
            ) { requiresAgriculture = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.GrainElevatorCoop,
                "Elevadores de Grãos e Cooperativas",
                "Cooperativa Grãos",
                "Grandes complexos que compram colheitas a granel (trigo, milho, soja, etc.).",
                StarterCareer.Farmer,
                StructureGroup.RawMaterialTrade,
                StructureServices.CargoSell,
                new Color(0.85f, 0.65f, 0.15f),
                "🌾"
            ) { requiresAgriculture = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.LogisticsTrainStation,
                "Estação Ferroviária de Carga",
                "Estação de Trem",
                "Ponto logístico para exportação de grãos e grandes volumes em troca de melhores preços.",
                StarterCareer.Farmer,
                StructureGroup.RawMaterialTrade,
                StructureServices.CargoSell | StructureServices.LogisticsContracts,
                new Color(0.35f, 0.55f, 0.65f),
                "🚂"
            ) { requiresRailways = true, requiresRoadNetwork = true });

            Register(new GameStructureDefinition(
                GameStructureType.LivestockMarket,
                "Comerciante de Animais",
                "Mercado Pecuário",
                "Compra e venda de gado, ovelhas, porcos e cavalos para propriedades rurais.",
                StarterCareer.Farmer,
                StructureGroup.RawMaterialTrade,
                StructureServices.LivestockTrading,
                new Color(0.70f, 0.45f, 0.25f),
                "🐂"
            ) { requiresAgriculture = true, forbiddenInDesert = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.SawmillBiomassPlant,
                "Serraria e Usina de Biomassa",
                "Serraria & Biomassa",
                "Compra de toras de madeira de reflorestamento e lascas de madeira (woodchips).",
                StarterCareer.Farmer,
                StructureGroup.RawMaterialTrade,
                StructureServices.CargoSell | StructureServices.IndustrialProcessing,
                new Color(0.55f, 0.38f, 0.22f),
                "🪵"
            ) { requiresForest = true, forbiddenInDesert = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.BaleSellPoint,
                "Ponto de Venda de Fardos",
                "Celeiro de Fardos",
                "Celeiros que compram fardos de feno, grama e palha a excelentes cotações.",
                StarterCareer.Farmer,
                StructureGroup.RawMaterialTrade,
                StructureServices.CargoSell,
                new Color(0.80f, 0.72f, 0.20f),
                "📦"
            ) { requiresAgriculture = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.DairyAndSpinningMill,
                "Laticínios e Fiação",
                "Laticínios & Têxtil",
                "Transformam leite em queijo/manteiga e lã/algodão em tecidos de alta qualidade.",
                StarterCareer.Farmer,
                StructureGroup.ProcessingIndustry,
                StructureServices.IndustrialProcessing | StructureServices.CargoSell,
                new Color(0.92f, 0.88f, 0.78f),
                "🧀"
            ) { requiresAgriculture = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.GrainAndOilMills,
                "Moinhos (Grãos e Óleo)",
                "Moinhos Centrais",
                "Transformam grãos em farinha refinada e sementes oleaginosas em óleos vegetais.",
                StarterCareer.Farmer,
                StructureGroup.ProcessingIndustry,
                StructureServices.IndustrialProcessing | StructureServices.CargoSell,
                new Color(0.78f, 0.65f, 0.45f),
                "⚙️"
            ) { requiresAgriculture = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.SugarAndBiogasPlants,
                "Usinas (Açúcar e Biogás)",
                "Usina Açúcar/Biogás",
                "Processam beterraba/cana em açúcar e transformam resíduos biológicos em energia e biofertilizante.",
                StarterCareer.Farmer,
                StructureGroup.ProcessingIndustry,
                StructureServices.IndustrialProcessing | StructureServices.CargoSell,
                new Color(0.30f, 0.65f, 0.45f),
                "🏭"
            ) { requiresAgriculture = true, forbiddenInDesert = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.CarpentryAndBakery,
                "Carpintaria e Padaria Artesanal",
                "Carpintaria & Padaria",
                "Indústrias de bens de consumo que fabricam móveis, pães e bolos a partir de matérias-primas processadas.",
                StarterCareer.Farmer,
                StructureGroup.ProcessingIndustry,
                StructureServices.IndustrialProcessing | StructureServices.RetailSales,
                new Color(0.85f, 0.55f, 0.35f),
                "🍞"
            ) { forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.FarmersMarketSupermarket,
                "Mercado de Fazendeiros e Supermercados",
                "Mercado & Super",
                "Pontos de venda final para produtos de estufa (tomate, alface), mel, ovos e produtos industrializados.",
                StarterCareer.Farmer,
                StructureGroup.RetailTrade,
                StructureServices.RetailSales | StructureServices.CargoSell,
                new Color(0.35f, 0.78f, 0.50f),
                "🛒"
            ) { forbiddenInPolar = true });

            // =========================================================================
            // 2. MOTORISTA (Logística e Transporte Rodoviário)
            // =========================================================================
            Register(new GameStructureDefinition(
                GameStructureType.TruckDealership,
                "Concessionária de Caminhões",
                "Conc. Caminhões",
                "Compra, aluguel e customização de cavalos mecânicos, carretas graneleiras, baús, pranchas e tanques.",
                StarterCareer.Trucker,
                StructureGroup.MachineryAndSupplies,
                StructureServices.VehicleSales | StructureServices.VehicleRepair,
                new Color(0.88f, 0.45f, 0.15f),
                "🚛"
            ) { requiresRoadNetwork = true });

            Register(new GameStructureDefinition(
                GameStructureType.HighwayTruckStop,
                "Posto de Estrada (Truck Stop)",
                "Truck Stop Rodoviário",
                "Abastecimento de Diesel de alta vazão, descanso do motorista, compra de suprimentos e pequenas manutenções.",
                StarterCareer.Trucker,
                StructureGroup.MachineryAndSupplies,
                StructureServices.FuelRefuel | StructureServices.SuppliesStore | StructureServices.VehicleRepair,
                new Color(0.95f, 0.35f, 0.20f),
                "⛽"
            ) { requiresRoadNetwork = true });

            Register(new GameStructureDefinition(
                GameStructureType.HeavyRepairWorkshop,
                "Oficina Especializada (Borracharia/Molas)",
                "Borracharia & Molas",
                "Manutenção pesada de desgaste rodoviário, alinhamento de suspensão, molas e pneus.",
                StarterCareer.Trucker,
                StructureGroup.MachineryAndSupplies,
                StructureServices.VehicleRepair,
                new Color(0.45f, 0.50f, 0.58f),
                "🔧"
            ) { requiresRoadNetwork = true });

            Register(new GameStructureDefinition(
                GameStructureType.CrossDockingHub,
                "Centro de Distribuição (Cross-Docking)",
                "Hub Cross-Docking",
                "Galpões logísticos para coleta de contratos de frete fechado (baús de carga geral e frigoríficos).",
                StarterCareer.Trucker,
                StructureGroup.LogisticsHubs,
                StructureServices.LogisticsContracts,
                new Color(0.20f, 0.50f, 0.85f),
                "🏢"
            ) { requiresRoadNetwork = true });

            Register(new GameStructureDefinition(
                GameStructureType.DryPortContainerTerminal,
                "Porto Seco / Terminal de Contêineres",
                "Porto Seco Intermodal",
                "Hub intermodal que gera contratos de alto valor e peso para o transporte rodoviário de contêineres marítimos.",
                StarterCareer.Trucker,
                StructureGroup.LogisticsHubs,
                StructureServices.LogisticsContracts | StructureServices.CargoSell,
                new Color(0.15f, 0.65f, 0.75f),
                "🚢"
            ) { requiresRoadNetwork = true });

            Register(new GameStructureDefinition(
                GameStructureType.QuarryConstructionYard,
                "Pátio de Obras / Pedreira",
                "Pátio de Obras & Brita",
                "Pontos para coleta e entrega de cargas de construção civil (cimento, brita, vigas de aço, maquinário pesado).",
                StarterCareer.Trucker,
                StructureGroup.LogisticsHubs,
                StructureServices.LogisticsContracts | StructureServices.CargoSell,
                new Color(0.60f, 0.60f, 0.60f),
                "🪨"
            ) { requiresMinerals = true });

            // =========================================================================
            // 3. PESCADOR (Extração e Aquicultura)
            // =========================================================================
            Register(new GameStructureDefinition(
                GameStructureType.ShipyardMarina,
                "Estaleiro e Marina Náutica",
                "Estaleiro & Marina",
                "Compra, atracação e melhoria de embarcações, desde pequenos barcos costeiros até traineiras oceânicas.",
                StarterCareer.Fisherman,
                StructureGroup.MachineryAndSupplies,
                StructureServices.VehicleSales | StructureServices.VehicleRepair,
                new Color(0.15f, 0.60f, 0.85f),
                "⚓"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            Register(new GameStructureDefinition(
                GameStructureType.NauticalSupplyShop,
                "Loja de Artigos Náuticos",
                "Artigos Náuticos",
                "Venda de insumos essenciais como iscas vivas, redes de pesca, covos (armadilhas) e caixas térmicas.",
                StarterCareer.Fisherman,
                StructureGroup.MachineryAndSupplies,
                StructureServices.SuppliesStore,
                new Color(0.20f, 0.70f, 0.75f),
                "🎣"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            Register(new GameStructureDefinition(
                GameStructureType.IceFactory,
                "Fábrica de Gelo Portuária",
                "Fábrica de Gelo",
                "Fornecimento essencial de gelo em escamas para conservação das cargas de peixe fresco durante os dias no mar.",
                StarterCareer.Fisherman,
                StructureGroup.MachineryAndSupplies,
                StructureServices.SuppliesStore,
                new Color(0.70f, 0.88f, 0.98f),
                "🧊"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            Register(new GameStructureDefinition(
                GameStructureType.FishMarket,
                "Mercado de Peixes",
                "Mercado de Peixe",
                "Varejão portuário para venda de peixe fresco a granel diretamente dos porões do barco.",
                StarterCareer.Fisherman,
                StructureGroup.RawMaterialTrade,
                StructureServices.CargoSell | StructureServices.RetailSales,
                new Color(0.25f, 0.75f, 0.65f),
                "🐟"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            Register(new GameStructureDefinition(
                GameStructureType.CanningFactory,
                "Fábrica de Enlatados",
                "Fábrica Enlatados",
                "Indústria de processamento que transforma toneladas de peixes comuns (sardinha, atum) em paletes de produtos de longa validade.",
                StarterCareer.Fisherman,
                StructureGroup.ProcessingIndustry,
                StructureServices.IndustrialProcessing | StructureServices.CargoSell,
                new Color(0.45f, 0.60f, 0.78f),
                "🥫"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            Register(new GameStructureDefinition(
                GameStructureType.FishMealPlant,
                "Indústria de Farinha de Peixe",
                "Farinha de Peixe",
                "Compra de peixes de baixo valor, miúdos e descartes para transformação industrial em ração ou fertilizante.",
                StarterCareer.Fisherman,
                StructureGroup.ProcessingIndustry,
                StructureServices.IndustrialProcessing | StructureServices.CargoSell,
                new Color(0.55f, 0.50f, 0.68f),
                "🧪"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            Register(new GameStructureDefinition(
                GameStructureType.SeafoodRestaurant,
                "Restaurantes de Frutos do Mar",
                "Restaurante Frutos do Mar",
                "Compradores de nicho que pagam prêmios altíssimos por pescas nobres (lagosta, salmão), exigindo máxima qualidade e frescor.",
                StarterCareer.Fisherman,
                StructureGroup.RetailTrade,
                StructureServices.RetailSales | StructureServices.CargoSell,
                new Color(0.85f, 0.35f, 0.30f),
                "🦞"
            ) { requiresWaterBody = true, forbiddenInDesert = true });

            // =========================================================================
            // 4. PILOTO (Aviação Civil, Carga e Agro)
            // =========================================================================
            Register(new GameStructureDefinition(
                GameStructureType.AviationHangarSales,
                "Hangar de Vendas & Customização",
                "Hangar Aeronaves",
                "Compra e customização de monomotores, jatos executivos, helicópteros e aviões agrícolas.",
                StarterCareer.Aviator,
                StructureGroup.MachineryAndSupplies,
                StructureServices.VehicleSales | StructureServices.VehicleRepair,
                new Color(0.40f, 0.60f, 0.95f),
                "✈️"
            ) { requiresAviation = true });

            Register(new GameStructureDefinition(
                GameStructureType.FBO_AirServices,
                "FBO (Fixed-Base Operator)",
                "FBO Serviços Solo",
                "Serviços de solo aeroportuário, abastecimento de Avgas e Jet A-1, hangaragem e manutenção de aeronaves.",
                StarterCareer.Aviator,
                StructureGroup.MachineryAndSupplies,
                StructureServices.FuelRefuel | StructureServices.VehicleRepair,
                new Color(0.30f, 0.70f, 0.90f),
                "🛫"
            ) { requiresAviation = true });

            Register(new GameStructureDefinition(
                GameStructureType.FlyingClub,
                "Aeroclube Regional",
                "Aeroclube",
                "Local para obtenção de novas licenças de voo ou geração de renda prestando instrução a novos pilotos.",
                StarterCareer.Aviator,
                StructureGroup.MachineryAndSupplies,
                StructureServices.FlightSchool | StructureServices.LogisticsContracts,
                new Color(0.80f, 0.40f, 0.85f),
                "🎓"
            ) { requiresAviation = true });

            Register(new GameStructureDefinition(
                GameStructureType.PassengerAirTerminal,
                "Terminal de Passageiros",
                "Terminal Passageiros",
                "Geração de contratos de Táxi Aéreo e voos executivos. A remuneração foca em suavidade do voo e pontualidade.",
                StarterCareer.Aviator,
                StructureGroup.LogisticsHubs,
                StructureServices.LogisticsContracts,
                new Color(0.25f, 0.50f, 0.90f),
                "🧳"
            ) { requiresAviation = true });

            Register(new GameStructureDefinition(
                GameStructureType.AirCargoTerminal,
                "Terminal de Cargas Aéreas",
                "Terminal Carga Aérea",
                "Geração de contratos de frete expresso de alto valor agregado (eletrônicos, urgências médicas, malotes) com prazos apertados.",
                StarterCareer.Aviator,
                StructureGroup.LogisticsHubs,
                StructureServices.LogisticsContracts,
                new Color(0.35f, 0.45f, 0.80f),
                "📦"
            ) { requiresAviation = true });

            Register(new GameStructureDefinition(
                GameStructureType.AgroSprayingBase,
                "Base de Pulverização Aérea",
                "Base Aeroagrícola",
                "Hub logístico onde o piloto abastece aviões agrícolas com defensivos/adubos para cumprir contratos de pulverização nas fazendas.",
                StarterCareer.Aviator,
                StructureGroup.SpecializedServices,
                StructureServices.LogisticsContracts | StructureServices.SuppliesStore | StructureServices.FuelRefuel,
                new Color(0.45f, 0.75f, 0.35f),
                "🌾"
            ) { requiresAviation = true, requiresAgriculture = true, forbiddenInDesert = true, forbiddenInPolar = true });

            Register(new GameStructureDefinition(
                GameStructureType.RescueHelipadHospital,
                "Heliponto de Resgate / Hospital",
                "Heliponto Hospitalar",
                "Base para contratos de emergência médica (Medevac), exigindo pousos difíceis em terrenos irregulares e voos em tempo recorde.",
                StarterCareer.Aviator,
                StructureGroup.SpecializedServices,
                StructureServices.LogisticsContracts,
                new Color(0.90f, 0.20f, 0.25f),
                "🚁"
            ) { requiresAviation = true });
        }

        private static void Register(GameStructureDefinition def)
        {
            catalog[def.type] = def;
        }
    }
}
