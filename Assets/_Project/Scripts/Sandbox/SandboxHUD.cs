using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// HUD imersivo e moderno da Cena Sandbox Regional:
    /// Exibe conta bancária, reputação da empresa, objetivos comerciais com distância da rota,
    /// velocímetro analógico/digital, nível de combustível, capacidade de carga e alertas.
    /// </summary>
    public class SandboxHUD : MonoBehaviour
    {
        private RegionSaveData activeSave;
        private Texture2D whiteTex;

        // Estilos GUI
        private GUIStyle panelBoxStyle;
        private GUIStyle titleStyle;
        private GUIStyle moneyStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle missionTitleStyle;
        private GUIStyle missionDescStyle;
        private GUIStyle speedValueStyle;
        private GUIStyle speedUnitStyle;
        private GUIStyle hintBoxStyle;
        private GUIStyle successBannerStyle;
        private bool stylesReady = false;

        private void Start()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();

            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                activeSave = SaveManager.Instance.ActiveSave;
            }
        }

        private readonly int[] timeWarpPresets = { 1, 2, 4, 8, 16 };
        private int currentTimeWarpIdx = 0;

        private void Update()
        {
            // Atalho de teclado [M] para retornar ao Globo Terrestre
            if (Input.GetKeyDown(KeyCode.M))
            {
                ReturnToGlobe();
            }

            // Atalhos para aceleração de tempo [ e ]
            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Period))
            {
                SetTimeWarp(currentTimeWarpIdx + 1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Comma))
            {
                SetTimeWarp(currentTimeWarpIdx - 1);
            }
        }

        public void SetTimeWarp(int newIdx)
        {
            currentTimeWarpIdx = Mathf.Clamp(newIdx, 0, timeWarpPresets.Length - 1);
            int multiplier = timeWarpPresets[currentTimeWarpIdx];
            Time.timeScale = multiplier;
            Time.fixedDeltaTime = 0.02f * multiplier;
            Debug.Log($"[TimeWarp] Simulação acelerada: {multiplier}x");
        }

        public void ReturnToGlobe()
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            // Salva antes de sair
            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                SaveManager.Instance.SaveToFile(SaveManager.Instance.ActiveSave);
            }
            SceneManager.LoadScene("MainEarthScene");
        }

        private void InitStyles()
        {
            if (stylesReady) return;

            panelBoxStyle = new GUIStyle(GUI.skin.box);
            panelBoxStyle.normal.background = whiteTex;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.white;

            moneyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
            moneyStyle.normal.textColor = new Color(0.3f, 1f, 0.45f);

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };
            subtitleStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);

            badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            badgeStyle.normal.background = whiteTex;
            badgeStyle.normal.textColor = Color.white;

            missionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            missionTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.35f);

            missionDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                wordWrap = true
            };
            missionDescStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

            speedValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            speedValueStyle.normal.textColor = Color.white;

            speedUnitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft
            };
            speedUnitStyle.normal.textColor = new Color(0.6f, 0.8f, 1f);

            hintBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hintBoxStyle.normal.background = whiteTex;
            hintBoxStyle.normal.textColor = Color.white;

            successBannerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            successBannerStyle.normal.background = whiteTex;
            successBannerStyle.normal.textColor = new Color(0.2f, 1f, 0.3f);

            stylesReady = true;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawTopLeftStatusPanel();
            DrawMissionObjectiveTracker();
            DrawVehicleTelemetry();
            DrawContextualInteractionHints();
            DrawSuccessBanner();
        }

        private void DrawTopLeftStatusPanel()
        {
            if (activeSave == null && SaveManager.Instance != null)
            {
                activeSave = SaveManager.Instance.ActiveSave;
            }

            float w = 410f;
            float h = 120f;
            Rect rect = new Rect(20f, 20f, w, h);

            // Fundo escuro com borda azul ciano
            GUI.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.25f, 0.65f, 1.0f, 0.8f);
            GUI.DrawTexture(new Rect(20f, 20f, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(30f, 25f, w - 20f, h - 10f));

            // Linha 1: Região e País + Dimensões 1:1 + Botão Globo
            GUILayout.BeginHorizontal();
            string regName = activeSave != null ? activeSave.regionName : "Região Sandbox";
            string country = activeSave != null ? activeSave.countryName : "Mundo";
            GUILayout.Label($"🌍 {regName}, {country}", titleStyle);

            GUI.color = new Color(0.85f, 0.92f, 1f);
            if (GUILayout.Button("🌐 Globo [M]", GUILayout.Width(85), GUILayout.Height(22)))
            {
                ReturnToGlobe();
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            // Linha 2: Dimensões Reais 1:1 do Território
            float wKm = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.worldWidthMeters / 1000f : 60f;
            float lKm = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.worldLengthMeters / 1000f : 80f;
            int area = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.realAreaKm2 : 4800;
            GUI.color = new Color(0.35f, 0.9f, 1.0f);
            GUILayout.Label($"📏 Escala Real 1:1: {wKm:F0} km × {lKm:F0} km  •  Área: {area:N0} km²", subtitleStyle);
            GUI.color = Color.white;

            // Linha 3: Carreira & Reputação
            string careerLabel = GetCareerBadge();
            int rep = activeSave != null ? activeSave.reputation : 100;
            int deliveries = activeSave != null ? activeSave.completedDeliveries : 0;
            GUILayout.Label($"{careerLabel}  •  ⭐ Reputação: {rep}  •  📦 Entregas: {deliveries}", subtitleStyle);

            // Linha 4: Saldo Bancário e Controle de Time Warp
            GUILayout.BeginHorizontal();
            string money = activeSave != null ? activeSave.GetFormattedMoney() : "$500,000";
            GUILayout.Label($"Conta: {money}", moneyStyle);
            GUILayout.FlexibleSpace();

            // Botões de Time Warp
            int curMultiplier = timeWarpPresets[currentTimeWarpIdx];
            GUI.color = curMultiplier > 1 ? new Color(1f, 0.85f, 0.2f) : Color.white;
            if (GUILayout.Button("◀", GUILayout.Width(24), GUILayout.Height(20)))
            {
                SetTimeWarp(currentTimeWarpIdx - 1);
            }
            GUILayout.Label($"⏩ {curMultiplier}x", subtitleStyle, GUILayout.Width(45));
            if (GUILayout.Button("▶", GUILayout.Width(24), GUILayout.Height(20)))
            {
                SetTimeWarp(currentTimeWarpIdx + 1);
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private string GetCareerBadge()
        {
            if (activeSave == null) return "🌾 Agricultor";
            switch (activeSave.starterCareer)
            {
                case StarterCareer.Farmer: return "🌾 Agricultor";
                case StarterCareer.Trucker: return "🚛 Motorista";
                case StarterCareer.Aviator: return "🛩️ Aviador";
                case StarterCareer.Fisherman: return "🎣 Pescador";
                default: return "🌾 Produtor";
            }
        }

        private void DrawMissionObjectiveTracker()
        {
            if (MissionManager.Instance == null || !MissionManager.Instance.hasActiveContract || MissionManager.Instance.currentContract == null)
                return;

            var contract = MissionManager.Instance.currentContract;
            if (contract.isCompleted) return;

            float w = 480f;
            float h = 85f;
            Rect rect = new Rect((Screen.width - w) * 0.5f, 20f, w, h);

            // Fundo escuro com borda dourada
            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.90f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(1f, 0.8f, 0.2f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 15f, rect.y + 8f, w - 30f, h - 16f));

            GUILayout.BeginHorizontal();
            GUILayout.Label(contract.title, missionTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Recompensa: +${contract.rewardMoney:N0}", moneyStyle);
            GUILayout.EndHorizontal();

            float dist = MissionManager.Instance.GetDistanceToDestination();
            string distText = dist > 1000f ? $"{(dist / 1000f):F1} km" : $"{dist:F0} m";

            GUILayout.Label($"📍 Destino: {contract.destinationName} ({distText})  •  Carga: {contract.cargoType} ({contract.cargoWeightKg:N0} kg)", subtitleStyle);
            GUILayout.Label(contract.description, missionDescStyle);

            GUILayout.EndArea();
        }

        private void DrawVehicleTelemetry()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null || !player.isDriving || player.currentVehicle == null) return;

            var vehicle = player.currentVehicle;

            float w = 260f;
            float h = 135f;
            Rect rect = new Rect(Screen.width - w - 25f, Screen.height - h - 25f, w, h);

            GUI.color = new Color(0.04f, 0.07f, 0.14f, 0.92f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.2f, 0.7f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 15f, rect.y + 10f, w - 30f, h - 20f));

            // Nome do Veículo
            GUILayout.Label(vehicle.vehicleName, subtitleStyle);

            // Velocímetro
            GUILayout.BeginHorizontal();
            string unit = vehicle.category == VehicleCategory.Boat ? "NÓS" : "KM/H";
            float speedDisplay = vehicle.category == VehicleCategory.Boat ? vehicle.currentSpeedKmh * 0.54f : vehicle.currentSpeedKmh;
            GUILayout.Label($"{speedDisplay:F0}", speedValueStyle, GUILayout.Width(75));
            GUILayout.Label(unit, speedUnitStyle, GUILayout.Height(30));
            GUILayout.EndHorizontal();

            // Barra de Combustível
            DrawBar("⛽ Tanque:", vehicle.fuelPercent, 100f, new Color(0.95f, 0.65f, 0.15f));

            // Barra de Carga
            DrawBar($"📦 Carga ({vehicle.cargoItemName}):", vehicle.cargoFillPercent, 100f, new Color(0.35f, 0.85f, 0.45f));

            GUILayout.EndArea();
        }

        private void DrawBar(string label, float current, float max, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, subtitleStyle, GUILayout.Width(130));
            GUILayout.Label($"{current:F0}%", subtitleStyle, GUILayout.Width(35));
            GUILayout.EndHorizontal();

            Rect barBg = GUILayoutUtility.GetRect(180, 5);
            GUI.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            GUI.DrawTexture(barBg, whiteTex);

            float fillWidth = (current / max) * barBg.width;
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barBg.x, barBg.y, fillWidth, barBg.height), whiteTex);
            GUI.color = Color.white;
            GUILayout.Space(2);
        }

        private void DrawContextualInteractionHints()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            string hintText = "";
            Color borderColor = new Color(0.2f, 0.7f, 1f, 0.8f);

            if (player.isDriving)
            {
                string camModeStr = player.vehicleCameraMode == VehicleCameraMode.FirstPerson ? "1ª Pessoa (Cockpit)" : "3ª Pessoa (Externa)";
                // Se chegou ao ponto de entrega da missão
                if (MissionManager.Instance != null && MissionManager.Instance.GetDistanceToDestination() <= MissionManager.Instance.deliveryRadius)
                {
                    hintText = $"✅ [F/ENTER] Descarregar  •  [V/C] Câmera: {camModeStr}  •  [E] Sair";
                    borderColor = new Color(0.3f, 1f, 0.4f);
                }
                else
                {
                    hintText = $"[WASD] Conduzir  •  [V/C] Câmera: {camModeStr}  •  [E] Sair";
                }
            }
            else if (player.nearbyVehicle != null)
            {
                hintText = $"🔑 [E] Embarcar no {player.nearbyVehicle.vehicleName}  •  Câmera: 1ª Pessoa";
                borderColor = new Color(1f, 0.85f, 0.2f);
            }
            else
            {
                hintText = "[WASD] Mover  •  [SHIFT] Correr  •  [ESPAÇO] Pular  •  Câmera: 1ª Pessoa";
                borderColor = new Color(0.2f, 0.65f, 0.95f, 0.75f);
            }

            if (!string.IsNullOrEmpty(hintText))
            {
                float w = 580f;
                float h = 34f;
                Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 35f, w, h);

                GUI.color = new Color(0.04f, 0.08f, 0.16f, 0.94f);
                GUI.DrawTexture(rect, whiteTex);
                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
                GUI.color = Color.white;

                GUI.Label(rect, hintText, hintBoxStyle);
            }
        }

        private void DrawSuccessBanner()
        {
            if (MissionManager.Instance == null || MissionManager.Instance.completionBannerTimer <= 0f) return;

            float w = 550f;
            float h = 75f;
            Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.35f, w, h);

            GUI.color = new Color(0.05f, 0.25f, 0.12f, 0.95f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.3f, 1f, 0.4f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 3), whiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y + h - 3, w, 3), whiteTex);
            GUI.color = Color.white;

            GUI.Label(rect, MissionManager.Instance.completionBannerMessage, successBannerStyle);
        }
    }
}
