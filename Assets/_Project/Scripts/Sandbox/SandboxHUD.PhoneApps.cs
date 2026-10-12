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
            public bool fragile;
            public bool urgent;
        }

        private List<PhoneJob> jobMarket;
        // Regeneração adiada para o Update (fora das passagens do OnGUI, evitando
        // mismatch de layout do GUILayout). O salt faz o botão "Atualizar"variar.
        private bool jobMarketDirty = false;
        private int jobMarketSalt = 0;

        /// <summary>Renova o mercado de fretes (ofertas novas no próximo frame). Chamado pelo GameClock a cada dia.</summary>
        public void RefreshJobMarket()
        {
            jobMarketDirty = true;
        }

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

                bool fragile = entry.lic == DriverLicense.Fragile || entry.lic == DriverLicense.Refrigerated || entry.lic == DriverLicense.HighValue;
                bool urgent = rng.Next(0, 100) < 18; // ~1 em cada 5-6 fretes é urgente
                if (urgent) reward = (long)(reward * 1.45f);

                jobMarket.Add(new PhoneJob
                {
                    icon = entry.icon,
                    title = $"{cargo}",
                    cargo = cargo,
                    weightKg = weight,
                    reward = reward,
                    rep = rng.Next(8, 28),
                    required = entry.lic,
                    fragile = fragile,
                    urgent = urgent
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

                string tags = (j.urgent ? "[URGENTE] " : "") + (j.fragile ? "[Frágil] " : "");
                GUILayout.Label($"{tags}{j.icon} {j.title}", phoneBodyStyle);
                GUILayout.Label($" {j.weightKg:F0} kg ${j.reward:N0} +{j.rep}", phoneSmallStyle);
                if (j.urgent) GUILayout.Label("Prazo apertado • recompensa aumentada.", phoneSmallStyle);
                if (j.fragile) GUILayout.Label("Conduza com cuidado: trancos reduzem o pagamento.", phoneSmallStyle);

                if (!licensed)
                {
                    GUILayout.Label($"[Bloqueado] Requer licença: {info.icon} {info.name}", phoneLockStyle);
                }
                else if (GUILayout.Button("[OK] Aceitar frete", phoneListButtonStyle, GUILayout.Height(26)))
                {
                    if (mm != null)
                    {
                        mm.AcceptMarketContract(j.title, $"Transportar {j.cargo} até o destino indicado no mapa.",
                            j.cargo, j.weightKg, j.reward, j.rep, j.fragile, j.urgent);
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

            if (save.advancedEconomy)
            {
                GUILayout.Space(8);
                DrawPhoneDivider();
                GUILayout.Space(4);

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

                GUILayout.Label("DESPESAS", phoneSectionStyle);
                GUILayout.Label($"Dificuldade: {cfg.displayName} • Dia {save.gameDay}", phoneSmallStyle);
                GUILayout.Label($"Despesa diária estimada: ${dailyUpkeep:N0}", phoneBodyStyle);
                GUILayout.Label($"(seguro ${cfg.upkeepBasePerDay:N0} + frota de {fleet})", phoneSmallStyle);
                GUILayout.Label($"Total gasto em custos: ${save.totalExpenses:N0}", phoneSmallStyle);
                GUILayout.Label($"Combustível: ${save.EffectiveFuelPrice:F2}/L", phoneSmallStyle);
            }

            GUILayout.Space(8);
            DrawPhoneDivider();
            GUILayout.Space(4);

            GUILayout.Label("BANCO", phoneSectionStyle);
            GUILayout.Label($"Dívida atual: ${save.loanDebt:N0}", phoneBodyStyle);
            GUILayout.Space(4);

            const long loanAmount = 50000;
            const long loanDebtAdded = 55000; // 10% de juros
            const long maxDebt = 500000;

            if (save.loanDebt + loanDebtAdded <= maxDebt)
            {
                if (GUILayout.Button($"Empréstimo +${loanAmount:N0}", phoneListButtonStyle, GUILayout.Height(28)))
                {
                    save.currentMoney += loanAmount;
                    save.loanDebt += loanDebtAdded;
                    PersistSave();
                    ShowToast($"Empréstimo de ${loanAmount:N0} creditado.");
                }
                GUILayout.Label("Juros de 10% sobre o valor tomado.", phoneSmallStyle);
            }
            else
            {
                GUILayout.Label("[Bloqueado] Limite de crédito atingido.", phoneLockStyle);
            }

            GUILayout.Space(4);
            if (save.loanDebt > 0)
            {
                long pay = System.Math.Min(save.currentMoney, save.loanDebt);
                bool canPay = pay > 0;
                if (canPay && GUILayout.Button($"Quitar ${pay:N0}", phoneListButtonStyle, GUILayout.Height(28)))
                {
                    save.currentMoney -= pay;
                    save.loanDebt -= pay;
                    PersistSave();
                    ShowToast($"Dívida reduzida em ${pay:N0}.");
                }
                if (!canPay) GUILayout.Label("Sem saldo para abater a dívida.", phoneSmallStyle);
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

            var save = CurrentSave;
            bool advanced = save != null && save.advancedEconomy;
            long perPercent = save != null ? save.DifficultyConfig.repairCostPerPercent : 0;

            foreach (var v in vm.possessedVehicles)
            {
                if (v == null) continue;
                bool isCurrent = player != null && player.currentVehicle == v;
                string tag = isCurrent ? "[ATIVO] EM USO" : "";
                GUILayout.Label($"{CategoryIcon(v.category)} {v.vehicleName}{tag}", phoneBodyStyle);

                if (advanced)
                {
                    string condTag = v.conditionPercent > 60f ? "Bom" : (v.conditionPercent > 30f ? "Desgastado" : "Crítico");
                    GUILayout.Label($"Condição: {v.conditionPercent:F0}% ({condTag}) • Tanque: {v.fuelPercent:F0}%", phoneSmallStyle);

                    int missing = Mathf.RoundToInt(100f - v.conditionPercent);
                    if (missing >= 1 && save != null)
                    {
                        long repairCost = perPercent * missing;
                        bool canPay = save.currentMoney >= repairCost;
                        if (!canPay)
                        {
                            GUILayout.Label($"[Bloqueado] Reparo total: ${repairCost:N0} (saldo insuficiente)", phoneLockStyle);
                        }
                        else if (GUILayout.Button($"Reparar — ${repairCost:N0}", phoneListButtonStyle, GUILayout.Height(24)))
                        {
                            v.conditionPercent = 100f;
                            save.currentMoney -= repairCost;
                            save.totalExpenses += repairCost;
                            PersistSave();
                            ShowToast($"{v.vehicleName} reparado (-${repairCost:N0}).");
                        }
                    }
                    else
                    {
                        GUILayout.Label("[OK] Em perfeito estado.", phoneLinkStyle);
                    }
                }

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

        #region Posto de Combustível

        /// <summary>Veículo que o posto/mecânico atende: o que o jogador conduz, ou o primeiro da frota.</summary>
        private VehicleController ServiceableVehicle
        {
            get
            {
                var player = PlayerCharacterController.Instance;
                if (player != null && player.currentVehicle != null) return player.currentVehicle;
                var vm = VehicleManager.Instance;
                if (vm != null && vm.possessedVehicles != null)
                {
                    foreach (var v in vm.possessedVehicles)
                        if (v != null) return v;
                }
                return null;
            }
        }

        private void DrawPhoneFuelStation(Rect content)
        {
            DrawAppHeader(content, "Posto");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            var vehicle = ServiceableVehicle;

            float price = save != null ? save.EffectiveFuelPrice : 2.2f;
            GUILayout.Label("POSTO TERRAFUEL", phoneSectionStyle);
            GUILayout.Label($"Preço: ${price:F2}/litro", phoneBodyStyle);
            GUILayout.Label("Atende o veículo em uso (ou o primeiro da frota).", phoneSmallStyle);

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);

            if (vehicle == null)
            {
                GUILayout.Label("Nenhum veículo para abastecer.", phoneBodyStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label(vehicle.vehicleName, phoneBodyStyle);
            GUILayout.Label($"Tanque: {vehicle.fuelPercent:F0}% de {vehicle.fuelTankLiters:F0} L", phoneSmallStyle);

            float litersNeeded = Mathf.Max(0f, (100f - vehicle.fuelPercent) / 100f * vehicle.fuelTankLiters);
            long cost = (long)Mathf.Ceil(litersNeeded * price);

            GUILayout.Space(4);
            if (litersNeeded < 0.5f)
            {
                GUILayout.Label("[OK] Tanque cheio.", phoneLinkStyle);
            }
            else if (save == null)
            {
                GUILayout.Label("Sem partida ativa.", phoneSmallStyle);
            }
            else
            {
                GUILayout.Label($"Encher: {litersNeeded:F0} L • ${cost:N0}", phoneSmallStyle);
                bool canPay = save.currentMoney >= cost;
                if (!canPay)
                {
                    GUILayout.Label("[Bloqueado] Saldo insuficiente.", phoneLockStyle);
                    // Reabastecimento parcial com o que o jogador tem.
                    float litersAfford = cost > 0 ? (float)save.currentMoney / price : 0f;
                    if (litersAfford > 0.5f && GUILayout.Button($"Abastecer o possível ({litersAfford:F0} L)", phoneListButtonStyle, GUILayout.Height(26)))
                    {
                        long spend = save.currentMoney;
                        vehicle.Refuel(litersAfford / vehicle.fuelTankLiters * 100f);
                        save.currentMoney -= spend;
                        save.totalExpenses += spend;
                        PersistSave();
                        ShowToast($"Abastecido parcialmente (-${spend:N0}).");
                    }
                }
                else if (GUILayout.Button($"Encher tanque — ${cost:N0}", phoneListButtonStyle, GUILayout.Height(28)))
                {
                    vehicle.Refuel(100f);
                    save.currentMoney -= cost;
                    save.totalExpenses += cost;
                    PersistSave();
                    ShowToast($"Tanque cheio (-${cost:N0}).");
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Metas de Campanha

        private void DrawPhoneGoals(Rect content)
        {
            DrawAppHeader(content, "Metas");
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

            GUILayout.Label($"Progresso: {claimed}/{CampaignGoalCatalog.All.Length} metas", phoneSectionStyle);
            GUILayout.Label("Cumpra objetivos e resgate as recompensas.", phoneSmallStyle);
            GUILayout.Space(6);

            foreach (var g in CampaignGoalCatalog.All)
            {
                bool isClaimed = CampaignGoalCatalog.IsClaimed(save, g.bit);
                bool met = g.isMet(save, fleet, lic);

                GUILayout.Label(g.title, phoneBodyStyle);
                GUILayout.Label(g.description, phoneSmallStyle);
                string reward = (g.rewardMoney > 0 ? $"${g.rewardMoney:N0} " : "") + (g.rewardReputation > 0 ? $"+{g.rewardReputation} rep" : "");

                if (isClaimed)
                {
                    GUILayout.Label("[OK] Resgatada", phoneLinkStyle);
                }
                else if (met)
                {
                    if (GUILayout.Button($"Resgatar recompensa ({reward})", phoneListButtonStyle, GUILayout.Height(26)))
                    {
                        save.currentMoney += g.rewardMoney;
                        save.reputation += g.rewardReputation;
                        CampaignGoalCatalog.Claim(save, g.bit);
                        PersistSave();
                        ShowToast($"Meta concluída: {g.title} ({reward})");
                    }
                }
                else
                {
                    GUILayout.Label($"[Bloqueado] Em progresso • recompensa: {reward}", phoneLockStyle);
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
            if (save.advancedEconomy) GUILayout.Label($"Entregas atrasadas: {save.lateDeliveries}", phoneSmallStyle);
            GUILayout.Label($"Reputação: {save.reputation}", phoneSmallStyle);
            GUILayout.Label($"Dívida: ${save.loanDebt:N0}", phoneSmallStyle);

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
