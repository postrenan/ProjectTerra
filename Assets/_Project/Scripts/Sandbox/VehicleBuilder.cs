using UnityEngine;

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
            col.size = new Vector3(2.4f, 2.6f, 3.8f);
            col.center = new Vector3(0f, 1.3f, 0f);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = VehicleCategory.Tractor;
            vehicle.vehicleName = "Trator Agrícola TerraMaster 110";
            vehicle.maxSpeedKmh = 38f;
            vehicle.enginePower = 3200f;
            vehicle.maxCargoCapacityKg = 2000f;

            // Modelo 3D Importado ou Procedural
            var meshPrefab = Resources.Load<GameObject>("Models/Vehicles/tractor");
            if (meshPrefab != null)
            {
                var visual = Object.Instantiate(meshPrefab, root.transform);
                visual.name = "Visual_Trator3D";
                visual.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                visual.transform.localScale = Vector3.one * 1.6f;
                ApplyVehicleColormap(visual);
                // O modelo FBX já possui rodas próprias — NÃO criar cilindros procedurais por cima.
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
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-1.8f, 0.2f, -0.6f), "ExitPoint");
            vehicle.CreateDefaultHeadlights();

            return vehicle;
        }

        public static VehicleController CreateTruck(Vector3 position)
        {
            var root = new GameObject("Veiculo_CaminhaoCarga");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(2.6f, 3.0f, 7.5f);
            col.center = new Vector3(0f, 1.5f, 0f);

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
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                visual.transform.localScale = Vector3.one * 1.8f;
                ApplyVehicleColormap(visual);
                // O modelo FBX já possui rodas próprias — NÃO criar cilindros procedurais por cima.
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
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-2.1f, 0.2f, 2.1f), "ExitPoint");
            vehicle.CreateDefaultHeadlights();

            return vehicle;
        }

        public static VehicleController CreatePlane(Vector3 position)
        {
            var root = new GameObject("Veiculo_AviaoMonomotor");
            root.transform.position = position;

            var rb = root.GetComponent<Rigidbody>();
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(10.5f, 2.4f, 8.5f);
            col.center = new Vector3(0f, 1.2f, 0f);

            var vehicle = root.AddComponent<VehicleController>();
            vehicle.category = VehicleCategory.Plane;
            vehicle.vehicleName = "Aeronave Utilitária Skylark 180";
            vehicle.maxSpeedKmh = 190f;
            vehicle.enginePower = 5200f;
            vehicle.maxCargoCapacityKg = 650f;

            // Fuselagem Central
            var fuselage = CreatePart(root.transform, new Vector3(0f, 1.2f, 0f), new Vector3(1.5f, 1.6f, 7.5f), new Color(0.92f, 0.92f, 0.95f), "Fuselagem");

            // Asas (Envergadura 10.5m)
            var wings = CreatePart(root.transform, new Vector3(0f, 1.7f, 0.4f), new Vector3(10.5f, 0.15f, 1.8f), new Color(0.85f, 0.2f, 0.18f), "Asas");

            // Estabilizador Horizontal (Cauda)
            var tailH = CreatePart(root.transform, new Vector3(0f, 1.8f, -3.4f), new Vector3(3.2f, 0.12f, 0.9f), new Color(0.85f, 0.2f, 0.18f), "CaudaHorizontal");

            // Leme Vertical
            var tailV = CreatePart(root.transform, new Vector3(0f, 2.4f, -3.4f), new Vector3(0.15f, 1.4f, 1.2f), new Color(0.85f, 0.2f, 0.18f), "LemeVertical");

            // Trem de Pouso Triciclo
            CreateWheel(root.transform, new Vector3(0f, 0.35f, 2.6f), new Vector3(0.25f, 0.7f, 0.7f), "TremPousoDianteiro");
            CreateWheel(root.transform, new Vector3(-1.3f, 0.35f, -0.2f), new Vector3(0.25f, 0.7f, 0.7f), "TremPousoEsq");
            CreateWheel(root.transform, new Vector3(1.3f, 0.35f, -0.2f), new Vector3(0.25f, 0.7f, 0.7f), "TremPousoDir");

            // Hélice Frontal
            var propHub = new GameObject("Helice_Hub");
            propHub.transform.SetParent(root.transform);
            propHub.transform.localPosition = new Vector3(0f, 1.2f, 3.85f);
            var blade = CreatePart(propHub.transform, Vector3.zero, new Vector3(2.2f, 0.2f, 0.05f), new Color(0.1f, 0.1f, 0.1f), "Pas_Helice");
            vehicle.propellerTransform = propHub.transform;

            vehicle.driverSeatPoint = CreatePoint(root.transform, new Vector3(0f, 1.3f, 0.3f), "DriverSeat");
            vehicle.cockpitCameraPoint = CreatePoint(root.transform, new Vector3(0f, 1.6f, 0.5f), "CockpitCam");
            vehicle.exitPoint = CreatePoint(root.transform, new Vector3(-1.8f, 0.2f, 0.3f), "ExitPoint");
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
            col.size = new Vector3(2.1f, 1.7f, 4.6f);
            col.center = new Vector3(0f, 0.85f, 0f);

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
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                visual.transform.localScale = Vector3.one * 1.55f;
                ApplyVehicleColormap(visual);
                // O modelo FBX já possui rodas próprias — NÃO criar cilindros procedurais por cima.
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
            Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            var mat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            mat.color = color;
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.2f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.1f);
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
            Shader s = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Unlit/Color");
            var mat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            mat.color = new Color(0.12f, 0.12f, 0.13f); // Pneu de borracha escura
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.08f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
            rend.sharedMaterial = mat;
            Object.Destroy(wheel.GetComponent<Collider>());

            // Aro metálico interno
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = $"{name}_Aro";
            rim.transform.SetParent(wheel.transform);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localRotation = Quaternion.identity;
            rim.transform.localScale = new Vector3(0.55f, 1.02f, 0.55f);
            var rimMat = s != null ? new Material(s) : new Material(Shader.Find("Hidden/InternalErrorShader"));
            rimMat.color = new Color(0.78f, 0.80f, 0.83f);
            if (rimMat.HasProperty("_Glossiness")) rimMat.SetFloat("_Glossiness", 0.45f);
            if (rimMat.HasProperty("_Metallic")) rimMat.SetFloat("_Metallic", 0.8f);
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
    }
}
