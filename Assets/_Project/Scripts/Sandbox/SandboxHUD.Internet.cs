using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Portais de serviços do TerraBrowser moderno:
    /// - Gazeta Regional (Revista/jornal digital contemporâneo clean)
    /// - AgroBolsa (Dashboard de cotações financeiras com design minimalista)
    /// - TerraBank (Internet Banking moderno estilo fintech com extrato e empréstimos)
    /// - Rede TerraFuel (Hub de energia e tele-abastecimento web)
    /// - Desafios da Campanha (Dashboard de metas e conquistas com recompensas)
    /// - Guia de Rodovias (Tabela moderna de rotas, condições e socorro mecânico)
    /// </summary>
    public partial class SandboxHUD
    {
        private bool embeddedInBrowser = false;

        private class ServiceSite
        {
            public string id;
            public string title;
            public string subtitle;
            public string favLabel;
            public string tagline;
            public string url;
            public Color brand;
        }

        private static readonly ServiceSite[] ServiceSites =
        {
            new ServiceSite
            {
                id = "noticias",
                title = "Gazeta Regional",
                subtitle = "Portal de notícias e economia regional",
                favLabel = "Notícias",
                url = "https://gazeta.terranet.com/",
                brand = new Color(0.20f, 0.35f, 0.60f),
                tagline = "Cobertura diária de safra, fretes, infraestrutura e agronegócio."
            },
            new ServiceSite
            {
                id = "mercado",
                title = "AgroBolsa Mercantil",
                subtitle = "Terminal financeiro de commodities e grãos",
                favLabel = "Cotações",
                url = "https://agrobolsa.terranet.com/",
                brand = new Color(0.75f, 0.55f, 0.12f),
                tagline = "Preços atualizados em tempo real de soja, milho, gado e diesel."
            },
            new ServiceSite
            {
                id = "banco",
                title = "TerraBank",
                subtitle = "Internet banking, crédito rural e gestão financeira",
                favLabel = "Banco",
                url = "https://bank.terranet.com/",
                brand = new Color(0.08f, 0.50f, 0.35f),
                tagline = "Gestão de despesas de frota, pagamento de parcelas e linhas de financiamento."
            },
            new ServiceSite
            {
                id = "posto",
                title = "Rede TerraFuel",
                subtitle = "Abastecimento e conveniência para transporte",
                favLabel = "Posto",
                url = "https://fuel.terranet.com/",
                brand = new Color(0.85f, 0.35f, 0.12f),
                tagline = "Preços oficiais de combustíveis e tele-abastecimento de frotas."
            },
            new ServiceSite
            {
                id = "metas",
                title = "Metas da Campanha",
                subtitle = "Programa regional de metas e premiações",
                favLabel = "Metas",
                url = "https://desafios.terranet.com/",
                brand = new Color(0.45f, 0.25f, 0.70f),
                tagline = "Alcance metas operacionais e desbloqueie recompensas em dinheiro e prestígio."
            },
            new ServiceSite
            {
                id = "rodovias",
                title = "Guia de Rodovias",
                subtitle = "Rotas, distâncias e assistência ao motorista",
                favLabel = "Rodovias",
                url = "https://rotas.terranet.com/",
                brand = new Color(0.18f, 0.40f, 0.65f),
                tagline = "Quadro de distâncias, condições do pavimento e telefones de emergência."
            },
        };

        private ServiceSite FindService(string id)
        {
            foreach (var s in ServiceSites) if (s.id == id) return s;
            return null;
        }

        private void DrawServicePage(Rect page, ServiceSite s)
        {
            if (s == null) return;

            float maxContainerW = Mathf.Min(1160f, page.width - 48f);
            float startX = page.x + (page.width - maxContainerW) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, page.y + 16f, maxContainerW, page.height - 24f));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            // Banner Clean Superior
            Rect header = GUILayoutUtility.GetRect(maxContainerW, 80f);
            Fill(header, UiCardBg);
            DrawOutline(header, UiBorderLight);

            Fill(new Rect(header.x, header.y, 4f, header.height), s.brand);

            GUI.Label(new Rect(header.x + 20f, header.y + 14f, header.width - 40f, 26f), s.title, brPageH1Style);
            GUI.Label(new Rect(header.x + 20f, header.y + 42f, header.width - 40f, 20f), $"{s.subtitle}  •  {s.tagline}", brBodyStyle);

            GUILayout.Space(20);

            switch (s.id)
            {
                case "noticias": DrawModernNewsSite(maxContainerW); break;
                case "mercado": DrawModernMarketSite(maxContainerW); break;
                case "banco": DrawModernBankSite(maxContainerW); break;
                case "posto": DrawModernFuelSite(maxContainerW); break;
                case "metas": DrawModernGoalsSite(maxContainerW); break;
                case "rodovias": DrawModernRoadsSite(maxContainerW); break;
            }

            GUILayout.Space(24);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // =========================================================================
        // 1. GAZETA REGIONAL — JORNAL DIGITAL CLEAN
        // =========================================================================
        private void DrawModernNewsSite(float w)
        {
            var save = CurrentSave;
            string reg = save != null ? save.regionName : "a região";
            string country = save != null ? save.countryName : "o país";
            long day = save != null ? save.gameDay : 1;

            // Manchete Principal (Hero Card)
            Rect hero = GUILayoutUtility.GetRect(w, 140f);
            Fill(hero, UiCardBg);
            DrawOutline(hero, UiBorderLight);

            float hx = hero.x + 20f;
            GUI.Label(new Rect(hx, hero.y + 14f, hero.width - 40f, 20f), $"DESTAQUE DO DIA {day}  •  {reg.ToUpper()}", brSmallStyle);
            GUI.Label(new Rect(hx, hero.y + 36f, hero.width - 40f, 26f),
                $"Safra recorde movimenta o setor de transportes e logística em {country}", brPageH1Style);
            GUI.Label(new Rect(hx, hero.y + 68f, hero.width - 40f, 56f),
                $"Armazéns e cooperativas da região operam em capacidade máxima nesta semana. Transportadores com frota própria registram alta na demanda por fretes de grãos e insumos. Especialistas recomendam revisar suspensão e freios para trechos não pavimentados.", brBodyStyle);

            GUILayout.Space(16);

            // Grid de Notícias Secundárias
            int cols = w >= 660f ? 2 : 1;
            float gap = 16f;
            float cardW = (w - (cols - 1) * gap) / cols;

            (string title, string cat, string body)[] newsList =
            {
                ("Obras de recapeamento nas principais rodovias", "Infraestrutura",
                 "Trechos sinalizados estão recebendo novo pavimento asfáltico para suportar o tráfego pesado de carretas durante o período de escoamento da colheita."),

                ("TerraBank disponibiliza crédito para modernização de frotas", "Economia",
                 "Linhas de financiamento com taxas competitivas foram liberadas para a aquisição de novos caminhões e tratores de alta produtividade."),

                ("Feirão regional reúne seminovos e utilitários", "Automotivo",
                 "A procura por picapes 4x2 e caminhões toco usados aumentou 20% no pátio do Zé das Latas, com opções revisadas para pronta entrega."),

                ("Rancho Online expande oferta de matrizes leiteiras", "Agropecuária",
                 "Criadores locais destacam a boa adaptação das vacas holandesas e cavalos de montaria ao clima e relevo da bacia regional.")
            };

            for (int i = 0; i < newsList.Length; i++)
            {
                if (i % cols == 0) GUILayout.BeginHorizontal();

                Rect card = GUILayoutUtility.GetRect(cardW, 115f, GUILayout.Width(cardW), GUILayout.Height(115f));
                Fill(card, UiCardBg);
                DrawOutline(card, UiBorderLight);

                GUI.Label(new Rect(card.x + 16f, card.y + 12f, card.width - 32f, 18f), newsList[i].cat.ToUpper(), brSmallStyle);
                GUI.Label(new Rect(card.x + 16f, card.y + 32f, card.width - 32f, 22f), newsList[i].title, brPageH2Style);
                GUI.Label(new Rect(card.x + 16f, card.y + 58f, card.width - 32f, 50f), newsList[i].body, brBodyStyle);

                if (i % cols == cols - 1) GUILayout.EndHorizontal();
                else GUILayout.Space(gap);
            }
            if (newsList.Length % cols != 0) GUILayout.EndHorizontal();
        }

        // =========================================================================
        // 2. AGROBOLSA — TERMINAL FINANCEIRO CLEAN
        // =========================================================================
        private void DrawModernMarketSite(float w)
        {
            var save = CurrentSave;
            long day = save != null ? save.gameDay : 1;

            (string name, string unit, float baseP)[] items =
            {
                ("Soja em Grãos (Tipo 1)", "Saca 60 kg", 135f),
                ("Milho Seco Amarelo", "Saca 60 kg", 68f),
                ("Trigo Moído Especial", "Saca 60 kg", 92f),
                ("Leite Refrigerado", "Litro", 2.90f),
                ("Gado de Corte (Arroba)", "Arroba (15 kg)", 310f),
                ("Lã Branca Ovinos", "Quilograma", 14.00f),
                ("Óleo Diesel S-10", "Litro", save != null ? save.EffectiveFuelPrice : 2.20f),
            };

            // Tabela Clean
            Rect th = GUILayoutUtility.GetRect(w, 36f);
            Fill(th, new Color(0.95f, 0.96f, 0.98f));
            DrawOutline(th, UiBorderLight);

            float c1W = Mathf.Clamp(w * 0.35f, 160f, 280f);
            float c2W = Mathf.Clamp(w * 0.22f, 100f, 160f);
            float c3W = Mathf.Clamp(w * 0.22f, 100f, 160f);
            float c4W = w - c1W - c2W - c3W - 24f;

            float tx = th.x + 16f;
            GUI.Label(new Rect(tx, th.y + 8f, c1W, 20f), "Commodity", brPageH3Style); tx += c1W;
            GUI.Label(new Rect(tx, th.y + 8f, c2W, 20f), "Unidade", brPageH3Style); tx += c2W;
            GUI.Label(new Rect(tx, th.y + 8f, c3W, 20f), "Preço Atual", brPageH3Style); tx += c3W;
            GUI.Label(new Rect(tx, th.y + 8f, c4W, 20f), "Variação Diária", brPageH3Style);

            for (int i = 0; i < items.Length; i++)
            {
                float t = (float)day * 0.7f + i * 1.9f;
                float now = items[i].baseP * (1f + 0.08f * Mathf.Sin(t));
                float prev = items[i].baseP * (1f + 0.08f * Mathf.Sin(t - 0.7f));
                float deltaP = (now - prev) / prev * 100f;
                bool isUp = now >= prev;

                Rect row = GUILayoutUtility.GetRect(w, 42f);
                Fill(row, UiCardBg);
                DrawOutline(row, UiBorderLight);

                float rx = row.x + 16f;
                GUI.Label(new Rect(rx, row.y + 10f, c1W, 22f), items[i].name, brPageH2Style); rx += c1W;
                GUI.Label(new Rect(rx, row.y + 11f, c2W, 20f), items[i].unit, brBodyStyle); rx += c2W;
                GUI.Label(new Rect(rx, row.y + 9f, c3W, 24f), $"$ {now:F2}", brPriceStyle); rx += c3W;

                // Chip de variação verde/vermelho sutil
                Color chipBg = isUp ? new Color(0.88f, 0.98f, 0.92f) : new Color(0.98f, 0.90f, 0.90f);
                Color chipText = isUp ? new Color(0.08f, 0.60f, 0.32f) : new Color(0.85f, 0.20f, 0.20f);
                Rect chip = new Rect(rx, row.y + 9f, 85f, 24f);
                Fill(chip, chipBg);
                DrawOutline(chip, isUp ? new Color(0.70f, 0.90f, 0.78f) : new Color(0.95f, 0.75f, 0.75f));

                GUI.color = chipText;
                GUI.Label(chip, $"{(isUp ? "+ " : "- ")}{Mathf.Abs(deltaP):0.0}%", brBadgeStyle);
                GUI.color = Color.white;
            }

            GUILayout.Space(16);

            Rect note = GUILayoutUtility.GetRect(w, 54f);
            Fill(note, UiCardBg);
            DrawOutline(note, UiBorderLight);
            TerraIcons.Draw(new Rect(note.x + 14f, note.y + 14f, 16f, 16f), TerraIcon.Info, UiPrimaryBlue);
            GUI.Label(new Rect(note.x + 36f, note.y + 10f, note.width - 50f, 36f),
                "Análise de Mercado: Os valores variam diariamente com base nas condições climáticas e na demanda das cooperativas. Aproveite picos de alta para entregar cargas armazenadas.", brSmallStyle);
        }

        // =========================================================================
        // 3. TERRABANK — FINTECH BANCÁRIA MODERNA
        // =========================================================================
        private void DrawModernBankSite(float w)
        {
            var save = CurrentSave;
            if (save == null) return;

            // Cartão de Saldo Principal
            Rect accCard = GUILayoutUtility.GetRect(w, 95f);
            Fill(accCard, UiCardBg);
            DrawOutline(accCard, UiBorderLight);

            GUI.Label(new Rect(accCard.x + 20f, accCard.y + 16f, accCard.width - 40f, 20f), "CONTA CORRENTE DIGITAL", brSmallStyle);
            GUI.Label(new Rect(accCard.x + 20f, accCard.y + 38f, accCard.width - 40f, 32f), save.GetFormattedMoney(), brPageH1Style);
            GUI.Label(new Rect(accCard.xMax - 220f, accCard.y + 16f, 200f, 20f), $"Score de Crédito: {save.reputation} pts", brSmallStyle);

            GUILayout.Space(16);

            // Custos Diários da Frota (se economia avançada)
            if (save.advancedEconomy)
            {
                var cfg = save.DifficultyConfig;
                int fleet = 1;
                var vm = VehicleManager.Instance;
                if (vm != null && vm.possessedVehicles != null)
                {
                    int c = 0;
                    foreach (var v in vm.possessedVehicles) if (v != null) c++;
                    if (c > fleet) fleet = c;
                }
                long dailyUpkeep = cfg.upkeepBasePerDay + cfg.upkeepPerVehiclePerDay * fleet;

                Rect costCard = GUILayoutUtility.GetRect(w, 75f);
                Fill(costCard, UiCardBg);
                DrawOutline(costCard, UiBorderLight);

                GUI.Label(new Rect(costCard.x + 20f, costCard.y + 14f, costCard.width - 40f, 20f), "CUSTO DIÁRIO ESTIMADO DE OPERAÇÃO", brSmallStyle);
                GUI.Label(new Rect(costCard.x + 20f, costCard.y + 36f, costCard.width - 40f, 24f),
                    $"${dailyUpkeep:N0} / dia  •  Seguro: ${cfg.upkeepBasePerDay:N0}  •  Frota ativa: {fleet} veículos", brPageH2Style);

                GUILayout.Space(16);
            }

            // Empréstimos Ativos
            GUILayout.Label("Empréstimos Ativos", brPageH2Style);
            GUILayout.Space(8);

            if (save.ActiveLoanCount == 0)
            {
                Rect noL = GUILayoutUtility.GetRect(w, 50f);
                Fill(noL, UiCardBg);
                DrawOutline(noL, UiBorderLight);
                GUI.Label(new Rect(noL.x + 16f, noL.y + 15f, noL.width - 32f, 20f), "Nenhum financiamento em aberto. Seu limite de crédito está totalmente liberado.", brBodyStyle);
            }
            else
            {
                var activeLoans = new List<BankLoan>(save.loans);
                foreach (var loan in activeLoans)
                {
                    if (loan == null) continue;

                    Rect lCard = GUILayoutUtility.GetRect(w, 90f);
                    Fill(lCard, UiCardBg);
                    DrawOutline(lCard, UiBorderLight);

                    GUI.Label(new Rect(lCard.x + 16f, lCard.y + 14f, 280f, 22f), loan.bankName, brPageH2Style);
                    GUI.Label(new Rect(lCard.x + 16f, lCard.y + 38f, 280f, 20f),
                        $"Dívida restante: ${loan.remainingDebt:N0}  •  Parcela: ${loan.installmentAmount:N0}", brBodyStyle);
                    GUI.Label(new Rect(lCard.x + 16f, lCard.y + 60f, 280f, 18f),
                        $"Pagas: {loan.installmentsPaid} de {loan.installmentsTotal}", brSmallStyle);

                    float bx = lCard.xMax - 260f;
                    float by = lCard.y + 26f;

                    bool canPay1 = save.currentMoney >= loan.installmentAmount;
                    GUI.enabled = canPay1;
                    if (GUI.Button(new Rect(bx, by, 120, 36), "Pagar 1 Parcela", brBuySecondaryStyle))
                    {
                        PayLoan(save, loan, 1);
                    }
                    bx += 128f;

                    bool canPayAll = save.currentMoney >= loan.remainingDebt;
                    GUI.enabled = canPayAll;
                    if (GUI.Button(new Rect(bx, by, 120, 36), "Quitar Total", brBuyPrimaryStyle))
                    {
                        PayLoan(save, loan, 0);
                    }
                    GUI.enabled = true;

                    GUILayout.Space(10);
                }
            }

            GUILayout.Space(20);

            // Contratação de Crédito
            GUILayout.Label("Linhas de Crédito Disponíveis", brPageH2Style);
            GUILayout.Space(8);

            if (save.ActiveLoanCount >= RegionSaveData.MaxActiveLoans)
            {
                Rect lMax = GUILayoutUtility.GetRect(w, 46f);
                Fill(lMax, UiCardBg);
                DrawOutline(lMax, UiBorderLight);
                GUI.Label(new Rect(lMax.x + 16f, lMax.y + 13f, lMax.width - 32f, 20f), "Limite máximo de contratos ativos atingido. Quite um empréstimo para contratar outro.", brSmallStyle);
            }
            else
            {
                foreach (var bank in BankOffers)
                {
                    Rect bCard = GUILayoutUtility.GetRect(w, 75f);
                    Fill(bCard, UiCardBg);
                    DrawOutline(bCard, UiBorderLight);

                    GUI.Label(new Rect(bCard.x + 16f, bCard.y + 14f, bCard.width - 180f, 22f), bank.name, brPageH2Style);
                    GUI.Label(new Rect(bCard.x + 16f, bCard.y + 40f, bCard.width - 180f, 20f),
                        $"Liberação: ${bank.principal:N0}  •  Juros: {bank.interestPercent}%  •  {bank.installments} parcelas de ${bank.InstallmentAmount:N0}", brBodyStyle);

                    bool alreadyHas = save.HasLoanWith(bank.id);
                    GUI.enabled = !alreadyHas;
                    string btnTxt = alreadyHas ? "Contratado" : "Contratar";
                    if (GUI.Button(new Rect(bCard.xMax - 140f, bCard.y + 20f, 124f, 36f), btnTxt, brBuyPrimaryStyle))
                    {
                        save.currentMoney += bank.principal;
                        save.loans.Add(new BankLoan
                        {
                            bankId = bank.id,
                            bankName = bank.name,
                            remainingDebt = bank.TotalDebt,
                            installmentAmount = bank.InstallmentAmount,
                            installmentsPaid = 0,
                            installmentsTotal = bank.installments,
                            startDay = save.gameDay,
                            lastPenaltyDay = save.gameDay,
                        });
                        save.AddLoanHistory(bank.name, "Contratação Digital", bank.principal);
                        PersistSave();
                        ShowToast($"Crédito de ${bank.principal:N0} liberado na sua conta!");
                    }
                    GUI.enabled = true;

                    GUILayout.Space(8);
                }
            }
        }

        // =========================================================================
        // 4. TERRAFUEL — REDE DE POSTOS & ABASTECIMENTO
        // =========================================================================
        private void DrawModernFuelSite(float w)
        {
            var save = CurrentSave;
            var vehicle = ServiceableVehicle;
            float price = save != null ? save.EffectiveFuelPrice : 2.20f;

            // Cards de Preços de Combustível
            int cols = w >= 600f ? 3 : 1;
            float gap = 12f;
            float cardW = (w - (cols - 1) * gap) / cols;

            (string fuel, string val, string desc)[] fuels =
            {
                ("Diesel S-10", $"${price:F2} / L", "Recomendado para utilitários e caminhões"),
                ("Gasolina Comum", $"${price * 1.35f:F2} / L", "Ideal para veículos leves e sedans"),
                ("Etanol Hidratado", $"${price * 0.95f:F2} / L", "Combustível renovável econômico")
            };

            for (int i = 0; i < fuels.Length; i++)
            {
                if (i % cols == 0) GUILayout.BeginHorizontal();

                Rect fCard = GUILayoutUtility.GetRect(cardW, 85f, GUILayout.Width(cardW), GUILayout.Height(85f));
                Fill(fCard, UiCardBg);
                DrawOutline(fCard, UiBorderLight);

                GUI.Label(new Rect(fCard.x + 16f, fCard.y + 12f, fCard.width - 32f, 18f), fuels[i].fuel, brSmallStyle);
                GUI.Label(new Rect(fCard.x + 16f, fCard.y + 32f, fCard.width - 32f, 26f), fuels[i].val, brPageH1Style);
                GUI.Label(new Rect(fCard.x + 16f, fCard.y + 60f, fCard.width - 32f, 18f), fuels[i].desc, brSmallStyle);

                if (i % cols == cols - 1) GUILayout.EndHorizontal();
                else GUILayout.Space(gap);
            }
            if (fuels.Length % cols != 0) GUILayout.EndHorizontal();

            GUILayout.Space(20);

            // Telemetria do Veículo em Uso
            Rect vCard = GUILayoutUtility.GetRect(w, 150f);
            Fill(vCard, UiCardBg);
            DrawOutline(vCard, UiBorderLight);

            if (vehicle == null)
            {
                GUI.Label(new Rect(vCard.x + 20f, vCard.y + 20f, vCard.width - 40f, 24f), "Tele-Abastecimento Remoto", brPageH2Style);
                GUI.Label(new Rect(vCard.x + 20f, vCard.y + 50f, vCard.width - 40f, 40f),
                    "Nenhum veículo em uso detectado na sua posição atual. Assuma o volante de um veículo ou selecione-o na sua frota para abastecer.", brBodyStyle);
            }
            else
            {
                GUI.Label(new Rect(vCard.x + 20f, vCard.y + 16f, vCard.width - 40f, 24f),
                    $"Veículo Conectado: {vehicle.vehicleName}", brPageH2Style);

                // Barra de nível de combustível limpa
                float barW = Mathf.Clamp(vCard.width - 40f, 200f, 480f);
                Rect bar = new Rect(vCard.x + 20f, vCard.y + 48f, barW, 14f);
                Fill(bar, new Color(0.90f, 0.92f, 0.95f));
                Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(vehicle.fuelPercent / 100f), bar.height), UiSuccessGreen);
                DrawOutline(bar, UiBorderLight);

                GUI.Label(new Rect(bar.xMax + 14f, vCard.y + 44f, 140f, 22f), $"{vehicle.fuelPercent:F0}% do tanque", brPageH3Style);

                float litersNeeded = Mathf.Max(0f, (100f - vehicle.fuelPercent) / 100f * vehicle.fuelTankLiters);
                long cost = (long)Mathf.Ceil(litersNeeded * price);

                if (litersNeeded < 0.5f)
                {
                    TerraIcons.Draw(new Rect(vCard.x + 20f, vCard.y + 83f, 16f, 16f), TerraIcon.Checkmark, UiSuccessGreen);
                    GUI.Label(new Rect(vCard.x + 42f, vCard.y + 80f, vCard.width - 60f, 24f), "Tanque 100% abastecido. O veículo está pronto para rodar.", brPriceStyle);
                }
                else
                {
                    GUI.Label(new Rect(vCard.x + 20f, vCard.y + 76f, vCard.width - 40f, 22f),
                        $"Volume para abastecer: {litersNeeded:F1} Litros  •  Custo estimado: ${cost:N0}", brBodyStyle);

                    bool canPay = save != null && save.currentMoney >= cost;
                    GUI.enabled = canPay;
                    if (GUI.Button(new Rect(vCard.x + 20f, vCard.y + 104f, 220f, 34f), $"Completar Tanque (${cost:N0})", brBuyPrimaryStyle))
                    {
                        vehicle.Refuel(100f);
                        save.currentMoney -= cost;
                        save.totalExpenses += cost;
                        PersistSave();
                        ShowToast($"Tanque abastecido (-${cost:N0}).");
                    }
                    GUI.enabled = true;
                }
            }
        }

        // =========================================================================
        // 5. METAS DA CAMPANHA — CONQUISTAS & RECOMPENSAS
        // =========================================================================
        private void DrawModernGoalsSite(float w)
        {
            var save = CurrentSave;
            if (save == null) return;

            int fleet = 1;
            var vm = VehicleManager.Instance;
            if (vm != null && vm.possessedVehicles != null)
            {
                int c = 0;
                foreach (var v in vm.possessedVehicles) if (v != null) c++;
                if (c > fleet) fleet = c;
            }
            int lic = CampaignGoalCatalog.CountLicenses(save);

            int claimed = 0;
            foreach (var g in CampaignGoalCatalog.All)
                if (CampaignGoalCatalog.IsClaimed(save, g.bit)) claimed++;

            // Progresso Geral
            Rect pCard = GUILayoutUtility.GetRect(w, 85f);
            Fill(pCard, UiCardBg);
            DrawOutline(pCard, UiBorderLight);

            GUI.Label(new Rect(pCard.x + 20f, pCard.y + 14f, pCard.width - 40f, 20f), "PROGRESSO GERAL DE RECOMPENSAS", brSmallStyle);
            GUI.Label(new Rect(pCard.x + 20f, pCard.y + 36f, 200f, 24f),
                $"{claimed} de {CampaignGoalCatalog.All.Length} Metas Concluídas", brPageH2Style);

            float pBarW = Mathf.Clamp(pCard.width - 240f, 180f, 400f);
            Rect pBar = new Rect(pCard.xMax - pBarW - 20f, pCard.y + 40f, pBarW, 12f);
            Fill(pBar, new Color(0.90f, 0.92f, 0.95f));
            Fill(new Rect(pBar.x, pBar.y, pBar.width * ((float)claimed / Mathf.Max(1, CampaignGoalCatalog.All.Length)), pBar.height), UiPrimaryBlue);
            DrawOutline(pBar, UiBorderLight);

            GUILayout.Space(16);

            // Lista de Metas
            foreach (var g in CampaignGoalCatalog.All)
            {
                bool isClaimed = CampaignGoalCatalog.IsClaimed(save, g.bit);
                bool met = g.isMet(save, fleet, lic);

                Rect card = GUILayoutUtility.GetRect(w, 80f);
                Fill(card, UiCardBg);
                DrawOutline(card, isClaimed ? new Color(0.80f, 0.92f, 0.85f) : (met ? UiBorderFocus : UiBorderLight));

                string reward = (g.rewardMoney > 0 ? $"${g.rewardMoney:N0} " : "") + (g.rewardReputation > 0 ? $"+{g.rewardReputation} Rep" : "");

                GUI.Label(new Rect(card.x + 18f, card.y + 14f, card.width - 200f, 22f), g.title, brPageH2Style);
                GUI.Label(new Rect(card.x + 18f, card.y + 38f, card.width - 200f, 32f),
                    $"{g.description}  •  Prêmio: {reward}", brBodyStyle);

                float bx = card.xMax - 160f;
                float by = card.y + 22f;

                if (isClaimed)
                {
                    TerraIcons.Draw(new Rect(bx, by + 7f, 14f, 14f), TerraIcon.Checkmark, UiSuccessGreen);
                    GUI.Label(new Rect(bx + 18f, by + 4f, 130f, 24f), "Resgatado", brPriceStyle);
                }
                else if (met)
                {
                    if (GUI.Button(new Rect(bx, by, 144f, 36f), "Resgatar Prêmio", brBuyPrimaryStyle))
                    {
                        save.currentMoney += g.rewardMoney;
                        save.reputation += g.rewardReputation;
                        CampaignGoalCatalog.Claim(save, g.bit);
                        PersistSave();
                        ShowToast($"Meta concluída: {g.title} ({reward})!");
                    }
                }
                else
                {
                    GUI.Label(new Rect(bx, by + 4f, 140f, 24f), "Em progresso", brSmallStyle);
                }

                GUILayout.Space(10);
            }
        }

        // =========================================================================
        // 6. GUIA DE RODOVIAS — ROTAS E TRANSPORTE
        // =========================================================================
        private void DrawModernRoadsSite(float w)
        {
            (string via, string rota, string pav, string km, string vel)[] roads =
            {
                ("BR-101 Regional", "Centro Urbano ↔ Polo Logístico", "Asfalto Excelente", "48 km", "80–100 km/h"),
                ("Estrada das Fazendas", "Vila Rural ↔ Sedes Agropecuárias", "Terra Batida", "32 km", "40–60 km/h"),
                ("Via Litorânea Fluvial", "Porto Fluvial ↔ Distrito Industrial", "Asfalto Sinuoso", "65 km", "60–70 km/h"),
                ("Serra do Mirante", "Planalto Central ↔ Vale Verde", "Cascalho Reforçado", "28 km", "30–50 km/h"),
            };

            Rect th = GUILayoutUtility.GetRect(w, 36f);
            Fill(th, new Color(0.95f, 0.96f, 0.98f));
            DrawOutline(th, UiBorderLight);

            float c1 = Mathf.Clamp(w * 0.25f, 120f, 180f);
            float c2 = Mathf.Clamp(w * 0.35f, 160f, 280f);
            float c3 = Mathf.Clamp(w * 0.20f, 90f, 140f);
            float c4 = 70f;
            float c5 = w - c1 - c2 - c3 - c4 - 24f;

            float x = th.x + 16f;
            GUI.Label(new Rect(x, th.y + 8f, c1, 20f), "Rodovia", brPageH3Style); x += c1;
            GUI.Label(new Rect(x, th.y + 8f, c2, 20f), "Ligação", brPageH3Style); x += c2;
            GUI.Label(new Rect(x, th.y + 8f, c3, 20f), "Pavimento", brPageH3Style); x += c3;
            GUI.Label(new Rect(x, th.y + 8f, c4, 20f), "Extensão", brPageH3Style); x += c4;
            GUI.Label(new Rect(x, th.y + 8f, c5, 20f), "Velocidade", brPageH3Style);

            for (int i = 0; i < roads.Length; i++)
            {
                Rect row = GUILayoutUtility.GetRect(w, 40f);
                Fill(row, UiCardBg);
                DrawOutline(row, UiBorderLight);

                float rx = row.x + 16f;
                GUI.Label(new Rect(rx, row.y + 10f, c1, 20f), roads[i].via, brPageH2Style); rx += c1;
                GUI.Label(new Rect(rx, row.y + 11f, c2, 20f), roads[i].rota, brBodyStyle); rx += c2;
                GUI.Label(new Rect(rx, row.y + 11f, c3, 20f), roads[i].pav, brBodyStyle); rx += c3;
                GUI.Label(new Rect(rx, row.y + 11f, c4, 20f), roads[i].km, brPageH3Style); rx += c4;
                GUI.Label(new Rect(rx, row.y + 11f, c5, 20f), roads[i].vel, brSmallStyle);
            }

            GUILayout.Space(16);

            Rect helpCard = GUILayoutUtility.GetRect(w, 80f);
            Fill(helpCard, UiCardBg);
            DrawOutline(helpCard, UiBorderLight);

            GUI.Label(new Rect(helpCard.x + 20f, helpCard.y + 14f, helpCard.width - 40f, 20f), "CENTRAL DE ASSISTÊNCIA E SOCORRO NA RODOVIA", brSmallStyle);
            GUI.Label(new Rect(helpCard.x + 20f, helpCard.y + 36f, helpCard.width - 40f, 34f),
                "• Polícia Rodoviária: 191  |  • Guincho e Socorro 24h: 0800-GUINCHO\n• Dica: Use o app Mapa no celular in-game para marcar destinos e conferir rotas.", brBodyStyle);
        }
    }
}
