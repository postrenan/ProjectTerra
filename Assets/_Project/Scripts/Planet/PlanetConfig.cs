using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Configuração e constantes físicas e geodésicas do planeta Terra.
    /// </summary>
    [CreateAssetMenu(fileName = "PlanetConfig_Earth", menuName = "ProjectTerra/Planet Config")]
    public class PlanetConfig : ScriptableObject
    {
        [Header("Physical Scale (1:1 Earth Data)")]
        [Tooltip("Raio médio volumétrico da Terra em metros (WGS 84 médio ~6.371.008 m)")]
        public double meanRadius = 6371000.0;

        [Tooltip("Raio equatorial da Terra em metros (WGS 84 a = 6.378.137 m)")]
        public double equatorialRadius = 6378137.0;

        [Tooltip("Raio polar da Terra em metros (WGS 84 b = 6.356.752,3 m)")]
        public double polarRadius = 6356752.3142;

        [Header("Topography & Bathymetry Extents (Meters)")]
        [Tooltip("Altitude máxima (Monte Everest: ~8.848,86 m)")]
        public float maxElevation = 8848.86f;

        [Tooltip("Profundidade máxima dos oceanos (Fossa das Marianas: ~ -10.994 m)")]
        public float maxOceanDepth = -10994.0f;

        [Header("Rotation & Dynamics")]
        [Tooltip("Duração de um dia sideral da Terra em segundos (~86.164,09 s)")]
        public double siderealDaySeconds = 86164.0905;

        [Tooltip("Inclinação axial da Terra em graus (obliquidade da eclíptica ~23,44°)")]
        public float axialTiltDegrees = 23.44f;

        [Tooltip("Aceleração gravitacional padrão na superfície (m/s²)")]
        public float surfaceGravity = 9.80665f;

        [Header("Visual & Scaling Helper")]
        [Tooltip("Fator de escala para visualização no Editor. 1.0 = Escala Real 1:1 (6.371.000m). Use valores menores como 0.001 (1km) para protótipo em escala de maquete.")]
        [Range(0.00001f, 1.0f)]
        public double displayScaleFactor = 1.0;

        public double GetEffectiveRadius()
        {
            return meanRadius * displayScaleFactor;
        }
    }
}
