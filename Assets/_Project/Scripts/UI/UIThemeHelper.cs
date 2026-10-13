using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.UI
{
    /// <summary>
    /// Utilitário para geração e gerenciamento de texturas procedurais com cantos arredondados,
    /// bordas suaves e 9-slice para elementos de UI / IMGUI no Project Terra.
    /// </summary>
    public static class UIThemeHelper
    {
        private static readonly Dictionary<string, Texture2D> roundedTextureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, GUIStyle> roundedStyleCache = new Dictionary<string, GUIStyle>();

        private static uint ColorToRGBA32(Color c)
        {
            uint r = (uint)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
            uint g = (uint)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
            uint b = (uint)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
            uint a = (uint)Mathf.Clamp(Mathf.RoundToInt(c.a * 255f), 0, 255);
            return (r << 24) | (g << 16) | (b << 8) | a;
        }

        /// <summary>
        /// Gera ou recupera do cache uma Texture2D com cantos arredondados e suporte a 9-slice (GUIStyle.border).
        /// </summary>
        /// <param name="fillColor">Cor de preenchimento interna.</param>
        /// <param name="borderColor">Cor da borda arredondada.</param>
        /// <param name="borderThickness">Espessura da borda em pixels.</param>
        /// <param name="radius">Raio dos cantos arredondados.</param>
        /// <param name="size">Tamanho da textura quadrada gerada.</param>
        public static Texture2D CreateRoundedTexture(Color fillColor, Color borderColor, float borderThickness = 1.5f, float radius = 7f, int size = 32)
        {
            if (radius > 9f && size < 48) size = 48;

            uint f = ColorToRGBA32(fillColor);
            uint b = ColorToRGBA32(borderColor);
            string key = $"{f:X8}_{b:X8}_{borderThickness:F1}_{radius:F1}_{size}";
            if (roundedTextureCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Rounded_{key}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[size * size];
            float halfBorder = borderThickness * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetSignedDistanceToRoundedBox(x + 0.5f, y + 0.5f, size, size, radius);

                    // dist < 0 = dentro da forma
                    // Antialiasing suave na borda externa (distância até 0)
                    float outerAlpha = Mathf.Clamp01(0.5f - dist);

                    if (borderThickness > 0.05f && borderColor.a > 0.005f)
                    {
                        // Distância até a linha central da borda (-halfBorder)
                        float edgeDist = Mathf.Abs(dist + halfBorder) - halfBorder;
                        float borderAlpha = Mathf.Clamp01(0.5f - edgeDist);

                        Color c = Color.Lerp(fillColor, borderColor, borderAlpha);
                        c.a *= outerAlpha;
                        pixels[y * size + x] = c;
                    }
                    else
                    {
                        Color c = fillColor;
                        c.a *= outerAlpha;
                        pixels[y * size + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            roundedTextureCache[key] = tex;
            return tex;
        }

        /// <summary>
        /// Retorna a distância com sinal de um ponto (px, py) até a borda de uma caixa arredondada de tamanho (w, h).
        /// </summary>
        private static float GetSignedDistanceToRoundedBox(float px, float py, float w, float h, float radius)
        {
            float halfW = w * 0.5f;
            float halfH = h * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(halfW, halfH) - 1f);

            // Coordenadas relativas ao centro
            float dx = Mathf.Abs(px - halfW) - (halfW - r);
            float dy = Mathf.Abs(py - halfH) - (halfH - r);

            float outsideDist = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            float insideDist = Mathf.Min(Mathf.Max(dx, dy), 0f);

            return (outsideDist + insideDist) - r;
        }

        /// <summary>
        /// Recupera ou gera um GUIStyle 9-slice configurado com cantos arredondados e borda.
        /// </summary>
        public static GUIStyle GetRoundedBoxStyle(Color fillColor, Color borderColor, float radius = 8f, float borderThickness = 1.5f)
        {
            uint f = ColorToRGBA32(fillColor);
            uint b = ColorToRGBA32(borderColor);
            string key = $"{f:X8}_{b:X8}_{radius:F1}_{borderThickness:F1}";

            if (roundedStyleCache.TryGetValue(key, out var cached) && cached != null && cached.normal.background != null)
            {
                return cached;
            }

            int size = radius > 9f ? 48 : 32;
            int slice = Mathf.Clamp(Mathf.RoundToInt(radius + borderThickness + 1f), 4, (size / 2) - 2);
            var tex = CreateRoundedTexture(fillColor, borderColor, borderThickness, radius, size);

            var style = new GUIStyle(GUIStyle.none)
            {
                name = $"RoundedBox_{key}",
                border = new RectOffset(slice, slice, slice, slice),
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = tex;
            roundedStyleCache[key] = style;
            return style;
        }

        /// <summary>
        /// Desenha uma caixa ou interface com preenchimento e borda de cantos arredondados antialiased.
        /// Respeita o canal alpha global da GUI para compatibilidade com efeitos de fade.
        /// </summary>
        public static void DrawRoundedBox(Rect rect, Color fillColor, Color borderColor, float radius = 8f, float borderThickness = 1.5f)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            if (Event.current != null && Event.current.type != EventType.Repaint) return;

            Color prevColor = GUI.color;
            float globalAlpha = prevColor.a;
            GUI.color = new Color(1f, 1f, 1f, globalAlpha);

            var style = GetRoundedBoxStyle(fillColor, borderColor, radius, borderThickness);
            style.Draw(rect, false, false, false, false);

            GUI.color = prevColor;
        }

        /// <summary>
        /// Desenha uma borda arredondada vazada (sem preenchimento).
        /// </summary>
        public static void DrawRoundedBorder(Rect rect, Color borderColor, float borderThickness = 1.5f, float radius = 8f)
        {
            DrawRoundedBox(rect, Color.clear, borderColor, radius, borderThickness);
        }

        /// <summary>
        /// Cria um GUIStyle pronto para uso em cards (ex: GUILayout.BeginVertical(cardStyle)).
        /// </summary>
        public static GUIStyle CreateRoundedCardStyle(Color fillColor, Color borderColor, float radius = 8f, float borderThickness = 1.5f, RectOffset padding = null)
        {
            var baseStyle = GetRoundedBoxStyle(fillColor, borderColor, radius, borderThickness);
            var cardStyle = new GUIStyle(baseStyle);
            if (padding != null) cardStyle.padding = padding;
            return cardStyle;
        }

        /// <summary>
        /// Aplica texturas e bordas 9-slice arredondadas a um GUIStyle de botão.
        /// </summary>
        public static GUIStyle ApplyRoundedStyle(
            this GUIStyle style,
            Color normalBg,
            Color normalBorder,
            Color hoverBg,
            Color hoverBorder,
            Color activeBg,
            Color activeBorder,
            float radius = 7f,
            float borderThickness = 1.5f,
            int sliceBorder = 8)
        {
            int size = radius > 9f ? 48 : 32;
            int slice = Mathf.Clamp(sliceBorder, 4, (size / 2) - 2);

            var normalTex = CreateRoundedTexture(normalBg, normalBorder, borderThickness, radius, size);
            var hoverTex = CreateRoundedTexture(hoverBg, hoverBorder, borderThickness, radius, size);
            var activeTex = CreateRoundedTexture(activeBg, activeBorder, borderThickness, radius, size);

            style.normal.background = normalTex;
            style.hover.background = hoverTex;
            style.active.background = activeTex;
            style.border = new RectOffset(slice, slice, slice, slice);

            return style;
        }

        /// <summary>
        /// Aplica tema padrão arredondado para botões escuros modernos da interface.
        /// </summary>
        public static void ApplyModernButtonSkin(GUIStyle style)
        {
            ApplyRoundedStyle(
                style,
                normalBg: new Color(0.06f, 0.12f, 0.22f, 0.90f),
                normalBorder: new Color(0.22f, 0.55f, 0.85f, 0.70f),
                hoverBg: new Color(0.12f, 0.28f, 0.50f, 0.96f),
                hoverBorder: new Color(0.40f, 0.85f, 1.0f, 0.95f),
                activeBg: new Color(0.14f, 0.42f, 0.78f, 0.98f),
                activeBorder: new Color(0.60f, 0.92f, 1.0f, 1.0f),
                radius: 7f,
                borderThickness: 1.5f,
                sliceBorder: 8
            );
        }

        /// <summary>
        /// Aplica tema primário (destaque ciano/ouro) com cantos arredondados.
        /// </summary>
        public static void ApplyPrimaryButtonSkin(GUIStyle style)
        {
            ApplyRoundedStyle(
                style,
                normalBg: new Color(0.12f, 0.38f, 0.72f, 0.95f),
                normalBorder: new Color(0.35f, 0.80f, 1.0f, 0.90f),
                hoverBg: new Color(0.16f, 0.48f, 0.88f, 0.98f),
                hoverBorder: new Color(0.60f, 0.95f, 1.0f, 1.0f),
                activeBg: new Color(0.08f, 0.28f, 0.56f, 1.0f),
                activeBorder: new Color(1.0f, 0.90f, 0.40f, 1.0f),
                radius: 7f,
                borderThickness: 1.5f,
                sliceBorder: 8
            );
        }

        /// <summary>
        /// Aplica tema perigo/alerta (vermelho) com cantos arredondados.
        /// </summary>
        public static void ApplyDangerButtonSkin(GUIStyle style)
        {
            ApplyRoundedStyle(
                style,
                normalBg: new Color(0.50f, 0.12f, 0.12f, 0.92f),
                normalBorder: new Color(0.85f, 0.25f, 0.25f, 0.75f),
                hoverBg: new Color(0.65f, 0.18f, 0.18f, 0.98f),
                hoverBorder: new Color(1.0f, 0.45f, 0.45f, 0.95f),
                activeBg: new Color(0.40f, 0.08f, 0.08f, 1.0f),
                activeBorder: new Color(1.0f, 0.70f, 0.70f, 1.0f),
                radius: 7f,
                borderThickness: 1.5f,
                sliceBorder: 8
            );
        }
    }
}
