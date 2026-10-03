using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Campo de grama DENSO que acompanha o jogador, desenhado via <see cref="Graphics.DrawMeshInstanced"/>
    /// apenas num raio ao redor do jogador. Contagem fixa (independente do tamanho do mundo) = denso e leve.
    ///
    /// Estabilidade em escala 1:1: a grade é definida por ÍNDICES INTEIROS de célula e o jitter vem de um
    /// HASH INTEIRO determinístico — assim cada célula do mundo sempre gera o mesmo tufo, sem saltar com a
    /// precisão de float quando o jogador está a milhares de metros da origem.
    /// </summary>
    public class PlayerFollowGrass : MonoBehaviour
    {
        public Terrain terrain;

        [Header("Campo")]
        public float radius = 95f;           // raio coberto ao redor do jogador
        public float spacing = 0.9f;         // distância média entre tufos (menor = mais denso)
        public float rebuildThreshold = 8f;  // recentraliza a grade quando o jogador anda isto
        public float maxSlopeDegrees = 36f;  // acima disso não nasce grama
        public float minGroundY = 1.0f;      // abaixo disso (água) não nasce grama

        [Header("Tufo")]
        public Vector2 widthRange = new Vector2(0.8f, 1.5f);
        public Vector2 heightRange = new Vector2(0.4f, 0.8f);
        public Color healthyColor = new Color(0.40f, 0.60f, 0.26f);

        private Mesh mesh;
        private Material material;
        private Transform player;
        private Vector3 lastBuildCenter = new Vector3(1e9f, 1e9f, 1e9f);
        private bool forceRebuild;

        private readonly List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
        private const int BatchMax = 1023;

        public static PlayerFollowGrass Create(Terrain terrain)
        {
            var go = new GameObject("PlayerGrassField");
            var g = go.AddComponent<PlayerFollowGrass>();
            g.terrain = terrain;
            return g;
        }

        private void Start()
        {
            mesh = BuildCrossQuadMesh();
            material = BuildGrassMaterial();
        }

        private void OnEnable() { FloatingOrigin.OnOriginRebased += OnOriginRebased; }
        private void OnDisable() { FloatingOrigin.OnOriginRebased -= OnOriginRebased; }
        private void OnOriginRebased(Vector3 offset) { forceRebuild = true; }

        private void Update()
        {
            if (terrain == null || mesh == null || material == null) return;

            if (player == null)
            {
                var cam = Camera.main;
                if (cam != null) player = cam.transform;
            }
            Vector3 center = player != null ? player.position : transform.position;

            if (forceRebuild || (center - lastBuildCenter).sqrMagnitude > rebuildThreshold * rebuildThreshold)
            {
                Rebuild(center);
                lastBuildCenter = center;
                forceRebuild = false;
            }

            for (int i = 0; i < batches.Count; i++)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, batches[i], batches[i].Length,
                    null, ShadowCastingMode.Off, false);
            }
        }

        private void Rebuild(Vector3 center)
        {
            batches.Clear();

            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            float r2 = radius * radius;

            int cellRadius = Mathf.CeilToInt(radius / spacing);
            long centerCellX = Mathf.RoundToInt(center.x / spacing);
            long centerCellZ = Mathf.RoundToInt(center.z / spacing);

            var current = new List<Matrix4x4>(BatchMax);

            for (long cx = centerCellX - cellRadius; cx <= centerCellX + cellRadius; cx++)
            {
                for (long cz = centerCellZ - cellRadius; cz <= centerCellZ + cellRadius; cz++)
                {
                    uint h = Hash(cx, cz);
                    // Jitter dentro da célula (estável por célula do mundo).
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

                    uint h2 = Hash(cx * 2654435761 + 1, cz * 40503 + 7);
                    float yaw = (h2 & 0xFF) / 255f * 360f;
                    float w = Mathf.Lerp(widthRange.x, widthRange.y, ((h2 >> 8) & 0xFF) / 255f);
                    float hh = Mathf.Lerp(heightRange.x, heightRange.y, ((h2 >> 16) & 0xFF) / 255f);

                    var trs = Matrix4x4.TRS(new Vector3(wx, y, wz), Quaternion.Euler(0f, yaw, 0f), new Vector3(w, hh, w));
                    current.Add(trs);
                    if (current.Count == BatchMax)
                    {
                        batches.Add(current.ToArray());
                        current = new List<Matrix4x4>(BatchMax);
                    }
                }
            }

            if (current.Count > 0) batches.Add(current.ToArray());
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
        /// (técnica clássica de grama) para receber luz do céu/sol e não ficar preta.
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
        private Material BuildGrassMaterial()
        {
            Texture2D tex = GenerateBladeTexture();

            Shader shader = Shader.Find("Standard");
            Material mat;
            if (shader != null)
            {
                mat = new Material(shader) { name = "PlayerGrass_Standard" };
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
                mat.mainTexture = tex;
                mat.SetColor("_Color", healthyColor);
                // Leve emissão para garantir que nunca fique preta em sombra.
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", healthyColor * 0.18f);
                }
            }
            else
            {
                mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
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
