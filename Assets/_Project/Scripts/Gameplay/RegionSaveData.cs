using System;
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
        public long loanDebt = 0;

        // --- Economia avançada da campanha (save v3) ---
        // Gate mestre: só partidas criadas com os sistemas novos ligam custos operacionais,
        // despesa diária e (futuro) prazos. Saves v1/v2 carregam com isto em false e seguem
        // jogando como antes.
        public bool advancedEconomy = false;
        // Dificuldade escolhida na criação (índice de GameDifficulty).
        public int difficulty = (int)GameDifficulty.Balanced;
        // Dia de jogo corrente (começa em 1), avançado pelo GameClock à meia-noite.
        public int gameDay = 1;
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
            writer.Write(loanDebt);

            // v3
            writer.Write(advancedEconomy);
            writer.Write(difficulty);
            writer.Write(gameDay);
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
            return save;
        }

        private static RegionSaveData ReadBinaryV3(System.IO.BinaryReader reader)
        {
            var save = new RegionSaveData();
            ReadCore(reader, save);
            save.licenses = reader.ReadInt32();
            save.loanDebt = reader.ReadInt64();
            save.advancedEconomy = reader.ReadBoolean();
            save.difficulty = reader.ReadInt32();
            save.gameDay = reader.ReadInt32();
            save.fuelPricePerLiter = reader.ReadSingle();
            save.totalExpenses = reader.ReadInt64();
            save.milestonesClaimed = reader.ReadInt64();
            save.lateDeliveries = reader.ReadInt32();
            return save;
        }
    }

    [Serializable]
    public class RegionSaveCollection
    {
        public System.Collections.Generic.List<RegionSaveData> saves = new System.Collections.Generic.List<RegionSaveData>();
    }
}
