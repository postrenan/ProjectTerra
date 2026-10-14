using System;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Relógio da campanha: acompanha o horário solar do <see cref="HDRPAtmosphereController"/>,
    /// conta os dias de jogo e, à meia-noite, cobra as despesas operacionais diárias
    /// (seguro + manutenção da frota) na partida ativa. É a base de progressão temporal
    /// usada pela Fase 1 (custos) e pelas fases seguintes (prazos/eventos), via o evento
    /// <see cref="OnNewDay"/>.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        public static GameClock Instance { get; private set; }

        /// <summary>Disparado quando o dia de jogo vira (parâmetro: novo número do dia).</summary>
        public static event Action<int> OnNewDay;

        private float previousTime = -1f;
        private bool initialized = false;

        private const int LoanLateFeePercent = 5; // multa de 5% da parcela por dia de atraso

        private void Awake()
        {
            Instance = this;
        }

        private RegionSaveData Save => SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : null;

        public int CurrentDay => Save != null ? (int)Save.gameDay : 1;

        /// <summary>Tempo de jogo acumulado em horas (dia × 24 + horário solar). Base dos prazos de frete.</summary>
        public static float TotalGameHoursNow
        {
            get
            {
                int day = Instance != null ? Instance.CurrentDay : 1;
                return (day - 1) * 24f + SandboxHUD.InGameTimeOfDay;
            }
        }

        private void Update()
        {
            if (HDRPAtmosphereController.Instance == null) return;

            float now = HDRPAtmosphereController.Instance.timeOfDay;

            if (!initialized)
            {
                previousTime = now;
                initialized = true;
                return;
            }

            // Virada de meia-noite: o horário salta de madrugada alta (~23h) para baixa (~0-4h).
            // A janela evita contar como "novo dia" os ajustes manuais de horário (comando/HUD)
            // que não cruzam a meia-noite.
            bool crossedMidnight = previousTime > 20f && now < 4f;
            previousTime = now;

            if (crossedMidnight)
            {
                AdvanceDay();
            }
        }

        private void AdvanceDay()
        {
            var save = Save;
            if (save == null) return;

            // Único avançador de dia do jogo: incrementa gameDay e cobra multa por
            // parcelas de empréstimo vencidas (sistema de bancos do main).
            long loanPenalty = save.AdvanceDayAndChargeLate(LoanLateFeePercent);
            if (loanPenalty > 0)
            {
                SandboxHUD.Instance?.ShowToast($"[Banco] Multa por atraso: ${loanPenalty:N0}", 4f);
            }

            if (save.advancedEconomy)
            {
                ApplyDailyUpkeep(save);
                ApplyDailyEvents(save);
            }

            OnNewDay?.Invoke((int)save.gameDay);

            // Renova o mercado de fretes a cada dia, dando sensação de mundo vivo.
            SandboxHUD.Instance?.RefreshJobMarket();

            if (SaveManager.Instance != null)
            {
                save.lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                SaveManager.Instance.SaveToFile(save);
            }
        }

        private void ApplyDailyUpkeep(RegionSaveData save)
        {
            var cfg = save.DifficultyConfig;

            int fleet = 0;
            if (VehicleManager.Instance != null && VehicleManager.Instance.possessedVehicles != null)
            {
                foreach (var v in VehicleManager.Instance.possessedVehicles)
                    if (v != null) fleet++;
            }
            if (fleet < 1) fleet = 1; // sempre há ao menos o veículo inicial

            long upkeep = cfg.upkeepBasePerDay + cfg.upkeepPerVehiclePerDay * fleet;
            if (upkeep <= 0) return;

            save.currentMoney -= upkeep;
            save.totalExpenses += upkeep;

            string msg = $"Dia {save.gameDay} — Despesas: -${upkeep:N0} (seguro + frota de {fleet})";
            if (save.currentMoney < 0)
            {
                msg += "\n[Alerta] Saldo negativo! Aceite fretes ou quite despesas.";
            }
            SandboxHUD.Instance?.ShowToast(msg, 5f);
        }

        /// <summary>
        /// Eventos dinâmicos de mundo vivo a cada dia: flutuação do preço do combustível,
        /// mudança de clima e chance de avaria na frota.
        /// </summary>
        private void ApplyDailyEvents(RegionSaveData save)
        {
            // 1. Flutuação do preço do combustível em torno da base da dificuldade.
            float basePrice = save.DifficultyConfig.fuelPricePerLiter;
            float cur = save.fuelPricePerLiter > 0f ? save.fuelPricePerLiter : basePrice;
            float delta = UnityEngine.Random.Range(-0.08f, 0.10f);
            cur = Mathf.Clamp(cur * (1f + delta), basePrice * 0.8f, basePrice * 1.3f);
            save.fuelPricePerLiter = Mathf.Round(cur * 100f) / 100f;

            // 2. Clima do dia (cosmético + atmosfera). 55% mantém o tempo bom.
            if (HDRPAtmosphereController.Instance != null && UnityEngine.Random.value > 0.55f)
            {
                string[] weathers = { "overcast", "fog", "storm", "clear" };
                HDRPAtmosphereController.Instance.SetWeather(weathers[UnityEngine.Random.Range(0, weathers.Length)]);
            }

            // 3. Avaria aleatória: chance (maior na dificuldade) de um veículo perder condição.
            float breakdownChance = 0.08f + save.DifficultyConfig.wearMultiplier * 0.05f;
            if (UnityEngine.Random.value < breakdownChance && VehicleManager.Instance != null && VehicleManager.Instance.possessedVehicles != null)
            {
                var fleet = VehicleManager.Instance.possessedVehicles;
                var candidates = new System.Collections.Generic.List<VehicleController>();
                foreach (var v in fleet) if (v != null && v.conditionPercent > 20f) candidates.Add(v);
                if (candidates.Count > 0)
                {
                    var victim = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    float loss = UnityEngine.Random.Range(10f, 25f);
                    victim.conditionPercent = Mathf.Max(0f, victim.conditionPercent - loss);
                    SandboxHUD.Instance?.ShowToast($"[Avaria] {victim.vehicleName} apresentou defeito: -{loss:F0}% condição. Repare na Garagem.", 5f);
                }
            }
        }
    }
}
