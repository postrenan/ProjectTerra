namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Nível de dificuldade escolhido na criação da partida. Controla a intensidade da
    /// economia da campanha (preço do combustível, despesa diária, custo de reparo e
    /// velocidade de desgaste). Afeta apenas partidas com economia avançada ligada
    /// (<see cref="RegionSaveData.advancedEconomy"/>).
    /// </summary>
    public enum GameDifficulty
    {
        Relaxed = 0,  // Relaxado — custos suaves, recuperável
        Balanced = 1, // Equilibrado — padrão estilo FS/ETS2
        Hardcore = 2  // Hardcore — combustível caro, prazos e multas duros
    }

    /// <summary>
    /// Parâmetros econômicos associados a cada dificuldade. São imutáveis e consultados
    /// pelos sistemas de custo (combustível, relógio de despesas, reparo, desgaste).
    /// </summary>
    public class DifficultySettings
    {
        public readonly GameDifficulty id;
        public readonly string displayName;
        public readonly string description;

        /// <summary>Preço por litro de combustível no posto.</summary>
        public readonly float fuelPricePerLiter;
        /// <summary>Despesa fixa diária (seguro/estrutura), independente da frota.</summary>
        public readonly long upkeepBasePerDay;
        /// <summary>Despesa diária adicional por veículo possuído.</summary>
        public readonly long upkeepPerVehiclePerDay;
        /// <summary>Custo para reparar 1% de condição do veículo.</summary>
        public readonly long repairCostPerPercent;
        /// <summary>Multiplicador de velocidade de desgaste do veículo (distância e colisões).</summary>
        public readonly float wearMultiplier;
        /// <summary>Aperto dos prazos de entrega (0 = folgado … 1 = apertado). Usado na Fase 2.</summary>
        public readonly float deadlineTightness;

        public DifficultySettings(GameDifficulty id, string displayName, string description,
            float fuelPricePerLiter, long upkeepBasePerDay, long upkeepPerVehiclePerDay,
            long repairCostPerPercent, float wearMultiplier, float deadlineTightness)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.fuelPricePerLiter = fuelPricePerLiter;
            this.upkeepBasePerDay = upkeepBasePerDay;
            this.upkeepPerVehiclePerDay = upkeepPerVehiclePerDay;
            this.repairCostPerPercent = repairCostPerPercent;
            this.wearMultiplier = wearMultiplier;
            this.deadlineTightness = deadlineTightness;
        }
    }

    public static class DifficultySettingsCatalog
    {
        public static readonly DifficultySettings[] All =
        {
            new DifficultySettings(
                GameDifficulty.Relaxed, "Relaxado",
                "Custos suaves e prazos folgados. Dá para errar e se recuperar sem sufoco.",
                fuelPricePerLiter: 1.2f, upkeepBasePerDay: 0, upkeepPerVehiclePerDay: 120,
                repairCostPerPercent: 70, wearMultiplier: 0.5f, deadlineTightness: 0.25f),
            new DifficultySettings(
                GameDifficulty.Balanced, "Equilibrado",
                "Custos e prazos notáveis mas justos, no espírito de FS/ETS2.",
                fuelPricePerLiter: 2.2f, upkeepBasePerDay: 200, upkeepPerVehiclePerDay: 350,
                repairCostPerPercent: 120, wearMultiplier: 1.0f, deadlineTightness: 0.55f),
            new DifficultySettings(
                GameDifficulty.Hardcore, "Hardcore",
                "Combustível caro, despesas pesadas, reparo salgado e prazos apertados. Falência é real.",
                fuelPricePerLiter: 3.5f, upkeepBasePerDay: 600, upkeepPerVehiclePerDay: 800,
                repairCostPerPercent: 190, wearMultiplier: 1.8f, deadlineTightness: 0.85f),
        };

        public static DifficultySettings Get(GameDifficulty id)
        {
            foreach (var d in All)
                if (d.id == id) return d;
            return All[1]; // Equilibrado como fallback seguro
        }
    }
}
