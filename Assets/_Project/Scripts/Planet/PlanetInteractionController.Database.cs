using System;
using System.IO;
using UnityEngine;

namespace ProjectTerra.Planet
{
    public partial class PlanetInteractionController
    {
        private void LoadDatabase()
        {
            string binDbPath = Path.Combine(Application.streamingAssetsPath, "regions_database.bin");
            string jsonDbPath = Path.Combine(Application.streamingAssetsPath, "regions_database.json");
            string binPath = Path.Combine(Application.streamingAssetsPath, "region_id_map.bin");

            if (File.Exists(binDbPath))
            {
                try
                {
                    database = RegionDatabase.LoadFromBinary(binDbPath);
                    isDatabaseLoaded = database != null && database.regions.Count > 0;
                    Debug.Log($"[PlanetInteraction] Base de dados binária carregada com {database.regions.Count} regiões ({new FileInfo(binDbPath).Length / 1024} KB).");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar regions_database.bin: {ex.Message}");
                }
            }
            else if (File.Exists(jsonDbPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonDbPath);
                    database = JsonUtility.FromJson<RegionDatabase>(json);
                    isDatabaseLoaded = database != null && database.regions.Count > 0;
                    Debug.Log($"[PlanetInteraction] Base de dados legada (JSON) carregada com {database.regions.Count} regiões.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar regions_database.json: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlanetInteraction] Arquivo de regiões não encontrado em {binDbPath} nem {jsonDbPath}");
            }

            if (File.Exists(binPath))
            {
                try
                {
                    regionIdMap = File.ReadAllBytes(binPath);
                    Debug.Log($"[PlanetInteraction] Mapa binário de IDs geográficos carregado ({regionIdMap.Length} bytes).");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlanetInteraction] Erro ao carregar region_id_map.bin: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlanetInteraction] Arquivo region_id_map.bin não encontrado em {binPath}");
            }

            // Inicializa a base de dados de infraestrutura e viabilidade geográfica
            RegionInfrastructureDatabase.EnsureLoaded();
        }
    }
}
