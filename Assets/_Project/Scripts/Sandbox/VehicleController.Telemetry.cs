using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class VehicleController
    {
        /// <summary>
        /// Nome da marcha atual do veículo para exibição no velocímetro do HUD.
        /// Retorna P, R, N, D ou número da marcha correspondente (1 a 6).
        /// </summary>
        public string CurrentGearString
        {
            get
            {
                if (category == VehicleCategory.Plane)
                {
                    return aircraftThrottle > 0.05f ? "VOO" : "TAXI";
                }

                if (category == VehicleCategory.Boat)
                {
                    if (currentThrottle < -0.05f) return "R";
                    if (currentSpeedKmh < 0.5f && Mathf.Abs(currentThrottle) < 0.05f) return "N";
                    return "F";
                }

                if (rb == null) return "N";

                float fwdDot = Vector3.Dot(rb.linearVelocity, transform.forward);

                // Marcha à ré
                if (fwdDot < -0.3f || (currentThrottle < -0.05f && fwdDot <= 0.1f))
                {
                    return "R";
                }

                // Veículo parado
                if (currentSpeedKmh < 0.6f && Mathf.Abs(currentThrottle) < 0.05f)
                {
                    return "P";
                }

                // Marchas à frente por velocidade
                if (category == VehicleCategory.Tractor)
                {
                    if (currentSpeedKmh < 9f) return "1";
                    if (currentSpeedKmh < 20f) return "2";
                    if (currentSpeedKmh < 32f) return "3";
                    return "4";
                }
                else
                {
                    if (currentSpeedKmh < 22f) return "1";
                    if (currentSpeedKmh < 45f) return "2";
                    if (currentSpeedKmh < 70f) return "3";
                    if (currentSpeedKmh < 95f) return "4";
                    if (currentSpeedKmh < 120f) return "5";
                    return "6";
                }
            }
        }

        /// <summary>
        /// Modo de transmissão (D, R, P, N) para exibição.
        /// </summary>
        public string TransmissionModeString
        {
            get
            {
                string gear = CurrentGearString;
                if (gear == "P" || gear == "R" || gear == "N" || gear == "TAXI" || gear == "VOO" || gear == "F")
                    return gear;
                return "D";
            }
        }

        /// <summary>
        /// Descrição amigável do status de conexão/engates do veículo.
        /// </summary>
        public string GetConnectionStatusSummary()
        {
            if (category == VehicleCategory.Tractor)
            {
                var hitchedPlow = PlowImplement.GetHitchedPlow(this);
                if (hitchedPlow != null)
                {
                    string mode = hitchedPlow.isLowered ? "Arando" : "Transporte";
                    return $"Arado ({mode})";
                }

                var nearPlow = PlowImplement.FindNearestPlow(transform.position, 4.5f);
                if (nearPlow != null && !nearPlow.isHitched)
                {
                    return "Arado Próximo [G]";
                }

                return "Engate Livre";
            }

            if (category == VehicleCategory.Truck)
            {
                return "Quinta Roda: Livre";
            }

            if (category == VehicleCategory.Plane)
            {
                return currentAltitudeMeters > 2f ? "Trem de Pouso: Recolhido" : "Trem de Pouso: Baixado";
            }

            if (category == VehicleCategory.Boat)
            {
                return "Âncora: Recolhida";
            }

            return "Engate Traseiro: Livre";
        }
    }
}
