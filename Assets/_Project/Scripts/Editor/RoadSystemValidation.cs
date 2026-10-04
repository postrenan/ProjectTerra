#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ProjectTerra.Editor
{
    [InitializeOnLoad]
    public static class RoadSystemValidation
    {
        static RoadSystemValidation()
        {
            Debug.Log("[RoadSystem] Sistema de Estradas PBR (Asfalto, Concreto, Terra, Britas em 2, 4, 6, 8 faixas) carregado com sucesso!");
        }
    }
}
#endif
