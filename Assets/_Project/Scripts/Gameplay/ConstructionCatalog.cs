using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Categorias principais de construção solicitadas pelo jogador.
    /// </summary>
    public enum BuildCategory
    {
        Estruturas,            // 1. Estruturas (Foco em Abrigo e Armazenamento)
        OperacoesProducao,     // 2. Operações e Produção (Foco na Economia e Logística)
        BiologiaCultivo,       // 3. Biologia e Cultivo (O equivalente à aba Animais)
        Decoracao,             // 4. Decoração (Assets visuais para imersão por área)
        Paisagismo             // 5. Paisagismo (Ferramentas de Modificação do Mundo)
    }

    /// <summary>
    /// Tipo de ação realizada ao confirmar uma construção/ferramenta.
    /// </summary>
    public enum BuildActionType
    {
        StructureObject,
        TerrainTerraform,
        WaterTerraform,
        GroundPaint,
        VegetationPaint
    }

    /// <summary>
    /// Definição de um item de construção/obra no menu.
    /// </summary>
    [System.Serializable]
    public class BuildItemDefinition
    {
        public string id;
        public string name;
        public string description;
        public BuildCategory category;
        public string subcategory;
        public StarterCareer requiredCareer; // Carreira principal vinculada
        public DriverLicense requiredLicense; // Licença associada (se houver, DriverLicense.None se livre para a profissão)
        public long cost;
        public BuildActionType actionType;
        public string actionPayload; // ex: nome do prefab, índice de layer ou tipo de relevo
        public string iconSymbol; // Símbolo de exibição

        public BuildItemDefinition() { }

        public BuildItemDefinition(
            string id,
            string name,
            string description,
            BuildCategory category,
            string subcategory,
            StarterCareer requiredCareer,
            DriverLicense requiredLicense,
            long cost,
            BuildActionType actionType,
            string actionPayload,
            string iconSymbol = "")
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.category = category;
            this.subcategory = subcategory;
            this.requiredCareer = requiredCareer;
            this.requiredLicense = requiredLicense;
            this.cost = cost;
            this.actionType = actionType;
            this.actionPayload = actionPayload;
            this.iconSymbol = iconSymbol;
        }

        /// <summary>
        /// Valida se a profissão/habilitação está desbloqueada no save ativo.
        /// Uma profissão está desbloqueada se for a carreira inicial do jogador
        /// OU se o jogador tiver a licença correspondente a ela.
        /// </summary>
        public bool IsUnlocked(RegionSaveData save)
        {
            if (save == null) return true;

            // Se for Paisagismo geral ou livre para todas as carreiras:
            // Verificação por carreira inicial ou habilitação da carreira
            bool careerUnlocked = false;

            // Carreira do item bate com a carreira inicial?
            if (save.starterCareer == requiredCareer)
            {
                careerUnlocked = true;
            }
            else
            {
                // Ou o jogador obteve a habilitação da profissão?
                switch (requiredCareer)
                {
                    case StarterCareer.Farmer:
                        careerUnlocked = save.HasLicense(DriverLicense.Agriculture);
                        break;
                    case StarterCareer.Trucker:
                        careerUnlocked = save.HasLicense(DriverLicense.Heavy) || save.HasLicense(DriverLicense.Standard);
                        break;
                    case StarterCareer.Aviator:
                        careerUnlocked = save.HasLicense(DriverLicense.Aviation);
                        break;
                    case StarterCareer.Fisherman:
                        careerUnlocked = save.HasLicense(DriverLicense.Maritime);
                        break;
                }
            }

            if (!careerUnlocked) return false;

            // Se o item exige uma licença técnica adicional específica
            if (requiredLicense != DriverLicense.None)
            {
                return save.HasLicense(requiredLicense);
            }

            return true;
        }

        public string GetRequirementText()
        {
            string profName = requiredCareer == StarterCareer.Farmer ? "Agricultor" :
                              requiredCareer == StarterCareer.Trucker ? "Caminhoneiro / Logística" :
                              requiredCareer == StarterCareer.Aviator ? "Aviador / Piloto" : "Pescador / Náutica";

            if (requiredLicense != DriverLicense.None)
            {
                var licInfo = DriverLicenseCatalog.Get(requiredLicense);
                string licName = licInfo != null ? licInfo.name : requiredLicense.ToString();
                return $"Profissão: {profName} + Licença: {licName}";
            }

            return $"Profissão: {profName}";
        }

        /// <summary>
        /// Gera a lista de recursos e materiais necessários para construir esta obra manualmente.
        /// </summary>
        public List<BuildResourceRequirement> GetResourceRequirements()
        {
            var reqs = new List<BuildResourceRequirement>();

            // Calcula quantidade base proporcional ao custo
            int baseUnits = Mathf.Clamp(Mathf.RoundToInt(cost / 250f), 10, 800);

            switch (category)
            {
                case BuildCategory.Estruturas:
                    if (id.Contains("hangar") || id.Contains("torre") || id.Contains("silo"))
                    {
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Steel, Mathf.RoundToInt(baseUnits * 0.45f)));
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Concrete, Mathf.RoundToInt(baseUnits * 0.40f)));
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.RoundToInt(baseUnits * 0.15f)));
                    }
                    else if (id.Contains("pier") || id.Contains("wood"))
                    {
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Wood, Mathf.RoundToInt(baseUnits * 0.70f)));
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Steel, Mathf.RoundToInt(baseUnits * 0.20f)));
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.RoundToInt(baseUnits * 0.10f)));
                    }
                    else
                    {
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Concrete, Mathf.RoundToInt(baseUnits * 0.40f)));
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Steel, Mathf.RoundToInt(baseUnits * 0.35f)));
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.RoundToInt(baseUnits * 0.25f)));
                    }
                    break;

                case BuildCategory.OperacoesProducao:
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Concrete, Mathf.RoundToInt(baseUnits * 0.40f)));
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Steel, Mathf.RoundToInt(baseUnits * 0.40f)));
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.RoundToInt(baseUnits * 0.20f)));
                    break;

                case BuildCategory.BiologiaCultivo:
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Wood, Mathf.RoundToInt(baseUnits * 0.50f)));
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.RoundToInt(baseUnits * 0.30f)));
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Concrete, Mathf.RoundToInt(baseUnits * 0.20f)));
                    break;

                case BuildCategory.Decoracao:
                    if (id.Contains("rustica") || id.Contains("madeira") || id.Contains("palete"))
                    {
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Wood, Mathf.Max(5, baseUnits)));
                    }
                    else if (id.Contains("alambrado") || id.Contains("poste") || id.Contains("conteiner"))
                    {
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Steel, Mathf.Max(5, baseUnits)));
                    }
                    else
                    {
                        reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.Max(5, baseUnits)));
                    }
                    break;

                case BuildCategory.Paisagismo:
                    // Terraplanagem, dragagem e nivelamento utilizam brita/pedra e cimento
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Stone, Mathf.RoundToInt(baseUnits * 0.60f)));
                    reqs.Add(new BuildResourceRequirement(BuildResourceType.Concrete, Mathf.RoundToInt(baseUnits * 0.40f)));
                    break;
            }

            return reqs;
        }
    }

    /// <summary>
    /// Catálogo central de todas as construções organizadas conforme a especificação do usuário.
    /// </summary>
    public static class ConstructionCatalog
    {
        public static readonly List<BuildItemDefinition> Items = new List<BuildItemDefinition>();

        static ConstructionCatalog()
        {
            InitializeCatalog();
        }

        private static void InitializeCatalog()
        {
            // =========================================================================
            // 1. ESTRUTURAS (Foco em Abrigo e Armazenamento)
            // =========================================================================

            // Garagens e Hangares
            Items.Add(new BuildItemDefinition(
                "str_gar_farm", "Abrigo de Máquinas Agrícolas",
                "Galpão aberto reforçado para proteger tratores, colheitadeiras e arados.",
                BuildCategory.Estruturas, "Garagens e Hangares",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                28000, BuildActionType.StructureObject, "Farm_MachineryShed", "[Galpão]"));

            Items.Add(new BuildItemDefinition(
                "str_gar_truck", "Pátio de Estacionamento de Frotas",
                "Área ampla pavimentada para manobra e estacionamento de cavalos mecânicos e carretas.",
                BuildCategory.Estruturas, "Garagens e Hangares",
                StarterCareer.Trucker, DriverLicense.Heavy,
                42000, BuildActionType.StructureObject, "Fleet_ParkingYard", "[Pátio]"));

            Items.Add(new BuildItemDefinition(
                "str_gar_hangar_sm", "Hangar Modular Pequeno",
                "Hangar para aeronaves monomotores e helicópteros utilitários leves.",
                BuildCategory.Estruturas, "Garagens e Hangares",
                StarterCareer.Aviator, DriverLicense.Aviation,
                65000, BuildActionType.StructureObject, "Hangar_Small", "[Hangar P]"));

            Items.Add(new BuildItemDefinition(
                "str_gar_hangar_md", "Hangar Modular Médio",
                "Hangar espaçoso para bimotores executivos e turboélices regionais.",
                BuildCategory.Estruturas, "Garagens e Hangares",
                StarterCareer.Aviator, DriverLicense.Aviation,
                120000, BuildActionType.StructureObject, "Hangar_Medium", "[Hangar M]"));

            Items.Add(new BuildItemDefinition(
                "str_gar_hangar_lg", "Hangar Modular Grande",
                "Estrutura de grande vão livre com oficina integrada para jatos e aeronaves de carga.",
                BuildCategory.Estruturas, "Garagens e Hangares",
                StarterCareer.Aviator, DriverLicense.Aviation,
                240000, BuildActionType.StructureObject, "Hangar_Large", "[Hangar G]"));

            // =========================================================================
            // FAZENDA MODULAR REALISTA (Modelos 3D Blender com Texturas PBR 2K)
            // =========================================================================
            Items.Add(new BuildItemDefinition(
                "mod_farm_wall_wood", "Parede de Celeiro em Madeira Rústica (4m)",
                "Módulo de parede 4x3.5m com tábuas verticais rústicas, vigas chanfradas e rodapé de pedra.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                2400, BuildActionType.StructureObject, "Farm_Wall_Wood_Planks", "[Parede Madeira]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_wall_door", "Portão Duplo de Correr de Celeiro (4m)",
                "Módulo com portão duplo rústico, travessas em X, roldanas e trilho superior em ferro forjado.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                3800, BuildActionType.StructureObject, "Farm_Wall_Door_Double", "[Portão Celeiro]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_wall_window", "Parede com Janela Colonial & Peitoril (4m)",
                "Módulo de parede com janela envidraçada de fazenda, esquadria em cruz e peitoril reforçado.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                2900, BuildActionType.StructureObject, "Farm_Wall_Window", "[Janela Rural]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_roof_straight", "Telhado de Duas Águas em Chapa de Zinco (4m)",
                "Seção de telhado com chapa ondulada galvanizada, cumeeira e vigamento estrutural aparente.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                3200, BuildActionType.StructureObject, "Farm_Roof_Straight", "[Telhado Zinco]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_roof_gable", "Frontão / Oitão com Respiro de Sótão (4m)",
                "Fechamento triangular superior do celeiro com aletas de ventilação para o sótão.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                2100, BuildActionType.StructureObject, "Farm_Roof_Gable_End", "[Frontão Sótão]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_silo_modular", "Silo Industrial de Grãos com Escada (9m)",
                "Torre cilíndrica de chapa corrugada com anéis de reforço, cúpula, exaustor e escada de inspeção.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                28000, BuildActionType.StructureObject, "Farm_Silo_Modular", "[Silo Grãos]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_fence_wood", "Cerca Rústica de Eucalipto (3m)",
                "Módulo de cerca rural com mourões roliços tratados e três réguas horizontais chanfradas.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.None,
                450, BuildActionType.StructureObject, "Farm_Fence_Wood", "[Cerca Madeira]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_fence_gate", "Porteira Articulável de Fazenda (3m)",
                "Porteira de madeira maciça com reforço diagonal em Z, ferragens de ferro e tranca de segurança.",
                BuildCategory.Estruturas, "Fazenda Modular (Realista)",
                StarterCareer.Farmer, DriverLicense.None,
                1100, BuildActionType.StructureObject, "Farm_Fence_Gate", "[Porteira]"));

            // =========================================================================
            // CIDADE MODULAR REALISTA (Modelos 3D Blender com Texturas PBR 2K)
            // =========================================================================
            Items.Add(new BuildItemDefinition(
                "mod_city_wall_shop", "Fachada de Vitrine Comercial & Marquise (4m)",
                "Fachada térrea de loja comercial com vitrine dupla de alta reflexão, porta com puxador inox e marquise moderna.",
                BuildCategory.Estruturas, "Cidade Modular (Realista)",
                StarterCareer.Trucker, DriverLicense.Standard,
                14500, BuildActionType.StructureObject, "City_Wall_Concrete_ShopFront", "[Loja Vitrine]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_wall_brick", "Fachada Residencial de Tijolo Aparente (4m)",
                "Módulo urbano clássico em alvenaria com junta de argamassa e duas janelas guilhotina emolduradas.",
                BuildCategory.Estruturas, "Cidade Modular (Realista)",
                StarterCareer.Trucker, DriverLicense.Standard,
                11200, BuildActionType.StructureObject, "City_Wall_Brick_Residential", "[Prédio Tijolo]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_wall_glass", "Painel Cortina de Vidro Contemporâneo (4m)",
                "Fachada corporativa moderna em vidro reflexivo com perfis estruturais de alumínio escovado.",
                BuildCategory.Estruturas, "Cidade Modular (Realista)",
                StarterCareer.Trucker, DriverLicense.Standard,
                18500, BuildActionType.StructureObject, "City_Wall_Modern_Glass", "[Cortina Vidro]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_roof_parapet", "Laje de Cobertura com Platibanda & Zinco (4x4m)",
                "Módulo de laje impermeabilizada com mureta platibanda e pingadeira de chapa galvanizada.",
                BuildCategory.Estruturas, "Cidade Modular (Realista)",
                StarterCareer.Trucker, DriverLicense.Standard,
                7800, BuildActionType.StructureObject, "City_Roof_Parapet_4m", "[Laje Platibanda]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_sidewalk_straight", "Calçada de Concreto com Meio-Fio & Sarjeta (4m)",
                "Placas de concreto polido com juntas de dilatação, guia de granito chanfrado e sarjeta asfáltica.",
                BuildCategory.Estruturas, "Cidade Modular (Realista)",
                StarterCareer.Trucker, DriverLicense.None,
                1600, BuildActionType.StructureObject, "City_Sidewalk_Straight_4m", "[Calçada 4m]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_sidewalk_corner", "Esquina de Calçada com Rampa Acessível (4x4m)",
                "Módulo de esquina urbana com curvatura de guia e piso rebaixado para travessia de pedestres.",
                BuildCategory.Estruturas, "Cidade Modular (Realista)",
                StarterCareer.Trucker, DriverLicense.None,
                2400, BuildActionType.StructureObject, "City_Sidewalk_Corner_4m", "[Esquina Calçada]"));

            // Docas e Atracadouros
            Items.Add(new BuildItemDefinition(
                "str_doc_pier_wood", "Píer de Madeira Rústico",
                "Píer tradicional sobre estacas de madeira para atracação de botes e barcos de pesca.",
                BuildCategory.Estruturas, "Docas e Atracadouros",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                15000, BuildActionType.StructureObject, "Pier_Wood", "[Píer]"));

            Items.Add(new BuildItemDefinition(
                "str_doc_marina_conc", "Marina de Concreto",
                "Píer flutuante de concreto armado com cabeços de amarração de alta resistência.",
                BuildCategory.Estruturas, "Docas e Atracadouros",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                55000, BuildActionType.StructureObject, "Marina_Concrete", "[Marina]"));

            Items.Add(new BuildItemDefinition(
                "str_doc_ancoradouro", "Ancoradouro Protegido",
                "Área de fundeio seguro para embarcações de médio porte em enseadas e margens fluviais.",
                BuildCategory.Estruturas, "Docas e Atracadouros",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                38000, BuildActionType.StructureObject, "Anchorage_Protected", "[Fundeio]"));

            Items.Add(new BuildItemDefinition(
                "str_doc_boathouse", "Casa de Barcos & Manutenção",
                "Galpão aquático coberto com rampa de recolhimento para manutenção preventiva naval.",
                BuildCategory.Estruturas, "Docas e Atracadouros",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                72000, BuildActionType.StructureObject, "BoatHouse_Maintenance", "[Casa Barco]"));

            // Armazenamento de Carga
            Items.Add(new BuildItemDefinition(
                "str_arm_silo", "Silo Agrícola de Grãos",
                "Torre cilíndrica de aeração para armazenagem e transbordo seguro de soja, milho e trigo.",
                BuildCategory.Estruturas, "Armazenamento de Carga",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                48000, BuildActionType.StructureObject, "Grain_Silo", "[Silo]"));

            Items.Add(new BuildItemDefinition(
                "str_arm_crossdock", "Galpão Cross-Docking",
                "Armazém logístico de carga seca com múltiplas docas de entrada e saída rápida de mercadorias.",
                BuildCategory.Estruturas, "Armazenamento de Carga",
                StarterCareer.Trucker, DriverLicense.Standard,
                85000, BuildActionType.StructureObject, "Warehouse_CrossDock", "[Galpão Seco]"));

            Items.Add(new BuildItemDefinition(
                "str_arm_coldroom", "Câmara Fria Industrial",
                "Instalação frigorificada com controle térmico rigoroso para pescados frescos e laticínios.",
                BuildCategory.Estruturas, "Armazenamento de Carga",
                StarterCareer.Fisherman, DriverLicense.Refrigerated,
                92000, BuildActionType.StructureObject, "ColdStorage_Unit", "[Câmara Fria]"));

            // Tanques e Insumos
            Items.Add(new BuildItemDefinition(
                "str_tq_water_fert", "Reservatório de Água & Fertilizante",
                "Tanques elevados de polietileno para estocagem de calda de pulverização e irrigação.",
                BuildCategory.Estruturas, "Tanques e Insumos",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                18000, BuildActionType.StructureObject, "Water_Fertilizer_Tank", "[Tanque Agro]"));

            Items.Add(new BuildItemDefinition(
                "str_tq_diesel", "Posto & Tanque de Diesel B10",
                "Bomba com bocal de alta vazão para abastecimento ágil de tratores, caminhões e traineiras.",
                BuildCategory.Estruturas, "Tanques e Insumos",
                StarterCareer.Trucker, DriverLicense.Hazmat,
                38000, BuildActionType.StructureObject, "Diesel_Fuel_Station", "[Tanque Diesel]"));

            Items.Add(new BuildItemDefinition(
                "str_tq_jet_a1", "Tanque de Querosene Jet A-1",
                "Reservatório homologado para querosene de aviação com filtro desidratador e carretel.",
                BuildCategory.Estruturas, "Tanques e Insumos",
                StarterCareer.Aviator, DriverLicense.Hazmat,
                75000, BuildActionType.StructureObject, "Aviation_JetA1_Tank", "[Jet A-1]"));

            // Manutenção
            Items.Add(new BuildItemDefinition(
                "str_man_oficina", "Oficina Mecânica Universal",
                "Oficina equipada com elevador hidráulico e bancada de ferramentas para reparo automotivo.",
                BuildCategory.Estruturas, "Manutenção",
                StarterCareer.Trucker, DriverLicense.Standard,
                45000, BuildActionType.StructureObject, "Universal_Workshop", "[Oficina]"));

            Items.Add(new BuildItemDefinition(
                "str_man_lavagem", "Estação de Lavagem Pesada",
                "Box de alta pressão com separador de água e óleo para desinfecção de caminhões e tratores.",
                BuildCategory.Estruturas, "Manutenção",
                StarterCareer.Trucker, DriverLicense.Standard,
                24000, BuildActionType.StructureObject, "HeavyWash_Station", "[Lavador]"));

            Items.Add(new BuildItemDefinition(
                "str_man_guindaste", "Guindaste de Estaleiro",
                "Guindaste giratório para içamento de barcos da água para calçamento em terra firme.",
                BuildCategory.Estruturas, "Manutenção",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                68000, BuildActionType.StructureObject, "Shipyard_Crane", "[Guindaste]"));

            Items.Add(new BuildItemDefinition(
                "str_man_rampa", "Rampa de Inspeção & Troca",
                "Rampa elevada em concreto para vistoria do chassi e drenagem de cárter sem necessidade de elevador.",
                BuildCategory.Estruturas, "Manutenção",
                StarterCareer.Farmer, DriverLicense.Standard,
                12000, BuildActionType.StructureObject, "Inspection_Ramp", "[Rampa Vistoria]"));

            // =========================================================================
            // 2. OPERAÇÕES E PRODUÇÃO (Foco na Economia e Logística)
            // =========================================================================

            // Processamento e Indústria
            Items.Add(new BuildItemDefinition(
                "op_ind_moinho", "Moinho Regional de Grãos",
                "Unidade de moagem e beneficiamento de cereais para farinhas e ração animal.",
                BuildCategory.OperacoesProducao, "Processamento e Indústria",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                54000, BuildActionType.StructureObject, "Grain_Mill", "[Moinho]"));

            Items.Add(new BuildItemDefinition(
                "op_ind_laticinio", "Laticínio & Fábrica de Queijos",
                "Beneficiamento de leite pasteurizado, queijos artesanais e derivados lácteos.",
                BuildCategory.OperacoesProducao, "Processamento e Indústria",
                StarterCareer.Farmer, DriverLicense.Livestock,
                78000, BuildActionType.StructureObject, "Dairy_ProcessingPlant", "[Laticínio]"));

            Items.Add(new BuildItemDefinition(
                "op_ind_gelo", "Fábrica de Gelo Escamado",
                "Produção contínua de gelo em escamas indispensável para conservar pescado em porões.",
                BuildCategory.OperacoesProducao, "Processamento e Indústria",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                36000, BuildActionType.StructureObject, "Ice_Factory", "[Fábrica Gelo]"));

            Items.Add(new BuildItemDefinition(
                "op_ind_enlatamento", "Enlatamento de Sardinhas",
                "Linha industrial de conserva, cozimento e selagem de pescados em óleo e molho.",
                BuildCategory.OperacoesProducao, "Processamento e Indústria",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                88000, BuildActionType.StructureObject, "Canning_Plant", "[Enlatamento]"));

            Items.Add(new BuildItemDefinition(
                "op_ind_filetagem", "Unidade de Filetagem",
                "Bancadas higienizadas de evisceração, corte em filés e embalagem a vácuo.",
                BuildCategory.OperacoesProducao, "Processamento e Indústria",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                46000, BuildActionType.StructureObject, "Filleting_Unit", "[Filetagem]"));

            // Infraestrutura Logística
            Items.Add(new BuildItemDefinition(
                "op_log_doca_bau", "Plataforma de Carregamento (Doca)",
                "Doca niveladora com selo de borracha para encaixe hermético de baús de carretas.",
                BuildCategory.OperacoesProducao, "Infraestrutura Logística",
                StarterCareer.Trucker, DriverLicense.Standard,
                32000, BuildActionType.StructureObject, "Loading_Dock", "[Doca Baú]"));

            Items.Add(new BuildItemDefinition(
                "op_log_triagem", "Pátio de Triagem de Contêineres",
                "Pátio compactado de alta capacidade com marcações de blocagem TEU para empilhamento.",
                BuildCategory.OperacoesProducao, "Infraestrutura Logística",
                StarterCareer.Trucker, DriverLicense.Heavy,
                95000, BuildActionType.StructureObject, "Container_TriagingYard", "[Pátio Contêiner]"));

            Items.Add(new BuildItemDefinition(
                "op_log_pesagem", "Posto de Pesagem Rodoviária",
                "Balança rodoviária de plataforma para aferição de peso bruto total por eixo.",
                BuildCategory.OperacoesProducao, "Infraestrutura Logística",
                StarterCareer.Trucker, DriverLicense.Heavy,
                42000, BuildActionType.StructureObject, "Weigh_Station", "[Balança]"));

            // Aviação e Controle
            Items.Add(new BuildItemDefinition(
                "op_av_torre", "Torre de Controle de Tráfego (TWR)",
                "Torre de observação 360° equipada com rádio VHF, farol rotativo e biruta anemométrica.",
                BuildCategory.OperacoesProducao, "Aviação e Controle",
                StarterCareer.Aviator, DriverLicense.Aviation,
                160000, BuildActionType.StructureObject, "Control_Tower", "[Torre Controle]"));

            Items.Add(new BuildItemDefinition(
                "op_av_radar", "Estação de Radar e Meteorologia",
                "Domo de radar de aproximação com antena giratória e estação meteorológica automatizada.",
                BuildCategory.OperacoesProducao, "Aviação e Controle",
                StarterCareer.Aviator, DriverLicense.Aviation,
                110000, BuildActionType.StructureObject, "Radar_Station", "[Radar]"));

            Items.Add(new BuildItemDefinition(
                "op_av_terminal_pax", "Terminal de Passageiros (Táxi Aéreo)",
                "Saguão climatizado com sala VIP e balcão de embarque para transporte aéreo executivo.",
                BuildCategory.OperacoesProducao, "Aviação e Controle",
                StarterCareer.Aviator, DriverLicense.HighValue,
                185000, BuildActionType.StructureObject, "Passenger_Terminal", "[Terminal Pax]"));

            Items.Add(new BuildItemDefinition(
                "op_av_terminal_carga", "Terminal de Carga Aérea",
                "Armazém alfandegado com balança de paletes ULD para fretes aéreos urgentes.",
                BuildCategory.OperacoesProducao, "Aviação e Controle",
                StarterCareer.Aviator, DriverLicense.Aviation,
                140000, BuildActionType.StructureObject, "AirCargo_Terminal", "[Terminal Carga]"));

            // Pontos de Venda e Contratos
            Items.Add(new BuildItemDefinition(
                "op_venda_peixe", "Mercado de Peixe Municipal",
                "Pavilhão público com bancadas de gelo para venda direta de pescado e mariscos frescos.",
                BuildCategory.OperacoesProducao, "Pontos de Venda e Contratos",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                52000, BuildActionType.StructureObject, "Fish_Market", "[Mercado Peixe]"));

            Items.Add(new BuildItemDefinition(
                "op_venda_restaurante", "Restaurante Local Beira-Rio",
                "Restaurante de culinária regional com demanda contínua por legumes da horta e peixes.",
                BuildCategory.OperacoesProducao, "Pontos de Venda e Contratos",
                StarterCareer.Fisherman, DriverLicense.Standard,
                64000, BuildActionType.StructureObject, "Local_Restaurant", "[Restaurante]"));

            Items.Add(new BuildItemDefinition(
                "op_venda_exportacao", "Empresa Comercial Exportadora",
                "Escritório de comércio exterior para fechamento de contratos de grandes volumes de safras.",
                BuildCategory.OperacoesProducao, "Pontos de Venda e Contratos",
                StarterCareer.Trucker, DriverLicense.HighValue,
                125000, BuildActionType.StructureObject, "Export_Company", "[Exportadora]"));

            Items.Add(new BuildItemDefinition(
                "op_venda_mural", "Mural de Contratos da Província",
                "Quadro de avisos comunitário onde empresas e NPCs cadastram pedidos de fretes e rotas.",
                BuildCategory.OperacoesProducao, "Pontos de Venda e Contratos",
                StarterCareer.Trucker, DriverLicense.Standard,
                15000, BuildActionType.StructureObject, "Contract_NoticeBoard", "[Mural Contratos]"));

            // =========================================================================
            // 3. BIOLOGIA E CULTIVO (Equivalente à aba "Animais")
            // =========================================================================

            // Pecuária
            Items.Add(new BuildItemDefinition(
                "bio_pec_celeiro", "Celeiro Tradicional",
                "Celeiro de madeira com sótão para feno e baias confortáveis para animais.",
                BuildCategory.BiologiaCultivo, "Pecuária",
                StarterCareer.Farmer, DriverLicense.Livestock,
                35000, BuildActionType.StructureObject, "Barn_Traditional", "[Celeiro]"));

            Items.Add(new BuildItemDefinition(
                "bio_pec_curral", "Curral de Manejo Bovino",
                "Estrutura cercada com tronco de contenção e embarcadouro para gado de corte e leite.",
                BuildCategory.BiologiaCultivo, "Pecuária",
                StarterCareer.Farmer, DriverLicense.Livestock,
                26000, BuildActionType.StructureObject, "Cattle_Corral", "[Curral]"));

            Items.Add(new BuildItemDefinition(
                "bio_pec_chiqueiro", "Chiqueiro & Pocilga",
                "Baia com piso lavável e cocho automático para criação controlada de suínos.",
                BuildCategory.BiologiaCultivo, "Pecuária",
                StarterCareer.Farmer, DriverLicense.Livestock,
                22000, BuildActionType.StructureObject, "Pig_Pen", "[Chiqueiro]"));

            Items.Add(new BuildItemDefinition(
                "bio_pec_galinheiro", "Galinheiro Tradicional",
                "Abrigo cercado com ninhos acolchoados e poleiros para postura de ovos caipiras.",
                BuildCategory.BiologiaCultivo, "Pecuária",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                14000, BuildActionType.StructureObject, "Chicken_Coop", "[Galinheiro]"));

            // Aquicultura
            Items.Add(new BuildItemDefinition(
                "bio_aqui_tanque_rede", "Tanque-Rede (Tilápias / Salmão)",
                "Gaiola flutuante de tela aramada instalada em rios ou lagos para engorda intensiva.",
                BuildCategory.BiologiaCultivo, "Aquicultura",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                28000, BuildActionType.StructureObject, "FishCage_Floating", "[Tanque-Rede]"));

            Items.Add(new BuildItemDefinition(
                "bio_aqui_ostras", "Fazenda de Ostras & Maricultura",
                "Linhas marinhas com lanternas suspensas para cultivo costeiro de ostras e mexilhões.",
                BuildCategory.BiologiaCultivo, "Aquicultura",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                34000, BuildActionType.StructureObject, "Oyster_Farm", "[Fazenda Ostras]"));

            Items.Add(new BuildItemDefinition(
                "bio_aqui_camarao", "Viveiro de Camarão Costeiro",
                "Bacia escavada de água salobra com aeradores de pá para cultivo de camarões.",
                BuildCategory.BiologiaCultivo, "Aquicultura",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                49000, BuildActionType.StructureObject, "Shrimp_Nursery", "[Viveiro Camarão]"));

            Items.Add(new BuildItemDefinition(
                "bio_aqui_iscas", "Criadouro de Iscas Vivas",
                "Tanques sombreados para produção contínua de camarões-vivos, lambaris e caranguejos.",
                BuildCategory.BiologiaCultivo, "Aquicultura",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                18000, BuildActionType.StructureObject, "LiveBait_Breeder", "[Criadouro Iscas]"));

            // Estufas e Pomares
            Items.Add(new BuildItemDefinition(
                "bio_est_vegetais", "Estufa de Vegetais Climatizada",
                "Estufa com cobertura de policarbonato, controle de umidade e gotejamento automático.",
                BuildCategory.BiologiaCultivo, "Estufas e Pomares",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                38000, BuildActionType.StructureObject, "Greenhouse_Vegetables", "[Estufa Vegetais]"));

            Items.Add(new BuildItemDefinition(
                "bio_est_pomar", "Pomar de Frutas Selecionadas",
                "Alinhamento de árvores frutíferas enxertadas (laranja, maçã, pêssego) com irrigação.",
                BuildCategory.BiologiaCultivo, "Estufas e Pomares",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                22000, BuildActionType.StructureObject, "Fruit_Orchard", "[Pomar Frutas]"));

            Items.Add(new BuildItemDefinition(
                "bio_est_videiras", "Vinhedo & Videiras em Espaldeira",
                "Linhas de postes de sustentação com aramados para produção de uvas nobres.",
                BuildCategory.BiologiaCultivo, "Estufas e Pomares",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                29000, BuildActionType.StructureObject, "Vineyard_Trellis", "[Vinhedo]"));

            // =========================================================================
            // 4. DECORAÇÃO (Assets visuais para imersão por área)
            // =========================================================================

            // Cercas e Muros
            Items.Add(new BuildItemDefinition(
                "dec_cer_rustica", "Cerca Rústica de Fazenda",
                "Módulos de cerca de madeira com mourões torneados e arame liso para áreas rurais.",
                BuildCategory.Decoracao, "Cercas e Muros",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                1200, BuildActionType.StructureObject, "Fence_RusticFarm", "[Cerca Rústica]"));

            Items.Add(new BuildItemDefinition(
                "dec_cer_alambrado", "Alambrado com Arame Farpado",
                "Grade de aço galvanizado padrão de segurança aeroportuária com concertina superior.",
                BuildCategory.Decoracao, "Cercas e Muros",
                StarterCareer.Aviator, DriverLicense.Aviation,
                2800, BuildActionType.StructureObject, "Fence_AirportSecurity", "[Alambrado Aero]"));

            Items.Add(new BuildItemDefinition(
                "dec_cer_muro_porto", "Muro Industrial de Porto",
                "Painéis pesados de concreto pré-moldado para fechamento perimetral de pátios e cais.",
                BuildCategory.Decoracao, "Cercas e Muros",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                4500, BuildActionType.StructureObject, "Wall_IndustrialPort", "[Muro Porto]"));

            // Sinalização e Iluminação
            Items.Add(new BuildItemDefinition(
                "dec_sin_poste_rodovia", "Poste de Rodovia Duplo",
                "Poste metálico com luminárias LED de alta potência para pátios logísticos e vias.",
                BuildCategory.Decoracao, "Sinalização e Iluminação",
                StarterCareer.Trucker, DriverLicense.Standard,
                1800, BuildActionType.StructureObject, "LampPost_Highway", "[Poste Vias]"));

            Items.Add(new BuildItemDefinition(
                "dec_sin_baliza_pista", "Luzes Balizadoras de Pista",
                "Conjunto de sinalizadores verdes e brancos para demarcação noturna de pouso e taxiamento.",
                BuildCategory.Decoracao, "Sinalização e Iluminação",
                StarterCareer.Aviator, DriverLicense.Aviation,
                3200, BuildActionType.StructureObject, "Runway_LightBalizers", "[Balizas Pista]"));

            Items.Add(new BuildItemDefinition(
                "dec_sin_farol_rotativo", "Farol Rotativo Aeroportuário (Beacon)",
                "Farol óptico giratório verde/branco para orientação de navegação aérea noturna.",
                BuildCategory.Decoracao, "Sinalização e Iluminação",
                StarterCareer.Aviator, DriverLicense.Aviation,
                11000, BuildActionType.StructureObject, "Airport_RotatingBeacon", "[Farol Aero]"));

            Items.Add(new BuildItemDefinition(
                "dec_sin_boia_nav", "Boia de Navegação Flutuante",
                "Boia marítima de balizamento de canal com lanterna solar e refletor de radar.",
                BuildCategory.Decoracao, "Sinalização e Iluminação",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                2900, BuildActionType.StructureObject, "Navigation_Buoy", "[Boia Canal]"));

            Items.Add(new BuildItemDefinition(
                "dec_sin_farol_costeiro", "Farol Costeiro de Alvenaria",
                "Torre sinalizadora com facho luminoso de longo alcance para orientação marítima.",
                BuildCategory.Decoracao, "Sinalização e Iluminação",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                48000, BuildActionType.StructureObject, "Coastal_Lighthouse", "[Farol Costa]"));

            // Adereços Temáticos (Props)
            Items.Add(new BuildItemDefinition(
                "dec_prp_redes", "Redes de Pesca Secando no Cavalete",
                "Cavalete de madeira com redes marinhas estendidas para secagem e reparo.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                800, BuildActionType.StructureObject, "Prop_FishingNets", "[Redes Pesca]"));

            Items.Add(new BuildItemDefinition(
                "dec_prp_covos", "Armadilhas para Caranguejos (Covos)",
                "Pilha de covos aramados de madeira para captura de crustáceos.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                950, BuildActionType.StructureObject, "Prop_CrabTraps", "[Covos]"));

            Items.Add(new BuildItemDefinition(
                "dec_prp_biruta", "Biruta de Vento (Windsock)",
                "Mastro metálico com biruta em tecido fluorescente para indicação de direção e vento.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Aviator, DriverLicense.Aviation,
                1600, BuildActionType.StructureObject, "Prop_Windsock", "[Biruta]"));

            Items.Add(new BuildItemDefinition(
                "dec_prp_cones", "Cones de Trânsito Refletivos",
                "Conjunto de cones de borracha laranja com faixas refletivas para isolar pistas e cargas.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Trucker, DriverLicense.Standard,
                400, BuildActionType.StructureObject, "Prop_TrafficCones", "[Cones]"));

            Items.Add(new BuildItemDefinition(
                "dec_prp_pneus_cais", "Pneus de Proteção de Cais",
                "Defensas de atracação em borracha instaladas na borda do píer contra impacto de cascos.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                700, BuildActionType.StructureObject, "Prop_DockTires", "[Defensas Cais]"));

            Items.Add(new BuildItemDefinition(
                "dec_prp_conteiner_rust", "Contêiner Marítimo Vintage",
                "Contêiner 20 pés de aço corten envelhecido para ambientação portuária realista.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Trucker, DriverLicense.Standard,
                3500, BuildActionType.StructureObject, "Prop_RustyContainer", "[Contêiner]"));

            Items.Add(new BuildItemDefinition(
                "dec_prp_paletes", "Pilha de Paletes Vazios",
                "Pilha organizada de estrados de madeira padrão PBR para áreas de carga e fazenda.",
                BuildCategory.Decoracao, "Adereços Temáticos (Props)",
                StarterCareer.Trucker, DriverLicense.Standard,
                300, BuildActionType.StructureObject, "Prop_EmptyPallets", "[Paletes]"));

            // =========================================================================
            // MOBILIÁRIO URBANO REALISTA (Modelos 3D Blender com Texturas PBR 2K)
            // =========================================================================
            Items.Add(new BuildItemDefinition(
                "mod_city_prop_light", "Poste de Iluminação LED Moderno (6.5m)",
                "Poste de aço tubular com fuste cônico, braço aerodinâmico e luminária viária LED.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                1800, BuildActionType.StructureObject, "City_Prop_StreetLight_Modern", "[Poste LED]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_prop_shelter", "Ponto de Ônibus Moderno com Totem",
                "Abrigo urbano em vidro temperado, pilares de aço, cobertura protetora e totem publicitário.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                5600, BuildActionType.StructureObject, "City_Prop_Bus_Shelter", "[Ponto Ônibus]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_prop_trash", "Lixeira Urbana Dupla Coleta Seletiva",
                "Conjunto duplo em aço inoxidável escovado para descarte separado de recicláveis e orgânicos.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                850, BuildActionType.StructureObject, "City_Prop_TrashBin_Duo", "[Lixeira Inox]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_prop_bench", "Banco de Praça em Madeira & Ferro",
                "Banco contemporâneo com ripas de madeira nobre tratada e pés curvos de ferro fundido.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                750, BuildActionType.StructureObject, "City_Prop_Park_Bench", "[Banco Parque]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_prop_hydrant", "Hidrante Industrial de Alta Pressão",
                "Hidrante de rua em ferro fundido vermelho de segurança, conexões em latão polido e tampas de proteção.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                1200, BuildActionType.StructureObject, "City_Prop_Fire_Hydrant", "[Hidrante]"));

            Items.Add(new BuildItemDefinition(
                "mod_city_prop_bollard", "Balizador de Proteção em Concreto",
                "Pilar balizador de pedestres em concreto polido com topo semi-esférico e faixa de segurança.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                320, BuildActionType.StructureObject, "City_Prop_Bollard_Concrete", "[Balizador]"));

            // =========================================================================
            // OBJETOS E PROPS DE FAZENDA (Modelos 3D Blender com Texturas PBR 2K)
            // =========================================================================
            Items.Add(new BuildItemDefinition(
                "mod_farm_prop_hay", "Fardo de Feno Dourado Prensado",
                "Fardo retangular de feno compacto com amarrações de corda para alimentação e ambientação do pasto.",
                BuildCategory.Decoracao, "Objetos de Fazenda PBR",
                StarterCareer.Farmer, DriverLicense.None,
                120, BuildActionType.StructureObject, "Farm_Prop_Hay_Bale", "[Fardo Feno]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_prop_barrel", "Barril de Carvalho Rústico com Aros de Ferro",
                "Barril tradicional de carvalho com ripas curvas, aros de ferro forjado e acabamento envelhecido.",
                BuildCategory.Decoracao, "Objetos de Fazenda PBR",
                StarterCareer.Farmer, DriverLicense.None,
                250, BuildActionType.StructureObject, "Farm_Prop_Wooden_Barrel", "[Barril Carvalho]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_prop_trough", "Cocho / Comedouro Rústico de Madeira",
                "Comedouro e bebedouro reforçado para gado e cavalos em madeira maciça com suporte elevado.",
                BuildCategory.Decoracao, "Objetos de Fazenda PBR",
                StarterCareer.Farmer, DriverLicense.None,
                480, BuildActionType.StructureObject, "Farm_Prop_Trough", "[Cocho Madeira]"));

            Items.Add(new BuildItemDefinition(
                "mod_farm_prop_pallet", "Pilha de Paletes Industriais de Madeira",
                "Pilha com dois estrados de madeira padronizados de alta resistência para armazenamento e logística.",
                BuildCategory.Decoracao, "Objetos de Fazenda PBR",
                StarterCareer.Farmer, DriverLicense.None,
                180, BuildActionType.StructureObject, "Farm_Prop_Pallet_Stack", "[Paletes Madeira]"));

            // -------------------------------------------------------------------------
            // FERRAMENTAS AGRÍCOLAS RÚSTICAS (Rusted Farm Tools)
            // -------------------------------------------------------------------------
            Items.Add(new BuildItemDefinition(
                "tool_farm_shovel_sq", "Pá Reta de Corte (Aço Rústico)",
                "Pá reta clássica em aço forjado oxidado com cabo longo de freixo tratado.",
                BuildCategory.Decoracao, "Ferramentas Agrícolas",
                StarterCareer.Farmer, DriverLicense.None,
                65, BuildActionType.StructureObject, "Tool_Farm_ShovelSquare", "[Pá Reta]"));

            Items.Add(new BuildItemDefinition(
                "tool_farm_spade", "Pá de Bico (Aço Forjado)",
                "Pá de ponta ogival para escavação, plantio e movimentação de terra vegetal.",
                BuildCategory.Decoracao, "Ferramentas Agrícolas",
                StarterCareer.Farmer, DriverLicense.None,
                65, BuildActionType.StructureObject, "Tool_Farm_Spade", "[Pá Bico]"));

            Items.Add(new BuildItemDefinition(
                "tool_farm_pitchfork", "Forcado / Garfo de Feno",
                "Garfo agrícola de 4 dentes afiados para manipulação de palha, silagem e feno seco.",
                BuildCategory.Decoracao, "Ferramentas Agrícolas",
                StarterCareer.Farmer, DriverLicense.None,
                85, BuildActionType.StructureObject, "Tool_Farm_Pitchfork", "[Garfo Feno]"));

            Items.Add(new BuildItemDefinition(
                "tool_farm_hoe", "Enxada Agrícola Tradicional",
                "Enxada de capina com lâmina de corte transversal e fixação reforçada.",
                BuildCategory.Decoracao, "Ferramentas Agrícolas",
                StarterCareer.Farmer, DriverLicense.None,
                55, BuildActionType.StructureObject, "Tool_Farm_Hoe", "[Enxada]"));

            Items.Add(new BuildItemDefinition(
                "tool_farm_rake", "Ancinho / Rastelo Agrícola",
                "Rastelo de aço multi-dentes para nivelamento de canteiros e coleta de folhas secas.",
                BuildCategory.Decoracao, "Ferramentas Agrícolas",
                StarterCareer.Farmer, DriverLicense.None,
                60, BuildActionType.StructureObject, "Tool_Farm_Rake", "[Rastelo]"));

            Items.Add(new BuildItemDefinition(
                "tool_farm_collection", "Conjunto de Ferramentas de Fazenda",
                "Bancada completa com as 5 ferramentas agrícolas prontas para uso rural.",
                BuildCategory.Decoracao, "Ferramentas Agrícolas",
                StarterCareer.Farmer, DriverLicense.None,
                290, BuildActionType.StructureObject, "FarmEquipment_Collection", "[Kit Ferramentas]"));

            Items.Add(new BuildItemDefinition(
                "mod_pop_seats_col", "Conjunto de Assentos e Bancos Urbanos",
                "Fileira de bancos e assentos públicos de praça da coleção Populate.",
                BuildCategory.Decoracao, "Mobiliário Urbano PBR",
                StarterCareer.Trucker, DriverLicense.None,
                450, BuildActionType.StructureObject, "Populate_Seats_Collection", "[Bancos Praça]"));

            // =========================================================================
            // 5. PAISAGISMO (Ferramentas de Modificação do Mundo)
            // =========================================================================

            // Esculpir Terra
            Items.Add(new BuildItemDefinition(
                "pai_esc_elevar", "Elevar Terreno",
                "Ergue o solo suavemente sob o cursor para criar aterros, colinas e diques.",
                BuildCategory.Paisagismo, "Esculpir Terra",
                StarterCareer.Trucker, DriverLicense.Heavy,
                5000, BuildActionType.TerrainTerraform, "raise", "[▲ Elevar]"));

            Items.Add(new BuildItemDefinition(
                "pai_esc_rebaixar", "Rebaixar Terreno",
                "Escava o solo criando valas, trincheiras ou preparando leitos de canais.",
                BuildCategory.Paisagismo, "Esculpir Terra",
                StarterCareer.Trucker, DriverLicense.Heavy,
                5000, BuildActionType.TerrainTerraform, "lower", "[▼ Rebaixar]"));

            Items.Add(new BuildItemDefinition(
                "pai_esc_suavizar", "Suavizar / Nivelar Platô",
                "Aplaina e suaviza a inclinação do terreno. Essencial para pistas de pouso e pátios.",
                BuildCategory.Paisagismo, "Esculpir Terra",
                StarterCareer.Aviator, DriverLicense.Aviation,
                7500, BuildActionType.TerrainTerraform, "smooth", "[▬ Nivelar]"));

            // Esculpir Água
            Items.Add(new BuildItemDefinition(
                "pai_agu_dragagem", "Dragagem Costeira & Fluvial",
                "Aprofunda o leito de rios ou enseadas permitindo a passagem de barcos de maior calado.",
                BuildCategory.Paisagismo, "Esculpir Água",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                15000, BuildActionType.WaterTerraform, "dredge", "[Dragagem]"));

            Items.Add(new BuildItemDefinition(
                "pai_agu_lago", "Criar Lago Artificial",
                "Escava bacia hidrográfica impermeabilizada com lâmina d'água permanente.",
                BuildCategory.Paisagismo, "Esculpir Água",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                22000, BuildActionType.WaterTerraform, "createlake", "[Lago Novo]"));

            // Pintar Solo
            Items.Add(new BuildItemDefinition(
                "pai_solo_asfalto", "Asfalto Rodoviário",
                "Pinta textura de asfalto asfáltico negro de alta durabilidade para rodovias.",
                BuildCategory.Paisagismo, "Pintar Solo",
                StarterCareer.Trucker, DriverLicense.Standard,
                2500, BuildActionType.GroundPaint, "10", "[Asfalto]"));

            Items.Add(new BuildItemDefinition(
                "pai_solo_concreto", "Concreto Reforçado",
                "Piso industrial de concreto armado que suporta empilhadeiras e aeronaves pesadas.",
                BuildCategory.Paisagismo, "Pintar Solo",
                StarterCareer.Aviator, DriverLicense.Heavy,
                3800, BuildActionType.GroundPaint, "9", "[Concreto]"));

            Items.Add(new BuildItemDefinition(
                "pai_solo_cascalho", "Cascalho de Rio / Brita",
                "Camada de cascalho e seixos para estradas vicinais e drenagem natural.",
                BuildCategory.Paisagismo, "Pintar Solo",
                StarterCareer.Farmer, DriverLicense.Standard,
                1200, BuildActionType.GroundPaint, "5", "[Cascalho]"));

            Items.Add(new BuildItemDefinition(
                "pai_solo_lama", "Lama & Barro Úmido",
                "Solo encharcado orgânico e lamaçal para caminhos rurais e áreas alagadas.",
                BuildCategory.Paisagismo, "Pintar Solo",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                800, BuildActionType.GroundPaint, "13", "[Lama]"));

            Items.Add(new BuildItemDefinition(
                "pai_solo_areia", "Areia de Praia & Dunas",
                "Areia dourada de granulação fina para faixas costeiras e orlas fluviais.",
                BuildCategory.Paisagismo, "Pintar Solo",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                1000, BuildActionType.GroundPaint, "4", "[Areia]"));

            Items.Add(new BuildItemDefinition(
                "pai_solo_grama", "Grama Lush Verdejante",
                "Restaura cobertura vegetal verde viçosa natural do bioma.",
                BuildCategory.Paisagismo, "Pintar Solo",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                500, BuildActionType.GroundPaint, "0", "[Grama]"));

            // Vegetação
            Items.Add(new BuildItemDefinition(
                "pai_veg_arvores", "Pincel de Árvores Nativas",
                "Planta agrupamentos de árvores nativas adaptadas à latitude da província.",
                BuildCategory.Paisagismo, "Vegetação",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                2400, BuildActionType.VegetationPaint, "trees", "[Árvores]"));

            Items.Add(new BuildItemDefinition(
                "pai_veg_arbustos", "Arbustos & Moitas",
                "Povoa a margem de estradas e propriedades com arbustos e folhagens densas.",
                BuildCategory.Paisagismo, "Vegetação",
                StarterCareer.Farmer, DriverLicense.Agriculture,
                1200, BuildActionType.VegetationPaint, "bushes", "[Arbustos]"));

            Items.Add(new BuildItemDefinition(
                "pai_veg_mato_alto", "Mato Alto Silvestre",
                "Mato alto e vegetação campestre para enriquecer a imersão de pastagens.",
                BuildCategory.Paisagismo, "Vegetação",
                StarterCareer.Farmer, DriverLicense.Livestock,
                800, BuildActionType.VegetationPaint, "tallgrass", "[Mato Alto]"));

            Items.Add(new BuildItemDefinition(
                "pai_veg_aquatica", "Vegetação Aquática & Algas",
                "Macrófitas e algas para compor as margens de rios, lagos e orlas litorâneas.",
                BuildCategory.Paisagismo, "Vegetação",
                StarterCareer.Fisherman, DriverLicense.Maritime,
                1600, BuildActionType.VegetationPaint, "aquatic", "[Algas/Rios]"));
        }

        public static List<string> GetSubcategories(BuildCategory category)
        {
            var set = new List<string>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].category == category && !set.Contains(Items[i].subcategory))
                {
                    set.Add(Items[i].subcategory);
                }
            }
            return set;
        }

        public static List<BuildItemDefinition> GetItems(BuildCategory category, string subcategory)
        {
            var res = new List<BuildItemDefinition>();
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].category == category && (string.IsNullOrEmpty(subcategory) || Items[i].subcategory == subcategory))
                {
                    res.Add(Items[i]);
                }
            }
            return res;
        }
    }
}
