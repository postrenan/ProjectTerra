using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Iluminação e atmosfera para o Built-in Render Pipeline:
    /// sol direcional calibrado, céu procedural, ambiente Trilight e neblina linear.
    ///
    /// NOTA: o pacote HDRP foi removido do projeto (ele forçava os materiais padrão — inclusive o do
    /// Terrain — a serem HDRP, que não renderizam no Built-in, causando terreno rosa/branco). O nome
    /// da classe foi mantido para não quebrar referências existentes (AddComponent, Instance, etc.).
    /// </summary>
    public partial class HDRPAtmosphereController : MonoBehaviour
    {
        public static HDRPAtmosphereController Instance { get; private set; }

        [Header("Controle Solar")]
        public Light sunLight;
        public float sunColorKelvin = 5800f;   // Luz solar diurna balanceada

        [Header("Ciclo Horário Dia/Noite")]
        [Range(0f, 24f)] public float timeOfDay = 12.0f; // Meio-dia por padrão
        public bool autoCycleTime = false;
        public float dayCycleDurationMinutes = 20.0f; // 20 min reais para 24h in-game

        [Header("Neblina")]
        public float fogDistance = 45000f;

        private void Awake()
        {
            Instance = this;
            ConfigureAtmosphere();
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
                sunLight.useColorTemperature = true;
                sunLight.colorTemperature = sunColorKelvin;
                sunLight.shadows = LightShadows.Soft;

                // Built-in: intensidade fotométrica balanceada (valores altos estouram a tela em branco).
                sunLight.intensity = 1.0f;
                sunLight.color = new Color(1.0f, 0.96f, 0.90f);
                sunLight.shadowStrength = 0.85f;
            }
        }

        private void ConfigureAtmosphere()
        {
            // Céu procedural (cor visível e reflexo de ambiente coerente).
            if (RenderSettings.skybox == null)
            {
                var skyShader = Shader.Find("Skybox/Procedural");
                if (skyShader != null)
                {
                    var skyMat = new Material(skyShader);
                    skyMat.name = "Skybox_Procedural_Builtin";
                    if (skyMat.HasProperty("_SunSize")) skyMat.SetFloat("_SunSize", 0.04f);
                    if (skyMat.HasProperty("_AtmosphereThickness")) skyMat.SetFloat("_AtmosphereThickness", 1.0f);
                    if (skyMat.HasProperty("_SkyTint")) skyMat.SetColor("_SkyTint", new Color(0.48f, 0.62f, 0.88f));
                    if (skyMat.HasProperty("_GroundColor")) skyMat.SetColor("_GroundColor", new Color(0.32f, 0.30f, 0.28f));
                    RenderSettings.skybox = skyMat;
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.62f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.46f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.26f, 0.22f);
            RenderSettings.ambientIntensity = 1.0f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 3000f;
            RenderSettings.fogEndDistance = Mathf.Max(8000f, fogDistance);
            RenderSettings.fogColor = new Color(0.60f, 0.72f, 0.85f);

            Debug.Log("[Atmosphere] Built-in: céu procedural, iluminação solar e atmosfera configuradas.");
        }
    }
}
