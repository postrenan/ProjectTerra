using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    public enum VehicleCategory
    {
        Tractor,
        Truck,
        Plane,
        Boat,
        Car
    }

    /// <summary>
    /// Controlador físico unificado ("Simcade") para veículos da simulação:
    /// Trator Agrícola, Caminhão de Carga, Avião Monomotor, Barco Pesqueiro e Carro/Utilitário.
    /// Inclui física imersiva, faróis dianteiros [L], telemetria e sistema de condução.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public partial class VehicleController : MonoBehaviour
    {
        [Header("Configuração Geral")]
        private VehicleCategory _category = VehicleCategory.Tractor;

        /// <summary>
        /// Categoria do veículo. Atribuir reconfigura na hora massa, amortecimento e centro
        /// de massa. Antes era um campo público: o builder fazia root.AddComponent (que
        /// dispara Awake e configurava o Rigidbody com o default Tractor) e só DEPOIS
        /// atribuía a categoria — então avião, barco e carro ficavam todos com 3800 kg e o
        /// CoM do trator, e toda a física que escala com rb.mass saía calibrada errado.
        /// </summary>
        public VehicleCategory category
        {
            get { return _category; }
            set
            {
                if (_category == value) return;
                _category = value;
                if (rb != null) ConfigureRigidbodyForCategory();
            }
        }

        public string vehicleName = "Veículo Utilitário";
        public Transform driverSeatPoint;
        public Transform exitPoint;
        public Transform cockpitCameraPoint;
        public Transform rearHitchPoint; // Ponto de engate traseiro para implementos (arado, carretas)

        [Header("Faróis Dianteiros")]
        public Light[] headLights;
        public bool headLightsOn = false;

        public Vector3 GetCockpitPosition()
        {
            if (cockpitCameraPoint != null)
                return cockpitCameraPoint.position;
            if (driverSeatPoint != null)
                return driverSeatPoint.position + transform.up * 0.35f + transform.forward * 0.15f;
            return transform.position + transform.up * 1.5f;
        }

        [Header("Especificações de Motor & Movimento Realistas")]
        public float maxSpeedKmh = 45f;
        public float maxReverseSpeedKmh = 18f;
        public float accelerationRate = 3.0f; // Aceleração base máxima em m/s²
        public float brakeDeceleration = 7.5f; // Desaceleração de frenagem em m/s²
        public float throttleResponseRate = 2.5f; // Taxa de subida suave da aceleração / inércia do motor
        public float enginePower = 2500f;
        public float brakePower = 3500f;
        public float steerAngleMax = 35f;
        public float steerSpeed = 4f;

        // Inércia de entrada / resposta progressiva do acelerador
        public float smoothedThrottle { get; private set; } = 0f;

        [Header("Específico para Avião")]
        public float takeOffSpeedKmh = 65f;
        public float liftCoefficient = 1.8f;
        public float pitchRate = 45f;
        public float rollRate = 60f;
        public float yawRate = 30f;
        public Transform propellerTransform;
        public float propellerMaxRpm = 1800f;
        [Range(0f, 1f)] public float aircraftThrottle = 0f;
        public float currentAltitudeMeters { get; private set; }

        [Header("Específico para Barco")]
        public float waterLevelY = 0.5f;
        public float buoyancyForce = 25f;
        public float waterDrag = 1.8f;

        [Header("Telemetria & Consumo")]
        [Range(0f, 100f)] public float fuelPercent = 100f;
        public float fuelConsumptionRate = 0.05f; // % por segundo em aceleração
        public float fuelTankLiters = 150f; // Capacidade do tanque (define o custo de reabastecer)
        [Range(0f, 100f)] public float cargoFillPercent = 0f;
        public float maxCargoCapacityKg = 1500f;
        public string cargoItemName = "Vazio";
        public bool infiniteFuel = false;

        [Header("Condição & Desgaste")]
        [Range(0f, 100f)] public float conditionPercent = 100f; // 100 = perfeito, 0 = sucata
        public float wearPerKm = 0.06f; // perda de condição base por km rodado (× dificuldade)

        /// <summary>
        /// Fator de potência pelo estado do veículo: 1.0 em perfeito estado, caindo até ~0.6
        /// quando sucateado. Multiplica a aceleração na física para dar peso ao desgaste.
        /// </summary>
        public float ConditionPowerFactor => 0.6f + 0.4f * Mathf.Clamp01(conditionPercent / 100f);

        /// <summary>Consumo efetivo: um motor desgastado bebe mais combustível.</summary>
        public float EffectiveFuelConsumption => fuelConsumptionRate * (1f + (1f - Mathf.Clamp01(conditionPercent / 100f)) * 0.5f);

        private bool AdvancedEconomyActive =>
            SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null && SaveManager.Instance.ActiveSave.advancedEconomy;

        private float WearMultiplier =>
            SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null
                ? SaveManager.Instance.ActiveSave.DifficultyConfig.wearMultiplier : 1f;

        [Header("Estado")]
        public bool isPlayerDriven = false;
        public float currentSpeedKmh { get; private set; }
        public float currentThrottle { get; private set; }
        public float currentSteer { get; private set; }
        public bool isInWater { get; private set; } = false;
        public bool isEngineFlooded { get; private set; } = false;
        public float currentSubmersion { get; private set; } = 0f;

        private Rigidbody rb;
        private float currentSteerAngle = 0f;
        private float propellerAngle = 0f;

        // Efeitos visuais e rodas
        public Transform frontLeftWheel;
        public Transform frontRightWheel;
        public Transform rearLeftWheel;
        public Transform rearRightWheel;

        // Áudio do Motor
        private AudioSource engineAudioSource;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // Pode já vir com a categoria atribuída por Initialize() (o builder chama
            // Initialize antes de AddComponent em alguns caminhos); senão usa o default.
            ConfigureRigidbodyForCategory();
            SetupEngineAudio();
        }

        /// <summary>
        /// (Re)aplica massa, amortecimento e centro de massa da categoria atual.
        /// Precisa ser chamado de novo sempre que <see cref="category"/> mudar em runtime,
        /// porque toda a física downstream escala com rb.mass.
        /// </summary>
        public void ConfigureRigidbodyForCategory()
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            switch (category)
            {
                case VehicleCategory.Tractor:
                    rb.mass = 3800f;
                    rb.linearDamping = 0.4f;
                    rb.angularDamping = 2.5f;
                    rb.centerOfMass = new Vector3(0f, -0.45f, 0f);
                    fuelTankLiters = 180f;
                    break;
                case VehicleCategory.Truck:
                    rb.mass = 4500f;
                    rb.linearDamping = 0.3f;
                    rb.angularDamping = 2.5f;
                    rb.centerOfMass = new Vector3(0f, -0.35f, 0.2f);
                    fuelTankLiters = 500f;
                    break;
                case VehicleCategory.Car:
                    rb.mass = 1600f;
                    rb.linearDamping = 0.25f;
                    rb.angularDamping = 2.2f;
                    rb.centerOfMass = new Vector3(0f, -0.4f, 0.1f);
                    fuelTankLiters = 60f;
                    break;
                case VehicleCategory.Plane:
                    rb.mass = 1200f;
                    rb.linearDamping = 0.15f;
                    rb.angularDamping = 1.2f;
                    rb.centerOfMass = new Vector3(0f, -0.1f, 0.3f);
                    fuelTankLiters = 150f;
                    break;
                case VehicleCategory.Boat:
                    rb.mass = 3200f;
                    rb.linearDamping = waterDrag;
                    rb.angularDamping = 2.5f;
                    rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
                    fuelTankLiters = 250f;
                    break;
            }
        }

        private void Update()
        {
            currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;

            if (category == VehicleCategory.Plane)
            {
                float groundY = RegionalSandboxManager.Instance != null ?
                    RegionalSandboxManager.Instance.GetTerrainHeight(transform.position) : 0f;
                currentAltitudeMeters = Mathf.Max(0f, transform.position.y - groundY);
            }

            if (isPlayerDriven)
            {
                ReadInputs();
            }
            else
            {
                currentThrottle = 0f;
                currentSteer = 0f;
                if (category == VehicleCategory.Plane)
                {
                    aircraftThrottle = Mathf.MoveTowards(aircraftThrottle, 0f, Time.deltaTime * 0.5f);
                }
            }

            AccrueDistanceWear();
            UpdateVisuals();
            UpdateEngineAudio();
        }

        private void ReadInputs()
        {
            // Ignora entrada de direção se o console de comandos ou mapa estiver aberto
            if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpenOrJustClosed)
            {
                currentThrottle = 0f;
                currentSteer = 0f;
                return;
            }

            if (SandboxHUD.Instance != null && (SandboxHUD.Instance.IsRegionalMapOpen || SandboxHUD.Instance.IsOptionsMenuOpen || SandboxHUD.Instance.IsBrowserOpen))
            {
                currentThrottle = 0f;
                currentSteer = 0f;
                return;
            }

            // Tratamento especializado para Avião:
            if (category == VehicleCategory.Plane)
            {
                bool throttleUp = TerraInput.GetKey(Key.LeftShift) || TerraInput.GetKey(Key.RightShift);
                bool throttleDown = TerraInput.GetKey(Key.LeftCtrl) || TerraInput.GetKey(Key.RightCtrl) || TerraInput.GetKey(Key.C);
                float vert = TerraInput.GetAxis("Vertical");

                // Apenas Shift acelera potência do motor (potência de cruzeiro persistente)
                if (throttleUp)
                {
                    aircraftThrottle = Mathf.MoveTowards(aircraftThrottle, 1.0f, Time.deltaTime * 0.9f);
                }
                else if (throttleDown)
                {
                    aircraftThrottle = Mathf.MoveTowards(aircraftThrottle, 0.0f, Time.deltaTime * 0.85f);
                }

                currentThrottle = aircraftThrottle;
                currentSteer = TerraInput.GetAxis("Horizontal");

                if (!infiniteFuel && aircraftThrottle > 0.05f && fuelPercent > 0f)
                {
                    fuelPercent = Mathf.Max(0f, fuelPercent - EffectiveFuelConsumption * aircraftThrottle * Time.deltaTime);
                }
                return;
            }

            float vertical = TerraInput.GetAxis("Vertical"); // W/S ou Seta Cima/Baixo
            float horizontal = TerraInput.GetAxis("Horizontal"); // A/D ou Seta Esq/Dir

            currentThrottle = vertical;
            currentSteer = horizontal;

            // Consumo de combustível se estiver acelerando
            if (!infiniteFuel && Mathf.Abs(currentThrottle) > 0.05f && fuelPercent > 0f)
            {
                fuelPercent = Mathf.Max(0f, fuelPercent - EffectiveFuelConsumption * Time.deltaTime);
            }
        }

        /// <summary>Desgaste por distância: o veículo perde condição conforme roda (× dificuldade).</summary>
        private void AccrueDistanceWear()
        {
            if (!AdvancedEconomyActive || !isPlayerDriven || conditionPercent <= 0f) return;
            float km = currentSpeedKmh * Time.deltaTime / 3600f;
            if (km <= 0f) return;
            conditionPercent = Mathf.Max(0f, conditionPercent - wearPerKm * WearMultiplier * km);
        }

        /// <summary>Dano por impacto: colisões fortes abatem a condição do veículo.</summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (!AdvancedEconomyActive || conditionPercent <= 0f) return;
            if (rb == null) return;

            float impulse = collision.impulse.magnitude;
            // Normaliza pela massa para que impactos leves (meio-fio) não destruam o veículo.
            float severity = impulse / Mathf.Max(1f, rb.mass * 2.5f);
            if (severity < 0.15f) return; // ignora toques e trepidação de terreno

            float damage = Mathf.Clamp(severity, 0f, 1f) * 14f * WearMultiplier;
            conditionPercent = Mathf.Max(0f, conditionPercent - damage);

            // Repassa o tranco à carga frágil do contrato ativo (se este for o veículo em uso).
            if (isPlayerDriven)
            {
                MissionManager.Instance?.ReportCargoShock(damage);
            }

            if (damage >= 3f && isPlayerDriven && SandboxHUD.Instance != null)
            {
                SandboxHUD.Instance.ShowToast($"[Impacto] Dano no veículo: -{damage:F0}% condição ({conditionPercent:F0}%)", 2.5f);
            }
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

        public void ResetOrientation()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Alinha o veículo com o horizonte, mantendo a direção que estava apontando
                Vector3 flatFwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                if (flatFwd.sqrMagnitude < 0.01f) flatFwd = Vector3.forward;

                transform.rotation = Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
                transform.position += Vector3.up * 1.0f;
            }
        }

        #region Áudio do Veículo

        private void SetupEngineAudio()
        {
            engineAudioSource = GetComponent<AudioSource>();
            if (engineAudioSource == null)
            {
                engineAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (category == VehicleCategory.Tractor)
            {
                engineAudioSource.clip = ProjectTerra.Audio.TerraAudioService.TractorEngineLoop;
            }

            engineAudioSource.loop = true;
            engineAudioSource.spatialBlend = 1.0f; // 3D
            engineAudioSource.minDistance = 2.5f;
            engineAudioSource.maxDistance = 55f;
            engineAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            engineAudioSource.dopplerLevel = 0.4f;
            engineAudioSource.playOnAwake = false;
        }

        private void UpdateEngineAudio()
        {
            if (engineAudioSource == null || engineAudioSource.clip == null) return;

            // Motor afogado ou sem combustível
            if (isEngineFlooded || (fuelPercent <= 0f && !infiniteFuel))
            {
                if (engineAudioSource.isPlaying)
                {
                    engineAudioSource.volume = Mathf.MoveTowards(engineAudioSource.volume, 0f, Time.deltaTime * 3f);
                    if (engineAudioSource.volume <= 0.01f) engineAudioSource.Stop();
                }
                return;
            }

            if (isPlayerDriven)
            {
                if (!engineAudioSource.isPlaying)
                {
                    engineAudioSource.volume = 0.2f;
                    engineAudioSource.pitch = 0.85f;
                    engineAudioSource.Play();
                }

                float throttleNorm = Mathf.Abs(currentThrottle);
                float speedNorm = Mathf.Clamp01(currentSpeedKmh / Mathf.Max(15f, maxSpeedKmh));

                // Modulação realista de pitch e volume
                float targetPitch = Mathf.Lerp(0.85f, 1.4f, speedNorm * 0.6f + throttleNorm * 0.4f);
                float targetVol = Mathf.Lerp(0.4f, 0.9f, throttleNorm * 0.7f + speedNorm * 0.3f);

                engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 6f);
                engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume, targetVol, Time.deltaTime * 7f);
            }
            else
            {
                if (engineAudioSource.isPlaying)
                {
                    engineAudioSource.volume = Mathf.MoveTowards(engineAudioSource.volume, 0f, Time.deltaTime * 1.5f);
                    if (engineAudioSource.volume <= 0.01f) engineAudioSource.Stop();
                }
            }
        }

        #endregion
    }
}
