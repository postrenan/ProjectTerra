using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet.TerrainStreaming;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Planta a vegetação da região a partir da cobertura do solo REAL de satélite
    /// (ESA WorldCover 10m), carregada de StreamingAssets/GeographicData/landcover_&lt;id&gt;.raw.
    ///
    /// Cada célula da grade de cobertura vira uma densidade de árvores por classe
    /// (mata, arbusto, campo, mangue, pântano), espalhadas como <see cref="TreeInstance"/>
    /// nativas do Unity Terrain — billboards/instancing cuidam de milhares por região,
    /// e o streaming por região (uma de cada vez) escala isso para o planeta inteiro.
    ///
    /// Os prefabs de árvore são gerados proceduralmente em runtime (tronco + copa),
    /// sem dependência de assets externos: folhosa, conífera, acácia/savana e arbusto.
    /// </summary>
    public static class VegetationSpawner
    {
        // Índices dos protótipos de árvore gerados.
        private const int ProtoBroadleaf = 0;
        private const int ProtoConifer   = 1;
        private const int ProtoAcacia    = 2;
        private const int ProtoShrub     = 3;

        // Densidade (árvores por km²) por classe WorldCover. Valores "de jogo"
        // (legíveis, não 1:1 com a floresta real, que teria dezenas de milhares/km²).
        private const float DensTree     = 350f; // 10 Tree cover
        private const float DensMangrove = 300f; // 95 Mangroves
        private const float DensWetland  = 25f;  // 90 Herbaceous wetland
        private const float DensShrub    = 90f;  // 20 Shrubland
        private const float DensGrass    = 6f;   // 30 Grassland (árvore ocasional)

        // Teto de instâncias por região (acima disso a densidade é escalada p/ caber).
        // O Terrain instancia árvores na GPU e aguenta centenas de milhares; manter
        // isto alto é essencial para que regiões grandes (dezenas de km) tenham
        // floresta VISÍVEL, e não ~5 árvores/km².
        private const int MaxInstances = 150000;

        // Declives acima disto raramente recebem árvores (evita árvores em paredões).
        private const float MaxSteepnessDeg = 42f;

        private static GameObject prototypeHolder;

        public static void Populate(Terrain terrain, int regionId, int seed, float centerLat)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            if (!GeographicDataLoader.LoadRegionalLandcover(regionId, out byte[,] cover, out int res) || cover == null)
            {
                Debug.Log($"[VegetationSpawner] Região {regionId} sem camada de vegetação (landcover_{regionId}.raw). " +
                          "Gere com Tools/GenerateRegionVegetation.py.");
                return;
            }

            TerrainData data = terrain.terrainData;
            Vector3 size = data.size;
            float areaKm2 = (size.x * size.z) / 1_000_000f;
            float cellAreaKm2 = areaKm2 / (res * res);

            // 1) Pré-cálculo da contagem desejada por célula + total (para aplicar o teto).
            var random = new System.Random(seed);
            float totalWanted = 0f;
            var cellCounts = new float[res, res];
            for (int gy = 0; gy < res; gy++)
            {
                for (int gx = 0; gx < res; gx++)
                {
                    float d = DensityForClass(cover[gy, gx]);
                    if (d <= 0f) continue;
                    float n = d * cellAreaKm2;
                    cellCounts[gy, gx] = n;
                    totalWanted += n;
                }
            }

            if (totalWanted <= 0.5f)
            {
                Debug.Log($"[VegetationSpawner] Região {regionId}: cobertura sem classes vegetadas relevantes.");
                return;
            }

            float scale = totalWanted > MaxInstances ? MaxInstances / totalWanted : 1f;

            // 2) Protótipos.
            EnsurePrototypes(data);
            bool boreal = Mathf.Abs(centerLat) >= 50f;
            bool temperate = Mathf.Abs(centerLat) >= 35f && Mathf.Abs(centerLat) < 50f;
            bool tropical = Mathf.Abs(centerLat) < 23.5f;

            // 3) Espalhamento.
            var instances = new List<TreeInstance>(Mathf.Min(MaxInstances, Mathf.CeilToInt(totalWanted)));
            for (int gy = 0; gy < res; gy++)
            {
                for (int gx = 0; gx < res; gx++)
                {
                    float want = cellCounts[gy, gx] * scale;
                    if (want <= 0f) continue;
                    int count = Mathf.FloorToInt(want);
                    if (random.NextDouble() < (want - count)) count++;
                    if (count <= 0) continue;

                    byte cls = cover[gy, gx];
                    for (int i = 0; i < count; i++)
                    {
                        // Posição normalizada (0..1). Linha 0 do grid = norte (maxLat);
                        // no terreno o norte é +Z, então invertemos Z.
                        float nx = (gx + (float)random.NextDouble()) / res;
                        float nz = 1f - (gy + (float)random.NextDouble()) / res;
                        nx = Mathf.Clamp01(nx);
                        nz = Mathf.Clamp01(nz);

                        // Rejeição em declives acentuados (parcial, para não zerar encostas).
                        float steep = data.GetSteepness(nx, nz);
                        if (steep > MaxSteepnessDeg && random.NextDouble() < 0.6) continue;

                        int proto = ChoosePrototype(cls, boreal, temperate, tropical, random);
                        float w = 0.7f + (float)random.NextDouble() * 0.7f;  // 0.7 .. 1.4
                        float h = 0.75f + (float)random.NextDouble() * 0.75f; // 0.75 .. 1.5
                        float g = 0.82f + (float)random.NextDouble() * 0.18f; // variação de verde

                        instances.Add(new TreeInstance
                        {
                            position = new Vector3(nx, 0f, nz),
                            prototypeIndex = proto,
                            widthScale = w,
                            heightScale = h,
                            rotation = (float)(random.NextDouble() * Mathf.PI * 2.0),
                            color = new Color(g * 0.9f, g, g * 0.75f, 1f),
                            lightmapColor = Color.white
                        });
                    }
                }
            }

            // 4) Aplicar (snapToHeightmap assenta o Y de cada árvore no relevo).
            data.SetTreeInstances(instances.ToArray(), true);

            // Distâncias de render: o padrão (treeDistance 5000m) sumia com a floresta
            // em terrenos de dezenas de km. Cobrir todo o setor ativo.
            terrain.drawTreesAndFoliage = true;
            terrain.treeDistance = Mathf.Max(10000f, size.x);
            terrain.treeBillboardDistance = 200f;
            terrain.treeCrossFadeLength = 40f;
            terrain.treeMaximumFullLODCount = 400;
            terrain.Flush();

            Debug.Log($"[VegetationSpawner] Região {regionId}: {instances.Count} árvores plantadas a partir do " +
                      $"ESA WorldCover (grade {res}x{res}, área {areaKm2:F0} km², fator densidade {scale:F2}).");
        }

        private static float DensityForClass(byte cls)
        {
            switch (cls)
            {
                case 10: return DensTree;
                case 95: return DensMangrove;
                case 90: return DensWetland;
                case 20: return DensShrub;
                case 30: return DensGrass;
                default: return 0f; // 40 lavoura, 50 urbano, 60 solo, 70 neve, 80 água, 0 sem dado
            }
        }

        private static int ChoosePrototype(byte cls, bool boreal, bool temperate, bool tropical, System.Random rnd)
        {
            if (cls == 20 || cls == 30) // arbusto / campo
            {
                if (tropical && rnd.NextDouble() < 0.25) return ProtoAcacia;
                return ProtoShrub;
            }
            if (cls == 95) return ProtoBroadleaf; // mangue
            if (cls == 90) return rnd.NextDouble() < 0.5 ? ProtoShrub : ProtoBroadleaf; // pântano

            // cls == 10 (mata) → bioma por latitude.
            if (boreal) return ProtoConifer;
            if (temperate) return rnd.NextDouble() < 0.5 ? ProtoConifer : ProtoBroadleaf;
            if (tropical && rnd.NextDouble() < 0.18) return ProtoAcacia;
            return ProtoBroadleaf;
        }

        #region Protótipos procedurais

        private static void EnsurePrototypes(TerrainData data)
        {
            if (prototypeHolder != null)
            {
                Object.Destroy(prototypeHolder);
            }
            prototypeHolder = new GameObject("VegetationPrototypes")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            prototypeHolder.SetActive(false);

            Material trunk = MakeMat(new Color(0.33f, 0.24f, 0.15f));
            Material leafDark = MakeMat(new Color(0.16f, 0.34f, 0.13f));
            Material leafPine = MakeMat(new Color(0.12f, 0.30f, 0.18f));
            Material leafDry = MakeMat(new Color(0.42f, 0.47f, 0.20f));
            Material leafShrub = MakeMat(new Color(0.24f, 0.40f, 0.18f));

            var protos = new TreePrototype[4];
            protos[ProtoBroadleaf] = new TreePrototype { prefab = BuildBroadleaf(trunk, leafDark) };
            protos[ProtoConifer]   = new TreePrototype { prefab = BuildConifer(trunk, leafPine) };
            protos[ProtoAcacia]    = new TreePrototype { prefab = BuildAcacia(trunk, leafDry) };
            protos[ProtoShrub]     = new TreePrototype { prefab = BuildShrub(leafShrub) };
            data.treePrototypes = protos;
            data.RefreshPrototypes();
        }

        private static Material MakeMat(Color c)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(s) { enableInstancing = true };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
            return m;
        }

        // Cada árvore é UM mesh com 2 submeshes (tronco, folhagem) e 2 materiais,
        // num único MeshRenderer — o formato que o Terrain renderiza como árvore.
        private struct Part { public Mesh mesh; public Matrix4x4 m; public int mat; }

        private static GameObject BuildBroadleaf(Material trunk, Material leaf)
        {
            var parts = new List<Part>
            {
                Cyl(0f, 2.0f, 0f, 0.5f, 4.0f),          // tronco h=4 r=0.25
                Sph(0f, 5.2f, 0f, 3.2f, 2.6f),          // copa
                Sph(1.1f, 4.4f, 0.4f, 2.0f, 1.8f),
            };
            return Combine("Tree_Broadleaf", parts, trunk, leaf);
        }

        private static GameObject BuildConifer(Material trunk, Material leaf)
        {
            var parts = new List<Part>
            {
                Cyl(0f, 2.2f, 0f, 0.4f, 4.4f),
                Sph(0f, 4.0f, 0f, 2.6f, 1.6f),
                Sph(0f, 5.6f, 0f, 1.9f, 1.5f),
                Sph(0f, 7.0f, 0f, 1.1f, 1.4f),
            };
            return Combine("Tree_Conifer", parts, trunk, leaf);
        }

        private static GameObject BuildAcacia(Material trunk, Material leaf)
        {
            var parts = new List<Part>
            {
                Cyl(0f, 2.4f, 0f, 0.5f, 4.8f),
                Sph(0f, 5.4f, 0f, 6.0f, 1.5f),          // copa larga e achatada
            };
            return Combine("Tree_Acacia", parts, trunk, leaf);
        }

        private static GameObject BuildShrub(Material leaf)
        {
            var parts = new List<Part>
            {
                Sph(0f, 0.7f, 0f, 1.6f, 1.1f),
                Sph(0.5f, 0.6f, 0.3f, 1.1f, 0.9f),
            };
            // Arbusto só tem folhagem; usamos o mesmo material nos dois submeshes.
            return Combine("Shrub", parts, leaf, leaf);
        }

        private static Part Cyl(float x, float y, float z, float diameter, float height)
        {
            // Cilindro primitivo: altura 2 (y -1..1), diâmetro 1. Escala p/ dimensões reais.
            return new Part
            {
                mesh = GetPrimitiveMesh(PrimitiveType.Cylinder),
                m = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(diameter, height * 0.5f, diameter)),
                mat = 0
            };
        }

        private static Part Sph(float x, float y, float z, float diameter, float heightScale)
        {
            // Esfera primitiva: diâmetro 1. heightScale achata/alonga verticalmente.
            return new Part
            {
                mesh = GetPrimitiveMesh(PrimitiveType.Sphere),
                m = Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.identity, new Vector3(diameter, diameter * heightScale, diameter)),
                mat = 1
            };
        }

        private static GameObject Combine(string name, List<Part> parts, Material mat0, Material mat1)
        {
            // Combina as partes de cada material num mesh, depois junta os dois como
            // submeshes separados (mergeSubMeshes:false) p/ manter 2 materiais.
            Mesh m0 = CombineByMat(parts, 0);
            Mesh m1 = CombineByMat(parts, 1);

            var finalCombine = new List<CombineInstance>();
            if (m0 != null) finalCombine.Add(new CombineInstance { mesh = m0, transform = Matrix4x4.identity });
            if (m1 != null) finalCombine.Add(new CombineInstance { mesh = m1, transform = Matrix4x4.identity });

            var finalMesh = new Mesh { name = name + "_Mesh" };
            finalMesh.CombineMeshes(finalCombine.ToArray(), false, false);
            finalMesh.RecalculateBounds();
            finalMesh.RecalculateNormals();

            var go = new GameObject(name);
            go.transform.SetParent(prototypeHolder.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = finalMesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mats = new List<Material>();
            if (m0 != null) mats.Add(mat0);
            if (m1 != null) mats.Add(mat1);
            mr.sharedMaterials = mats.ToArray();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        private static Mesh CombineByMat(List<Part> parts, int matIndex)
        {
            var list = new List<CombineInstance>();
            foreach (var p in parts)
            {
                if (p.mat != matIndex || p.mesh == null) continue;
                list.Add(new CombineInstance { mesh = p.mesh, transform = p.m, subMeshIndex = 0 });
            }
            if (list.Count == 0) return null;
            var mesh = new Mesh();
            mesh.CombineMeshes(list.ToArray(), true, true);
            return mesh;
        }

        private static Mesh cylMesh, sphMesh;

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (type == PrimitiveType.Cylinder)
            {
                if (cylMesh == null) cylMesh = ExtractPrimitiveMesh(PrimitiveType.Cylinder);
                return cylMesh;
            }
            if (sphMesh == null) sphMesh = ExtractPrimitiveMesh(PrimitiveType.Sphere);
            return sphMesh;
        }

        private static Mesh ExtractPrimitiveMesh(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            Object.Destroy(go);
            return mesh;
        }

        #endregion
    }
}
