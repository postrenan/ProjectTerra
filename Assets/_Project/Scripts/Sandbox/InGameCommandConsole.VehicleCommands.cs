using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class InGameCommandConsole
    {
        #region Comandos de Veículos e Frota

        private void CmdSpawn(string[] args)
        {
            if (args.Length == 0)
            {
                LogMessage("Uso: /spawn <tipo>  |  Ex: /spawn truck, /spawn car, /spawn plane, /spawn tractor, /spawn all", new Color(1f, 0.8f, 0.2f));
                return;
            }

            string type = args[0].ToLower();

            if (type == "all" || type == "frota")
            {
                string[] fleet = { "tractor", "truck", "car", "sports", "suv", "police", "plane" };
                var player = PlayerCharacterController.Instance;
                Vector3 basePos = player != null ? player.transform.position : Vector3.zero;
                Vector3 right = player != null ? player.transform.right : Vector3.right;
                Vector3 fwd = player != null ? player.transform.forward : Vector3.forward;

                for (int i = 0; i < fleet.Length; i++)
                {
                    Vector3 pos = basePos + fwd * 8f + right * ((i - 3) * 5.5f);
                    if (RegionalSandboxManager.Instance != null) pos.y = RegionalSandboxManager.Instance.GetTerrainHeight(pos) + 0.8f;
                    VehicleManager.Instance?.SpawnVehicle(fleet[i], pos, Quaternion.LookRotation(fwd, Vector3.up));
                }

                LogMessage($"✅ Frota de {fleet.Length} veículos instanciada com sucesso!", Color.green);
                SandboxHUD.Instance?.ShowToast("🚗 Frota de veículos criada ao seu redor!");
                return;
            }

            var spawned = VehicleManager.Instance?.SpawnVehicle(type);
            if (spawned != null)
            {
                LogMessage($"✅ Veículo '{spawned.vehicleName}' criado e adicionado à sua posse!", Color.green);
                SandboxHUD.Instance?.ShowToast($"🚗 Veículo criado: {spawned.vehicleName}");
            }
            else
            {
                LogMessage($"❌ Tipo de veículo desconhecido: '{type}'. Use /help spawn para a lista.", new Color(1f, 0.4f, 0.4f));
            }
        }

        private void CmdRefuel(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player != null && player.isDriving && player.currentVehicle != null)
            {
                player.currentVehicle.Refuel(100f);
                LogMessage($"⛽ Tanque de {player.currentVehicle.vehicleName} abastecido a 100%!", Color.green);
                SandboxHUD.Instance?.ShowToast("⛽ Tanque cheio (100%)!");
            }
            else if (VehicleManager.Instance != null && VehicleManager.Instance.possessedVehicles.Count > 0)
            {
                foreach (var v in VehicleManager.Instance.possessedVehicles)
                {
                    if (v != null) v.Refuel(100f);
                }
                LogMessage("⛽ Todos os veículos da frota foram totalmente abastecidos!", Color.green);
                SandboxHUD.Instance?.ShowToast("⛽ Frota totalmente abastecida!");
            }
            else
            {
                LogMessage("⚠️ Nenhum veículo encontrado para abastecer.", Color.yellow);
            }
        }

        private void CmdRepair(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            VehicleController targetVehicle = player != null && player.isDriving ? player.currentVehicle : null;

            if (targetVehicle == null && VehicleManager.Instance != null && VehicleManager.Instance.possessedVehicles.Count > 0)
            {
                targetVehicle = VehicleManager.Instance.possessedVehicles[VehicleManager.Instance.currentVehicleIndex];
            }

            if (targetVehicle != null)
            {
                targetVehicle.Refuel(100f);
                targetVehicle.ResetOrientation();
                LogMessage($"🔧 Veículo '{targetVehicle.vehicleName}' reparado e desvirado com sucesso!", Color.green);
                SandboxHUD.Instance?.ShowToast($"🔧 {targetVehicle.vehicleName} Reparado!");
            }
        }

        private void CmdUnflip(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player != null && player.currentVehicle != null)
            {
                player.currentVehicle.ResetOrientation();
                LogMessage("🔄 Veículo descapotado e alinhado ao terreno.", Color.green);
            }
            else if (VehicleManager.Instance != null && VehicleManager.Instance.possessedVehicles.Count > 0)
            {
                var v = VehicleManager.Instance.possessedVehicles[VehicleManager.Instance.currentVehicleIndex];
                if (v != null) v.ResetOrientation();
                LogMessage("🔄 Veículo descapotado.", Color.green);
            }
        }

        private void CmdCargo(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player == null || !player.isDriving || player.currentVehicle == null)
            {
                LogMessage("⚠️ Você precisa estar conduzindo um veículo para gerenciar a carga.", Color.yellow);
                return;
            }

            var vehicle = player.currentVehicle;
            if (args.Length == 0 || args[0].ToLower() == "unload" || args[0].ToLower() == "clear")
            {
                float unloaded = vehicle.UnloadCargo();
                LogMessage($"📦 Carga descarregada: {unloaded:F0} kg.", Color.green);
                SandboxHUD.Instance?.ShowToast($"📦 Caçamba esvaziada");
            }
            else
            {
                string itemName = args.Length > 1 ? args[1] : "Carga Geral";
                vehicle.LoadCargo(itemName, vehicle.maxCargoCapacityKg);
                LogMessage($"📦 Veículo carregado com {vehicle.maxCargoCapacityKg:F0} kg de {itemName} (100%).", Color.green);
                SandboxHUD.Instance?.ShowToast($"📦 Carregado com {itemName}!");
            }
        }

        private void CmdListVehicles(string[] args)
        {
            if (VehicleManager.Instance == null || VehicleManager.Instance.possessedVehicles.Count == 0)
            {
                LogMessage("Nenhum veículo em sua posse. Use /spawn para criar veículos.", Color.yellow);
                return;
            }

            var list = VehicleManager.Instance.possessedVehicles;
            var player = PlayerCharacterController.Instance;
            Vector3 pPos = player != null ? player.transform.position : Vector3.zero;

            LogMessage($"📋 <b>FROTA DO JOGADOR ({list.Count} veículos):</b>", Color.cyan);
            for (int i = 0; i < list.Count; i++)
            {
                var v = list[i];
                if (v == null) continue;
                float dist = Vector3.Distance(pPos, v.transform.position);
                string activeTag = (player != null && player.currentVehicle == v) ? " <color=#00FF7F>[CONDUZINDO]</color>" : "";
                LogMessage($"  #{i + 1}: <b>{v.vehicleName}</b> ({dist:F0}m de distância, {v.fuelPercent:F0}% combustível){activeTag}", Color.white);
            }
        }

        private void CmdSwitchVehicle(string[] args)
        {
            if (VehicleManager.Instance == null) return;

            if (args.Length > 0 && int.TryParse(args[0], out int idx))
            {
                var list = VehicleManager.Instance.possessedVehicles;
                if (idx >= 1 && idx <= list.Count)
                {
                    VehicleManager.Instance.currentVehicleIndex = idx - 1;
                    var player = PlayerCharacterController.Instance;
                    if (player != null)
                    {
                        if (player.isDriving) player.ExitVehicle();
                        player.EnterVehicle(list[idx - 1]);
                        LogMessage($"🚗 Assumiu o controle do veículo #{idx}: {list[idx - 1].vehicleName}", Color.green);
                        return;
                    }
                }
            }

            VehicleManager.Instance.SwitchToNextVehicle();
            LogMessage("🚗 Alternou para o próximo veículo.", Color.green);
        }

        private void CmdClearVehicles(string[] args)
        {
            int removed = VehicleManager.Instance != null ? VehicleManager.Instance.ClearSpawnedVehicles() : 0;
            LogMessage($"🧹 {removed} veículos extras foram removidos do mundo.", Color.green);
            SandboxHUD.Instance?.ShowToast($"🧹 {removed} veículos removidos");
        }

        #endregion
    }
}
