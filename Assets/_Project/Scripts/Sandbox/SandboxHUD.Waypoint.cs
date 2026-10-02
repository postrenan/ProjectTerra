using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Marcador de Waypoint 3D

        public void SetCustomWaypoint(Vector3 pos)
        {
            CustomWaypoint = pos;
            ShowToast("📍 Marcador de rota posicionado!", 2.5f);
        }

        public void ClearCustomWaypoint()
        {
            CustomWaypoint = null;
            if (waypointWorldMarker != null)
            {
                Destroy(waypointWorldMarker);
            }
            ShowToast("❌ Marcador de rota removido.", 2.0f);
        }

        private void UpdateWaypointVisualMarker()
        {
            if (CustomWaypoint.HasValue)
            {
                if (waypointWorldMarker == null)
                {
                    waypointWorldMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    waypointWorldMarker.name = "CustomWaypoint_Beacon";
                    Destroy(waypointWorldMarker.GetComponent<Collider>());
                    waypointWorldMarker.transform.localScale = new Vector3(8f, 150f, 8f);
                    var rend = waypointWorldMarker.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        rend.material.color = new Color(0.9f, 0.2f, 1.0f, 0.8f);
                    }
                }
                waypointWorldMarker.transform.position = CustomWaypoint.Value + Vector3.up * 75f;
            }
            else if (waypointWorldMarker != null)
            {
                Destroy(waypointWorldMarker);
            }
        }

        #endregion
    }
}
