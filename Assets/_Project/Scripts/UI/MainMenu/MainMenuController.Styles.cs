using UnityEngine;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        // Texturas procedurais da interface
        private Texture2D whiteTex;
        private Texture2D darkGlassTex;
        private Texture2D headerGlowTex;
        private Texture2D buttonNormalTex;
        private Texture2D buttonHoverTex;
        private Texture2D buttonPrimaryTex;
        private Texture2D buttonDangerTex;
        private Texture2D vignetteTex;

        // Estilos GUI
        private GUIStyle titleMainStyle;
        private GUIStyle titleSubStyle;
        private GUIStyle versionBadgeStyle;
        private GUIStyle menuBtnStyle;
        private GUIStyle menuBtnSubtextStyle;
        private GUIStyle activeMenuBtnStyle;
        private GUIStyle modalBoxStyle;
        private GUIStyle modalHeaderTitleStyle;
        private GUIStyle modalHeaderSubStyle;
        private GUIStyle cardStyle;
        private GUIStyle cardActiveStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle activeTabButtonStyle;
        private GUIStyle sectionHeaderStyle;
        private GUIStyle labelRegularStyle;
        private GUIStyle labelBoldStyle;
        private GUIStyle valueHighlightStyle;
        private GUIStyle modernTextFieldStyle;
        private GUIStyle actionBtnStyle;
        private GUIStyle primaryActionBtnStyle;
        private GUIStyle dangerActionBtnStyle;
        private GUIStyle floatingBackBtnStyle;

        private bool stylesInitialized = false;

        private void InitStyles()
        {
            if (stylesInitialized && whiteTex != null) return;

            CreateProceduralTextures();
            BuildGUIStyles();

            stylesInitialized = true;
        }

        private void CreateProceduralTextures()
        {
            whiteTex = MakeSolidColorTexture(Color.white);
            darkGlassTex = MakeSolidColorTexture(new Color(0.02f, 0.05f, 0.10f, 0.94f));
            headerGlowTex = MakeSolidColorTexture(new Color(0.08f, 0.22f, 0.45f, 0.85f));
            buttonNormalTex = MakeSolidColorTexture(new Color(0.06f, 0.12f, 0.22f, 0.88f));
            buttonHoverTex = MakeSolidColorTexture(new Color(0.12f, 0.28f, 0.50f, 0.95f));
            buttonPrimaryTex = MakeSolidColorTexture(new Color(0.14f, 0.42f, 0.78f, 0.98f));
            buttonDangerTex = MakeSolidColorTexture(new Color(0.55f, 0.14f, 0.14f, 0.95f));

            // Textura sutil de vinheta nas laterais para reforçar o centro onde fica o globo
            vignetteTex = MakeGradientVignetteTexture(256, 256);
        }

        private Texture2D MakeSolidColorTexture(Color col)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[4];
            for (int i = 0; i < 4; i++) pixels[i] = col;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private Texture2D MakeGradientVignetteTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float maxDist = Mathf.Sqrt(center.x * center.x + center.y * center.y);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = dist / maxDist;
                    // Centro translúcido para deixar o globo visível, bordas mais escuras
                    float alpha = Mathf.SmoothStep(0.0f, 0.65f, norm);
                    pixels[y * width + x] = new Color(0.01f, 0.02f, 0.05f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void BuildGUIStyles()
        {
            titleMainStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleMainStyle.normal.textColor = new Color(0.95f, 0.98f, 1.0f);

            titleSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            titleSubStyle.normal.textColor = new Color(0.35f, 0.82f, 1.0f, 0.90f);

            versionBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            versionBadgeStyle.normal.textColor = new Color(0.70f, 0.85f, 0.95f, 0.60f);

            menuBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 16, 6, 6)
            };
            menuBtnStyle.normal.background = buttonNormalTex;
            menuBtnStyle.hover.background = buttonHoverTex;
            menuBtnStyle.active.background = buttonPrimaryTex;
            menuBtnStyle.normal.textColor = new Color(0.92f, 0.96f, 1.0f);
            menuBtnStyle.hover.textColor = Color.white;

            menuBtnSubtextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            menuBtnSubtextStyle.normal.textColor = new Color(0.60f, 0.78f, 0.92f, 0.75f);

            activeMenuBtnStyle = new GUIStyle(menuBtnStyle);
            activeMenuBtnStyle.normal.background = buttonPrimaryTex;
            activeMenuBtnStyle.normal.textColor = new Color(1.0f, 0.95f, 0.40f);

            modalBoxStyle = new GUIStyle(GUI.skin.box);
            modalBoxStyle.normal.background = darkGlassTex;

            modalHeaderTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            modalHeaderTitleStyle.normal.textColor = Color.white;

            modalHeaderSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            modalHeaderSubStyle.normal.textColor = new Color(0.50f, 0.80f, 1.0f);

            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.background = buttonNormalTex;

            cardActiveStyle = new GUIStyle(GUI.skin.box);
            cardActiveStyle.normal.background = buttonHoverTex;

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            tabButtonStyle.normal.background = buttonNormalTex;
            tabButtonStyle.hover.background = buttonHoverTex;
            tabButtonStyle.normal.textColor = new Color(0.80f, 0.90f, 1.0f);

            activeTabButtonStyle = new GUIStyle(tabButtonStyle);
            activeTabButtonStyle.normal.background = buttonPrimaryTex;
            activeTabButtonStyle.normal.textColor = new Color(1.0f, 0.95f, 0.40f);

            sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            sectionHeaderStyle.normal.textColor = new Color(0.40f, 0.85f, 1.0f);

            labelRegularStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            labelRegularStyle.normal.textColor = new Color(0.85f, 0.90f, 0.98f);

            labelBoldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            labelBoldStyle.normal.textColor = Color.white;

            valueHighlightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            valueHighlightStyle.normal.textColor = new Color(1.0f, 0.88f, 0.35f);

            modernTextFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            actionBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            actionBtnStyle.normal.background = buttonNormalTex;
            actionBtnStyle.hover.background = buttonHoverTex;
            actionBtnStyle.normal.textColor = Color.white;

            primaryActionBtnStyle = new GUIStyle(actionBtnStyle);
            primaryActionBtnStyle.normal.background = buttonPrimaryTex;
            primaryActionBtnStyle.normal.textColor = new Color(1.0f, 0.95f, 0.45f);

            dangerActionBtnStyle = new GUIStyle(actionBtnStyle);
            dangerActionBtnStyle.normal.background = buttonDangerTex;
            dangerActionBtnStyle.normal.textColor = new Color(1.0f, 0.80f, 0.80f);

            floatingBackBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            floatingBackBtnStyle.normal.background = buttonPrimaryTex;
            floatingBackBtnStyle.hover.background = buttonHoverTex;
            floatingBackBtnStyle.normal.textColor = Color.white;
        }

        private void DrawBorder(Rect r, Color col, float thickness = 1f)
        {
            Color old = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), whiteTex);
            GUI.DrawTexture(new Rect(r.x + r.width - thickness, r.y, thickness, r.height), whiteTex);
            GUI.color = old;
        }
    }
}
