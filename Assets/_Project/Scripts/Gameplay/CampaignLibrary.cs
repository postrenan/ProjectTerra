using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Fonte das campanhas do modo carreira do Roger Mendes. Cada carreira traz os beats
    /// na sua ordem adaptativa (a jogatina inicial primeiro, depois as novidades), cobrindo
    /// as 4 modalidades: Fazendeiro, Transportador, Aviador e Pescador.
    ///
    /// As campanhas são montadas em código (robusto e garantido em build). O design e os
    /// textos completos vivem em docs/CAREER_MODE_STORY_TEXTS.md.
    /// </summary>
    public static class CampaignLibrary
    {
        public static CareerCampaign LoadFor(StarterCareer career)
        {
            switch (career)
            {
                case StarterCareer.Farmer:    return BuildFarmer();
                case StarterCareer.Trucker:   return BuildTrucker();
                case StarterCareer.Aviator:   return BuildAviator();
                case StarterCareer.Fisherman: return BuildFisherman();
                default:                      return BuildFarmer();
            }
        }

        // ---------------------------------------------------------------- helpers

        private static StoryBeat C(string id, string title, string desc, string cargo,
            float kg, long money, int rep, DriverLicense lic, string pickup, string delivery,
            BeatTrigger trigger = BeatTrigger.Immediate, float triggerValue = 0)
        {
            return new StoryBeat
            {
                id = id, type = StoryBeatType.Contract, trigger = trigger, triggerValue = triggerValue,
                contract = new ContractData
                {
                    title = title, description = desc, cargoType = cargo, cargoWeightKg = kg,
                    rewardMoney = money, rewardReputation = rep, requiredLicense = lic,
                    pickupAnchor = pickup, deliveryAnchor = delivery
                }
            };
        }

        private static StoryBeat Sms(string id, string contactId, string name, string portrait,
            BeatTrigger trigger, float triggerValue, params string[] lines)
        {
            return new StoryBeat
            {
                id = id, type = StoryBeatType.TextMessage, trigger = trigger, triggerValue = triggerValue,
                dialogue = new DialogueData { contactId = contactId, contactName = name, portrait = portrait,
                    channel = DialogueChannel.Sms, lines = lines }
            };
        }

        private static StoryBeat Call(string id, string contactId, string name, string portrait,
            bool urgent, BeatTrigger trigger, float triggerValue, params string[] lines)
        {
            return new StoryBeat
            {
                id = id, type = StoryBeatType.PhoneCall, trigger = trigger, triggerValue = triggerValue,
                dialogue = new DialogueData { contactId = contactId, contactName = name, portrait = portrait,
                    channel = DialogueChannel.Call, urgent = urgent, lines = lines }
            };
        }

        private static StoryBeat Talk(string id, string contactId, string name, string portrait,
            BeatTrigger trigger, float triggerValue, params string[] lines)
        {
            return new StoryBeat
            {
                id = id, type = StoryBeatType.NpcTalk, trigger = trigger, triggerValue = triggerValue,
                dialogue = new DialogueData { contactId = contactId, contactName = name, portrait = portrait,
                    channel = DialogueChannel.WorldTalk, lines = lines }
            };
        }

        private static StoryBeat Grant(string id, DriverLicense lic,
            BeatTrigger trigger = BeatTrigger.OnContractComplete)
        {
            return new StoryBeat { id = id, type = StoryBeatType.GrantLicense, trigger = trigger, grantLicense = lic };
        }

        private static StoryBeat Unlock(string id, string label, BeatTrigger trigger, float triggerValue)
        {
            return new StoryBeat { id = id, type = StoryBeatType.UnlockAct, trigger = trigger,
                triggerValue = triggerValue, unlockActLabel = label };
        }

        private static CareerCampaign Make(StarterCareer career, List<StoryBeat> beats)
            => new CareerCampaign { career = career, beats = beats.ToArray() };

        // ============================================================ 🌾 FAZENDEIRO
        private static CareerCampaign BuildFarmer()
        {
            var b = new List<StoryBeat>
            {
                // --- Ato 0 ---
                Sms("FA_A0_LETTER", "mateus", "Tio Mateus", "⚓", BeatTrigger.Immediate, 0,
                    "Roger, se você tá lendo isso, eu já era.",
                    "A fazenda é sua agora. Não é muito: terra, um trator velho e umas dívidas.",
                    "Procura a Dona Íris, o Vega e o pessoal do Sítio Aurora. Eles me devem favores."),
                C("FA_A0_KEYS", "As Chaves", "Leve a caixa de pertences do tio Mateus da sede até o Mercado Municipal e conheça a cidade.",
                    "Pertences da Família", 120, 5000, 10, DriverLicense.None, "base", "town"),
                Grant("FA_A0_GRANT", DriverLicense.Agriculture),

                // --- Ato I: Raízes (agro) ---
                C("FA_A1_HARVEST", "Primeira Safra: Grãos para a Cooperativa",
                    "Engate a carga de soja no trator e leve até os Silos da Cooperativa Central.",
                    "Soja em Grãos", 1200, 18500, 15, DriverLicense.Agriculture, "base", "town"),
                Call("FA_A1_COOP", "system", "Cooperativa", "🌾", false, BeatTrigger.OnContractComplete, 0,
                    "Safra pesada na balança, Roger. Dinheiro na conta.",
                    "Você dá conta de volume maior? Temos um contrato grande chegando."),
                C("FA_A1_RAIN", "Janela da Chuva",
                    "O temporal chega em horas. Entregue o lote restante antes da chuva estragar o grão.",
                    "Soja (lote 2)", 1400, 21000, 18, DriverLicense.Agriculture, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                C("FA_A1_BIG", "O Contrato da Cooperativa",
                    "Volume alto demais para o trator em uma viagem. Entregue a safra consolidada à cidade.",
                    "Safra Consolidada", 4000, 28000, 25, DriverLicense.Agriculture, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("FA_A1_IRIS", "iris", "Dona Íris", "🚚", false, BeatTrigger.OnContractComplete, 0,
                    "Soube que você tá crescendo, menino. Cara do Mateus.",
                    "Grão bom não espera trator. Passa na minha garagem — tenho uma rota e um caminhão pra você."),
                Unlock("FA_A2_UNLOCK", "Ato II — A Estrada", BeatTrigger.OnContractComplete, 0),

                // --- Ato II: A Estrada ---
                Talk("FA_A2_IRIS", "iris", "Dona Íris", "🚚", BeatTrigger.Immediate, 0,
                    "O caminhão tá ali. A parcela vence todo mês, não esquece.",
                    "Financia ele no app Finanças e rode certo que a estrada paga."),
                C("FA_A2_FIRST", "A Transportadora",
                    "Financie seu primeiro caminhão e faça o frete de estreia: peças ao Centro de Distribuição.",
                    "Peças Industriais", 2500, 30000, 20, DriverLicense.Standard, "base", "town"),
                C("FA_A2_CHAIN", "Cadeia de Suprimentos",
                    "Viagem longa com paradas. Olho no combustível e no desgaste do caminhão.",
                    "Suprimentos Consolidados", 6000, 45000, 30, DriverLicense.Heavy, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                C("FA_A2_IMPOSSIBLE", "A Carga Impossível",
                    "Equipamento caríssimo, prazo curtíssimo entre cidades distantes. Por estrada não dá no prazo.",
                    "Equipamento Hospitalar", 1800, 60000, 35, DriverLicense.HighValue, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("FA_A2_VEGA", "vega", "Comandante Vega", "✈️", true, BeatTrigger.OnContractComplete, 0,
                    "Ouvi no rádio que você tem carga boa e pouco tempo.",
                    "Estrada tem limite, garoto. O céu não. Me procura no campo de pouso."),
                Unlock("FA_A3_UNLOCK", "Ato III — O Céu", BeatTrigger.OnContractComplete, 0),

                // --- Ato III: O Céu ---
                C("FA_A3_BREVE", "Brevê",
                    "Pague o curso e cumpra o voo de avaliação: decole, contorne a antena e pouse na pista.",
                    "Instrutor de Voo", 90, 4000, 15, DriverLicense.None, "airport", "airport"),
                Grant("FA_A3_GRANT", DriverLicense.Aviation),
                C("FA_A3_BRIDGE", "Ponte Aérea",
                    "Cargas leves e urgentes entre cidades distantes. Vigie a autonomia e o clima.",
                    "Encomendas Expressas", 300, 75000, 30, DriverLicense.Aviation, "airport", "town",
                    BeatTrigger.OnContractComplete, 0),
                C("FA_A3_LINK", "O Elo Perdido",
                    "Um restaurante premium quer frutos do mar frescos por via aérea. A margem está na origem.",
                    "Pescado Fresco Premium", 400, 110000, 40, DriverLicense.Refrigerated, "docks", "airport",
                    BeatTrigger.OnContractComplete, 0),
                Call("FA_A3_MARINA", "marina", "Marina", "🎣", false, BeatTrigger.OnContractComplete, 0,
                    "Você voa o peixe, mas compra caro de terceiros.",
                    "O ouro tá na água, não no ar. Vem comigo pro cais que eu te mostro."),
                Unlock("FA_A4_UNLOCK", "Ato IV — A Água", BeatTrigger.OnContractComplete, 0),

                // --- Ato IV: A Água ---
                C("FA_A4_CAIS", "De Volta ao Cais",
                    "Financie a embarcação e faça a primeira captura: siga à boia, pesque a cota e descarregue.",
                    "Pescado Fresco", 850, 40000, 25, DriverLicense.None, "docks", "town"),
                Grant("FA_A4_GRANT", DriverLicense.Maritime),
                C("FA_A4_COLD", "Cadeia do Frescor",
                    "Pesque, transfira ao caminhão frigorífico até o aeroporto e despache no avião à capital.",
                    "Lote Premium Refrigerado", 1100, 130000, 45, DriverLicense.Maritime, "docks", "airport",
                    BeatTrigger.OnContractComplete, 0),
                C("FA_A4_STORM", "Tempestade no Mar",
                    "A maior cota da temporada, com frente fria chegando. Pesque e volte antes da tempestade.",
                    "Captura Recorde", 1600, 95000, 50, DriverLicense.Maritime, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                Unlock("FA_A5_UNLOCK", "Ato V — O Grupo", BeatTrigger.OnContractComplete, 0),

                // --- Ato V: O Grupo ---
                Call("FA_A5_CONF", "iris", "Íris, Vega e Marina", "📞", false, BeatTrigger.Immediate, 0,
                    "Um ano atrás você era um moleque com um trator velho.",
                    "Hoje a região inteira depende das suas rodas, cascos e asas. Fecha esse contrato."),
                C("FA_A5_INTERMODAL", "Operação Intermodal",
                    "O megacontrato só fecha com as 4 pontas: colha, pesque, consolide por estrada e despache por ar. Use [Tab] para alternar os veículos.",
                    "Remessa Integrada Regional", 5000, 200000, 80,
                    DriverLicense.Agriculture | DriverLicense.Maritime | DriverLicense.Aviation, "base", "airport"),
                C("FA_A5_LEGACY", "O Legado",
                    "Quite as dívidas e registre o Grupo Logístico Mendes na prefeitura.",
                    "Escritura do Grupo Mendes", 0, 0, 100, DriverLicense.None, "town", "town",
                    BeatTrigger.OnContractComplete, 0),
                Sms("FA_END", "mateus", "Tio Mateus", "⚓", BeatTrigger.OnContractComplete, 0,
                    "Deu certo, tio. Não é mais 'umas dívidas e um nome'. É um grupo.",
                    "E ainda tem mundo lá fora pra conquistar."),
            };
            return Make(StarterCareer.Farmer, b);
        }

        // ============================================================ 🚛 TRANSPORTADOR
        // Ordem adaptativa: já domina a estrada → novidade vem da água, do céu e do agro.
        private static CareerCampaign BuildTrucker()
        {
            var b = new List<StoryBeat>
            {
                Sms("TR_A0_LETTER", "mateus", "Tio Mateus", "⚓", BeatTrigger.Immediate, 0,
                    "Roger, a transportadora é sua. Um caminhão e muita estrada pela frente.",
                    "Procura a Íris no pátio. Ela ensina o resto."),
                C("TR_A0_KEYS", "As Chaves", "Leve os pertences do tio Mateus da garagem ao Centro de Distribuição.",
                    "Pertences da Família", 120, 5000, 10, DriverLicense.None, "base", "town"),
                Grant("TR_A0_GRANT", DriverLicense.Standard),

                C("TR_A1_FIRST", "Frete Rodoviário: Carga de Maquinário",
                    "Carregue o palete de peças e entregue no Centro Comercial da Cidade.",
                    "Peças Industriais", 2500, 24000, 20, DriverLicense.Standard, "base", "town"),
                C("TR_A1_DOUBLE", "Rota Dupla",
                    "Dois pontos de coleta, uma entrega. Planeje a rota no mapa tático [M].",
                    "Carga Fracionada", 3200, 30000, 22, DriverLicense.Heavy, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("TR_A1_MARINA", "marina", "Marina", "🎣", false, BeatTrigger.OnContractComplete, 0,
                    "Transportador bom é raro por aqui. Soube do Mateus, sinto muito.",
                    "Tenho peixe demais e barco de menos. Se topar pescar comigo, a margem é sua."),
                Unlock("TR_A2_UNLOCK", "Ato II — A Água", BeatTrigger.OnContractComplete, 0),

                C("TR_A2_BOAT", "De Volta ao Cais",
                    "Financie a embarcação e faça a primeira captura na boia oceânica.",
                    "Pescado Fresco", 850, 40000, 25, DriverLicense.None, "docks", "town"),
                Grant("TR_A2_GRANT", DriverLicense.Maritime),
                C("TR_A2_COLD", "Corrente Fria",
                    "Tire a licença frigorífica e leve a captura gelada do cais à cidade sem quebrar o frio.",
                    "Perecíveis Refrigerados", 5000, 48000, 30, DriverLicense.Refrigerated, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                C("TR_A2_URGENT", "Carga que Não Espera",
                    "Restaurante estrelado na capital, prazo de horas. Por terra o peixe chega velho demais.",
                    "Lote Premium Urgente", 1500, 62000, 35, DriverLicense.HighValue, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("TR_A2_VEGA", "vega", "Comandante Vega", "✈️", true, BeatTrigger.OnContractComplete, 0,
                    "Carga que não espera é especialidade minha.",
                    "Me acha no campo de pouso. Tá na hora de você subir."),
                Unlock("TR_A3_UNLOCK", "Ato III — O Céu", BeatTrigger.OnContractComplete, 0),

                C("TR_A3_BREVE", "Brevê",
                    "Pague o curso e faça o voo de prova com o Vega: decole, contorne a antena e pouse suave.",
                    "Instrutor de Voo", 90, 4000, 15, DriverLicense.None, "airport", "airport"),
                Grant("TR_A3_GRANT", DriverLicense.Aviation),
                C("TR_A3_BRIDGE", "Ponte do Pescado",
                    "Entregas aéreas de frutos do mar entre o litoral e o interior. Vigie autonomia e clima.",
                    "Frutos do Mar Expressos", 350, 78000, 30, DriverLicense.Aviation | DriverLicense.Refrigerated,
                    "airport", "town", BeatTrigger.OnContractComplete, 0),
                C("TR_A3_GOLD", "O Cliente de Ouro",
                    "Uma rede quer fornecimento aéreo fixo, com rastreabilidade da origem. Falta dominar a terra.",
                    "Pescado Premium Refrigerado", 420, 115000, 40, DriverLicense.Aviation | DriverLicense.Refrigerated,
                    "docks", "airport", BeatTrigger.OnContractComplete, 0),
                Call("TR_A3_AURORA", "aurora", "Sítio Aurora", "🌾", false, BeatTrigger.OnContractComplete, 0,
                    "O tio Mateus comprava nossos grãos pra ração e isca.",
                    "Se quer fechar a cadeia, vem ver a lavoura."),
                Unlock("TR_A4_UNLOCK", "Ato IV — A Terra", BeatTrigger.OnContractComplete, 0),

                C("TR_A4_FARM", "O Sítio Aurora",
                    "Tire a licença agrícola e reative a parceria do tio: colha os grãos e leve à base.",
                    "Grãos para Ração", 1200, 42000, 25, DriverLicense.None, "base", "town"),
                Grant("TR_A4_GRANT", DriverLicense.Agriculture),
                C("TR_A4_FIELD", "Da Terra ao Prato",
                    "Una as quatro pontas: grão do sítio, peixe no porão, caminhão frigorífico e avião à capital.",
                    "Insumo + Pescado Integrado", 3500, 135000, 45,
                    DriverLicense.Agriculture | DriverLicense.Refrigerated | DriverLicense.Aviation,
                    "base", "airport", BeatTrigger.OnContractComplete, 0),
                Unlock("TR_A5_UNLOCK", "Ato V — O Grupo", BeatTrigger.OnContractComplete, 0),

                C("TR_A5_INTERMODAL", "Operação Intermodal",
                    "Colha, pesque, consolide por estrada e despache por ar num ciclo só. Alterne com [Tab].",
                    "Remessa Integrada Regional", 5000, 200000, 80,
                    DriverLicense.Agriculture | DriverLicense.Maritime | DriverLicense.Aviation, "base", "airport"),
                C("TR_A5_LEGACY", "O Legado",
                    "Quite tudo e registre o Grupo Logístico Mendes na prefeitura.",
                    "Escritura do Grupo Mendes", 0, 0, 100, DriverLicense.None, "town", "town",
                    BeatTrigger.OnContractComplete, 0),
                Sms("TR_END", "mateus", "Tio Mateus", "⚓", BeatTrigger.OnContractComplete, 0,
                    "De um caminhão a uma frota inteira, tio. Deu certo."),
            };
            return Make(StarterCareer.Trucker, b);
        }

        // ============================================================ 🛩️ AVIADOR
        // Ordem adaptativa: domina o céu → novidade vem da estrada, da água e do agro.
        private static CareerCampaign BuildAviator()
        {
            var b = new List<StoryBeat>
            {
                Sms("AV_A0_LETTER", "mateus", "Tio Mateus", "⚓", BeatTrigger.Immediate, 0,
                    "Roger, o hangar é seu. Um monomotor e contas de combustível que não param.",
                    "O Vega era meu copiloto. Confia nele."),
                C("AV_A0_KEYS", "As Chaves", "Leve os pertences do tio Mateus do hangar ao Mercado Municipal.",
                    "Pertences da Família", 120, 5000, 10, DriverLicense.None, "airport", "town"),
                Grant("AV_A0_GRANT", DriverLicense.Aviation),

                C("AV_A1_INSPECT", "Voo de Inspeção & Encomenda Aérea",
                    "Decole, sobrevoe a antena repetidora e pouse na pista para entregar o malote postal.",
                    "Malote Postal Expresso", 200, 32000, 25, DriverLicense.Aviation, "airport", "airport"),
                C("AV_A1_RESCUE", "Resgate de Suprimentos",
                    "Entregue remédios a um ponto isolado sem estrada. Só o avião chega lá.",
                    "Suprimentos Médicos", 260, 36000, 22, DriverLicense.Aviation, "airport", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("AV_A1_IRIS", "iris", "Dona Íris", "🚚", false, BeatTrigger.OnContractComplete, 0,
                    "Voar sozinho não paga o combustível, aviador. Soube do Mateus.",
                    "Tenho carga terrestre barata pra complementar sua renda. Passa no pátio."),
                Unlock("AV_A2_UNLOCK", "Ato II — A Estrada", BeatTrigger.OnContractComplete, 0),

                C("AV_A2_TRUCK", "A Transportadora",
                    "Financie um caminhão e complemente a renda com frete rodoviário. A parcela vence todo mês.",
                    "Peças Industriais", 2500, 30000, 20, DriverLicense.Standard, "base", "town"),
                C("AV_A2_HEAVY", "Cadeia de Suprimentos",
                    "Rota longa com paradas. Combustível e manutenção do caminhão contam.",
                    "Suprimentos Consolidados", 6000, 45000, 30, DriverLicense.Heavy, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("AV_A2_MARINA", "marina", "Marina", "🎣", false, BeatTrigger.OnContractComplete, 0,
                    "Você voa e roda, mas compra o peixe caro de terceiros.",
                    "A margem tá na origem. Vem pescar comigo."),
                Unlock("AV_A3_UNLOCK", "Ato III — A Água", BeatTrigger.OnContractComplete, 0),

                C("AV_A3_BOAT", "De Volta ao Cais",
                    "Financie a embarcação e faça a primeira captura na boia oceânica.",
                    "Pescado Fresco", 850, 40000, 25, DriverLicense.None, "docks", "town"),
                Grant("AV_A3_GRANT", DriverLicense.Maritime),
                C("AV_A3_COLD", "Cadeia do Frescor",
                    "Pesque, leve ao aeroporto no caminhão frigorífico e despache no seu avião à capital.",
                    "Lote Premium Refrigerado", 1100, 130000, 45,
                    DriverLicense.Maritime | DriverLicense.Refrigerated | DriverLicense.Aviation,
                    "docks", "airport", BeatTrigger.OnContractComplete, 0),
                Call("AV_A3_AURORA", "aurora", "Sítio Aurora", "🌾", false, BeatTrigger.OnContractComplete, 0,
                    "O tio Mateus fornecia nosso grão. A cadeia fecha na lavoura.",
                    "Vem ver o Sítio Aurora."),
                Unlock("AV_A4_UNLOCK", "Ato IV — A Terra", BeatTrigger.OnContractComplete, 0),

                C("AV_A4_FARM", "O Sítio Aurora",
                    "Tire a licença agrícola e colha os grãos da parceria do tio.",
                    "Grãos para Ração", 1200, 42000, 25, DriverLicense.None, "base", "town"),
                Grant("AV_A4_GRANT", DriverLicense.Agriculture),
                C("AV_A4_FIELD", "Da Terra ao Céu",
                    "Una as quatro pontas: grão, peixe, caminhão frigorífico e avião. Prova de conceito do grupo.",
                    "Cadeia Integrada", 3500, 135000, 45,
                    DriverLicense.Agriculture | DriverLicense.Refrigerated | DriverLicense.Aviation,
                    "base", "airport", BeatTrigger.OnContractComplete, 0),
                Unlock("AV_A5_UNLOCK", "Ato V — O Grupo", BeatTrigger.OnContractComplete, 0),

                C("AV_A5_INTERMODAL", "Operação Intermodal",
                    "Colha, pesque, consolide por estrada e despache por ar num ciclo só. Alterne com [Tab].",
                    "Remessa Integrada Regional", 5000, 200000, 80,
                    DriverLicense.Agriculture | DriverLicense.Maritime | DriverLicense.Aviation, "base", "airport"),
                C("AV_A5_LEGACY", "O Legado",
                    "Quite tudo e registre o Grupo Logístico Mendes na prefeitura.",
                    "Escritura do Grupo Mendes", 0, 0, 100, DriverLicense.None, "town", "town",
                    BeatTrigger.OnContractComplete, 0),
                Sms("AV_END", "mateus", "Tio Mateus", "⚓", BeatTrigger.OnContractComplete, 0,
                    "O hangar não voa mais sozinho, tio. Tem uma frota inteira atrás dele. Deu certo."),
            };
            return Make(StarterCareer.Aviator, b);
        }

        // ============================================================ 🎣 PESCADOR
        // Ordem adaptativa: domina a água → novidade vem da estrada, do céu e do agro.
        private static CareerCampaign BuildFisherman()
        {
            var b = new List<StoryBeat>
            {
                Sms("FI_A0_LETTER", "mateus", "Tio Mateus", "⚓", BeatTrigger.Immediate, 0,
                    "Roger, se você tá lendo isso, eu já era.",
                    "A Marujo I é sua. Ela já viu mares piores que suas dúvidas.",
                    "Procura a Íris, o Vega e o pessoal do Sítio Aurora. Eles me devem favores."),
                C("FI_A0_KEYS", "As Chaves",
                    "Leve os pertences do tio Mateus da traineira ao Mercado do Cais e conheça o entreposto.",
                    "Rede e Pertences", 120, 5000, 10, DriverLicense.None, "docks", "town"),
                Grant("FI_A0_GRANT", DriverLicense.Maritime),
                Sms("FI_A0_IRIS", "iris", "Dona Íris", "🚚", BeatTrigger.AfterDelaySeconds, 4,
                    "Oi Roger. Soube do Mateus. Sinto muito.",
                    "Quando quiser escoar peixe de verdade, me liga. Tenho caminhão parado."),

                C("FI_A1_FIRST", "Primeira Pesca",
                    "Navegue até a boia oceânica, pesque a cota e descarregue no Entreposto do Mercado.",
                    "Pescado Fresco", 850, 21000, 18, DriverLicense.Maritime, "docks", "town"),
                C("FI_A1_MARKET", "Maré e Mercado",
                    "O preço despencou no cais. A cidade grande paga o dobro — entregue o lote antes de perder o frescor.",
                    "Pescado (lote premium)", 900, 24000, 20, DriverLicense.Maritime, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                C("FI_A1_CARDUME", "Captura Além da Cota",
                    "Cardume gigante. Sua traineira não escoa tudo a tempo sozinha — você precisa de rodas.",
                    "Supercaptura", 1800, 27000, 25, DriverLicense.Maritime, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("FI_A1_IRIS", "iris", "Dona Íris", "🚚", false, BeatTrigger.OnContractComplete, 0,
                    "Roger! Vi que você encheu o porão hoje. Peixe parado é peixe perdido, menino.",
                    "Aparece na minha garagem — tenho uma rota e um caminhão pra você."),
                Unlock("FI_A2_UNLOCK", "Ato II — A Estrada", BeatTrigger.OnContractComplete, 0),

                Talk("FI_A2_IRIS", "iris", "Dona Íris", "🚚", BeatTrigger.Immediate, 0,
                    "Então é você o sobrinho do Mateus. Cara dele.",
                    "O caminhão tá ali. A parcela vence todo mês, não esquece. Rode certo que a estrada paga."),
                C("FI_A2_FIRST", "A Garagem da Íris",
                    "Financie seu primeiro caminhão e leve a captura gelada do cais ao Centro de Distribuição.",
                    "Pescado + Gelo", 2500, 32000, 20, DriverLicense.Standard, "docks", "town"),
                C("FI_A2_COLD", "Corrente Fria",
                    "Tire a licença frigorífica e faça a rota tripla sem quebrar a cadeia de frio.",
                    "Perecíveis Consolidados", 5000, 48000, 30, DriverLicense.Refrigerated, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                C("FI_A2_URGENT", "Carga que Não Espera",
                    "Restaurante estrelado na capital, prazo de horas. Por terra o peixe chega velho demais.",
                    "Lote Premium Urgente", 1500, 62000, 35, DriverLicense.HighValue, "docks", "town",
                    BeatTrigger.OnContractComplete, 0),
                Call("FI_A2_VEGA", "vega", "Comandante Vega", "✈️", true, BeatTrigger.OnContractComplete, 0,
                    "Pescador virou caminhoneiro, e agora quer o céu?",
                    "Me acha no campo de pouso. Carga que não espera é especialidade minha."),
                Unlock("FI_A3_UNLOCK", "Ato III — O Céu", BeatTrigger.OnContractComplete, 0),

                C("FI_A3_BREVE", "Brevê do Mar pro Ar",
                    "Pague o curso e faça o voo de prova com o Vega: decole, contorne a antena e pouse suave.",
                    "Instrutor de Voo", 90, 4000, 15, DriverLicense.None, "airport", "airport"),
                Grant("FI_A3_GRANT", DriverLicense.Aviation),
                C("FI_A3_BRIDGE", "Ponte do Pescado",
                    "Entregas aéreas de frutos do mar entre o litoral e o interior. Vigie autonomia e clima.",
                    "Frutos do Mar Expressos", 350, 78000, 30, DriverLicense.Aviation | DriverLicense.Refrigerated,
                    "airport", "town", BeatTrigger.OnContractComplete, 0),
                C("FI_A3_GOLD", "O Cliente de Ouro",
                    "Uma rede quer fornecimento aéreo fixo, com rastreabilidade. Falta dominar a origem em terra.",
                    "Pescado Premium Refrigerado", 420, 115000, 40, DriverLicense.Aviation | DriverLicense.Refrigerated,
                    "docks", "airport", BeatTrigger.OnContractComplete, 0),
                Call("FI_A3_AURORA", "aurora", "Sítio Aurora", "🌾", false, BeatTrigger.OnContractComplete, 0,
                    "O tio Mateus comprava nossos grãos pra isca e ração. A gente ainda planta.",
                    "Se quer fechar a cadeia, vem ver a lavoura."),
                Unlock("FI_A4_UNLOCK", "Ato IV — A Terra", BeatTrigger.OnContractComplete, 0),

                C("FI_A4_FARM", "O Sítio Aurora",
                    "Tire a licença agrícola e reative a parceria do tio: colha os grãos e leve à base.",
                    "Grãos para Ração/Isca", 1200, 42000, 25, DriverLicense.None, "base", "town"),
                Grant("FI_A4_GRANT", DriverLicense.Agriculture),
                C("FI_A4_FIELD", "Da Terra ao Prato",
                    "Una as quatro pontas da SUA cadeia: grão do sítio, peixe no porão, caminhão frigorífico e avião.",
                    "Insumo + Pescado Integrado", 3500, 135000, 45,
                    DriverLicense.Agriculture | DriverLicense.Refrigerated | DriverLicense.Aviation,
                    "base", "airport", BeatTrigger.OnContractComplete, 0),
                C("FI_A4_DOUBLE", "Safra e Maré",
                    "Colheita e cota de pesca na mesma semana, com tempo virando. Orquestre as duas produções.",
                    "Dupla Colheita", 4000, 98000, 50, DriverLicense.Agriculture, "base", "town",
                    BeatTrigger.OnContractComplete, 0),
                Unlock("FI_A5_UNLOCK", "Ato V — O Grupo", BeatTrigger.OnContractComplete, 0),

                Call("FI_A5_CONF", "iris", "Íris, Vega e Aurora", "📞", false, BeatTrigger.Immediate, 0,
                    "Começou com uma traineira enferrujada. Olha você agora, moleque.",
                    "Fecha esse contrato e o nome Mendes entra pra história do litoral."),
                C("FI_A5_INTERMODAL", "Operação Maré Cheia",
                    "Colha no Aurora, pesque no mar, consolide por estrada e despache por ar num ciclo só. Alterne com [Tab].",
                    "Remessa Integrada Regional", 5000, 200000, 80,
                    DriverLicense.Agriculture | DriverLicense.Maritime | DriverLicense.Aviation, "docks", "airport"),
                C("FI_A5_LEGACY", "O Legado do Tio Mateus",
                    "Quite tudo e registre o Grupo Mendes. A traineira falida virou o maior operador multimodal do litoral.",
                    "Escritura do Grupo", 0, 0, 100, DriverLicense.None, "town", "town",
                    BeatTrigger.OnContractComplete, 0),
                Sms("FI_END", "mateus", "Tio Mateus", "⚓", BeatTrigger.OnContractComplete, 0,
                    "A Marujo I ainda flutua, tio. Só que agora ela não pesca sozinha — tem uma frota atrás dela.",
                    "Deu certo."),
            };
            return Make(StarterCareer.Fisherman, b);
        }
    }
}
