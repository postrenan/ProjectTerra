using System;
using System.Collections.Generic;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Representa os dados de infraestrutura e viabilidade geográfica de um estado/província ou país.
    /// Considera litoral, rios, grandes lagos, portos marítimos/fluviais, aeroportos e malha rodoviária.
    /// Determina com fidelidade geográfica quais carreiras e opções de jogo são viáveis
    /// (ex: pesca proibida no Saara ou nos Andes; agricultura viável exceto em extremos polares e dunas hiperáridas).
    /// </summary>
    [Serializable]
    public class RegionInfrastructure
    {
        public int regionId;
        public string regionName;
        public string countryName;

        // Hidrografia & Litoral
        public bool hasCoastline;
        public bool hasRivers;
        public bool hasLakes;
        public string waterBodyName;

        // Infraestrutura de Transporte
        public bool hasPort;
        public string portName;
        public bool hasAirport;
        public string airportName;
        public bool hasRoadNetwork;

        // Viabilidade de Carreiras
        public bool isAgricultureViable;
        public string agricultureReason;

        public bool isFishingViable;
        public string fishingReason;

        public bool isAviationViable;
        public string aviationReason;

        public bool isTruckingViable;
        public string truckingReason;

        // Classificação Geográfica / Bioma
        public string environmentCategory;

        public bool IsCareerViable(StarterCareer career, out string reason)
        {
            switch (career)
            {
                case StarterCareer.Farmer:
                    reason = agricultureReason;
                    return isAgricultureViable;

                case StarterCareer.Trucker:
                    reason = truckingReason;
                    return isTruckingViable;

                case StarterCareer.Aviator:
                    reason = aviationReason;
                    return isAviationViable;

                case StarterCareer.Fisherman:
                    reason = fishingReason;
                    return isFishingViable;

                default:
                    reason = "Opção desconhecida.";
                    return false;
            }
        }
    }

    [Serializable]
    public class RegionInfrastructureCollection
    {
        public List<RegionInfrastructure> regions;
    }
}
