using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Esculpe o RELEVO REAL dos marcos geográficos diretamente no heightmap do terreno,
    /// em vez de plantar cones/cubos decorativos por cima. Assim um vulcão vira um vulcão
    /// de verdade (cone com cratera no cume), uma montanha vira um pico natural, uma chapada
    /// vira uma mesa de topo plano, uma queda d'água ganha a escarpa real (penhasco + poço)
    /// e um lago ganha a bacia escavada que o recebe.
    ///
    /// A leitura/seleção dos marcos (landmarks.bin, GeoNames) também mora aqui para rodar
    /// no momento da geração do terreno — antes de estradas, cidades e vegetação, de modo que
    /// tudo se assente sobre o relevo já esculpido (GetTerrainHeight fica coerente).
    /// </summary>
    public static class TerrainLandmarkSculptor
    {
        private const int MaxPerRegion = 36;       // teto total de marcos por região
        private const int MaxTerrainFeatures = 26; // teto de montanhas/chapadas (o resto são especiais)

        // ------------------------------------------------------------------
        // Modelo de proeminência/tamanho — COMPARTILHADO com a decoração
        // (LandmarkManager usa os mesmos valores para posicionar água e rótulos).
        // Para montanha/vulcão/chapada, lm.height é a elevação do cume em metros;
        // para queda d'água é a altura da queda; para lago é o raio em metros.
        // ------------------------------------------------------------------
        public static float VolcanoRise(Landmark lm) => Mathf.Clamp(lm.height * 0.55f, 600f, 3200f);
        public static float VolcanoRadius(Landmark lm) => Mathf.Clamp(VolcanoRise(lm) * 3.0f, 2200f, 13000f);
        public const float VolcanoCraterRadiusFrac = 0.15f;
        public static float VolcanoCraterDepth(Landmark lm) => Mathf.Min(VolcanoRise(lm) * 0.16f, 320f);

        public static float MountainRise(Landmark lm) => Mathf.Clamp(lm.height * 0.5f, 400f, 3500f);
        public static float MountainRadius(Landmark lm) => Mathf.Clamp(MountainRise(lm) * 2.6f, 1600f, 16000f);

        public static float PlateauRise(Landmark lm) => Mathf.Clamp(lm.height * 0.14f, 120f, 650f);
        public static float PlateauRadius(Landmark lm) => Mathf.Clamp(5000f + lm.height * 2.2f, 4000f, 20000f);

        public static float WaterfallDrop(Landmark lm) => Mathf.Clamp(lm.height, 35f, 900f);
        public static float WaterfallRadius(Landmark lm) => Mathf.Clamp(WaterfallDrop(lm) * 5.0f, 700f, 5000f);

        public static float LakeRadius(Landmark lm) => Mathf.Clamp(lm.height, 400f, 6000f);
        public static float LakeDepth(Landmark lm) => Mathf.Clamp(LakeRadius(lm) * 0.03f, 6f, 90f);

        // ==================================================================
        //  LEITURA + SELEÇÃO
        // ==================================================================

        private static string TypeName(byte b)
        {
            switch (b) { case 1: return "volcano"; case 2: return "plateau"; case 3: return "waterfall"; case 4: return "river"; case 5: return "lake"; default: return "mountain"; }
        }

        private static bool IsTerrainType(string t) { return t == "mountain" || t == "plateau"; }

        /// <summary>
        /// Lê landmarks.bin, filtra pela bounding box da região e seleciona os mais
        /// proeminentes (garante o maior de cada tipo + preenche relevo e especiais até o teto).
        /// </summary>
        public static List<Landmark> ReadAndSelect(RegionData region)
        {
            var chosen = new List<Landmark>();
            if (region == null) return chosen;

            var specials = new List<Landmark>(); // vulcões, quedas, rios, lagos
            var terrainy = new List<Landmark>();  // montanhas, chapadas (relevo)

            string p = Path.Combine(Application.streamingAssetsPath, "landmarks.bin");
            if (!File.Exists(p)) { Debug.LogWarning("[Landmarks] landmarks.bin não encontrado."); return chosen; }
            try
            {
                using (var fs = File.OpenRead(p))
                using (var br = new BinaryReader(fs, Encoding.UTF8))
                {
                    var magic = new string(br.ReadChars(4));
                    if (magic != "LMK1") { Debug.LogWarning("[Landmarks] magic inválido em landmarks.bin."); return chosen; }
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
                return chosen;
            }

            // Proeminência: mais alto/maior primeiro.
            specials.Sort((a, b) => b.height.CompareTo(a.height));
            terrainy.Sort((a, b) => b.height.CompareTo(a.height));

            var picked = new HashSet<Landmark>();
            AddTopOfEachType(terrainy, chosen, picked);
            AddTopOfEachType(specials, chosen, picked);

            int terrainInChosen = 0;
            foreach (var c in chosen) if (IsTerrainType(c.type)) terrainInChosen++;

            for (int i = 0; i < terrainy.Count && chosen.Count < MaxPerRegion && terrainInChosen < MaxTerrainFeatures; i++)
                if (picked.Add(terrainy[i])) { chosen.Add(terrainy[i]); terrainInChosen++; }
            for (int i = 0; i < specials.Count && chosen.Count < MaxPerRegion; i++)
                if (picked.Add(specials[i])) chosen.Add(specials[i]);

            return chosen;
        }

        /// <summary>Garante o marco mais proeminente de cada tipo presente (assinatura da região).</summary>
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

        // ==================================================================
        //  ESCULTURA DO RELEVO (opera no array normalizado 0..1 do heightmap)
        // ==================================================================

        /// <summary>
        /// Esculpe cada marco de relevo no heightmap normalizado. O array é indexado [y, x]
        /// onde y acompanha o eixo Z (norte-sul) e x o eixo X (leste-oeste) do terreno — a mesma
        /// convenção do TerrainData.SetHeights e da síntese do DEM.
        /// </summary>
        public static void SculptIntoHeights(float[,] heights, int res, RegionData region,
                                             float worldW, float worldL, float elevRange, List<Landmark> chosen)
        {
            if (heights == null || chosen == null || region == null || res < 4 || elevRange < 1f) return;

            float invElev = 1f / elevRange;
            float mPerCellX = worldW / (res - 1);
            float mPerCellY = worldL / (res - 1);
            float lonSpan = Mathf.Max(1e-5f, region.maxLon - region.minLon);
            float latSpan = Mathf.Max(1e-5f, region.maxLat - region.minLat);
            int sculpted = 0;

            foreach (var lm in chosen)
            {
                float u = Mathf.Clamp01((lm.lon - region.minLon) / lonSpan);
                float v = Mathf.Clamp01((lm.lat - region.minLat) / latSpan);
                float cx = u * (res - 1);
                float cy = v * (res - 1);

                switch (lm.type)
                {
                    case "volcano": SculptVolcano(heights, res, lm, cx, cy, mPerCellX, mPerCellY, invElev); sculpted++; break;
                    case "mountain": SculptMountain(heights, res, lm, cx, cy, mPerCellX, mPerCellY, invElev); sculpted++; break;
                    case "plateau": SculptPlateau(heights, res, lm, cx, cy, mPerCellX, mPerCellY, invElev); sculpted++; break;
                    case "waterfall": SculptWaterfall(heights, res, lm, cx, cy, mPerCellX, mPerCellY, invElev); sculpted++; break;
                    case "lake": SculptLake(heights, res, lm, cx, cy, mPerCellX, mPerCellY, invElev); sculpted++; break;
                    // "river": a hidrografia própria (RiverManager/RegionRivers) desenha os rios reais.
                }
            }

            if (sculpted > 0)
                Debug.Log($"[Landmarks] Relevo esculpido para {sculpted} marcos em {region.name} (vulcões/montanhas/chapadas/quedas/lagos integrados ao terreno).");
        }

        // --- utilitários de varredura radial ---

        private static void ForEachCellInRadius(float[,] heights, int res, float cx, float cy,
                                                 float radiusM, float mPerCellX, float mPerCellY,
                                                 System.Action<int, int, float, float, float> body)
        {
            int rCellsX = Mathf.CeilToInt(radiusM / mPerCellX) + 1;
            int rCellsY = Mathf.CeilToInt(radiusM / mPerCellY) + 1;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx) - rCellsX);
            int x1 = Mathf.Min(res - 1, Mathf.CeilToInt(cx) + rCellsX);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy) - rCellsY);
            int y1 = Mathf.Min(res - 1, Mathf.CeilToInt(cy) + rCellsY);

            for (int y = y0; y <= y1; y++)
            {
                float dym = (y - cy) * mPerCellY;
                for (int x = x0; x <= x1; x++)
                {
                    float dxm = (x - cx) * mPerCellX;
                    float distM = Mathf.Sqrt(dxm * dxm + dym * dym);
                    float t = distM / radiusM; // 0 no centro .. 1 na borda
                    if (t > 1f) continue;
                    body(x, y, t, dxm, dym);
                }
            }
        }

        private static float Noise01(float x, float y, float freq, float seed)
        {
            return Mathf.PerlinNoise(x * freq + seed, y * freq + seed * 0.7f + 13.1f);
        }

        // --- VULCÃO: cone côncavo + cratera no cume ---
        private static void SculptVolcano(float[,] heights, int res, Landmark lm, float cx, float cy,
                                          float mX, float mY, float invElev)
        {
            float riseN = VolcanoRise(lm) * invElev;
            float craterN = VolcanoCraterDepth(lm) * invElev;
            float R = VolcanoRadius(lm);
            float seed = (lm.lat * 12.9f + lm.lon * 7.3f);

            ForEachCellInRadius(heights, res, cx, cy, R, mX, mY, (x, y, t, dx, dy) =>
            {
                // Flanco côncavo do estratovulcão (mais íngreme perto do cume).
                float flank = Mathf.Pow(1f - t, 1.5f);
                // Leve irregularidade nos flancos (erosão/ravinas), some no cume e na borda.
                float rough = (Noise01(x, y, 0.08f, seed) - 0.5f) * 0.18f * flank * (1f - t);
                float add = riseN * (flank + rough);

                // Cratera: depressão no cume, com borda (rim) levemente elevada.
                if (t < VolcanoCraterRadiusFrac)
                {
                    float ct = t / VolcanoCraterRadiusFrac; // 0 centro .. 1 na borda da cratera
                    add -= craterN * (1f - Mathf.SmoothStep(0f, 1f, ct));
                }
                else if (t < VolcanoCraterRadiusFrac * 1.35f)
                {
                    add += craterN * 0.18f; // rim saliente
                }

                heights[y, x] = Mathf.Clamp01(heights[y, x] + add);
            });
        }

        // --- MONTANHA: pico natural, irregular (sem cratera) ---
        private static void SculptMountain(float[,] heights, int res, Landmark lm, float cx, float cy,
                                           float mX, float mY, float invElev)
        {
            float riseN = MountainRise(lm) * invElev;
            float R = MountainRadius(lm);
            float seed = (lm.lat * 5.7f + lm.lon * 9.1f);

            ForEachCellInRadius(heights, res, cx, cy, R, mX, mY, (x, y, t, dx, dy) =>
            {
                float profile = Mathf.Pow(1f - t, 1.7f);
                // Modulação fractal: cumes/contrafortes em vez de cone liso.
                float n1 = Noise01(x, y, 0.05f, seed);
                float n2 = Noise01(x, y, 0.14f, seed + 40f);
                float mod = 0.65f + 0.5f * n1 + 0.2f * n2;
                float add = riseN * profile * mod;
                heights[y, x] = Mathf.Clamp01(heights[y, x] + add);
            });
        }

        // --- CHAPADA/MESA: topo plano, bordas íngremes ---
        private static void SculptPlateau(float[,] heights, int res, Landmark lm, float cx, float cy,
                                          float mX, float mY, float invElev)
        {
            float riseN = PlateauRise(lm) * invElev;
            float R = PlateauRadius(lm);
            float seed = (lm.lat * 3.3f + lm.lon * 4.9f);

            ForEachCellInRadius(heights, res, cx, cy, R, mX, mY, (x, y, t, dx, dy) =>
            {
                // 1 no topo (t<0.68), descendo a pique até 0 na borda.
                float top = 1f - Mathf.SmoothStep(0.68f, 1f, t);
                // Topo levemente ondulado para não parecer uma mesa de plástico.
                float relief = 0.9f + 0.15f * Noise01(x, y, 0.07f, seed);
                float add = riseN * top * relief;
                heights[y, x] = Mathf.Clamp01(heights[y, x] + add);
            });
        }

        // --- QUEDA D'ÁGUA: escarpa real (platô a montante, penhasco, poço a jusante) ---
        // A água em si (lâmina caindo + poço) é um mesh desenhado pelo LandmarkManager.
        private static void SculptWaterfall(float[,] heights, int res, Landmark lm, float cx, float cy,
                                            float mX, float mY, float invElev)
        {
            float dropN = WaterfallDrop(lm) * invElev;
            float R = WaterfallRadius(lm);
            float seed = (lm.lat * 8.1f + lm.lon * 2.7f);
            const float band = 0.10f; // largura (em t ao longo) da transição do penhasco

            ForEachCellInRadius(heights, res, cx, cy, R, mX, mY, (x, y, t, dx, dy) =>
            {
                float along = dy / R;    // -1 montante .. +1 jusante (eixo Z)
                float lateral = Mathf.Abs(dx) / R;
                if (lateral > 1f) return;

                // Degrau: +meia-queda a montante, -meia-queda a jusante, transição estreita em along=0.
                float step = Mathf.SmoothStep(-band, band, along); // 0 montante .. 1 jusante
                float delta = (0.5f - step) * dropN;               // + a montante / - a jusante

                // Suaviza nas laterais e nas pontas do corredor; ruído dá textura à parede.
                float fadeLat = 1f - Mathf.SmoothStep(0.6f, 1f, lateral);
                float fadeLen = 1f - Mathf.Abs(along);
                float wobble = 1f + 0.12f * (Noise01(x, y, 0.1f, seed) - 0.5f);
                delta *= fadeLat * fadeLen * wobble;

                heights[y, x] = Mathf.Clamp01(heights[y, x] + delta);
            });
        }

        // --- LAGO: bacia escavada (a lâmina d'água é desenhada pelo LandmarkManager) ---
        private static void SculptLake(float[,] heights, int res, Landmark lm, float cx, float cy,
                                       float mX, float mY, float invElev)
        {
            float depthN = LakeDepth(lm) * invElev;
            float R = LakeRadius(lm);
            float seed = (lm.lat * 6.6f + lm.lon * 1.9f);

            ForEachCellInRadius(heights, res, cx, cy, R, mX, mY, (x, y, t, dx, dy) =>
            {
                // Bacia: mais funda no centro, encostando no nível do terreno na borda.
                float bowl = (1f - t * t) * (1f - Mathf.SmoothStep(0.82f, 1f, t));
                float wobble = 1f + 0.1f * (Noise01(x, y, 0.09f, seed) - 0.5f);
                heights[y, x] = Mathf.Clamp01(heights[y, x] - depthN * bowl * wobble);
            });
        }
    }
}
