using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    public enum GrassTier
    {
        Lush,       // Verde temperado / tropical
        Savanna,    // Dourado / oliva seco
        Desert      // Palha seca de deserto / ocre árido
    }

    /// <summary>
    /// Campo de grama que acompanha o jogador via GPU Instancing (<see cref="Graphics.DrawMeshInstanced"/>).
    /// Suporta classificação orgânica por bioma a partir das camadas de PBR do terreno (Splatmap) e metadados regionais:
    /// - Áreas verdes/temperadas: grama verdejante e densa (100% cobertura).
    /// - Savanas/estepes: grama dourada-oliva com densidade moderada (60% cobertura).
    /// - Deserto / Dunas / Áreas Áridas: grama MUITO MAIS ESPARSA (5% a 18% de cobertura) com coloração de palha seca/ocre desidratada.
    /// - Penhascos rochosos e calotas polares de neve: supressão natural de vegetação.
    /// </summary>
    public class PlayerFollowGrass : MonoBehaviour
    {
        public Terrain terrain;

        [Header("Bioma e Região")]
        public bool isDesertRegion = false;

        [Header("Campo")]
        public float radius = 95f;           // Raio coberto ao redor do jogador
        public float spacing = 0.9f;         // Distância base entre células da grade
        public float rebuildThreshold = 8f;  // Recentraliza a grade quando o jogador anda isto
        public float maxSlopeDegrees = 36f;  // Acima disso não nasce grama (escarpa íngreme)
        public float minGroundY = 1.0f;      // Abaixo disso (água/leito) não nasce grama

        [Header("Tufos - Dimensões")]
        public Vector2 widthRange = new Vector2(0.8f, 1.5f);
        public Vector2 heightRange = new Vector2(0.4f, 0.8f);

        [Header("Coloração por Bioma")]
        public Color lushColor = new Color(0.38f, 0.58f, 0.24f);       // Verde vibrante
        public Color savannaColor = new Color(0.66f, 0.60f, 0.32f);    // Dourado / oliva seco
        public Color desertColor = new Color(0.79f, 0.72f, 0.46f);     // Palha seca de deserto / ocre

        private Mesh mesh;
        private Texture2D sharedBladeTexture;
        private Material lushMaterial;
        private Material savannaMaterial;
        private Material desertMaterial;

        private Transform player;
        private Vector3 lastBuildCenter = new Vector3(1e9f, 1e9f, 1e9f);
        private bool forceRebuild;

        private const int BatchMax = 1023;
        private List<Matrix4x4[]> lushBatches = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> savannaBatches = new List<Matrix4x4[]>();
        private List<Matrix4x4[]> desertBatches = new List<Matrix4x4[]>();

        // Cache de leitura rápida do splatmap (512x512x8)
        private float[,,] splatCache;
        private int splatRes = 0;

        // Máscara de vias: tufos que caem sobre uma estrada/ferrovia são descartados
        private readonly List<RoadMaskSegment> nearRoadSegs = new List<RoadMaskSegment>();

        public static PlayerFollowGrass Create(Terrain terrain, RegionSaveData save = null, RegionData regionData = null)
        {
            var go = new GameObject("PlayerGrassField");
            var g = go.AddComponent<PlayerFollowGrass>();
            g.terrain = terrain;
            g.isDesertRegion = CheckIfDesert(save, regionData);
            return g;
        }

        public static bool CheckIfDesert(RegionSaveData save, RegionData regionData)
        {
            if (save != null)
            {
                string name = (save.regionName ?? "").ToLower();
                string country = (save.countryName ?? "").ToLower();

                if (name.Contains("desert") || name.Contains("deserto") || name.Contains("saara") || name.Contains("sahara") ||
                    name.Contains("atacama") || name.Contains("kalahari") || name.Contains("namib") || name.Contains("gobi") ||
                    name.Contains("mojave") || name.Contains("sonora") || name.Contains("rub' al khali"))
                {
                    return true;
                }

                if (country == "algeria" || country == "libya" || country == "chad" || country == "niger" ||
                    country == "mali" || country == "mauritania" || country == "sudan" || country == "egypt" ||
                    country == "western sahara" || country == "saudi arabia" || country == "united arab emirates" ||
                    country == "oman" || country == "kuwait" || country == "qatar" || country == "yemen")
                {
                    return true;
                }

                // Critério estatístico: quase sem floresta, sem água e solo arável mínimo
                if (save.forestPercent <= 3 && save.waterPercent <= 8 && save.arablePercent <= 12)
                {
                    return true;
                }
            }

            if (regionData != null)
            {
                float absLat = Mathf.Abs(regionData.centerLat);
                if (absLat >= 15f && absLat <= 32f && save != null && save.forestPercent <= 6 && save.arablePercent <= 15)
                {
                    return true;
                }
            }

            return false;
        }

        private void Start()
        {
            mesh = BuildCrossQuadMesh();
            sharedBladeTexture = GenerateBladeTexture();

            lushMaterial = BuildGrassMaterial(lushColor, "Lush", lushColor * 0.16f);
            savannaMaterial = BuildGrassMaterial(savannaColor, "Savanna", savannaColor * 0.18f);
            desertMaterial = BuildGrassMaterial(desertColor, "Desert", new Color(0.24f, 0.20f, 0.12f));

            EnsureSplatmapLoaded();
        }

        private void OnEnable() { FloatingOrigin.OnOriginRebased += OnOriginRebased; }
        private void OnDisable() { FloatingOrigin.OnOriginRebased -= OnOriginRebased; }
        private void OnOriginRebased(Vector3 offset) { forceRebuild = true; }

        private void EnsureSplatmapLoaded()
        {
            if (splatCache != null && splatRes > 0) return;
            if (terrain == null || terrain.terrainData == null) return;

            try
            {
                splatRes = terrain.terrainData.alphamapResolution;
                splatCache = terrain.terrainData.GetAlphamaps(0, 0, splatRes, splatRes);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PlayerFollowGrass] Não foi possível ler alphamap do terreno: {ex.Message}");
            }
        }

        public static PlayerFollowGrass Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void InvalidateSplatCache()
        {
            if (terrain != null && terrain.terrainData != null)
            {
                try
                {
                    splatRes = terrain.terrainData.alphamapResolution;
                    splatCache = terrain.terrainData.GetAlphamaps(0, 0, splatRes, splatRes);
                    lastBuildCenter = new Vector3(1e9f, 1e9f, 1e9f);
                    forceRebuild = true;
                }
                catch {}
            }
        }

        private float lastPruneTime = 0f;
        public void RequestGrassPruning()
        {
            // Reconstroi os lotes de grama quando o arado cria novos sulcos, sem re-alocar 16MB de splatmap
            if (Time.time - lastPruneTime > 0.75f)
            {
                lastPruneTime = Time.time;
                forceRebuild = true;
            }
        }

        private void GetBiomeWeights(float nx, float nz, out float sand, out float savanna, out float rock, out float snow, out float pavedOrDirt)
        {
            sand = 0f;
            savanna = 0f;
            rock = 0f;
            snow = 0f;
            pavedOrDirt = 0f;

            if (splatCache != null && splatRes > 0)
            {
                int ix = Mathf.Clamp((int)(nx * (splatRes - 1)), 0, splatRes - 1);
                int iz = Mathf.Clamp((int)(nz * (splatRes - 1)), 0, splatRes - 1);

                // Índices das camadas PBR (16 camadas)
                int layers = splatCache.GetLength(2);
                if (layers > 4) sand = splatCache[iz, ix, 4];
                if (layers > 2) savanna = splatCache[iz, ix, 2];
                if (layers > 6) rock = splatCache[iz, ix, 6];
                if (layers > 7) snow = splatCache[iz, ix, 7];

                // Camadas de concreto, asfalto, paralelepípedo, brita e terra simples
                for (int l = 8; l < layers && l <= 12; l++)
                {
                    pavedOrDirt += splatCache[iz, ix, l];
                }
                if (layers > 3) pavedOrDirt += splatCache[iz, ix, 3] * 0.6f;
            }
            else if (isDesertRegion)
            {
                sand = 1f;
            }
        }

        private void Update()
        {
            if (terrain == null || mesh == null) return;

            EnsureSplatmapLoaded();

            if (player == null)
            {
                var cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
            Vector3 center = player != null ? player.position : transform.position;

            // Medir altitude em relação à superfície do terreno abaixo
            float groundY = terrain.SampleHeight(center) + terrain.transform.position.y;
            float altitudeAboveGround = center.y - groundY;

            // Se o jogador/câmera estiver voando alto (> 35 metros acima do solo),
            // a grama fina de 40cm é imperceptível a olho nu. Não renderiza nem recalcula,
            // poupando GPU/CPU e eliminando completamente a cintilação / "flip" da imagem no avião.
            if (altitudeAboveGround > 35f)
            {
                return;
            }

            // Adapta o limiar de reconstrução à velocidade do jogador/veículo (evita recálculo a cada fração de segundo em alta velocidade)
            float playerSpeed = (center - lastBuildCenter).magnitude / Mathf.Max(0.001f, Time.deltaTime);
            float dynamicThreshold = Mathf.Clamp(rebuildThreshold + playerSpeed * 0.45f, 8f, 32f);

            if (forceRebuild || (center - lastBuildCenter).sqrMagnitude > dynamicThreshold * dynamicThreshold)
            {
                Rebuild(center);
                lastBuildCenter = center;
                forceRebuild = false;
            }

            // Renderiza cada grupo com seu material e cor respectivos
            for (int i = 0; i < lushBatches.Count; i++)
            {
                Graphics.DrawMeshInstanced(mesh, 0, lushMaterial, lushBatches[i], lushBatches[i].Length,
                    null, ShadowCastingMode.Off, false);
            }

            for (int i = 0; i < savannaBatches.Count; i++)
            {
                Graphics.DrawMeshInstanced(mesh, 0, savannaMaterial, savannaBatches[i], savannaBatches[i].Length,
                    null, ShadowCastingMode.Off, false);
            }

            for (int i = 0; i < desertBatches.Count; i++)
            {
                Graphics.DrawMeshInstanced(mesh, 0, desertMaterial, desertBatches[i], desertBatches[i].Length,
                    null, ShadowCastingMode.Off, false);
            }
        }

        private void Rebuild(Vector3 center)
        {
            var newLush = new List<Matrix4x4[]>();
            var newSavanna = new List<Matrix4x4[]>();
            var newDesert = new List<Matrix4x4[]>();

            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            float r2 = radius * radius;

            // Pré-filtra os segmentos de via próximos ao centro atual (uma vez por rebuild)
            nearRoadSegs.Clear();
            var rsm = RegionalSandboxManager.Instance;
            if (rsm != null && rsm.RoadMaskSegments.Count > 0)
            {
                var segs = rsm.RoadMaskSegments;
                for (int i = 0; i < segs.Count; i++)
                {
                    float d = DistPointSegment(center.x, center.z, segs[i].a, segs[i].b);
                    if (d < radius + segs[i].clearance + 1f) nearRoadSegs.Add(segs[i]);
                }
            }

            int cellRadius = Mathf.CeilToInt(radius / spacing);
            long centerCellX = Mathf.RoundToInt(center.x / spacing);
            long centerCellZ = Mathf.RoundToInt(center.z / spacing);

            var curLush = new List<Matrix4x4>(BatchMax);
            var curSavanna = new List<Matrix4x4>(BatchMax);
            var curDesert = new List<Matrix4x4>(BatchMax);

            for (long cx = centerCellX - cellRadius; cx <= centerCellX + cellRadius; cx++)
            {
                for (long cz = centerCellZ - cellRadius; cz <= centerCellZ + cellRadius; cz++)
                {
                    uint h = Hash(cx, cz);
                    // Jitter dentro da célula (estável por célula do mundo)
                    float jx = ((h & 0xFFFF) / 65535f - 0.5f) * 0.9f;
                    float jz = (((h >> 16) & 0xFFFF) / 65535f - 0.5f) * 0.9f;
                    float wx = cx * spacing + jx * spacing;
                    float wz = cz * spacing + jz * spacing;

                    float dx = wx - center.x;
                    float dz = wz - center.z;
                    if (dx * dx + dz * dz > r2) continue;

                    float nx = (wx - tPos.x) / tSize.x;
                    float nz = (wz - tPos.z) / tSize.z;
                    if (nx < 0f || nx > 1f || nz < 0f || nz > 1f) continue;

                    if (terrain.terrainData.GetSteepness(nx, nz) > maxSlopeDegrees) continue;

                    float y = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + tPos.y;
                    if (y < minGroundY) continue;

                    // Não nasce grama sobre a via
                    if (IsOnRoad(wx, wz)) continue;

                    // Amostra pesos geológicos do terreno
                    GetBiomeWeights(nx, nz, out float sand, out float savanna, out float rock, out float snow, out float pavedOrDirt);

                    // Supressão em penhascos de rocha, neve ou pavimentos/terra simples pintados
                    if (rock > 0.45f || snow > 0.35f || pavedOrDirt > 0.35f) continue;

                    // Determinação do Bioma e da Taxa de Sobrevivência (Esparsidade)
                    GrassTier tier;
                    float survivalRate;

                    if (isDesertRegion || sand >= 0.35f)
                    {
                        tier = GrassTier.Desert;
                        // Em areia / deserto a grama é MUITO MAIS ESPARSA:
                        // Em areia moderada: ~18% de chance de nascer
                        // Em dunas e deserto pleno: apenas 5% a 7% (apenas pequenos tufos secos espaçados)
                        float sandFactor = Mathf.Clamp01((sand - 0.35f) / 0.65f);
                        survivalRate = Mathf.Lerp(0.18f, 0.05f, sandFactor);
                    }
                    else if (savanna >= 0.30f)
                    {
                        tier = GrassTier.Savanna;
                        survivalRate = 0.60f; // 60% de densidade em savana
                    }
                    else
                    {
                        tier = GrassTier.Lush;
                        survivalRate = 1.0f; // 100% em pradarias e campos verdes
                    }

                    // Sorteio determinístico por célula estável:
                    // Se o número for maior que a taxa de sobrevivência, descarta o tufo (gera a esparsidade)
                    float roll = ((h >> 8) & 0xFFFF) / 65535f;
                    if (roll > survivalRate) continue;

                    // Rotação e escala variadas
                    uint h2 = Hash(cx * 2654435761 + 1, cz * 40503 + 7);
                    float yaw = (h2 & 0xFF) / 255f * 360f;

                    float w, hh;
                    if (tier == GrassTier.Desert)
                    {
                        // No deserto: tufo mais baixo, seco e achatado pelo vento árido
                        w = Mathf.Lerp(0.9f, 1.6f, ((h2 >> 8) & 0xFF) / 255f);
                        hh = Mathf.Lerp(0.24f, 0.50f, ((h2 >> 16) & 0xFF) / 255f);
                    }
                    else if (tier == GrassTier.Savanna)
                    {
                        w = Mathf.Lerp(0.8f, 1.5f, ((h2 >> 8) & 0xFF) / 255f);
                        hh = Mathf.Lerp(0.35f, 0.70f, ((h2 >> 16) & 0xFF) / 255f);
                    }
                    else
                    {
                        w = Mathf.Lerp(widthRange.x, widthRange.y, ((h2 >> 8) & 0xFF) / 255f);
                        hh = Mathf.Lerp(heightRange.x, heightRange.y, ((h2 >> 16) & 0xFF) / 255f);
                    }

                    var trs = Matrix4x4.TRS(new Vector3(wx, y, wz), Quaternion.Euler(0f, yaw, 0f), new Vector3(w, hh, w));

                    // Adiciona ao lote correto do bioma
                    switch (tier)
                    {
                        case GrassTier.Desert:
                            curDesert.Add(trs);
                            if (curDesert.Count == BatchMax)
                            {
                                newDesert.Add(curDesert.ToArray());
                                curDesert = new List<Matrix4x4>(BatchMax);
                            }
                            break;
                        case GrassTier.Savanna:
                            curSavanna.Add(trs);
                            if (curSavanna.Count == BatchMax)
                            {
                                newSavanna.Add(curSavanna.ToArray());
                                curSavanna = new List<Matrix4x4>(BatchMax);
                            }
                            break;
                        default:
                            curLush.Add(trs);
                            if (curLush.Count == BatchMax)
                            {
                                newLush.Add(curLush.ToArray());
                                curLush = new List<Matrix4x4>(BatchMax);
                            }
                            break;
                    }
                }
            }

            if (curLush.Count > 0) newLush.Add(curLush.ToArray());
            if (curSavanna.Count > 0) newSavanna.Add(curSavanna.ToArray());
            if (curDesert.Count > 0) newDesert.Add(curDesert.ToArray());

            // Troca atômica dos lotes: elimina qualquer frame em branco ou cintilação visual
            lushBatches = newLush;
            savannaBatches = newSavanna;
            desertBatches = newDesert;
        }

        private bool IsOnRoad(float wx, float wz)
        {
            for (int i = 0; i < nearRoadSegs.Count; i++)
            {
                var s = nearRoadSegs[i];
                if (DistPointSegment(wx, wz, s.a, s.b) < s.clearance) return true;
            }
            return false;
        }

        /// <summary>Distância (XZ) de um ponto ao segmento a→b.</summary>
        private static float DistPointSegment(float px, float pz, Vector2 a, Vector2 b)
        {
            float abx = b.x - a.x, abz = b.y - a.y;
            float apx = px - a.x, apz = pz - a.y;
            float len2 = abx * abx + abz * abz;
            float t = len2 > 1e-6f ? Mathf.Clamp01((apx * abx + apz * abz) / len2) : 0f;
            float cx = a.x + abx * t, cz = a.y + abz * t;
            float dx = px - cx, dz = pz - cz;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Hash inteiro determinístico (estável e sem erro de precisão de float).</summary>
        private static uint Hash(long x, long z)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(z * 19349663);
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return h;
            }
        }

        /// <summary>
        /// Tufo: 3 quads cruzados com dupla face, pivô na base. As NORMAIS apontam para CIMA
        /// para receber luz do céu/sol e não ficar escura.
        /// </summary>
        private static Mesh BuildCrossQuadMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            const int planes = 3;
            for (int p = 0; p < planes; p++)
            {
                float ang = (180f / planes) * p * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 left = -dir * 0.5f;
                Vector3 right = dir * 0.5f;

                int b = verts.Count;
                verts.Add(left);
                verts.Add(right);
                verts.Add(right + Vector3.up);
                verts.Add(left + Vector3.up);
                for (int k = 0; k < 4; k++) normals.Add(Vector3.up);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
            }

            var m = new Mesh { name = "PlayerGrassTuft" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetNormals(normals);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Material cutout do Built-in RP (Standard em modo Cutout), com GPU instancing.</summary>
        private Material BuildGrassMaterial(Color color, string nameSuffix, Color emission)
        {
            Shader shader = Shader.Find("Standard");
            Material mat;
            if (shader != null)
            {
                mat = new Material(shader) { name = $"PlayerGrass_{nameSuffix}" };
                mat.SetFloat("_Mode", 1f); // Cutout
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.4f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
                mat.mainTexture = sharedBladeTexture;
                mat.SetColor("_Color", color);
                // Leve emissão adaptada à tonalidade para garantir boa visibilidade sob sombra
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emission);
                }
            }
            else
            {
                mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = sharedBladeTexture };
            }
            mat.enableInstancing = true;
            return mat;
        }

        /// <summary>Textura procedural de lâminas claras (a cor final vem de _Color).</summary>
        private static Texture2D GenerateBladeTexture()
        {
            const int W = 64, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true);
            var px = new Color[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);

            for (int bld = 0; bld < 20; bld++)
            {
                float cx = Random.Range(2f, W - 2f);
                float baseWidth = Random.Range(1.6f, 3.2f);
                float lean = Random.Range(-5f, 5f);
                float topY = Random.Range(H * 0.6f, H - 2f);

                for (int y = 0; y < topY; y++)
                {
                    float t = y / topY;
                    float w = baseWidth * (1f - t * 0.8f);
                    float centerX = cx + lean * t;
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(centerX - w), 0, W - 1);
                    int x1 = Mathf.Clamp(Mathf.CeilToInt(centerX + w), 0, W - 1);
                    float shade = Mathf.Lerp(0.85f, 1f, t);
                    var c = new Color(shade, shade, shade, 1f);
                    for (int x = x0; x <= x1; x++) px[y * W + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true);
            return tex;
        }
    }
}
