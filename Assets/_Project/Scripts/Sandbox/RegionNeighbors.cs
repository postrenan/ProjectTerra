using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Descobre as regiões de fronteira (divisas) de uma região por sobreposição de bounding box,
    /// a partir do regions_database.bin. Vizinhos do mesmo país aparecem pelo NOME do estado;
    /// vizinhos estrangeiros são agrupados pelo NOME DO PAÍS.
    /// Ex.: Rio Grande do Sul → "Santa Catarina • Argentina • Uruguai".
    /// </summary>
    public static class RegionNeighbors
    {
        private static List<RegionData> all;

        private static void EnsureLoaded()
        {
            if (all != null) return;
            all = new List<RegionData>();
            string p = Path.Combine(Application.streamingAssetsPath, "regions_database.bin");
            try
            {
                var db = RegionDatabase.LoadFromBinary(p);
                if (db != null && db.regions != null) all = db.regions;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RegionNeighbors] Falha ao carregar banco de regiões: {ex.Message}");
            }
        }

        private static bool IsCountryLevel(RegionData r)
        {
            // Entrada de país inteiro (ex.: "Brazil"/"Brazil") ou bbox gigante — não é uma divisa estadual.
            return r.name == r.country ||
                   ((r.maxLat - r.minLat) > 10f && (r.maxLon - r.minLon) > 10f);
        }

        /// <summary>
        /// Lista de divisas (rótulos prontos), ordenada pela extensão da fronteira.
        /// Estados do mesmo país pelo nome; países estrangeiros agrupados pelo nome do país.
        /// </summary>
        public static List<string> GetBorderLabels(RegionData me, int max = 6)
        {
            var labels = new List<string>();
            if (me == null) return labels;
            EnsureLoaded();

            var scored = new List<KeyValuePair<float, string>>();
            var domesticSeen = new HashSet<string>();
            var foreignArea = new Dictionary<string, float>();

            foreach (var r in all)
            {
                if (r == null || r.id == me.id || IsCountryLevel(r)) continue;

                float ox = Mathf.Min(me.maxLon, r.maxLon) - Mathf.Max(me.minLon, r.minLon);
                float oy = Mathf.Min(me.maxLat, r.maxLat) - Mathf.Max(me.minLat, r.minLat);
                if (ox <= 0f || oy <= 0f) continue; // sem sobreposição real de bbox

                float area = ox * oy;
                if (r.country == me.country)
                {
                    if (domesticSeen.Add(r.name))
                        scored.Add(new KeyValuePair<float, string>(area, r.name));
                }
                else
                {
                    foreignArea.TryGetValue(r.country, out float acc);
                    foreignArea[r.country] = acc + area;
                }
            }

            foreach (var kv in foreignArea)
                scored.Add(new KeyValuePair<float, string>(kv.Value, kv.Key));

            scored.Sort((a, b) => b.Key.CompareTo(a.Key));
            for (int i = 0; i < scored.Count && labels.Count < max; i++)
                labels.Add(scored[i].Value);

            return labels;
        }
    }
}
