using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public partial class VehicleController
    {
        /// <summary>
        /// Repinta a lataria do veículo com a cor escolhida na loja. Só afeta as peças de
        /// carroceria construídas proceduralmente (chassi, cabine, casco, asa, etc.), nunca
        /// rodas, vidros ou materiais PBR importados — evitando corromper detalhes do modelo.
        /// Cria uma instância de material única por cor para não contaminar materiais partilhados.
        /// </summary>
        public void SetBodyColor(Color color)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            Material paint = URPMaterialHelper.CreateURPLitMaterial(
                $"Paint_{name}_{color.GetHashCode():X}", color, 0.45f, 0.1f);

            bool paintedAny = false;
            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                string n = rend.gameObject.name.ToLowerInvariant();
                if (IsBodyPart(n))
                {
                    rend.sharedMaterial = paint;
                    paintedAny = true;
                }
            }

            // Sem peças nomeadas (modelo PBR importado): não força recoloração para não
            // estragar os materiais originais. A cor segue como a "cor encomendada".
            if (!paintedAny)
            {
                Debug.Log($"[VehicleController] {vehicleName}: cor registrada, lataria importada preservada.");
            }
        }

        private static bool IsBodyPart(string n)
        {
            return n.Contains("chassi") || n.Contains("carroceria") || n.Contains("casco") ||
                   n.Contains("cabine") || n.Contains("asa") || n.Contains("proa") ||
                   n.Contains("fuselagem") || n.Contains("corpo") || n.Contains("body") ||
                   n.Contains("hull") || n.Contains("capo");
        }
    }
}
