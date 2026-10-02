using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Configura dinamicamente a iluminação atmosférica e volume pós-processamento AAA no Unity HDRP:
    /// Céu Físico (Physical Sky), Nuvens Volumétricas dinâmicas, Neblina Volumétrica,
    /// Oclusão de Ambiente de Alta Fidelidade (GTAO), Reflexões em Tempo Real (SSR)
    /// e Calibração Solar em Lux e Temperatura Kelvin.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public partial class HDRPAtmosphereController : MonoBehaviour
    {
        public static HDRPAtmosphereController Instance { get; private set; }

        [Header("Controle Solar")]
        public Light sunLight;
        public float sunIntensityLux = 110000f; // Iluminação solar direta realista ao meio-dia
        public float sunColorKelvin = 5800f;   // Luz solar diurna balanceada

        [Header("Nuvens Volumétricas")]
        public bool enableVolumetricClouds = true;
        public float cloudAltitude = 1200f;
        public float cloudThickness = 800f;

        [Header("Neblina Volumétrica")]
        public bool enableVolumetricFog = true;
        public float fogDistance = 1800f;

        [Header("Ciclo Horário Dia/Noite")]
        [Range(0f, 24f)] public float timeOfDay = 12.0f; // Meio-dia por padrão
        public bool autoCycleTime = false;
        public float dayCycleDurationMinutes = 20.0f; // 20 minutos de tempo real para 24h in-game

        private Volume volume;
        private VolumeProfile profile;

        private void Awake()
        {
            Instance = this;
            volume = GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;

            ConfigureHDRPProfile();
            ConfigureSunLight();
            UpdateTimeOfDay(timeOfDay);
        }

        private void Update()
        {
            if (autoCycleTime && Application.isPlaying)
            {
                float hoursPerSecond = 24.0f / (dayCycleDurationMinutes * 60.0f);
                timeOfDay = Mathf.Repeat(timeOfDay + hoursPerSecond * Time.deltaTime, 24.0f);
                UpdateTimeOfDay(timeOfDay);
            }
        }

        private bool IsHDRPActive()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            return rp != null && rp.GetType().Name.Contains("HD");
        }

        private void ConfigureSunLight()
        {
            if (sunLight == null)
            {
                var lightObj = GameObject.Find("Directional Light (Sun)") ?? GameObject.Find("Sun_DirectionalLight") ?? GameObject.Find("Directional Light");
                if (lightObj != null)
                {
                    sunLight = lightObj.GetComponent<Light>();
                }
            }

            if (sunLight != null)
            {
                sunLight.type = LightType.Directional;
                sunLight.colorTemperature = sunColorKelvin;
                sunLight.useColorTemperature = true;
                sunLight.shadows = LightShadows.Soft;

                if (IsHDRPActive())
                {
                    sunLight.intensity = sunIntensityLux;

                    var hdLight = sunLight.GetComponent<HDAdditionalLightData>();
                    if (hdLight == null)
                    {
                        hdLight = sunLight.gameObject.AddComponent<HDAdditionalLightData>();
                    }

                    if (hdLight != null)
                    {
                        hdLight.EnableColorTemperature(true);
                        hdLight.SetColor(Color.white, sunColorKelvin);
                        hdLight.volumetricDimmer = 1.0f;
                    }
                }
                else
                {
                    // No Built-in Render Pipeline, intensidades de 100k lux causam tela 100% branca estourada.
                    // Usamos intensidade padrão fotométrica balanceada para iluminação solar direta.
                    sunLight.intensity = 1.35f;
                    sunLight.color = new Color(1.0f, 0.96f, 0.90f);
                    sunLight.shadowStrength = 0.85f;
                }
            }
        }

        private void ConfigureHDRPProfile()
        {
            if (!IsHDRPActive())
            {
                // Configuração balanceada AAA para Built-in Render Pipeline
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.68f, 0.78f, 0.90f);
                RenderSettings.ambientEquatorColor = new Color(0.50f, 0.54f, 0.58f);
                RenderSettings.ambientGroundColor = new Color(0.28f, 0.30f, 0.25f);
                RenderSettings.ambientIntensity = 1.15f;

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 5000f;
                RenderSettings.fogEndDistance = 60000f;
                RenderSettings.fogColor = new Color(0.68f, 0.80f, 0.92f);

                Debug.Log("[HDRPAtmosphere] Pipeline Built-in detectado: Iluminação solar e atmosfera balanceadas configuradas.");
                return;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            // 1. Physically Based Sky
            if (!profile.TryGet<PhysicallyBasedSky>(out var sky))
            {
                sky = profile.Add<PhysicallyBasedSky>(true);
            }
            sky.active = true;
            sky.type.Override(PhysicallyBasedSkyModel.EarthAdvanced);
            sky.airDensityR.Override(0.044f);
            sky.airDensityG.Override(0.095f);
            sky.airDensityB.Override(0.24f);
            sky.aerosolDensity.Override(0.055f);

            // Visual Environment
            if (!profile.TryGet<VisualEnvironment>(out var visualEnv))
            {
                visualEnv = profile.Add<VisualEnvironment>(true);
            }
            visualEnv.active = true;
            visualEnv.skyType.Override((int)SkyType.PhysicallyBased);

            // 2. Volumetric Clouds
            if (enableVolumetricClouds)
            {
                if (!profile.TryGet<VolumetricClouds>(out var clouds))
                {
                    clouds = profile.Add<VolumetricClouds>(true);
                }
                clouds.active = true;
                clouds.enable.Override(true);
                clouds.bottomAltitude.Override(cloudAltitude);
                clouds.altitudeRange.Override(cloudThickness);
                clouds.densityMultiplier.Override(0.42f);
                clouds.shapeFactor.Override(0.75f);
                clouds.erosionFactor.Override(0.65f);
            }

            // 3. Fog (Neblina Atmosférica Volumétrica)
            if (enableVolumetricFog)
            {
                if (!profile.TryGet<Fog>(out var fog))
                {
                    fog = profile.Add<Fog>(true);
                }
                fog.active = true;
                fog.enabled.Override(true);
                fog.enableVolumetricFog.Override(true);
                fog.meanFreePath.Override(fogDistance);
                fog.baseHeight.Override(0f);
                fog.maximumHeight.Override(600f);
                fog.anisotropy.Override(0.55f);
            }

            // 4. Ambient Occlusion (GTAO - Ground Truth Ambient Occlusion)
            if (!profile.TryGet<ScreenSpaceAmbientOcclusion>(out var ao))
            {
                ao = profile.Add<ScreenSpaceAmbientOcclusion>(true);
            }
            ao.active = true;
            ao.intensity.Override(0.95f);
            ao.directLightingStrength.Override(0.45f);
            ao.radius.Override(2.5f);

            // 5. Screen Space Reflection (SSR)
            if (!profile.TryGet<ScreenSpaceReflection>(out var ssr))
            {
                ssr = profile.Add<ScreenSpaceReflection>(true);
            }
            ssr.active = true;
            ssr.enabled.Override(true);
            ssr.minSmoothness = 0.55f;

            // 6. Exposure (Exposição Física Automática)
            if (!profile.TryGet<Exposure>(out var exposure))
            {
                exposure = profile.Add<Exposure>(true);
            }
            exposure.active = true;
            exposure.mode.Override(ExposureMode.Automatic);
            exposure.limitMin.Override(8f);
            exposure.limitMax.Override(17f);
            exposure.adaptationSpeedLightToDark.Override(3.0f);
            exposure.adaptationSpeedDarkToLight.Override(3.0f);

            // 7. Bloom Suave
            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.active = true;
            bloom.intensity.Override(0.18f);
            bloom.scatter.Override(0.7f);

            Debug.Log("[HDRPAtmosphere] Perfil atmosférico fotorrealista HDRP configurado com sucesso.");
        }
    }
}
