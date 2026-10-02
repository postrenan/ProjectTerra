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
                        // NUNCA usar rend.material.color em primitivos: no Built-in Pipeline com o
                        // pacote HDRP instalado, o Default-Material herdado é um shader HDRP que não
                        // renderiza sob o Built-in, deixando o objeto branco. Criamos um material
                        // Standard explícito via o helper central (mesmo padrão dos demais spawners).
                        rend.sharedMaterial = RegionalSandboxManager.CreateSolidMaterial(new Color(0.9f, 0.2f, 1.0f, 0.8f));
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
