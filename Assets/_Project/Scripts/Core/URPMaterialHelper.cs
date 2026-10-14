using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Core
{
    /// <summary>
    /// Helpers para criação de materiais compatíveis com URP (Universal Render Pipeline).
    /// Centraliza a lógica de seleção de shader para evitar duplicação em todo o código.
    /// </summary>
    public static class URPMaterialHelper
    {
        // Shader URP Lit padrão (compatível com SRP Batcher)
        private static Shader _urpLitShader;
        private static Shader _urpSimpleLitShader;
        private static Shader _urpParticlesLitShader;
        private static Shader _urpUnlitShader;

        public static Shader URPLitShader
        {
            get
            {
                if (_urpLitShader == null)
                    _urpLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Unlit/Color");
                return _urpLitShader;
            }
        }

        public static Shader URPSimpleLitShader
        {
            get
            {
                if (_urpSimpleLitShader == null)
                    _urpSimpleLitShader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? URPLitShader;
                return _urpSimpleLitShader;
            }
        }

        public static Shader URPLitParticlesShader
        {
            get
            {
                if (_urpParticlesLitShader == null)
                    _urpParticlesLitShader = Shader.Find("Universal Render Pipeline/Particles/Lit") ?? URPLitShader;
                return _urpParticlesLitShader;
            }
        }

        public static Shader URPUnlitShader
        {
            get
            {
                if (_urpUnlitShader == null)
                    _urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                return _urpUnlitShader;
            }
        }

        /// <summary>
        /// Cria um material URP Lit padrão com cor base, suavidade e metallic.
        /// </summary>
        public static Material CreateURPLitMaterial(string name, Color baseColor, float smoothness = 0.2f, float metallic = 0.0f)
        {
            var mat = new Material(URPLitShader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_SmoothnessTextureChannel")) mat.SetFloat("_SmoothnessTextureChannel", 0f);
            return mat;
        }

        /// <summary>
        /// Cria um material URP Particles/Lit (suporta transparência e alpha clip).
        /// </summary>
        public static Material CreateURPParticlesLitMaterial(string name, Color baseColor, float smoothness = 0.2f, float metallic = 0.0f)
        {
            var mat = new Material(URPLitParticlesShader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        /// <summary>
        /// Cria um material URP Unlit (sem iluminação).
        /// </summary>
        public static Material CreateURPUnlitMaterial(string name, Color color)
        {
            var mat = new Material(URPUnlitShader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        /// <summary>
        /// Configura propriedades de transparência padrão URP (Alpha Blend).
        /// </summary>
        public static void SetupTransparentBlend(Material mat, int renderQueue = 3000)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // SrcAlpha OneMinusSrcAlpha
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.renderQueue = renderQueue;
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
        }

        /// <summary>
        /// Configura propriedades de Alpha Clip (Cutout) padrão URP.
        /// </summary>
        public static void SetupAlphaClip(Material mat, float cutoff = 0.5f, int renderQueue = 2450)
        {
            if (mat == null) return;
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
            if (mat.HasProperty("_Cutoff")) mat.SetFloat("_Cutoff", cutoff);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 0f); // Opaque
            mat.renderQueue = renderQueue;
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
        }

        /// <summary>
        /// Configura texturas padrão (Albedo/BaseMap, Normal/BumpMap) com fallbacks.
        /// </summary>
        public static void SetupTextures(Material mat, Texture albedo, Texture normal = null, float bumpScale = 1f)
        {
            if (mat == null) return;
            
            // Albedo / BaseMap
            if (albedo != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
                else if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
            }

            // Normal Map
            if (normal != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        /// <summary>
        /// Cria um material de cor sólida simples (compatível Built-in/URP).
        /// </summary>
        public static Material CreateSolidMaterial(Color color, float smoothness = 0.15f, float metallic = 0.0f)
        {
            var mat = CreateURPLitMaterial("Solid_" + color.GetHashCode(), color, smoothness, metallic);
            return mat;
        }

        // Cache de conversões (material-fonte -> material URP) para evitar recriar
        // materiais idênticos ao converter centenas de instâncias (arbustos, rochas).
#if UNITY_6000_4_OR_NEWER
        private static readonly Dictionary<EntityId, Material> _urpConversionCache = new Dictionary<EntityId, Material>();
#else
        private static readonly Dictionary<int, Material> _urpConversionCache = new Dictionary<int, Material>();
#endif

        /// <summary>
        /// Percorre os renderers de um modelo instanciado e converte qualquer material cujo
        /// shader não seja do Universal Render Pipeline (ex.: Standard do Built-in, que aparece
        /// ROSA/magenta em URP) para um material URP/Lit equivalente, preservando albedo, normal,
        /// cor, metallic/smoothness e alpha clip. Materiais já URP são mantidos intactos.
        /// </summary>
        /// <param name="foliageAlphaClip">Força alpha clip (cutout) — use para folhagem recortada.</param>
        public static void UpgradeRenderersToURP(GameObject root, bool foliageAlphaClip = false)
        {
            if (root == null) return;
            if (URPLitShader == null) return;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;

            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                var mats = rend.sharedMaterials;
                if (mats == null || mats.Length == 0) continue;

                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;

                    // Já é um shader do URP? Mantém como está.
                    var sh = src.shader;
                    if (sh != null && sh.name != null && sh.name.StartsWith("Universal Render Pipeline/"))
                        continue;

                    mats[i] = ConvertToURPLit(src, foliageAlphaClip);
                    changed = true;
                }

                if (changed) rend.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// Converte um material (tipicamente Standard do Built-in) para URP/Lit preservando
        /// as texturas e parâmetros principais. O resultado é cacheado por material-fonte.
        /// </summary>
        public static Material ConvertToURPLit(Material src, bool preferAlphaClip = false)
        {
            if (src == null) return null;

#if UNITY_6000_4_OR_NEWER
            EntityId key = src.GetEntityId();
#else
            int key = src.GetInstanceID();
#endif
            if (_urpConversionCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var mat = new Material(URPLitShader) { name = src.name + "_URP" };

            // Cor base
            Color baseColor = Color.white;
            if (src.HasProperty("_Color")) baseColor = src.GetColor("_Color");
            else if (src.HasProperty("_BaseColor")) baseColor = src.GetColor("_BaseColor");
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);

            // Albedo / BaseMap (transfere também tiling/offset)
            Texture albedo = null;
            if (src.HasProperty("_MainTex")) albedo = src.GetTexture("_MainTex");
            if (albedo == null && src.HasProperty("_BaseMap")) albedo = src.GetTexture("_BaseMap");
            if (albedo != null && mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", albedo);
                if (src.HasProperty("_MainTex"))
                {
                    mat.SetTextureScale("_BaseMap", src.GetTextureScale("_MainTex"));
                    mat.SetTextureOffset("_BaseMap", src.GetTextureOffset("_MainTex"));
                }
            }

            // Normal map
            if (src.HasProperty("_BumpMap"))
            {
                var normal = src.GetTexture("_BumpMap");
                if (normal != null && mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                    if (src.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", src.GetFloat("_BumpScale"));
                }
            }

            // Metallic / Smoothness (mapeia _Glossiness do Standard -> _Smoothness do URP)
            if (src.HasProperty("_Metallic") && mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", src.GetFloat("_Metallic"));
            if (mat.HasProperty("_Smoothness"))
            {
                if (src.HasProperty("_Glossiness")) mat.SetFloat("_Smoothness", src.GetFloat("_Glossiness"));
                else if (src.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", src.GetFloat("_Smoothness"));
            }

            // Alpha clip (cutout): folhagem recortada ou Standard em modo Cutout
            bool cutout = preferAlphaClip || src.IsKeywordEnabled("_ALPHATEST_ON");
            if (src.HasProperty("_Mode") && Mathf.Approximately(src.GetFloat("_Mode"), 1f))
                cutout = true; // Standard "Cutout"
            if (cutout)
            {
                float cutoff = src.HasProperty("_Cutoff") ? src.GetFloat("_Cutoff") : 0.5f;
                SetupAlphaClip(mat, cutoff);
            }

            _urpConversionCache[key] = mat;
            return mat;
        }
    }
}