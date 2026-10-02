using System;
using UnityEngine;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void TryRaycastPlanet(Vector2 screenPos)
        {
            if (planet == null)
            {
                planet = CubeSpherePlanet.Instance ?? FindAnyObjectByType<CubeSpherePlanet>();
            }
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            if (planet == null || mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            Vector3 center = planet.transform.position;
            double radius = planet.PlanetRadius;

            // Interseção analítica raio-esfera de alta precisão (double precision)
            double ocX = ray.origin.x - center.x;
            double ocY = ray.origin.y - center.y;
            double ocZ = ray.origin.z - center.z;

            double dirX = ray.direction.x;
            double dirY = ray.direction.y;
            double dirZ = ray.direction.z;

            double b = ocX * dirX + ocY * dirY + ocZ * dirZ;
            double c = (ocX * ocX + ocY * ocY + ocZ * ocZ) - (radius * radius);
            double discriminant = b * b - c;

            if (discriminant >= 0.0)
            {
                double t = -b - Math.Sqrt(discriminant);
                if (t > 0.0)
                {
                    Vector3 worldHit = ray.origin + ray.direction * (float)t;
                    Vector3 localHit = planet.transform.InverseTransformPoint(worldHit);
                    selectedLocalNormal = localHit.normalized;
                    selectedWorldPoint = worldHit;

                    // Converter coordenadas locais para Latitude e Longitude
                    float lat = Mathf.Asin(Mathf.Clamp(selectedLocalNormal.y, -1f, 1f)) * Mathf.Rad2Deg;
                    float lon = Mathf.Atan2(selectedLocalNormal.x, -selectedLocalNormal.z) * Mathf.Rad2Deg;

                    SelectRegionAt(lat, lon);
                }
            }
        }

        private RegionData GenerateOceanRegion(float lat, float lon)
        {
            string oceanName = "Oceano Global";
            if (lat > 65.0f)
            {
                oceanName = "Oceano Ártico";
            }
            else if (lat < -60.0f)
            {
                oceanName = "Oceano Antártico";
            }
            else if (lat >= 30.0f && lat <= 46.0f && lon >= -6.0f && lon <= 36.0f)
            {
                oceanName = "Mar Mediterrâneo";
            }
            else if (lat >= 53.0f && lat <= 66.0f && lon >= 10.0f && lon <= 30.0f)
            {
                oceanName = "Mar Báltico";
            }
            else if (lon >= -75.0f && lon <= 20.0f)
            {
                oceanName = lat >= 0 ? "Oceano Atlântico Norte" : "Oceano Atlântico Sul";
            }
            else if (lon > 20.0f && lon <= 100.0f && lat <= 30.0f)
            {
                oceanName = "Oceano Índico";
            }
            else
            {
                oceanName = lat >= 0 ? "Oceano Pacífico Norte" : "Oceano Pacífico Sul";
            }

            return new RegionData
            {
                id = 0,
                name = oceanName,
                country = "Águas Internacionais",
                type = "Zona Marítima",
                centerLat = lat,
                centerLon = lon,
                forestPercent = 0,
                mineralsPercent = 40,
                arablePercent = 0,
                waterPercent = 100
            };
        }
    }
}
