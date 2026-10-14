using System.IO;
using UnityEngine;
using UnityEditor;

namespace ProjectTerra.Editor
{
    /// <summary>
    /// Utilitário para re-salvar assets legados (como meshes com serializedVersion 9)
    /// para a versão suportada pelo Unity atual (serializedVersion 10+).
    /// </summary>
    [InitializeOnLoad]
    public static class AssetVersionUpgrader
    {
        private const string ConifersMeshDir = "Assets/_Project/Models/Nature/RealisticForest/Conifers/Sources/Meshes";

        static AssetVersionUpgrader()
        {
            EditorApplication.delayCall += UpgradeLegacyMeshes;
        }

        [MenuItem("Tools/Assets/Upgrade Legacy Mesh Assets")]
        public static void UpgradeLegacyMeshes()
        {
            if (!Directory.Exists(ConifersMeshDir)) return;

            string[] assetFiles = Directory.GetFiles(ConifersMeshDir, "*.asset", SearchOption.AllDirectories);
            int upgradedCount = 0;

            foreach (string file in assetFiles)
            {
                string assetPath = file.Replace('\\', '/');
                try
                {
                    string content = File.ReadAllText(assetPath);
                    if (content.Contains("serializedVersion: 9"))
                    {
                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                        if (mesh != null)
                        {
                            EditorUtility.SetDirty(mesh);
                            upgradedCount++;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AssetVersionUpgrader] Erro ao verificar {assetPath}: {ex.Message}");
                }
            }

            if (upgradedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[AssetVersionUpgrader] {upgradedCount} meshes legados foram atualizados e re-salvos com sucesso.");
            }
        }
    }
}
