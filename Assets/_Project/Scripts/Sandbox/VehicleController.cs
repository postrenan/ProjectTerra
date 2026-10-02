using System;
using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public enum VehicleCategory
    {
        Tractor,
        Truck,
        Plane,
        Boat
    }

    /// <summary>
    /// Controlador físico unificado ("Simcade") para os 4 tipos de veículos do Marco 1:
    /// Trator Agrícola, Caminhão de Carga, Avião Monomotor e Barco Pesqueiro.
    /// Inclui física imersiva (aceleração, inércia, sustentação aerodinâmica, flutuação aquática)
    /// e sistema de embarque/desembarque de condutor.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Header("Configuração Geral")]
        public VehicleCategory category = VehicleCategory.Tractor;
        public string vehicleName = "Veículo Utilitário";
        public Transform driverSeatPoint;
        public Transform exitPoint;
        public Transform cockpitCameraPoint;

        public Vector3 GetCockpitPosition()
        {
            if (cockpitCameraPoint != null)
                return cockpitCameraPoint.position;
            if (driverSeatPoint != null)
                return driverSeatPoint.position + transform.up * 0.35f + transform.forward * 0.15f;
            return transform.position + transform.up * 1.5f;
        }

        [Header("Especificações de Motor & Movimento")]
        public float maxSpeedKmh = 45f;
        public float enginePower = 2500f;
        public float brakePower = 3500f;
        public float steerAngleMax = 35f;
        public float steerSpeed = 4f;

        [Header("Específico para Avião")]
        public float takeOffSpeedKmh = 45f;
        public float liftCoefficient = 1.8f;
        public float pitchRate = 45f;
        public float rollRate = 60f;
        public float yawRate = 30f;
        public Transform propellerTransform;
        public float propellerMaxRpm = 1800f;

        [Header("Específico para Barco")]
        public float waterLevelY = 0.5f;
        public float buoyancyForce = 25f;
        public float waterDrag = 1.8f;

        [Header("Telemetria & Consumo")]
        [Range(0f, 100f)] public float fuelPercent = 100f;
        public float fuelConsumptionRate = 0.05f; // % por segundo em aceleração
        [Range(0f, 100f)] public float cargoFillPercent = 0f;
        public float maxCargoCapacityKg = 1500f;
        public string cargoItemName = "Vazio";

        [Header("Estado")]
        public bool isPlayerDriven = false;
        public float currentSpeedKmh { get; private set; }
        public float currentThrottle { get; private set; }
        public float currentSteer { get; private set; }

        private Rigidbody rb;
        private float currentSteerAngle = 0f;
        private float propellerAngle = 0f;

        // Efeitos visuais e rodas
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ConfigureRigidbodyForCategory();
        }

        private void ConfigureRigidbodyForCategory()
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            switch (category)
            {
                case VehicleCategory.Tractor:
                    rb.mass = 3800f;
                    rb.linearDamping = 0.4f;
                    rb.angularDamping = 2.0f;
                    rb.centerOfMass = new Vector3(0f, -0.4f, 0f);
                    break;
                case VehicleCategory.Truck:
                    rb.mass = 4500f;
                    rb.linearDamping = 0.3f;
                    rb.angularDamping = 2.2f;
                    rb.centerOfMass = new Vector3(0f, -0.3f, 0.2f);
                    break;
                case VehicleCategory.Plane:
                    rb.mass = 1200f;
                    rb.linearDamping = 0.15f;
                    rb.angularDamping = 1.2f;
                    rb.centerOfMass = new Vector3(0f, -0.1f, 0.3f);
                    break;
                case VehicleCategory.Boat:
                    rb.mass = 3200f;
                    rb.linearDamping = waterDrag;
                    rb.angularDamping = 2.5f;
                    rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
                    break;
            }
        }

        private void Update()
        {
            currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;

            if (isPlayerDriven)
            {
                ReadInputs();
            }
            else
            {
                currentThrottle = 0f;
                currentSteer = 0f;
            }

            UpdateVisuals();
        }

        private void FixedUpdate()
        {
            switch (category)
            {
                case VehicleCategory.Tractor:
                case VehicleCategory.Truck:
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

        private void ReadInputs()
        {
            float vertical = Input.GetAxis("Vertical");   // W/S ou Seta Cima/Baixo
            float horizontal = Input.GetAxis("Horizontal"); // A/D ou Seta Esq/Dir

            currentThrottle = vertical;
            currentSteer = horizontal;

            // Consumo de combustível se estiver acelerando
            if (Mathf.Abs(currentThrottle) > 0.05f && fuelPercent > 0f)
            {
                fuelPercent = Mathf.Max(0f, fuelPercent - fuelConsumptionRate * Time.deltaTime);
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
                    Vector3 force = transform.forward * (activeThrottle * enginePower * rb.mass * 0.001f);
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

        private void UpdateVisuals()
        {
            // Girar hélice do avião
            if (propellerTransform != null)
            {
                float rpm = isPlayerDriven ? Mathf.Lerp(300f, propellerMaxRpm, Mathf.Max(0.1f, currentThrottle)) : 0f;
                propellerAngle += rpm * 6f * Time.deltaTime;
                propellerTransform.localRotation = Quaternion.Euler(0f, 0f, propellerAngle);
            }

            // Esterçar rodas dianteiras se atribuídas
            if (frontLeftWheel != null) frontLeftWheel.localEulerAngles = new Vector3(0f, currentSteerAngle, 0f);
            if (frontRightWheel != null) frontRightWheel.localEulerAngles = new Vector3(0f, currentSteerAngle, 0f);
        }

        public void LoadCargo(string itemName, float amountKg)
        {
            cargoItemName = itemName;
            cargoFillPercent = Mathf.Clamp((amountKg / maxCargoCapacityKg) * 100f, 0f, 100f);
            Debug.Log($"[{vehicleName}] Carga carregada: {amountKg} kg de {itemName} ({cargoFillPercent:F0}% ocupado).");
        }

        public float UnloadCargo()
        {
            float amountKg = (cargoFillPercent / 100f) * maxCargoCapacityKg;
            cargoFillPercent = 0f;
            cargoItemName = "Vazio";
            Debug.Log($"[{vehicleName}] Carga descarregada: {amountKg} kg.");
            return amountKg;
        }

        public void Refuel(float amountPercent = 100f)
        {
            fuelPercent = Mathf.Clamp(fuelPercent + amountPercent, 0f, 100f);
        }
    }
}
