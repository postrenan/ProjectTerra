using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Fábrica procedural que modela o Arado Agrícola Subsolador de Discos ("TerraMaster AgroPlow 500"):
    /// - Chassi tubular reforçado em vermelho agrícola
    /// - Barra de tração triangular com olhal de engate universal
    /// - Conjunto basculante de 5 discos côncavos de arar em aço forjado
    /// - Hastes curvas com raspadores de terra
    /// - Rodas de apoio de profundidade com pneus de borracha
    /// - Cilindro hidráulico de levante com haste cromada
    /// - Emissor de partículas de terra e fumaça de solo revolvido
    /// </summary>
    public static class PlowBuilder
    {
        public static PlowImplement CreatePlow(Vector3 position, Quaternion? rotation = null)
        {
            var root = new GameObject("Implemento_AradoAgricola");
            root.transform.position = position;
            root.transform.rotation = rotation ?? Quaternion.identity;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 850f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(2.8f, 1.2f, 3.4f);
            col.center = new Vector3(0f, 0.6f, 0f);

            var plow = root.AddComponent<PlowImplement>();

            // Materiais PBR
            Color redAgro = new Color(0.85f, 0.20f, 0.12f);
            Color darkSteel = new Color(0.22f, 0.24f, 0.26f);
            Color chromeSteel = new Color(0.85f, 0.88f, 0.92f);
            Color tireRubber = new Color(0.12f, 0.12f, 0.13f);

            Material matRed = CreatePbrMaterial("Mat_Plow_Red", redAgro, 0.2f, 0.35f);
            Material matSteel = CreatePbrMaterial("Mat_Plow_Steel", darkSteel, 0.85f, 0.65f);
            Material matChrome = CreatePbrMaterial("Mat_Plow_Chrome", chromeSteel, 0.95f, 0.85f);
            Material matRubber = CreatePbrMaterial("Mat_Plow_Rubber", tireRubber, 0.05f, 0.08f);

            // ================= 1. BARRA DE TRAÇÃO TRIANGULAR (ENGATE) =================
            var tongueRoot = new GameObject("Barra_Engate_Triangular");
            tongueRoot.transform.SetParent(root.transform, false);

            // Braço esquerdo da lança
            var leftBeam = CreatePart(tongueRoot.transform, new Vector3(-0.45f, 0.45f, 0.9f), new Vector3(0.12f, 0.12f, 1.9f), matRed, "Braco_Lanca_Esq");
            leftBeam.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);

            // Braço direito da lança
            var rightBeam = CreatePart(tongueRoot.transform, new Vector3(0.45f, 0.45f, 0.9f), new Vector3(0.12f, 0.12f, 1.9f), matRed, "Braco_Lanca_Dir");
            rightBeam.transform.localRotation = Quaternion.Euler(0f, -22f, 0f);

            // Travessa de reforço da lança
            CreatePart(tongueRoot.transform, new Vector3(0f, 0.45f, 0.8f), new Vector3(1.1f, 0.10f, 0.10f), matRed, "Travessa_Lanca");

            // Olhal / Acoplador de Engate Frontal
            var hitchCoupler = CreatePart(tongueRoot.transform, new Vector3(0f, 0.45f, 1.85f), new Vector3(0.25f, 0.14f, 0.35f), matChrome, "Olhal_Engate_Pino");
            var couplerHole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            couplerHole.name = "Pino_Acoplamento";
            couplerHole.transform.SetParent(hitchCoupler.transform, false);
            couplerHole.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            couplerHole.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
            couplerHole.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.Destroy(couplerHole.GetComponent<Collider>());

            plow.tongueCouplerPoint = hitchCoupler.transform;

            // ================= 2. CHASSI ESTRUTURAL PRINCIPAL =================
            var chassisRoot = new GameObject("Chassi_Principal");
            chassisRoot.transform.SetParent(root.transform, false);

            // Viga mestra transversal
            CreatePart(chassisRoot.transform, new Vector3(0f, 0.70f, 0f), new Vector3(2.6f, 0.18f, 0.22f), matRed, "Viga_Mestra_Transversal");

            // Longarinas longitudinais
            CreatePart(chassisRoot.transform, new Vector3(-0.95f, 0.70f, -0.6f), new Vector3(0.16f, 0.18f, 1.4f), matRed, "Longarina_Esq");
            CreatePart(chassisRoot.transform, new Vector3(0.95f, 0.70f, -0.6f), new Vector3(0.16f, 0.18f, 1.4f), matRed, "Longarina_Dir");
            CreatePart(chassisRoot.transform, new Vector3(0f, 0.70f, -0.8f), new Vector3(0.16f, 0.18f, 1.7f), matRed, "Longarina_Central");

            // ================= 3. RODAS DE APOIO E PROFUNDIDADE =================
            CreateGaugeWheel(chassisRoot.transform, new Vector3(-1.35f, 0.40f, -0.3f), matRubber, matChrome, "RodaApoioEsq");
            CreateGaugeWheel(chassisRoot.transform, new Vector3(1.35f, 0.40f, -0.3f), matRubber, matChrome, "RodaApoioDir");

            // ================= 4. CILINDRO HIDRÁULICO DE LEVANTE =================
            var hydroRoot = new GameObject("Cilindro_Hidraulico");
            hydroRoot.transform.SetParent(chassisRoot.transform, false);
            hydroRoot.transform.localPosition = new Vector3(0f, 0.95f, -0.1f);
            hydroRoot.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);

            var hydroBody = CreatePart(hydroRoot.transform, new Vector3(0f, 0f, 0f), new Vector3(0.15f, 0.15f, 0.6f), matSteel, "Camisa_Cilindro");
            var hydroPiston = CreatePart(hydroRoot.transform, new Vector3(0f, 0f, -0.35f), new Vector3(0.08f, 0.08f, 0.5f), matChrome, "Haste_Cromada");

            // ================= 5. CONJUNTO BASCULANTE DE DISCOS =================
            var discPivotObj = new GameObject("Pivo_Basculante_Discos");
            discPivotObj.transform.SetParent(root.transform, false);
            discPivotObj.transform.localPosition = new Vector3(0f, 0.65f, -0.4f);
            plow.discGangPivot = discPivotObj.transform;

            // Viga diagonal de suporte dos discos (ângulo de arado)
            var gangBeam = CreatePart(discPivotObj.transform, new Vector3(0f, 0f, 0f), new Vector3(2.5f, 0.15f, 0.18f), matRed, "Viga_PortaDiscos");
            gangBeam.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);

            // 5 Discos de corte espaçados diagonalmente
            var discTransforms = new List<Transform>();
            float[] xOffsets = { -1.0f, -0.5f, 0.0f, 0.5f, 1.0f };
            float[] zOffsets = {  0.35f,  0.18f, 0.0f, -0.18f, -0.35f };

            for (int i = 0; i < 5; i++)
            {
                var shank = new GameObject($"Conjunto_Disco_{i + 1}");
                shank.transform.SetParent(discPivotObj.transform, false);
                shank.transform.localPosition = new Vector3(xOffsets[i], -0.15f, zOffsets[i]);

                // Haste curva de aço fundido descendo da viga
                var leg = CreatePart(shank.transform, new Vector3(0f, -0.15f, 0f), new Vector3(0.10f, 0.35f, 0.12f), matSteel, "Haste_Curva");
                leg.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

                // Mancal / Eixo do disco
                var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hub.name = "Mancal_Rolamento";
                hub.transform.SetParent(shank.transform, false);
                hub.transform.localPosition = new Vector3(0f, -0.32f, 0f);
                hub.transform.localScale = new Vector3(0.20f, 0.15f, 0.20f);
                hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                hub.GetComponent<Renderer>().sharedMaterial = matSteel;
                Object.Destroy(hub.GetComponent<Collider>());

                // Disco de corte de solo (Cilindro achatado com concavidade aparente)
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Lamina_Disco_Arado";
                disc.transform.SetParent(shank.transform, false);
                disc.transform.localPosition = new Vector3(0.04f, -0.32f, 0f);
                disc.transform.localScale = new Vector3(0.68f, 0.045f, 0.68f);
                // Inclinação aerodinâmica de revolvimento: 24 graus de camber e 38 graus de ataque
                disc.transform.localRotation = Quaternion.Euler(24f, 38f, 90f);
                disc.GetComponent<Renderer>().sharedMaterial = matSteel;
                Object.Destroy(disc.GetComponent<Collider>());

                // Raspadeira traseira para evitar acúmulo de terra
                var scraper = CreatePart(shank.transform, new Vector3(-0.06f, -0.22f, -0.15f), new Vector3(0.05f, 0.25f, 0.08f), matSteel, "Raspadeira_Lama");
                scraper.transform.localRotation = Quaternion.Euler(20f, 15f, 0f);

                discTransforms.Add(disc.transform);
            }
            plow.discBlades = discTransforms.ToArray();

            // ================= 6. SINALIZAÇÃO & DETALHES =================
            CreateReflector(chassisRoot.transform, new Vector3(-1.2f, 0.75f, -1.3f), Color.red);
            CreateReflector(chassisRoot.transform, new Vector3(1.2f, 0.75f, -1.3f), Color.red);
            CreateReflector(chassisRoot.transform, new Vector3(-1.3f, 0.75f, 0.1f), new Color(1.0f, 0.6f, 0.1f));
            CreateReflector(chassisRoot.transform, new Vector3(1.3f, 0.75f, 0.1f), new Color(1.0f, 0.6f, 0.1f));

            // ================= 7. SISTEMA DE PARTÍCULAS DE TERRA =================
            var dustObj = new GameObject("Emissor_Poeira_Solo");
            dustObj.transform.SetParent(discPivotObj.transform, false);
            dustObj.transform.localPosition = new Vector3(0f, -0.35f, 0f);

            var ps = dustObj.AddComponent<ParticleSystem>();
            var psMain = ps.main;
            psMain.playOnAwake = false;
            psMain.loop = true;
            psMain.startLifetime = 0.8f;
            psMain.startSpeed = 1.8f;
            psMain.startSize = 0.65f;
            psMain.startColor = new Color(0.38f, 0.27f, 0.18f, 0.75f); // Cor terra revolvida
            psMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var psEmission = ps.emission;
            psEmission.rateOverTime = 35f;

            var psShape = ps.shape;
            psShape.shapeType = ParticleSystemShapeType.Box;
            psShape.scale = new Vector3(2.4f, 0.2f, 0.4f);

            var psRenderer = dustObj.GetComponent<ParticleSystemRenderer>();
            Shader partShader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Mobile/Particles/Alpha Blended") ?? Shader.Find("Unlit/Color");
            if (partShader != null)
            {
                psRenderer.sharedMaterial = new Material(partShader);
                psRenderer.sharedMaterial.color = new Color(0.38f, 0.27f, 0.18f, 0.7f);
            }

            plow.dirtParticles = ps;

            return plow;
        }

        private static GameObject CreatePart(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(part.GetComponent<Collider>());
            return part;
        }

        private static void CreateGaugeWheel(Transform parent, Vector3 localPos, Material matTire, Material matRim, string name)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPos;
            wheel.transform.localScale = new Vector3(0.42f, 0.16f, 0.42f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.GetComponent<Renderer>().sharedMaterial = matTire;
            Object.Destroy(wheel.GetComponent<Collider>());

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Aro_Metalico";
            rim.transform.SetParent(wheel.transform, false);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localScale = new Vector3(0.55f, 1.05f, 0.55f);
            rim.transform.localRotation = Quaternion.identity;
            rim.GetComponent<Renderer>().sharedMaterial = matRim;
            Object.Destroy(rim.GetComponent<Collider>());
        }

        private static void CreateReflector(Transform parent, Vector3 localPos, Color color)
        {
            var refObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            refObj.name = "Refletor_Seguranca";
            refObj.transform.SetParent(parent, false);
            refObj.transform.localPosition = localPos;
            refObj.transform.localScale = new Vector3(0.12f, 0.12f, 0.04f);
            var mat = CreatePbrMaterial("Mat_Reflector", color, 0.0f, 0.8f);
            refObj.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(refObj.GetComponent<Collider>());
        }

        private static Material CreatePbrMaterial(string name, Color color, float metallic, float smoothness)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            mat.name = name;
            mat.color = color;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            return mat;
        }
    }
}
