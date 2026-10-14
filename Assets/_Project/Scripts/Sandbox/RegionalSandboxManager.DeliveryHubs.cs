using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>Um destino de entrega espalhado pelo território (nome + posição no mundo).</summary>
    public struct DeliveryHub
    {
        public string name;
        public Vector3 position;
    }

    public partial class RegionalSandboxManager
    {
        public List<DeliveryHub> deliveryHubs { get; private set; }

        /// <summary>
        /// Gera pontos de entrega variados espalhados pelo setor, nomeados com as cidades
        /// reais da região quando disponíveis. Substitui o destino único (que antes caía na
        /// origem) por uma rede de destinos, dando vida e distância real aos fretes.
        /// </summary>
        private void BuildDeliveryHubs()
        {
            deliveryHubs = new List<DeliveryHub>();

            // Nomes reais das cidades da região (até 10 maiores). Fallback para nomes genéricos.
            List<string> cityNames = new List<string>();
            RegionCitiesDatabase.EnsureLoaded();
            var cities = RegionCitiesDatabase.GetCities(activeSave.regionId);
            if (cities != null)
            {
                foreach (var c in cities)
                    if (c != null && !string.IsNullOrEmpty(c.name)) cityNames.Add(c.name);
            }

            string[] genericSuffix = { "Norte", "Sul", "Leste", "Oeste", "Central", "Litoral" };

            const int hubCount = 6;
            float maxX = worldWidthMeters * 0.42f;
            float maxZ = worldLengthMeters * 0.42f;

            for (int i = 0; i < hubCount; i++)
            {
                // Distribui os hubs em ângulos espaçados com raios variados a partir da origem (cidade inicial).
                float angle = (i / (float)hubCount) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
                float radius = Mathf.Lerp(3000f, Mathf.Min(maxX, maxZ), (i + Random.Range(0.2f, 0.9f)) / hubCount);

                Vector3 pos = townCenterPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
                pos.z = Mathf.Clamp(pos.z, -maxZ, maxZ);
                pos.y = GetTerrainHeight(pos);

                string hubName;
                if (i < cityNames.Count)
                    hubName = cityNames[i];
                else
                    hubName = $"Entreposto {genericSuffix[i % genericSuffix.Length]}";

                deliveryHubs.Add(new DeliveryHub { name = hubName, position = pos });
                CreateDeliveryBeacon(pos, hubName, i);
            }

            // Compatibilidade: o destino "mercado" padrão passa a ser o primeiro hub.
            if (deliveryHubs.Count > 0)
            {
                deliveryMarketPosition = deliveryHubs[0].position;
            }

            Debug.Log($"[RegionalSandbox] {deliveryHubs.Count} hubs de entrega gerados para a região {activeSave.regionName}.");
        }

        private void CreateDeliveryBeacon(Vector3 pos, string label, int index)
        {
            var root = new GameObject($"HubEntrega_{index}_{label}");
            root.transform.position = pos;

            // Disco no chão (zona de entrega visível).
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "ZonaEntrega";
            disc.transform.SetParent(root.transform);
            disc.transform.position = new Vector3(pos.x, pos.y + 0.1f, pos.z);
            disc.transform.localScale = new Vector3(30f, 0.05f, 30f);
            disc.GetComponent<Renderer>().sharedMaterial = CreatePrimitiveMaterial(new Color(0.2f, 0.9f, 0.5f, 0.6f));
            Destroy(disc.GetComponent<Collider>());

            // Pilar/baliza alto para ser visto de longe.
            var pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pylon.name = "Baliza";
            pylon.transform.SetParent(root.transform);
            pylon.transform.position = new Vector3(pos.x, pos.y + 40f, pos.z);
            pylon.transform.localScale = new Vector3(2.5f, 40f, 2.5f);
            pylon.GetComponent<Renderer>().sharedMaterial = CreatePrimitiveMaterial(new Color(1f, 0.82f, 0.2f, 0.85f));
            Destroy(pylon.GetComponent<Collider>());
        }
    }
}
