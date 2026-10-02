using System;

namespace ProjectTerra.Gameplay
{
    public enum StarterCareer
    {
        Farmer = 0,     // 🌾 Agricultor
        Trucker = 1,    // 🚛 Motorista de Carga
        Aviator = 2,    // 🛩️ Aviador Particular
        Fisherman = 3   // 🎣 Pescador
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
        }

        public static RegionSaveData ReadBinary(System.IO.BinaryReader reader, byte version = 1)
        {
            var save = new RegionSaveData();
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

            return save;
        }
    }

    [Serializable]
    public class RegionSaveCollection
    {
        public System.Collections.Generic.List<RegionSaveData> saves = new System.Collections.Generic.List<RegionSaveData>();
    }
}
