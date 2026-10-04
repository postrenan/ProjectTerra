using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public partial class PlayerCharacterController
    {
        private void UpdateInVehicle()
        {
            if (IsInputBlocked()) return;

            // Alternar modos de câmera veicular com [C] ou [V]
            if (TerraInput.GetKeyDown(Key.C) || TerraInput.GetKeyDown(Key.V))
            {
                CycleVehicleCameraMode();
            }

            float mouseX = TerraInput.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = TerraInput.GetAxis("Mouse Y") * mouseSensitivity;

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
                thirdPersonPitch = Mathf.Clamp(thirdPersonPitch, 0f, 65f);

                // Auto-alinhar suavemente atrás do veículo em movimento caso o jogador não esteja movendo o mouse
                if (Mathf.Abs(mouseX) < 0.02f && currentVehicle.currentSpeedKmh > 2.0f)
                {
                    Vector3 flatFwd = Vector3.ProjectOnPlane(currentVehicle.transform.forward, Vector3.up);
                    if (flatFwd.sqrMagnitude > 0.001f)
                    {
                        float targetYaw = Quaternion.LookRotation(flatFwd.normalized, Vector3.up).eulerAngles.y;
                        thirdPersonYaw = Mathf.LerpAngle(thirdPersonYaw, targetYaw, Time.deltaTime * 3.5f);
                    }
                }

                // Ajuste fino de distância na 3ª pessoa com roda de scroll
                float scroll = TerraInput.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    vehicleThirdPersonDistance = Mathf.Clamp(vehicleThirdPersonDistance - scroll * 1.5f, 3.0f, 35f);
                }
            }

            // Tecla E para desembarcar
            if (TerraInput.GetKeyDown(Key.E))
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
                if (RegionalSandboxManager.Instance != null)
                {
                    float minFootY = RegionalSandboxManager.Instance.GetTerrainHeight(eyePos) + 0.35f;
                    if (eyePos.y < minFootY) eyePos.y = minFootY;
                }
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
                    Vector3 focusPoint = currentVehicle.transform.position + Vector3.up * (currentVehicle.category == VehicleCategory.Plane ? 1.2f : 1.6f);
                    Quaternion camRot = Quaternion.Euler(thirdPersonPitch, thirdPersonYaw, 0f);
                    Vector3 camBackDir = -(camRot * Vector3.forward);
                    float targetDist = vehicleThirdPersonDistance;

                    // Detecção de colisão do terreno e obstáculos externos
                    // Em avião voando alto (>3m do solo), ignorar SphereCast para evitar solavancos da câmera ao passar perto de copas ou folhagens
                    bool isFlyingHigh = currentVehicle.category == VehicleCategory.Plane && currentVehicle.currentAltitudeMeters > 3.0f;
                    float closestHitDist = targetDist;

                    if (!isFlyingHigh)
                    {
                        RaycastHit[] hits = Physics.SphereCastAll(focusPoint, 0.35f, camBackDir, targetDist, ~0, QueryTriggerInteraction.Ignore);
                        for (int i = 0; i < hits.Length; i++)
                        {
                            var hit = hits[i];
                            if (hit.collider.transform.IsChildOf(currentVehicle.transform) || hit.collider.transform.IsChildOf(transform))
                                continue;

                            if (hit.distance < closestHitDist)
                            {
                                closestHitDist = Mathf.Max(1.6f, hit.distance - 0.25f);
                            }
                        }
                    }

                    Vector3 desiredCamPos = focusPoint + camBackDir * closestHitDist;

                    // Proteção de corte absoluto: a câmera nunca pode descer abaixo da superfície do terreno
                    if (RegionalSandboxManager.Instance != null)
                    {
                        float terrainHeight = RegionalSandboxManager.Instance.GetTerrainHeight(desiredCamPos);
                        float minGroundY = terrainHeight + 0.65f;
                        if (desiredCamPos.y < minGroundY)
                        {
                            desiredCamPos.y = minGroundY;
                        }
                    }

                    // Amortecimento suave da câmera externa.
                    // Vector3.Lerp NÃO limita t (Mathf.Lerp limita). Com dt > 0,055 s
                    // (~18 fps, hitch de GC, ou o warp de 16x do HUD) t passa de 1 e a
                    // câmera é colocada DEPOIS do alvo, voltando no frame seguinte:
                    // oscilação violenta. exp(1-t) é limitado em [0,1) para qualquer dt.
                    float camT = 1f - Mathf.Exp(-18f * Time.deltaTime);
                    activeCamera.transform.position = Vector3.Lerp(activeCamera.transform.position, desiredCamPos, camT);

                    Vector3 lookDir = focusPoint - activeCamera.transform.position;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Vector3 upVec = Vector3.up;
                        // Evita singularidade de LookAt se a câmera estiver olhando diretamente de cima para baixo
                        if (Mathf.Abs(Vector3.Dot(lookDir.normalized, Vector3.up)) > 0.98f)
                        {
                            upVec = currentVehicle.transform.up;
                        }
                        activeCamera.transform.rotation = Quaternion.LookRotation(lookDir, upVec);
                    }
                }
            }
        }
    }
}
