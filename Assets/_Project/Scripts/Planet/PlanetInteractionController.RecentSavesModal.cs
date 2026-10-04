using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void DrawRecentSavesUI()
        {
            int totalSaves = SaveManager.Instance != null ? SaveManager.Instance.TotalSaveCount : 0;

            // 1. Botão Retrátil Superior Esquerdo
            float btnX = 24f;
            float btnY = 24f;
            float btnWidth = 240f;
            float btnHeight = 38f;
            recentSavesButtonRect = new Rect(btnX, btnY, btnWidth, btnHeight);

            // Fundo estilizado do botão
            GUI.color = isRecentSavesOpen ? new Color(0.06f, 0.14f, 0.28f, 0.96f) : new Color(0.02f, 0.06f, 0.14f, 0.90f);
            GUI.DrawTexture(recentSavesButtonRect, whiteTex);

            // Borda do botão
            Color borderColor = isRecentSavesOpen ? new Color(0.4f, 0.85f, 1.0f, 0.95f) : new Color(0.25f, 0.6f, 0.85f, 0.75f);
            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(btnX, btnY, btnWidth, 2), whiteTex);
            GUI.DrawTexture(new Rect(btnX, btnY + btnHeight - 2, btnWidth, 2), whiteTex);
            GUI.DrawTexture(new Rect(btnX, btnY, 2, btnHeight), whiteTex);
            GUI.DrawTexture(new Rect(btnX + btnWidth - 2, btnY, 2, btnHeight), whiteTex);
            GUI.color = Color.white;

            // Texto do botão
            string arrow = isRecentSavesOpen ? "▲" : "▼";
            string buttonLabel = totalSaves > 0 ? $"📁 Saves Recentes ({totalSaves})  {arrow}" : $"📁 Saves Recentes (0)  {arrow}";

            if (GUI.Button(recentSavesButtonRect, buttonLabel, recentBtnStyle ?? primaryButtonStyle))
            {
                ToggleRecentSavesModal();
            }

            // 2. Modal Retrátil com Animação Fluida
            if (recentSavesAnimProgress <= 0.001f)
            {
                recentSavesModalRect = Rect.zero;
                return;
            }

            float modalX = btnX;
            float modalY = btnY + btnHeight + 6f;
            float modalWidth = 440f;
            float maxModalHeight = Mathf.Min(Screen.height - modalY - 24f, 560f);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, recentSavesAnimProgress);
            float currentModalHeight = maxModalHeight * smoothProgress;

            recentSavesModalRect = new Rect(modalX, modalY, modalWidth, currentModalHeight);

            // Fundo escuro translúcido com fade da animação
            GUI.color = new Color(0.02f, 0.05f, 0.12f, 0.96f * smoothProgress);
            GUI.DrawTexture(recentSavesModalRect, whiteTex);

            // Bordas do modal
            GUI.color = new Color(0.3f, 0.75f, 1.0f, 0.85f * smoothProgress);
            GUI.DrawTexture(new Rect(modalX, modalY, modalWidth, 2), whiteTex);
            GUI.DrawTexture(new Rect(modalX, modalY + currentModalHeight - 2, modalWidth, 2), whiteTex);
            GUI.DrawTexture(new Rect(modalX, modalY, 2, currentModalHeight), whiteTex);
            GUI.DrawTexture(new Rect(modalX + modalWidth - 2, modalY, 2, currentModalHeight), whiteTex);
            GUI.color = Color.white;

            // Grupo de recorte para efeito retrátil sanfonado suave
            GUI.BeginGroup(recentSavesModalRect);
            GUILayout.BeginArea(new Rect(14, 12, modalWidth - 28, maxModalHeight - 20));

            // --- CABEÇALHO DO MODAL ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("💾 Partidas Recentes", headerTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("▲ Recolher", buttonStyle, GUILayout.Width(85), GUILayout.Height(24)))
            {
                CloseRecentSavesModal();
            }
            GUILayout.EndHorizontal();

            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // --- BARRA DE BUSCA RÁPIDA ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("🔍", labelStyle, GUILayout.Width(22));
            recentSavesSearchQuery = GUILayout.TextField(recentSavesSearchQuery, textFieldStyle, GUILayout.Height(24));
            if (!string.IsNullOrEmpty(recentSavesSearchQuery))
            {
                if (GUILayout.Button("✕", buttonStyle, GUILayout.Width(24), GUILayout.Height(24)))
                {
                    recentSavesSearchQuery = "";
                    GUI.FocusControl(null);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // --- LISTA DE SAVES ---
            var allSaves = SaveManager.Instance != null ? SaveManager.Instance.GetAllSaves() : new List<RegionSaveData>();

            List<RegionSaveData> displaySaves = new List<RegionSaveData>();
            string query = (recentSavesSearchQuery ?? "").Trim().ToLowerInvariant();

            foreach (var s in allSaves)
            {
                if (string.IsNullOrEmpty(query) ||
                    (s.saveName != null && s.saveName.ToLowerInvariant().Contains(query)) ||
                    (s.regionName != null && s.regionName.ToLowerInvariant().Contains(query)) ||
                    (s.countryName != null && s.countryName.ToLowerInvariant().Contains(query)))
                {
                    displaySaves.Add(s);
                }
            }

            if (displaySaves.Count == 0)
            {
                GUILayout.Space(20);
                if (allSaves.Count == 0)
                {
                    GUILayout.Label("Nenhuma partida salva encontrada no sistema.", headerSubtitleStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("💡 Clique em qualquer estado ou país do globo para iniciar uma nova jornada!", headerSubtitleStyle);
                }
                else
                {
                    GUILayout.Label($"Nenhum save encontrado para '{recentSavesSearchQuery}'.", headerSubtitleStyle);
                }
                GUILayout.Space(20);
            }
            else
            {
                float scrollAreaHeight = maxModalHeight - 105f;
                recentSavesScrollPos = GUILayout.BeginScrollView(recentSavesScrollPos, GUILayout.Height(scrollAreaHeight));

                foreach (var save in displaySaves)
                {
                    // Mini card do save
                    GUI.color = new Color(0.08f, 0.15f, 0.28f, 0.88f);
                    GUILayout.BeginVertical(saveCardBoxStyle);
                    GUI.color = Color.white;

                    // Linha 1: Ícone de Carreira + Nome do Save + Saldo
                    GUILayout.BeginHorizontal();
                    string careerIcon = save.starterCareer == StarterCareer.Farmer ? "🌾" :
                                        save.starterCareer == StarterCareer.Trucker ? "🚛" :
                                        save.starterCareer == StarterCareer.Aviator ? "🛩️" : "🎣";
                    GUILayout.Label($"{careerIcon} {save.saveName}", labelStyle);
                    GUILayout.Label(save.GetFormattedMoney(), valueStyle);
                    GUILayout.EndHorizontal();

                    // Linha 2: Região, País e Tipo
                    GUI.color = new Color(0.5f, 0.85f, 1.0f, 0.95f);
                    GUILayout.Label($"📍 {save.regionName}, {save.countryName} ({save.regionType})", headerSubtitleStyle);

                    // Linha 3: Data de Salvamento + Reputação
                    GUI.color = new Color(0.7f, 0.82f, 0.95f, 0.80f);
                    GUILayout.Label($"📅 Salvo em: {save.lastSavedDate}  •  ★ {save.reputation} Reputação", headerSubtitleStyle);

                    // Linha 4: Resumo de alterações de status
                    if (save.IsStatsModified)
                    {
                        GUI.color = new Color(1f, 0.85f, 0.4f, 0.9f);
                        GUILayout.Label(save.GetStatChangesSummary(), headerSubtitleStyle);
                    }
                    GUI.color = Color.white;

                    GUILayout.Space(4);

                    // Linha 5: Botões de Ação
                    GUILayout.BeginHorizontal();

                    // Botão 1: Localizar no Globo (Foca câmera e seleciona região no planeta)
                    if (GUILayout.Button("📍 Localizar no Globo", buttonStyle, GUILayout.Height(24)))
                    {
                        FocusAndSelectRegion(save.regionId, true);
                        CloseRecentSavesModal();
                        notificationMessage = $"Região '{save.regionName}' localizada no globo!";
                        notificationTimer = 3.5f;
                    }

                    // Botão 2: Carregar Jogo (Mergulho orbital + tela de loading)
                    if (GUILayout.Button("▶ Carregar", primaryButtonStyle, GUILayout.Height(24)))
                    {
                        LoadSaveGame(save);
                    }

                    // Botão 3: Excluir Partida (com confirmação segura)
                    if (saveIdConfirmDelete == save.saveId)
                    {
                        GUI.color = new Color(1f, 0.35f, 0.35f);
                        if (GUILayout.Button("Confirmar?", dangerButtonStyle, GUILayout.Width(75), GUILayout.Height(24)))
                        {
                            if (SaveManager.Instance != null)
                            {
                                SaveManager.Instance.DeleteSave(save.saveId);
                            }
                            saveIdConfirmDelete = null;
                        }
                        GUI.color = Color.white;
                        if (GUILayout.Button("✕", buttonStyle, GUILayout.Width(24), GUILayout.Height(24)))
                        {
                            saveIdConfirmDelete = null;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("🗑 Excluir", dangerButtonStyle, GUILayout.Width(70), GUILayout.Height(24)))
                        {
                            saveIdConfirmDelete = save.saveId;
                            deleteConfirmTimer = 4.0f;
                        }
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndArea();
            GUI.EndGroup();
        }
    }
}
