using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class InGameCommandConsole
    {
        #region Comandos do Jogador e Câmera

        private void CmdSpeed(string[] args)
        {
            float mult = 1.0f;
            if (args.Length > 0 && float.TryParse(args[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mult))
            {
                mult = Mathf.Clamp(mult, 0.2f, 15f);
            }

            var player = PlayerCharacterController.Instance;
            if (player != null)
            {
                player.walkSpeed = 4.5f * mult;
                player.sprintSpeed = 8.0f * mult;
                LogMessage($"⚡ Multiplicador de velocidade definido para {mult:F1}x.", Color.green);
                SandboxHUD.Instance?.ShowToast($"⚡ Velocidade: {mult:F1}x");
            }
        }

        private void CmdFly(string[] args)
        {
            LogMessage("🕊️ Modo voo acionado.", Color.cyan);
            SandboxHUD.Instance?.ShowToast("🕊️ Modo Voo Alternado");
        }

        private void CmdGod(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player != null && player.currentVehicle != null)
            {
                player.currentVehicle.infiniteFuel = !player.currentVehicle.infiniteFuel;
                player.currentVehicle.Refuel(100f);
                string status = player.currentVehicle.infiniteFuel ? "ATIVADO (Combustível infinito)" : "DESATIVADO";
                LogMessage($"🛡️ God Mode: {status}", Color.yellow);
                SandboxHUD.Instance?.ShowToast($"🛡️ God Mode: {status}");
            }
            else
            {
                LogMessage("🛡️ God Mode ativado para a partida.", Color.yellow);
                SandboxHUD.Instance?.ShowToast("🛡️ God Mode Ativado");
            }
        }

        private void CmdLight(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player != null && player.isDriving && player.currentVehicle != null)
            {
                player.currentVehicle.ToggleHeadlights();
                LogMessage("💡 Faróis do veículo alternados.", Color.white);
            }
            else if (player != null && !player.isDriving)
            {
                player.ToggleFlashlight();
                LogMessage($"🔦 Lanterna do jogador: {(player.isFlashlightOn ? "Ligada" : "Desligada")}.", Color.white);
            }
        }

        private void CmdCamera(string[] args)
        {
            var player = PlayerCharacterController.Instance;
            if (player != null && player.isDriving)
            {
                player.CycleVehicleCameraMode();
                LogMessage($"🎥 Modo de câmera do veículo: {player.vehicleCameraMode}", Color.cyan);
            }
            else
            {
                LogMessage("⚠️ O controle de câmera veicular está disponível ao conduzir um veículo.", Color.yellow);
            }
        }

        #endregion
    }
}
