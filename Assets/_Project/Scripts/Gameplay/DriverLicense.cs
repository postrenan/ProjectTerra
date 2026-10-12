using System;

namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Licenças/habilitações do jogador, no espírito das skills do Euro Truck (ADR,
    /// carga frágil, alto valor...) e das categorias do Farming Simulator (agrícola,
    /// animais). Cada licença destrava tipos de frete no mercado e classes de veículo
    /// na concessionária. Armazenada como bitflags no save.
    /// </summary>
    [Flags]
    public enum DriverLicense
    {
        None         = 0,
        Standard     = 1 << 0, // 🚙 Carga geral rodoviária (básica)
        Agriculture  = 1 << 1, // 🌾 Agrícola (grãos, colheita, maquinário)
        Refrigerated = 1 << 2, // ❄️ Frigorificada (perecíveis)
        Fragile      = 1 << 3, // 📦 Carga frágil
        Livestock    = 1 << 4, // 🐄 Animais vivos
        Heavy        = 1 << 5, // 🏗️ Carga pesada / superdimensionada
        HighValue    = 1 << 6, // 💎 Alto valor
        Hazmat       = 1 << 7, // ☢️ Materiais perigosos (ADR)
        Aviation     = 1 << 8, // 🛩️ Piloto
        Maritime     = 1 << 9  // ⚓ Náutica / pesca
    }

    /// <summary>Metadados de exibição e custo de uma licença.</summary>
    public class LicenseInfo
    {
        public readonly DriverLicense id;
        public readonly string icon;
        public readonly string name;
        public readonly string description;
        public readonly long cost;
        public readonly int reputationRequired;

        public LicenseInfo(DriverLicense id, string icon, string name, string description, long cost, int reputationRequired)
        {
            this.id = id;
            this.icon = icon;
            this.name = name;
            this.description = description;
            this.cost = cost;
            this.reputationRequired = reputationRequired;
        }
    }

    public static class DriverLicenseCatalog
    {
        // Ordenadas por dificuldade de obtenção (custo/reputação crescentes).
        public static readonly LicenseInfo[] All =
        {
            new LicenseInfo(DriverLicense.Standard,     "🚙", "Carga Geral",     "Fretes rodoviários comuns de carga seca. Habilitação básica.", 0,      0),
            new LicenseInfo(DriverLicense.Agriculture,  "🌾", "Agrícola",        "Grãos, colheita, insumos e maquinário agrícola.",             15000,  0),
            new LicenseInfo(DriverLicense.Fragile,      "📦", "Carga Frágil",    "Mercadorias delicadas que exigem condução suave.",            25000,  100),
            new LicenseInfo(DriverLicense.Livestock,    "🐄", "Animais Vivos",   "Transporte de gado e animais vivos.",                         30000,  150),
            new LicenseInfo(DriverLicense.Refrigerated, "❄️", "Frigorificada",   "Perecíveis em cadeia de frio (laticínios, carnes).",          35000,  120),
            new LicenseInfo(DriverLicense.Maritime,     "⚓", "Náutica",         "Habilitação para operar embarcações e pesca.",                40000,  100),
            new LicenseInfo(DriverLicense.Heavy,        "🏗️", "Carga Pesada",    "Cargas superdimensionadas e maquinário pesado.",              45000,  150),
            new LicenseInfo(DriverLicense.HighValue,    "💎", "Alto Valor",      "Cargas valiosas com segurança reforçada.",                    50000,  300),
            new LicenseInfo(DriverLicense.Hazmat,       "☢️", "Perigosa (ADR)",  "Materiais perigosos, inflamáveis e químicos (ADR).",          60000,  250),
            new LicenseInfo(DriverLicense.Aviation,     "🛩️", "Piloto",          "Habilitação para operar aeronaves.",                          120000, 400),
        };

        public static LicenseInfo Get(DriverLicense id)
        {
            foreach (var l in All)
                if (l.id == id) return l;
            return null;
        }

        /// <summary>Licenças concedidas de graça conforme a carreira inicial escolhida.</summary>
        public static DriverLicense DefaultFor(StarterCareer career)
        {
            switch (career)
            {
                case StarterCareer.Farmer:    return DriverLicense.Standard | DriverLicense.Agriculture;
                case StarterCareer.Trucker:   return DriverLicense.Standard | DriverLicense.Heavy;
                case StarterCareer.Aviator:   return DriverLicense.Standard | DriverLicense.Aviation;
                case StarterCareer.Fisherman: return DriverLicense.Standard | DriverLicense.Maritime;
                default:                      return DriverLicense.Standard;
            }
        }
    }
}
