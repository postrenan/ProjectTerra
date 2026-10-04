using System;
using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Tipo de pavimento / superfície da estrada.
    /// Suporta Asfalto, Concreto, Terra batida e Britas/Cascalho.
    /// </summary>
    public enum RoadSurfaceType
    {
        Asphalt = 0,    // Asfalto PBR (rodovias principais e autoestradas)
        Concrete = 1,   // Concreto PBR (rodovias modernas e pistas expressas)
        Dirt = 2,       // Terra batida PBR (estradas rurais e vicinais)
        Gravel = 3      // Britas / Cascalho compactado PBR
    }

    /// <summary>
    /// Quantidade de faixas de rolamento da via (2, 4, 6 ou 8 faixas).
    /// </summary>
    public enum RoadLaneCount
    {
        TwoLanes = 2,   // 2 faixas (1 faixa em cada sentido)
        FourLanes = 4,  // 4 faixas (2 faixas por sentido com canteiro/faixa central)
        SixLanes = 6,   // 6 faixas (3 faixas por sentido com barreira central)
        EightLanes = 8  // 8 faixas (4 faixas por sentido com canteiro largo e barreira)
    }

    /// <summary>
    /// Configuração dimensional e visual de um perfil de estrada.
    /// </summary>
    [Serializable]
    public class RoadProfileConfig
    {
        public RoadSurfaceType surfaceType = RoadSurfaceType.Asphalt;
        public RoadLaneCount laneCount = RoadLaneCount.TwoLanes;
        public float laneWidth = 3.5f;          // Largura padrão de cada faixa de tráfego (metros)
        public float shoulderWidth = 1.8f;       // Acostamento lateral (metros)
        public float embankmentWidth = 3.2f;     // Talude / aterro lateral que ancora no terreno (metros)
        public float medianWidth = 0f;           // Canteiro central (0m para 2 faixas, 1.2m para 4, 2.0m para 6, 2.6m para 8)

        public float TotalRoadwayWidth => ((int)laneCount * laneWidth) + medianWidth;
        public float TotalShoulderWidth => TotalRoadwayWidth + (shoulderWidth * 2f);
        public float TotalFootprintWidth => TotalShoulderWidth + (embankmentWidth * 2f);

        public static RoadProfileConfig GetDefault(RoadSurfaceType type, RoadLaneCount lanes)
        {
            var cfg = new RoadProfileConfig
            {
                surfaceType = type,
                laneCount = lanes,
                laneWidth = (type == RoadSurfaceType.Dirt || type == RoadSurfaceType.Gravel) ? 3.3f : 3.5f
            };

            switch (lanes)
            {
                case RoadLaneCount.TwoLanes:
                    cfg.shoulderWidth = 1.6f;
                    cfg.embankmentWidth = 2.8f;
                    cfg.medianWidth = 0.4f;
                    break;
                case RoadLaneCount.FourLanes:
                    cfg.shoulderWidth = 2.2f;
                    cfg.embankmentWidth = 3.4f;
                    cfg.medianWidth = 1.2f;
                    break;
                case RoadLaneCount.SixLanes:
                    cfg.shoulderWidth = 2.6f;
                    cfg.embankmentWidth = 4.0f;
                    cfg.medianWidth = 2.0f;
                    break;
                case RoadLaneCount.EightLanes:
                    cfg.shoulderWidth = 3.0f;
                    cfg.embankmentWidth = 4.6f;
                    cfg.medianWidth = 2.6f;
                    break;
            }

            return cfg;
        }
    }
}
