using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public enum WaterBodyType
    {
        None,
        Ocean,
        Lake,
        River
    }

    public struct WaterInfo
    {
        public bool isInWater;
        public float surfaceY;       // Altura da superfície da água no ponto (com ondas)
        public float baseWaterY;     // Altura média estática da água
        public float depth;          // Profundidade do leito até a superfície
        public float submersion;     // Profundidade do ponto consultado abaixo da superfície (surfaceY - queryY)
        public Vector3 flowVelocity; // Velocidade vetorial da correnteza (m/s)
        public WaterBodyType bodyType;
        public string bodyName;
    }

    /// <summary>
    /// Sistema centralizado de hidrografia, dinâmica de ondas, correntezas e efeitos aquáticos.
    /// Fornece consultas em tempo real para física de barcos, natação do jogador, veículos e efeitos de câmera submersa.
    /// </summary>
    public class WaterSystem : MonoBehaviour
    {
        public static WaterSystem Instance { get; private set; }

        public class RiverSegment
        {
            public Vector3 start;
            public Vector3 end;
            public float width;
            public Vector3 flowDir;
            public float flowSpeed;
            public string name;
            public Bounds bounds;
        }

        public class LakeArea
        {
            public Vector3 center;
            public float radiusX;
            public float radiusZ;
            public float surfaceY;
            public string name;
        }

        public class OceanArea
        {
            public float seaLevelY;
            public Bounds bounds;
            public string name;
        }

        private readonly List<RiverSegment> rivers = new List<RiverSegment>();
        private readonly List<LakeArea> lakes = new List<LakeArea>();
        private OceanArea ocean = null;

        [Header("Configuração de Ondas")]
        public float globalWaveHeight = 0.35f;
        public float globalWaveSpeed = 1.3f;

        [Header("Efeitos Subaquáticos")]
        private bool isCameraUnderwater = false;
        private bool prevFogEnabled;
        private Color prevFogColor;
        private float prevFogDensity;
        private FogMode prevFogMode;
        private float prevFogStart;
        private float prevFogEnd;
        private bool savedOriginalFog = false;

        private Camera cachedCamera;

        public static WaterSystem EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindAnyObjectByType<WaterSystem>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var go = new GameObject("WaterSystem");
            Instance = go.AddComponent<WaterSystem>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void ClearAll()
        {
            rivers.Clear();
            lakes.Clear();
            ocean = null;
        }

        #region Registro de Corpos D'Água

        public void RegisterOcean(float seaLevelY, Bounds bounds, string name = "Oceano")
        {
            ocean = new OceanArea
            {
                seaLevelY = seaLevelY,
                bounds = bounds,
                name = name
            };
            Debug.Log($"[WaterSystem] Oceano registrado: '{name}' em Y={seaLevelY:F1}m.");
        }

        public void RegisterLake(Vector3 center, float radiusX, float radiusZ, float surfaceY, string name = "Lago")
        {
            lakes.Add(new LakeArea
            {
                center = center,
                radiusX = Mathf.Max(10f, radiusX),
                radiusZ = Mathf.Max(10f, radiusZ),
                surfaceY = surfaceY,
                name = name
            });
            Debug.Log($"[WaterSystem] Lago registrado: '{name}' em {center} Y={surfaceY:F1}m (R={radiusX:F0}x{radiusZ:F0}).");
        }

        public void RegisterRiverSegment(Vector3 a, Vector3 b, float width, float flowSpeed, string name)
        {
            Vector3 dir = (b - a);
            dir.y = 0f;
            float len = dir.magnitude;
            if (len < 0.01f) return;

            Vector3 flowDir = dir.normalized;
            var min = Vector3.Min(a, b) - new Vector3(width * 0.7f, 10f, width * 0.7f);
            var max = Vector3.Max(a, b) + new Vector3(width * 0.7f, 10f, width * 0.7f);

            var bounds = new Bounds((min + max) * 0.5f, max - min);

            rivers.Add(new RiverSegment
            {
                start = a,
                end = b,
                width = width,
                flowDir = flowDir,
                flowSpeed = flowSpeed,
                name = string.IsNullOrEmpty(name) ? "Rio" : name,
                bounds = bounds
            });
        }

        #endregion

        #region Cálculo de Ondas Procedurais (Sincronizado com o Shader)

        /// <summary>
        /// Calcula a oscilação da crista da onda na CPU exatamente com a mesma fórmula do WaterSurface.shader.
        /// Garante que barcos, nadadores e física acompanhem perfeitamente a malha visual na tela.
        /// </summary>
        public static float GetWaveDisplacement(Vector3 worldPos, float time, float amplitude = 0.35f, float speed = 1.3f)
        {
            float t = time * speed;
            float w1 = Mathf.Sin((worldPos.x * 0.08f + worldPos.z * 0.05f) + t * 1.5f) * amplitude;
            float w2 = Mathf.Cos((worldPos.x * -0.04f + worldPos.z * 0.09f) + t * 2.1f) * (amplitude * 0.55f);
            float w3 = Mathf.Sin((worldPos.x * 0.15f - worldPos.z * 0.12f) + t * 3.0f) * (amplitude * 0.22f);
            return w1 + w2 + w3;
        }

        #endregion

        #region Consultas de Água em Tempo Real

        /// <summary>
        /// Retorna a informação completa de água para qualquer posição do mundo (superfície, profundidade, correnteza).
        /// </summary>
        public bool GetWaterInfo(Vector3 worldPos, out WaterInfo info)
        {
            info = new WaterInfo
            {
                isInWater = false,
                surfaceY = -9999f,
                baseWaterY = -9999f,
                depth = 0f,
                submersion = 0f,
                flowVelocity = Vector3.zero,
                bodyType = WaterBodyType.None,
                bodyName = string.Empty
            };

            float currentTime = Time.time;

            // 1. Verificar Rios Primeiro (alta prioridade local)
            for (int i = 0; i < rivers.Count; i++)
            {
                var r = rivers[i];
                if (!r.bounds.Contains(worldPos)) continue;

                float distSqr = DistancePointToSegment2DSqr(worldPos, r.start, r.end, out float t);
                float maxDist = r.width * 0.5f;

                if (distSqr <= maxDist * maxDist)
                {
                    float baseRiverY = Mathf.Lerp(r.start.y, r.end.y, t);
                    float wave = GetWaveDisplacement(worldPos, currentTime, globalWaveHeight * 0.6f, globalWaveSpeed * 1.5f);
                    float surfaceY = baseRiverY + wave;

                    info.isInWater = true;
                    info.baseWaterY = baseRiverY;
                    info.surfaceY = surfaceY;
                    info.submersion = surfaceY - worldPos.y;
                    info.flowVelocity = r.flowDir * r.flowSpeed;
                    info.bodyType = WaterBodyType.River;
                    info.bodyName = r.name;

                    float bedY = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.GetTerrainHeight(worldPos) : (baseRiverY - 2.5f);
                    info.depth = Mathf.Max(0.5f, surfaceY - bedY);
                    return true;
                }
            }

            // 2. Verificar Lagos
            for (int i = 0; i < lakes.Count; i++)
            {
                var lake = lakes[i];
                float dx = (worldPos.x - lake.center.x) / lake.radiusX;
                float dz = (worldPos.z - lake.center.z) / lake.radiusZ;

                if (dx * dx + dz * dz <= 1.0f)
                {
                    float wave = GetWaveDisplacement(worldPos, currentTime, globalWaveHeight * 0.75f, globalWaveSpeed);
                    float surfaceY = lake.surfaceY + wave;

                    info.isInWater = true;
                    info.baseWaterY = lake.surfaceY;
                    info.surfaceY = surfaceY;
                    info.submersion = surfaceY - worldPos.y;

                    // Corrente circular leve de lago
                    Vector3 toCenter = (worldPos - lake.center).normalized;
                    info.flowVelocity = Vector3.Cross(Vector3.up, toCenter) * 0.35f;
                    info.bodyType = WaterBodyType.Lake;
                    info.bodyName = lake.name;

                    float bedY = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.GetTerrainHeight(worldPos) : (lake.surfaceY - 4.0f);
                    info.depth = Mathf.Max(0.5f, surfaceY - bedY);
                    return true;
                }
            }

            // 3. Verificar Oceano
            if (ocean != null)
            {
                bool insideOcean = ocean.bounds.size == Vector3.zero || ocean.bounds.Contains(worldPos);
                if (insideOcean)
                {
                    float wave = GetWaveDisplacement(worldPos, currentTime, globalWaveHeight, globalWaveSpeed);
                    float surfaceY = ocean.seaLevelY + wave;

                    float terrainH = RegionalSandboxManager.Instance != null ? RegionalSandboxManager.Instance.GetTerrainHeight(worldPos) : -100f;
                    // Só está na água do mar se o terreno for menor ou igual ao nível do mar (costa e mar aberto)
                    if (terrainH <= ocean.seaLevelY + 1.5f)
                    {
                        info.isInWater = true;
                        info.baseWaterY = ocean.seaLevelY;
                        info.surfaceY = surfaceY;
                        info.submersion = surfaceY - worldPos.y;

                        // Corrente marítima suave em direção à costa
                        info.flowVelocity = new Vector3(0.2f, 0f, 0.4f);
                        info.bodyType = WaterBodyType.Ocean;
                        info.bodyName = ocean.name;
                        info.depth = Mathf.Max(0.5f, surfaceY - terrainH);
                        return true;
                    }
                }
            }

            return false;
        }

        private static float DistancePointToSegment2DSqr(Vector3 p, Vector3 a, Vector3 b, out float t)
        {
            float abx = b.x - a.x;
            float abz = b.z - a.z;
            float apx = p.x - a.x;
            float apz = p.z - a.z;

            float abLenSqr = abx * abx + abz * abz;
            if (abLenSqr < 0.0001f)
            {
                t = 0f;
                return apx * apx + apz * apz;
            }

            t = Mathf.Clamp01((apx * abx + apz * abz) / abLenSqr);
            float projX = a.x + t * abx;
            float projZ = a.z + t * abz;

            float dx = p.x - projX;
            float dz = p.z - projZ;
            return dx * dx + dz * dz;
        }

        #endregion

        #region Criação de Materiais Aquáticos Fotorrealistas

        public static Material CreateWaterMaterial(bool isRiver = false, float flowSpeed = 0.5f)
        {
            var shader = Shader.Find("ProjectTerra/WaterSurface") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.name = isRiver ? "Mat_RiverWater_Dynamic" : "Mat_OceanLake_Dynamic";

            if (isRiver)
            {
                mat.SetColor("_ShallowColor", new Color(0.18f, 0.60f, 0.68f, 0.82f));
                mat.SetColor("_DeepColor", new Color(0.08f, 0.28f, 0.42f, 0.90f));
                mat.SetFloat("_FlowSpeed", flowSpeed);
                mat.SetFloat("_WaveHeight", 0.22f);
                mat.SetFloat("_WaveSpeed", 1.8f);
            }
            else
            {
                mat.SetColor("_ShallowColor", new Color(0.12f, 0.52f, 0.70f, 0.80f));
                mat.SetColor("_DeepColor", new Color(0.03f, 0.16f, 0.36f, 0.94f));
                mat.SetFloat("_FlowSpeed", 0.05f);
                mat.SetFloat("_WaveHeight", 0.38f);
                mat.SetFloat("_WaveSpeed", 1.2f);
            }

            return mat;
        }

        #endregion

        #region Atualização de Câmera Submersa (Visão Subaquática)

        private void LateUpdate()
        {
            UpdateUnderwaterEffects();
        }

        private void UpdateUnderwaterEffects()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null) return;
            }

            Vector3 camPos = cachedCamera.transform.position;
            bool underwater = false;

            if (GetWaterInfo(camPos, out var info))
            {
                if (camPos.y < info.surfaceY)
                {
                    underwater = true;
                }
            }

            if (underwater && !isCameraUnderwater)
            {
                // Entrou na água: salva névoa original e ativa névoa azul submersa
                if (!savedOriginalFog)
                {
                    prevFogEnabled = RenderSettings.fog;
                    prevFogColor = RenderSettings.fogColor;
                    prevFogDensity = RenderSettings.fogDensity;
                    prevFogMode = RenderSettings.fogMode;
                    prevFogStart = RenderSettings.fogStartDistance;
                    prevFogEnd = RenderSettings.fogEndDistance;
                    savedOriginalFog = true;
                }

                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(0.05f, 0.25f, 0.38f, 1f);
                RenderSettings.fogDensity = 0.045f;
                isCameraUnderwater = true;
            }
            else if (!underwater && isCameraUnderwater)
            {
                // Saiu da água: restaura atmosfera original
                if (savedOriginalFog)
                {
                    RenderSettings.fog = prevFogEnabled;
                    RenderSettings.fogColor = prevFogColor;
                    RenderSettings.fogDensity = prevFogDensity;
                    RenderSettings.fogMode = prevFogMode;
                    RenderSettings.fogStartDistance = prevFogStart;
                    RenderSettings.fogEndDistance = prevFogEnd;
                }
                isCameraUnderwater = false;
            }
        }

        #endregion
    }
}
