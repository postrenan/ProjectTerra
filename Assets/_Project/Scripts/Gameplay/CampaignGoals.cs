using System;

namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Uma meta de campanha: um objetivo de progressão que, ao ser cumprido, pode ser
    /// resgatado pelo jogador por uma recompensa. O estado de resgate é guardado como
    /// bitmask em <see cref="RegionSaveData.milestonesClaimed"/>.
    /// </summary>
    public class CampaignGoal
    {
        public readonly int bit;
        public readonly string title;
        public readonly string description;
        public readonly long rewardMoney;
        public readonly int rewardReputation;
        // Predicado: (save, frota, nº de licenças) → meta cumprida?
        public readonly Func<RegionSaveData, int, int, bool> isMet;

        public CampaignGoal(int bit, string title, string description, long rewardMoney, int rewardReputation,
            Func<RegionSaveData, int, int, bool> isMet)
        {
            this.bit = bit;
            this.title = title;
            this.description = description;
            this.rewardMoney = rewardMoney;
            this.rewardReputation = rewardReputation;
            this.isMet = isMet;
        }
    }

    public static class CampaignGoalCatalog
    {
        public static readonly CampaignGoal[] All =
        {
            new CampaignGoal(0, "Primeira Entrega", "Conclua seu primeiro contrato de frete.",
                5000, 10, (s, fleet, lic) => s.completedDeliveries >= 1),
            new CampaignGoal(1, "Transportador", "Conclua 10 entregas.",
                25000, 15, (s, fleet, lic) => s.completedDeliveries >= 10),
            new CampaignGoal(2, "Frota em Crescimento", "Tenha ao menos 2 veículos na frota.",
                15000, 10, (s, fleet, lic) => fleet >= 2),
            new CampaignGoal(3, "Habilitado", "Possua 4 ou mais licenças.",
                30000, 20, (s, fleet, lic) => lic >= 4),
            new CampaignGoal(4, "Reputação de Ouro", "Alcance 300 de reputação.",
                40000, 0, (s, fleet, lic) => s.reputation >= 300),
            new CampaignGoal(5, "Empresário", "Acumule $1.000.000 em caixa.",
                0, 50, (s, fleet, lic) => s.currentMoney >= 1000000),
            new CampaignGoal(6, "Veterano das Estradas", "Conclua 30 entregas.",
                80000, 30, (s, fleet, lic) => s.completedDeliveries >= 30),
            new CampaignGoal(7, "Magnata da Logística", "Acumule $5.000.000 em caixa.",
                0, 100, (s, fleet, lic) => s.currentMoney >= 5000000),
        };

        public static bool IsClaimed(RegionSaveData save, int bit)
        {
            return (save.milestonesClaimed & (1L << bit)) != 0;
        }

        public static void Claim(RegionSaveData save, int bit)
        {
            save.milestonesClaimed |= (1L << bit);
        }

        /// <summary>Número de licenças adquiridas no save (conta os bits do catálogo de licenças).</summary>
        public static int CountLicenses(RegionSaveData save)
        {
            int n = 0;
            foreach (var li in DriverLicenseCatalog.All)
                if (save.HasLicense(li.id)) n++;
            return n;
        }
    }
}
