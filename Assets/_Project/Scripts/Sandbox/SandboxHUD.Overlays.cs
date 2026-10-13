using UnityEngine;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        private static readonly Color HudBoxBg = new Color(0.04f, 0.08f, 0.14f, 0.90f);
        private static readonly Color HudBoxBorder = new Color(0.35f, 0.65f, 0.95f, 0.75f);

        #region 1. Topo Esquerdo: Ajuda de Botões (Print 1 & Print 2)

        private void DrawTopLeftButtonHelp()
        {
            var player = PlayerCharacterController.Instance;
            bool inVehicle = player != null && player.isDriving && player.currentVehicle != null;

            float w = inVehicle ? 360f : 340f;
            float h = 48f;
            Rect rect = new Rect(20f, 15f, w, h);

            UIThemeHelper.DrawRoundedBox(rect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 4f, w - 20f, h - 8f));

            // Cabeçalho da Caixa
            GUILayout.BeginHorizontal();
            HudLayoutIcon(TerraIcon.Tools, 13f, new Color(0.45f, 0.85f, 1f));
            GUILayout.Space(4);
            GUILayout.Label("AJUDA DE BOTÕES", hudBoxTitleStyle);
            GUILayout.EndHorizontal();

            // Atalhos Contextuais
            GUILayout.BeginHorizontal();
            if (inVehicle)
            {
                var v = player.currentVehicle;
                if (v.category == VehicleCategory.Plane)
                {
                    GUILayout.Label("[W/S] Arfagem  [A/D] Rolar  [Shift/Ctrl] Potência  [E] Sair", hudBoxSubStyle);
                }
                else if (v.category == VehicleCategory.Tractor)
                {
                    GUILayout.Label("[WASD] Conduzir  [E] Sair  [G] Engatar  [X] Arado  [C] Câm.", hudBoxSubStyle);
                }
                else if (v.category == VehicleCategory.Boat)
                {
                    GUILayout.Label("[WASD] Navegar  [E] Sair  [C] Câmera  [M] Mapa  [Alt] Celular", hudBoxSubStyle);
                }
                else
                {
                    GUILayout.Label("[WASD] Conduzir  [E] Sair  [L] Faróis  [C] Câmera  [M] Mapa", hudBoxSubStyle);
                }
            }
            else
            {
                if (player != null && player.nearbyVehicle != null)
                {
                    GUILayout.Label("[E] Entrar no Veículo  [WASD] Andar  [Shift] Correr  [M] Mapa", hudBoxSubStyle);
                }
                else
                {
                    GUILayout.Label("[WASD] Andar  [Shift] Correr  [Espaço] Pular  [E] Usar  [M] Mapa", hudBoxSubStyle);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        #endregion

        #region 2. Topo Centro: Bússola (Print 1 & Print 2)

        private void DrawTopCenterCompass()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float heading = cam.transform.eulerAngles.y;
            float w = 340f;
            float h = 48f;
            float x = (Screen.width - w) * 0.5f;
            float y = 15f;
            Rect rect = new Rect(x, y, w, h);

            UIThemeHelper.DrawRoundedBox(rect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            // Título sutil no topo da caixa da bússola
            Rect titleRect = new Rect(x + 10f, y + 2f, w - 20f, 14f);
            GUI.Label(titleRect, $"BÚSSOLA • {heading:F0}° {GetCompassCardinal(heading)}", hudBoxTitleStyle);

            // Área de rolagem da fita navegacional
            Rect tapeRect = new Rect(x + 8f, y + 18f, w - 16f, 24f);

            // Fundo escuro interno da fita
            GUI.color = new Color(0.02f, 0.05f, 0.09f, 0.8f);
            GUI.DrawTexture(tapeRect, whiteTex);
            GUI.color = Color.white;

            // Marcador central fixo (Indicador dourado do rumo)
            GUI.color = new Color(1f, 0.85f, 0.25f, 0.95f);
            GUI.DrawTexture(new Rect(tapeRect.x + tapeRect.width * 0.5f - 1f, tapeRect.y, 2f, tapeRect.height), whiteTex);
            GUI.color = Color.white;

            // Marcadores de Pontos Cardeais na fita
            string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            GUI.BeginGroup(tapeRect);
            for (int i = 0; i < angles.Length; i++)
            {
                float diff = Mathf.DeltaAngle(heading, angles[i]);
                if (Mathf.Abs(diff) <= 60f)
                {
                    float posX = (tapeRect.width * 0.5f) + (diff / 60f) * (tapeRect.width * 0.48f);
                    bool isNorth = i == 0;
                    compassLabelStyle.normal.textColor = isNorth ? new Color(1f, 0.35f, 0.35f) : new Color(0.8f, 0.95f, 1f);
                    GUI.Label(new Rect(posX - 15f, 2f, 30f, 20f), cardinals[i], compassLabelStyle);
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
                    float posX = (tapeRect.width * 0.5f) + (diff / 60f) * (tapeRect.width * 0.48f);
                    HudIconRect(new Rect(posX - 7f, 4f, 14f, 14f), TerraIcon.Flag, new Color(1f, 0.85f, 0.2f));
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
                    float posX = (tapeRect.width * 0.5f) + (diff / 60f) * (tapeRect.width * 0.48f);
                    HudIconRect(new Rect(posX - 7f, 4f, 14f, 14f), TerraIcon.LocationPin, new Color(0.9f, 0.3f, 1f));
                }
            }

            GUI.EndGroup();
        }

        private string GetCompassCardinal(float degrees)
        {
            degrees = Mathf.Repeat(degrees, 360f);
            if (degrees >= 337.5f || degrees < 22.5f) return "N";
            if (degrees < 67.5f) return "NE";
            if (degrees < 112.5f) return "E";
            if (degrees < 157.5f) return "SE";
            if (degrees < 202.5f) return "S";
            if (degrees < 247.5f) return "SW";
            if (degrees < 292.5f) return "W";
            return "NW";
        }

        #endregion

        #region 3. Topo Direito: Data + Finança + Hora + Clima(ícone) (Print 1 & Print 2)

        private void DrawTopRightStatusBox()
        {
            if (activeSave == null && SaveManager.Instance != null)
            {
                activeSave = SaveManager.Instance.ActiveSave;
            }

            float w = 380f;
            float h = 48f;
            float x = Screen.width - w - 20f;
            float y = 15f;
            Rect rect = new Rect(x, y, w, h);

            UIThemeHelper.DrawRoundedBox(rect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 12f, w - 20f, h - 16f));
            GUILayout.BeginHorizontal();

            // 1. Data (Dia do Save)
            long day = activeSave != null ? activeSave.gameDay : 1;
            HudLayoutIcon(TerraIcon.Calendar, 15f, new Color(0.45f, 0.85f, 1f));
            GUILayout.Space(2);
            GUILayout.Label($"Dia {day}", subtitleStyle);

            GUILayout.Space(8);
            DrawVerticalDivider();
            GUILayout.Space(8);

            // 2. Finança (Saldo com cor verde destacada)
            string money = activeSave != null ? activeSave.GetFormattedMoney() : "$500,000";
            HudLayoutIcon(TerraIcon.Wallet, 15f, new Color(0.35f, 0.95f, 0.5f));
            GUILayout.Space(2);
            GUILayout.Label(money, moneyStyle);

            GUILayout.Space(8);
            DrawVerticalDivider();
            GUILayout.Space(8);

            // 3. Hora (Horário solar formatado HH:mm)
            string timeStr = FormattedInGameTimeString;
            HudLayoutIcon(TerraIcon.Clock, 14f, new Color(0.95f, 0.85f, 0.35f));
            GUILayout.Space(2);
            GUILayout.Label(timeStr, subtitleStyle);

            GUILayout.Space(8);
            DrawVerticalDivider();
            GUILayout.Space(8);

            // 4. Clima (Ícone + Temperatura estimada)
            DrawWeatherIndicator();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawWeatherIndicator()
        {
            var atmos = HDRPAtmosphereController.Instance;
            string weatherKey = atmos != null ? atmos.currentWeatherKey : "clear";
            float temp = atmos != null ? atmos.CurrentTemperatureCelsius : 24f;

            TerraIcon icon = TerraIcon.Info;
            Color iconColor = new Color(1f, 0.85f, 0.25f);

            if (weatherKey == "storm")
            {
                icon = TerraIcon.WaterDrop;
                iconColor = new Color(0.35f, 0.75f, 1f);
            }
            else if (weatherKey == "fog" || weatherKey == "densefog")
            {
                icon = TerraIcon.Satellite;
                iconColor = new Color(0.75f, 0.85f, 0.95f);
            }
            else if (weatherKey == "overcast")
            {
                icon = TerraIcon.Park;
                iconColor = new Color(0.8f, 0.85f, 0.9f);
            }
            else
            {
                // Ensolarado / Céu Limpo
                icon = TerraIcon.ReputationStar;
                iconColor = new Color(1f, 0.85f, 0.25f);
            }

            HudLayoutIcon(icon, 15f, iconColor);
            GUILayout.Space(3);
            GUILayout.Label($"{temp:F0}°C", subtitleStyle);
        }

        private void DrawVerticalDivider()
        {
            Rect r = GUILayoutUtility.GetRect(1f, 18f, GUILayout.Width(1f), GUILayout.Height(18f));
            if (Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(0.3f, 0.5f, 0.75f, 0.35f);
                GUI.DrawTexture(r, whiteTex);
                GUI.color = Color.white;
            }
        }

        #endregion

        #region 4. Rastreador de Missão (Discreto abaixo do status no topo direito)

        private void DrawTopRightMissionCard()
        {
            if (MissionManager.Instance == null || !MissionManager.Instance.hasActiveContract || MissionManager.Instance.currentContract == null)
                return;

            var contract = MissionManager.Instance.currentContract;
            if (contract.isCompleted) return;

            bool hasDeadline = contract.deadlineGameHours > 0f;

            float w = 380f;
            float h = hasDeadline ? 82f : 68f;
            float x = Screen.width - w - 20f;
            float y = 70f; // Posicionado exatamente abaixo da caixa de status
            Rect rect = new Rect(x, y, w, h);

            UIThemeHelper.DrawRoundedBox(rect, HudBoxBg, new Color(1f, 0.82f, 0.25f, 0.75f), radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 6f, w - 24f, h - 10f));

            GUILayout.BeginHorizontal();
            HudLayoutIcon(TerraIcon.Contract, 15f, new Color(1f, 0.82f, 0.25f));
            GUILayout.Space(3);
            GUILayout.Label(contract.title, missionTitleStyle);
            GUILayout.FlexibleSpace();
            HudLayoutIcon(TerraIcon.Wallet, 13f, new Color(0.35f, 0.95f, 0.5f));
            GUILayout.Space(2);
            GUILayout.Label($"+${contract.rewardMoney:N0}", moneyStyle);
            GUILayout.EndHorizontal();

            float dist = MissionManager.Instance.GetDistanceToDestination();
            string distText = dist > 1000f ? $"{(dist / 1000f):F1} km" : $"{dist:F0} m";

            GUILayout.BeginHorizontal();
            HudLayoutIcon(TerraIcon.LocationPin, 13f, new Color(0.45f, 0.85f, 1f));
            GUILayout.Space(3);
            GUILayout.Label($" {contract.destinationName} ({distText}) • {contract.cargoType}", subtitleStyle);
            GUILayout.EndHorizontal();

            if (hasDeadline)
            {
                float hrs = MissionManager.Instance.GetHoursRemaining();
                string tl = hrs <= 0f ? "ATRASADO" : (hrs < 1f ? $"{hrs * 60f:F0} min" : $"{hrs:F1} h");
                string urg = contract.urgent ? "[URGENTE] " : "";
                string frag = contract.fragile ? $" • Carga {contract.cargoIntegrity:F0}%" : "";
                GUI.color = hrs <= 0f ? new Color(1f, 0.4f, 0.4f) : (contract.urgent ? new Color(1f, 0.6f, 0.3f) : Color.white);
                GUILayout.Label($"{urg}Prazo: {tl}{frag}", subtitleStyle);
                GUI.color = Color.white;
            }

            GUILayout.EndArea();
        }

        #endregion

        #region 5. HUD A Pé (Print 2): Canto Inferior Direito -> Estamina + Fome + Sono

        private void DrawOnFootHUDCluster(PlayerCharacterController player)
        {
            if (player == null) return;

            float w = 115f;
            float h = 180f;
            float x = Screen.width - w - 20f;
            float y = Screen.height - h - 20f;
            Rect rect = new Rect(x, y, w, h);

            // Caixa vertical com cantos arredondados
            UIThemeHelper.DrawRoundedBox(rect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 8f, w - 16f, h - 16f));

            // 1. Estamina
            DrawVerticalNeedModule("ESTAMINA", player.stamina, player.maxStamina, new Color(0.1f, 0.9f, 0.75f), TerraIcon.TrendingUp);
            GUILayout.Space(6);

            // 2. Fome
            DrawVerticalNeedModule("FOME", player.hunger, player.maxHunger, new Color(1f, 0.65f, 0.2f), TerraIcon.Crops);
            GUILayout.Space(6);

            // 3. Sono
            DrawVerticalNeedModule("SONO", player.sleep, player.maxSleep, new Color(0.55f, 0.65f, 1f), TerraIcon.Clock);

            GUILayout.EndArea();
        }

        private void DrawVerticalNeedModule(string label, float current, float max, Color barColor, TerraIcon icon)
        {
            GUILayout.BeginHorizontal();
            HudLayoutIcon(icon, 13f, barColor);
            GUILayout.Space(2);
            GUILayout.Label(label, hudBoxTitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{current:F0}%", hudMiniValueStyle);
            GUILayout.EndHorizontal();

            Rect barRect = GUILayoutUtility.GetRect(80, 8);
            DrawProgressBar(barRect, (current / max) * 100f, barColor, new Color(0.15f, 0.20f, 0.28f, 0.9f));
        }

        #endregion

        #region 6. HUD Dentro do Veículo (Print 1): Canto Inferior Direito

        private void DrawVehicleHUDCluster(VehicleController vehicle)
        {
            if (vehicle == null) return;

            var player = PlayerCharacterController.Instance;

            float hGroup = 180f;
            float yGroup = Screen.height - hGroup - 20f;

            // 1. Caixa Vertical da Extrema Direita: Combustivel + Dano + Sono
            float wVert = 115f;
            float xVert = Screen.width - wVert - 20f;
            Rect vertRect = new Rect(xVert, yGroup, wVert, hGroup);
            UIThemeHelper.DrawRoundedBox(vertRect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(vertRect.x + 8f, vertRect.y + 8f, wVert - 16f, hGroup - 16f));

            // Combustível
            Color fuelColor = vehicle.fuelPercent > 45f ? new Color(0.2f, 0.85f, 0.4f) :
                             (vehicle.fuelPercent > 20f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));
            DrawVerticalNeedModule("TANQUE", vehicle.fuelPercent, 100f, fuelColor, TerraIcon.Fuel);
            GUILayout.Space(6);

            // Dano / Integridade
            Color condColor = vehicle.conditionPercent > 60f ? new Color(0.2f, 0.85f, 0.4f) :
                             (vehicle.conditionPercent > 30f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));
            DrawVerticalNeedModule("DANO", vehicle.conditionPercent, 100f, condColor, TerraIcon.Wrench);
            GUILayout.Space(6);

            // Sono do motorista
            float sleepVal = player != null ? player.sleep : 100f;
            DrawVerticalNeedModule("SONO", sleepVal, 100f, new Color(0.55f, 0.65f, 1f), TerraIcon.Clock);

            GUILayout.EndArea();

            // 2. Caixa Central: Velocímetro + Marcha
            float wSpeed = 150f;
            float xSpeed = xVert - wSpeed - 10f;
            Rect speedRect = new Rect(xSpeed, yGroup, wSpeed, hGroup);
            UIThemeHelper.DrawRoundedBox(speedRect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(speedRect.x + 8f, speedRect.y + 6f, wSpeed - 16f, hGroup - 12f));

            // Topo do Velocímetro: Categoria / Nome
            GUILayout.BeginHorizontal();
            HudLayoutIcon(VehicleCategoryIcon(vehicle.category), 14f, new Color(0.45f, 0.85f, 1f));
            GUILayout.Space(2);
            GUILayout.Label(vehicle.category.ToString().ToUpper(), hudBoxTitleStyle);
            GUILayout.EndHorizontal();

            // Velocidade em dígitos grandes
            float speedVal = vehicle.category == VehicleCategory.Boat ? vehicle.currentSpeedKmh * 0.54f : vehicle.currentSpeedKmh;
            GUILayout.Label($"{speedVal:F0}", hudSpeedBigStyle, GUILayout.Height(46));

            // Unidade (KM/H ou NÓS)
            string unit = vehicle.category == VehicleCategory.Boat ? "NÓS" : "KM/H";
            GUILayout.Label(unit, speedUnitStyle, GUILayout.Height(16));

            GUILayout.Space(4);

            // Marcha em destaque
            string gear = vehicle.CurrentGearString;
            string transMode = vehicle.TransmissionModeString;

            Rect gearPill = GUILayoutUtility.GetRect(wSpeed - 16f, 26f);
            UIThemeHelper.DrawRoundedBox(gearPill, new Color(0.12f, 0.2f, 0.32f, 0.9f), new Color(0.4f, 0.75f, 1f, 0.9f), radius: 6f, borderThickness: 1f);
            GUI.Label(gearPill, $"MARCHA [{gear}] • {transMode}", hudGearTextStyle);

            // Barra de Aceleração / Força
            float throttleFill = Mathf.Abs(vehicle.currentThrottle) * 100f;
            Rect throttleBar = GUILayoutUtility.GetRect(wSpeed - 16f, 5f);
            DrawProgressBar(throttleBar, throttleFill, new Color(0.35f, 0.9f, 0.45f), new Color(0.15f, 0.20f, 0.28f, 0.9f));

            GUILayout.EndArea();

            // 3. Duas Caixas Empilhadas à Esquerda do Velocímetro
            float wLeft = 160f;
            float hSub = (hGroup - 10f) * 0.5f; // 85px cada
            float xLeft = xSpeed - wLeft - 10f;

            // Caixa Superior: % Produto Carregado
            Rect cargoBoxRect = new Rect(xLeft, yGroup, wLeft, hSub);
            UIThemeHelper.DrawRoundedBox(cargoBoxRect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(cargoBoxRect.x + 8f, cargoBoxRect.y + 6f, wLeft - 16f, hSub - 12f));
            GUILayout.BeginHorizontal();
            HudLayoutIcon(TerraIcon.CargoBox, 14f, new Color(0.35f, 0.85f, 1f));
            GUILayout.Space(2);
            GUILayout.Label("% PRODUTO CARREGADO", hudBoxTitleStyle);
            GUILayout.EndHorizontal();

            string cargoName = string.IsNullOrEmpty(vehicle.cargoItemName) || vehicle.cargoItemName == "Vazio" ? "Vazio" : vehicle.cargoItemName;
            GUILayout.Label(cargoName, hudBoxSubStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Capacidade:", subtitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{vehicle.cargoFillPercent:F0}%", hudMiniValueStyle);
            GUILayout.EndHorizontal();

            Rect cargoBar = GUILayoutUtility.GetRect(wLeft - 16f, 6f);
            DrawProgressBar(cargoBar, vehicle.cargoFillPercent, new Color(0.25f, 0.8f, 1f), new Color(0.15f, 0.20f, 0.28f, 0.9f));
            GUILayout.EndArea();

            // Caixa Inferior: Conexões do Veículo
            Rect connBoxRect = new Rect(xLeft, yGroup + hSub + 10f, wLeft, hSub);
            UIThemeHelper.DrawRoundedBox(connBoxRect, HudBoxBg, HudBoxBorder, radius: 8f, borderThickness: 1.5f);

            GUILayout.BeginArea(new Rect(connBoxRect.x + 8f, connBoxRect.y + 6f, wLeft - 16f, hSub - 12f));
            GUILayout.BeginHorizontal();
            HudLayoutIcon(TerraIcon.Tools, 14f, new Color(0.95f, 0.85f, 0.35f));
            GUILayout.Space(2);
            GUILayout.Label("CONEXÕES DO VEÍCULO", hudBoxTitleStyle);
            GUILayout.EndHorizontal();

            string connSummary = vehicle.GetConnectionStatusSummary();
            GUILayout.Label(connSummary, hudBoxSubStyle);

            GUILayout.BeginHorizontal();
            HudLayoutIcon(TerraIcon.Headlights, 12f, vehicle.headLightsOn ? new Color(1f, 0.9f, 0.4f) : new Color(0.5f, 0.6f, 0.7f));
            GUILayout.Space(2);
            GUILayout.Label(vehicle.headLightsOn ? "Faróis: Ligados" : "Faróis: Desligados", subtitleStyle);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        #endregion

        #region Utilitários de Barra de Progresso e Ícones

        private void DrawProgressBar(Rect rect, float fillPercent, Color fillColor, Color bgColor)
        {
            UIThemeHelper.DrawRoundedBox(rect, bgColor, Color.clear, 3f, 0f);

            float fillW = Mathf.Clamp01(fillPercent / 100f) * rect.width;
            if (fillW > 1f)
            {
                UIThemeHelper.DrawRoundedBox(new Rect(rect.x, rect.y, fillW, rect.height), fillColor, Color.clear, 3f, 0f);
            }
        }

        private void HudLayoutIcon(TerraIcon icon, float size, Color tint)
        {
            Rect r = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            if (Event.current.type == EventType.Repaint)
                ProjectTerra.UI.TerraIcons.Draw(r, icon, tint);
        }

        private void HudIconRect(Rect r, TerraIcon icon, Color tint)
        {
            if (Event.current.type == EventType.Repaint)
                ProjectTerra.UI.TerraIcons.Draw(r, icon, tint);
        }

        private static TerraIcon VehicleCategoryIcon(VehicleCategory cat)
        {
            switch (cat)
            {
                case VehicleCategory.Tractor: return TerraIcon.Tractor;
                case VehicleCategory.Truck: return TerraIcon.HeavyTruck;
                case VehicleCategory.Plane: return TerraIcon.Airplane;
                case VehicleCategory.Boat: return TerraIcon.FishingBoat;
                default: return TerraIcon.Pickup;
            }
        }

        #endregion
    }
}
