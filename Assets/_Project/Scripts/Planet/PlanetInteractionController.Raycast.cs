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
                    Vector3 localNormal = localHit.normalized;

                    // Converter coordenadas locais para Latitude e Longitude
                    float lat = Mathf.Asin(Mathf.Clamp(localNormal.y, -1f, 1f)) * Mathf.Rad2Deg;
                    float lon = Mathf.Atan2(localNormal.x, -localNormal.z) * Mathf.Rad2Deg;

                    // Só confirma o ponto selecionado (marcador/câmera) se a região for válida;
                    // clicar no mar não deve mover o marcador nem herdar a última região.
                    SelectRegionAt(lat, lon, localNormal, worldHit);
                }
            }
        }
    }
}
