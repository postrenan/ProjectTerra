using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;

namespace ProjectTerra.Gameplay
{
    /// <summary>
    /// Gerencia o salvamento, carregamento e exclusão de partidas por região.
    /// Armazena os dados em formato JSON no diretório persistente do usuário.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private string saveDirectory;
        private Dictionary<string, RegionSaveData> loadedSaves = new Dictionary<string, RegionSaveData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
            }

            LoadAllSaves();
        }

        public void LoadAllSaves()
        {
            loadedSaves.Clear();
            if (!Directory.Exists(saveDirectory)) return;

            string[] files = Directory.GetFiles(saveDirectory, "*.json");
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var save = JsonUtility.FromJson<RegionSaveData>(json);
                    if (save != null && !string.IsNullOrEmpty(save.saveId))
                    {
                        loadedSaves[save.saveId] = save;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Erro ao carregar save {file}: {ex.Message}");
                }
            }
            if (loadedSaves.Count == 0)
            {
                SeedDemoSavesIfEmpty();
            }
            Debug.Log($"[SaveManager] Total de {loadedSaves.Count} partidas salvas carregadas.");
        }

        private void SeedDemoSavesIfEmpty()
        {
            // Partida de demonstração 1: São Paulo (ID 264)
            var saveSP1 = new RegionSaveData
            {
                saveId = "sp_demo_1",
                saveName = "Império do Café & Indústria",
                regionId = 264,
                regionName = "São Paulo",
                countryName = "Brazil",
                regionType = "State",
                lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy 14:30"),
                startingMoney = 500000,
                currentMoney = 1450000,
                forestPercent = 48,
                mineralsPercent = 35,
                arablePercent = 65,
                waterPercent = 40,
                originalForestPercent = 42,
                originalMineralsPercent = 35,
                originalArablePercent = 58,
                originalWaterPercent = 40
            };
            SaveToFile(saveSP1);
            loadedSaves[saveSP1.saveId] = saveSP1;

            var saveSP2 = new RegionSaveData
            {
                saveId = "sp_demo_2",
                saveName = "Metrópole Sustentável",
                regionId = 264,
                regionName = "São Paulo",
                countryName = "Brazil",
                regionType = "State",
                lastSavedDate = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy 09:15"),
                startingMoney = 2500000,
                currentMoney = 2890000,
                forestPercent = 55,
                mineralsPercent = 25,
                arablePercent = 58,
                waterPercent = 45,
                originalForestPercent = 42,
                originalMineralsPercent = 35,
                originalArablePercent = 58,
                originalWaterPercent = 40
            };
            SaveToFile(saveSP2);
            loadedSaves[saveSP2.saveId] = saveSP2;
        }

        public List<RegionSaveData> GetSavesForRegion(int regionId)
        {
            var list = new List<RegionSaveData>();
            foreach (var kvp in loadedSaves.Values)
            {
                if (kvp.regionId == regionId)
                {
                    list.Add(kvp);
                }
            }
            // Ordenar pelas mais recentes
            list.Sort((a, b) => string.Compare(b.lastSavedDate, a.lastSavedDate, StringComparison.Ordinal));
            return list;
        }

        public RegionSaveData CreateNewSave(RegionData region, string saveName, long startingMoney)
        {
            if (region == null) return null;

            string id = Guid.NewGuid().ToString().Substring(0, 8);
            if (string.IsNullOrWhiteSpace(saveName))
            {
                saveName = $"Partida {id}";
            }

            var save = new RegionSaveData
            {
                saveId = id,
                saveName = saveName,
                regionId = region.id,
                regionName = region.name,
                countryName = region.country,
                regionType = region.type,
                lastSavedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                startingMoney = startingMoney,
                currentMoney = startingMoney,
                forestPercent = region.forestPercent,
                mineralsPercent = region.mineralsPercent,
                arablePercent = region.arablePercent,
                waterPercent = region.waterPercent,
                originalForestPercent = region.forestPercent,
                originalMineralsPercent = region.mineralsPercent,
                originalArablePercent = region.arablePercent,
                originalWaterPercent = region.waterPercent
            };

            SaveToFile(save);
            loadedSaves[save.saveId] = save;
            Debug.Log($"[SaveManager] Nova partida criada: {save.saveName} na região {save.regionName} com verba de ${save.startingMoney:N0}");
            return save;
        }

        public void SaveToFile(RegionSaveData save)
        {
            if (save == null) return;
            string filePath = Path.Combine(saveDirectory, $"save_{save.saveId}.json");
            string json = JsonUtility.ToJson(save, true);
            File.WriteAllText(filePath, json);
        }

        public void DeleteSave(string saveId)
        {
            if (loadedSaves.ContainsKey(saveId))
            {
                loadedSaves.Remove(saveId);
            }
            string filePath = Path.Combine(saveDirectory, $"save_{saveId}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"[SaveManager] Save {saveId} excluído com sucesso.");
            }
        }
    }
}
