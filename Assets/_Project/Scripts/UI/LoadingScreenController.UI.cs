using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.UI
{
    public partial class LoadingScreenController
    {
        private void InitStyles()
        {
            if (stylesInitialized) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            subtitleStyle.normal.textColor = new Color(0.35f, 0.85f, 1.0f);

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            statusStyle.normal.textColor = new Color(0.9f, 0.95f, 1.0f);

            percentStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            percentStyle.normal.textColor = new Color(1.0f, 0.9f, 0.35f);

            quoteStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            quoteStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f, 0.85f);

            dossierCardStyle = new GUIStyle(GUI.skin.box);

            enterButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            enterButtonStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!isVisible || screenAlpha <= 0f) return;

            InitStyles();
            GUI.depth = -1000; // Desenha sobre todos os outros elementos da interface

            float sw = Screen.width;
            float sh = Screen.height;

            // Salva cor da GUI com fade alpha
            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            // 1. Imagem de Fundo Panorâmica com Efeito Ken Burns (Zoom e Pan suave)
            if (backgroundTexture != null)
            {
                float zoom = 1.05f + 0.05f * Mathf.Sin(animationTimer * 0.15f);
                float panX = Mathf.Sin(animationTimer * 0.08f) * 20.0f;
                float panY = Mathf.Cos(animationTimer * 0.06f) * 10.0f;

                float bw = sw * zoom;
                float bh = sh * zoom;
                float bx = (sw - bw) * 0.5f + panX;
                float by = (sh - bh) * 0.5f + panY;

                GUI.DrawTexture(new Rect(bx, by, bw, bh), backgroundTexture, ScaleMode.ScaleAndCrop);
            }
            else
            {
                // Fallback de gradiente escuro elegante
                Color topBg = currentTheme == LoadingTheme.Plantation ? new Color(0.12f, 0.16f, 0.08f) :
                              (currentTheme == LoadingTheme.Metropolis ? new Color(0.08f, 0.1f, 0.18f) : new Color(0.06f, 0.14f, 0.18f));
                GUI.color = topBg * screenAlpha;
                GUI.DrawTexture(new Rect(0, 0, sw, sh), whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }

            // 2. Vinheta e Gradientes Escuros de Legibilidade
            // Gradiente Superior
            GUI.color = new Color(0f, 0f, 0f, 0.75f * screenAlpha);
            GUI.DrawTexture(new Rect(0, 0, sw, 180), whiteTexture);
            // Gradiente Inferior
            GUI.color = new Color(0f, 0f, 0f, 0.85f * screenAlpha);
            GUI.DrawTexture(new Rect(0, sh - 220, sw, 220), whiteTexture);
            // Painel Lateral de Relevo
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.70f * screenAlpha);
            GUI.DrawTexture(new Rect(sw - 480, 0, 480, sh), whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            // 3. Cabeçalho Principal (Top-Left)
            GUILayout.BeginArea(new Rect(40, 30, sw - 540, 130));
            string themeTag = currentTheme == LoadingTheme.Plantation ? "🌾 ZONA DE PRODUÇÃO AGRÍCOLA & PLANTAÇÕES GLOBAIS" :
                             (currentTheme == LoadingTheme.Metropolis ? "🏙️ METRÓPOLE GLOBAL & CENTRO ECONÔMICO-FINANCEIRO" : "⚓ POLO LOGÍSTICO PORTUÁRIO & LITORAL");
            GUILayout.Label(themeTag, subtitleStyle);
            GUILayout.Space(2);
            string displayTitle = currentSave != null && !string.IsNullOrEmpty(currentSave.saveName) ?
                currentSave.saveName.ToUpper() : $"GOVERNO DE {(currentRegion != null ? currentRegion.name.ToUpper() : "TERRA")}";
            GUILayout.Label(displayTitle, titleStyle);
            GUILayout.Space(4);
            string locationDesc = currentRegion != null ?
                $"{currentRegion.country.ToUpper()} • {currentRegion.type} • Coordenadas: Lat {currentRegion.centerLat:F2}° | Lon {currentRegion.centerLon:F2}° (Escala 1:1 Datum WGS84)" :
                "TERRITÓRIO GLOBAL • ESCALA 1:1";
            GUILayout.Label(locationDesc, quoteStyle);
            GUILayout.EndArea();

            // 4. Painel de Dossiê Regional e Econômico (Top-Right Glassmorphism)
            GUILayout.BeginArea(new Rect(sw - 450, 40, 410, sh - 280));
            GUI.color = new Color(0.05f, 0.1f, 0.18f, 0.88f * screenAlpha);
            GUILayout.BeginVertical(dossierCardStyle, GUILayout.ExpandHeight(true));
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            GUILayout.Label("📊 DOSSIÊ ESTRATÉGICO REGIONAL", subtitleStyle);
            GUILayout.Space(6);

            // Divisor
            GUI.color = new Color(0.3f, 0.7f, 1f, 0.35f * screenAlpha);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            GUILayout.Space(8);

            // Orçamento Inicial
            string budgetStr = currentSave != null ? currentSave.GetFormattedMoney() : "$500,000";
            GUILayout.Label("💰 Orçamento de Tesouro Inicial:", quoteStyle);
            GUI.color = new Color(0.3f, 1.0f, 0.5f);
            GUILayout.Label(budgetStr, percentStyle);
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            GUILayout.Space(12);

            // Indicadores de Recursos
            if (currentRegion != null)
            {
                DrawDossierStat("🌾 Terras Aráveis & Plantações", currentRegion.arablePercent, new Color(1f, 0.88f, 0.3f));
                DrawDossierStat("🌲 Cobertura Florestal", currentRegion.forestPercent, new Color(0.2f, 0.85f, 0.35f));
                DrawDossierStat("⛏️ Potencial Mineral e Geológico", currentRegion.mineralsPercent, new Color(1f, 0.65f, 0.25f));
                DrawDossierStat("💧 Recursos Hídricos e Aquíferos", currentRegion.waterPercent, new Color(0.25f, 0.75f, 1f));

                GUILayout.Space(8);
                var infra = RegionInfrastructureDatabase.GetInfrastructure(currentRegion.id, currentRegion);
                if (infra != null)
                {
                    GUI.color = new Color(0.45f, 0.85f, 1f, 0.95f * screenAlpha);
                    GUILayout.Label($"📍 {infra.environmentCategory}", quoteStyle);
                    GUI.color = new Color(0.85f, 0.92f, 1f, 0.9f * screenAlpha);
                    GUILayout.Label($"🌊 {infra.waterBodyName}", quoteStyle);
                    GUILayout.Label($"⚓ {infra.portName}", quoteStyle);
                    GUILayout.Label($"🛫 {infra.airportName}", quoteStyle);
                    GUI.color = new Color(1f, 1f, 1f, screenAlpha);
                }
            }

            GUILayout.Space(16);
            GUILayout.Label("💡 DIRETRIZ DE GESTÃO DO TERRITÓRIO:", subtitleStyle);
            GUILayout.Space(4);
            string tipText = currentTheme == LoadingTheme.Plantation ?
                "O cultivo sustentável em grande escala garante a segurança alimentar e insumos industriais vitais. O relevo suave e os aquíferos locais facilitam irrigação pivotante de alta produtividade." :
                (currentTheme == LoadingTheme.Metropolis ?
                "Áreas urbanas concentram os maiores mercados consumidores e pólos tecnológicos do planeta. Gerencie a matriz energética e zoneamentos industriais para maximizar a arrecadação fiscal." :
                "A infraestrutura portuária conecta a produção agrícola e mineral aos mercados mundiais. Preserve os ecossistemas marinhos enquanto expande a capacidade de escoamento logístico.");
            GUILayout.Label(tipText, quoteStyle);

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // 5. Barra de Progresso e Notificação Técnica de Topografia 1:1 (Rodapé)
            GUILayout.BeginArea(new Rect(50, sh - 170, sw - 540, 140));

            // Status e Porcentagem
            GUILayout.BeginHorizontal();
            GUILayout.Label(currentStatusMessage, statusStyle, GUILayout.Width(sw - 660));
            GUILayout.Label($"{Mathf.RoundToInt(loadProgress * 100f)}%", percentStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // Fundo da Barra de Progresso
            Rect progressBgRect = GUILayoutUtility.GetRect(sw - 540, 12);
            GUI.color = new Color(0.12f, 0.15f, 0.22f, 0.95f * screenAlpha);
            GUI.DrawTexture(progressBgRect, whiteTexture);

            // Preenchimento com Brilho
            float barWidth = (progressBgRect.width - 4) * loadProgress;
            Rect progressFillRect = new Rect(progressBgRect.x + 2, progressBgRect.y + 2, barWidth, progressBgRect.height - 4);
            Color barColor = isFinishedLoading ?
                new Color(0.35f, 0.95f, 0.5f, 1f * screenAlpha) :
                new Color(0.3f, 0.75f, 1f, 1f * screenAlpha);
            GUI.color = barColor;
            GUI.DrawTexture(progressFillRect, whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, screenAlpha);

            GUILayout.Space(14);

            // 6. Botão de Entrada Interativa / Conclusão
            if (isFinishedLoading)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 5.0f);
                GUI.color = new Color(0.2f, 0.8f, 0.4f, pulse * screenAlpha);
                if (GUILayout.Button("▶ ENTRAR NO TERRITÓRIO [ENTER]", enterButtonStyle, GUILayout.Height(40), GUILayout.Width(340)))
                {
                    StartFadeOut();
                }
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }
            else
            {
                GUI.color = new Color(0.6f, 0.75f, 0.9f, 0.6f * screenAlpha);
                GUILayout.Label("Pressione [ESC] para cancelar se desejar retornar à órbita.", quoteStyle);
                GUI.color = new Color(1f, 1f, 1f, screenAlpha);
            }

            GUILayout.EndArea();

            GUI.color = prevColor;
        }

        private void DrawDossierStat(string label, float percent, Color barColor)
        {
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, quoteStyle, GUILayout.Width(240));
            GUILayout.Label($"{percent:F0}%", quoteStyle);
            GUILayout.EndHorizontal();

            Rect bgRect = GUILayoutUtility.GetRect(380, 6);
            GUI.color = new Color(0.1f, 0.15f, 0.22f, 0.8f);
            GUI.DrawTexture(bgRect, whiteTexture);

            Rect fillRect = new Rect(bgRect.x, bgRect.y, bgRect.width * (percent / 100f), bgRect.height);
            GUI.color = barColor;
            GUI.DrawTexture(fillRect, whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(2);
        }
    }
}
