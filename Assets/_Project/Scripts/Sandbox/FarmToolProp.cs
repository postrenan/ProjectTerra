using UnityEngine;
using ProjectTerra.Core;

namespace ProjectTerra.Sandbox
{
    public enum FarmToolType
    {
        ShovelSquare,
        Spade,
        Pitchfork,
        Hoe,
        Rake
    }

    /// <summary>
    /// Componente acoplado às ferramentas agrícolas rústicas (Rusted Farm Tools).
    /// Gerencia tipo, nome amigável, colisores automáticos e material PBR URP.
    /// </summary>
    [SelectionBase]
    public class FarmToolProp : MonoBehaviour
    {
        [Header("Propriedades da Ferramenta")]
        public FarmToolType toolType = FarmToolType.ShovelSquare;
        public string displayName = "Ferramenta Agrícola";
        public float lengthMeters = 1.55f;

        private static Material _sharedFarmToolsMaterial;

        private void Awake()
        {
            SetupMaterial();
            SetupCollider();
        }

        /// <summary>
        /// Garante que o material URP Lit com texturas Albedo, Normal Map e AO esteja configurado.
        /// </summary>
        public void SetupMaterial()
        {
            if (_sharedFarmToolsMaterial == null)
            {
                var alb = Resources.Load<Texture2D>("Textures/Props/FarmTools/FarmTools_ALB");
                var nml = Resources.Load<Texture2D>("Textures/Props/FarmTools/FarmTools_NML");
                var ao = Resources.Load<Texture2D>("Textures/Props/FarmTools/FarmTools_AO");

                _sharedFarmToolsMaterial = URPMaterialHelper.CreateURPLitMaterial("M_FarmTools_Rusted_Runtime", Color.white, 0.28f, 0.35f);
                if (alb != null)
                {
                    if (_sharedFarmToolsMaterial.HasProperty("_BaseMap")) _sharedFarmToolsMaterial.SetTexture("_BaseMap", alb);
                    else if (_sharedFarmToolsMaterial.HasProperty("_MainTex")) _sharedFarmToolsMaterial.SetTexture("_MainTex", alb);
                }
                if (nml != null && _sharedFarmToolsMaterial.HasProperty("_BumpMap"))
                {
                    _sharedFarmToolsMaterial.SetTexture("_BumpMap", nml);
                    _sharedFarmToolsMaterial.SetFloat("_BumpScale", 1.0f);
                    _sharedFarmToolsMaterial.EnableKeyword("_NORMALMAP");
                }
                if (ao != null && _sharedFarmToolsMaterial.HasProperty("_OcclusionMap"))
                {
                    _sharedFarmToolsMaterial.SetTexture("_OcclusionMap", ao);
                    _sharedFarmToolsMaterial.SetFloat("_OcclusionStrength", 1.0f);
                }
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && _sharedFarmToolsMaterial != null)
                {
                    r.sharedMaterial = _sharedFarmToolsMaterial;
                }
            }
        }

        /// <summary>
        /// Cria colisor adaptado às dimensões da ferramenta caso não exista.
        /// </summary>
        public void SetupCollider()
        {
            var col = GetComponent<Collider>();
            if (col != null) return;

            var r = GetComponentInChildren<Renderer>();
            if (r == null) return;

            Bounds b = r.bounds;
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(b.center);
            Vector3 ls = transform.lossyScale;
            box.size = new Vector3(
                Mathf.Max(0.12f, Mathf.Abs(ls.x) > 1e-4f ? b.size.x / Mathf.Abs(ls.x) : b.size.x),
                Mathf.Max(0.12f, Mathf.Abs(ls.y) > 1e-4f ? b.size.y / Mathf.Abs(ls.y) : b.size.y),
                Mathf.Max(0.12f, Mathf.Abs(ls.z) > 1e-4f ? b.size.z / Mathf.Abs(ls.z) : b.size.z)
            );
        }

        /// <summary>
        /// Spawna uma ferramenta com orientação e posição calibradas.
        /// </summary>
        public static GameObject SpawnTool(FarmToolType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            string modelName = GetModelResourceName(type);
            string fullPath = $"Models/Props/FarmTools/{modelName}";
            var prefab = Resources.Load<GameObject>(fullPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[FarmToolProp] Modelo não encontrado em Resources: {fullPath}");
                return null;
            }

            var inst = Object.Instantiate(prefab, position, rotation, parent);
            inst.name = modelName;
            var prop = inst.AddComponent<FarmToolProp>();
            prop.toolType = type;
            prop.displayName = GetDisplayName(type);
            return inst;
        }

        public static string GetModelResourceName(FarmToolType type)
        {
            switch (type)
            {
                case FarmToolType.ShovelSquare: return "Tool_Farm_ShovelSquare";
                case FarmToolType.Spade: return "Tool_Farm_Spade";
                case FarmToolType.Pitchfork: return "Tool_Farm_Pitchfork";
                case FarmToolType.Hoe: return "Tool_Farm_Hoe";
                case FarmToolType.Rake: return "Tool_Farm_Rake";
                default: return "Tool_Farm_ShovelSquare";
            }
        }

        public static string GetDisplayName(FarmToolType type)
        {
            switch (type)
            {
                case FarmToolType.ShovelSquare: return "Pá Reta de Corte (Aço Rústico)";
                case FarmToolType.Spade: return "Pá de Bico (Aço Forjado)";
                case FarmToolType.Pitchfork: return "Garfo de Feno / Forcado";
                case FarmToolType.Hoe: return "Enxada Agrícola de Capina";
                case FarmToolType.Rake: return "Ancinho / Rastelo Agrícola";
                default: return "Ferramenta Agrícola";
            }
        }
    }
}
