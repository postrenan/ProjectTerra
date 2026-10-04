using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Iluminação e atmosfera para o Built-in Render Pipeline:
    /// sol direcional calibrado, céu procedural, nuvens procedurais, ambiente Trilight e neblina linear.
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

        [Header("Nuvens Procedurais")]
        public bool enableClouds = true;
        [Range(0f, 1f)] public float cloudCoverage = 0.5f;      // Quantidade de nuvens
        [Range(0f, 1f)] public float cloudDensity = 0.5f;       // Densidade/opacidade
        [Range(1000f, 50000f)] public float cloudAltitude = 6000f; // Altitude da camada de nuvens (metros)
        [Range(1000f, 100000f)] public float cloudLayerRadius = 50000f; // Raio horizontal da camada
        public float cloudWindSpeed = 2.0f; // Velocidade do vento (m/s)
        public Vector2 cloudWindDirection = new Vector2(1f, 0.3f); // Direção do vento (X, Z)
        public Color cloudColorDay = new Color(0.95f, 0.95f, 0.98f); // Cor das nuvens de dia
        public Color cloudColorNight = new Color(0.35f, 0.38f, 0.45f); // Cor das nuvens de noite
        public Color cloudColorSunset = new Color(1.0f, 0.65f, 0.35f); // Cor no pôr/nascer do sol

        private GameObject cloudDome;
        private Material cloudMaterial;
        private Vector2 cloudOffset = Vector2.zero;

        private void Awake()
        {
            Instance = this;
            ConfigureAtmosphere();
            ConfigureSunLight();
            ConfigureClouds();
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

            UpdateClouds();
        }

        private void ConfigureClouds()
        {
            if (!enableClouds) return;

            // Carregar shader de nuvens (arquivo .shader)
            var cloudShader = Shader.Find("ProjectTerra/Clouds/Procedural");
            if (cloudShader == null)
            {
                Debug.LogWarning("[Atmosphere] Shader 'ProjectTerra/Clouds/Procedural' não encontrado. Verifique se o arquivo Clouds.shader está em Assets/_Project/Shaders/.");
                return;
            }

            cloudMaterial = new Material(cloudShader);
            cloudMaterial.name = "CloudMaterial_Procedural";
            cloudMaterial.renderQueue = 2450; // Antes do skybox (2500), depois de geometria opaca

            // Gerar textura de ruído para as nuvens
            Texture2D noiseTex = GenerateCloudNoiseTexture(256, 256);
            if (noiseTex != null)
            {
                cloudMaterial.SetTexture("_MainTex", noiseTex);
            }

            // Criar dome de nuvens (hemisfério grande)
            cloudDome = new GameObject("CloudDome");
            cloudDome.transform.SetParent(transform);
            cloudDome.transform.localPosition = Vector3.zero;

            var mf = cloudDome.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateCloudDomeMesh();

            var mr = cloudDome.AddComponent<MeshRenderer>();
            mr.sharedMaterial = cloudMaterial;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

            // Configurar propriedades iniciais do material
            UpdateCloudMaterialProperties();

            Debug.Log("[Atmosphere] Sistema de nuvens procedurais ativado.");
        }

        private Texture2D GenerateCloudNoiseTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "CloudNoiseTexture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1
            };

            var colors = new Color[width * height];

            // Gerar ruído Perlin simples via código
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)width;
                    float ny = y / (float)height;

                    // FBM simples
                    float value = 0f;
                    float amp = 1f;
                    float freq = 1f;
                    for (int oct = 0; oct < 4; oct++)
                    {
                        float sampleX = nx * freq;
                        float sampleY = ny * freq;
                        value += Mathf.PerlinNoise(sampleX * 8f, sampleY * 8f) * amp;
                        amp *= 0.5f;
                        freq *= 2f;
                    }
                    value = Mathf.Clamp01(value);

                    colors[y * width + x] = new Color(value, value, value, value);
                }
            }

            tex.SetPixels(colors);
            tex.Apply(true, false);
            return tex;
        }

        private Mesh CreateCloudDomeMesh()
        {
            // Criar um hemisfério grande para as nuvens
            int segments = 32;
            int rings = 16;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var normals = new List<Vector3>();

            vertices.Add(Vector3.up * cloudLayerRadius); // Topo do dome
            uvs.Add(new Vector2(0.5f, 0.5f));
            normals.Add(Vector3.down); // Normal apontando para baixo (vemos de dentro)

            for (int ring = 1; ring <= rings; ring++)
            {
                float phi = Mathf.PI * 0.5f * (ring / (float)rings); // 0 a 90 graus
                float y = Mathf.Cos(phi) * cloudLayerRadius;
                float r = Mathf.Sin(phi) * cloudLayerRadius;

                for (int seg = 0; seg <= segments; seg++)
                {
                    float theta = 2f * Mathf.PI * (seg / (float)segments);
                    float x = r * Mathf.Cos(theta);
                    float z = r * Mathf.Sin(theta);

                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(x / cloudLayerRadius * 0.5f + 0.5f, z / cloudLayerRadius * 0.5f + 0.5f));
                    normals.Add(-new Vector3(x, y, z).normalized); // Normal para dentro
                }
            }

            // Triângulos: topo para primeiro anel
            for (int seg = 0; seg < segments; seg++)
            {
                triangles.Add(0);
                triangles.Add(seg + 2);
                triangles.Add(seg + 1);
            }

            // Anéis intermediários
            for (int ring = 1; ring < rings; ring++)
            {
                int ringStart = 1 + (ring - 1) * (segments + 1);
                int nextRingStart = 1 + ring * (segments + 1);

                for (int seg = 0; seg < segments; seg++)
                {
                    int a = ringStart + seg;
                    int b = ringStart + seg + 1;
                    int c = nextRingStart + seg;
                    int d = nextRingStart + seg + 1;

                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
                }
            }

            var mesh = new Mesh { name = "CloudDomeMesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void UpdateClouds()
        {
            if (!enableClouds || cloudDome == null || cloudMaterial == null) return;

            // Atualizar posição do dome para seguir a câmera (apenas XZ, Y fixo na altitude)
            var cam = Camera.main;
            if (cam != null)
            {
                cloudDome.transform.position = new Vector3(cam.transform.position.x, cloudAltitude, cam.transform.position.z);
            }

            // Atualizar offset do vento para animação
            cloudOffset += cloudWindDirection.normalized * (cloudWindSpeed * Time.deltaTime * 0.0001f);
            cloudMaterial.SetVector("_CloudOffset", cloudOffset);

            // Atualizar propriedades que mudam com o tempo
            UpdateCloudMaterialProperties();
        }

        private void UpdateCloudMaterialProperties()
        {
            if (cloudMaterial == null) return;

            // Direção do sol para silver lining
            if (sunLight != null)
            {
                cloudMaterial.SetVector("_SunDirection", sunLight.transform.forward);
            }

            // Hora do dia para cores
            cloudMaterial.SetFloat("_TimeOfDay", timeOfDay);
            cloudMaterial.SetFloat("_Coverage", cloudCoverage);
            cloudMaterial.SetFloat("_Density", cloudDensity);
            cloudMaterial.SetColor("_ColorDay", cloudColorDay);
            cloudMaterial.SetColor("_ColorNight", cloudColorNight);
            cloudMaterial.SetColor("_ColorSunset", cloudColorSunset);
            cloudMaterial.SetFloat("_CloudScale", 1f / cloudLayerRadius);
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

            Debug.Log("[Atmosphere] Built-in: céu procedural, iluminação solar, nuvens e atmosfera configuradas.");
        }
    }
}