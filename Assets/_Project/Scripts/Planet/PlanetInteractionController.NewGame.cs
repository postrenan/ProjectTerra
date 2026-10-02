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

            GUILayout.Label("🎮 Configurar Nova Partida", headerTitleStyle);
            GUILayout.Label($"Região: {selectedRegion.name} ({selectedRegion.country}) • {infra.environmentCategory}", headerSubtitleStyle);
            GUILayout.Space(4);

            GUI.color = new Color(1f, 1f, 1f, 0.15f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(4);

            // Validação de viabilidade de cada carreira
            bool canFarm = infra.IsCareerViable(StarterCareer.Farmer, out string farmReason);
            bool canTruck = infra.IsCareerViable(StarterCareer.Trucker, out string truckReason);
            bool canFly = infra.IsCareerViable(StarterCareer.Aviator, out string flyReason);
            bool canFish = infra.IsCareerViable(StarterCareer.Fisherman, out string fishReason);

            // Auto-fallback: Se a carreira selecionada atualmente for inviável nesta região, troca automaticamente
            if (!infra.IsCareerViable(selectedCareer, out _))
            {
                if (canFarm) selectedCareer = StarterCareer.Farmer;
                else if (canTruck) selectedCareer = StarterCareer.Trucker;
                else if (canFly) selectedCareer = StarterCareer.Aviator;
                else if (canFish) selectedCareer = StarterCareer.Fisherman;
            }

            // 1. Escolha da Carreira Inicial com Verificação de Infraestrutura
            GUILayout.Label("Escolha sua Primeira Jornada (Ofício Inicial):", labelStyle);
            GUILayout.BeginHorizontal();

            // Botão Agricultor
            if (canFarm)
            {
                bool isFarmer = selectedCareer == StarterCareer.Farmer;
                GUI.color = isFarmer ? new Color(0.3f, 1f, 0.4f) : Color.white;
                if (GUILayout.Button("🌾 Agricultor", buttonStyle, GUILayout.Height(26)))
                {
                    selectedCareer = StarterCareer.Farmer;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.4f, 0.4f, 0.7f);
                if (GUILayout.Button("🌾 Agricultor 🚫", buttonStyle, GUILayout.Height(26)))
                {
                    notificationMessage = $"🚫 {farmReason}";
                    notificationTimer = 5.0f;
                }
            }

            // Botão Motorista
            if (canTruck)
            {
                bool isTrucker = selectedCareer == StarterCareer.Trucker;
                GUI.color = isTrucker ? new Color(1f, 0.85f, 0.3f) : Color.white;
                if (GUILayout.Button("🚛 Motorista", buttonStyle, GUILayout.Height(26)))
                {
                    selectedCareer = StarterCareer.Trucker;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.4f, 0.4f, 0.7f);
                if (GUILayout.Button("🚛 Motorista 🚫", buttonStyle, GUILayout.Height(26)))
                {
                    notificationMessage = $"🚫 {truckReason}";
                    notificationTimer = 5.0f;
                }
            }

            // Botão Aviador
            if (canFly)
            {
                bool isAviator = selectedCareer == StarterCareer.Aviator;
                GUI.color = isAviator ? new Color(0.4f, 0.85f, 1f) : Color.white;
                if (GUILayout.Button("🛩️ Aviador", buttonStyle, GUILayout.Height(26)))
                {
                    selectedCareer = StarterCareer.Aviator;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.4f, 0.4f, 0.7f);
                if (GUILayout.Button("🛩️ Aviador 🚫", buttonStyle, GUILayout.Height(26)))
                {
                    notificationMessage = $"🚫 {flyReason}";
                    notificationTimer = 5.0f;
                }
            }

            // Botão Pescador
            if (canFish)
            {
                bool isFisherman = selectedCareer == StarterCareer.Fisherman;
                GUI.color = isFisherman ? new Color(0.3f, 0.95f, 0.95f) : Color.white;
                if (GUILayout.Button("🎣 Pescador", buttonStyle, GUILayout.Height(26)))
                {
                    selectedCareer = StarterCareer.Fisherman;
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.4f, 0.4f, 0.7f);
                if (GUILayout.Button("🎣 Pescador 🚫", buttonStyle, GUILayout.Height(26)))
                {
                    notificationMessage = $"🚫 {fishReason}";
                    notificationTimer = 5.0f;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Descrição da Carreira Selecionada e Infraestrutura Atendida
            GUI.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            string careerDesc = "";
            switch (selectedCareer)
            {
                case StarterCareer.Farmer:
                    careerDesc = $"🌾 Fazenda rural com trator e lavoura cultivável. ({farmReason})";
                    break;
                case StarterCareer.Trucker:
                    careerDesc = $"🚛 Pátio logístico e fretes rodoviários intermunicipais. ({truckReason})";
                    break;
                case StarterCareer.Aviator:
                    careerDesc = $"🛩️ Base aérea em '{infra.airportName}' com monomotor de carga. ({flyReason})";
                    break;
                case StarterCareer.Fisherman:
                    careerDesc = $"🎣 Cais em '{infra.portName}' para pesca comercial em {infra.waterBodyName}. ({fishReason})";
                    break;
            }
            GUILayout.Label(careerDesc, headerSubtitleStyle);
            GUI.color = Color.white;

            // Alerta Geográfico caso haja opções restritas
            if (!canFish || !canFarm)
            {
                GUILayout.Space(2);
                GUI.color = new Color(1f, 0.75f, 0.35f, 0.95f);
                if (!canFish)
                {
                    GUILayout.Label($"⚠️ Restrição Hídrica: {fishReason}", headerSubtitleStyle);
                }
                if (!canFarm)
                {
                    GUILayout.Label($"⚠️ Restrição Agrícola: {farmReason}", headerSubtitleStyle);
                }
                GUI.color = Color.white;
            }

            GUILayout.Space(4);

            // Caixa com Resumo da Infraestrutura Local Detectada
            GUI.color = new Color(0.1f, 0.2f, 0.35f, 0.65f);
            GUILayout.BeginVertical(saveCardBoxStyle);
            GUI.color = new Color(0.7f, 0.9f, 1.0f);
            GUILayout.Label($"⚓ Porto: {infra.portName}  |  🛫 Aeroporto: {infra.airportName}", headerSubtitleStyle);
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
            if (GUILayout.Button("▶ Iniciar & Desembarcar na Região", primaryButtonStyle, GUILayout.Height(32)))
            {
                // Trava de segurança: impede iniciar em carreira inviável para a região
                if (!infra.IsCareerViable(selectedCareer, out string blockReason))
                {
                    notificationMessage = $"🚫 Impossível iniciar: {blockReason}";
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
            if (GUILayout.Button("↩ Voltar aos Detalhes", buttonStyle, GUILayout.Height(22)))
            {
                currentCardMode = CardMode.RegionDetails;
            }
        }
    }
}
