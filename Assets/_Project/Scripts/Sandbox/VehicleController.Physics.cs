using UnityEngine;

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

            // Força de tração
            if (Mathf.Abs(activeThrottle) > 0.02f)
            {
                if (currentSpeedKmh < maxSpeedKmh || Mathf.Sign(activeThrottle) != Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward)))
                {
                    // ForceMode.Acceleration já ignora a massa (aplica m/s² direto),
                    // então NÃO multiplicar por rb.mass. enginePower * 0.001 define a aceleração em m/s²
                    // (ex.: trator 3200 -> 3.2 m/s²; caminhão 4800 -> 4.8 m/s²).
                    Vector3 force = transform.forward * (activeThrottle * enginePower * 0.001f);
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

            if (currentSpeedKmh > 1.0f)
            {
                float turnDir = Mathf.Sign(Vector3.Dot(rb.linearVelocity, transform.forward));
                float yawDelta = (currentSteerAngle / steerAngleMax) * 45f * turnDir * (Mathf.Clamp(currentSpeedKmh / 20f, 0.1f, 1f));
                Quaternion deltaRotation = Quaternion.Euler(0f, yawDelta * Time.fixedDeltaTime, 0f);
                rb.MoveRotation(rb.rotation * deltaRotation);
            }

            // Estabilização vertical contra tombamento
            Vector3 predictedUp = Quaternion.AngleAxis(rb.angularVelocity.magnitude * Mathf.Rad2Deg * 0.1f / 10f, rb.angularVelocity) * transform.up;
            Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
            rb.AddTorque(torqueVector * (rb.mass * 0.5f));
        }

        private void FixedUpdateAircraft()
        {
            float activeThrottle = fuelPercent > 0f ? Mathf.Max(0f, currentThrottle) : 0f;

            // Empuxo do motor frontal
            Vector3 thrust = transform.forward * (activeThrottle * enginePower * 1.5f);
            rb.AddForce(thrust, ForceMode.Force);

            // Sustentação Aerodinâmica (Lift) proporcional ao quadrado da velocidade à frente
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (forwardSpeed > 3f)
            {
                float liftMagnitude = (forwardSpeed * forwardSpeed) * liftCoefficient * 0.35f;
                // Sustentação direcionada perpendicularmente às asas
                Vector3 lift = transform.up * Mathf.Min(liftMagnitude, rb.mass * Physics.gravity.magnitude * 2.2f);
                rb.AddForce(lift, ForceMode.Force);

                // Controle aerodinâmico em voo (Pitch, Roll, Yaw)
                float controlAuth = Mathf.Clamp01(forwardSpeed / (takeOffSpeedKmh / 3.6f));

                float pitch = -Input.GetAxis("Vertical") * pitchRate * controlAuth; // W empina para baixo, S para cima
                float roll = -Input.GetAxis("Horizontal") * rollRate * controlAuth; // A inclina esq, D inclina dir
                float yaw = 0f;
                if (Input.GetKey(KeyCode.Q)) yaw -= yawRate * controlAuth;
                if (Input.GetKey(KeyCode.E)) yaw += yawRate * controlAuth;

                Vector3 angularTorque = transform.right * pitch + transform.forward * roll + transform.up * yaw;
                rb.AddTorque(angularTorque * (rb.mass * 0.08f), ForceMode.Force);
            }

            // Alinhamento aerodinâmico natural (aeronave busca apontar na direção do fluxo de ar)
            if (rb.linearVelocity.magnitude > 5f)
            {
                Vector3 airFlowDir = rb.linearVelocity.normalized;
                Vector3 alignTorque = Vector3.Cross(transform.forward, airFlowDir);
                rb.AddTorque(alignTorque * (rb.mass * 0.8f), ForceMode.Force);
            }
        }

        private void FixedUpdateBoat()
        {
            float activeThrottle = fuelPercent > 0f ? currentThrottle : 0f;

            // Flutuação hidrostática simples
            float submergedDepth = waterLevelY - transform.position.y;
            if (submergedDepth > 0f)
            {
                // Empuxo de Arquimedes
                float buoyantLift = Mathf.Clamp(submergedDepth * buoyancyForce, 0f, buoyancyForce * 2.5f);
                rb.AddForce(Vector3.up * buoyantLift * rb.mass * 0.1f, ForceMode.Acceleration);

                // Amortecimento de ondas e resistência na água
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z), Time.fixedDeltaTime * 2.5f);
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
