using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class PlayerCharacterController
    {
        private void UpdateInVehicle()
        {
            // Alternar modos de câmera veicular com [C] ou [V]
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.V))
            {
                CycleVehicleCameraMode();
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (vehicleCameraMode == VehicleCameraMode.FirstPersonCockpit)
            {
                // Olhar livre dentro do cockpit (1ª pessoa)
                vehicleYawLook += mouseX;
                vehiclePitchLook -= mouseY;
                vehicleYawLook = Mathf.Clamp(vehicleYawLook, -90f, 90f);
                vehiclePitchLook = Mathf.Clamp(vehiclePitchLook, -45f, 45f);
            }
            else if (vehicleCameraMode == VehicleCameraMode.BumperOrHood)
            {
                // Câmera no capô / para-choque: leve visão lateral ao virar o mouse
                vehicleYawLook += mouseX;
                vehiclePitchLook -= mouseY;
                vehicleYawLook = Mathf.Clamp(vehicleYawLook, -35f, 35f);
                vehiclePitchLook = Mathf.Clamp(vehiclePitchLook, -20f, 20f);
            }
            else
            {
                // Câmera orbital externa (3ª pessoa perto ou panorâmica)
                thirdPersonYaw += mouseX;
                thirdPersonPitch -= mouseY;
                thirdPersonPitch = Mathf.Clamp(thirdPersonPitch, -15f, 65f);

                // Auto-alinhar suavemente atrás do veículo em movimento caso o jogador não esteja movendo o mouse
                if (Mathf.Abs(mouseX) < 0.02f && currentVehicle.currentSpeedKmh > 2.0f)
                {
                    float targetYaw = currentVehicle.transform.eulerAngles.y;
                    thirdPersonYaw = Mathf.LerpAngle(thirdPersonYaw, targetYaw, Time.deltaTime * 3.5f);
                }

                // Ajuste fino de distância na 3ª pessoa com roda de scroll
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    vehicleThirdPersonDistance = Mathf.Clamp(vehicleThirdPersonDistance - scroll * 4f, 3.0f, 35f);
                }
            }

            // Tecla E para desembarcar
            if (Input.GetKeyDown(KeyCode.E))
            {
                ExitVehicle();
            }
        }

        public void CycleVehicleCameraMode()
        {
            if (!isDriving || currentVehicle == null) return;

            switch (vehicleCameraMode)
            {
                case VehicleCameraMode.ThirdPersonClose:
                    vehicleCameraMode = VehicleCameraMode.ThirdPersonFar;
                    ApplyCameraDistancesForMode();
                    SandboxHUD.Instance?.ShowToast("🎥 Câmera: Terceira Pessoa (Panorâmica)", 1.8f);
                    break;
                case VehicleCameraMode.ThirdPersonFar:
                    vehicleCameraMode = VehicleCameraMode.FirstPersonCockpit;
                    vehicleYawLook = 0f;
                    vehiclePitchLook = 0f;
                    SandboxHUD.Instance?.ShowToast("🎥 Câmera: Cockpit (1ª Pessoa)", 1.8f);
                    break;
                case VehicleCameraMode.FirstPersonCockpit:
                    vehicleCameraMode = VehicleCameraMode.BumperOrHood;
                    vehicleYawLook = 0f;
                    vehiclePitchLook = 0f;
                    SandboxHUD.Instance?.ShowToast("🎥 Câmera: Capô / Para-choque Frontal", 1.8f);
                    break;
                case VehicleCameraMode.BumperOrHood:
                default:
                    vehicleCameraMode = VehicleCameraMode.ThirdPersonClose;
                    ApplyCameraDistancesForMode();
                    SandboxHUD.Instance?.ShowToast("🎥 Câmera: Terceira Pessoa (Perto)", 1.8f);
                    break;
            }
        }

        public void ApplyCameraDistancesForMode()
        {
            if (currentVehicle == null) return;

            if (vehicleCameraMode == VehicleCameraMode.ThirdPersonClose)
            {
                switch (currentVehicle.category)
                {
                    case VehicleCategory.Tractor: vehicleThirdPersonDistance = 5.5f; break;
                    case VehicleCategory.Truck: vehicleThirdPersonDistance = 8.5f; break;
                    case VehicleCategory.Plane: vehicleThirdPersonDistance = 11.0f; break;
                    case VehicleCategory.Boat: vehicleThirdPersonDistance = 8.5f; break;
                    case VehicleCategory.Car:
                    default: vehicleThirdPersonDistance = 5.5f; break;
                }
            }
            else if (vehicleCameraMode == VehicleCameraMode.ThirdPersonFar)
            {
                switch (currentVehicle.category)
                {
                    case VehicleCategory.Tractor: vehicleThirdPersonDistance = 11.0f; break;
                    case VehicleCategory.Truck: vehicleThirdPersonDistance = 16.0f; break;
                    case VehicleCategory.Plane: vehicleThirdPersonDistance = 20.0f; break;
                    case VehicleCategory.Boat: vehicleThirdPersonDistance = 16.0f; break;
                    case VehicleCategory.Car:
                    default: vehicleThirdPersonDistance = 10.5f; break;
                }
            }
        }

        private void UpdateCamera()
        {
            if (activeCamera == null) return;

            if (!isDriving || currentVehicle == null)
            {
                // 1ª PESSOA A PÉ
                Vector3 eyePos = cameraFollowPoint != null ? cameraFollowPoint.position : transform.position + Vector3.up * 1.65f;
                activeCamera.transform.position = eyePos;
                activeCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            else
            {
                // VEÍCULO: Cockpit, Capô ou 3ª Pessoa (Perto/Far)
                if (vehicleCameraMode == VehicleCameraMode.FirstPersonCockpit)
                {
                    Vector3 cockpitPos = currentVehicle.GetCockpitPosition();
                    activeCamera.transform.position = cockpitPos;

                    Quaternion vehicleRot = currentVehicle.transform.rotation;
                    Quaternion lookOffset = Quaternion.Euler(vehiclePitchLook, vehicleYawLook, 0f);
                    activeCamera.transform.rotation = vehicleRot * lookOffset;
                }
                else if (vehicleCameraMode == VehicleCameraMode.BumperOrHood)
                {
                    float fwdOffset = (currentVehicle.category == VehicleCategory.Truck || currentVehicle.category == VehicleCategory.Boat) ? 3.5f : 2.1f;
                    float upOffset = (currentVehicle.category == VehicleCategory.Truck) ? 1.6f : 0.9f;
                    Vector3 hoodPos = currentVehicle.transform.position + currentVehicle.transform.forward * fwdOffset + currentVehicle.transform.up * upOffset;
                    activeCamera.transform.position = hoodPos;

                    Quaternion vehicleRot = currentVehicle.transform.rotation;
                    Quaternion lookOffset = Quaternion.Euler(vehiclePitchLook * 0.4f, vehicleYawLook * 0.4f, 0f);
                    activeCamera.transform.rotation = vehicleRot * lookOffset;
                }
                else
                {
                    Vector3 focusPoint = currentVehicle.transform.position + Vector3.up * (currentVehicle.category == VehicleCategory.Plane ? 1.0f : 1.5f);
                    Quaternion camRot = Quaternion.Euler(thirdPersonPitch, thirdPersonYaw, 0f);
                    Vector3 desiredCamPos = focusPoint - (camRot * Vector3.forward * vehicleThirdPersonDistance);

                    // Amortecimento suave da câmera externa
                    activeCamera.transform.position = Vector3.Lerp(activeCamera.transform.position, desiredCamPos, Time.deltaTime * 18f);
                    activeCamera.transform.LookAt(focusPoint);
                }
            }
        }
    }
}
