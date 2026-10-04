using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>Segmento de via em coordenadas locais XZ, com folga lateral para mascarar a grama.</summary>
    public struct RoadMaskSegment
    {
        public Vector2 a;
        public Vector2 b;
        public float clearance;
    }

    public partial class RegionalSandboxManager
    {
        #region Malha Viária Real (Rodovias/Ferrovias) & Cidades Econômicas

        // Núcleos econômicos da região (as 10 maiores cidades). Base para demanda/oferta e rotas.
        public List<RegionCity> ActiveCities { get; private set; } = new List<RegionCity>();

        // Segmentos de via (em XZ local) usados para suprimir a grama que nasce por cima das estradas.
        public readonly List<RoadMaskSegment> RoadMaskSegments = new List<RoadMaskSegment>();

        // Teto defensivo de trechos renderizados no mundo para estados gigantes (evita hitch).
        private const int MaxRenderedPolylines = 3000;

        private void BuildRegionalInfrastructure()
        {
            if (activeRegionData == null)
            {
                Debug.LogWarning("[RegionInfra] Sem metadados geográficos da região — malha viária e cidades ignoradas.");
                return;
            }
            BuildRegionalRivers();
            // Marca o início dos segmentos de estrada: os canais de rio acima já
            // ocuparam a lista e precisam sobreviver a futuros rebuilds.
            RecordRoadMaskMark();
            BuildRegionalRoadNetwork();
            BuildRegionalCities();

            // Marcos geográficos (vulcões, montanhas, chapadas, quedas d'água, rios)
            if (activeTerrain != null)
            {
                LandmarkManager.Create(activeTerrain, activeRegionData);
            }
            else
            {
                Debug.LogWarning("[RegionInfra] Terreno não disponível — marcos geográficos não posicionados.");
            }
        }

        private void BuildRegionalRivers()
        {
            var rivers = RegionRiversDatabase.GetRivers(activeRegionData.id);
            if (rivers.Count == 0) return;

            var root = new GameObject("RegionRivers");
            int built = 0;
            foreach (var river in rivers)
            {
                if (built >= MaxRenderedPolylines) break;
                if (river.points == null || river.points.Length < 2) continue;
                BuildRiverChannel(root.transform, river);
                built++;
            }
            Debug.Log($"[RegionInfra] Hidrografia construída: {built} rios em {activeRegionData.name}.");
            // Sem culling de 1.4 km — rios visíveis até o far-clip.
        }

        private void BuildRiverChannel(Transform parent, RiverPolyline river)
        {
            // Projeta + registra máscara de grama + suaviza, como nas estradas.
            int n = river.points.Length;
            var flat = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                Vector3 local = RegionGeoProjection.ToLocal(river.points[i].x, river.points[i].y, activeRegionData, worldWidthMeters, worldLengthMeters);
                flat.Add(new Vector2(local.x, local.z));
            }

            float wh = Mathf.Max(5f, river.width * 0.5f);         // meia-largura da lâmina d'água
            float bank = Mathf.Max(6f, river.width * 0.5f);       // barranco lateral
            float depth = Mathf.Clamp(river.width * 0.5f, 3f, 12f); // profundidade da calha

            float clearance = wh + bank + 1f;
            for (int i = 0; i < flat.Count - 1; i++)
                RoadMaskSegments.Add(new RoadMaskSegment { a = flat[i], b = flat[i + 1], clearance = clearance });

            var sm = SmoothXZ(flat);
            int m = sm.Count;
            if (m < 2) return;

            var center = new Vector3[m];
            var rights = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                var p = new Vector3(sm[i].x, 0f, sm[i].y);
                p.y = GetTerrainHeight(p);
                center[i] = p;
            }
            for (int i = 0; i < m; i++)
            {
                Vector3 fwd = (i == 0) ? center[1] - center[0]
                            : (i == m - 1) ? center[m - 1] - center[m - 2]
                            : center[i + 1] - center[i - 1];
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                fwd.Normalize();
                rights[i] = new Vector3(fwd.z, 0f, -fwd.x);
            }

            var root = new GameObject("Rio");
            root.transform.SetParent(parent);

            // Leito escavado (barranco → fundo → barranco): 4 vértices de seção transversal.
            float[] lat = { -(wh + bank), -wh, wh, wh + bank };
            float[] yd = { 0.2f, -depth, -depth, 0.2f };
            BuildCrossSection(root.transform, center, rights, lat, yd, GetDirtMaterial(), "Leito");

            // Lâmina d'água preenchendo a calha (um pouco abaixo do nível do solo).
            BuildStrip(root.transform, center, rights, wh, 0f, -depth * 0.35f, GetWaterMaterial(), "Agua");
        }

        /// <summary>Malha de seção transversal: 'lat.Length' linhas longitudinais com deslocamento lateral e altura próprios.</summary>
        private void BuildCrossSection(Transform parent, Vector3[] center, Vector3[] rights, float[] lat, float[] yd, Material mat, string name)
        {
            int m = center.Length, rows = lat.Length;
            var verts = new Vector3[m * rows];
            var uvs = new Vector2[m * rows];
            for (int i = 0; i < m; i++)
            {
                float vrun = i / (float)(m - 1);
                for (int k = 0; k < rows; k++)
                {
                    verts[i * rows + k] = center[i] + rights[i] * lat[k] + Vector3.up * yd[k];
                    uvs[i * rows + k] = new Vector2(k / (float)(rows - 1), vrun * 20f);
                }
            }
            var tris = new List<int>((m - 1) * (rows - 1) * 6);
            for (int i = 0; i < m - 1; i++)
            {
                for (int k = 0; k < rows - 1; k++)
                {
                    int a = i * rows + k, b = i * rows + k + 1;
                    int c = (i + 1) * rows + k, d = (i + 1) * rows + k + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            var mesh = new Mesh { name = name };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static Material cachedDirtMat;
        private Material GetDirtMaterial()
        {
            if (cachedDirtMat != null) return cachedDirtMat;
            cachedDirtMat = CreateSolidMaterial(new Color(0.34f, 0.27f, 0.19f));
            if (cachedDirtMat.HasProperty("_Glossiness")) cachedDirtMat.SetFloat("_Glossiness", 0.06f);
            return cachedDirtMat;
        }

        private static Material cachedWaterMat;
        private Material GetWaterMaterial()
        {
            if (cachedWaterMat != null) return cachedWaterMat;
            Shader s = Shader.Find("Standard");
            var m = s != null ? new Material(s) : CreateSolidMaterial(new Color(0.12f, 0.4f, 0.6f));
            m.name = "River_Water";
            if (s != null)
            {
                m.SetFloat("_Mode", 3f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = 3000;
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.9f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.1f);
            }
            m.color = new Color(0.12f, 0.42f, 0.62f, 0.72f);
            cachedWaterMat = m;
            return m;
        }

        private void BuildRegionalRoadNetwork()
        {
            var polylines = RegionRoadsDatabase.GetRoads(activeRegionData.id);
            if (polylines.Count == 0) return;

            var root = new GameObject("RegionRoadNetwork");

            int built = 0;
            foreach (var poly in polylines)
            {
                if (built >= MaxRenderedPolylines) break;
                if (poly.points == null || poly.points.Length < 2) continue;
                BuildRoadRibbon(root.transform, poly);
                built++;
            }
            Debug.Log($"[RegionInfra] Malha viária construída: {built} trechos (rodovias/ferrovias) em {activeRegionData.name}.");
            // NÃO aplicar culling de estrutura (1.4 km): no 1:1 real (ex: SP = 886 km) as vias
            // ficam dezenas de km do spawn. Mantém na layer padrão (0) para renderizar até o far-clip (45 km).
        }
        public static RoadSurfaceType GlobalPreferredSurface = RoadSurfaceType.Asphalt;
        public static RoadLaneCount GlobalPreferredLanes = RoadLaneCount.FourLanes;
        public static bool UseCustomGlobalRoadSettings = false;

        public void RebuildRoadNetwork()
        {
            var existing = GameObject.Find("RegionRoadNetwork");
            if (existing != null) Destroy(existing);

            // RoadMaskSegments é compartilhado: além das estradas, ele recebe os canais
            // de rio (BuildRiverChannel), as ruas da cidade (CreateRoad) e os sulcos do
            // arado (PlowFurrowManager). Um Clear() global apagava a supressão de grama
            // de todos esses sistemas e a relva brotava sobre rios e ruas. Agora só a
            // cauda pertencente às estradas é removida (o prefixo foi gravado por
            // RecordRoadMaskMark).
            int roadOwnedCount = RoadMaskSegments.Count - roadMaskMark;
            if (roadOwnedCount > 0) RoadMaskSegments.RemoveRange(roadMaskMark, roadOwnedCount);

            // Após o RemoveRange a lista termina exatamente em roadMaskMark, então os novos
            // segmentos de estrada passam a ser a "cauda" proprietária.
            BuildRegionalRoadNetwork();
            roadMaskMark = RoadMaskSegments.Count;
        }

        // Índice em RoadMaskSegments onde começam os segmentos de estrada. Tudo antes
        // pertence a rios/ruas/sulhos e deve sobreviver a um rebuild.
        private int roadMaskMark = 0;

        private void RecordRoadMaskMark()
        {
            roadMaskMark = RoadMaskSegments.Count;
        }

        private void BuildRoadRibbon(Transform parent, RoadPolyline poly)
        {
            // 1. Projeta os pontos reais para XZ local.
            int n = poly.points.Length;
            var flat = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                Vector3 local = RegionGeoProjection.ToLocal(poly.points[i].x, poly.points[i].y, activeRegionData, worldWidthMeters, worldLengthMeters);
                flat.Add(new Vector2(local.x, local.z));
            }

            // 2. Determinar tipo de pavimento e quantidade de faixas
            RoadSurfaceType surface;
            RoadLaneCount lanes;

            if (UseCustomGlobalRoadSettings)
            {
                surface = GlobalPreferredSurface;
                lanes = GlobalPreferredLanes;
            }
            else
            {
                switch (poly.roadClass)
                {
                    case RoadClass.Expressway:
                        surface = RoadSurfaceType.Asphalt;
                        lanes = RoadLaneCount.SixLanes; // 6 faixas para autoestrada expressa
                        break;
                    case RoadClass.MajorHighway:
                        surface = RoadSurfaceType.Asphalt;
                        lanes = RoadLaneCount.FourLanes; // 4 faixas para rodovia tronco
                        break;
                    case RoadClass.SecondaryHighway:
                        surface = RoadSurfaceType.Asphalt;
                        lanes = RoadLaneCount.TwoLanes;  // 2 faixas para rodovia secundária
                        break;
                    case RoadClass.Connector:
                        // Conectores vicinais: alternância orgânica entre Terra batida e Britas
                        surface = ((flat[0].GetHashCode() & 1) == 0) ? RoadSurfaceType.Dirt : RoadSurfaceType.Gravel;
                        lanes = RoadLaneCount.TwoLanes;
                        break;
                    case RoadClass.Railroad:
                    default:
                        surface = RoadSurfaceType.Gravel;
                        lanes = RoadLaneCount.TwoLanes;
                        break;
                }
            }

            var config = RoadProfileConfig.GetDefault(surface, lanes);

            // 3. Registra os segmentos originais para suprimir grama sobre a via e acostamentos
            // A malha vai até +(halfRoad + shoulder + talude), ou seja, TotalFootprintWidth/2
            // de cada lado. Usar TotalShoulderWidth/2 (20.3 m contra 22.9 m reais no
            // perfil de 8 faixas) deixava vegetação nascendo sobre o próprio talude.
            float clearance = config.TotalFootprintWidth * 0.5f + 2.0f;
            for (int i = 0; i < flat.Count - 1; i++)
                RoadMaskSegments.Add(new RoadMaskSegment { a = flat[i], b = flat[i + 1], clearance = clearance });

            // 4. Suavização XZ densa (passo fino de ~14m para contornar relevos suavemente sem vãos)
            var sm = RoadMeshBuilder.SmoothPolylineXZ(flat, 14f);
            int m = sm.Count;
            if (m < 2) return;

            // 5. Ferrovia especial (lastro com ancoragem de solo + trilhos) ou Rodovia com perfil completo
            if (poly.roadClass == RoadClass.Railroad)
            {
                BuildRailroadTrack(parent, sm);
            }
            else
            {
                var spine = new Vector3[m];
                for (int i = 0; i < m; i++)
                {
                    spine[i] = new Vector3(sm[i].x, 0f, sm[i].y);
                }
                RoadMeshBuilder.BuildRoad(parent, spine, config, GetTerrainHeight, $"Road_{poly.roadClass}_{surface}_{(int)lanes}L");
            }
        }

        private void BuildRailroadTrack(Transform parent, List<Vector2> sm)
        {
            int m = sm.Count;
            var spine = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                spine[i] = new Vector3(sm[i].x, 0f, sm[i].y);
            }

            var railCfg = new RoadProfileConfig
            {
                surfaceType = RoadSurfaceType.Gravel,
                laneCount = RoadLaneCount.TwoLanes,
                laneWidth = 2.2f,
                shoulderWidth = 1.2f,
                embankmentWidth = 2.6f,
                medianWidth = 0f
            };

            var roadObj = RoadMeshBuilder.BuildRoad(parent, spine, railCfg, GetTerrainHeight, "Ferrovia_Leito");

            // Trilhos metálicos de aço
            var rights = new Vector3[m];
            var center = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                center[i] = new Vector3(sm[i].x, GetTerrainHeight(new Vector3(sm[i].x, 0f, sm[i].y)) + 0.16f, sm[i].y);
            }
            for (int i = 0; i < m; i++)
            {
                Vector3 fwd = (i == 0) ? center[1] - center[0]
                            : (i == m - 1) ? center[m - 1] - center[m - 2]
                            : center[i + 1] - center[i - 1];
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
                fwd.Normalize();
                rights[i] = new Vector3(fwd.z, 0f, -fwd.x);
            }

            var steelMat = RoadMeshBuilder.GetMarkingMaterial(new Color(0.65f, 0.67f, 0.72f));
            BuildStrip(roadObj.transform, center, rights, 0.18f, -1.8f, 0.12f, steelMat, "Trilho_Esq");
            BuildStrip(roadObj.transform, center, rights, 0.18f, 1.8f, 0.12f, steelMat, "Trilho_Dir");
        }

        /// <summary>Constrói uma faixa (fita) paralela ao traçado, com largura, deslocamento lateral e altura próprios.</summary>
        private void BuildStrip(Transform parent, Vector3[] center, Vector3[] rights, float halfWidth, float lateralOffset, float yExtra, Material mat, string name)
        {
            int m = center.Length;
            var verts = new Vector3[m * 2];
            var uvs = new Vector2[m * 2];
            float uRun = 0f;
            for (int i = 0; i < m; i++)
            {
                Vector3 c = center[i] + rights[i] * lateralOffset + Vector3.up * yExtra;
                verts[i * 2] = c - rights[i] * halfWidth;
                verts[i * 2 + 1] = c + rights[i] * halfWidth;
                if (i > 0) uRun += Vector3.Distance(center[i], center[i - 1]) / Mathf.Max(0.5f, halfWidth * 2f);
                uvs[i * 2] = new Vector2(0f, uRun);
                uvs[i * 2 + 1] = new Vector2(1f, uRun);
            }
            var tris = new int[(m - 1) * 6];
            int t = 0;
            for (int i = 0; i < m - 1; i++)
            {
                int a = i * 2, b = i * 2 + 1, cc = (i + 1) * 2, d = (i + 1) * 2 + 1;
                tris[t++] = a; tris[t++] = cc; tris[t++] = b;
                tris[t++] = b; tris[t++] = cc; tris[t++] = d;
            }
            var mesh = new Mesh { name = name };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static List<Vector2> SmoothXZ(List<Vector2> p)
        {
            return RoadMeshBuilder.SmoothPolylineXZ(p, 14f);
        }

        private static float RoadHalfWidth(RoadClass c)
        {
            switch (c)
            {
                case RoadClass.Expressway: return 12f;
                case RoadClass.MajorHighway: return 9f;
                case RoadClass.SecondaryHighway: return 6f;
                case RoadClass.Railroad: return 3.5f;
                case RoadClass.Connector: return 5f;
                default: return 6f;
            }
        }

        private void BuildRegionalCities()
        {
            ActiveCities = RegionCitiesDatabase.GetCities(activeRegionData.id);
            if (ActiveCities.Count == 0) return;

            var root = new GameObject("RegionCities");
            foreach (var city in ActiveCities)
            {
                Vector3 local = RegionGeoProjection.ToLocal(city.lat, city.lon, activeRegionData, worldWidthMeters, worldLengthMeters);
                local.y = GetTerrainHeight(local);
                BuildCityCluster(root.transform, local, city);
            }
            Debug.Log($"[RegionInfra] {ActiveCities.Count} cidades (base econômica) posicionadas em {activeRegionData.name}.");
            // Cidades ficam na layer padrão (sem culling de 1.4 km) — balizas altas visíveis até o far-clip.
        }

        private void BuildCityCluster(Transform parent, Vector3 center, RegionCity city)
        {
            var cityRoot = new GameObject($"Cidade_{city.name}");
            cityRoot.transform.SetParent(parent);
            cityRoot.transform.position = center;

            // Quantidade e porte de edifícios escalam com o peso econômico da cidade.
            int blocks = Mathf.Clamp(2 + Mathf.RoundToInt(city.economicWeight * 10f), 2, 12);
            float footprint = 60f + city.economicWeight * 240f;
            Color buildingTint = city.isCapital ? new Color(0.80f, 0.78f, 0.72f) : new Color(0.68f, 0.70f, 0.74f);

            var rng = new System.Random(city.name.GetHashCode());
            for (int i = 0; i < blocks; i++)
            {
                float ox = (float)(rng.NextDouble() - 0.5) * footprint;
                float oz = (float)(rng.NextDouble() - 0.5) * footprint;
                Vector3 pos = new Vector3(center.x + ox, 0f, center.z + oz);
                pos.y = GetTerrainHeight(pos);
                float h = 18f + (float)rng.NextDouble() * (20f + city.economicWeight * 90f);
                float w = 14f + (float)rng.NextDouble() * 16f;

                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = $"{city.name}_Bloco_{i + 1}";
                b.transform.SetParent(cityRoot.transform);
                b.transform.position = new Vector3(pos.x, pos.y + h * 0.5f, pos.z);
                b.transform.localScale = new Vector3(w, h, w);
                b.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial(buildingTint * (0.75f + (float)rng.NextDouble() * 0.25f));
            }

            // Baliza vertical para identificar a cidade à distância (dourada = capital).
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Baliza";
            beacon.transform.SetParent(cityRoot.transform);
            float beaconH = 120f + city.economicWeight * 260f;
            beacon.transform.position = new Vector3(center.x, center.y + beaconH, center.z);
            beacon.transform.localScale = new Vector3(6f, beaconH, 6f);
            Destroy(beacon.GetComponent<Collider>());
            Color beaconCol = city.isCapital ? new Color(1f, 0.82f, 0.2f) : new Color(0.3f, 0.75f, 1f);
            beacon.GetComponent<Renderer>().sharedMaterial = CreateSolidMaterial(beaconCol);
        }

        #endregion
    }
}
