using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectTerra.Sandbox
{
    public partial class HDRPAtmosphereController
    {
        #region Controle de Horário e Ciclo Solar

        public void SetTimeOfDay(float hour)
        {
            timeOfDay = Mathf.Repeat(hour, 24.0f);
            UpdateTimeOfDay(timeOfDay);
            int h = Mathf.FloorToInt(timeOfDay);
            int m = Mathf.FloorToInt((timeOfDay - h) * 60f);
            SandboxHUD.Instance?.ShowToast($"🕒 Horário alterado para {h:D2}:{m:D2}", 2.0f);
        }

        public void UpdateTimeOfDay(float hour)
        {
            timeOfDay = Mathf.Repeat(hour, 24.0f);

            if (sunLight == null)
            {
                var lightObj = GameObject.Find("Directional Light (Sun)") ?? GameObject.Find("Sun_DirectionalLight") ?? GameObject.Find("Directional Light");
                if (lightObj != null) sunLight = lightObj.GetComponent<Light>();
            }

            if (sunLight == null) return;

            // Rotação do Sol: 06:00 nascer, 12:00 zênite, 18:00 pôr, 00:00 meia-noite.
            float sunPitch = ((timeOfDay - 6f) / 24f) * 360f;
            sunLight.transform.rotation = Quaternion.Euler(sunPitch, 50f, 0f);

            float elevationFactor = Mathf.Sin((timeOfDay / 24f) * Mathf.PI * 2f - Mathf.PI * 0.5f);
            bool isDay = elevationFactor > 0.04f;
            bool isGoldenHour = Mathf.Abs(elevationFactor) <= 0.28f;

            if (isDay)
            {
                float factor = Mathf.Clamp01(elevationFactor * 1.5f);
                sunLight.intensity = Mathf.Lerp(0.35f, 1.0f, factor);
                sunLight.color = isGoldenHour ? new Color(1.0f, 0.72f, 0.42f) : new Color(1.0f, 0.96f, 0.90f);
                
                // URP: ambient intensity controlada via RenderSettings
                RenderSettings.ambientIntensity = Mathf.Lerp(0.55f, 1.0f, factor);
            }
            else
            {
                // Noite
                sunLight.intensity = 0.08f;
                sunLight.color = new Color(0.32f, 0.45f, 0.78f);
                RenderSettings.ambientIntensity = 0.22f;
            }
        }

        #endregion

        #region Clima e Atmosfera

        public void SetWeather(string weatherType)
        {
            string key = (weatherType ?? "").Trim().ToLower();
            switch (key)
            {
                case "clear":
                case "limpo":
                case "ensolarado":
                    SetFogDistance(45000f);
                    SandboxHUD.Instance?.ShowToast("☀️ Clima alterado para: Céu Limpo", 2.0f);
                    break;
                case "fog":
                case "neblina":
                case "nevoeiro":
                    SetFogDistance(1200f);
                    SandboxHUD.Instance?.ShowToast("🌫️ Clima alterado para: Neblina Moderada", 2.0f);
                    break;
                case "densefog":
                case "denso":
                    SetFogDistance(450f);
                    SandboxHUD.Instance?.ShowToast("🌫️ Clima alterado para: Neblina Densa", 2.0f);
                    break;
                case "overcast":
                case "nublado":
                    SetFogDistance(8000f);
                    SandboxHUD.Instance?.ShowToast("☁️ Clima alterado para: Nublado", 2.0f);
                    break;
                case "storm":
                case "tempestade":
                case "chuva":
                    SetFogDistance(900f);
                    SandboxHUD.Instance?.ShowToast("⛈️ Clima alterado para: Tempestade", 2.0f);
                    break;
                default:
                    SandboxHUD.Instance?.ShowToast($"⚠️ Clima desconhecido '{key}'. Opções: clear, fog, densefog, overcast, storm", 3.0f);
                    break;
            }
        }

        private void SetFogDistance(float distance)
        {
            fogDistance = distance;
            
            // Atualizar RenderSettings (compatibilidade básica)
            RenderSettings.fog = true;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogStartDistance = Mathf.Min(distance * 0.1f, 3000f);
            RenderSettings.fogEndDistance = distance;
            RenderSettings.fogColor = fogColor;

            // Atualizar Volume URP se disponível
            UpdateAtmosphereVolumes();
        }

        #endregion
    }
}