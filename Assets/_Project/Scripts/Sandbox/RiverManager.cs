using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Desenha os grandes rios como FITAS contínuas seguindo o curso real (polilinhas do Natural Earth),
    /// recortadas pela região ativa e assentadas sobre o relevo. Aparecem em todas as regiões que o rio cruza.
    /// </summary>
    public class RiverManager : MonoBehaviour
    {
        private Terrain terrain;
        private RegionData region;

        private const float RiverWidth = 90f;     // largura da fita (m)
        private const float YOffset = 2.5f;       // levanta a fita acima do solo (evita z-fighting)

        public static RiverManager Create(Terrain terrain, RegionData region)
        {
            var go = new GameObject("RiverManager");
            var m = go.AddComponent<RiverManager>();
            m.terrain = terrain; m.region = region;
            return m;
        }

        private void Start()
        {
            if (terrain == null || region == null) return;
            string p = Path.Combine(Application.streamingAssetsPath, "rivers.bin");
            if (!File.Exists(p)) { Debug.LogWarning("[Rivers] rivers.bin não encontrado."); return; }

            Vector3 size = terrain.terrainData.size;
            Vector3 tp = terrain.transform.position;
            float padLat = (region.maxLat - region.minLat) * 0.02f;
            float padLon = (region.maxLon - region.minLon) * 0.02f;

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var labelsDone = new HashSet<string>();
            var labelRoot = new GameObject("River_Labels").transform;
            int riversHit = 0;

            try
            {
                using (var fs = File.OpenRead(p))
                using (var br = new BinaryReader(fs, Encoding.UTF8))
                {
                    if (new string(br.ReadChars(4)) != "RIV1") { Debug.LogWarning("[Rivers] magic inválido."); return; }
                    int lineCount = br.ReadInt32();
                    for (int l = 0; l < lineCount; l++)
                    {
                        ushort nl = br.ReadUInt16();
                        string name = Encoding.UTF8.GetString(br.ReadBytes(nl));
                        int ptCount = br.ReadInt32();

                        // Lê todos os pontos da linha.
                        var lats = new float[ptCount];
                        var lons = new float[ptCount];
                        for (int i = 0; i < ptCount; i++) { lats[i] = br.ReadSingle(); lons[i] = br.ReadSingle(); }

                        bool any = false;
                        Vector3 labelPos = Vector3.zero;
                        for (int i = 0; i < ptCount - 1; i++)
                        {
                            bool in0 = InRegion(lats[i], lons[i], padLat, padLon);
                            bool in1 = InRegion(lats[i + 1], lons[i + 1], padLat, padLon);
                            if (!in0 && !in1) continue;

                            Vector3 a = WorldPos(lats[i], lons[i], size, tp);
                            Vector3 b = WorldPos(lats[i + 1], lons[i + 1], size, tp);
                            AddQuad(verts, tris, a, b);
                            any = true;
                            if (labelPos == Vector3.zero) labelPos = a;
                        }

                        if (any)
                        {
                            riversHit++;
                            if (!string.IsNullOrEmpty(name) && name != "Rio" && labelsDone.Add(name))
                                AddLabel(labelRoot, labelPos + Vector3.up * 120f, name);
                        }
                    }
                }
            }
            catch (System.Exception ex) { Debug.LogWarning($"[Rivers] Falha ao ler rivers.bin: {ex.Message}"); }

            if (verts.Count >= 3)
            {
                var go = new GameObject("Rivers_Mesh");
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                var mesh = new Mesh { name = "Rivers", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(verts);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mf.sharedMesh = mesh;
                var s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                var mat = s != null ? new Material(s) : new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0.16f, 0.42f, 0.62f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.75f);
                mr.sharedMaterial = mat;
            }

            Debug.Log($"[Rivers] {riversHit} trechos de rio desenhados em {region.name} (fita contínua sobre o relevo).");
        }

        private bool InRegion(float lat, float lon, float padLat, float padLon)
        {
            return lat >= region.minLat - padLat && lat <= region.maxLat + padLat &&
                   lon >= region.minLon - padLon && lon <= region.maxLon + padLon;
        }

        private Vector3 WorldPos(float lat, float lon, Vector3 size, Vector3 tp)
        {
            float u = Mathf.Clamp01((lon - region.minLon) / Mathf.Max(1e-5f, region.maxLon - region.minLon));
            float v = Mathf.Clamp01((lat - region.minLat) / Mathf.Max(1e-5f, region.maxLat - region.minLat));
            float x = (u - 0.5f) * size.x;
            float z = (v - 0.5f) * size.z;
            float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + tp.y + YOffset;
            return new Vector3(x, y, z);
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b)
        {
            Vector3 dir = b - a; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            dir.Normalize();
            Vector3 perp = Vector3.Cross(Vector3.up, dir) * (RiverWidth * 0.5f);

            int baseIdx = verts.Count;
            verts.Add(a - perp); verts.Add(a + perp); verts.Add(b + perp); verts.Add(b - perp);
            tris.Add(baseIdx); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
            tris.Add(baseIdx); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);
        }

        private void AddLabel(Transform root, Vector3 pos, string text)
        {
            var go = new GameObject("River_Label"); go.transform.SetParent(root); go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.color = new Color(0.6f, 0.85f, 1f);
            tm.anchor = TextAnchor.LowerCenter; tm.alignment = TextAlignment.Center;
            tm.fontSize = 90; tm.characterSize = 1.0f;
            go.transform.localScale = Vector3.one * 5f;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial.renderQueue = 4000;
            go.AddComponent<LandmarkBillboard>();
        }
    }
}
