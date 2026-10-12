using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Apps do celular inspirados em Farming Simulator e Euro Truck Simulator 2:
    /// mercado de fretes, finanças/banco, licenças (habilitações), garagem/frota,
    /// concessionária e estatísticas. O conteúdo é destravado conforme as licenças
    /// do jogador (DriverLicense), salvas no RegionSaveData.
    /// </summary>
    public partial class SandboxHUD
    {
        #region Mercado de Fretes (Jobs)

        private class PhoneJob
        {
            public string icon;
            public string title;
            public string cargo;
            public float weightKg;
            public long reward;
            public int rep;
            public DriverLicense required;
        }

        private List<PhoneJob> jobMarket;
        // Regeneração adiada para o Update (fora das passagens do OnGUI, evitando
        // mismatch de layout do GUILayout). O salt faz o botão "Atualizar"variar.
        private bool jobMarketDirty = false;
        private int jobMarketSalt = 0;

        /// <summary>Processado uma vez por frame no Update, antes das passagens do OnGUI.</summary>
        private void TickJobMarket()
        {
            if (jobMarketDirty)
            {
                jobMarketSalt++;
                jobMarket = null;
                jobMarketDirty = false;
            }
        }

        // Tabelas de carga por licença (ícone + título do frete + nomes de carga).
        private static readonly (DriverLicense lic, string icon, string[] cargos)[] CargoTable =
        {
            (DriverLicense.Standard, "", new[] { "Paletes Industriais", "Eletrodomésticos", "Material de Construção" }),
            (DriverLicense.Agriculture, "", new[] { "Soja a Granel", "Fardos de Feno", "Fertilizante NPK" }),
            (DriverLicense.Refrigerated, "", new[] { "Laticínios", "Carnes Resfriadas", "Lote de Vacinas" }),
            (DriverLicense.Fragile, "", new[] { "Vidros Temperados", "Peças de Cerâmica", "Painéis Solares" }),
            (DriverLicense.Livestock, "", new[] { "Gado de Corte", "Lote de Suínos", "Aves Vivas" }),
            (DriverLicense.Heavy, "️", new[] { "Escavadeira", "Vigas de Aço", "Transformador Elétrico" }),
            (DriverLicense.HighValue, "[Luxo]", new[] { "Equipamentos Médicos", "Eletrônicos Premium", "Cofre Blindado" }),
            (DriverLicense.Hazmat, "[ADR]", new[] { "Combustível", "Gás Industrial", "Produtos Químicos" }),
            (DriverLicense.Maritime, "", new[] { "Pescado Fresco", "Frutos do Mar", "Contêiner Costeiro" }),
            (DriverLicense.Aviation, "", new[] { "Encomenda Aérea Expressa", "Suprimentos Remotos" }),
        };

        private void EnsureJobMarket(RegionSaveData save)
        {
            if (jobMarket != null) return;

            jobMarket = new List<PhoneJob>();
            int seed = 7919 + jobMarketSalt * 104729 + (save != null ? save.completedDeliveries * 31 + save.regionId : 0);
            var rng = new System.Random(seed);

            // Gera 7 fretes variados, embaralhando as categorias disponíveis na tabela.
            int[] order = new int[CargoTable.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            int jobs = 7;
            for (int k = 0; k < jobs; k++)
            {
                var entry = CargoTable[order[k % order.Length]];
                string cargo = entry.cargos[rng.Next(entry.cargos.Length)];
                float weight = Mathf.Round(rng.Next(600, 6000) / 50f) * 50f;

                // Recompensa cresce com o peso e com o "nível"da licença no catálogo.
                var info = DriverLicenseCatalog.Get(entry.lic);
                long tierBonus = info != null ? info.cost / 3 : 0;
                long reward = 12000 + (long)(weight * rng.Next(6, 14)) + tierBonus + rng.Next(0, 8000);

                jobMarket.Add(new PhoneJob
                {
                    icon = entry.icon,
                    title = $"{cargo}",
                    cargo = cargo,
                    weightKg = weight,
                    reward = reward,
                    rep = rng.Next(8, 28),
                    required = entry.lic
                });
            }
        }

        private void DrawPhoneJobs(Rect content)
        {
            DrawAppHeader(content, "Frete");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            var mm = MissionManager.Instance;

            if (mm != null && mm.hasActiveContract && mm.currentContract != null && !mm.currentContract.isCompleted)
            {
                var c = mm.currentContract;
                GUILayout.Label("CONTRATO ATIVO", phoneSectionStyle);
                GUILayout.Label(c.title, phoneBodyStyle);
                float dist = mm.GetDistanceToDestination();
                string distText = dist > 1000f ? $"{(dist / 1000f):F1} km" : $"{dist:F0} m";
                GUILayout.Label($" {c.destinationName} • {distText}", phoneSmallStyle);
                GUILayout.Label($" {c.cargoType} • ${c.rewardMoney:N0}", phoneSmallStyle);
            }
            else
            {
                GUILayout.Label("CONTRATO ATIVO", phoneSectionStyle);
                GUILayout.Label("Nenhum frete em andamento.", phoneSmallStyle);
            }

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);
            GUILayout.Label("MERCADO DE FRETES", phoneSectionStyle);
            GUILayout.Label("Ofertas liberadas conforme suas licenças.", phoneSmallStyle);
            GUILayout.Space(6);

            EnsureJobMarket(save);
            foreach (var j in jobMarket)
            {
                bool licensed = save == null || save.HasLicense(j.required);
                var info = DriverLicenseCatalog.Get(j.required);

                GUILayout.Label($"{j.icon} {j.title}", phoneBodyStyle);
                GUILayout.Label($" {j.weightKg:F0} kg ${j.reward:N0} +{j.rep}", phoneSmallStyle);

                if (!licensed)
                {
                    GUILayout.Label($"[Bloqueado] Requer licença: {info.icon} {info.name}", phoneLockStyle);
                }
                else if (GUILayout.Button("[OK] Aceitar frete", phoneListButtonStyle, GUILayout.Height(26)))
                {
                    if (mm != null)
                    {
                        mm.AcceptMarketContract(j.title, $"Transportar {j.cargo} até o destino indicado no mapa.",
                            j.cargo, j.weightKg, j.reward, j.rep);
                        ShowToast($"Frete aceito: {j.title}");
                        jobMarketDirty = true; // regenerado no próximo frame (Update)
                    }
                }

                GUILayout.Space(4);
                DrawPhoneDivider();
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Atualizar ofertas", phoneListButtonStyle, GUILayout.Height(26)))
            {
                jobMarketDirty = true;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Finanças e Banco

        /// <summary>Oferta de crédito de um banco: valor liberado, juros e nº de parcelas.</summary>
        private struct BankOffer
        {
            public string id;
            public string name;
            public long principal;      // valor creditado ao jogador
            public int interestPercent; // juros sobre o principal
            public int installments;    // número de parcelas

            public long TotalDebt => principal + principal * interestPercent / 100;
            public long InstallmentAmount => (TotalDebt + installments - 1) / installments; // arredonda p/ cima
        }

        // Três bancos com perfis diferentes: do crédito pequeno e barato ao grande e caro.
        private static readonly BankOffer[] BankOffers =
        {
            new BankOffer { id = "popular",     name = "Banco Popular",     principal = 30000,  interestPercent = 8,  installments = 12 },
            new BankOffer { id = "nacional",    name = "Banco Nacional",    principal = 80000,  interestPercent = 12, installments = 18 },
            new BankOffer { id = "continental", name = "Banco Continental", principal = 200000, interestPercent = 18, installments = 24 },
        };

        /// <summary>
        /// Abate <paramref name="n"/> parcelas (ou o total, se n &lt;= 0) de um empréstimo,
        /// limitado ao saldo disponível. Remove o empréstimo quando quitado.
        /// </summary>
        private void PayLoan(RegionSaveData save, BankLoan loan, int n)
        {
            long desired = n > 0 ? loan.installmentAmount * n : loan.remainingDebt;
            long pay = System.Math.Min(System.Math.Min(desired, loan.remainingDebt), save.currentMoney);
            if (pay <= 0)
            {
                ShowToast("Sem saldo para pagar.");
                return;
            }

            save.currentMoney -= pay;
            loan.remainingDebt -= pay;

            if (loan.remainingDebt <= 0)
            {
                save.loans.Remove(loan);
                save.AddLoanHistory(loan.bankName, "Quitação", pay);
                ShowToast($"{loan.bankName}: empréstimo quitado!");
            }
            else
            {
                // Recalcula o progresso de parcelas a partir do saldo devedor.
                long parcelasRestantes = (loan.remainingDebt + loan.installmentAmount - 1) / loan.installmentAmount;
                loan.installmentsPaid = (int)System.Math.Max(0, loan.installmentsTotal - parcelasRestantes);
                save.AddLoanHistory(loan.bankName, "Pagamento", pay);
                ShowToast($"{loan.bankName}: pago ${pay:N0}.");
            }

            PersistSave();
        }

        private void DrawPhoneFinances(Rect content)
        {
            DrawAppHeader(content, "Finanças");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            if (save == null)
            {
                GUILayout.Label("Sem partida ativa.", phoneBodyStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label("CONTA", phoneSectionStyle);
            GUILayout.Label(save.GetFormattedMoney(), moneyStyle);
            long net = save.currentMoney - save.startingMoney;
            GUILayout.Label($"Capital inicial: ${save.startingMoney:N0}", phoneSmallStyle);
            GUILayout.Label($"Resultado: {(net >= 0 ? "[Alta] +" : "[Baixa] ")}${net:N0}", phoneSmallStyle);
            GUILayout.Label($"Reputação: {save.reputation} Entregas: {save.completedDeliveries}", phoneSmallStyle);

            GUILayout.Space(8);
            DrawPhoneDivider();
            GUILayout.Space(4);

            GUILayout.Label("BANCO", phoneSectionStyle);
            GUILayout.Label($"Dia {save.gameDay}", phoneSmallStyle);
            GUILayout.Label($"Dívida total: ${save.TotalLoanDebt:N0}", phoneBodyStyle);
            GUILayout.Label($"Empréstimos ativos: {save.ActiveLoanCount}/{RegionSaveData.MaxActiveLoans}", phoneSmallStyle);
            GUILayout.Space(6);

            // --- Empréstimos ativos (pagamento parcelado) ---
            if (save.ActiveLoanCount > 0)
            {
                GUILayout.Label("MEUS EMPRÉSTIMOS", phoneSectionStyle);
                GUILayout.Label($"Vence 1 parcela por dia • multa de {LoanLateFeePercent}% por atraso.", phoneSmallStyle);
                GUILayout.Space(4);

                // Cópia para iterar com segurança (PayLoan pode remover da lista).
                var activeLoans = new List<BankLoan>(save.loans);
                foreach (var loan in activeLoans)
                {
                    if (loan == null) continue;

                    GUILayout.Label(loan.bankName, phoneBodyStyle);
                    GUILayout.Label($"Devendo ${loan.remainingDebt:N0}", phoneSmallStyle);
                    GUILayout.Label($"Parcela ${loan.installmentAmount:N0} • {loan.installmentsPaid}/{loan.installmentsTotal} pagas", phoneSmallStyle);

                    // Parcelas vencidas: uma por dia desde a contratação.
                    long elapsed = System.Math.Max(0, save.gameDay - loan.startDay);
                    long expectedPaid = System.Math.Min(loan.installmentsTotal, elapsed);
                    long overdue = System.Math.Max(0, expectedPaid - loan.installmentsPaid);
                    if (overdue > 0)
                        GUILayout.Label($"[Atraso] {overdue} parcela(s) vencida(s) acumulando multa.", phoneLockStyle);

                    if (save.currentMoney <= 0)
                    {
                        GUILayout.Label("Sem saldo para pagar parcelas.", phoneLockStyle);
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Pagar 1 parcela", phoneListButtonStyle, GUILayout.Height(26)))
                            PayLoan(save, loan, 1);
                        if (loan.InstallmentsRemaining > 1 &&
                            GUILayout.Button("Pagar 3", phoneListButtonStyle, GUILayout.Height(26)))
                            PayLoan(save, loan, 3);
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button($"Quitar tudo (${System.Math.Min(save.currentMoney, loan.remainingDebt):N0})",
                                phoneListButtonStyle, GUILayout.Height(26)))
                            PayLoan(save, loan, 0);
                    }

                    GUILayout.Space(4);
                    DrawPhoneDivider();
                    GUILayout.Space(5);
                }
            }

            // --- Contratar novos empréstimos ---
            GUILayout.Label("CONTRATAR CRÉDITO", phoneSectionStyle);

            if (save.ActiveLoanCount >= RegionSaveData.MaxActiveLoans)
            {
                GUILayout.Label($"[Bloqueado] Limite de {RegionSaveData.MaxActiveLoans} empréstimos atingido. Quite um para contratar outro.", phoneLockStyle);
            }
            else
            {
                foreach (var bank in BankOffers)
                {
                    GUILayout.Label(bank.name, phoneBodyStyle);
                    GUILayout.Label($"Libera ${bank.principal:N0} • {bank.interestPercent}% juros", phoneSmallStyle);
                    GUILayout.Label($"{bank.installments}x de ${bank.InstallmentAmount:N0}", phoneSmallStyle);

                    if (save.HasLoanWith(bank.id))
                    {
                        GUILayout.Label("[Ativo] Já possui um empréstimo neste banco.", phoneLockStyle);
                    }
                    else if (GUILayout.Button($"Contratar +${bank.principal:N0}", phoneListButtonStyle, GUILayout.Height(28)))
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
                        save.AddLoanHistory(bank.name, "Contratação", bank.principal);
                        PersistSave();
                        ShowToast($"{bank.name}: ${bank.principal:N0} creditados.");
                    }

                    GUILayout.Space(4);
                    DrawPhoneDivider();
                    GUILayout.Space(5);
                }
            }

            // --- Histórico de pagamentos ---
            GUILayout.Space(2);
            GUILayout.Label("HISTÓRICO", phoneSectionStyle);
            if (save.loanHistory == null || save.loanHistory.Count == 0)
            {
                GUILayout.Label("Nenhuma movimentação bancária ainda.", phoneSmallStyle);
            }
            else
            {
                // Mais recentes primeiro.
                for (int i = save.loanHistory.Count - 1; i >= 0; i--)
                {
                    var h = save.loanHistory[i];
                    if (h == null) continue;
                    // + crédito recebido, - saída de caixa, ! multa somada à dívida.
                    string sinal = h.type == "Contratação" ? "+"
                                 : h.type == "Multa" ? "!"
                                 : "-";
                    GUILayout.Label($"Dia {h.day} • {h.type} • {h.bankName}", phoneSmallStyle);
                    GUILayout.Label($"   {sinal}${h.amount:N0}", phoneSmallStyle);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Licenças / Habilitações

        private void DrawPhoneLicenses(Rect content)
        {
            DrawAppHeader(content, "Licenças");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            if (save == null)
            {
                GUILayout.Label("Sem partida ativa.", phoneBodyStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"Saldo: {save.GetFormattedMoney()} • {save.reputation}", phoneSmallStyle);
            GUILayout.Space(6);

            foreach (var li in DriverLicenseCatalog.All)
            {
                bool owned = save.HasLicense(li.id);
                GUILayout.Label($"{li.icon} {li.name}", phoneBodyStyle);
                GUILayout.Label(li.description, phoneSmallStyle);

                if (owned)
                {
                    GUILayout.Label("[OK] Adquirida", phoneLinkStyle);
                }
                else
                {
                    GUILayout.Label($" ${li.cost:N0} req. {li.reputationRequired}", phoneSmallStyle);
                    bool canRep = save.reputation >= li.reputationRequired;
                    bool canMoney = save.currentMoney >= li.cost;

                    if (!canRep)
                    {
                        GUILayout.Label($"[Bloqueado] Reputação insuficiente ({save.reputation}/{li.reputationRequired})", phoneLockStyle);
                    }
                    else if (!canMoney)
                    {
                        GUILayout.Label("[Bloqueado] Saldo insuficiente", phoneLockStyle);
                    }
                    else if (GUILayout.Button("Adquirir licença", phoneListButtonStyle, GUILayout.Height(26)))
                    {
                        save.currentMoney -= li.cost;
                        save.GrantLicense(li.id);
                        PersistSave();
                        ShowToast($"Licença obtida: {li.name}");
                    }
                }

                GUILayout.Space(4);
                DrawPhoneDivider();
                GUILayout.Space(5);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Garagem / Frota

        private static string CategoryIcon(VehicleCategory cat)
        {
            switch (cat)
            {
                case VehicleCategory.Tractor: return "";
                case VehicleCategory.Truck: return "";
                case VehicleCategory.Plane: return "";
                case VehicleCategory.Boat: return "";
                default: return "";
            }
        }

        private void DrawPhoneGarage(Rect content)
        {
            DrawAppHeader(content, "Garagem");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var vm = VehicleManager.Instance;
            var player = PlayerCharacterController.Instance;

            if (vm == null || vm.possessedVehicles == null || vm.possessedVehicles.Count == 0)
            {
                GUILayout.Label("Nenhum veículo na frota.", phoneBodyStyle);
                GUILayout.Label("Compre veículos no app Loja.", phoneSmallStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"Frota: {vm.possessedVehicles.Count} veículo(s)", phoneSectionStyle);
            GUILayout.Space(4);

            foreach (var v in vm.possessedVehicles)
            {
                if (v == null) continue;
                bool isCurrent = player != null && player.currentVehicle == v;
                string tag = isCurrent ? "[ATIVO] EM USO" : "";
                GUILayout.Label($"{CategoryIcon(v.category)} {v.vehicleName}{tag}", phoneBodyStyle);
                GUILayout.Space(2);
                DrawPhoneDivider();
                GUILayout.Space(4);
            }

            GUILayout.Space(2);
            if (GUILayout.Button("Alternar veículo ativo", phoneListButtonStyle, GUILayout.Height(28)))
            {
                vm.SwitchToNextVehicle();
                ShowToast("Veículo alternado.");
            }
            GUILayout.Label("Também disponível com [Tab] no jogo.", phoneSmallStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Concessionária / Loja

        private struct DealerOffer
        {
            public string icon;
            public string name;
            public string type; // chave aceita por VehicleManager.SpawnVehicle
            public long price;
            public DriverLicense required;
        }

        private static readonly DealerOffer[] DealerOffers =
        {
            new DealerOffer { icon = "", name = "Sedan Urbano", type = "car", price = 45000, required = DriverLicense.Standard },
            new DealerOffer { icon = "", name = "Van Utilitária", type = "van", price = 72000, required = DriverLicense.Standard },
            new DealerOffer { icon = "", name = "Caminhão de Carga", type = "truck", price = 140000, required = DriverLicense.Standard },
            new DealerOffer { icon = "", name = "Scania (Realista)", type = "scania", price = 210000, required = DriverLicense.Heavy },
            new DealerOffer { icon = "", name = "Trator Agrícola", type = "tractor", price = 85000, required = DriverLicense.Agriculture },
            new DealerOffer { icon = "", name = "Barco Pesqueiro", type = "boat", price = 90000, required = DriverLicense.Maritime },
            new DealerOffer { icon = "", name = "Aeronave Monomotor", type = "plane", price = 320000, required = DriverLicense.Aviation },
        };

        private void DrawPhoneDealer(Rect content)
        {
            DrawAppHeader(content, "Loja");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            GUILayout.Label($"Saldo: {(save != null ? save.GetFormattedMoney() : "--")}", phoneSmallStyle);
            GUILayout.Label("Entrega: à sua frente, na posição atual.", phoneSmallStyle);
            GUILayout.Space(6);

            foreach (var offer in DealerOffers)
            {
                var info = DriverLicenseCatalog.Get(offer.required);
                bool licensed = save == null || save.HasLicense(offer.required);
                bool canMoney = save != null && save.currentMoney >= offer.price;

                GUILayout.Label($"{offer.icon} {offer.name}", phoneBodyStyle);
                GUILayout.Label($" ${offer.price:N0}", phoneSmallStyle);

                if (!licensed)
                {
                    GUILayout.Label($"[Bloqueado] Requer licença: {info.icon} {info.name}", phoneLockStyle);
                }
                else if (!canMoney)
                {
                    GUILayout.Label("[Bloqueado] Saldo insuficiente", phoneLockStyle);
                }
                else if (GUILayout.Button("Comprar", phoneListButtonStyle, GUILayout.Height(26)))
                {
                    var vm = VehicleManager.Instance;
                    if (vm != null)
                    {
                        var spawned = vm.SpawnVehicle(offer.type);
                        if (spawned != null)
                        {
                            save.currentMoney -= offer.price;
                            PersistSave();
                            ShowToast($" {offer.name} adquirido!");
                        }
                        else
                        {
                            ShowToast("[Aviso] Não foi possível entregar o veículo.");
                        }
                    }
                }

                GUILayout.Space(4);
                DrawPhoneDivider();
                GUILayout.Space(5);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Estatísticas

        private void DrawPhoneStats(Rect content)
        {
            DrawAppHeader(content, "Estatísticas");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            if (save == null)
            {
                GUILayout.Label("Sem partida ativa.", phoneBodyStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            string careerName = save.starterCareer == StarterCareer.Farmer ? "Agricultor" :
                                save.starterCareer == StarterCareer.Trucker ? "Caminhoneiro" :
                                save.starterCareer == StarterCareer.Aviator ? "Aviador" : "Pescador";

            GUILayout.Label("CARREIRA", phoneSectionStyle);
            GUILayout.Label(careerName, phoneBodyStyle);
            GUILayout.Label($"{save.regionName}, {save.countryName}", phoneSmallStyle);

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);

            GUILayout.Label("DESEMPENHO", phoneSectionStyle);
            GUILayout.Label($"Saldo: {save.GetFormattedMoney()}", phoneBodyStyle);
            long net = save.currentMoney - save.startingMoney;
            GUILayout.Label($"Resultado: {(net >= 0 ? "+" : "")}${net:N0}", phoneSmallStyle);
            GUILayout.Label($"Entregas concluídas: {save.completedDeliveries}", phoneSmallStyle);
            GUILayout.Label($"Reputação: {save.reputation}", phoneSmallStyle);
            GUILayout.Label($"Dívida: ${save.TotalLoanDebt:N0}", phoneSmallStyle);

            int ownedLic = 0;
            foreach (var li in DriverLicenseCatalog.All)
                if (save.HasLicense(li.id)) ownedLic++;
            GUILayout.Label($"Licenças: {ownedLic}/{DriverLicenseCatalog.All.Length}", phoneSmallStyle);

            var vm = VehicleManager.Instance;
            int fleet = vm != null && vm.possessedVehicles != null ? vm.possessedVehicles.Count : 0;
            GUILayout.Label($"Frota: {fleet} veículo(s)", phoneSmallStyle);

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);

            GUILayout.Label("REGIÃO", phoneSectionStyle);
            GUILayout.Label($"Floresta: {save.forestPercent}% Minérios: {save.mineralsPercent}%", phoneSmallStyle);
            GUILayout.Label($"Arável: {save.arablePercent}% Água: {save.waterPercent}%", phoneSmallStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Utilitário

        /// <summary>Linha divisória fina dentro de um layout vertical (1 controle estável).</summary>
        private void DrawPhoneDivider()
        {
            Rect r = GUILayoutUtility.GetRect(1f, 1f);
            GUI.color = new Color(1f, 1f, 1f, 0.12f);
            GUI.DrawTexture(r, whiteTex);
            GUI.color = Color.white;
        }

        #endregion
    }
}
