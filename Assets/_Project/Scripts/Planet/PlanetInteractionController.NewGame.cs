using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;
using ProjectTerra.Cameras;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void DrawNewGameUI()
        {
            var infra = RegionInfrastructureDatabase.GetInfrastructure(selectedRegion.id, selectedRegion);

            GUILayout.Label("Configurar Nova Partida", headerTitleStyle);
            GUILayout.Label($"Região: {selectedRegion.name} ({selectedRegion.country}) - {infra.environmentCategory}", headerSubtitleStyle);
            GUILayout.Space(4);

            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Validação de viabilidade da carreira de Agricultor (única liberada nesta versão)
            bool canFarm = infra.IsCareerViable(StarterCareer.Farmer, out string farmReason);

            // Trava de versão: somente Agricultor pode ser jogada. Motorista, Aviador e Pescador
            // permanecem bloqueados (futuras adições), então a seleção é sempre forçada para Agricultor.
            selectedCareer = StarterCareer.Farmer;

            // 1. Escolha da Carreira Inicial com Verificação de Infraestrutura
            GUILayout.Label("Escolha sua Primeira Jornada (Ofício Inicial):", labelStyle);
            GUILayout.BeginHorizontal();

            // Botão Agricultor (único disponível)
            if (canFarm)
            {
                GUI.color = new Color(0.3f, 1f, 0.4f);
                if (GUILayout.Button("Agricultor", buttonStyle, GUILayout.Height(26)))
                {
                    selectedCareer = StarterCareer.Farmer;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.4f, 0.4f, 0.7f);
                if (GUILayout.Button("Agricultor [Indisponível]", buttonStyle, GUILayout.Height(26)))
                {
                    notificationMessage = $"[Indisponível] {farmReason}";
                    notificationTimer = 5.0f;
                }
            }

            // Botões bloqueados: Motorista, Aviador e Pescador (futuras adições)
            DrawLockedCareerButton("Motorista");
            DrawLockedCareerButton("Aviador");
            DrawLockedCareerButton("Pescador");

            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Descrição da Carreira Selecionada e Infraestrutura Atendida
            GUI.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            GUILayout.Label($"Fazenda rural com trator e lavoura cultivável. ({farmReason})", headerSubtitleStyle);
            GUI.color = Color.white;

            // Aviso sobre carreiras ainda não liberadas
            GUI.color = new Color(1f, 0.85f, 0.45f, 0.95f);
            GUILayout.Label("Motorista, Aviador e Pescador chegam em futuras atualizações.", headerSubtitleStyle);
            GUI.color = Color.white;

            // Alerta Geográfico caso Agricultor esteja restrito nesta região
            if (!canFarm)
            {
                GUILayout.Space(2);
                GUI.color = new Color(1f, 0.75f, 0.35f, 0.95f);
                GUILayout.Label($"[Aviso] Restrição Agrícola: {farmReason}", headerSubtitleStyle);
                GUI.color = Color.white;
            }

            GUILayout.Space(4);

            // Caixa com Resumo da Infraestrutura Local Detectada
            GUI.color = new Color(0.1f, 0.2f, 0.35f, 0.65f);
            GUILayout.BeginVertical(saveCardBoxStyle);
            GUI.color = new Color(0.7f, 0.9f, 1.0f);
            GUILayout.Label($"Porto: {infra.portName} | Aeroporto: {infra.airportName}", headerSubtitleStyle);
            GUILayout.EndVertical();
            GUI.color = Color.white;
            GUILayout.Space(6);

            // 2. Nome da Partida
            GUILayout.Label("Nome da Empresa / Partida:", labelStyle);
            newGameSaveName = GUILayout.TextField(newGameSaveName, 40, textFieldStyle, GUILayout.Height(24));
            GUILayout.Space(6);

            // 3. Orçamento Inicial
            GUILayout.BeginHorizontal();
            GUILayout.Label("Orçamento Inicial:", labelStyle);
            GUILayout.Label($"${newGameBudget:N0}", valueStyle);
            GUILayout.EndHorizontal();

            // Botões de Presets rápidos de orçamento
            GUILayout.BeginHorizontal();
            for (int i = 0; i < BudgetPresets.Length; i++)
            {
                bool isSelected = newGameBudget == BudgetPresets[i];
                GUI.color = isSelected ? new Color(1f, 0.9f, 0.3f) : Color.white;
                if (GUILayout.Button(BudgetLabels[i], buttonStyle, GUILayout.Height(20)))
                {
                    newGameBudget = BudgetPresets[i];
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            // Slider livre para ajuste fino
            newGameBudget = (long)(GUILayout.HorizontalSlider(newGameBudget, 50000, 10000000) / 10000) * 10000;
            GUILayout.Space(8);

            // Notificação temporária de bloqueio se houver
            if (!string.IsNullOrEmpty(notificationMessage))
            {
                GUI.color = new Color(1f, 0.45f, 0.45f);
                GUILayout.Label(notificationMessage, labelStyle);
                GUI.color = Color.white;
                GUILayout.Space(4);
            }

            // 4. Botão Iniciar Partida com Verificação de Infraestrutura
            if (GUILayout.Button("Iniciar & Desembarcar na Região", primaryButtonStyle, GUILayout.Height(32)))
            {
                // Trava de segurança: impede iniciar em carreira inviável para a região
                if (!infra.IsCareerViable(selectedCareer, out string blockReason))
                {
                    notificationMessage = $"[Indisponível] Impossível iniciar: {blockReason}";
                    notificationTimer = 5.0f;
                    return;
                }

                RegionSaveData save = null;
                if (SaveManager.Instance != null)
                {
                    save = SaveManager.Instance.CreateNewSave(selectedRegion, newGameSaveName, newGameBudget, selectedCareer);
                    SaveManager.Instance.ActiveSave = save;
                }
                currentCardMode = CardMode.RegionDetails;
                hasSelection = false;
                lastCardRect = Rect.zero;

                // Aciona o mergulho cinemático da câmera orbital antes de abrir a tela de carregamento
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

            GUILayout.Space(4);
            if (GUILayout.Button("Voltar aos Detalhes", buttonStyle, GUILayout.Height(22)))
            {
                currentCardMode = CardMode.RegionDetails;
            }
        }

        // Carreira ainda não disponível: botão travado que exibe pop-up de futuras adições.
        private void DrawLockedCareerButton(string label)
        {
            GUI.color = new Color(0.55f, 0.58f, 0.65f, 0.75f);
            if (GUILayout.Button(label + " [Em breve]", buttonStyle, GUILayout.Height(26)))
            {
                notificationMessage = $"[Em breve] Carreira '{label}' em desenvolvimento. Apenas Agricultor está disponível. Novas carreiras em futuras atualizações!";
                notificationTimer = 5.0f;
            }
            GUI.color = Color.white;
        }
    }
}
