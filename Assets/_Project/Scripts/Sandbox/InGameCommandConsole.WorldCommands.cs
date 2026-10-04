using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public partial class InGameCommandConsole
    {
        #region Comandos de Mundo, Clima, Economia e Teleporte

        private void CmdTime(string[] args)
        {
            if (args.Length == 0)
            {
                float curTime = HDRPAtmosphereController.Instance != null ? HDRPAtmosphereController.Instance.timeOfDay : 12f;
                int h = Mathf.FloorToInt(curTime);
                int m = Mathf.FloorToInt((curTime - h) * 60f);
                LogMessage($"🕒 Horário atual da província: {h:D2}:{m:D2}. Uso: /time <0-24 | day | night | noon | sunset | cycle on/off>", Color.yellow);
                return;
            }

            string sub = args[0].ToLower();

            if (sub == "cycle" || sub == "ciclo")
            {
                if (args.Length > 1 && (args[1] == "off" || args[1] == "desligar" || args[1] == "0"))
                {
                    if (HDRPAtmosphereController.Instance != null) HDRPAtmosphereController.Instance.autoCycleTime = false;
                    LogMessage("⏸️ Ciclo automático de dia/noite desativado.", Color.yellow);
                }
                else
                {
                    if (HDRPAtmosphereController.Instance != null) HDRPAtmosphereController.Instance.autoCycleTime = true;
                    LogMessage("▶️ Ciclo automático de dia/noite ativado!", Color.green);
                }
                return;
            }

            float targetHour = 12f;
            switch (sub)
            {
                case "day":
                case "dia":
                case "noon":
                case "meiodia":
                    targetHour = 12.0f;
                    break;
                case "night":
                case "noite":
                case "midnight":
                case "meianoite":
                    targetHour = 0.0f;
                    break;
                case "sunrise":
                case "dawn":
                case "amanhecer":
                    targetHour = 6.0f;
                    break;
                case "sunset":
                case "dusk":
                case "entardecer":
                case "pordosol":
                    targetHour = 18.0f;
                    break;
                default:
                    if (sub.Contains(":"))
                    {
                        var timeParts = sub.Split(':');
                        if (float.TryParse(timeParts[0], out float hourPart))
                        {
                            float minPart = 0f;
                            if (timeParts.Length > 1) float.TryParse(timeParts[1], out minPart);
                            targetHour = hourPart + (minPart / 60f);
                        }
                    }
                    else if (!float.TryParse(sub, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out targetHour))
                    {
                        LogMessage($"❌ Formato de horário inválido: '{sub}'. Exemplos: /time 14, /time 08:30, /time day, /time night", new Color(1f, 0.4f, 0.4f));
                        return;
                    }
                    break;
            }

            if (HDRPAtmosphereController.Instance != null)
            {
                HDRPAtmosphereController.Instance.SetTimeOfDay(targetHour);
                int h = Mathf.FloorToInt(targetHour);
                int m = Mathf.FloorToInt((targetHour - h) * 60f);
                LogMessage($"☀️ Horário solar alterado para {h:D2}:{m:D2}.", Color.green);
            }
        }

        private void CmdTimeScale(string[] args)
        {
            if (args.Length == 0)
            {
                LogMessage($"Velocidade atual: {Time.timeScale}x. Uso: /timescale <multiplicador> (ex: 1, 2, 5, 0.5)", Color.yellow);
                return;
            }

            if (float.TryParse(args[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale))
            {
                scale = Mathf.Clamp(scale, 0.05f, 20f);
                Time.timeScale = scale;
                Time.fixedDeltaTime = 0.02f * scale;
                LogMessage($"⏩ Velocidade da simulação ajustada para {scale:F1}x.", Color.green);
                SandboxHUD.Instance?.ShowToast($"⏩ Simulação: {scale:F1}x");
            }
        }

        private void CmdWeather(string[] args)
        {
            if (args.Length == 0)
            {
                LogMessage("Uso: /weather <clear | fog | densefog | overcast | storm>", Color.yellow);
                return;
            }

            HDRPAtmosphereController.Instance?.SetWeather(args[0]);
            LogMessage($"🌤️ Clima alterado para '{args[0]}'.", Color.green);
        }

        private void CmdTeleport(string[] args)
        {
            if (args.Length == 0)
            {
                LogMessage("Uso: /tp <town | base | market | airport | port | origin | x z | x y z>", Color.yellow);
                return;
            }

            Vector3 targetPos = Vector3.zero;
            string destName = "";
            var rsm = RegionalSandboxManager.Instance;

            string key = args[0].ToLower();
            switch (key)
            {
                case "town":
                case "cidade":
                    targetPos = rsm != null ? rsm.townCenterPosition : Vector3.zero;
                    destName = "Cidade Central";
                    break;
                case "base":
                case "fazenda":
                case "home":
                    targetPos = rsm != null ? rsm.starterBasePosition : Vector3.zero;
                    destName = "Sede / Base Inicial";
                    break;
                case "market":
                case "mercado":
                case "delivery":
                    targetPos = rsm != null ? rsm.deliveryMarketPosition : Vector3.zero;
                    destName = "Centro de Distribuição / Silos";
                    break;
                case "airport":
                case "aeroporto":
                case "pista":
                    targetPos = rsm != null ? rsm.townCenterPosition + new Vector3(2500f, 0f, 4500f) : Vector3.zero;
                    destName = "Aeródromo Regional";
                    break;
                case "port":
                case "porto":
                case "pier":
                    targetPos = rsm != null ? new Vector3(0f, 0f, -rsm.worldLengthMeters * 0.36f) : Vector3.zero;
                    destName = "Porto Marítimo / Cais";
                    break;
                case "origin":
                case "zero":
                    targetPos = Vector3.zero;
                    destName = "Origem da Província (0, 0)";
                    break;
                default:
                    // Coordenadas numéricas X e Z (ou X, Y, Z)
                    if (args.Length >= 2 && float.TryParse(args[0], out float tx))
                    {
                        if (args.Length >= 3 && float.TryParse(args[1], out float ty) && float.TryParse(args[2], out float tz))
                        {
                            targetPos = new Vector3(tx, ty, tz);
                            destName = $"({tx:F0}, {ty:F0}, {tz:F0})";
                        }
                        else if (float.TryParse(args[1], out float tz2))
                        {
                            targetPos = new Vector3(tx, 0f, tz2);
                            destName = $"({tx:F0}, {tz2:F0})";
                        }
                    }
                    else
                    {
                        LogMessage($"❌ Destino ou coordenadas inválidas: '{key}'.", new Color(1f, 0.4f, 0.4f));
                        return;
                    }
                    break;
            }

            if (rsm != null)
            {
                targetPos.y = rsm.GetTerrainHeight(targetPos) + 1.0f;
            }

            var player = PlayerCharacterController.Instance;
            if (player != null)
            {
                if (player.isDriving && player.currentVehicle != null)
                {
                    player.currentVehicle.transform.position = targetPos + Vector3.up * 0.8f;
                    player.currentVehicle.ResetOrientation();
                }
                else
                {
                    player.transform.position = targetPos;
                }

                LogMessage($"🚀 Teleportado para {destName}!", Color.green);
                SandboxHUD.Instance?.ShowToast($"🚀 Teleportado para: {destName}");
            }
        }

        private void CmdMoney(string[] args, bool isSet)
        {
            if (args.Length == 0 || !long.TryParse(args[0], out long amount))
            {
                LogMessage($"Uso: {(isSet ? "/setmoney" : "/money")} <quantia>  (ex: /money 500000)", Color.yellow);
                return;
            }

            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                var save = SaveManager.Instance.ActiveSave;
                if (isSet) save.currentMoney = amount;
                else save.currentMoney += amount;

                SaveManager.Instance.SaveToFile(save);
                LogMessage($"💰 Saldo atualizado com sucesso: {save.GetFormattedMoney()}", Color.green);
                SandboxHUD.Instance?.ShowToast($"💰 Saldo: {save.GetFormattedMoney()}");
            }
        }

        private void CmdMission(string[] args)
        {
            if (MissionManager.Instance == null) return;

            string sub = args.Length > 0 ? args[0].ToLower() : "complete";
            if (sub == "complete" || sub == "concluir")
            {
                MissionManager.Instance.CompleteActiveContract();
                LogMessage("🎯 Contrato de entrega concluído via comando!", Color.green);
            }
            else
            {
                LogMessage("🎯 Gerando nova missão regional...", Color.cyan);
                MissionManager.Instance.CompleteActiveContract();
            }
        }

        private void CmdReputation(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int rep))
            {
                if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
                {
                    SaveManager.Instance.ActiveSave.reputation += rep;
                    LogMessage($"⭐ Reputação atual: {SaveManager.Instance.ActiveSave.reputation}", Color.green);
                    SandboxHUD.Instance?.ShowToast($"⭐ Reputação: +{rep}");
                }
            }
        }

        private void CmdAnimals(string[] args)
        {
            var animals = FindObjectsByType<AnimalController>();
            if (animals == null || animals.Length == 0)
            {
                LogMessage("ℹ️ Nenhum animal ativo encontrado no cenário no momento.", Color.yellow);
                return;
            }

            if (args.Length > 0)
            {
                string sub = args[0].ToLower();
                if (sub == "tp" || sub == "teleport")
                {
                    string target = args.Length > 1 ? args[1].ToLower() : "pasture";
                    AnimalController targetAnimal = null;
                    foreach (var a in animals)
                    {
                        if (target == "wild" || target == "lobo" || target == "raposa")
                        {
                            if (a.species == AnimalSpecies.Wolf || a.species == AnimalSpecies.Fox)
                            {
                                targetAnimal = a;
                                break;
                            }
                        }
                        else
                        {
                            if (a.species == AnimalSpecies.Cow || a.species == AnimalSpecies.Horse || a.species == AnimalSpecies.Sheep)
                            {
                                targetAnimal = a;
                                break;
                            }
                        }
                    }

                    if (targetAnimal != null && PlayerCharacterController.Instance != null)
                    {
                        PlayerCharacterController.Instance.TeleportTo(targetAnimal.transform.position + Vector3.back * 4f + Vector3.up * 1f);
                        LogMessage($"📍 Teleportado para perto de {targetAnimal.name} ({targetAnimal.species})!", Color.green);
                        return;
                    }
                }
            }

            // Estatísticas gerais dos animais
            int cows = 0, horses = 0, sheep = 0, foxes = 0, wolves = 0;
            int idleCount = 0, grazeCount = 0, walkCount = 0, runCount = 0;

            foreach (var a in animals)
            {
                switch (a.species)
                {
                    case AnimalSpecies.Cow: cows++; break;
                    case AnimalSpecies.Horse: horses++; break;
                    case AnimalSpecies.Sheep: sheep++; break;
                    case AnimalSpecies.Fox: foxes++; break;
                    case AnimalSpecies.Wolf: wolves++; break;
                }

                switch (a.currentState)
                {
                    case AnimalState.Idle:
                    case AnimalState.LookAround: idleCount++; break;
                    case AnimalState.Graze: grazeCount++; break;
                    case AnimalState.Walk: walkCount++; break;
                    case AnimalState.Run: runCount++; break;
                }
            }

            LogMessage($"🐾 Fauna Ativa ({animals.Length} animais): {cows} Vacas, {horses} Cavalos, {sheep} Ovelhas, {foxes} Raposas, {wolves} Lobos.", Color.cyan);
            LogMessage($"📊 Animações Atuais: {grazeCount} Pastando, {walkCount} Caminhando, {idleCount} Em Repouso/Observando, {runCount} Correndo.", Color.white);
            LogMessage("💡 Dica: use '/animals tp pasture' ou '/animals tp wild' para ir até os animais.", Color.gray);
        }

        #endregion
    }
}
