using UnityEngine;
using ProjectTerra.Planet;

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

                // Silo de aço galvanizado industrial com reflexo metálico
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                var siloMat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                siloMat.name = "Silo_GalvanizedSteel";
                siloMat.color = new Color(0.82f, 0.85f, 0.88f);
                if (siloMat.HasProperty("_Metallic")) siloMat.SetFloat("_Metallic", 0.75f);
                if (siloMat.HasProperty("_Glossiness")) siloMat.SetFloat("_Glossiness", 0.45f);
                silo.GetComponent<Renderer>().sharedMaterial = siloMat;

                // Cúpula metálica do silo
                var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dome.name = $"Silo_Cupula_{i + 1}";
                dome.transform.SetParent(silo.transform);
                dome.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                dome.transform.localScale = new Vector3(1.02f, 0.45f, 1.02f);
                dome.GetComponent<Renderer>().sharedMaterial = siloMat;
                Destroy(dome.GetComponent<Collider>());
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
            // Detecta a orientação e extensão longitudinal da via
            Vector3 startPos;
            Vector3 endPos;
            float roadWidth;

            if (size.z >= size.x)
            {
                startPos = new Vector3(pos.x, 0f, pos.z - size.z * 0.5f);
                endPos = new Vector3(pos.x, 0f, pos.z + size.z * 0.5f);
                roadWidth = size.x;
            }
            else
            {
                startPos = new Vector3(pos.x - size.x * 0.5f, 0f, pos.z);
                endPos = new Vector3(pos.x + size.x * 0.5f, 0f, pos.z);
                roadWidth = size.z;
            }

            // Identificar tipo de pavimento pelo contexto do nome da via
            RoadSurfaceType surface = RoadSurfaceType.Asphalt;
            string lowerName = name.ToLower();
            if (lowerName.Contains("rural") || lowerName.Contains("terra"))
            {
                surface = RoadSurfaceType.Dirt;
            }
            else if (lowerName.Contains("brita") || lowerName.Contains("cascalho"))
            {
                surface = RoadSurfaceType.Gravel;
            }
            else if (lowerName.Contains("concreto"))
            {
                surface = RoadSurfaceType.Concrete;
            }

            // Identificar quantidade de faixas pela largura métrica especificada
            RoadLaneCount lanes;
            if (roadWidth <= 12f) lanes = RoadLaneCount.TwoLanes;
            else if (roadWidth <= 22f) lanes = RoadLaneCount.FourLanes;
            else if (roadWidth <= 32f) lanes = RoadLaneCount.SixLanes;
            else lanes = RoadLaneCount.EightLanes;

            var roadObj = RoadMeshBuilder.BuildStraightRoad(parent, startPos, endPos, surface, lanes, GetTerrainHeight, name);

            // Registrar máscara de grama para evitar que vegetação nasça na pista
            var cfg = RoadProfileConfig.GetDefault(surface, lanes);
            float clearance = cfg.TotalFootprintWidth * 0.5f + 2.0f;
            Vector2 a2 = new Vector2(startPos.x, startPos.z);
            Vector2 b2 = new Vector2(endPos.x, endPos.z);
            RoadMaskSegments.Add(new RoadMaskSegment { a = a2, b = b2, clearance = clearance });
        }

        private void CreateBuilding(Transform parent, Vector3 pos, Vector3 size, Color color, string name)
        {
            // [removido] Assets de prédios/edifícios desativados do cenário
            return;
        }

        private static Material cachedCommercialMat;
        private static Material cachedSuburbanMat;

        private static Material GetBuildingMaterial(bool isSuburban)
        {
            if (isSuburban)
            {
                if (cachedSuburbanMat == null)
                {
                    Texture2D tex = Resources.Load<Texture2D>("Models/Structures/Textures/colormap_suburban")
                                 ?? Resources.Load<Texture2D>("Models/Structures/Textures/colormap");
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    cachedSuburbanMat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                    cachedSuburbanMat.name = "Building_Suburban_Mat";
                    cachedSuburbanMat.mainTexture = tex;
                    cachedSuburbanMat.color = Color.white;
                    if (cachedSuburbanMat.HasProperty("_Glossiness")) cachedSuburbanMat.SetFloat("_Glossiness", 0.15f);
                    if (cachedSuburbanMat.HasProperty("_Metallic")) cachedSuburbanMat.SetFloat("_Metallic", 0.0f);
                }
                return cachedSuburbanMat;
            }
            else
            {
                if (cachedCommercialMat == null)
                {
                    Texture2D tex = Resources.Load<Texture2D>("Models/Structures/Textures/colormap_commercial")
                                 ?? Resources.Load<Texture2D>("Models/Structures/Textures/colormap");
                    Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                    cachedCommercialMat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                    cachedCommercialMat.name = "Building_Commercial_Mat";
                    cachedCommercialMat.mainTexture = tex;
                    cachedCommercialMat.color = Color.white;
                    if (cachedCommercialMat.HasProperty("_Glossiness")) cachedCommercialMat.SetFloat("_Glossiness", 0.20f);
                    if (cachedCommercialMat.HasProperty("_Metallic")) cachedCommercialMat.SetFloat("_Metallic", 0.05f);
                }
                return cachedCommercialMat;
            }
        }

        private static Texture2D cachedAsphaltTex;
        private static Texture2D GetAsphaltTexture()
        {
            if (cachedAsphaltTex != null) return cachedAsphaltTex;
#if UNITY_EDITOR
            cachedAsphaltTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/PBR/Infrastructure/Asphalt/Albedo.png");
            if (cachedAsphaltTex != null) return cachedAsphaltTex;
#endif
            string path = System.IO.Path.Combine(Application.dataPath, "_Project", "Textures", "PBR", "Infrastructure", "Asphalt", "Albedo.png");
            if (System.IO.File.Exists(path))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    var tex = new Texture2D(2, 2);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Repeat;
                        tex.filterMode = FilterMode.Bilinear;
                        cachedAsphaltTex = tex;
                        return cachedAsphaltTex;
                    }
                }
                catch {}
            }
            return null;
        }

        private void CreateDeliveryTriggerZone(Vector3 pos)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Trigger_PontoDeEntrega";
            float h = GetTerrainHeight(pos);
            marker.transform.position = new Vector3(pos.x, h + 0.1f, pos.z);
            marker.transform.localScale = new Vector3(25f, 0.05f, 25f);
            marker.GetComponent<Renderer>().sharedMaterial = CreatePrimitiveMaterial(new Color(0.2f, 1.0f, 0.4f, 0.6f));
            Destroy(marker.GetComponent<Collider>());
        }

        /// <summary>
        /// Cria um Material com shader Standard e cor sólida.
        /// Use sempre este método ao colorir primitivos — NUNCA use rend.material.color diretamente,
        /// pois isso modifica o Default-Material compartilhado entre todos os objetos (tudo fica branco/magenta).
        /// </summary>
        private static Material CreatePrimitiveMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
            return mat;
        }

        #endregion
    }
}
