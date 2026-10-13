using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Gameplay
{
    public enum StarterCareer
    {
        Farmer = 0, // Agricultor
        Trucker = 1, // Motorista de Carga
        Aviator = 2, // Aviador Particular
        Fisherman = 3 // Pescador
    }

    /// <summary>
    /// Um empréstimo ativo com um banco específico. A dívida é paga em parcelas:
    /// <see cref="remainingDebt"/> é o total ainda devido (principal + juros) e
    /// <see cref="installmentAmount"/> é quanto cada parcela abate.
    /// </summary>
    [Serializable]
    public class BankLoan
    {
        public string bankId;
        public string bankName;
        public long remainingDebt;     // total ainda devido (com juros)
        public long installmentAmount; // valor de cada parcela
        public int installmentsPaid;   // parcelas já quitadas
        public int installmentsTotal;  // número total de parcelas contratadas
        public long startDay;          // dia (gameDay) em que foi contratado
        public long lastPenaltyDay;    // último gameDay em que recebeu multa por atraso

        public int InstallmentsRemaining => Mathf.Max(0, installmentsTotal - installmentsPaid);
        public bool IsPaidOff => remainingDebt <= 0;
    }

    /// <summary>Registro de uma movimentação bancária, para o histórico do app Finanças.</summary>
    [Serializable]
    public class LoanTransaction
    {
        public long day;        // gameDay em que ocorreu
        public string bankName;
        public string type;     // "Contratação", "Pagamento", "Quitação", "Multa"
        public long amount;     // valor movimentado (sempre positivo)
    }

    [Serializable]
    public class RegionSaveData
    {
        public string saveId;
        public string saveName;
        public int regionId;
        public string regionName;
        public string countryName;
        public string regionType;
        public string lastSavedDate;

        public StarterCareer starterCareer = StarterCareer.Farmer;
        public int reputation = 100;
        public int completedDeliveries = 0;
        public string currentVehicleType = "StarterVehicle";

        public long startingMoney;
        public long currentMoney;

        // --- Progressão estilo FS / ETS2 (save v2) ---
        // Bitflags de DriverLicense. 0 = ainda não inicializado → derivado da carreira.
        public int licenses = 0;
        // Dívida bancária em aberto (empréstimos tomados no app Finanças).
        // Mantido apenas para migração de saves v2; a dívida real vive em loans.
        public long loanDebt = 0;

        // --- Empréstimos (save v3) ---
        // Empréstimos ativos, até RegionSaveData.MaxActiveLoans ao mesmo tempo,
        // cada um com um banco diferente e pago em parcelas.
        public List<BankLoan> loans = new List<BankLoan>();

        public const int MaxActiveLoans = 3;

        // Dia corrente do jogo (incrementa a cada virada de meia-noite in-game).
        // Usado para agendar parcelas, cobrar multa por atraso e as despesas diárias.
        public long gameDay = 0;

        // Histórico de movimentações bancárias (mais recentes no fim da lista).
        public List<LoanTransaction> loanHistory = new List<LoanTransaction>();

        public const int MaxHistoryEntries = 40;

        // --- Economia avançada da campanha (save v3) ---
        // Gate mestre: só partidas criadas com os sistemas novos ligam custos operacionais,
        // despesa diária, prazos e metas. Saves v1/v2 carregam com isto em false e seguem
        // jogando como antes (mas o sistema de empréstimos acima vale para todos).
        public bool advancedEconomy = false;
        // Dificuldade escolhida na criação (índice de GameDifficulty).
        public int difficulty = (int)GameDifficulty.Balanced;

        // Parcelas de terreno compradas na Loja de Terrenos (índice linha * colunas + coluna).
        public List<int> ownedParcels = new List<int>();
        // Preço corrente do combustível por litro. 0 = ainda não definido → derivado da dificuldade.
        public float fuelPricePerLiter = 0f;
        // Total gasto em custos operacionais (combustível, reparo, despesa diária) — estatística.
        public long totalExpenses = 0;
        // Bitmask das metas de campanha já resgatadas (ver CampaignGoalCatalog).
        public long milestonesClaimed = 0;
        // Entregas atrasadas e perdidas (estatística / pressão).
        public int lateDeliveries = 0;

        public GameDifficulty Difficulty => (GameDifficulty)difficulty;

        /// <summary>Parâmetros econômicos da dificuldade desta partida.</summary>
        public DifficultySettings DifficultyConfig => DifficultySettingsCatalog.Get(Difficulty);

        /// <summary>Preço do combustível efetivo; deriva da dificuldade se nunca definido.</summary>
        public float EffectiveFuelPrice => fuelPricePerLiter > 0f ? fuelPricePerLiter : DifficultyConfig.fuelPricePerLiter;

        /// <summary>Adiciona um registro ao histórico, mantendo apenas os mais recentes.</summary>
        public void AddLoanHistory(string bankName, string type, long amount)
        {
            if (loanHistory == null) loanHistory = new List<LoanTransaction>();
            loanHistory.Add(new LoanTransaction { day = gameDay, bankName = bankName, type = type, amount = amount });
            int overflow = loanHistory.Count - MaxHistoryEntries;
            if (overflow > 0) loanHistory.RemoveRange(0, overflow);
        }

        /// <summary>Soma de toda a dívida bancária em aberto.</summary>
        public long TotalLoanDebt
        {
            get
            {
                long sum = 0;
                if (loans != null)
                    foreach (var l in loans)
                        if (l != null) sum += l.remainingDebt;
                return sum;
            }
        }

        /// <summary>True se já existe um empréstimo ativo com o banco informado.</summary>
        public bool HasLoanWith(string bankId)
        {
            if (loans == null) return false;
            foreach (var l in loans)
                if (l != null && l.bankId == bankId) return true;
            return false;
        }

        public int ActiveLoanCount => loans != null ? loans.Count : 0;

        /// <summary>
        /// Avança um dia de jogo e cobra multa por atraso em cada empréstimo com
        /// parcelas vencidas e ainda não pagas (uma parcela vence por dia a partir
        /// da contratação). A multa entra na dívida e no histórico. Retorna o total
        /// de multas aplicadas neste dia.
        /// </summary>
        public long AdvanceDayAndChargeLate(int lateFeePercent)
        {
            gameDay++;
            long totalPenalty = 0;
            if (loans == null) return 0;
            foreach (var l in loans)
            {
                if (l == null || l.IsPaidOff) continue;

                long elapsed = Math.Max(0, gameDay - l.startDay);
                long expectedPaid = Math.Min(l.installmentsTotal, elapsed);
                bool overdue = l.installmentsPaid < expectedPaid;

                if (overdue && l.lastPenaltyDay < gameDay)
                {
                    long fee = Math.Max(1, (l.installmentAmount * lateFeePercent + 99) / 100); // ceil
                    l.remainingDebt += fee;
                    l.lastPenaltyDay = gameDay;
                    totalPenalty += fee;
                    AddLoanHistory(l.bankName, "Multa", fee);
                }
            }
            return totalPenalty;
        }

        /// <summary>Licenças efetivas do jogador; deriva da carreira se nunca inicializadas.</summary>
        public DriverLicense Licenses
        {
            get
            {
                if (licenses == 0) licenses = (int)DriverLicenseCatalog.DefaultFor(starterCareer);
                return (DriverLicense)licenses;
            }
            set { licenses = (int)value; }
        }

        public bool HasLicense(DriverLicense lic) => (Licenses & lic) == lic;
        public void GrantLicense(DriverLicense lic) { Licenses |= lic; }
        public void AddLicense(DriverLicense lic) => GrantLicense(lic);

        public int forestPercent;
        public int mineralsPercent;
        public int arablePercent;
        public int waterPercent;

        public int originalForestPercent;
        public int originalMineralsPercent;
        public int originalArablePercent;
        public int originalWaterPercent;

        public bool IsStatsModified =>
            forestPercent != originalForestPercent ||
            mineralsPercent != originalMineralsPercent ||
            arablePercent != originalArablePercent ||
            waterPercent != originalWaterPercent;

        public string GetFormattedMoney()
        {
            return $"${currentMoney:N0}";
        }

        public string GetStatChangesSummary()
        {
            if (!IsStatsModified) return "Status originais mantidos (sem alterações)";

            var parts = new System.Collections.Generic.List<string>();
            if (forestPercent != originalForestPercent)
            {
                int diff = forestPercent - originalForestPercent;
                parts.Add($"Floresta: {forestPercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }
            if (mineralsPercent != originalMineralsPercent)
            {
                int diff = mineralsPercent - originalMineralsPercent;
                parts.Add($"Minérios: {mineralsPercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }
            if (arablePercent != originalArablePercent)
            {
                int diff = arablePercent - originalArablePercent;
                parts.Add($"Arável: {arablePercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }
            if (waterPercent != originalWaterPercent)
            {
                int diff = waterPercent - originalWaterPercent;
                parts.Add($" Água: {waterPercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }

            return string.Join(" • ", parts);
        }

        public void WriteBinary(System.IO.BinaryWriter writer)
        {
            writer.Write(saveId ?? "");
            writer.Write(saveName ?? "");
            writer.Write(regionId);
            writer.Write(regionName ?? "");
            writer.Write(countryName ?? "");
            writer.Write(regionType ?? "");
            writer.Write(lastSavedDate ?? "");

            writer.Write((int)starterCareer);
            writer.Write(reputation);
            writer.Write(completedDeliveries);
            writer.Write(currentVehicleType ?? "StarterVehicle");

            writer.Write(startingMoney);
            writer.Write(currentMoney);

            writer.Write(forestPercent);
            writer.Write(mineralsPercent);
            writer.Write(arablePercent);
            writer.Write(waterPercent);

            writer.Write(originalForestPercent);
            writer.Write(originalMineralsPercent);
            writer.Write(originalArablePercent);
            writer.Write(originalWaterPercent);

            // v2
            writer.Write(licenses);

            // v3: lista de empréstimos (substitui o campo único loanDebt).
            int loanCount = loans != null ? loans.Count : 0;
            writer.Write(loanCount);
            for (int i = 0; i < loanCount; i++)
            {
                var l = loans[i];
                writer.Write(l.bankId ?? "");
                writer.Write(l.bankName ?? "");
                writer.Write(l.remainingDebt);
                writer.Write(l.installmentAmount);
                writer.Write(l.installmentsPaid);
                writer.Write(l.installmentsTotal);
                writer.Write(l.startDay);
                writer.Write(l.lastPenaltyDay);
            }

            // v3: calendário e histórico bancário.
            writer.Write(gameDay);
            int histCount = loanHistory != null ? loanHistory.Count : 0;
            writer.Write(histCount);
            for (int i = 0; i < histCount; i++)
            {
                var h = loanHistory[i];
                writer.Write(h.day);
                writer.Write(h.bankName ?? "");
                writer.Write(h.type ?? "");
                writer.Write(h.amount);
            }

            // v3: economia avançada da campanha (custos, dificuldade, metas).
            writer.Write(advancedEconomy);
            writer.Write(difficulty);
            writer.Write(fuelPricePerLiter);
            writer.Write(totalExpenses);
            writer.Write(milestonesClaimed);
            writer.Write(lateDeliveries);
        }

        public const byte CurrentVersion = 3;

        /// <summary>
        /// Deserializa um save. O byte de versão decide o layout: cada layout tem o seu
        /// próprio leitor e uma versão desconhecida é recusada em vez de ser adivinhada.
        /// </summary>
        public static RegionSaveData ReadBinary(System.IO.BinaryReader reader, byte version = CurrentVersion)
        {
            switch (version)
            {
                case 1:
                    return ReadBinaryV1(reader);
                case 2:
                    return ReadBinaryV2(reader);
                case 3:
                    return ReadBinaryV3(reader);
                default:
                    // Antes o byte de versão era lido e descartado, sempre usando o layout v1.
                    // Um save de versão futura (com um campo extra) seria lido fora de fase
                    // campo-a-campo: currentMoney leria um string prefixada por tamanho como
                    // long, e a partida apareceria com economia corrompida em vez de falhar alto.
                    Debug.LogError($"[RegionSaveData] Save na versão {version} é mais recente que a suportada ({CurrentVersion}). Recusando carregar para não corromper a partida.");
                    return null;
            }
        }

        /// <summary>Lê o bloco comum a todas as versões (layout original v1).</summary>
        private static void ReadCore(System.IO.BinaryReader reader, RegionSaveData save)
        {
            save.saveId = reader.ReadString();
            save.saveName = reader.ReadString();
            save.regionId = reader.ReadInt32();
            save.regionName = reader.ReadString();
            save.countryName = reader.ReadString();
            save.regionType = reader.ReadString();
            save.lastSavedDate = reader.ReadString();

            save.starterCareer = (StarterCareer)reader.ReadInt32();
            save.reputation = reader.ReadInt32();
            save.completedDeliveries = reader.ReadInt32();
            save.currentVehicleType = reader.ReadString();

            save.startingMoney = reader.ReadInt64();
            save.currentMoney = reader.ReadInt64();

            save.forestPercent = reader.ReadInt32();
            save.mineralsPercent = reader.ReadInt32();
            save.arablePercent = reader.ReadInt32();
            save.waterPercent = reader.ReadInt32();

            save.originalForestPercent = reader.ReadInt32();
            save.originalMineralsPercent = reader.ReadInt32();
            save.originalArablePercent = reader.ReadInt32();
            save.originalWaterPercent = reader.ReadInt32();
        }

        private static RegionSaveData ReadBinaryV1(System.IO.BinaryReader reader)
        {
            var save = new RegionSaveData();
            ReadCore(reader, save);
            // Campos v2 ausentes: licenses=0 (derivado da carreira sob demanda), loanDebt=0.
            return save;
        }

        private static RegionSaveData ReadBinaryV2(System.IO.BinaryReader reader)
        {
            var save = new RegionSaveData();
            ReadCore(reader, save);
            save.licenses = reader.ReadInt32();
            save.loanDebt = reader.ReadInt64();
            // Migra a dívida única v2 para um empréstimo legado pago em 10 parcelas.
            MigrateLegacyLoan(save);
            return save;
        }

        private static RegionSaveData ReadBinaryV3(System.IO.BinaryReader reader)
        {
            var save = new RegionSaveData();
            ReadCore(reader, save);
            save.licenses = reader.ReadInt32();

            int loanCount = reader.ReadInt32();
            save.loans = new List<BankLoan>(Mathf.Max(0, loanCount));
            for (int i = 0; i < loanCount; i++)
            {
                var l = new BankLoan
                {
                    bankId = reader.ReadString(),
                    bankName = reader.ReadString(),
                    remainingDebt = reader.ReadInt64(),
                    installmentAmount = reader.ReadInt64(),
                    installmentsPaid = reader.ReadInt32(),
                    installmentsTotal = reader.ReadInt32(),
                    startDay = reader.ReadInt64(),
                    lastPenaltyDay = reader.ReadInt64(),
                };
                save.loans.Add(l);
            }

            save.gameDay = reader.ReadInt64();
            int histCount = reader.ReadInt32();
            save.loanHistory = new List<LoanTransaction>(Mathf.Max(0, histCount));
            for (int i = 0; i < histCount; i++)
            {
                save.loanHistory.Add(new LoanTransaction
                {
                    day = reader.ReadInt64(),
                    bankName = reader.ReadString(),
                    type = reader.ReadString(),
                    amount = reader.ReadInt64(),
                });
            }

            // v3: economia avançada da campanha.
            save.advancedEconomy = reader.ReadBoolean();
            save.difficulty = reader.ReadInt32();
            save.fuelPricePerLiter = reader.ReadSingle();
            save.totalExpenses = reader.ReadInt64();
            save.milestonesClaimed = reader.ReadInt64();
            save.lateDeliveries = reader.ReadInt32();
            return save;
        }

        /// <summary>
        /// Converte a dívida única de um save antigo (loanDebt) num empréstimo legado,
        /// parcelado em 10 vezes, para que continue quitável no novo sistema.
        /// </summary>
        private static void MigrateLegacyLoan(RegionSaveData save)
        {
            if (save.loanDebt <= 0) return;
            const int parcelas = 10;
            long parcela = (save.loanDebt + parcelas - 1) / parcelas; // arredonda para cima
            save.loans.Add(new BankLoan
            {
                bankId = "legado",
                bankName = "Empréstimo anterior",
                remainingDebt = save.loanDebt,
                installmentAmount = parcela,
                installmentsPaid = 0,
                installmentsTotal = parcelas,
            });
            save.loanDebt = 0;
        }
    }

    [Serializable]
    public class RegionSaveCollection
    {
        public System.Collections.Generic.List<RegionSaveData> saves = new System.Collections.Generic.List<RegionSaveData>();
    }
}
