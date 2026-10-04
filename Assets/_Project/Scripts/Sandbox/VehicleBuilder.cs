using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Fábrica que monta os 4 veículos jogáveis do Marco 1 com física, colisores e geometrias visuais completas:
    /// 1. Trator Agrícola com cabine e rodas grandes
    /// 2. Caminhão de Carga Pesada com chassi e caçamba
    /// 3. Avião Utilitário Monomotor com asas, hélice e trem de pouso
    /// 4. Barco Pesqueiro com casco náutico e cabine de comando
    /// </summary>
    public static class VehicleBuilder
    {
        public static VehicleController CreateTractor(Vector3 position)
        {
            var root = new GameObject("Veiculo_TratorAgricola");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            // Collider do chassi elevado do chão para evitar fricção/engaste na malha do terreno
            col.size = new Vector3(2.0f, 2.0f, 3.4f);
            col.center = new Vector3(0f, 1.45f, 0f);

            // Esferas de contato de baixo atrito nas 4 rodas para rolagem fluida e tração contínua
            var wMat = GetWheelPhysicsMaterial();
            AddWheelSphere(root, new Vector3(-1.05f, 0.85f, -0.8f), 0.85f, wMat);
            AddWheelSphere(root, new Vector3(1.05f, 0.85f, -0.8f), 0.85f, wMat);
            AddWheelSphere(root, new Vector3(-0.95f, 0.5f, 1.1f), 0.5f, wMat);
            AddWheelSphere(root, new Vector3(0.95f, 0.5f, 1.1f), 0.5f, wMat);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = VehicleCategory.Tractor;
            vehicle.vehicleName = "Trator Agrícola TerraMaster 110";
            vehicle.maxSpeedKmh = 42f;
            vehicle.enginePower = 3400f;
            vehicle.maxCargoCapacityKg = 2000f;

            // Modelo 3D Importado ou Procedural
            var meshPrefab = Resources.Load<GameObject>("Models/Vehicles/tractor");
            if (meshPrefab != null)
            {
                var visual = Object.Instantiate(meshPrefab, root.transform);
                visual.name = "Visual_Trator3D";
                visual.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 1.6f;
                ApplyVehicleColormap(visual);

                // Mapeia as rodas do FBX para esterçamento e animação visual
                vehicle.frontLeftWheel = FindDeepChild(visual.transform, "wheel-front-left");
                vehicle.frontRightWheel = FindDeepChild(visual.transform, "wheel-front-right");
                vehicle.rearLeftWheel = FindDeepChild(visual.transform, "wheel-back-left");
                vehicle.rearRightWheel = FindDeepChild(visual.transform, "wheel-back-right");
            }
            else
            {
                // Chassi / Capô
                var body = CreatePart(root.transform, new Vector3(0f, 1.1f, 0.4f), new Vector3(1.6f, 1.2f, 2.4f), new Color(0.2f, 0.65f, 0.25f), "Capo_Motor");

                // Cabine de Vidro
                var cab = CreatePart(root.transform, new Vector3(0f, 1.9f, -0.6f), new Vector3(1.7f, 1.5f, 1.5f), new Color(0.2f, 0.25f, 0.3f), "Cabine_Cab");

                // Escapamento
                var exhaust = CreatePart(root.transform, new Vector3(0.7f, 2.2f, 0.8f), new Vector3(0.15f, 1.6f, 0.15f), new Color(0.1f, 0.1f, 0.12f), "Escapamento");

                // Rodas procedurais (somente no fallback sem modelo 3D)
                var rWheelL = CreateWheel(root.transform, new Vector3(-1.1f, 0.9f, -0.8f), new Vector3(0.5f, 1.8f, 1.8f), "RodaTraseiraEsq");
                var rWheelR = CreateWheel(root.transform, new Vector3(1.1f, 0.9f, -0.8f), new Vector3(0.5f, 1.8f, 1.8f), "RodaTraseiraDir");
                var fWheelL = CreateWheel(root.transform, new Vector3(-0.95f, 0.5f, 1.1f), new Vector3(0.4f, 1.0f, 1.0f), "RodaDianteiraEsq");
                var fWheelR = CreateWheel(root.transform, new Vector3(0.95f, 0.5f, 1.1f), new Vector3(0.4f, 1.0f, 1.0f), "RodaDianteiraDir");

                vehicle.frontLeftWheel = fWheelL.transform;
                vehicle.frontRightWheel = fWheelR.transform;
                vehicle.rearLeftWheel = rWheelL.transform;
                vehicle.rearRightWheel = rWheelR.transform;
            }

            // Assento, Câmera de Cabine (1ª Pessoa) e Saída
            vehicle.driverSeatPoint = CreatePoint(root.transform, new Vector3(0f, 1.8f, -0.6f), "DriverSeat");
            vehicle.cockpitCameraPoint = CreatePoint(root.transform, new Vector3(0f, 2.1f, -0.35f), "CockpitCam");
            // Fora do collider da roda dianteira (centro -1.05,0.85,-0.8 r=0.85): a 1.01 m
            // de distância o jogador (capsula r=0.4) nascia dentro do Rigidbody.
            // Precisa de >= 0.85 + 0.4 = 1.25 m; agora está a 1.63 m.
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-2.6f, 0.4f, -0.6f), "ExitPoint");
            vehicle.rearHitchPoint = CreatePoint(root.transform, new Vector3(0f, 0.5f, -1.9f), "RearHitchPoint");
            CreatePart(root.transform, new Vector3(0f, 0.45f, -1.85f), new Vector3(0.3f, 0.15f, 0.35f), new Color(0.2f, 0.2f, 0.22f), "Engate_Traseiro_Trator");
            vehicle.CreateDefaultHeadlights();

            return vehicle;
        }

        public static VehicleController CreateTruck(Vector3 position)
        {
            var root = new GameObject("Veiculo_CaminhaoCarga");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(2.4f, 2.2f, 7.0f);
            col.center = new Vector3(0f, 1.6f, 0f);

            var wMat = GetWheelPhysicsMaterial();
            AddWheelSphere(root, new Vector3(-1.25f, 0.55f, 2.2f), 0.55f, wMat);
            AddWheelSphere(root, new Vector3(1.25f, 0.55f, 2.2f), 0.55f, wMat);
            AddWheelSphere(root, new Vector3(-1.25f, 0.55f, -2.2f), 0.55f, wMat);
            AddWheelSphere(root, new Vector3(1.25f, 0.55f, -2.2f), 0.55f, wMat);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = VehicleCategory.Truck;
            vehicle.vehicleName = "Caminhão de Carga Atlas Hauler";
            vehicle.maxSpeedKmh = 85f;
            vehicle.enginePower = 4800f;
            vehicle.maxCargoCapacityKg = 6000f;

            // Modelo 3D Importado ou Procedural
            var meshPrefab = Resources.Load<GameObject>("Models/Vehicles/truck");
            if (meshPrefab != null)
            {
                var visual = Object.Instantiate(meshPrefab, root.transform);
                visual.name = "Visual_Caminhao3D";
                visual.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 1.8f;
                ApplyVehicleColormap(visual);

                vehicle.frontLeftWheel = FindDeepChild(visual.transform, "wheel-front-left");
                vehicle.frontRightWheel = FindDeepChild(visual.transform, "wheel-front-right");
                vehicle.rearLeftWheel = FindDeepChild(visual.transform, "wheel-back-left");
                vehicle.rearRightWheel = FindDeepChild(visual.transform, "wheel-back-right");
            }
            else
            {
                // Cabine Frontal
                var cab = CreatePart(root.transform, new Vector3(0f, 1.8f, 2.2f), new Vector3(2.5f, 2.4f, 2.2f), new Color(0.15f, 0.35f, 0.75f), "Cabine_Caminhao");

                // Para-brisa
                var windshield = CreatePart(root.transform, new Vector3(0f, 2.2f, 3.2f), new Vector3(2.2f, 1.0f, 0.2f), new Color(0.15f, 0.2f, 0.25f), "Parabrisa");

                // Chassi / Longarinas
                var chassis = CreatePart(root.transform, new Vector3(0f, 0.7f, -0.5f), new Vector3(2.4f, 0.5f, 6.8f), new Color(0.18f, 0.18f, 0.2f), "Chassi");

                // Carroceria / Caçamba de Carga Traseira
                var cargoBed = CreatePart(root.transform, new Vector3(0f, 1.6f, -1.2f), new Vector3(2.4f, 1.4f, 4.4f), new Color(0.6f, 0.6f, 0.65f), "Carroceria_Carga");

                // Rodas procedurais (somente no fallback sem modelo 3D)
                var fWheelL = CreateWheel(root.transform, new Vector3(-1.25f, 0.55f, 2.2f), new Vector3(0.45f, 1.1f, 1.1f), "RodaDiantEsq");
                var fWheelR = CreateWheel(root.transform, new Vector3(1.25f, 0.55f, 2.2f), new Vector3(0.45f, 1.1f, 1.1f), "RodaDiantDir");
                var rWheelL = CreateWheel(root.transform, new Vector3(-1.25f, 0.55f, -2.2f), new Vector3(0.55f, 1.1f, 1.1f), "RodaTrasEsq");
                var rWheelR = CreateWheel(root.transform, new Vector3(1.25f, 0.55f, -2.2f), new Vector3(0.55f, 1.1f, 1.1f), "RodaTrasDir");

                vehicle.frontLeftWheel = fWheelL.transform;
                vehicle.frontRightWheel = fWheelR.transform;
                vehicle.rearLeftWheel = rWheelL.transform;
                vehicle.rearRightWheel = rWheelR.transform;
            }

            vehicle.driverSeatPoint = CreatePoint(root.transform, new Vector3(-0.6f, 1.9f, 2.1f), "DriverSeat");
            vehicle.cockpitCameraPoint = CreatePoint(root.transform, new Vector3(-0.6f, 2.2f, 2.3f), "CockpitCam");
            // Fora do collider da roda (centro -1.25,0.55,2.2 r=0.55): a 0.93 m de distância
            // o jogador nascia dentro do Rigidbody. Precisa de >= 0.55 + 0.4 = 0.95 m.
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-3.0f, 0.4f, 2.1f), "ExitPoint");
            vehicle.CreateDefaultHeadlights();

            return vehicle;
        }

        public static VehicleController CreatePlane(Vector3 position)
        {
            var root = new GameObject("Veiculo_AviaoMonomotor");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            // Collider principal na cabine central (elevado para permitir rotação de decolagem sem tail-strike)
            col.size = new Vector3(1.4f, 1.3f, 4.4f);
            col.center = new Vector3(0f, 1.6f, 0.2f);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = VehicleCategory.Plane;
            vehicle.vehicleName = "Aeronave Utilitária Skylark 180";
            vehicle.maxSpeedKmh = 210f;
            vehicle.enginePower = 5400f;
            vehicle.takeOffSpeedKmh = 45f;
            vehicle.maxCargoCapacityKg = 650f;

            // --- 1. FUSELAGEM & CABINE AERODINÂMICA ---
            // Cabine Principal (branco aviação com linhas limpas)
            var cabin = CreatePart(root.transform, new Vector3(0f, 1.45f, 0.2f), new Vector3(1.35f, 1.35f, 3.2f), new Color(0.95f, 0.95f, 0.97f), "Cabine_Principal");
            // Faixa esportiva lateral vermelha
            CreatePart(root.transform, new Vector3(0f, 1.35f, 0.2f), new Vector3(1.38f, 0.16f, 3.2f), new Color(0.85f, 0.15f, 0.15f), "Faixa_Esportiva");

            // Capô do Motor afunilado
            var cowl = CreatePart(root.transform, new Vector3(0f, 1.32f, 2.3f), new Vector3(1.15f, 1.1f, 1.6f), new Color(0.92f, 0.92f, 0.94f), "Capo_Motor");
            // Grade frontal de arrefecimento / admissão de ar
            CreatePart(root.transform, new Vector3(0f, 1.25f, 3.12f), new Vector3(0.9f, 0.85f, 0.15f), new Color(0.12f, 0.12f, 0.14f), "Grade_Radiador");

            // Para-brisa panorâmico aerodinâmico inclinado
            var windshield = CreatePart(root.transform, new Vector3(0f, 1.85f, 1.15f), new Vector3(1.22f, 0.75f, 1.1f), new Color(0.12f, 0.18f, 0.26f), "Parabrisa_Panoramico");
            windshield.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);

            // Janelas laterais de observação
            CreatePart(root.transform, new Vector3(0f, 1.65f, 0.1f), new Vector3(1.38f, 0.45f, 1.8f), new Color(0.12f, 0.18f, 0.26f), "Janelas_Laterais");

            // Cone de Cauda esguio com diedro para cima (garante >25° de rotação sem colidir na pista)
            var tailBoom = CreatePart(root.transform, new Vector3(0f, 1.65f, -2.6f), new Vector3(0.75f, 0.8f, 3.2f), new Color(0.95f, 0.95f, 0.97f), "Cone_Cauda");
            tailBoom.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);

            // --- 2. ASAS ALTAS & ESTRUTURA AERODINÂMICA ---
            // Asa Superior (Envergadura 11.8m para excelente sustentação STOL)
            var wings = CreatePart(root.transform, new Vector3(0f, 2.22f, 0.35f), new Vector3(11.8f, 0.16f, 1.9f), new Color(0.95f, 0.95f, 0.97f), "Asa_Principal");
            // Faixa vermelha no bordo de ataque da asa
            CreatePart(root.transform, new Vector3(0f, 2.22f, 1.25f), new Vector3(11.82f, 0.14f, 0.25f), new Color(0.85f, 0.15f, 0.15f), "Bordo_Ataque_Vermelho");

            // Montantes de Asa (Struts diagonais aerodinâmicos conectando fuselagem e asa)
            var strutL = CreatePart(root.transform, new Vector3(-1.8f, 1.55f, 0.4f), new Vector3(0.08f, 0.08f, 2.8f), new Color(0.3f, 0.3f, 0.35f), "Montante_Asa_Esq");
            strutL.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            var strutR = CreatePart(root.transform, new Vector3(1.8f, 1.55f, 0.4f), new Vector3(0.08f, 0.08f, 2.8f), new Color(0.3f, 0.3f, 0.35f), "Montante_Asa_Dir");
            strutR.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);

            // Luzes de Navegação FAA nos bordos da asa e na cauda
            var navL = CreatePart(root.transform, new Vector3(-5.95f, 2.22f, 0.35f), new Vector3(0.2f, 0.22f, 0.5f), new Color(1.0f, 0.05f, 0.05f), "Luz_Bombordo_Vermelha");
            var lightL = navL.AddComponent<Light>();
            lightL.color = Color.red; lightL.range = 8f; lightL.intensity = 1.2f;

            var navR = CreatePart(root.transform, new Vector3(5.95f, 2.22f, 0.35f), new Vector3(0.2f, 0.22f, 0.5f), new Color(0.05f, 1.0f, 0.15f), "Luz_Boreste_Verde");
            var lightR = navR.AddComponent<Light>();
            lightR.color = Color.green; lightR.range = 8f; lightR.intensity = 1.2f;

            var navTail = CreatePart(root.transform, new Vector3(0f, 3.4f, -4.4f), new Vector3(0.15f, 0.25f, 0.15f), new Color(1.0f, 1.0f, 1.0f), "Luz_Estrobo_Cauda");
            var lightTail = navTail.AddComponent<Light>();
            lightTail.color = Color.white; lightTail.range = 10f; lightTail.intensity = 1.8f;

            // --- 3. EMPENAGEM (CAUDA & PROFUNDOR) ---
            // Estabilizador Vertical elegante com leme vermelho
            var tailV = CreatePart(root.transform, new Vector3(0f, 2.7f, -4.2f), new Vector3(0.14f, 1.6f, 1.4f), new Color(0.85f, 0.15f, 0.15f), "EstabilizadorVertical");
            tailV.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

            // Estabilizador Horizontal (Envergadura 3.6m) com profundores
            var tailH = CreatePart(root.transform, new Vector3(0f, 2.05f, -4.2f), new Vector3(3.6f, 0.12f, 1.1f), new Color(0.95f, 0.95f, 0.97f), "EstabilizadorHorizontal");
            CreatePart(root.transform, new Vector3(0f, 2.05f, -4.65f), new Vector3(3.62f, 0.10f, 0.3f), new Color(0.85f, 0.15f, 0.15f), "Profundor_Elevator");

            // --- 4. TREM DE POUSO BUSH PLANE & FÍSICA DE ROLAGEM ---
            var gearMat = new PhysicsMaterial("AeroGearPhysMat")
            {
                dynamicFriction = 0.02f,
                staticFriction = 0.02f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0.05f
            };

            // Pernas em lâmina de aço para trem de pouso principal
            var legL = CreatePart(root.transform, new Vector3(-0.85f, 0.65f, 0.1f), new Vector3(0.12f, 0.85f, 0.14f), new Color(0.2f, 0.2f, 0.25f), "Perna_TremPouso_Esq");
            legL.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            var legR = CreatePart(root.transform, new Vector3(0.85f, 0.65f, 0.1f), new Vector3(0.12f, 0.85f, 0.14f), new Color(0.2f, 0.2f, 0.25f), "Perna_TremPouso_Dir");
            legR.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);

            // Rodas Tundra / Bush (raio 0.42m para absorver irregularidades)
            var lWheel = CreateWheel(root.transform, new Vector3(-1.35f, 0.42f, 0.05f), new Vector3(0.35f, 0.84f, 0.84f), "TremPousoEsq");
            var lCol = lWheel.AddComponent<SphereCollider>();
            lCol.radius = 0.42f;
            lCol.sharedMaterial = gearMat;

            var rWheel = CreateWheel(root.transform, new Vector3(1.35f, 0.42f, 0.05f), new Vector3(0.35f, 0.84f, 0.84f), "TremPousoDir");
            var rCol = rWheel.AddComponent<SphereCollider>();
            rCol.radius = 0.42f;
            rCol.sharedMaterial = gearMat;

            // Bequilha dianteira direcional com amortecedor
            CreatePart(root.transform, new Vector3(0f, 0.7f, 2.5f), new Vector3(0.1f, 0.75f, 0.1f), new Color(0.2f, 0.2f, 0.25f), "Perna_Bequilha");
            var fWheel = CreateWheel(root.transform, new Vector3(0f, 0.35f, 2.5f), new Vector3(0.25f, 0.70f, 0.70f), "TremPousoDianteiro");
            var fCol = fWheel.AddComponent<SphereCollider>();
            fCol.radius = 0.35f;
            fCol.sharedMaterial = gearMat;

            // --- 5. HÉLICE TRIPÁ COM SPINNER CROMADO ---
            var propHub = new GameObject("Helice_Hub");
            propHub.transform.SetParent(root.transform);
            propHub.transform.localPosition = new Vector3(0f, 1.28f, 3.22f);

            // Spinner ogival polido
            var spinner = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spinner.name = "Spinner_Ogiva";
            spinner.transform.SetParent(propHub.transform);
            spinner.transform.localPosition = Vector3.zero;
            spinner.transform.localScale = new Vector3(0.42f, 0.42f, 0.65f);
            Object.Destroy(spinner.GetComponent<Collider>());
            var spinMat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            spinMat.color = new Color(0.85f, 0.87f, 0.90f);
            if (spinMat.HasProperty("_Metallic")) spinMat.SetFloat("_Metallic", 0.9f);
            if (spinMat.HasProperty("_Glossiness")) spinMat.SetFloat("_Glossiness", 0.85f);
            spinner.GetComponent<Renderer>().sharedMaterial = spinMat;

            // 3 Pás em compósito de carbono com pontas amarelas de aviso
            for (int b = 0; b < 3; b++)
            {
                float bladeAngle = b * 120f;
                var blade = CreatePart(propHub.transform, Vector3.zero, new Vector3(0.18f, 2.1f, 0.04f), new Color(0.12f, 0.12f, 0.13f), $"Pa_Helice_{b}");
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, bladeAngle);

                // Faixa amarela de segurança na ponta da hélice
                var tip = CreatePart(blade.transform, new Vector3(0f, 0.92f, 0f), new Vector3(0.19f, 0.24f, 0.05f), new Color(0.95f, 0.82f, 0.05f), $"Ponta_Amarela_{b}");
            }
            vehicle.propellerTransform = propHub.transform;

            // Pontos de Referência (Assento, Câmera e Saída)
            vehicle.driverSeatPoint = CreatePoint(root.transform, new Vector3(0f, 1.4f, 0.3f), "DriverSeat");
            vehicle.cockpitCameraPoint = CreatePoint(root.transform, new Vector3(0f, 1.75f, 0.6f), "CockpitCam");
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-1.8f, 0.3f, 0.3f), "ExitPoint");
            vehicle.CreateDefaultHeadlights();

            return vehicle;
        }

        public static VehicleController CreateBoat(Vector3 position)
        {
            var root = new GameObject("Veiculo_BarcoPesqueiro");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(3.2f, 3.5f, 9.0f);
            col.center = new Vector3(0f, 1.2f, 0f);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = VehicleCategory.Boat;
            vehicle.vehicleName = "Traineira Costeira Mar Azul";
            vehicle.maxSpeedKmh = 32f;
            vehicle.enginePower = 3800f;
            vehicle.maxCargoCapacityKg = 3000f;

            // Casco Náutico
            var hull = CreatePart(root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(3.0f, 1.5f, 8.5f), new Color(0.18f, 0.25f, 0.45f), "Casco_Barco");

            // Proa afilada
            var bow = CreatePart(root.transform, new Vector3(0f, 0.7f, 4.3f), new Vector3(2.2f, 1.4f, 1.5f), new Color(0.18f, 0.25f, 0.45f), "Proa");

            // Cabine de Comando / Passadiço
            var wheelhouse = CreatePart(root.transform, new Vector3(0f, 2.0f, 0.8f), new Vector3(2.2f, 1.8f, 2.4f), new Color(0.9f, 0.9f, 0.92f), "Cabine_Comando");

            // Mastro com Antena
            var mast = CreatePart(root.transform, new Vector3(0f, 3.8f, 0.6f), new Vector3(0.15f, 2.0f, 0.15f), new Color(0.7f, 0.7f, 0.7f), "Mastro");

            // Convés Traseiro para Redes e Carga de Peixe
            var deckStorage = CreatePart(root.transform, new Vector3(0f, 1.2f, -2.4f), new Vector3(2.4f, 0.8f, 2.6f), new Color(0.55f, 0.45f, 0.35f), "Porão_Peixe");

            vehicle.driverSeatPoint = CreatePoint(root.transform, new Vector3(0f, 2.0f, 0.8f), "DriverSeat");
            vehicle.cockpitCameraPoint = CreatePoint(root.transform, new Vector3(0f, 2.35f, 1.1f), "CockpitCam");
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-2.2f, 0.8f, 0.8f), "ExitPoint");
            vehicle.CreateDefaultHeadlights();

            return vehicle;
        }

        public static VehicleController CreateCar(Vector3 position, string modelName = "sedan", string displayName = "Sedan Executivo", VehicleCategory category = VehicleCategory.Car)
        {
            var root = new GameObject($"Veiculo_{modelName}");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.9f, 1.3f, 4.2f);
            col.center = new Vector3(0f, 1.05f, 0f);

            var wMat = GetWheelPhysicsMaterial();
            AddWheelSphere(root, new Vector3(-0.9f, 0.45f, 1.3f), 0.45f, wMat);
            AddWheelSphere(root, new Vector3(0.9f, 0.45f, 1.3f), 0.45f, wMat);
            AddWheelSphere(root, new Vector3(-0.9f, 0.45f, -1.3f), 0.45f, wMat);
            AddWheelSphere(root, new Vector3(0.9f, 0.45f, -1.3f), 0.45f, wMat);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = category;
            vehicle.vehicleName = displayName;
            vehicle.maxSpeedKmh = modelName.Contains("race") || modelName.Contains("sports") ? 180f : (modelName.Contains("suv") ? 130f : 140f);
            vehicle.enginePower = 4400f;
            vehicle.maxCargoCapacityKg = 500f;

            var meshPrefab = Resources.Load<GameObject>($"Models/Vehicles/{modelName}");
            if (meshPrefab != null)
            {
                var visual = Object.Instantiate(meshPrefab, root.transform);
                visual.name = $"Visual_{modelName}3D";
                visual.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 1.55f;
                ApplyVehicleColormap(visual);

                vehicle.frontLeftWheel = FindDeepChild(visual.transform, "wheel-front-left");
                vehicle.frontRightWheel = FindDeepChild(visual.transform, "wheel-front-right");
                vehicle.rearLeftWheel = FindDeepChild(visual.transform, "wheel-back-left");
                vehicle.rearRightWheel = FindDeepChild(visual.transform, "wheel-back-right");
            }
            else
            {
                // Corpo procedural de carro
                var body = CreatePart(root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 0.7f, 4.2f), new Color(0.2f, 0.45f, 0.85f), "Chassi_Carro");
                var cabin = CreatePart(root.transform, new Vector3(0f, 1.2f, -0.2f), new Vector3(1.6f, 0.65f, 2.2f), new Color(0.15f, 0.18f, 0.22f), "Cabine_Carro");

                // Rodas procedurais (somente no fallback sem modelo 3D)
                var fWheelL = CreateWheel(root.transform, new Vector3(-0.95f, 0.45f, 1.3f), new Vector3(0.35f, 0.85f, 0.85f), "RodaDiantEsq");
                var fWheelR = CreateWheel(root.transform, new Vector3(0.95f, 0.45f, 1.3f), new Vector3(0.35f, 0.85f, 0.85f), "RodaDiantDir");
                var rWheelL = CreateWheel(root.transform, new Vector3(-0.95f, 0.45f, -1.3f), new Vector3(0.35f, 0.85f, 0.85f), "RodaTrasEsq");
                var rWheelR = CreateWheel(root.transform, new Vector3(0.95f, 0.45f, -1.3f), new Vector3(0.35f, 0.85f, 0.85f), "RodaTrasDir");

                vehicle.frontLeftWheel = fWheelL.transform;
                vehicle.frontRightWheel = fWheelR.transform;
                vehicle.rearLeftWheel = rWheelL.transform;
                vehicle.rearRightWheel = rWheelR.transform;
            }

            vehicle.driverSeatPoint = CreatePoint(root.transform, new Vector3(-0.45f, 0.85f, 0f), "DriverSeat");
            vehicle.cockpitCameraPoint = CreatePoint(root.transform, new Vector3(-0.45f, 1.2f, 0.1f), "CockpitCam");
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-1.6f, 0.2f, 0f), "ExitPoint");

            vehicle.CreateDefaultHeadlights();
            return vehicle;
        }

        /// <summary>
        /// Aplica a textura de paleta colormap.png original da coleção Kenney em todos os MeshRenderers
        /// do veículo instanciado, garantindo cores reais de fábrica com sombreamento Standard.
        /// </summary>
        private static void ApplyVehicleColormap(GameObject visual)
        {
            if (visual == null) return;
            var colormap = Resources.Load<Texture2D>("Models/Vehicles/Textures/colormap");
            if (colormap == null)
            {
                colormap = Resources.Load<Texture2D>("Textures/colormap") 
                        ?? Resources.Load<Texture2D>("colormap");
            }

            if (colormap != null)
            {
                Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Texture");
                var mat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                mat.name = "Vehicle_Colormap_Material";
                mat.mainTexture = colormap;
                mat.color = Color.white;

                foreach (var mr in visual.GetComponentsInChildren<Renderer>(true))
                {
                    mr.sharedMaterial = mat;
                }
            }
        }

        private static GameObject CreatePart(Transform parent, Vector3 localPos, Vector3 scale, Color color, string name)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            var rend = part.GetComponent<Renderer>();
            var mat = URPMaterialHelper.CreateURPLitMaterial("VehiclePart_" + name, color, 0.2f, 0.1f);
            rend.sharedMaterial = mat;
            Object.Destroy(part.GetComponent<Collider>());
            return part;
        }

        private static GameObject CreateWheel(Transform parent, Vector3 localPos, Vector3 scale, string name)
        {
            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent);
            wheel.transform.localPosition = localPos;
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = scale;
            var rend = wheel.GetComponent<Renderer>();
            var mat = URPMaterialHelper.CreateURPLitMaterial("VehicleWheel_" + name, new Color(0.12f, 0.12f, 0.13f), 0.08f, 0.0f);
            rend.sharedMaterial = mat;
            Object.Destroy(wheel.GetComponent<Collider>());

            // Aro metálico interno
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = $"{name}_Aro";
            rim.transform.SetParent(wheel.transform);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localRotation = Quaternion.identity;
            rim.transform.localScale = new Vector3(0.55f, 1.02f, 0.55f);
            var rimMat = URPMaterialHelper.CreateURPLitMaterial("VehicleRim_" + name, new Color(0.78f, 0.80f, 0.83f), 0.45f, 0.8f);
            rim.GetComponent<Renderer>().sharedMaterial = rimMat;
            Object.Destroy(rim.GetComponent<Collider>());

            return wheel;
        }

        private static Transform CreatePoint(Transform parent, Vector3 localPos, string name)
        {
            var pt = new GameObject(name);
            pt.transform.SetParent(parent);
            pt.transform.localPosition = localPos;
            return pt.transform;
        }

        private static PhysicsMaterial wheelPhysMat;
        public static PhysicsMaterial GetWheelPhysicsMaterial()
        {
            if (wheelPhysMat == null)
            {
                wheelPhysMat = new PhysicsMaterial("VehicleWheelPhysMat")
                {
                    dynamicFriction = 0.03f,
                    staticFriction = 0.03f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounciness = 0.04f
                };
            }
            return wheelPhysMat;
        }

        private static SphereCollider AddWheelSphere(GameObject target, Vector3 center, float radius, PhysicsMaterial mat)
        {
            var col = target.AddComponent<SphereCollider>();
            col.center = center;
            col.radius = radius;
            col.sharedMaterial = mat;
            return col;
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }
            return null;
        }
    }
}
