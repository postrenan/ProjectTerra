using UnityEngine;

namespace ProjectTerra.Planet
{
    /// <summary>
    /// Converte coordenadas geográficas reais (lat/lon) para o espaço local em metros
    /// do setor sandbox 1:1, usando a bounding box da região.
    ///
    /// Convenção do mundo (igual a SandboxHUD.WorldToMapScreen e ao waypoint):
    ///   terreno centralizado na origem, abrangendo [-worldW/2, +worldW/2] em X (Oeste→Leste)
    ///   e [-worldL/2, +worldL/2] em Z (Sul→Norte). +X = Leste (lon maior), +Z = Norte (lat maior).
    /// </summary>
    public static class RegionGeoProjection
    {
        /// <summary>Projeta (lat, lon) no plano local XZ (Y não é preenchido).</summary>
        public static Vector3 ToLocal(float lat, float lon, RegionData region, float worldW, float worldL)
        {
            float dLon = region.maxLon - region.minLon;
            float dLat = region.maxLat - region.minLat;
            float u = dLon > 1e-6f ? (lon - region.minLon) / dLon : 0.5f;
            float v = dLat > 1e-6f ? (lat - region.minLat) / dLat : 0.5f;
            float x = (u - 0.5f) * worldW;
            float z = (v - 0.5f) * worldL;
            return new Vector3(x, 0f, z);
        }

        /// <summary>True se a coordenada cai dentro da bounding box da região.</summary>
        public static bool IsInside(float lat, float lon, RegionData region)
        {
            return lat >= region.minLat && lat <= region.maxLat &&
                   lon >= region.minLon && lon <= region.maxLon;
        }
    }
}
