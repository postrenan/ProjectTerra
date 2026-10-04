using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public partial class VehicleController
    {
        private void FixedUpdate()
        {
            switch (category)
            {
                case VehicleCategory.Tractor:
                case VehicleCategory.Truck:
                case VehicleCategory.Car:
                    FixedUpdateWheeledVehicle();
                    break;
                case VehicleCategory.Plane:
                    FixedUpdateAircraft();
                    break;
                case VehicleCategory.Boat:
                    FixedUpdateBoat();
                    break;
            }
        }

        private void FixedUpdateWheeledVehicle()
        {
            // Limitar aceleração se sem combustível
            float activeThrottle = fuelPercent > 0f ? currentThrottle : 0f;

            // Interação hidrodinâmica e inundação de motor em veículos terrestres
            if (WaterSystem.Instance != null && WaterSystem.Instance.GetWaterInfo(transform.position, out var wInfo))
            {
                isInWater = wInfo.isInWater;
                float waterDepthOnVehicle = wInfo.surfaceY - transform.position.y;
                currentSubmersion = Mathf.Max(0f, waterDepthOnVehicle);

                if (waterDepthOnVehicle > 0.15f)
                {
                    // Resistência hidrodinâmica nas rodas
                    float dragFactor = Mathf.Clamp(waterDepthOnVehicle * 1.6f, 0.4f, 4.0f);
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * dragFactor);

                    // Se água profunda cobrir o bloco do motor (> 0.95m)
                    if (waterDepthOnVehicle > 0.95f)
                    {
                        if (!isEngineFlooded)
                        {
                            isEngineFlooded = true;
                            if (isPlayerDriven)
                            {
                                SandboxHUD.Instance?.ShowToast("⚠️ Atenção: Motor inundado pela água profunda!", 3.0f);
                            }
                        }
                        activeThrottle = 0f; // Corta potência do motor inundado

                        // Empuxo parcial da carcaça
                        float vehicleBuoyancy = Mathf.Clamp(waterDepthOnVehicle * 5.0f, 0f, 18f);
                        rb.AddForce(Vector3.up * vehicleBuoyancy, ForceMode.Acceleration);
                    }
                    else
                    {
                        isEngineFlooded = false;
                    }

                    // Correnteza da água exercendo força no chassi
                    if (wInfo.flowVelocity.sqrMagnitude > 0.01f)
                    {
                        rb.AddForce(wInfo.flowVelocity * (waterDepthOnVehicle * 1.5f), ForceMode.Acceleration);
                    }
                }
                else
                {
                    isEngineFlooded = false;
                }
            }
            else
            {
                isInWater = false;
                isEngineFlooded = false;
                currentSubmersion = 0f;
            }

            // Força de tração com alto torque (garante superar atrito estático e puxar implementos/cargas)
            if (Mathf.Abs(activeThrottle) > 0.02f)
            {
                if (currentSpeedKmh < maxSpeedKmh || Mathf.Sign(activeThrottle) != Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward)))
                {
                    float accelFactor = category == VehicleCategory.Tractor ? 0.0038f : 0.0032f;
                    Vector3 force = transform.forward * (activeThrottle * enginePower * accelFactor);
                    rb.AddForce(force, ForceMode.Acceleration);
                }
            }
            else
            {
                // Freio motor / atrito de rolamento
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 0.4f);
            }

            // Esterçamento adaptativo com a velocidade
            float steerFactor = Mathf.Clamp01(1.0f - (currentSpeedKmh / (maxSpeedKmh * 1.5f)));
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, currentSteer * steerAngleMax * Mathf.Max(steerFactor, 0.4f), Time.fixedDeltaTime * steerSpeed);

            // Permite esterçar em movimento e também manobrar em marcha lenta / saída
            if (Mathf.Abs(currentSteer) > 0.05f && (currentSpeedKmh > 0.2f || Mathf.Abs(activeThrottle) > 0.05f))
            {
                float turnDir = Mathf.Abs(activeThrottle) > 0.05f ? Mathf.Sign(activeThrottle) : (Vector3.Dot(rb.linearVelocity, transform.forward) >= 0 ? 1f : -1f);
                float speedMultiplier = Mathf.Clamp(currentSpeedKmh / 15f, 0.45f, 1.0f);
                float yawDelta = (currentSteerAngle / steerAngleMax) * 50f * turnDir * speedMultiplier;
                Quaternion deltaRotation = Quaternion.Euler(0f, yawDelta * Time.fixedDeltaTime, 0f);
                rb.MoveRotation(rb.rotation * deltaRotation);
            }

            // Estabilização vertical contra tombamento
            Vector3 predictedUp = Quaternion.AngleAxis(rb.angularVelocity.magnitude * Mathf.Rad2Deg * 0.1f / 10f, rb.angularVelocity) * transform.up;
            Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
            rb.AddTorque(torqueVector * (rb.mass * 0.8f));
        }

        private void FixedUpdateAircraft()
        {
            float activeThrottle = fuelPercent > 0f ? Mathf.Max(0f, currentThrottle) : 0f;

            // Empuxo do motor frontal (Thrust proporcional à potência de 5200)
            Vector3 thrust = transform.forward * (activeThrottle * enginePower * 3.8f);
            rb.AddForce(thrust, ForceMode.Force);

            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float totalSpeed = rb.linearVelocity.magnitude;
            float vTakeoff = Mathf.Max(5f, takeOffSpeedKmh / 3.6f); // ~12.5 m/s (45 km/h)

            // Sustentação Aerodinâmica (Lift):
            // Na velocidade de decolagem (vTakeoff), o perfil alar gera sustentação positiva (1.25x peso)
            // permitindo que o avião decole naturalmente e suba suavemente
            if (forwardSpeed > 2.0f)
            {
                float speedRatio = forwardSpeed / vTakeoff;
                float baseWeight = rb.mass * Physics.gravity.magnitude;
                float liftMagnitude = baseWeight * Mathf.Min(4.0f, speedRatio * speedRatio * 1.25f);

                // Efeito do ângulo de ataque (cabrar/empinar o nariz aumenta a sustentação alar)
                float pitchBonus = 1.0f + Mathf.Clamp(transform.forward.y, -0.3f, 0.85f) * 1.4f;
                liftMagnitude *= pitchBonus;

                Vector3 lift = transform.up * liftMagnitude;
                rb.AddForce(lift, ForceMode.Force);
            }

            // Controles de Voo (Pitch, Roll, Yaw)
            bool inputBlocked = (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpenOrJustClosed) ||
                                (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen));

            float pitch = 0f;
            float roll = 0f;
            float yaw = 0f;

            if (!inputBlocked && isPlayerDriven)
            {
                float controlAuth = Mathf.Clamp01(forwardSpeed / (vTakeoff * 0.45f));
                float vertInput = TerraInput.GetAxis("Vertical");     // W=+1 (picar/descer), S=-1 (cabrar/subir)
                bool spaceClimb = TerraInput.GetKey(Key.Space);       // Barra de Espaço também sobe / decola

                if (spaceClimb && vertInput >= -0.1f)
                {
                    vertInput = -1.0f; // Cabrar / subir
                }

                float horizInput = TerraInput.GetAxis("Horizontal"); // D=+1, A=-1

                // S ou Espaço ou Seta Baixo: Cabrar (Nariz para CIMA / Subir) -> torque positivo
                // W ou Seta Cima: Picar (Nariz para BAIXO / Descer) -> torque negativo
                pitch = -vertInput * pitchRate * controlAuth;

                // A ou Seta Esq (-1): Rolar para a esquerda -> torque positivo
                // D ou Seta Dir (+1): Rolar para a direita -> torque negativo
                roll = -horizInput * rollRate * controlAuth;

                // Leme de cauda (Yaw) via Q / E
                if (TerraInput.GetKey(Key.Q)) yaw -= yawRate * controlAuth;
                if (TerraInput.GetKey(Key.E)) yaw += yawRate * controlAuth;

                // Direção no solo (Taxiing com bequilha dianteira):
                // No chão, A/D ou Q/E esterça o leme e a roda dianteira para manobrar na pista
                if (currentAltitudeMeters < 2.5f && forwardSpeed > 0.5f)
                {
                    float groundSteer = (TerraInput.GetKey(Key.Q) ? -1f : (TerraInput.GetKey(Key.E) ? 1f : horizInput));
                    yaw += groundSteer * yawRate * 1.2f;
                }
            }

            Vector3 angularTorque = transform.right * (pitch * 1.5f) + transform.forward * roll + transform.up * yaw;
            rb.AddTorque(angularTorque * (rb.mass * 0.28f), ForceMode.Force);

            // Curva coordenada natural: inclinação das asas (Bank Angle) gera giro suave no horizonte
            if (currentAltitudeMeters > 2.0f && totalSpeed > 8f)
            {
                float bankAngle = -Vector3.Dot(transform.right, Vector3.up); // Positivo se inclinado
                rb.AddTorque(Vector3.up * (bankAngle * rb.mass * 0.15f * Mathf.Clamp01(totalSpeed / 20f)), ForceMode.Force);
            }

            // Estabilização de rolamento (Roll/asas niveladas) no solo sem restringir a decolagem/cabrar:
            if (currentAltitudeMeters < 1.5f)
            {
                float rollTilt = Vector3.Dot(transform.right, Vector3.up);
                rb.AddTorque(-transform.forward * (rollTilt * rb.mass * 3.0f), ForceMode.Force);
            }

            // Alinhamento aerodinâmico natural no ar (fuselagem acompanha o fluxo de vento relativo)
            if (totalSpeed > 6f && currentAltitudeMeters > 0.8f)
            {
                Vector3 airFlowDir = rb.linearVelocity.normalized;
                Vector3 alignTorque = Vector3.Cross(transform.forward, airFlowDir);
                rb.AddTorque(alignTorque * (rb.mass * 0.9f), ForceMode.Force);
            }

            // Amortecimento aerodinâmico suave
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 0.6f);
        }

        private void FixedUpdateBoat()
        {
            float activeThrottle = fuelPercent > 0f ? currentThrottle : 0f;

            // Altura dinâmica da água, ondas e correnteza via WaterSystem
            float actualWaterY = waterLevelY;
            Vector3 flow = Vector3.zero;
            if (WaterSystem.Instance != null && WaterSystem.Instance.GetWaterInfo(transform.position, out var wInfo))
            {
                actualWaterY = wInfo.surfaceY;
                flow = wInfo.flowVelocity;
                isInWater = wInfo.isInWater;
            }
            else
            {
                isInWater = transform.position.y <= waterLevelY + 1.0f;
            }

            float submergedDepth = actualWaterY - transform.position.y;
            currentSubmersion = Mathf.Max(0f, submergedDepth);

            if (submergedDepth > 0f)
            {
                // Empuxo hidrostático de Arquimedes com amortecimento adaptativo
                float buoyantLift = Mathf.Clamp(submergedDepth * buoyancyForce, 0f, buoyancyForce * 2.8f);
                rb.AddForce(Vector3.up * buoyantLift * rb.mass * 0.1f, ForceMode.Acceleration);

                // Amortecimento de ondas e resistência na água
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), Time.fixedDeltaTime * waterDrag);

                // Correnteza da água
                if (flow.sqrMagnitude > 0.01f)
                {
                    rb.AddForce(flow * 2.2f, ForceMode.Acceleration);
                }

                // Amostragem multiponto para balanço com cristas e vales das ondas
                if (WaterSystem.Instance != null)
                {
                    float bowY = actualWaterY;
                    float sternY = actualWaterY;
                    if (WaterSystem.Instance.GetWaterInfo(transform.position + transform.forward * 3f, out var bowInfo))
                        bowY = bowInfo.surfaceY;
                    if (WaterSystem.Instance.GetWaterInfo(transform.position - transform.forward * 3f, out var sternInfo))
                        sternY = sternInfo.surfaceY;

                    float pitchDiff = bowY - sternY;
                    rb.AddTorque(transform.right * (pitchDiff * 12f * rb.mass * 0.01f), ForceMode.Acceleration);
                }
            }

            // Propulsão a hélice marítima
            if (Mathf.Abs(activeThrottle) > 0.02f)
            {
                Vector3 marineThrust = transform.forward * (activeThrottle * enginePower * 0.75f);
                rb.AddForce(marineThrust, ForceMode.Acceleration);
            }

            // Leme e curva na água
            if (currentSpeedKmh > 0.5f)
            {
                float rudderTurn = currentSteer * 35f * Mathf.Sign(activeThrottle >= 0 ? 1f : -1f);
                Quaternion deltaRotation = Quaternion.Euler(0f, rudderTurn * Time.fixedDeltaTime, 0f);
                rb.MoveRotation(rb.rotation * deltaRotation);
            }

            // Estabilização contra capotamento náutico
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (flatForward != Vector3.zero)
            {
                Quaternion targetUpRot = Quaternion.LookRotation(flatForward, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetUpRot, Time.fixedDeltaTime * 1.5f));
            }
        }
    }
}
