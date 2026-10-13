using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Tipos de materiais básicos de construção no jogo.
    /// Podem ser obtidos pela gameplay (madeira cortando florestas/fazenda, brita/pedra em pedreiras/transporte,
    /// aço/metal em siderúrgicas/fretes industriais, concreto ensacado) ou comprados diretamente.
    /// </summary>
    public enum BuildResourceType
    {
        Wood,      // Madeira (toras, tábuas)
        Stone,     // Pedra / Brita / Cascalho
        Steel,     // Aço / Vigas / Chapas metálicas
        Concrete   // Concreto / Cimento
    }

    /// <summary>
    /// Requisito de um recurso específico para erguer uma construção manualmente.
    /// </summary>
    [Serializable]
    public class BuildResourceRequirement
    {
        public BuildResourceType type;
        public int amountRequired;
        public int amountDelivered;

        public BuildResourceRequirement(BuildResourceType type, int required)
        {
            this.type = type;
            this.amountRequired = required;
            this.amountDelivered = 0;
        }

        public bool IsFulfilled => amountDelivered >= amountRequired;
        public int Remaining => Mathf.Max(0, amountRequired - amountDelivered);

        public static string GetName(BuildResourceType type)
        {
            switch (type)
            {
                case BuildResourceType.Wood: return "Madeira";
                case BuildResourceType.Stone: return "Pedra / Brita";
                case BuildResourceType.Steel: return "Aço Estrutural";
                case BuildResourceType.Concrete: return "Cimento / Concreto";
                default: return type.ToString();
            }
        }

        public static long GetUnitPrice(BuildResourceType type)
        {
            switch (type)
            {
                case BuildResourceType.Wood: return 35;
                case BuildResourceType.Stone: return 25;
                case BuildResourceType.Steel: return 90;
                case BuildResourceType.Concrete: return 50;
                default: return 40;
            }
        }
    }

    /// <summary>
    /// Componente anexado ao "Fantasma / Canteiro de Obras" (Ghost / Holograma) colocado no mapa.
    /// Permite que o jogador inspecione os recursos faltantes, entregue materiais obtidos na gameplay
    /// ou compre os recursos diretamente no local com o saldo de dinheiro da conta.
    /// Ao completar 100% dos recursos, a construção é finalizada automaticamente!
    /// </summary>
    public class ConstructionSiteGhost : MonoBehaviour
    {
        public BuildItemDefinition itemDefinition;
        public List<BuildResourceRequirement> requirements = new List<BuildResourceRequirement>();
        public float progressPercent = 0f;

        private Renderer[] cachedRenderers;
        private Material ghostMat;
        private bool isCompleted = false;

        public bool IsCompleted => isCompleted;

        public void Setup(BuildItemDefinition item, List<BuildResourceRequirement> reqs)
        {
            this.itemDefinition = item;
            this.requirements = reqs != null ? reqs : new List<BuildResourceRequirement>();
            ApplyGhostVisuals();
            UpdateProgress();
        }

        private void ApplyGhostVisuals()
        {
            // Material translúcido holográfico (ciano / azul de canteiro de obras)
            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            ghostMat = new Material(s);
            ghostMat.color = new Color(0.2f, 0.75f, 1f, 0.45f);

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in cachedRenderers)
            {
                if (r != null)
                {
                    r.sharedMaterial = ghostMat;
                }
            }

            // Desativa colisões sólidas enquanto for fantasma/canteiro de obras para não travar o jogador
            var colliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in colliders)
            {
                if (col != null) col.isTrigger = true;
            }
        }

        public void UpdateProgress()
        {
            int totalReq = 0;
            int totalDelivered = 0;

            for (int i = 0; i < requirements.Count; i++)
            {
                totalReq += requirements[i].amountRequired;
                totalDelivered += requirements[i].amountDelivered;
            }

            progressPercent = totalReq > 0 ? ((float)totalDelivered / totalReq) * 100f : 100f;

            if (totalDelivered >= totalReq && !isCompleted)
            {
                FinalizeConstruction();
            }
        }

        /// <summary>
        /// O jogador entrega uma quantidade de materiais que ele coletou ou transportou.
        /// </summary>
        public bool DeliverResource(BuildResourceType type, int amount, out int delivered)
        {
            delivered = 0;
            if (isCompleted) return false;

            var req = requirements.Find(r => r.type == type);
            if (req == null || req.IsFulfilled) return false;

            int needed = req.Remaining;
            delivered = Mathf.Min(needed, amount);
            req.amountDelivered += delivered;

            UpdateProgress();
            return true;
        }

        /// <summary>
        /// Compra os recursos restantes usando o dinheiro do jogador no save.
        /// </summary>
        public bool BuyResource(BuildResourceType type, int amount, RegionSaveData save, out string feedback)
        {
            feedback = "";
            if (isCompleted)
            {
                feedback = "Obra já finalizada.";
                return false;
            }

            var req = requirements.Find(r => r.type == type);
            if (req == null || req.IsFulfilled)
            {
                feedback = "Recurso já 100% entregue.";
                return false;
            }

            int needed = req.Remaining;
            int toBuy = Mathf.Min(needed, amount);
            long cost = toBuy * BuildResourceRequirement.GetUnitPrice(type);

            if (save != null && save.currentMoney < cost)
            {
                feedback = $"Saldo insuficiente (${save.currentMoney:N0} de ${cost:N0}).";
                return false;
            }

            if (save != null)
            {
                save.currentMoney -= cost;
                save.totalExpenses += cost;
                SaveManager.Instance?.SaveToFile(save);
            }

            req.amountDelivered += toBuy;
            UpdateProgress();

            feedback = $"Comprado {toBuy}x {BuildResourceRequirement.GetName(type)} por ${cost:N0}!";
            return true;
        }

        /// <summary>
        /// Transforma o fantasma na construção sólida e finalizada!
        /// </summary>
        public void FinalizeConstruction()
        {
            if (isCompleted) return;
            isCompleted = true;

            Vector3 finalPos = transform.position;
            Quaternion finalRot = transform.rotation;
            var item = this.itemDefinition;

            // Remove o canteiro de obras fantasma
            Destroy(gameObject);

            // Spawna a versão final sólida e com física
            if (ConstructionPlacementManager.Instance != null && item != null)
            {
                ConstructionPlacementManager.Instance.SpawnStructureFinal(item, finalPos, finalRot);
                SandboxHUD.Instance?.ShowToast($"[OBRA CONCLUÍDA] {item.name} finalizado com sucesso!");
            }
        }
    }
}
