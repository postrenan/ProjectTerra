using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Gerenciador da frota e posse de veículos do jogador na província regional.
    /// Permite alternar ciclicamente entre os veículos em posse via [Tab],
    /// e instanciar novos veículos via console de comandos [/spawn].
    /// </summary>
    public class VehicleManager : MonoBehaviour
    {
        public static VehicleManager Instance { get; private set; }

        [Header("Veículos em Posse")]
        public List<VehicleController> possessedVehicles = new List<VehicleController>();
        public int currentVehicleIndex = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Registra o veículo inicial caso já tenha sido spawnado
            if (RegionalSandboxManager.Instance != null && RegionalSandboxManager.Instance.starterVehicle != null)
            {
                RegisterVehicle(RegionalSandboxManager.Instance.starterVehicle);
            }
        }

        public void RegisterVehicle(VehicleController vehicle)
        {
            if (vehicle == null) return;

            if (!possessedVehicles.Contains(vehicle))
            {
                possessedVehicles.Add(vehicle);
                Debug.Log($"[VehicleManager] Veículo registrado na posse do jogador: {vehicle.vehicleName} (Total: {possessedVehicles.Count})");
            }
        }

        public void UnregisterVehicle(VehicleController vehicle)
        {
            if (vehicle == null) return;

            if (possessedVehicles.Contains(vehicle))
            {
                possessedVehicles.Remove(vehicle);
            }
        }

        /// <summary>
        /// Alterna entre os veículos em posse do jogador ao pressionar [Tab].
        /// Se estiver a pé, embarca no próximo veículo da frota.
        /// Se estiver dirigindo, transfere o jogador para o próximo veículo.
        /// </summary>
        public void SwitchToNextVehicle()
        {
            // Remove quaisquer referências destruídas
            possessedVehicles.RemoveAll(v => v == null);

            if (possessedVehicles.Count == 0)
            {
                SandboxHUD.Instance?.ShowToast("⚠️ Você não possui veículos na província. Use /spawn para criar um!");
                return;
            }

            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            if (player.isDriving && player.currentVehicle != null)
            {
                if (possessedVehicles.Count == 1 && possessedVehicles[0] == player.currentVehicle)
                {
                    SandboxHUD.Instance?.ShowToast($"ℹ️ Você já está no seu único veículo ({player.currentVehicle.vehicleName}).");
                    return;
                }

                int curIdx = possessedVehicles.IndexOf(player.currentVehicle);
                currentVehicleIndex = (curIdx >= 0) ? (curIdx + 1) % possessedVehicles.Count : 0;

                var nextVehicle = possessedVehicles[currentVehicleIndex];
                player.ExitVehicle();
                player.EnterVehicle(nextVehicle);

                SandboxHUD.Instance?.ShowToast($"🚗 Alternou veículo: {nextVehicle.vehicleName} ({currentVehicleIndex + 1}/{possessedVehicles.Count})", 2.2f);
            }
            else
            {
                // Jogador a pé: entra no veículo atual ou próximo
                currentVehicleIndex = Mathf.Clamp(currentVehicleIndex, 0, possessedVehicles.Count - 1);
                var targetVehicle = possessedVehicles[currentVehicleIndex];

                player.EnterVehicle(targetVehicle);
                SandboxHUD.Instance?.ShowToast($"🚗 Assumiu o controle: {targetVehicle.vehicleName} ({currentVehicleIndex + 1}/{possessedVehicles.Count})", 2.2f);
            }
        }

        /// <summary>
        /// Instancia um novo veículo dinamicamente no mundo em frente ao jogador.
        /// </summary>
        public VehicleController SpawnVehicle(string type, Vector3? customPos = null, Quaternion? customRot = null)
        {
            Vector3 spawnPos;
            Quaternion spawnRot;

            var player = PlayerCharacterController.Instance;
            if (customPos.HasValue)
            {
                spawnPos = customPos.Value;
                spawnRot = customRot ?? Quaternion.identity;
            }
            else if (player != null)
            {
                // Posição 8 metros à frente da direção que o jogador está olhando
                Vector3 forward = (player.isDriving && player.currentVehicle != null) ?
                    player.currentVehicle.transform.forward : player.transform.forward;
                Vector3 basePos = (player.isDriving && player.currentVehicle != null) ?
                    player.currentVehicle.transform.position : player.transform.position;

                spawnPos = basePos + forward * 8.5f;

                if (RegionalSandboxManager.Instance != null)
                {
                    spawnPos.y = RegionalSandboxManager.Instance.GetTerrainHeight(spawnPos) + 0.85f;
                }

                spawnRot = Quaternion.LookRotation(forward, Vector3.up);
            }
            else
            {
                spawnPos = new Vector3(0f, 2f, 0f);
                spawnRot = Quaternion.identity;
            }

            VehicleController vehicle = null;
            string key = (type ?? "").Trim().ToLower();

            switch (key)
            {
                case "tractor":
                case "trator":
                    vehicle = VehicleBuilder.CreateTractor(spawnPos);
                    break;
                case "truck":
                case "caminhao":
                case "caminhão":
                    vehicle = VehicleBuilder.CreateTruck(spawnPos);
                    break;
                case "plane":
                case "aviao":
                case "avião":
                    vehicle = VehicleBuilder.CreatePlane(spawnPos);
                    break;
                case "boat":
                case "barco":
                case "navio":
                    vehicle = VehicleBuilder.CreateBoat(spawnPos);
                    break;
                case "car":
                case "carro":
                case "sedan":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "sedan", "Sedan Urbano Executivo");
                    break;
                case "sports":
                case "sport":
                case "esportivo":
                case "race":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "race", "Carro Esportivo GT");
                    break;
                case "suv":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "suv", "SUV Todo-Terreno 4x4");
                    break;
                case "police":
                case "policia":
                case "polícia":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "police", "Viatura Policial Regional");
                    break;
                case "ambulance":
                case "ambulancia":
                case "ambulância":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "ambulance", "Ambulância de Resgate");
                    break;
                case "firetruck":
                case "bombeiro":
                case "bombeiros":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "firetruck", "Caminhão de Bombeiros");
                    break;
                case "taxi":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "taxi", "Táxi Urbano");
                    break;
                case "van":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "van", "Van Utilitária de Cargas");
                    break;
                case "speeder":
                    vehicle = VehicleBuilder.CreateCar(spawnPos, "craft_speederA", "Aero Speeder Futurista", VehicleCategory.Plane);
                    break;
                default:
                    // Tenta criar como carro com modelo específico de Resources
                    vehicle = VehicleBuilder.CreateCar(spawnPos, key, $"Veículo {key}");
                    break;
            }

            if (vehicle != null)
            {
                vehicle.transform.rotation = spawnRot;
                RegisterVehicle(vehicle);
                currentVehicleIndex = possessedVehicles.IndexOf(vehicle);
            }

            return vehicle;
        }

        /// <summary>
        /// Limpa veículos adicionais criados por comandos, preservando o veículo inicial ou o que está sendo conduzido.
        /// </summary>
        public int ClearSpawnedVehicles()
        {
            var starter = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.starterVehicle : null;
            var player = PlayerCharacterController.Instance;
            int removed = 0;

            for (int i = possessedVehicles.Count - 1; i >= 0; i--)
            {
                var v = possessedVehicles[i];
                if (v != null && v != starter && (player == null || player.currentVehicle != v))
                {
                    Destroy(v.gameObject);
                    possessedVehicles.RemoveAt(i);
                    removed++;
                }
            }

            currentVehicleIndex = 0;
            return removed;
        }
    }
}
