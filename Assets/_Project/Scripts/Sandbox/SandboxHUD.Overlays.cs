using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Componente 1: Status Pill (Topo Esquerdo Redesenhado)

        private void DrawTopLeftStatusPill()
        {
            if (activeSave == null && SaveManager.Instance != null)
            {
                activeSave = SaveManager.Instance.ActiveSave;
            }

            float w = 310f;
            float h = 50f;
            Rect rect = new Rect(20f, 15f, w, h);

            // Fundo escuro aerodinâmico com linha ciano sutil
            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.90f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.2f, 0.7f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 5f, w - 20f, h - 8f));

            // Linha 1: Região, País e Horário Solar
            string regName = activeSave != null ? activeSave.regionName : "Região";
            string country = activeSave != null ? activeSave.countryName : "Mundo";
            float curHour = HDRPAtmosphereController.Instance != null ? HDRPAtmosphereController.Instance.timeOfDay : 12f;
            int hInt = Mathf.FloorToInt(curHour);
            int mInt = Mathf.FloorToInt((curHour - hInt) * 60f);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"🌍 {regName}, {country}", titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"🕒 {hInt:D2}:{mInt:D2}", subtitleStyle);
            GUILayout.EndHorizontal();

            // Linha 2: Saldo + Time Warp compacto
            GUILayout.BeginHorizontal();
            string money = activeSave != null ? activeSave.GetFormattedMoney() : "$500,000";
            GUILayout.Label(money, moneyStyle);
            GUILayout.FlexibleSpace();

            int curMultiplier = timeWarpPresets[currentTimeWarpIdx];
            if (GUILayout.Button("◀", GUILayout.Width(20), GUILayout.Height(18)))
            {
                SetTimeWarp(currentTimeWarpIdx - 1);
            }
            GUILayout.Label($"⏩ {curMultiplier}x", timeWarpStyle, GUILayout.Width(45));
            if (GUILayout.Button("▶", GUILayout.Width(20), GUILayout.Height(18)))
            {
                SetTimeWarp(currentTimeWarpIdx + 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        #endregion

        #region Componente 2: Bússola 360° no Topo Central (Compass Ribbon)

        private void DrawTopCompassTape()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float heading = cam.transform.eulerAngles.y;
            float w = 340f;
            float h = 32f;
            float x = (Screen.width - w) * 0.5f;
            float y = 15f;
            Rect rect = new Rect(x, y, w, h);

            // Fundo da fita com borda superior
            GUI.color = new Color(0.04f, 0.07f, 0.12f, 0.85f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.3f, 0.75f, 1f, 0.6f);
            GUI.DrawTexture(new Rect(x, y + h - 1, w, 1), whiteTex);
            // Marcador central (Triângulo/Linha do Norte)
            GUI.color = new Color(1f, 0.85f, 0.25f, 0.95f);
            GUI.DrawTexture(new Rect(x + w * 0.5f - 1, y, 2, h), whiteTex);
            GUI.color = Color.white;

            // Marcadores de Pontos Cardeais na fita
            string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            GUI.BeginGroup(rect);
            for (int i = 0; i < angles.Length; i++)
            {
                float diff = Mathf.DeltaAngle(heading, angles[i]);
                if (Mathf.Abs(diff) <= 60f)
                {
                    float posX = (w * 0.5f) + (diff / 60f) * (w * 0.48f);
                    GUI.Label(new Rect(posX - 15f, 4f, 30f, 24f), cardinals[i], compassLabelStyle);
                }
            }

            // Marcador do Objetivo de Missão na Bússola
            if (MissionManager.Instance != null && MissionManager.Instance.hasActiveContract && !MissionManager.Instance.currentContract.isCompleted)
            {
                Vector3 toDest = MissionManager.Instance.deliveryTargetPosition - cam.transform.position;
                toDest.y = 0;
                float targetAngle = Vector3.SignedAngle(Vector3.forward, toDest, Vector3.up);
                float diff = Mathf.DeltaAngle(heading, targetAngle);
                if (Mathf.Abs(diff) <= 60f)
                {
                    float posX = (w * 0.5f) + (diff / 60f) * (w * 0.48f);
                    GUI.color = new Color(1f, 0.85f, 0.2f);
                    GUI.Label(new Rect(posX - 12f, 12f, 24f, 18f), "🎯", compassLabelStyle);
                    GUI.color = Color.white;
                }
            }

            // Marcador de Waypoint Customizado na Bússola
            if (CustomWaypoint.HasValue)
            {
                Vector3 toWp = CustomWaypoint.Value - cam.transform.position;
                toWp.y = 0;
                float wpAngle = Vector3.SignedAngle(Vector3.forward, toWp, Vector3.up);
                float diff = Mathf.DeltaAngle(heading, wpAngle);
                if (Mathf.Abs(diff) <= 60f)
                {
                    float posX = (w * 0.5f) + (diff / 60f) * (w * 0.48f);
                    GUI.color = new Color(0.9f, 0.3f, 1f);
                    GUI.Label(new Rect(posX - 12f, 12f, 24f, 18f), "🚩", compassLabelStyle);
                    GUI.color = Color.white;
                }
            }

            GUI.EndGroup();
        }

        #endregion

        #region Componente 3: Rastreador de Missão no Topo Direito (Card Discreto)

        private void DrawTopRightMissionCard()
        {
            if (MissionManager.Instance == null || !MissionManager.Instance.hasActiveContract || MissionManager.Instance.currentContract == null)
                return;

            var contract = MissionManager.Instance.currentContract;
            if (contract.isCompleted) return;

            float w = 310f;
            float h = 76f;
            Rect rect = new Rect(Screen.width - w - 20f, 15f, w, h);

            GUI.color = new Color(0.05f, 0.08f, 0.14f, 0.90f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(1f, 0.82f, 0.25f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 6f, w - 24f, h - 10f));

            GUILayout.BeginHorizontal();
            GUILayout.Label(contract.title, missionTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"+${contract.rewardMoney:N0}", moneyStyle);
            GUILayout.EndHorizontal();

            float dist = MissionManager.Instance.GetDistanceToDestination();
            string distText = dist > 1000f ? $"{(dist / 1000f):F1} km" : $"{dist:F0} m";

            GUILayout.Label($"📍 {contract.destinationName} ({distText})  •  📦 {contract.cargoType}", subtitleStyle);
            GUILayout.Label(contract.description, missionDescStyle);

            GUILayout.EndArea();
        }

        #endregion

        #region Componente 4: Telemetria de Veículo Redesenhada (Canto Inferior Direito)

        private void DrawVehicleTelemetry()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null || !player.isDriving || player.currentVehicle == null) return;

            var vehicle = player.currentVehicle;

            float w = 240f;
            float h = 105f;
            Rect rect = new Rect(Screen.width - w - 20f, Screen.height - h - 20f, w, h);

            GUI.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.2f, 0.7f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 8f, w - 24f, h - 14f));

            GUILayout.BeginHorizontal();
            GUILayout.Label(vehicle.vehicleName, subtitleStyle);
            GUILayout.FlexibleSpace();
            string unit = vehicle.category == VehicleCategory.Boat ? "NÓS" : "KM/H";
            float speedDisplay = vehicle.category == VehicleCategory.Boat ? vehicle.currentSpeedKmh * 0.54f : vehicle.currentSpeedKmh;
            GUILayout.Label($"{speedDisplay:F0}", speedValueStyle, GUILayout.Width(60));
            GUILayout.Label(unit, speedUnitStyle, GUILayout.Height(24));
            GUILayout.EndHorizontal();

            // Barra de Combustível com cor dinâmica
            Color fuelCol = vehicle.fuelPercent > 45f ? new Color(0.3f, 0.85f, 0.4f) :
                           (vehicle.fuelPercent > 20f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));
            DrawMiniBar("⛽ Tanque:", vehicle.fuelPercent, 100f, fuelCol);

            // Barra de Carga
            DrawMiniBar($"📦 Carga:", vehicle.cargoFillPercent, 100f, new Color(0.3f, 0.7f, 1.0f));

            GUILayout.EndArea();
        }

        private void DrawMiniBar(string label, float current, float max, Color barColor)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, subtitleStyle, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{current:F0}%", subtitleStyle);
            GUILayout.EndHorizontal();

            Rect barBg = GUILayoutUtility.GetRect(160, 4);
            GUI.color = new Color(0.18f, 0.22f, 0.28f, 0.9f);
            GUI.DrawTexture(barBg, whiteTex);

            float fillWidth = Mathf.Clamp01(current / max) * barBg.width;
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barBg.x, barBg.y, fillWidth, barBg.height), whiteTex);
            GUI.color = Color.white;
            GUILayout.Space(2);
        }

        #endregion

        #region Componente 5: Dicas Contextuais & Teclas de Atalho

        private void DrawContextualHints()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            string hintText = "";
            Color borderColor = new Color(0.2f, 0.7f, 1f, 0.8f);

            if (player.isDriving)
            {
                if (MissionManager.Instance != null && MissionManager.Instance.GetDistanceToDestination() <= MissionManager.Instance.deliveryRadius)
                {
                    hintText = "✅ [F/ENTER] Descarregar Entrega  •  [C] Câmera  •  [L] Faróis  •  [Tab] Trocar  •  [E] Sair";
                    borderColor = new Color(0.3f, 1f, 0.4f);
                }
                else
                {
                    hintText = "🎮 [WASD] Conduzir  •  [C] Câmera  •  [L] Faróis  •  [Tab] Trocar Veículo  •  [E] Desembarcar";
                    borderColor = new Color(0.2f, 0.7f, 1f, 0.8f);
                }
            }
            else if (player.nearbyVehicle != null)
            {
                hintText = $"🔑 [E] Embarcar no {player.nearbyVehicle.vehicleName}  •  [L] Lanterna  •  [Tab] Alternar";
                borderColor = new Color(1f, 0.85f, 0.2f);
            }

            if (!string.IsNullOrEmpty(hintText))
            {
                float w = 550f;
                float h = 32f;
                Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height - h - 45f, w, h);

                GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.94f);
                GUI.DrawTexture(rect, whiteTex);
                GUI.color = borderColor;
                GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
                GUI.color = Color.white;

                GUI.Label(rect, hintText, hintBoxStyle);
            }
        }

        private void DrawSubtleKeyShortcuts()
        {
            // Barra discreta no rodapé esquerdo com atalhos principais
            float w = 560f;
            float h = 20f;
            Rect rect = new Rect(20f, Screen.height - h - 12f, w, h);
            GUI.color = new Color(0.6f, 0.75f, 0.9f, 0.85f);
            GUI.Label(rect, "[M] Mapa  •  [/] Comandos  •  [L] Lanterna/Farol  •  [Tab] Veículos  •  [C] Câmera  •  [Alt] Menu", subtitleStyle);
            GUI.color = Color.white;
        }

        #endregion
    }
}
