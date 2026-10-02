using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class RegionalSandboxManager
    {
        #region Construção da Cidade Inicial & Malha Viária

        private void BuildStarterTown()
        {
            var townRoot = new GameObject("Town_CidadeInicial");
            townCenterPosition.y = GetTerrainHeight(townCenterPosition);
            townRoot.transform.position = townCenterPosition;

            // Malha viária principal (Avenida Central de 1.2km)
            CreateRoad(townRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(30f, 0.2f, 1200f), "Avenida Central Asfaltada");
            CreateRoad(townRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(1200f, 0.2f, 30f), "Rua Comercial Transversal");

            // Edifício 1: Cooperativa Agrícola & Silos Regionais
            deliveryMarketPosition = townCenterPosition + new Vector3(80f, 0f, 120f);
            deliveryMarketPosition.y = GetTerrainHeight(deliveryMarketPosition);
            CreateBuilding(townRoot.transform, deliveryMarketPosition, new Vector3(38f, 22f, 48f), new Color(0.75f, 0.78f, 0.82f), "🏢 Cooperativa & Silos Centrais");

            for (int i = 0; i < 4; i++)
            {
                Vector3 siloPos = deliveryMarketPosition + new Vector3(32f, 0f, -25f + i * 18f);
                siloPos.y = GetTerrainHeight(siloPos);
                var silo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                silo.name = $"Silo_Graos_{i + 1}";
                silo.transform.SetParent(townRoot.transform);
                silo.transform.position = siloPos + Vector3.up * 14f;
                silo.transform.localScale = new Vector3(14f, 14f, 14f);
                silo.GetComponent<Renderer>().material.color = new Color(0.85f, 0.87f, 0.9f);
            }

            CreateDeliveryTriggerZone(deliveryMarketPosition + new Vector3(-24f, 0f, 0f));

            // Edifício 2: Centro Administrativo / Prefeitura
            Vector3 prefPos = townCenterPosition + new Vector3(-90f, 0f, 120f);
            prefPos.y = GetTerrainHeight(prefPos);
            CreateBuilding(townRoot.transform, prefPos, new Vector3(42f, 16f, 36f), new Color(0.92f, 0.90f, 0.85f), "🏛️ Centro Administrativo & Alvarás");

            // Edifício 3: Posto Rodoviário de Combustíveis & Pátio
            Vector3 gasPos = townCenterPosition + new Vector3(-85f, 0f, -120f);
            gasPos.y = GetTerrainHeight(gasPos);
            CreateBuilding(townRoot.transform, gasPos, new Vector3(34f, 10f, 38f), new Color(0.9f, 0.35f, 0.25f), "⛽ Posto Rodoviário & Oficina");

            // Edifício 4: Armazém Geral & Mercado Central
            Vector3 mktPos = townCenterPosition + new Vector3(90f, 0f, -120f);
            mktPos.y = GetTerrainHeight(mktPos);
            CreateBuilding(townRoot.transform, mktPos, new Vector3(44f, 12f, 32f), new Color(0.6f, 0.65f, 0.7f), "🏪 Mercado Central & Armazém");
        }

        private void CreateRoad(Transform parent, Vector3 pos, Vector3 size, string name)
        {
            var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = name;
            road.transform.SetParent(parent);
            float h = GetTerrainHeight(pos);
            road.transform.position = new Vector3(pos.x, h + 0.1f, pos.z);
            road.transform.localScale = size;
            var rend = road.GetComponent<Renderer>();
            rend.material.color = new Color(0.18f, 0.18f, 0.20f);
        }

        private void CreateBuilding(Transform parent, Vector3 pos, Vector3 size, Color color, string name)
        {
            float h = GetTerrainHeight(pos);
            Vector3 worldPos = new Vector3(pos.x, h, pos.z);

            // Tenta instanciar modelo 3D importado
            string[] modelCandidates = new string[] {
                "Models/Structures/building-a",
                "Models/Structures/building-c",
                "Models/Structures/building-skyscraper-a",
                "Models/Structures/building-type-a",
                "Models/Structures/building-type-b"
            };

            int pick = Mathf.Abs(name.GetHashCode()) % modelCandidates.Length;
            GameObject prefab = SceneObjectSpawner.LoadModel(modelCandidates[pick]);

            if (prefab != null)
            {
                var building = Instantiate(prefab, worldPos, Quaternion.identity, parent);
                building.name = name;
                // Ajusta escala proporcional às dimensões do edifício
                float scaleY = size.y / 8f;
                building.transform.localScale = new Vector3(scaleY, scaleY, scaleY);
                return;
            }

            // Fallback para geometria primitiva
            var primitiveBuilding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitiveBuilding.name = name;
            primitiveBuilding.transform.SetParent(parent);
            primitiveBuilding.transform.position = new Vector3(pos.x, h + (size.y * 0.5f), pos.z);
            primitiveBuilding.transform.localScale = size;
            var rend = primitiveBuilding.GetComponent<Renderer>();
            rend.material.color = color;
        }

        private void CreateDeliveryTriggerZone(Vector3 pos)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Trigger_PontoDeEntrega";
            float h = GetTerrainHeight(pos);
            marker.transform.position = new Vector3(pos.x, h + 0.1f, pos.z);
            marker.transform.localScale = new Vector3(25f, 0.05f, 25f);
            var rend = marker.GetComponent<Renderer>();
            rend.material.color = new Color(0.2f, 1.0f, 0.4f, 0.6f);
            Destroy(marker.GetComponent<Collider>());
        }

        #endregion
    }
}
