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
                    // Vector3.Lerp não clampa t; em time warp alto t > 1 inverte a velocidade.
                    // Usar decaimento exponencial (sempre em [0,1]) para evitar overshoot.
                    float dampT = 1f - Mathf.Exp(-dragFactor * Time.fixedDeltaTime);
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, dampT);

                    // Se água profunda cobrir o bloco do motor (> 0.95m)
                    if (waterDepthOnVehicle > 0.95f)
                    {
                        if (!isEngineFlooded)
                        {
                            isEngineFlooded = true;
                            if (isPlayerDriven)
                            {
                                SandboxHUD.Instance?.ShowToast("[Aviso] Atenção: Motor inundado pela água profunda!", 3.0f);
                            }
                        }
                        activeThrottle = 0f; // Corta potência do motor inundado

                        // Empuxo parcial da carcaça.
                        // O teto era 18 m/s² contra uma gravidade de 9,81: acima de ~2 m
                        // de água a aceleração líquida vertical ficava positiva e o
                        // veículo era arremessado para fora, caindo e sendo arremessado
                        // de novo indefinidamente. O teto tem de ficar ABAIXO da gravidade
                        // para o veículo afundar/emergir até o equilíbrio em vez de pular.
                        float vehicleBuoyancy = Mathf.Min(waterDepthOnVehicle * 5.0f, Physics.gravity.magnitude * 0.9f);
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

            // Inércia e resposta progressiva do acelerador (evita arrancadas instantâneas)
            float responseSpeed = (Mathf.Abs(activeThrottle) < Mathf.Abs(smoothedThrottle)) ? throttleResponseRate * 1.8f : throttleResponseRate;
            smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, activeThrottle, Time.fixedDeltaTime * responseSpeed);

            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float forwardSpeedKmh = forwardSpeed * 3.6f;

            // Tração com curva de torque progressiva, frenagem ativa e marcha à ré realista
            if (activeThrottle > 0.02f)
            {
                // Comando para frente (Acelerador)
                if (forwardSpeed < -0.5f)
                {
                    // Se o veículo estiver rolando para trás, 'W' atua como FREIO
                    float brakeForce = brakeDeceleration * Mathf.Abs(activeThrottle);
                    rb.AddForce(transform.forward * brakeForce, ForceMode.Acceleration);
                }
                else
                {
                    // Aceleração para frente com curva de torque que decai realisticamente com a velocidade
                    float targetMax = Mathf.Max(5f, maxSpeedKmh);
                    float speedRatio = Mathf.Clamp01(forwardSpeedKmh / targetMax);

                    // Curva de torque: força máxima em baixa velocidade, atenuando conforme atinge a velocidade máxima
                    float torqueCurve = Mathf.Clamp01(1.0f - Mathf.Pow(speedRatio, 1.4f));

                    // Assistência de torque em subidas íngremes para tratores e caminhões pesados
                    float slopeAssist = 0f;
                    if (category == VehicleCategory.Tractor || category == VehicleCategory.Truck)
                    {
                        float slope = -transform.forward.y;
                        if (slope > 0.04f && speedRatio < 0.65f)
                        {
                            slopeAssist = slope * 3.2f * (1.0f - speedRatio);
                        }
                    }

                    float netAccel = (accelerationRate * torqueCurve + slopeAssist) * Mathf.Max(0.05f, smoothedThrottle) * ConditionPowerFactor;

                    if (forwardSpeedKmh < maxSpeedKmh)
                    {
                        rb.AddForce(transform.forward * netAccel, ForceMode.Acceleration);
                    }
                    else
                    {
                        // Resistência do ar / limitador natural quando ultrapassa a velocidade de cruzeiro máxima
                        float overSpeed = forwardSpeedKmh - maxSpeedKmh;
                        rb.AddForce(-transform.forward * Mathf.Min(overSpeed * 0.4f, 3.5f), ForceMode.Acceleration);
                    }
                }
            }
            else if (activeThrottle < -0.02f)
            {
                // Comando de ré / freio
                if (forwardSpeed > 0.5f)
                {
                    // Se o veículo estiver em movimento para frente, 'S' atua como FREIO ativo
                    float brakeForce = brakeDeceleration * Mathf.Abs(activeThrottle);
                    rb.AddForce(-transform.forward * brakeForce, ForceMode.Acceleration);
                }
                else
                {
                    // Veículo parado ou em ré: marcha à ré com velocidade limitada e aceleração suave
                    float revSpeedKmh = -forwardSpeedKmh;
                    float targetRevMax = Mathf.Max(3f, maxReverseSpeedKmh);
                    float revRatio = Mathf.Clamp01(revSpeedKmh / targetRevMax);
                    float revTorqueCurve = Mathf.Clamp01(1.0f - (revRatio * revRatio));

                    float revAccel = (accelerationRate * 0.55f * revTorqueCurve) * Mathf.Abs(smoothedThrottle);

                    if (revSpeedKmh < targetRevMax)
                    {
                        rb.AddForce(-transform.forward * revAccel, ForceMode.Acceleration);
                    }
                    else
                    {
                        float overRev = revSpeedKmh - targetRevMax;
                        rb.AddForce(transform.forward * Mathf.Min(overRev * 0.4f, 2.5f), ForceMode.Acceleration);
                    }
                }
            }
            else
            {
                // Sem acelerador nem freio: Freio motor gradual e atrito de rolamento
                float coastDamping = (category == VehicleCategory.Tractor || category == VehicleCategory.Truck) ? 0.65f : 0.45f;
                float rollDampT = 1f - Mathf.Exp(-coastDamping * Time.fixedDeltaTime);
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, rollDampT);
            }

            // Checagem de contato com o solo para aplicação de atrito de direção e tração
            bool isGrounded = true;
            if (Physics.Raycast(transform.position + Vector3.up * 0.6f, Vector3.down, out RaycastHit groundHit, 2.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                isGrounded = true;
            }
            else if (RegionalSandboxManager.Instance != null)
            {
                float terrainY = RegionalSandboxManager.Instance.GetTerrainHeight(transform.position);
                isGrounded = (transform.position.y - terrainY) < 1.9f;
            }

            // Esterçamento adaptativo com a velocidade (evita sobre-esterço brusco em alta velocidade)
            float steerFactor = Mathf.Clamp01(1.0f - (currentSpeedKmh / (maxSpeedKmh * 1.6f)));
            float targetAngle = currentSteer * steerAngleMax * Mathf.Max(steerFactor, 0.45f);
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.fixedDeltaTime * steerSpeed);

            // Permite esterçar em movimento e também manobrar em marcha lenta / saída
            if (isGrounded && Mathf.Abs(currentSteer) > 0.04f && (currentSpeedKmh > 0.15f || Mathf.Abs(activeThrottle) > 0.05f))
            {
                // Sentido da curva: se ré, inverte guinada para comportamento natural de esterçamento
                float turnDir = (forwardSpeed < -0.3f || (activeThrottle < -0.05f && forwardSpeed < 0.3f)) ? -1f : 1f;

                // Modulação de velocidade: parado sem acelerador não gira feito pião
                float minTurnMult = Mathf.Abs(activeThrottle) > 0.05f ? 0.35f : 0f;
                float speedMultiplier = Mathf.Clamp(currentSpeedKmh / 12f, minTurnMult, 1.0f);

                // Taxa máxima de guinada calibrada por categoria (trator mais sólido e pesado)
                float maxYawRate = category == VehicleCategory.Tractor ? 38f : (category == VehicleCategory.Truck ? 32f : 48f);

                float yawDelta = (currentSteerAngle / steerAngleMax) * maxYawRate * turnDir * speedMultiplier;
                Quaternion deltaRotation = Quaternion.Euler(0f, yawDelta * Time.fixedDeltaTime, 0f);
                rb.MoveRotation(rb.rotation * deltaRotation);
            }
            else if (Mathf.Abs(currentSteer) < 0.04f)
            {
                // Alinhamento reto: amortece velocidade angular residual em Y para travar oscilações
                Vector3 angVel = rb.angularVelocity;
                angVel.y = Mathf.MoveTowards(angVel.y, 0f, Time.fixedDeltaTime * 8f);
                rb.angularVelocity = angVel;
            }

            // -------------------------------------------------------------------------
            // SISTEMA ANTI-DRIFT & ADERÊNCIA LATERAL DOS PNEUS (TIRE LATERAL GRIP)
            // Cancela completamente o deslizamento lateral nas curvas (efeito "sabão"/drift)
            // e converte a inércia suavemente para a direção frontal da curva.
            // -------------------------------------------------------------------------
            if (isGrounded)
            {
                Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
                float curForward = localVel.z;
                float curLateral = localVel.x;
                float curVertical = localVel.y;

                float baseGrip = category == VehicleCategory.Tractor ? 36f : (category == VehicleCategory.Truck ? 28f : 24f);
                float effectiveGrip = isInWater ? baseGrip * 0.5f : baseGrip;
                float redirectionRatio = category == VehicleCategory.Tractor ? 0.95f : (category == VehicleCategory.Truck ? 0.90f : 0.85f);

                // Velocidade horizontal total combinada
                float totalHorizSpeed = Mathf.Sqrt(curLateral * curLateral + curForward * curForward);
                float targetForward = (curForward >= -0.2f ? 1f : -1f) * totalHorizSpeed;

                // Anula a componente lateral (drift) com decaimento suave e instantâneo
                float lateralDamp = 1f - Mathf.Exp(-effectiveGrip * Time.fixedDeltaTime);
                localVel.x = Mathf.Lerp(curLateral, 0f, lateralDamp);

                // Redireciona o vetor de velocidade para acompanhar a frente do veículo na curva
                localVel.z = Mathf.Lerp(curForward, targetForward, redirectionRatio * lateralDamp);

                // Preserva rigorosamente a velocidade vertical Y (relevo, aclives, gravidade, saltos)
                localVel.y = curVertical;

                rb.linearVelocity = transform.TransformDirection(localVel);
            }

            // Estabilização vertical contra tombamento
            Vector3 predictedUp = Quaternion.AngleAxis(rb.angularVelocity.magnitude * Mathf.Rad2Deg * 0.1f / 10f, rb.angularVelocity) * transform.up;
            Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
            rb.AddTorque(torqueVector * (rb.mass * 0.8f));
        }

        private void FixedUpdateAircraft()
        {
            float activeThrottle = fuelPercent > 0f ? Mathf.Max(0f, currentThrottle) : 0f;

            // Suavização do manete de aceleração
            smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, activeThrottle, Time.fixedDeltaTime * throttleResponseRate);

            // Empuxo do motor frontal proporcional à potência e velocidade no ar
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float forwardSpeedKmh = forwardSpeed * 3.6f;
            float totalSpeed = rb.linearVelocity.magnitude;
            float vTakeoff = Mathf.Max(5f, takeOffSpeedKmh / 3.6f); // ~18.0 m/s (65 km/h)

            float airSpeedRatio = Mathf.Clamp01(forwardSpeedKmh / Mathf.Max(20f, maxSpeedKmh));
            float thrustFalloff = Mathf.Clamp01(1.0f - Mathf.Pow(airSpeedRatio, 1.5f));

            // Aceleração máxima realista do empuxo da hélice (~2.6 m/s² na rolagem de pista)
            float effectiveThrustAccel = accelerationRate * smoothedThrottle * Mathf.Max(0.12f, thrustFalloff);
            Vector3 thrust = transform.forward * (effectiveThrustAccel * rb.mass);
            rb.AddForce(thrust, ForceMode.Force);

            // Freio de rodas no solo durante táxi/rolamento (se throttle estiver zerado)
            if (currentAltitudeMeters < 1.0f && smoothedThrottle < 0.05f && forwardSpeed > 0.4f)
            {
                bool brakePressed = TerraInput.GetKey(Key.LeftCtrl) || TerraInput.GetKey(Key.S) || TerraInput.GetKey(Key.C);
                float brakeStrength = brakePressed ? brakeDeceleration : 0.6f;
                rb.AddForce(-transform.forward * (brakeStrength * rb.mass), ForceMode.Force);
            }

            // Sustentação Aerodinâmica (Lift):
            // Na velocidade de decolagem (vTakeoff), o perfil alar gera sustentação positiva (~1.05x peso)
            // permitindo que o avião decole naturalmente e suba suavemente
            if (forwardSpeed > 2.0f)
            {
                float speedRatio = forwardSpeed / vTakeoff;
                float baseWeight = rb.mass * Physics.gravity.magnitude;
                float liftMagnitude = baseWeight * Mathf.Min(3.2f, speedRatio * speedRatio * 1.05f);

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
                float vertInput = TerraInput.GetAxis("Vertical"); // W=+1 (picar/descer), S=-1 (cabrar/subir)
                bool spaceClimb = TerraInput.GetKey(Key.Space); // Barra de Espaço também sobe / decola

                if (spaceClimb && vertInput >= -0.1f)
                {
                    vertInput = -1.0f; // Cabrar / subir
                }

                float horizInput = TerraInput.GetAxis("Horizontal"); // D=+1, A=-1

                // Torque positivo em torno de transform.right (rotação para +Y depois -Z)
                // LEVA O NARIZ PARA BAIXO: em Unity uma rotação positiva sobre +X leva
                // +Z (frente) até -Y. Logo o sinal tem de ser:
                // W (+1) -> torque positivo -> nariz para baixo (picar)
                // S/Espaço (-1) -> torque negativo -> nariz para cima (cabrar)
                // Com o sinal anterior (-vertInput) era o contrário: W subia e o
                // Espaço ("sobe/decola") mergulhava o nariz.
                pitch = vertInput * pitchRate * controlAuth;

                // A ou Seta Esq (-1): Rolar para a esquerda -> torque positivo
                // D ou Seta Dir (+1): Rolar para a direita -> torque negativo
                roll = -horizInput * rollRate * controlAuth;

                // Leme de cauda (Yaw) via , e .
                // NÃO pode usar E: PlayerCharacterController.Vehicle.cs trata Key.E
                // como "desembarcar"e roda no mesmo frame, então tocar o leme
                // direito por um único frame expulsava o piloto e o deixava caindo
                // de 800 m no vazio. Virgula/ponto é o par natural de guinada e
                // estava livre (o warp de tempo usa [ e ] como teclas primárias).
                if (TerraInput.GetKey(Key.Comma)) yaw -= yawRate * controlAuth;
                if (TerraInput.GetKey(Key.Period)) yaw += yawRate * controlAuth;

                // Direção no solo (Taxiing com bequilha dianteira):
                // No chão, A/D ou , e . esterça o leme e a roda dianteira para manobrar na pista
                if (currentAltitudeMeters < 2.5f && forwardSpeed > 0.5f)
                {
                    float groundSteer = (TerraInput.GetKey(Key.Comma) ? -1f : (TerraInput.GetKey(Key.Period) ? 1f : horizInput));
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
            // Vector3.Lerp não clampa t; usar decaimento exponencial para time warp seguro.
            float aeroDampT = 1f - Mathf.Exp(-0.6f * Time.fixedDeltaTime);
            rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, aeroDampT);
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
                // Empuxo hidrostático de Arquimedes com amortecimento adaptativo.
                // ForceMode.Acceleration JÁ é independente de massa: o argumento É a
                // aceleração em m/s². Multiplicar por rb.mass (3200 no barco) não
                // "compensa"a massa, eleva o empuxo 3200x -> ~22.000 m/s² e o barco
                // era arremessado para fora da água. Aqui o empuxo é uma aceleração
                // real, com teto logo acima da gravidade para o casco flutuar em
                // equilíbrio (aprofundamento de projeto) em vez de ser catapultado.
                float buoyantAccel = Mathf.Min(submergedDepth * 4f, Physics.gravity.magnitude * 1.15f);
                rb.AddForce(Vector3.up * buoyantAccel, ForceMode.Acceleration);

                // Amortecimento de ondas e resistência na água.
                // Vector3.Lerp NÃO limita t como Mathf.Lerp: acima de ~18x de warp o
                // dt fixo passa de 0.55 s, t > 1 e a velocidade ATRAVESSA o zero
                // invertendo o sentido. Exp(1-t) é limitado em [0,1] para qualquer dt.
                // Vector3.Lerp não clampa t; em time warp alto t > 1 extrapola.
                // Usar decaimento exponencial (sempre em [0,1]) para evitar overshoot.
                float waterDampT = 1f - Mathf.Exp(-waterDrag * Time.fixedDeltaTime);
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), waterDampT);

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
                    // Mesma armadilha do empuxo: em ForceMode.Acceleration a massa não
                    // precisa (e não pode) entrar no cálculo. Com rb.mass aqui o torque
                    // era ~384x maior e qualquer onda de 0,2 m girava o casco violentamente.
                    rb.AddTorque(transform.right * (pitchDiff * 12f * 0.01f), ForceMode.Acceleration);
                }
            }

            // Suavização do comando do manete marítimo
            smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, activeThrottle, Time.fixedDeltaTime * throttleResponseRate);

            // Propulsão marítima com curva de arrasto hidrodinâmico e ré realista
            if (activeThrottle > 0.02f)
            {
                float speedRatio = Mathf.Clamp01(currentSpeedKmh / Mathf.Max(5f, maxSpeedKmh));
                float thrustFactor = Mathf.Clamp01(1.0f - (speedRatio * speedRatio));
                float boatAccel = accelerationRate * smoothedThrottle * thrustFactor;

                if (currentSpeedKmh < maxSpeedKmh)
                {
                    rb.AddForce(transform.forward * boatAccel, ForceMode.Acceleration);
                }
            }
            else if (activeThrottle < -0.02f)
            {
                float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
                if (forwardSpeed > 0.4f)
                {
                    // Passo reverso da hélice atua como freio hidrodinâmico na água
                    rb.AddForce(-transform.forward * (brakeDeceleration * Mathf.Abs(smoothedThrottle)), ForceMode.Acceleration);
                }
                else
                {
                    float revSpeedKmh = -forwardSpeed * 3.6f;
                    float revRatio = Mathf.Clamp01(revSpeedKmh / Mathf.Max(3f, maxReverseSpeedKmh));
                    float revFactor = Mathf.Clamp01(1.0f - (revRatio * revRatio));
                    float revAccel = (accelerationRate * 0.5f) * Mathf.Abs(smoothedThrottle) * revFactor;

                    if (revSpeedKmh < maxReverseSpeedKmh)
                    {
                        rb.AddForce(-transform.forward * revAccel, ForceMode.Acceleration);
                    }
                }
            }

            // Leme e estabilização contra capotamento, fundidos em UMA única chamada.
            // MoveRotation define um único alvo consumido no próximo passo da simulação;
            // a segunda chamada no mesmo FixedUpdate sobrescrevia a primeira e o leme
            // era descartado. Pior: targetUpRot é construído com o forward do frame
            // ANTERIOR, então a guinada líquida da chamada sobrevivente era exatamente
            // zero — o barco acelerava em linha reta para sempre, sem resposta ao leme.
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (flatForward != Vector3.zero)
            {
                Quaternion targetUpRot = Quaternion.LookRotation(flatForward, Vector3.up);

                if (currentSpeedKmh > 0.5f)
                {
                    float rudderTurn = currentSteer * 35f * (activeThrottle >= 0f ? 1f : -1f);
                    targetUpRot = Quaternion.Euler(0f, rudderTurn * Time.fixedDeltaTime, 0f) * targetUpRot;
                }

                // Quaternion.Slerp também NÃO limita t (ao contrário de Mathf.Lerp),
                // então em warp alto ele extrapolaria além do alvo. exp(1-t) é limitado.
                float uprightT = 1f - Mathf.Exp(-1.5f * Time.fixedDeltaTime);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetUpRot, uprightT));
            }
        }
    }
}
