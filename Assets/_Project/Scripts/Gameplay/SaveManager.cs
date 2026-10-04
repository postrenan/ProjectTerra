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

        public RegionSaveData ActiveSave { get; set; }

        private string saveDirectory;
        private Dictionary<string, RegionSaveData> loadedSaves = new Dictionary<string, RegionSaveData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
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

        private const uint SaveMagic = 0x56415350; // 'PSAV'
        private const byte SaveVersion = RegionSaveData.CurrentVersion;

        public void LoadAllSaves()
        {
            loadedSaves.Clear();
            if (!Directory.Exists(saveDirectory)) return;

            // 0. Recuperar saves que ficaram so em .tmp (crash durante uma escrita)
            foreach (var tmp in Directory.GetFiles(saveDirectory, "*.bin.tmp"))
            {
                try
                {
                    string target = tmp.Substring(0, tmp.Length - 4); // remove ".tmp"
                    if (File.Exists(target)) continue; // save definitivo ja existe, .tmp e lixo
                    File.Move(tmp, target);
                    Debug.LogWarning($"[SaveManager] Save recuperado de ficheiro temporário: {Path.GetFileName(target)}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Falha ao recuperar {tmp}: {ex.Message}");
                }
            }

            // 1. Carregar saves binários (.bin)
            string[] binFiles = Directory.GetFiles(saveDirectory, "*.bin");
            foreach (var file in binFiles)
            {
                try
                {
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new BinaryReader(fs, System.Text.Encoding.UTF8))
                    {
                        uint magic = reader.ReadUInt32();
                        if (magic == SaveMagic)
                        {
                            byte ver = reader.ReadByte();
                            var save = RegionSaveData.ReadBinary(reader, ver);
                            if (save != null && !string.IsNullOrEmpty(save.saveId))
                            {
                                loadedSaves[save.saveId] = save;
                            }
                        }
                        else
                        {
                            // Antes era um "if" silencioso: um save corrompido sumia sem
                            // deixar rasto. Agora o arquivo orfao e preservado e logado.
                            Debug.LogWarning($"[SaveManager] Assinatura inválida em '{Path.GetFileName(file)}' (magic 0x{magic:X8}). Arquivo preservado em disco; se o save era importante, restaura-lo de um backup.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Erro ao carregar save binário {file}: {ex.Message}");
                }
            }

            // 2. Migrar automaticamente quaisquer saves legados em .json
            string[] jsonFiles = Directory.GetFiles(saveDirectory, "*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var save = JsonUtility.FromJson<RegionSaveData>(json);
                    if (save != null && !string.IsNullOrEmpty(save.saveId))
                    {
                        if (!loadedSaves.ContainsKey(save.saveId))
                        {
                            loadedSaves[save.saveId] = save;
                        }
                        SaveToFile(save);
                        File.Delete(file);
                        Debug.Log($"[SaveManager] Save legado {file} migrado para formato binário (.bin).");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Erro ao migrar save legado {file}: {ex.Message}");
                }
            }

            if (loadedSaves.Count == 0)
            {
                SeedDemoSavesIfEmpty();
            }
            Debug.Log($"[SaveManager] Total de {loadedSaves.Count} partidas salvas carregadas (formato binário).");
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

        public int TotalSaveCount => loadedSaves.Count;

        public List<RegionSaveData> GetAllSaves()
        {
            var list = new List<RegionSaveData>(loadedSaves.Values);
            list.Sort(CompareSaveDates);
            return list;
        }

        public RegionSaveData GetSaveById(string saveId)
        {
            if (string.IsNullOrEmpty(saveId)) return null;
            loadedSaves.TryGetValue(saveId, out var save);
            return save;
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
            list.Sort(CompareSaveDates);
            return list;
        }

        private static int CompareSaveDates(RegionSaveData a, RegionSaveData b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            DateTime dateA, dateB;
            bool okA = DateTime.TryParse(a.lastSavedDate, out dateA);
            bool okB = DateTime.TryParse(b.lastSavedDate, out dateB);

            if (okA && okB)
            {
                return dateB.CompareTo(dateA); // Do mais recente para o mais antigo
            }
            if (okA) return -1;
            if (okB) return 1;

            return string.Compare(b.lastSavedDate, a.lastSavedDate, StringComparison.Ordinal);
        }

        public RegionSaveData CreateNewSave(RegionData region, string saveName, long startingMoney, StarterCareer career = StarterCareer.Farmer)
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
                starterCareer = career,
                reputation = 100,
                completedDeliveries = 0,
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
            ActiveSave = save;
            Debug.Log($"[SaveManager] Nova partida criada: {save.saveName} na região {save.regionName} com verba de ${save.startingMoney:N0} e carreira {save.starterCareer}");
            return save;
        }

        public void SaveToFile(RegionSaveData save)
        {
            if (save == null) return;
            string filePath = Path.Combine(saveDirectory, $"save_{save.saveId}.bin");
            string tempPath = filePath + ".tmp";

            // Escrita atomica: serializa em .tmp, forca os dados ao disco e so entao
            // substitui o save definitivo. Sem isso, um crash/ALT-F4 no meio da escrita
            // deixaria um .bin truncado que falha no magic check e e descartado em
            // silencio no LoadAllSaves -- perda permanente da partida.
            try
            {
                using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(fs, System.Text.Encoding.UTF8))
                {
                    writer.Write(SaveMagic);
                    writer.Write(SaveVersion);
                    save.WriteBinary(writer);
                    writer.Flush();
                    fs.Flush(true);
                }

                if (File.Exists(filePath))
                {
                    // File.Replace e atomico (substitui sem janela de "nenhum arquivo").
                    File.Replace(tempPath, filePath, null, true);
                }
                else
                {
                    File.Move(tempPath, filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Falha ao salvar '{save.saveId}': {ex.Message}");
                // O .tmp e preservado de proposito: o LoadAllSaves tenta recupera-lo.
            }
        }

        public void DeleteSave(string saveId)
        {
            if (loadedSaves.ContainsKey(saveId))
            {
                loadedSaves.Remove(saveId);
            }
            string binPath = Path.Combine(saveDirectory, $"save_{saveId}.bin");
            if (File.Exists(binPath))
            {
                File.Delete(binPath);
            }
            string jsonPath = Path.Combine(saveDirectory, $"save_{saveId}.json");
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
            Debug.Log($"[SaveManager] Save {saveId} excluído com sucesso.");
        }
    }
}
