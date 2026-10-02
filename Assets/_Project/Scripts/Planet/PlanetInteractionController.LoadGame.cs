using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;
using ProjectTerra.Cameras;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void DrawLoadGameUI()
        {
            GUILayout.Label("💾 Carregar Partida Salva", headerTitleStyle);
            GUILayout.Label($"Região: {selectedRegion.name} ({selectedRegion.country})", headerSubtitleStyle);
            GUILayout.Space(6);

            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            List<RegionSaveData> saves = SaveManager.Instance != null ?
                SaveManager.Instance.GetSavesForRegion(selectedRegion.id) : new List<RegionSaveData>();

            if (saves.Count == 0)
            {
                GUILayout.Space(20);
                GUILayout.Label("Nenhuma partida salva encontrada para esta região.", headerSubtitleStyle);
                GUILayout.Space(20);
            }
            else
            {
                loadScrollPos = GUILayout.BeginScrollView(loadScrollPos, GUILayout.Height(200));

                foreach (var save in saves)
                {
                    // Mini card para cada save
                    GUI.color = new Color(0.08f, 0.15f, 0.28f, 0.85f);
                    GUILayout.BeginVertical(saveCardBoxStyle);
                    GUI.color = Color.white;

                    // Título do save, Carreira e Verba
                    GUILayout.BeginHorizontal();
                    string icon = save.starterCareer == StarterCareer.Farmer ? "🌾" :
                                  save.starterCareer == StarterCareer.Trucker ? "🚛" :
                                  save.starterCareer == StarterCareer.Aviator ? "🛩️" : "🎣";
                    GUILayout.Label($"{icon} {save.saveName}", labelStyle);
                    GUILayout.Label(save.GetFormattedMoney(), valueStyle);
                    GUILayout.EndHorizontal();

                    // Data e Hora
                    GUI.color = new Color(0.7f, 0.85f, 1f, 0.8f);
                    GUILayout.Label($"📅 Salvo em: {save.lastSavedDate}", headerSubtitleStyle);

                    // Status / Alterações regionais
                    string statusSummary = save.GetStatChangesSummary();
                    GUI.color = save.IsStatsModified ? new Color(1f, 0.85f, 0.4f) : new Color(0.6f, 0.7f, 0.8f);
                    GUILayout.Label(statusSummary, headerSubtitleStyle);
                    GUI.color = Color.white;

                    // Botões Carregar e Excluir
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("▶ Carregar & Entrar", primaryButtonStyle, GUILayout.Height(22)))
                    {
                        if (SaveManager.Instance != null)
                        {
                            SaveManager.Instance.ActiveSave = save;
                        }
                        notificationMessage = $"Partida '{save.saveName}' carregada! Verba: {save.GetFormattedMoney()}";
                        notificationTimer = 4.0f;
                        currentCardMode = CardMode.RegionDetails;
                        hasSelection = false;
                        lastCardRect = Rect.zero;

                        // Mergulho e transição
                        OrbitCameraController orbitCam = mainCamera != null ? mainCamera.GetComponent<OrbitCameraController>() : null;
                        if (orbitCam != null && selectedWorldPoint != Vector3.zero)
                        {
                            orbitCam.DiveTowardsPoint(selectedWorldPoint, 1.8f, () =>
                            {
                                if (LoadingScreenController.Instance != null)
                                {
                                    LoadingScreenController.Instance.Show(selectedRegion, save, () =>
                                    {
                                        SceneManager.LoadScene("RegionalSandboxScene");
                                    });
                                }
                            });
                        }
                        else
                        {
                            if (LoadingScreenController.Instance != null)
                            {
                                LoadingScreenController.Instance.Show(selectedRegion, save, () =>
                                {
                                    SceneManager.LoadScene("RegionalSandboxScene");
                                });
                            }
                        }
                    }
                    if (GUILayout.Button("🗑 Excluir", dangerButtonStyle, GUILayout.Width(70), GUILayout.Height(22)))
                    {
                        if (SaveManager.Instance != null)
                        {
                            SaveManager.Instance.DeleteSave(save.saveId);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                }

                GUILayout.EndScrollView();
            }

            GUILayout.Space(6);
            if (GUILayout.Button("↩ Voltar aos Detalhes", buttonStyle, GUILayout.Height(26)))
            {
                currentCardMode = CardMode.RegionDetails;
            }
        }
    }
}
