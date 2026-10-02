using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class VehicleController
    {
        private void UpdateVisuals()
        {
            // Girar hélice do avião
            if (propellerTransform != null)
            {
                float rpm = isPlayerDriven ? Mathf.Lerp(300f, propellerMaxRpm, Mathf.Max(0.1f, currentThrottle)) : 0f;
                propellerAngle += rpm * 6f * Time.deltaTime;
                propellerTransform.localRotation = Quaternion.Euler(0f, 0f, propellerAngle);
            }

            // Esterçar rodas dianteiras se atribuídas
            if (frontLeftWheel != null) frontLeftWheel.localEulerAngles = new Vector3(0f, currentSteerAngle, 0f);
            if (frontRightWheel != null) frontRightWheel.localEulerAngles = new Vector3(0f, currentSteerAngle, 0f);
        }

        #region Iluminação & Faróis [L]

        public void ToggleHeadlights()
        {
            SetHeadlights(!headLightsOn);
        }

        public void SetHeadlights(bool state)
        {
            headLightsOn = state;

            if (headLights == null || headLights.Length == 0)
            {
                CreateDefaultHeadlights();
            }

            if (headLights != null)
            {
                foreach (var light in headLights)
                {
                    if (light != null)
                    {
                        light.enabled = headLightsOn;
                    }
                }
            }

            SandboxHUD.Instance?.ShowToast(headLightsOn ? $"💡 Faróis de {vehicleName}: Ligados" : $"💡 Faróis de {vehicleName}: Desligados", 1.8f);
        }

        public void CreateDefaultHeadlights()
        {
            var lightList = new System.Collections.Generic.List<Light>();
            float forwardOffset = (category == VehicleCategory.Truck || category == VehicleCategory.Boat) ? 3.6f : 2.1f;
            float upOffset = (category == VehicleCategory.Truck) ? 1.1f : 0.65f;
            float spread = (category == VehicleCategory.Plane) ? 2.2f : 0.85f;

            for (int i = -1; i <= 1; i += 2)
            {
                var lightObj = new GameObject($"Headlight_{(i == -1 ? "Left" : "Right")}");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = new Vector3(i * spread, upOffset, forwardOffset);
                lightObj.transform.localRotation = Quaternion.Euler(6f, i * 2f, 0f);

                var spot = lightObj.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.spotAngle = 65f;
                spot.range = 65f;
                spot.color = new Color(1.0f, 0.96f, 0.88f);
                spot.intensity = 2.8f;
                spot.shadows = LightShadows.Soft;
                spot.enabled = headLightsOn;

                lightList.Add(spot);
            }

            headLights = lightList.ToArray();
        }

        #endregion
    }
}
