using System;
using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class InGameCommandConsole
    {
        #region Execução dos Comandos

        public void ExecuteCommand(string rawCommand)
        {
            if (string.IsNullOrEmpty(rawCommand)) return;

            string clean = rawCommand.Trim();
            if (clean.StartsWith("/")) clean = clean.Substring(1).Trim();

            string[] parts = clean.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string action = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, parts.Length - 1);

            try
            {
                switch (action)
                {
                    case "help":
                    case "?":
                    case "ajuda":
                        CmdHelp(args);
                        break;

                    case "spawn":
                    case "veiculo":
                    case "vehicle":
                        CmdSpawn(args);
                        break;

                    case "time":
                    case "hora":
                    case "tempo":
                        CmdTime(args);
                        break;

                    case "timescale":
                    case "speedtime":
                    case "velocidade":
                        CmdTimeScale(args);
                        break;

                    case "weather":
                    case "clima":
                        CmdWeather(args);
                        break;

                    case "tp":
                    case "teleport":
                    case "teleporte":
                        CmdTeleport(args);
                        break;

                    case "speed":
                    case "movespeed":
                        CmdSpeed(args);
                        break;

                    case "fly":
                    case "noclip":
                    case "voar":
                        CmdFly(args);
                        break;

                    case "god":
                    case "godmode":
                    case "invencivel":
                        CmdGod(args);
                        break;

                    case "money":
                    case "dinheiro":
                    case "addmoney":
                        CmdMoney(args, isSet: false);
                        break;

                    case "setmoney":
                        CmdMoney(args, isSet: true);
                        break;

                    case "refuel":
                    case "abastecer":
                    case "gasolina":
                        CmdRefuel(args);
                        break;

                    case "repair":
                    case "heal":
                    case "reparar":
                    case "consertar":
                        CmdRepair(args);
                        break;

                    case "unflip":
                    case "flip":
                    case "desvirar":
                        CmdUnflip(args);
                        break;

                    case "cargo":
                    case "carga":
                        CmdCargo(args);
                        break;

                    case "mission":
                    case "missao":
                    case "missão":
                    case "contract":
                        CmdMission(args);
                        break;

                    case "reputation":
                    case "rep":
                        CmdReputation(args);
                        break;

                    case "light":
                    case "luz":
                    case "flashlight":
                    case "lanterna":
                    case "farol":
                        CmdLight(args);
                        break;

                    case "camera":
                    case "cam":
                        CmdCamera(args);
                        break;

                    case "vehicles":
                    case "veiculos":
                    case "frota":
                        CmdListVehicles(args);
                        break;

                    case "switch":
                    case "trocar":
                        CmdSwitchVehicle(args);
                        break;

                    case "clearvehicles":
                    case "limparveiculos":
                        CmdClearVehicles(args);
                        break;

                    case "coords":
                    case "pos":
                    case "posicao":
                    case "coordenadas":
                        CmdCoords(args);
                        break;

                    case "hud":
                        CmdHud(args);
                        break;

                    case "map":
                    case "mapa":
                        CmdMap(args);
                        break;

                    case "save":
                    case "salvar":
                        CmdSave(args);
                        break;

                    case "clear":
                    case "cls":
                    case "limpar":
                        messageLog.Clear();
                        LogMessage("Histórico de mensagens limpo.", Color.gray);
                        break;

                    default:
                        LogMessage($"❌ Comando desconhecido: '{action}'. Digite /help para ver os comandos disponíveis.", new Color(1f, 0.4f, 0.4f));
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ Erro ao executar comando: {ex.Message}", new Color(1f, 0.35f, 0.35f));
            }
        }

        #endregion

        #region Comandos Gerais e Utilitários

        private void CmdHelp(string[] args)
        {
            if (args.Length > 0)
            {
                string topic = args[0].ToLower().Replace("/", "");
                switch (topic)
                {
                    case "spawn":
                        LogMessage("📖 /spawn <tipo> [quantidade]", Color.cyan);
                        LogMessage("Tipos suportados: tractor, truck, plane, boat, car, sports, suv, police, ambulance, firetruck, taxi, van, speeder, all", Color.white);
                        return;
                    case "time":
                        LogMessage("📖 /time <0-24 | day | night | noon | sunset | sunrise | cycle <on/off>>", Color.cyan);
                        LogMessage("Ajusta o horário solar ou ativa o ciclo contínuo em tempo real.", Color.white);
                        return;
                    case "tp":
                        LogMessage("📖 /tp <town | base | market | airport | port | origin | x z | x y z>", Color.cyan);
                        LogMessage("Teleporta o jogador (e o veículo atual) para locais chave ou coordenadas exatas.", Color.white);
                        return;
                }
            }

            LogMessage("═════════ LISTA DE COMANDOS DA PROVÍNCIA ═════════", Color.cyan);
            LogMessage("🚗 <b>Veículos:</b> /spawn <tipo>, /vehicles, /switch, /clearvehicles, /refuel, /repair, /unflip", Color.white);
            LogMessage("🕒 <b>Horário & Tempo:</b> /time <h/preset>, /timescale <mult>, /weather <clima>", Color.white);
            LogMessage("📍 <b>Navegação:</b> /tp <local/coords>, /coords", Color.white);
            LogMessage("⚡ <b>Jogador:</b> /fly (noclip), /speed <mult>, /god, /light", Color.white);
            LogMessage("💼 <b>Economia:</b> /money <quantia>, /setmoney <quantia>, /mission <complete/new>, /reputation <qnt>", Color.white);
            LogMessage("🎮 <b>Interface:</b> /camera <modo>, /hud <on/off>, /map, /save, /clear", Color.white);
            LogMessage("💡 <b>Atalhos:</b> [/] Console, [L] Lanterna/Farol, [Tab] Trocar Veículo, [C] Câmera", new Color(0.3f, 1f, 0.6f));
        }

        private void CmdCoords(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player != null)
            {
                Vector3 p = player.transform.position;
                float heading = player.transform.eulerAngles.y;
                LogMessage($"📍 Posição do jogador: X = {p.x:F1} m, Y = {p.y:F1} m (Altitude), Z = {p.z:F1} m  •  Rumo: {heading:F0}°", Color.cyan);
            }
        }

        private void CmdHud(string[] args)
        {
            if (SandboxHUD.Instance != null)
            {
                if (args.Length > 0)
                {
                    string arg = args[0].ToLower();
                    bool show = arg == "on" || arg == "1" || arg == "true";
                    typeof(SandboxHUD).GetProperty("IsHudVisible")?.SetValue(SandboxHUD.Instance, show);
                }
                else
                {
                    bool cur = SandboxHUD.Instance.IsHudVisible;
                    typeof(SandboxHUD).GetProperty("IsHudVisible")?.SetValue(SandboxHUD.Instance, !cur);
                }
                LogMessage($"HUD alterado.", Color.cyan);
            }
        }

        private void CmdMap(string[] args)
        {
            SandboxHUD.Instance?.ToggleRegionalMap();
            LogMessage("🗺️ Mapa tático regional alternado.", Color.cyan);
        }

        private void CmdSave(string[] args)
        {
            SandboxHUD.Instance?.SaveCurrentGame();
            LogMessage("💾 Partida salva com sucesso!", Color.green);
        }

        #endregion
    }
}
