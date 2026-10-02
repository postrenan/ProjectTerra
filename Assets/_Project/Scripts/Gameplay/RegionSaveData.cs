using System;

namespace ProjectTerra.Gameplay
{
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

        public long startingMoney;
        public long currentMoney;

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
                parts.Add($"🌲 Floresta: {forestPercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }
            if (mineralsPercent != originalMineralsPercent)
            {
                int diff = mineralsPercent - originalMineralsPercent;
                parts.Add($"⛏️ Minérios: {mineralsPercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }
            if (arablePercent != originalArablePercent)
            {
                int diff = arablePercent - originalArablePercent;
                parts.Add($"🌾 Arável: {arablePercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }
            if (waterPercent != originalWaterPercent)
            {
                int diff = waterPercent - originalWaterPercent;
                parts.Add($"💧 Água: {waterPercent}% ({(diff > 0 ? "+" : "")}{diff}%)");
            }

            return string.Join(" • ", parts);
        }
    }

    [Serializable]
    public class RegionSaveCollection
    {
        public System.Collections.Generic.List<RegionSaveData> saves = new System.Collections.Generic.List<RegionSaveData>();
    }
}
