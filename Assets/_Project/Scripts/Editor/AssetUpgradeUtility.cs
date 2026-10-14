#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectTerra.Editor
{
    /// <summary>
    /// Utilitário de Editor para atualizar e re-serializar assets legados da pasta RealisticForest
    /// (Texture2DArray v1 -> v2, Mesh v9 -> v10) para o formato suportado pelo Unity 6.
    /// </summary>
    [InitializeOnLoad]
    public static class AssetUpgradeUtility
    {
        private const string PrefKey = "ProjectTerra_UpgradedNatureAssets_v6_done";

        static AssetUpgradeUtility()
        {
            EditorApplication.delayCall += CheckAndUpgradeAssets;
        }

        private static void CheckAndUpgradeAssets()
        {
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            EditorPrefs.SetBool(PrefKey, true);
            UpgradeNatureAssets();
        }

        [MenuItem("Tools/Project Terra/Re-save and Upgrade Nature Assets")]
        public static void UpgradeNatureAssets()
        {
            string folder = "Assets/_Project/Models/Nature/RealisticForest";
            if (!Directory.Exists(folder)) return;

            string[] guids = AssetDatabase.FindAssets("t:Object", new[] { folder });
            var paths = new List<string>();
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(p);
                }
            }

            if (paths.Count > 0)
            {
                AssetDatabase.ForceReserializeAssets(paths, ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
                AssetDatabase.SaveAssets();
                Debug.Log($"[AssetUpgrade] {paths.Count} assets da floresta/coníferas foram re-salvos e atualizados para o Unity 6 com sucesso!");
            }
        }
    }
}
#endif
