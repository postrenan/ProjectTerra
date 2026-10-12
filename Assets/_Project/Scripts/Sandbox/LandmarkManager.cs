using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public class Landmark
    {
        public string name;
        public string type;   // volcano | mountain | plateau | waterfall | river
        public float lat;
        public float lon;
        public float height;
    }

    /// <summary>Billboard de texto que sempre encara a câmera (rótulos dos marcos).</summary>
    public class LandmarkBillboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    /// <summary>
    /// Posiciona marcos geográficos especiais (vulcões, montanhas/cordilheiras, chapadas, quedas d'água,
    /// grandes rios) na posição real dentro da região ativa — visíveis de longe e alcançáveis.
    /// Lê o dataset mundial landmarks.bin (GeoNames) e limita a quantidade por região para não poluir.
    /// </summary>
    public class LandmarkManager : MonoBehaviour
    {
        private const int MaxPerRegion = 36;     // teto total de marcos por região
        private const int MaxTerrainFeatures = 26; // teto de montanhas/chapadas (o resto são especiais)

        private Terrain terrain;
        private RegionData region;

        public static readonly List<KeyValuePair<Vector3, string>> Placed = new List<KeyValuePair<Vector3, string>>();

        public static LandmarkManager Create(Terrain terrain, RegionData region)
        {
            var go = new GameObject("LandmarkManager");
            var m = go.AddComponent<LandmarkManager>();
            m.terrain = terrain;
            m.region = region;
            return m;
        }

        private static string TypeName(byte b)
        {
            switch (b) { case 1: return "volcano"; case 2: return "plateau"; case 3: return "waterfall"; case 4: return "river"; case 5: return "lake"; default: return "mountain"; }
        }

        private static bool IsTerrainType(string t) { return t == "mountain" || t == "plateau"; }

        /// <summary>
        /// Garante o marco mais proeminente (mais alto/maior) de cada tipo presente na lista.
        /// A lista deve vir ordenada por altura decrescente. Assim a assinatura da região
        /// (vulcão mais alto, maior montanha, maior lago/queda) nunca é descartada.
        /// </summary>
        private static void AddTopOfEachType(List<Landmark> sortedByHeightDesc, List<Landmark> chosen, HashSet<Landmark> picked)
        {
            var typesDone = new HashSet<string>();
            foreach (var lm in sortedByHeightDesc)
            {
                if (chosen.Count >= MaxPerRegion) break;
                if (typesDone.Contains(lm.type)) continue;
                if (picked.Add(lm)) { chosen.Add(lm); typesDone.Add(lm.type); }
            }
        }

        private void Start()
        {
            Placed.Clear();
            if (terrain == null || region == null)
            {
                Debug.LogWarning("[Landmarks] Sem terreno/dados de região — marcos não posicionados.");
                return;
            }

            var specials = new List<Landmark>();   // vulcões, quedas, rios, lagos
            var terrainy = new List<Landmark>();    // montanhas, chapadas (relevo)
            ReadInRegion(specials, terrainy);

            // Ordena os dois grupos por proeminência (altura/tamanho) — mais marcante primeiro.
            specials.Sort((a, b) => b.height.CompareTo(a.height));
            terrainy.Sort((a, b) => b.height.CompareTo(a.height));

            // Seleção por proeminência (antes era pela ordem bruta do arquivo, o que descartava
            // marcos famosos como o Fuji em regiões densas). Agora:
            //   1) garante o maior marco de CADA tipo presente (assinatura da região);
            //   2) preenche o resto priorizando o relevo até o teto, do mais alto ao menos;
            //   3) completa com os especiais mais proeminentes restantes.
            var chosen = new List<Landmark>();
            var picked = new HashSet<Landmark>();

            AddTopOfEachType(terrainy, chosen, picked);
            AddTopOfEachType(specials, chosen, picked);

            int terrainInChosen = 0;
            foreach (var c in chosen) if (IsTerrainType(c.type)) terrainInChosen++;

            for (int i = 0; i < terrainy.Count && chosen.Count < MaxPerRegion && terrainInChosen < MaxTerrainFeatures; i++)
            {
                if (picked.Add(terrainy[i])) { chosen.Add(terrainy[i]); terrainInChosen++; }
            }
            for (int i = 0; i < specials.Count && chosen.Count < MaxPerRegion; i++)
            {
                if (picked.Add(specials[i])) chosen.Add(specials[i]);
            }

            var root = new GameObject("Landmarks").transform;
            foreach (var lm in chosen)
            {
                BuildLandmark(root, lm, WorldPos(lm.lat, lm.lon));
            }
            WorldStreaming.SetLayerRecursive(root.gameObject, WorldStreaming.LayerLandmark);

            Debug.Log($"[Landmarks] {chosen.Count} marcos em {region.name} " +
                      $"(relevo disp.={terrainy.Count}, especiais disp.={specials.Count}); visíveis até {WorldStreaming.DistLandmark:F0}m.");
        }

        private void ReadInRegion(List<Landmark> specials, List<Landmark> terrainy)
        {
            string p = Path.Combine(Application.streamingAssetsPath, "landmarks.bin");
            if (!File.Exists(p)) { Debug.LogWarning("[Landmarks] landmarks.bin não encontrado."); return; }
            try
            {
                using (var fs = File.OpenRead(p))
                using (var br = new BinaryReader(fs, Encoding.UTF8))
                {
                    var magic = new string(br.ReadChars(4));
                    if (magic != "LMK1") { Debug.LogWarning("[Landmarks] magic inválido em landmarks.bin."); return; }
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        float lat = br.ReadSingle();
                        float lon = br.ReadSingle();
                        float h = br.ReadSingle();
                        byte tp = br.ReadByte();
                        ushort nl = br.ReadUInt16();
                        byte[] nb = br.ReadBytes(nl);

                        if (lat < region.minLat || lat > region.maxLat || lon < region.minLon || lon > region.maxLon) continue;

                        var lm = new Landmark { lat = lat, lon = lon, height = h, type = TypeName(tp), name = Encoding.UTF8.GetString(nb) };
                        if (IsTerrainType(lm.type)) terrainy.Add(lm);
                        else specials.Add(lm);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Landmarks] Falha ao ler landmarks.bin: {ex.Message}");
            }
        }

        private Vector3 WorldPos(float lat, float lon)
        {
            Vector3 size = terrain.terrainData.size;
            Vector3 tp = terrain.transform.position;
            float u = Mathf.Clamp01((lon - region.minLon) / Mathf.Max(1e-5f, region.maxLon - region.minLon));
            float v = Mathf.Clamp01((lat - region.minLat) / Mathf.Max(1e-5f, region.maxLat - region.minLat));
            float x = (u - 0.5f) * size.x;
            float z = (v - 0.5f) * size.z;
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tp.y;
            return new Vector3(x, y, z);
        }

        private void BuildLandmark(Transform root, Landmark lm, Vector3 pos)
        {
            string icon; Color labelColor; float labelOffset;
            switch (lm.type)
            {
                case "volcano":
                    BuildVolcano(root, pos, Mathf.Clamp(lm.height * 0.45f, 700f, 2800f));
                    icon = "🌋"; labelColor = new Color(1f, 0.55f, 0.3f); labelOffset = 1600f; break;
                case "waterfall":
                    BuildWaterfall(root, pos, Mathf.Clamp(lm.height, 30f, 1000f));
                    icon = "💧"; labelColor = new Color(0.6f, 0.9f, 1f); labelOffset = 500f; break;
                case "river":
                    BuildRiver(root, pos);
                    icon = "🌊"; labelColor = new Color(0.5f, 0.8f, 1f); labelOffset = 350f; break;
                case "lake":
                    // height carrega o raio aproximado do lago em metros (gravado na ingestão).
                    BuildLake(root, pos, Mathf.Clamp(lm.height, 400f, 6000f));
                    icon = "🏞️"; labelColor = new Color(0.55f, 0.85f, 1f); labelOffset = 400f; break;
                case "plateau":
                    BuildMarker(root, pos, new Color(0.62f, 0.44f, 0.30f), Mathf.Clamp(lm.height * 0.1f, 180f, 700f));
                    icon = "🏜️"; labelColor = new Color(0.9f, 0.75f, 0.5f); labelOffset = 500f; break;
                default: // mountain
                    BuildMarker(root, pos, new Color(0.5f, 0.5f, 0.52f), Mathf.Clamp(lm.height * 0.12f, 220f, 900f));
                    icon = "⛰️"; labelColor = new Color(0.9f, 0.95f, 1f); labelOffset = 650f; break;
            }

            AddLabel(root, new Vector3(pos.x, pos.y + labelOffset, pos.z), lm.name, labelColor);
            Placed.Add(new KeyValuePair<Vector3, string>(pos, $"{icon} {lm.name}"));
        }

        private static Material Mat(Color c, float gloss = 0.1f)
        {
            return URPMaterialHelper.CreateURPLitMaterial("Landmark_" + c.GetHashCode(), c, gloss, 0.0f);
        }

        private static Mesh ConeMesh(float radius, float height, int seg = 20)
        {
            var verts = new List<Vector3>(); var tris = new List<int>();
            verts.Add(new Vector3(0f, height, 0f)); verts.Add(Vector3.zero);
            for (int i = 0; i < seg; i++) { float a = (i / (float)seg) * Mathf.PI * 2f; verts.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius)); }
            for (int i = 0; i < seg; i++) { int a = 2 + i, b = 2 + (i + 1) % seg; tris.Add(0); tris.Add(b); tris.Add(a); tris.Add(1); tris.Add(a); tris.Add(b); }
            var m = new Mesh { name = "Cone" }; m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }

        private static GameObject ConeObject(Transform parent, Vector3 pos, float radius, float height, Color col)
        {
            var go = new GameObject("Cone"); go.transform.SetParent(parent); go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = ConeMesh(radius, height);
            go.AddComponent<MeshRenderer>().sharedMaterial = Mat(col);
            return go;
        }

        private void BuildVolcano(Transform root, Vector3 pos, float h)
        {
            float r = h * 0.95f;
            ConeObject(root, pos, r, h, new Color(0.30f, 0.22f, 0.18f)).name = "Volcano";
            var crater = ConeObject(root, pos + Vector3.up * (h * 0.82f), r * 0.22f, h * 0.18f, new Color(0.12f, 0.08f, 0.07f));
            crater.transform.localScale = new Vector3(1f, -1f, 1f); crater.name = "Cratera";
            ConeObject(root, pos + Vector3.up * (h * 0.78f), r * 0.26f, h * 0.06f, new Color(0.6f, 0.18f, 0.08f)).name = "Rim";
        }

        private void BuildMarker(Transform root, Vector3 pos, Color col, float size)
        {
            ConeObject(root, pos, size * 0.6f, size, col).name = "Marker";
        }

        private void BuildWaterfall(Transform root, Vector3 pos, float h)
        {
            var cliff = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cliff.name = "Waterfall_Cliff"; cliff.transform.SetParent(root);
            cliff.transform.position = pos + Vector3.up * (h * 0.5f);
            cliff.transform.localScale = new Vector3(h * 1.4f, h, h * 0.4f);
            cliff.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.42f, 0.40f, 0.38f));

            var water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "Waterfall_Water"; water.transform.SetParent(root);
            water.transform.position = pos + Vector3.up * (h * 0.5f) + new Vector3(0f, 0f, h * 0.22f);
            water.transform.localScale = new Vector3(h * 0.9f, h, 1.5f);
            water.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.75f, 0.9f, 1f), 0.8f);
            Destroy(water.GetComponent<Collider>());
        }

        private void BuildRiver(Transform root, Vector3 pos)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "River_Segment"; seg.transform.SetParent(root);
            seg.transform.position = pos + Vector3.up * 1.0f;
            seg.transform.localScale = new Vector3(140f, 2f, 1400f);
            seg.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.15f, 0.42f, 0.62f), 0.7f);
            Destroy(seg.GetComponent<Collider>());
        }

        /// <summary>Corpo d'água de lago: disco raso e translúcido rente ao terreno, com raio em metros.</summary>
        private void BuildLake(Transform root, Vector3 pos, float radius)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Lake_Water"; water.transform.SetParent(root);
            // Cylinder do Unity tem 2m de altura e 1m de raio por padrão; escala para o raio desejado
            // e achata a espessura para uma lâmina d'água rente ao solo.
            water.transform.position = pos + Vector3.up * 1.5f;
            water.transform.localScale = new Vector3(radius * 2f, 1.5f, radius * 2f);
            water.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.20f, 0.45f, 0.68f), 0.85f);
            Destroy(water.GetComponent<Collider>());
        }

        private void AddLabel(Transform root, Vector3 pos, string text, Color color)
        {
            var go = new GameObject("Label"); go.transform.SetParent(root); go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.color = color; tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center;
            tm.fontSize = 90; tm.characterSize = 1.0f;
            go.transform.localScale = Vector3.one * 6f;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial.renderQueue = 4000;
            go.AddComponent<LandmarkBillboard>();
        }
    }
}
