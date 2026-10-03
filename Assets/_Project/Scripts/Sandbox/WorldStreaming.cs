using UnityEngine;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Categorias de objeto do mundo, cada uma com uma distância de render própria.
    /// Árvores e estruturas são visíveis de longe; rochas no meio; o resto (animais,
    /// folhagem) só de perto — para evitar sobrecarga no terreno 1:1 de dezenas de km.
    /// </summary>
    public enum WorldObjectCategory
    {
        Structure,  // prédios, casas, galpões, silos, estradas  → longe
        Tree,       // árvores                                    → longe
        Rock,       // rochas/formações                          → médio
        Animal,     // fauna                                     → perto
        Foliage     // arbustos/moitas                           → perto
    }

    /// <summary>
    /// Culling por distância usando o recurso nativo <see cref="Camera.layerCullDistances"/>
    /// (custo zero por frame — o próprio engine descarta os objetos além da distância da camada).
    /// Cada categoria é mapeada para uma layer dedicada (índices 8..10, livres por padrão) e
    /// cada layer recebe uma distância máxima de render na câmera.
    /// </summary>
    public static class WorldStreaming
    {
        // Layers por índice (8..31 são livres; não precisam de nome no TagManager para uso por índice).
        public const int LayerFar = 8;   // estruturas e árvores
        public const int LayerMid = 9;   // rochas
        public const int LayerNear = 10; // animais e folhagem

        // Distâncias de render por tipo (metros).
        public const float DistFar = 1400f;  // árvores e estruturas
        public const float DistMid = 600f;   // rochas
        public const float DistNear = 280f;  // animais e folhagem

        public static int LayerFor(WorldObjectCategory category)
        {
            switch (category)
            {
                case WorldObjectCategory.Structure:
                case WorldObjectCategory.Tree:
                    return LayerFar;
                case WorldObjectCategory.Rock:
                    return LayerMid;
                default:
                    return LayerNear;
            }
        }

        /// <summary>Define a layer de um objeto e de toda a sua hierarquia filha.</summary>
        public static void SetLayerRecursive(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        /// <summary>Aplica a layer correspondente à categoria em toda a hierarquia do objeto.</summary>
        public static void Apply(GameObject go, WorldObjectCategory category)
        {
            SetLayerRecursive(go, LayerFor(category));
        }

        /// <summary>
        /// Configura as distâncias de culling por layer na câmera. Usa culling esférico
        /// (distância radial real) em vez do planar padrão.
        /// </summary>
        public static void ConfigureCamera(Camera cam)
        {
            if (cam == null) return;
            float[] dist = cam.layerCullDistances;
            if (dist == null || dist.Length != 32) dist = new float[32];

            dist[LayerFar] = DistFar;
            dist[LayerMid] = DistMid;
            dist[LayerNear] = DistNear;

            cam.layerCullDistances = dist;
            cam.layerCullSpherical = true;
        }
    }

    /// <summary>
    /// Garante que a câmera ativa do sandbox esteja sempre com as distâncias de culling configuradas,
    /// mesmo que a câmera seja criada depois (pelo PlayerCharacterController) ou recriada.
    /// </summary>
    public class WorldCullingManager : MonoBehaviour
    {
        private Camera configured;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null && cam != configured)
            {
                WorldStreaming.ConfigureCamera(cam);
                configured = cam;
                Debug.Log($"[WorldStreaming] Culling por distância configurado na câmera '{cam.name}': " +
                          $"estruturas/árvores até {WorldStreaming.DistFar:F0}m, rochas {WorldStreaming.DistMid:F0}m, animais/folhagem {WorldStreaming.DistNear:F0}m.");
            }
        }
    }
}
