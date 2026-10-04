using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Gerador procedural avanÃƒÂ§ado de estradas fotorrealistas conformatÃƒÂ³rias ao terreno.
    /// Elimina completamente qualquer vÃƒÂ£o/espaÃƒÂ§o vazio entre a pista e o solo atravÃƒÂ©s de
    /// taludes de aterro integrados (saias de ancoragem que penetram suavemente no terreno).
    /// Suporta 4 tipos de superfÃƒÂ­cie (Asfalto, Concreto, Terra, Britas) e 4 configuraÃƒÂ§ÃƒÂµes de faixas (2, 4, 6 e 8 faixas),
    /// com acostamentos, sinalizaÃƒÂ§ÃƒÂ£o viÃƒÂ¡ria completa (faixas contÃƒÂ­nuas, seccionadas/tracejadas) e barreiras New Jersey.
    /// </summary>
    public static class RoadMeshBuilder
    {
        #region Cache de Texturas e Materiais

        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private static Texture2D dashedLineTexture;
        private static Texture2D tireRutTexture;

        public static void ClearCache()
        {
            textureCache.Clear();
            materialCache.Clear();
        }

        private static Texture2D LoadTexture(string relativeSubPath, string fileName)
        {
            string key = $"{relativeSubPath}/{fileName}";
            if (textureCache.TryGetValue(key, out var cached) && cached != null) return cached;

#if UNITY_EDITOR
            string assetPath = $"Assets/_Project/Textures/PBR/{relativeSubPath}/{fileName}.png";
            var edTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (edTex == null)
            {
                assetPath = $"Assets/_Project/Textures/PBR/{relativeSubPath}/{fileName}.tga";
                edTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            if (edTex != null)
            {
                textureCache[key] = edTex;
                return edTex;
            }
#endif

            // Em builds, tentar carregar de Resources (texturas devem estar em Assets/Resources/PBR/...)
            string resourcesPath = $"PBR/{relativeSubPath}/{fileName}";
            var resTex = Resources.Load<Texture2D>(resourcesPath);
            if (resTex != null)
            {
                textureCache[key] = resTex;
                return resTex;
            }

            // Fallback: StreamingAssets (texturas devem estar copiadas para StreamingAssets/PBR/...)
            string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, "PBR", relativeSubPath, $"{fileName}.png");
            if (!System.IO.File.Exists(streamingPath))
            {
                streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, "PBR", relativeSubPath, $"{fileName}.tga");
            }

            if (System.IO.File.Exists(streamingPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(streamingPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Repeat;
                        tex.filterMode = FilterMode.Trilinear;
                        textureCache[key] = tex;
                        return tex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RoadMeshBuilder] Falha ao carregar textura de StreamingAssets {streamingPath}: {ex.Message}");
                }
            }

            // Último recurso: tentar caminho relativo ao DataPath (só funciona no Editor)
            string diskPath = System.IO.Path.Combine(Application.dataPath, "_Project", "Textures", "PBR", relativeSubPath, $"{fileName}.png");
            if (!System.IO.File.Exists(diskPath))
            {
                diskPath = System.IO.Path.Combine(Application.dataPath, "_Project", "Textures", "PBR", relativeSubPath, $"{fileName}.tga");
            }

            if (System.IO.File.Exists(diskPath))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                    if (tex.LoadImage(bytes))
                    {
                        tex.wrapMode = TextureWrapMode.Repeat;
                        tex.filterMode = FilterMode.Trilinear;
                        textureCache[key] = tex;
                        return tex;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RoadMeshBuilder] Falha ao carregar textura {diskPath}: {ex.Message}");
                }
            }

#if UNITY_EDITOR
            // No Editor, log um aviso claro se a textura não foi encontrada
            Debug.LogWarning($"[RoadMeshBuilder] Textura não encontrada: {relativeSubPath}/{fileName}. Coloque em Resources/PBR/ ou StreamingAssets/PBR/ para funcionar em builds.");
#endif
            return null;
        }

        /// <summary>
        /// Cria um material no pipeline real do projeto. O pacote HDRP foi removido
        /// (ver RegionalSandboxManager.Terrain.EnsureTerrainMaterial), entÃƒÂ£o este projeto
        /// roda no Built-in: as propriedades HDRP (_BaseColorMap, _MaskMap, _Smoothness,
        /// _AlphaCutoffEnable) nÃƒÂ£o existem e nunca eram aplicadas.
        /// </summary>
        private static Material CreatePipelineMaterial(string name, Color baseColor, float glossiness = 0.2f, float metallic = 0.0f)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name };

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", glossiness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            return mat;
        }

        public static Material GetRoadSurfaceMaterial(RoadSurfaceType surface)
        {
            string key = $"Road_{surface}";
            if (materialCache.TryGetValue(key, out var mat) && mat != null) return mat;

            Color tint;
            float gloss;
            string subDir;

            switch (surface)
            {
                case RoadSurfaceType.Concrete:
                    tint = new Color(0.72f, 0.73f, 0.74f);
                    gloss = 0.32f;
                    subDir = "Infrastructure/Concrete";
                    break;
                case RoadSurfaceType.Dirt:
                    tint = new Color(0.48f, 0.36f, 0.24f);
                    gloss = 0.06f;
                    subDir = "Ground/Soil";
                    break;
                case RoadSurfaceType.Gravel:
                    tint = new Color(0.53f, 0.52f, 0.50f);
                    gloss = 0.12f;
                    subDir = "Ground/Gravel";
                    break;
                case RoadSurfaceType.Asphalt:
                default:
                    tint = new Color(0.26f, 0.26f, 0.27f);
                    gloss = 0.22f;
                    subDir = "Infrastructure/Asphalt";
                    break;
            }

            mat = CreatePipelineMaterial(key, tint, gloss, 0.0f);

            var albedo = LoadTexture(subDir, "Albedo");
            var normal = LoadTexture(subDir, "Normal");

            if (albedo != null && mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", albedo);
            }
            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.0f);
                mat.EnableKeyword("_NORMALMAP");
            }

            materialCache[key] = mat;
            return mat;
        }

        public static Material GetShoulderMaterial(RoadSurfaceType surface)
        {
            string key = $"Shoulder_{surface}";
            if (materialCache.TryGetValue(key, out var mat) && mat != null) return mat;

            Color tint = (surface == RoadSurfaceType.Dirt) ? new Color(0.42f, 0.32f, 0.20f)
                       : (surface == RoadSurfaceType.Gravel) ? new Color(0.46f, 0.45f, 0.43f)
                       : (surface == RoadSurfaceType.Concrete) ? new Color(0.60f, 0.61f, 0.62f)
                       : new Color(0.33f, 0.32f, 0.30f); // acostamento de asfalto britado

            mat = CreatePipelineMaterial(key, tint, 0.10f, 0.0f);
            var gravelTex = LoadTexture("Ground/Gravel", "Albedo") ?? LoadTexture("Ground/Soil", "Albedo");
            if (gravelTex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", gravelTex);

            materialCache[key] = mat;
            return mat;
        }

        public static Material GetEmbankmentMaterial()
        {
            const string key = "Road_Embankment_Talude";
            if (materialCache.TryGetValue(key, out var mat) && mat != null) return mat;

            mat = CreatePipelineMaterial(key, new Color(0.38f, 0.30f, 0.20f), 0.05f, 0.0f);
            var soilTex = LoadTexture("Ground/Soil", "Albedo");
            if (soilTex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", soilTex);

            materialCache[key] = mat;
            return mat;
        }

        public static Material GetMarkingMaterial(Color color, bool isDashed = false)
        {
            string key = $"Marking_{color.GetHashCode()}_{isDashed}";
            if (materialCache.TryGetValue(key, out var mat) && mat != null) return mat;

            Shader s = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");

            mat = new Material(s) { name = key };

            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.35f);

            if (isDashed)
            {
                var dashTex = GetDashedLineTexture();
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", dashTex);

                // Cutout no Standard. A propriedade correta ÃƒÂ© _AlphaCutoff (nÃƒÂ£o _Cutoff,
                // que nÃƒÂ£o existe no Standard) e a geometria alpha-testada pertence ÃƒÂ 
                // fila opaque: em 3000 ela ordenava como transparente e podia ocluir
                // outros transparentes.
                if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 1f);
                if (mat.HasProperty("_AlphaCutoff")) mat.SetFloat("_AlphaCutoff", 0.5f);
                if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = 2000;
            }

            materialCache[key] = mat;
            return mat;
        }

        public static Material GetTireRutMaterial()
        {
            const string key = "Road_TireRut_Dirt";
            if (materialCache.TryGetValue(key, out var mat) && mat != null) return mat;

            Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            mat = new Material(s) { name = key };
            Color c = new Color(0.28f, 0.20f, 0.12f, 0.65f);
            mat.color = c;
            if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 2f); // Fade
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 2990;

            var rutTex = GetTireRutTexture();
            mat.mainTexture = rutTex;
            materialCache[key] = mat;
            return mat;
        }

        public static Material GetConcreteBarrierMaterial()
        {
            const string key = "Road_Barrier_NewJersey";
            if (materialCache.TryGetValue(key, out var mat) && mat != null) return mat;

            mat = CreatePipelineMaterial(key, new Color(0.76f, 0.76f, 0.77f), 0.25f, 0.0f);
            var conTex = LoadTexture("Infrastructure/Concrete", "Albedo");
            if (conTex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", conTex);
            materialCache[key] = mat;
            return mat;
        }

        private static Texture2D GetDashedLineTexture()
        {
            if (dashedLineTexture != null) return dashedLineTexture;

            // Textura 32x64: metade superior branca sÃƒÂ³lida, metade inferior transparente
            dashedLineTexture = new Texture2D(32, 64, TextureFormat.RGBA32, true);
            dashedLineTexture.name = "Tex_Road_DashedLine";
            dashedLineTexture.wrapMode = TextureWrapMode.Repeat;
            dashedLineTexture.filterMode = FilterMode.Bilinear;

            var colors = new Color[32 * 64];
            for (int y = 0; y < 64; y++)
            {
                bool isPaint = (y < 32); // 50% de ciclo de pintura (ex: 4m tinta, 4m vÃƒÂ£o)
                for (int x = 0; x < 32; x++)
                {
                    // SuavizaÃƒÂ§ÃƒÂ£o lateral das bordas da linha
                    float edgeDist = Mathf.Min(x, 31 - x) / 3.0f;
                    float alpha = isPaint ? Mathf.Clamp01(edgeDist) : 0f;
                    colors[y * 32 + x] = new Color(0.98f, 0.98f, 0.95f, alpha);
                }
            }
            dashedLineTexture.SetPixels(colors);
            dashedLineTexture.Apply(true, true);
            return dashedLineTexture;
        }

        private static Texture2D GetTireRutTexture()
        {
            if (tireRutTexture != null) return tireRutTexture;

            tireRutTexture = new Texture2D(32, 32, TextureFormat.RGBA32, true);
            tireRutTexture.name = "Tex_Road_TireRut";
            tireRutTexture.wrapMode = TextureWrapMode.Repeat;
            tireRutTexture.filterMode = FilterMode.Bilinear;

            var colors = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float centerNorm = Mathf.Abs((x / 31.0f) - 0.5f) * 2f;
                    float alpha = Mathf.Clamp01(1f - centerNorm * centerNorm) * 0.7f;
                    colors[y * 32 + x] = new Color(0.24f, 0.18f, 0.12f, alpha);
                }
            }
            tireRutTexture.SetPixels(colors);
            tireRutTexture.Apply(true, true);
            return tireRutTexture;
        }

        #endregion

        #region Construtor Principal de Geometria de Estrada

        /// <summary>
        /// ConstrÃƒÂ³i uma estrada contÃƒÂ­nua com perfil completo, acostamentos, taludes de ancoragem no solo
        /// e sinalizaÃƒÂ§ÃƒÂ£o viÃƒÂ¡ria conforme a quantidade de faixas (2, 4, 6, 8) e tipo de pavimento.
        /// </summary>
        public static GameObject BuildRoad(
            Transform parent,
            Vector3[] centerSpine,
            RoadProfileConfig config,
            Func<Vector3, float> getTerrainHeight,
            string roadName)
        {
            if (centerSpine == null || centerSpine.Length < 2) return null;

            int m = centerSpine.Length;

            // 1. Suavizar e garantir alturas precisas na espinha dorsal
            var center = new Vector3[m];
            var rights = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                Vector3 p = centerSpine[i];
                float th = getTerrainHeight != null ? getTerrainHeight(p) : p.y;
                p.y = th + 0.12f; // Leito da pista ligeiramente elevado (12 cm) para drenagem
                center[i] = p;
            }

            // SuavizaÃƒÂ§ÃƒÂ£o das elevaÃƒÂ§ÃƒÂµes verticais para evitar descontinuidades abruptas
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 1; i < m - 1; i++)
                {
                    center[i].y = (center[i - 1].y + center[i].y * 2f + center[i + 1].y) * 0.25f;
                }
            }

            // Calcular vetores laterais ortogonais (right)
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

            var roadRoot = new GameObject(roadName);
            roadRoot.transform.SetParent(parent);

            float halfRoad = config.TotalRoadwayWidth * 0.5f;
            float shoulderW = config.shoulderWidth;
            float taludeW = config.embankmentWidth;

            // 2. Construir Malha Unificada com Pista, Acostamentos e Taludes de Ancoragem
            BuildUnifiedRoadBody(roadRoot.transform, center, rights, config, halfRoad, shoulderW, taludeW, getTerrainHeight);

            // 3. Adicionar SinalizaÃƒÂ§ÃƒÂ£o ViÃƒÂ¡ria (Asfalto / Concreto) ou Trilhas de Rodagem (Terra / Brita)
            if (config.surfaceType == RoadSurfaceType.Asphalt || config.surfaceType == RoadSurfaceType.Concrete)
            {
                BuildPavementMarkings(roadRoot.transform, center, rights, config, halfRoad);
            }
            else
            {
                BuildUnpavedTireRuts(roadRoot.transform, center, rights, config);
            }

            // 4. Adicionar Barreira Central New Jersey para 6 e 8 faixas (e opcional 4 faixas)
            if ((int)config.laneCount >= 6 && config.medianWidth >= 1.5f)
            {
                BuildCenterBarrier(roadRoot.transform, center, rights, config.medianWidth);
            }

            return roadRoot;
        }

        /// <summary>
        /// ConstrÃƒÂ³i o corpo contÃƒÂ­nuo da estrada composto por 7 colunas de vÃƒÂ©rtices transversais:
        /// [0] Talude Esquerdo (ancorado -0.30m dentro do terreno) -> ZERO VÃƒÆ’OS
        /// [1] Acostamento Esquerdo
        /// [2] Borda Esquerda da Pista
        /// [3] Centro da Pista (abaulado +0.06m)
        /// [4] Borda Direita da Pista
        /// [5] Acostamento Direito
        /// [6] Talude Direito (ancorado -0.30m dentro do terreno) -> ZERO VÃƒÆ’OS
        /// </summary>
        private static void BuildUnifiedRoadBody(
            Transform parent,
            Vector3[] center,
            Vector3[] rights,
            RoadProfileConfig config,
            float halfRoad,
            float shoulderW,
            float taludeW,
            Func<Vector3, float> getTerrainHeight)
        {
            int m = center.Length;
            const int cols = 7; // 7 colunas longitudinais

            var verts = new Vector3[m * cols];
            var uvs = new Vector2[m * cols];
            var normals = new Vector3[m * cols];

            float[] lateralOffsets = new float[cols]
            {
                -(halfRoad + shoulderW + taludeW), // 0: PÃƒÂ© do talude esquerdo
                -(halfRoad + shoulderW),           // 1: Borda externa acostamento esquerdo
                -halfRoad,                          // 2: Borda esquerda da pista
                0f,                                 // 3: Eixo central
                halfRoad,                           // 4: Borda direita da pista
                halfRoad + shoulderW,              // 5: Borda externa acostamento direito
                halfRoad + shoulderW + taludeW     // 6: PÃƒÂ© do talude direito
            };

            float uRun = 0f;
            float totalRoadWidth = halfRoad * 2f;

            for (int i = 0; i < m; i++)
            {
                if (i > 0) uRun += Vector3.Distance(center[i], center[i - 1]);

                Vector3 c = center[i];
                Vector3 r = rights[i];

                for (int col = 0; col < cols; col++)
                {
                    float off = lateralOffsets[col];
                    Vector3 worldPos = c + r * off;

                    float y;
                    if (col == 0 || col == 6)
                    {
                        // TALUDE DE ANCORAGEM:
                        // Amostra a altura real do terreno no pÃƒÂ© do talude e afunda 30 cm ABAIXO DO SOLO!
                        // Isso garante vedaÃƒÂ§ÃƒÂ£o hermÃƒÂ©tica 100% ÃƒÂ  prova de vÃƒÂ£os em qualquer topografia ou encosta.
                        float groundY = (getTerrainHeight != null) ? getTerrainHeight(worldPos) : (c.y - 0.5f);
                        y = groundY - 0.30f;
                    }
                    else if (col == 1 || col == 5)
                    {
                        // ACOSTAMENTO:
                        float groundY = (getTerrainHeight != null) ? getTerrainHeight(worldPos) : c.y;
                        y = Mathf.Max(groundY + 0.04f, c.y - 0.05f);
                    }
                    else if (col == 2 || col == 4)
                    {
                        // BORDAS DA PISTA
                        y = c.y + 0.02f;
                    }
                    else // col == 3 (Centro da pista)
                    {
                        // ABAULAMENTO CENTRAL (Crown) de drenagem pluvial suave
                        y = c.y + 0.06f;
                    }

                    int idx = i * cols + col;
                    verts[idx] = new Vector3(worldPos.x, y, worldPos.z);

                    // Mapeamento UV uniforme e suave
                    float u = (col == 0) ? -0.3f
                            : (col == 1) ? -0.1f
                            : (col == 2) ? 0f
                            : (col == 3) ? 0.5f
                            : (col == 4) ? 1.0f
                            : (col == 5) ? 1.1f
                            : 1.3f;

                    uvs[idx] = new Vector2(u * (totalRoadWidth / 7f), uRun / 10f);
                }
            }

            // GeraÃƒÂ§ÃƒÂ£o de TriÃƒÂ¢ngulos separados por Submeshes:
            // Submesh 0: Pista de rolamento (Colunas 2 a 4)
            // Submesh 1: Acostamentos laterais (Colunas 1->2 e 4->5)
            // Submesh 2: Taludes de ancoragem no solo (Colunas 0->1 e 5->6)
            var roadTris = new List<int>((m - 1) * 2 * 6);
            var shoulderTris = new List<int>((m - 1) * 2 * 6);
            var taludeTris = new List<int>((m - 1) * 2 * 6);

            for (int i = 0; i < m - 1; i++)
            {
                int rA = i * cols;
                int rB = (i + 1) * cols;

                // Talude Esquerdo (0 -> 1)
                AddQuad(taludeTris, rA + 0, rA + 1, rB + 0, rB + 1);

                // Acostamento Esquerdo (1 -> 2)
                AddQuad(shoulderTris, rA + 1, rA + 2, rB + 1, rB + 2);

                // Pista Esquerda (2 -> 3) e Pista Direita (3 -> 4)
                AddQuad(roadTris, rA + 2, rA + 3, rB + 2, rB + 3);
                AddQuad(roadTris, rA + 3, rA + 4, rB + 3, rB + 4);

                // Acostamento Direito (4 -> 5)
                AddQuad(shoulderTris, rA + 4, rA + 5, rB + 4, rB + 5);

                // Talude Direito (5 -> 6)
                AddQuad(taludeTris, rA + 5, rA + 6, rB + 5, rB + 6);
            }

            var mesh = new Mesh { name = "Road_Body_Mesh" };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(roadTris, 0);
            mesh.SetTriangles(shoulderTris, 1);
            mesh.SetTriangles(taludeTris, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var bodyObj = new GameObject("Pista_Principal");
            bodyObj.transform.SetParent(parent);
            bodyObj.AddComponent<MeshFilter>().sharedMesh = mesh;

            var rend = bodyObj.AddComponent<MeshRenderer>();
            rend.sharedMaterials = new Material[]
            {
                GetRoadSurfaceMaterial(config.surfaceType),
                GetShoulderMaterial(config.surfaceType),
                GetEmbankmentMaterial()
            };

            // Adicionar colisor fÃƒÂ­sico na pista para suporte a veÃƒÂ­culos e pedestre
            var colMesh = new Mesh { name = "Road_Collider_Mesh" };
            if (verts.Length > 65000) colMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            colMesh.vertices = verts;
            colMesh.subMeshCount = 1;
            // O colisor cobre a pista e os acostamentos (sem os taludes profundos)
            var colTris = new List<int>(roadTris.Count + shoulderTris.Count);
            colTris.AddRange(shoulderTris);
            colTris.AddRange(roadTris);
            colMesh.SetTriangles(colTris, 0);
            colMesh.RecalculateBounds();
            // Sem RecalculateTangents aqui de propósito: o mesh de colisão não tem UVs e
            // não é renderizado — só o corpo visível precisa de tangentes para o normal map.

            var collider = bodyObj.AddComponent<MeshCollider>();
            collider.sharedMesh = colMesh;
        }

        private static void AddQuad(List<int> list, int a, int b, int c, int d)
        {
            list.Add(a); list.Add(c); list.Add(b);
            list.Add(b); list.Add(c); list.Add(d);
        }

        #endregion

        #region SinalizaÃƒÂ§ÃƒÂ£o ViÃƒÂ¡ria e Faixas de Rolamento (2, 4, 6 e 8 faixas)

        /// <summary>
        /// Gera a sinalizaÃƒÂ§ÃƒÂ£o horizontal de faixas para Asfalto e Concreto:
        /// - 2 Faixas: Linha amarela central (eixo) + 2 faixas brancas de bordo
        /// - 4 Faixas: Eixo duplo amarelo/canteiro + linhas brancas tracejadas dividindo as 2 faixas em cada sentido + bordos
        /// - 6 Faixas: Eixo central largo + 2 linhas tracejadas em cada sentido (3 faixas cada) + bordos
        /// - 8 Faixas: Eixo central largo + 3 linhas tracejadas em cada sentido (4 faixas cada) + bordos
        /// Todas as faixas ficam coladas ÃƒÂ  pista (1.2 cm de espessura de tinta termoplÃƒÂ¡stica) sem flutuaÃƒÂ§ÃƒÂ£o!
        /// </summary>
        private static void BuildPavementMarkings(
            Transform parent,
            Vector3[] center,
            Vector3[] rights,
            RoadProfileConfig config,
            float halfRoad)
        {
            var markingsRoot = new GameObject("Sinalizacao_Faixas");
            markingsRoot.transform.SetParent(parent);

            var whiteSolid = GetMarkingMaterial(new Color(0.96f, 0.96f, 0.94f), isDashed: false);
            var whiteDashed = GetMarkingMaterial(new Color(0.96f, 0.96f, 0.94f), isDashed: true);
            var yellowSolid = GetMarkingMaterial(new Color(0.98f, 0.80f, 0.08f), isDashed: false);

            float edgeOffset = halfRoad - 0.45f;

            // A faixa precisa ficar ACIMA da superfÃƒÂ­cie, nunca abaixo. O corpo da pista
            // usa c.y+0.02 nas bordas e c.y+0.06 no abaulamento central (ver
            // BuildUnifiedRoadBody), interpolando entre os dois. Com o offset antigo de
            // 12 mm as faixas ficavam 8Ã¢â‚¬â€œ52 mm enterradas dentro da malha opaca e nunca
            // apareciam Ã¢â‚¬â€ o mesmo valia para os sulcos de pista nÃƒÂ£o pavimentada.
            const float markingClearance = 0.07f;
            float yOffset = markingClearance;

            // Linhas de Bordo Laterais (Brancas contÃƒÂ­nuas)
            BuildStripRibbon(markingsRoot.transform, center, rights, 0.22f, -edgeOffset, yOffset, whiteSolid, "Bordo_Esq");
            BuildStripRibbon(markingsRoot.transform, center, rights, 0.22f, edgeOffset, yOffset, whiteSolid, "Bordo_Dir");

            int lanes = (int)config.laneCount;
            float laneW = config.laneWidth;
            float halfMedian = config.medianWidth * 0.5f;

            if (lanes == 2)
            {
                // Pista Simples: Linha Amarela Dupla Central (divisor de fluxos opostos)
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.16f, -0.20f, yOffset, yellowSolid, "Eixo_Amarelo_E");
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.16f, 0.20f, yOffset, yellowSolid, "Eixo_Amarelo_D");
            }
            else if (lanes == 4)
            {
                // Pista Dupla (2 faixas por sentido):
                // Linha dupla central amarela delimitando o canteiro
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, -halfMedian, yOffset, yellowSolid, "Centro_Esq");
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, halfMedian, yOffset, yellowSolid, "Centro_Dir");

                // Faixa seccionada tracejada branca entre Faixa 1 e Faixa 2
                float laneDivEsq = -(halfMedian + laneW);
                float laneDivDir = (halfMedian + laneW);
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, laneDivEsq, yOffset, whiteDashed, "Faixa_Tracejada_Esq", dashTiling: true);
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, laneDivDir, yOffset, whiteDashed, "Faixa_Tracejada_Dir", dashTiling: true);
            }
            else if (lanes == 6)
            {
                // Autoestrada (3 faixas por sentido):
                // Linhas amarelas centrais delimitando a barreira/canteiro
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.20f, -halfMedian, yOffset, yellowSolid, "Centro_Esq");
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.20f, halfMedian, yOffset, yellowSolid, "Centro_Dir");

                // 2 divisÃƒÂ³rias tracejadas no lado esquerdo
                for (int l = 1; l <= 2; l++)
                {
                    float off = -(halfMedian + l * laneW);
                    BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, off, yOffset, whiteDashed, $"Tracejada_Esq_{l}", dashTiling: true);
                }

                // 2 divisÃƒÂ³rias tracejadas no lado direito
                for (int l = 1; l <= 2; l++)
                {
                    float off = (halfMedian + l * laneW);
                    BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, off, yOffset, whiteDashed, $"Tracejada_Dir_{l}", dashTiling: true);
                }
            }
            else if (lanes == 8)
            {
                // Super Rodovia (4 faixas por sentido):
                // Linhas amarelas centrais
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.22f, -halfMedian, yOffset, yellowSolid, "Centro_Esq");
                BuildStripRibbon(markingsRoot.transform, center, rights, 0.22f, halfMedian, yOffset, yellowSolid, "Centro_Dir");

                // 3 divisÃƒÂ³rias tracejadas no lado esquerdo
                for (int l = 1; l <= 3; l++)
                {
                    float off = -(halfMedian + l * laneW);
                    BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, off, yOffset, whiteDashed, $"Tracejada_Esq_{l}", dashTiling: true);
                }

                // 3 divisÃƒÂ³rias tracejadas no lado direito
                for (int l = 1; l <= 3; l++)
                {
                    float off = (halfMedian + l * laneW);
                    BuildStripRibbon(markingsRoot.transform, center, rights, 0.18f, off, yOffset, whiteDashed, $"Tracejada_Dir_{l}", dashTiling: true);
                }
            }
        }

        /// <summary>
        /// Gera marcas sutis de trilhas de rodagem de pneus compactados em estradas de Terra ou Britas.
        /// </summary>
        private static void BuildUnpavedTireRuts(
            Transform parent,
            Vector3[] center,
            Vector3[] rights,
            RoadProfileConfig config)
        {
            var rutsRoot = new GameObject("Trilhas_Rodagem");
            rutsRoot.transform.SetParent(parent);

            var rutMat = GetTireRutMaterial();
            int lanes = (int)config.laneCount;
            float laneW = config.laneWidth;
            float halfRoad = config.TotalRoadwayWidth * 0.5f;
            float yOffset = 0.07f; // mesmo clearance de BuildPavementMarkings: 8 mm ficavam sob a pista

            // Para cada faixa, adiciona 2 trilhas de pneus (roda esquerda e roda direita: distÃƒÂ¢ncia de ~1.8m)
            int halfLanes = lanes / 2;
            for (int l = 0; l < halfLanes; l++)
            {
                // Lado esquerdo
                float laneCenterEsq = -(config.medianWidth * 0.5f + (l + 0.5f) * laneW);
                BuildStripRibbon(rutsRoot.transform, center, rights, 0.35f, laneCenterEsq - 0.9f, yOffset, rutMat, $"Rut_E_{l}_1");
                BuildStripRibbon(rutsRoot.transform, center, rights, 0.35f, laneCenterEsq + 0.9f, yOffset, rutMat, $"Rut_E_{l}_2");

                // Lado direito
                float laneCenterDir = (config.medianWidth * 0.5f + (l + 0.5f) * laneW);
                BuildStripRibbon(rutsRoot.transform, center, rights, 0.35f, laneCenterDir - 0.9f, yOffset, rutMat, $"Rut_D_{l}_1");
                BuildStripRibbon(rutsRoot.transform, center, rights, 0.35f, laneCenterDir + 0.9f, yOffset, rutMat, $"Rut_D_{l}_2");
            }
        }

        /// <summary>
        /// ConstrÃƒÂ³i uma barreira New Jersey de concreto contÃƒÂ­nua ao longo do canteiro central.
        /// </summary>
        private static void BuildCenterBarrier(
            Transform parent,
            Vector3[] center,
            Vector3[] rights,
            float medianWidth)
        {
            int m = center.Length;
            var barrierMat = GetConcreteBarrierMaterial();

            // Perfil transversal da barreira New Jersey (4 vÃƒÂ©rtices: base larga 0.6m, topo 0.3m, altura 0.85m)
            float halfB = 0.32f;
            float halfT = 0.16f;
            float h = 0.85f;

            var verts = new Vector3[m * 4];
            var uvs = new Vector2[m * 4];
            float uRun = 0f;

            for (int i = 0; i < m; i++)
            {
                if (i > 0) uRun += Vector3.Distance(center[i], center[i - 1]);
                Vector3 c = center[i];
                Vector3 r = rights[i];

                verts[i * 4 + 0] = c - r * halfB + Vector3.up * 0.05f;      // Base esquerda
                verts[i * 4 + 1] = c - r * halfT + Vector3.up * h;          // Topo esquerdo
                verts[i * 4 + 2] = c + r * halfT + Vector3.up * h;          // Topo direito
                verts[i * 4 + 3] = c + r * halfB + Vector3.up * 0.05f;      // Base direita

                uvs[i * 4 + 0] = new Vector2(0f, uRun * 0.5f);
                uvs[i * 4 + 1] = new Vector2(0.4f, uRun * 0.5f);
                uvs[i * 4 + 2] = new Vector2(0.6f, uRun * 0.5f);
                uvs[i * 4 + 3] = new Vector2(1f, uRun * 0.5f);
            }

            var tris = new List<int>((m - 1) * 3 * 6);
            for (int i = 0; i < m - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;

                // Lado esquerdo
                AddQuad(tris, a + 0, a + 1, b + 0, b + 1);
                // Topo
                AddQuad(tris, a + 1, a + 2, b + 1, b + 2);
                // Lado direito
                AddQuad(tris, a + 2, a + 3, b + 2, b + 3);
            }

            var mesh = new Mesh { name = "Barrier_NewJersey_Mesh" };
            if (verts.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var barrierObj = new GameObject("Barreira_NewJersey_Central");
            barrierObj.transform.SetParent(parent);
            barrierObj.AddComponent<MeshFilter>().sharedMesh = mesh;
            barrierObj.AddComponent<MeshRenderer>().sharedMaterial = barrierMat;
            barrierObj.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// ConstrÃƒÂ³i uma faixa paralela (fita) posicionada na superfÃƒÂ­cie da pista com largura e offset prÃƒÂ³prios.
        /// </summary>
        private static void BuildStripRibbon(
            Transform parent,
            Vector3[] center,
            Vector3[] rights,
            float width,
            float lateralOffset,
            float yOffset,
            Material mat,
            string name,
            bool dashTiling = false)
        {
            int m = center.Length;
            var verts = new Vector3[m * 2];
            var uvs = new Vector2[m * 2];
            float uRun = 0f;
            float halfW = width * 0.5f;

            for (int i = 0; i < m; i++)
            {
                if (i > 0) uRun += Vector3.Distance(center[i], center[i - 1]);
                Vector3 c = center[i] + rights[i] * lateralOffset + Vector3.up * yOffset;

                verts[i * 2 + 0] = c - rights[i] * halfW;
                verts[i * 2 + 1] = c + rights[i] * halfW;

                // Para linhas tracejadas, mapear o ciclo (4m tinta, 4m espaÃƒÂ§o = 8 metros por repetiÃƒÂ§ÃƒÂ£o)
                float v = dashTiling ? (uRun / 8.0f) : (uRun / 5.0f);
                uvs[i * 2 + 0] = new Vector2(0f, v);
                uvs[i * 2 + 1] = new Vector2(1f, v);
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
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        #endregion

        #region UtilitÃƒÂ¡rios de TraÃƒÂ§ado Suave e Vias Retas

        /// <summary>
        /// ConstrÃƒÂ³i uma rodovia reta ou transversal adaptativa entre dois pontos no espaÃƒÂ§o,
        /// subdividindo a cada 12-15 metros e amostrando a altura real do terreno em cada nÃƒÂ³.
        /// Substitui primitivos de caixas estÃƒÂ¡ticas por estradas conformatÃƒÂ³rias de verdade!
        /// </summary>
        public static GameObject BuildStraightRoad(
            Transform parent,
            Vector3 startPos,
            Vector3 endPos,
            RoadSurfaceType surface,
            RoadLaneCount lanes,
            Func<Vector3, float> getTerrainHeight,
            string roadName)
        {
            float totalDist = Vector3.Distance(new Vector3(startPos.x, 0f, startPos.z), new Vector3(endPos.x, 0f, endPos.z));
            int segments = Mathf.Clamp(Mathf.CeilToInt(totalDist / 14f), 2, 300);

            var spine = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(startPos, endPos, t);
                p.y = (getTerrainHeight != null) ? getTerrainHeight(p) : p.y;
                spine[i] = p;
            }

            var cfg = RoadProfileConfig.GetDefault(surface, lanes);
            return BuildRoad(parent, spine, cfg, getTerrainHeight, roadName);
        }

        /// <summary>
        /// Suaviza coordenadas XZ usando interpolaÃƒÂ§ÃƒÂ£o Catmull-Rom com passo fino (12 a 15m),
        /// garantindo que curvas acompanhem fielmente o relevo montanhoso e vales sem vÃƒÂ£os.
        /// </summary>
        public static List<Vector2> SmoothPolylineXZ(List<Vector2> points, float targetSpacing = 14f)
        {
            if (points == null || points.Count < 2) return points;
            if (points.Count == 2)
            {
                // Subdivide linha reta simples a cada ~14m
                float d = Vector2.Distance(points[0], points[1]);
                int sub = Mathf.Clamp(Mathf.RoundToInt(d / targetSpacing), 1, 100);
                var res = new List<Vector2>(sub + 1);
                for (int s = 0; s <= sub; s++)
                {
                    res.Add(Vector2.Lerp(points[0], points[1], s / (float)sub));
                }
                return res;
            }

            var outp = new List<Vector2>(points.Count * 6);
            outp.Add(points[0]);

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p0 = points[Mathf.Max(0, i - 1)];
                Vector2 p1 = points[i];
                Vector2 p2 = points[i + 1];
                Vector2 p3 = points[Mathf.Min(points.Count - 1, i + 2)];

                float segDist = Vector2.Distance(p1, p2);
                int sub = Mathf.Clamp(Mathf.RoundToInt(segDist / targetSpacing), 2, 40);

                for (int s = 1; s <= sub; s++)
                {
                    float t = s / (float)sub;
                    outp.Add(CatmullRom(p0, p1, p2, p3, t));
                }
            }

            return outp;
        }

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>
        /// ConstrÃƒÂ³i um trecho demonstrativo de estrada diretamente na frente do jogador
        /// para permitir inspeÃƒÂ§ÃƒÂ£o e testes imediatos no jogo.
        /// </summary>
        public static GameObject SpawnShowcaseRoad(
            Vector3 origin,
            Vector3 forward,
            RoadSurfaceType surface,
            RoadLaneCount lanes,
            Func<Vector3, float> getTerrainHeight)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 start = origin + forward * 8f;
            Vector3 end = start + forward * 320f;

            string name = $"Showcase_{surface}_{lanes}Faixas_{DateTime.Now.Ticks % 10000}";
            var go = BuildStraightRoad(null, start, end, surface, lanes, getTerrainHeight, name);
            Debug.Log($"[RoadMeshBuilder] Trecho de demonstraÃƒÂ§ÃƒÂ£o gerado com sucesso: '{name}' (SuperfÃƒÂ­cie: {surface}, Faixas: {(int)lanes}).");
            return go;
        }

        #endregion
    }
}
