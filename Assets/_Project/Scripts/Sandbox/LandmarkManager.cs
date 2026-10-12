using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public class Landmark
    {
        public string name;
        public string type;   // volcano | mountain | plateau | waterfall | river | lake
        public float lat;
        public float lon;
        public float height;  // elevação do cume (m) | altura da queda (m) | raio do lago (m)
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
    /// Decora os marcos geográficos cujo RELEVO já foi esculpido no terreno pelo
    /// <see cref="TerrainLandmarkSculptor"/>. Em vez de plantar cones/cubos gigantes, aqui só
    /// adicionamos o que o heightmap não pode representar: a água (lâmina da cachoeira + poço,
    /// superfície do lago, fita do rio), o brilho de lava na cratera do vulcão, os rótulos e os
    /// marcadores do mapa. Assim um vulcão é um vulcão de verdade, uma cachoeira é uma escarpa
    /// com água caindo, um lago é uma bacia com lâmina d'água — relevo real, não decoração.
    /// </summary>
    public class LandmarkManager : MonoBehaviour
    {
        private Terrain terrain;
        private RegionData region;
        private List<Landmark> chosen;

        public static readonly List<KeyValuePair<Vector3, string>> Placed = new List<KeyValuePair<Vector3, string>>();

        public static LandmarkManager Create(Terrain terrain, RegionData region, List<Landmark> chosen = null)
        {
            var go = new GameObject("LandmarkManager");
            var m = go.AddComponent<LandmarkManager>();
            m.terrain = terrain;
            m.region = region;
            m.chosen = chosen;
            return m;
        }

        private void Start()
        {
            Placed.Clear();
            if (terrain == null || region == null)
            {
                Debug.LogWarning("[Landmarks] Sem terreno/dados de região — marcos não posicionados.");
                return;
            }

            // Se o relevo não foi esculpido na geração do terreno (ex.: metadados ausentes na época),
            // seleciona agora como fallback — a decoração ainda assenta sobre o SampleHeight atual.
            if (chosen == null) chosen = TerrainLandmarkSculptor.ReadAndSelect(region);
            if (chosen == null || chosen.Count == 0) return;

            var root = new GameObject("Landmarks").transform;
            foreach (var lm in chosen)
            {
                DecorateLandmark(root, lm, WorldPos(lm.lat, lm.lon));
            }
            WorldStreaming.SetLayerRecursive(root.gameObject, WorldStreaming.LayerLandmark);

            Debug.Log($"[Landmarks] {chosen.Count} marcos decorados em {region.name}; " +
                      $"relevo integrado ao terreno, visíveis até {WorldStreaming.DistLandmark:F0}m.");
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

        private float SampleY(float x, float z)
        {
            return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        }

        private void DecorateLandmark(Transform root, Landmark lm, Vector3 pos)
        {
            string icon; Color labelColor; float labelY;
            switch (lm.type)
            {
                case "volcano":
                    labelY = DecorateVolcano(root, lm, pos);
                    icon = "🌋"; labelColor = new Color(1f, 0.55f, 0.3f); break;
                case "waterfall":
                    labelY = DecorateWaterfall(root, lm, pos);
                    icon = "💧"; labelColor = new Color(0.6f, 0.9f, 1f); break;
                case "lake":
                    labelY = DecorateLake(root, lm, pos);
                    icon = "🏞️"; labelColor = new Color(0.55f, 0.85f, 1f); break;
                case "river":
                    labelY = DecorateRiver(root, lm, pos);
                    icon = "🌊"; labelColor = new Color(0.5f, 0.8f, 1f); break;
                case "plateau":
                    labelY = pos.y + TerrainLandmarkSculptor.PlateauRise(lm) * 0.4f + 120f;
                    icon = "🏜️"; labelColor = new Color(0.9f, 0.75f, 0.5f); break;
                default: // mountain
                    labelY = pos.y + TerrainLandmarkSculptor.MountainRise(lm) * 0.5f + 150f;
                    icon = "⛰️"; labelColor = new Color(0.9f, 0.95f, 1f); break;
            }

            AddLabel(root, new Vector3(pos.x, labelY, pos.z), lm.name, labelColor);
            Placed.Add(new KeyValuePair<Vector3, string>(pos, $"{icon} {lm.name}"));
        }

        // --- VULCÃO: o cone/cratera já são relevo; aqui só o brilho de lava no fundo da cratera. ---
        private float DecorateVolcano(Transform root, Landmark lm, Vector3 pos)
        {
            float rise = TerrainLandmarkSculptor.VolcanoRise(lm);
            float craterR = TerrainLandmarkSculptor.VolcanoRadius(lm) * TerrainLandmarkSculptor.VolcanoCraterRadiusFrac;

            // pos.y é a cota do fundo da cratera (ponto mais baixo do cume). Poça de lava rente a ele.
            var lava = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lava.name = "Volcano_Lava"; lava.transform.SetParent(root);
            lava.transform.position = new Vector3(pos.x, pos.y + 2f, pos.z);
            lava.transform.localScale = new Vector3(craterR * 1.1f, 2f, craterR * 1.1f);
            var col = lava.GetComponent<Collider>(); if (col != null) Destroy(col);
            lava.GetComponent<Renderer>().sharedMaterial = LavaMaterial();

            // Rótulo acima do cume (fundo da cratera + profundidade ≈ borda/cume).
            return pos.y + TerrainLandmarkSculptor.VolcanoCraterDepth(lm) + rise * 0.1f + 220f;
        }

        // --- CACHOEIRA: escarpa já é relevo; aqui a lâmina d'água caindo + o poço. ---
        private float DecorateWaterfall(Transform root, Landmark lm, Vector3 pos)
        {
            float R = TerrainLandmarkSculptor.WaterfallRadius(lm);
            float drop = TerrainLandmarkSculptor.WaterfallDrop(lm);
            float cliffHalf = R * 0.12f;

            float topY = SampleY(pos.x, pos.z - cliffHalf); // montante (-Z), alto
            float botY = SampleY(pos.x, pos.z + cliffHalf); // jusante (+Z), baixo
            float fallH = Mathf.Max(topY - botY, drop * 0.5f);
            float width = Mathf.Max(R * 0.22f, 40f);

            var mat = WaterSystem.CreateWaterMaterial(isRiver: true);

            // Lâmina d'água descendo a parede do penhasco (face voltada para +Z, jusante).
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sheet.name = "Waterfall_Sheet"; sheet.transform.SetParent(root);
            sheet.transform.position = new Vector3(pos.x, (topY + botY) * 0.5f, pos.z + cliffHalf * 0.25f);
            sheet.transform.localScale = new Vector3(width, fallH, Mathf.Max(R * 0.05f, 6f));
            var sc = sheet.GetComponent<Collider>(); if (sc != null) Destroy(sc);
            sheet.GetComponent<Renderer>().sharedMaterial = mat;

            // Poço de queda (plunge pool) a jusante, rente ao terreno baixo.
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = "Waterfall_Pool"; pool.transform.SetParent(root);
            pool.transform.position = new Vector3(pos.x, botY + 0.6f, pos.z + R * 0.3f);
            pool.transform.localScale = new Vector3(R * 0.55f, 0.8f, R * 0.55f);
            var pc = pool.GetComponent<Collider>(); if (pc != null) Destroy(pc);
            pool.GetComponent<Renderer>().sharedMaterial = mat;

            return topY + 120f;
        }

        // --- LAGO: bacia já é relevo; aqui a lâmina d'água rente à borda. ---
        private float DecorateLake(Transform root, Landmark lm, Vector3 pos)
        {
            float R = TerrainLandmarkSculptor.LakeRadius(lm);
            float depth = TerrainLandmarkSculptor.LakeDepth(lm);
            // pos.y é o fundo da bacia; a lâmina d'água fica logo abaixo da borda original.
            float waterY = pos.y + depth * 0.85f;

            var water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Lake_Water"; water.transform.SetParent(root);
            water.transform.position = new Vector3(pos.x, waterY, pos.z);
            water.transform.localScale = new Vector3(R * 1.9f, 1.0f, R * 1.9f);
            var col = water.GetComponent<Collider>(); if (col != null) Destroy(col);
            water.GetComponent<Renderer>().sharedMaterial = WaterSystem.CreateWaterMaterial(isRiver: false);

            return waterY + 80f;
        }

        // --- RIO (marco pontual): fita d'água rente ao terreno (os rios reais vêm do RiverManager). ---
        private float DecorateRiver(Transform root, Landmark lm, Vector3 pos)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "River_Segment"; seg.transform.SetParent(root);
            seg.transform.position = pos + Vector3.up * 1.0f;
            seg.transform.localScale = new Vector3(140f, 2f, 1400f);
            var col = seg.GetComponent<Collider>(); if (col != null) Destroy(col);
            seg.GetComponent<Renderer>().sharedMaterial = WaterSystem.CreateWaterMaterial(isRiver: true);
            return pos.y + 120f;
        }

        private static Material _lavaMat;
        private static Material LavaMaterial()
        {
            if (_lavaMat != null) return _lavaMat;
            _lavaMat = URPMaterialHelper.CreateURPLitMaterial("Volcano_Lava", new Color(0.85f, 0.22f, 0.05f), 0.35f, 0f);
            _lavaMat.EnableKeyword("_EMISSION");
            _lavaMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (_lavaMat.HasProperty("_EmissionColor"))
                _lavaMat.SetColor("_EmissionColor", new Color(1.4f, 0.45f, 0.08f));
            return _lavaMat;
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
