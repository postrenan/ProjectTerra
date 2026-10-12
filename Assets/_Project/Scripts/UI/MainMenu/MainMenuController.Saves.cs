using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        private Vector2 savesScrollPos = Vector2.zero;
        private string deleteSaveIdConfirm = null;
        private float deleteSaveTimer = 0f;

        private void DrawSavesListScreen()
        {
            DrawModalHeader("PARTIDAS SALVAS", "Gerencie e continue qualquer uma de suas expedições registradas na Terra");

            List<RegionSaveData> saves = SaveManager.Instance != null
                ? SaveManager.Instance.GetAllSaves()
                : new List<RegionSaveData>();

            if (saves == null || saves.Count == 0)
            {
                GUILayout.Space(40);
                GUILayout.Label("Nenhuma partida salva encontrada no momento.", labelBoldStyle);
                GUILayout.Label("Inicie uma nova expedição pelo menu 'Novo Jogo' para registrar suas conquistas territoriais.", labelRegularStyle);
                GUILayout.Space(20);

                if (GUILayout.Button("Criar Novo Jogo Agora", primaryActionBtnStyle, GUILayout.Width(220), GUILayout.Height(36)))
                {
                    OpenScreen(MenuScreen.NewGame);
                }
                return;
            }

            // Contador de Saves
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Total de Partidas Gravadas: {saves.Count}", sectionHeaderStyle);
            GUILayout.FlexibleSpace();
            if (IconBtn(TerraIcon.FastForward, "Atualizar Lista", actionBtnStyle, GUILayout.Width(130), GUILayout.Height(24)))
            {
                SaveManager.Instance.LoadAllSaves();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            savesScrollPos = GUILayout.BeginScrollView(savesScrollPos);

            for (int i = 0; i < saves.Count; i++)
            {
                var s = saves[i];

                GUI.color = new Color(0.04f, 0.10f, 0.20f, 0.90f);
                GUILayout.BeginVertical(cardStyle);
                GUI.color = Color.white;

                // Linha 1: Título e Dinheiro
                GUILayout.BeginHorizontal();
                IconLbl(CareerIcon(s.starterCareer), s.saveName, labelBoldStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(s.GetFormattedMoney(), valueHighlightStyle);
                GUILayout.EndHorizontal();

                // Linha 2: Região e País
                GUILayout.Label($"Região: {s.regionName} ({s.countryName}) - Carreira: {s.starterCareer}", labelRegularStyle);

                // Linha 3: Data de Salvamento
                GUILayout.Label($"Último salvamento: {s.lastSavedDate} - Reputação: {s.reputation}", menuBtnSubtextStyle);

                GUILayout.Space(6);

                // Botões de Ação
                GUILayout.BeginHorizontal();

                // Botão Carregar
                GUI.color = new Color(0.2f, 0.85f, 0.45f);
                if (IconBtn(TerraIcon.FastForward, "Carregar Partida", primaryActionBtnStyle, GUILayout.Height(26)))
                {
                    ContinueGame(s);
                }
                GUI.color = Color.white;

                GUILayout.FlexibleSpace();

                // Botão Excluir com confirmação
                if (deleteSaveIdConfirm == s.saveId)
                {
                    deleteSaveTimer -= Time.deltaTime;
                    if (deleteSaveTimer <= 0f) deleteSaveIdConfirm = null;

                    GUI.color = new Color(1f, 0.3f, 0.3f);
                    if (GUILayout.Button("Confirmar Exclusão?", dangerActionBtnStyle, GUILayout.Width(140), GUILayout.Height(26)))
                    {
                        SaveManager.Instance.DeleteSave(s.saveId);
                        deleteSaveIdConfirm = null;
                        ShowNotification("Partida excluída com sucesso.");
                    }
                    GUI.color = Color.white;

                    if (GUILayout.Button("Cancelar", actionBtnStyle, GUILayout.Width(70), GUILayout.Height(26)))
                    {
                        deleteSaveIdConfirm = null;
                    }
                }
                else
                {
                    GUI.color = new Color(0.7f, 0.3f, 0.3f);
                    if (IconBtn(TerraIcon.Trash, "Excluir", dangerActionBtnStyle, GUILayout.Width(80), GUILayout.Height(26)))
                    {
                        deleteSaveIdConfirm = s.saveId;
                        deleteSaveTimer = 4.0f;
                    }
                    GUI.color = Color.white;
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(6);
            }

            GUILayout.EndScrollView();
        }
    }
}
