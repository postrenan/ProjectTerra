using System.IO;
using UnityEngine;
using UnityEditor;

namespace ProjectTerra.Editor
{
    /// <summary>
    /// Utilitário de Editor para configurar a importação dos NPCs humanoides e ferramentas agrícolas.
    /// Define o Avatar como Humanoid compatível com a Universal Animation Library (UAL).
    /// Menus:
    /// - Tools -> Characters -> Setup NPC Humanoids
    /// - Tools -> Characters -> Setup Populate Humanoids
    /// - Tools -> Props -> Setup Farm Tools
    /// </summary>
    public static class NpcImportSetup
    {
        private const string NPCS_DIR = "Assets/_Project/Models/Characters/NPCs";
        private const string POPULATE_MODELS_DIR = "Assets/_Project/Models/Characters/Populate";
        private const string POPULATE_RES_DIR = "Assets/_Project/Resources/Models/Characters/Populate";
        private const string FARM_TOOLS_DIR = "Assets/_Project/Models/Props/FarmTools";
        private const string FARM_TOOLS_RES_DIR = "Assets/_Project/Resources/Models/Props/FarmTools";

        [MenuItem("Tools/Characters/Setup NPC Humanoids")]
        public static void SetupAllNpcHumanoids()
        {
            Debug.Log("[NpcImportSetup] Iniciando configuração de importação dos NPCs humanoides...");
            ConfigureHumanoidFolder(NPCS_DIR);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Characters/Setup Populate Humanoids")]
        public static void SetupAllPopulateNpcs()
        {
            Debug.Log("[NpcImportSetup] Iniciando configuração de importação dos modelos Populate...");
            int count1 = ConfigureHumanoidFolder(POPULATE_MODELS_DIR);
            int count2 = ConfigureHumanoidFolder(POPULATE_RES_DIR);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NpcImportSetup] Modelos Populate configurados: {count1 + count2} modelos processados.");
        }

        [MenuItem("Tools/Props/Setup Farm Tools")]
        public static void SetupFarmTools()
        {
            Debug.Log("[NpcImportSetup] Configurando modelos de ferramentas agrícolas (Farm Tools)...");
            int c1 = ConfigurePropFolder(FARM_TOOLS_DIR);
            int c2 = ConfigurePropFolder(FARM_TOOLS_RES_DIR);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NpcImportSetup] Ferramentas agrícolas configuradas: {c1 + c2} modelos processados.");
        }

        private static int ConfigureHumanoidFolder(string dir)
        {
            if (!Directory.Exists(dir)) return 0;

            string[] fbxFiles = Directory.GetFiles(dir, "*.fbx");
            int configuredCount = 0;

            foreach (string file in fbxFiles)
            {
                // Benches/seats não são humanoides
                if (file.Contains("Seat")) continue;

                string assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null) continue;

                importer.animationType = ModelImporterAnimationType.Generic; // Modelos de pedestres civis
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

                importer.SaveAndReimport();
                configuredCount++;
            }
            return configuredCount;
        }

        private static int ConfigurePropFolder(string dir)
        {
            if (!Directory.Exists(dir)) return 0;

            string[] fbxFiles = Directory.GetFiles(dir, "*.fbx");
            int configuredCount = 0;

            foreach (string file in fbxFiles)
            {
                string assetPath = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null) continue;

                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.addCollider = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

                importer.SaveAndReimport();
                configuredCount++;
            }
            return configuredCount;
        }
    }
}
