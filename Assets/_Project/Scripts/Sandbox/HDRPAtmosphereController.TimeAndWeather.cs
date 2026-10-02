using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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

            // Rotação do Sol:
            // 06:00 = Nascer do sol (0° de elevação)
            // 12:00 = Zênite / Meio-dia (75° de elevação)
            // 18:00 = Pôr do sol (180° de elevação)
            // 00:00 = Meia-noite (270° / abaixo do horizonte)
            float sunPitch = ((timeOfDay - 6f) / 24f) * 360f;
            sunLight.transform.rotation = Quaternion.Euler(sunPitch, 50f, 0f);

            // Altura angular do sol acima do horizonte
            float elevationFactor = Mathf.Sin((timeOfDay / 24f) * Mathf.PI * 2f - Mathf.PI * 0.5f);
            bool isDay = elevationFactor > 0.04f;
            bool isGoldenHour = Mathf.Abs(elevationFactor) <= 0.28f;

            if (IsHDRPActive())
            {
                var hdLight = sunLight.GetComponent<HDAdditionalLightData>();
                if (isDay)
                {
                    float factor = Mathf.Clamp01(elevationFactor * 1.6f);
                    sunLight.intensity = Mathf.Lerp(12000f, sunIntensityLux, factor);
                    float kelvin = isGoldenHour ? 3200f : sunColorKelvin;
                    if (hdLight != null)
                    {
                        hdLight.SetColor(Color.white, kelvin);
                        hdLight.volumetricDimmer = 1.0f;
                    }
                }
                else
                {
                    // Noite: Luar sereno
                    sunLight.intensity = 200f;
                    if (hdLight != null)
                    {
                        hdLight.SetColor(new Color(0.35f, 0.5f, 0.85f), 7500f);
                        hdLight.volumetricDimmer = 0.3f;
                    }
                }
            }
            else
            {
                if (isDay)
                {
                    float factor = Mathf.Clamp01(elevationFactor * 1.5f);
                    sunLight.intensity = Mathf.Lerp(0.35f, 1.35f, factor);
                    sunLight.color = isGoldenHour ? new Color(1.0f, 0.72f, 0.42f) : new Color(1.0f, 0.96f, 0.90f);
                    RenderSettings.ambientIntensity = Mathf.Lerp(0.4f, 1.15f, factor);
                }
                else
                {
                    // Noite Built-in
                    sunLight.intensity = 0.12f;
                    sunLight.color = new Color(0.32f, 0.45f, 0.78f);
                    RenderSettings.ambientIntensity = 0.22f;
                }
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
                    SetFogDistance(35000f, 0.25f);
                    SandboxHUD.Instance?.ShowToast("☀️ Clima alterado para: Céu Limpo", 2.0f);
                    break;
                case "fog":
                case "neblina":
                case "nevoeiro":
                    SetFogDistance(900f, 0.65f);
                    SandboxHUD.Instance?.ShowToast("🌫️ Clima alterado para: Neblina Moderada", 2.0f);
                    break;
                case "densefog":
                case "denso":
                    SetFogDistance(320f, 0.95f);
                    SandboxHUD.Instance?.ShowToast("🌫️ Clima alterado para: Neblina Densa", 2.0f);
                    break;
                case "overcast":
                case "nublado":
                    SetFogDistance(4500f, 0.50f);
                    SandboxHUD.Instance?.ShowToast("☁️ Clima alterado para: Nublado", 2.0f);
                    break;
                case "storm":
                case "tempestade":
                case "chuva":
                    SetFogDistance(650f, 0.85f);
                    SandboxHUD.Instance?.ShowToast("⛈️ Clima alterado para: Tempestade", 2.0f);
                    break;
                default:
                    SandboxHUD.Instance?.ShowToast($"⚠️ Clima desconhecido '{key}'. Opções: clear, fog, densefog, overcast, storm", 3.0f);
                    break;
            }
        }

        private void SetFogDistance(float distance, float cloudDensity)
        {
            fogDistance = distance;

            if (IsHDRPActive() && profile != null)
            {
                if (profile.TryGet<Fog>(out var fog))
                {
                    fog.meanFreePath.Override(distance);
                }
                if (profile.TryGet<VolumetricClouds>(out var clouds))
                {
                    clouds.densityMultiplier.Override(cloudDensity);
                }
            }
            else
            {
                RenderSettings.fog = true;
                RenderSettings.fogEndDistance = distance;
            }
        }

        #endregion
    }
}
