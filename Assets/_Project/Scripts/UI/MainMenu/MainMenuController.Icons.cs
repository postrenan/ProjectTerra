using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.UI
{
    /// <summary>
    /// Helpers de IMGUI que desenham ícones reais (PNGs do TerraIcons) ao lado do texto,
    /// substituindo os emojis que não renderizam nas fontes padrão.
    /// </summary>
    public partial class MainMenuController
    {
        private const float IconPad = 22f;
        private readonly Dictionary<GUIStyle, GUIStyle> iconPaddedStyleCache = new Dictionary<GUIStyle, GUIStyle>();

        private GUIStyle PaddedStyle(GUIStyle baseStyle, bool hasText)
        {
            if (!hasText) return baseStyle;
            if (!iconPaddedStyleCache.TryGetValue(baseStyle, out var padded))
            {
                padded = new GUIStyle(baseStyle);
                padded.padding = new RectOffset(baseStyle.padding.left + (int)IconPad, baseStyle.padding.right, baseStyle.padding.top, baseStyle.padding.bottom);
                iconPaddedStyleCache[baseStyle] = padded;
            }
            return padded;
        }

        private void DrawIconAt(Rect r, TerraIcon icon, GUIStyle style, bool hasText)
        {
            if (Event.current.type != EventType.Repaint) return;
            float size = Mathf.Clamp(r.height - 8f, 12f, 18f);
            float x = hasText ? r.x + 8f : r.x + (r.width - size) * 0.5f;
            Rect iconRect = new Rect(x, r.y + (r.height - size) * 0.5f, size, size);
            Color tint = style.normal.textColor;
            tint.a *= GUI.color.a;
            TerraIcons.Draw(iconRect, icon, tint);
        }

        private bool IconBtn(TerraIcon icon, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            bool hasText = !string.IsNullOrEmpty(text);
            bool clicked = GUILayout.Button(text, PaddedStyle(style, hasText), options);
            DrawIconAt(GUILayoutUtility.GetLastRect(), icon, style, hasText);
            return clicked;
        }

        private void IconLbl(TerraIcon icon, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.Label(text, PaddedStyle(style, true), options);
            DrawIconAt(GUILayoutUtility.GetLastRect(), icon, style, true);
        }

        private static TerraIcon CareerIcon(StarterCareer career)
        {
            switch (career)
            {
                case StarterCareer.Farmer: return TerraIcon.Tractor;
                case StarterCareer.Trucker: return TerraIcon.HeavyTruck;
                case StarterCareer.Aviator: return TerraIcon.Airplane;
                default: return TerraIcon.FishingBoat;
            }
        }
    }
}
