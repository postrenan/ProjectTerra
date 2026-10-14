using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        public enum ModHubTab { Catalog, Installed, LocalTools }
        private ModHubTab currentModHubTab = ModHubTab.Catalog;

        [System.Serializable]
        public class ModEntry
        {
            public string id;
            public string title;
            public string category;
            public string author;
            public string version;
            public float sizeMb;
            public int rating;
            public string description;
            public bool isInstalled;
            public bool isEnabled;

            // Progresso de download simulado
            public bool isDownloading;
            public float downloadProgress;
        }

        private List<ModEntry> modCatalog = new List<ModEntry>();
        private Vector2 modScrollPos = Vector2.zero;
        private string modSearchQuery = "";
        private string modCategoryFilter = "Todos";
        private readonly string[] modCategories = { "Todos", "Maquinário Agrícola", "Caminhões & Rodoviário", "Aviação & Aeronaves", "Mapas & Cenários", "Sistemas & Scripts" };

        private void InitModHub()
        {
            modCatalog.Clear();
        }


        private void UpdateModHubDownloads()
        {
            for (int i = 0; i < modCatalog.Count; i++)
            {
                var mod = modCatalog[i];
                if (mod.isDownloading)
                {
                    mod.downloadProgress += Time.deltaTime * 0.7f;
                    if (mod.downloadProgress >= 1.0f)
                    {
                        mod.downloadProgress = 1.0f;
                        mod.isDownloading = false;
                        mod.isInstalled = true;
                        mod.isEnabled = true;
                        ShowNotification($"Mod '{mod.title}' instalado com sucesso!");
                    }
                }
            }
        }

        private void DrawModHubScreen()
        {
            DrawModalHeader("MODHUB - CENTRAL DE MODIFICAÇÕES DA COMUNIDADE", "Baixe veículos oficiais, implementos agrícolas, aviões e ferramentas desenvolvidas pela comunidade");

            // Barra de abas
            GUILayout.BeginHorizontal();
            if (IconBtn(TerraIcon.Satellite, "Catálogo da Comunidade (Online)", currentModHubTab == ModHubTab.Catalog ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentModHubTab = ModHubTab.Catalog;
            }
            if (IconBtn(TerraIcon.CargoBox, $"Mods Instalados ({GetInstalledModCount()})", currentModHubTab == ModHubTab.Installed ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentModHubTab = ModHubTab.Installed;
            }
            if (IconBtn(TerraIcon.Tools, "Gerenciador de Arquivos & Pasta Local", currentModHubTab == ModHubTab.LocalTools ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentModHubTab = ModHubTab.LocalTools;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            switch (currentModHubTab)
            {
                case ModHubTab.Catalog:
                    DrawModCatalog();
                    break;
                case ModHubTab.Installed:
                    DrawInstalledMods();
                    break;
                case ModHubTab.LocalTools:
                    DrawLocalModTools();
                    break;
            }
        }

        private int GetInstalledModCount()
        {
            int count = 0;
            for (int i = 0; i < modCatalog.Count; i++)
            {
                if (modCatalog[i].isInstalled) count++;
            }
            return count;
        }

        private void DrawModCatalog()
        {
            // Barra de Filtro e Pesquisa
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pesquisar:", labelBoldStyle, GUILayout.Width(70));
            modSearchQuery = GUILayout.TextField(modSearchQuery, 30, modernTextFieldStyle, GUILayout.Height(24));
            if (!string.IsNullOrEmpty(modSearchQuery))
            {
                if (IconBtn(TerraIcon.CancelCross, "", actionBtnStyle, GUILayout.Width(24), GUILayout.Height(24)))
                {
                    modSearchQuery = "";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Filtro de Categorias
            GUILayout.BeginHorizontal();
            for (int c = 0; c < modCategories.Length; c++)
            {
                string cat = modCategories[c];
                bool isSelected = modCategoryFilter == cat;
                GUI.color = isSelected ? new Color(0.25f, 0.65f, 1f) : Color.white;
                if (GUILayout.Button(cat, isSelected ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(22)))
                {
                    modCategoryFilter = cat;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Lista de Mods
            modScrollPos = GUILayout.BeginScrollView(modScrollPos);

            for (int i = 0; i < modCatalog.Count; i++)
            {
                var mod = modCatalog[i];

                // Filtro de categoria
                if (modCategoryFilter != "Todos" && mod.category != modCategoryFilter) continue;

                // Filtro de texto
                if (!string.IsNullOrEmpty(modSearchQuery))
                {
                    if (!mod.title.ToLower().Contains(modSearchQuery.ToLower()) &&
                        !mod.description.ToLower().Contains(modSearchQuery.ToLower()) &&
                        !mod.author.ToLower().Contains(modSearchQuery.ToLower()))
                    {
                        continue;
                    }
                }

                GUI.color = new Color(0.04f, 0.10f, 0.20f, 0.90f);
                GUILayout.BeginVertical(cardStyle);
                GUI.color = Color.white;

                // Título e Estrelas
                GUILayout.BeginHorizontal();
                GUILayout.Label(mod.title, labelBoldStyle);
                GUILayout.FlexibleSpace();
                string stars = new string('*', mod.rating) + new string('-', 5 - mod.rating);
                GUI.color = new Color(1f, 0.9f, 0.35f);
                GUILayout.Label(stars, labelBoldStyle);
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                // Autor, Categoria e Tamanho
                GUILayout.Label($"Categoria: {mod.category} - Autor: {mod.author} - Versão: {mod.version} - {mod.sizeMb:F1} MB", menuBtnSubtextStyle);
                GUILayout.Space(2);

                // Descrição
                GUILayout.Label(mod.description, labelRegularStyle);
                GUILayout.Space(6);

                // Ações do Mod
                GUILayout.BeginHorizontal();
                if (mod.isDownloading)
                {
                    GUILayout.Label($"Baixando... {(int)(mod.downloadProgress * 100)}%", labelBoldStyle);
                    Rect dRect = GUILayoutUtility.GetRect(160, 18);
                    GUI.color = new Color(0.1f, 0.2f, 0.3f);
                    GUI.DrawTexture(dRect, whiteTex);
                    GUI.color = new Color(0.3f, 0.85f, 1.0f);
                    GUI.DrawTexture(new Rect(dRect.x, dRect.y, dRect.width * mod.downloadProgress, dRect.height), whiteTex);
                    GUI.color = Color.white;
                }
                else if (mod.isInstalled)
                {
                    GUI.color = new Color(0.3f, 1.0f, 0.5f);
                    IconLbl(TerraIcon.Checkmark, "Instalado no Jogo", labelBoldStyle);
                    GUI.color = Color.white;

                    GUILayout.FlexibleSpace();
                    bool newState = GUILayout.Toggle(mod.isEnabled, mod.isEnabled ? " Ativo" : " Desativado");
                    if (newState != mod.isEnabled)
                    {
                        mod.isEnabled = newState;
                        ShowNotification($"Mod '{mod.title}' {(mod.isEnabled ? "ativado" : "desativado")}.");
                    }
                }
                else
                {
                    GUILayout.FlexibleSpace();
                    GUI.color = new Color(0.2f, 0.85f, 0.45f);
                    if (IconBtn(TerraIcon.TrendingDown, "Baixar & Instalar Mod", primaryActionBtnStyle, GUILayout.Width(180), GUILayout.Height(26)))
                    {
                        mod.isDownloading = true;
                        mod.downloadProgress = 0.05f;
                    }
                    GUI.color = Color.white;
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(6);
            }

            if (modCatalog.Count == 0)
            {
                GUILayout.Space(40);
                GUILayout.Label("Nenhum mod disponível no catálogo.", labelBoldStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawInstalledMods()
        {
            modScrollPos = GUILayout.BeginScrollView(modScrollPos);

            int installedFound = 0;
            for (int i = 0; i < modCatalog.Count; i++)
            {
                var mod = modCatalog[i];
                if (!mod.isInstalled) continue;
                installedFound++;

                GUI.color = new Color(0.04f, 0.10f, 0.20f, 0.90f);
                GUILayout.BeginVertical(cardStyle);
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                GUILayout.Label(mod.title, labelBoldStyle);
                GUILayout.FlexibleSpace();
                mod.isEnabled = GUILayout.Toggle(mod.isEnabled, mod.isEnabled ? " Ativado" : " Desativado");
                GUILayout.EndHorizontal();

                GUILayout.Label($"{mod.category} - {mod.author} - {mod.sizeMb:F1} MB", menuBtnSubtextStyle);
                GUILayout.Space(4);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.color = new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button("Desinstalar Mod", dangerActionBtnStyle, GUILayout.Width(130), GUILayout.Height(24)))
                {
                    mod.isInstalled = false;
                    mod.isEnabled = false;
                    ShowNotification($"Mod '{mod.title}' foi desinstalado.");
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(6);
            }

            if (installedFound == 0)
            {
                GUILayout.Space(40);
                GUILayout.Label("Você não possui nenhum mod instalado atualmente.", labelBoldStyle);
                GUILayout.Label("Acesse a aba 'Catálogo da Comunidade' para navegar pelos tratores, aviões e implementos disponíveis.", labelRegularStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawLocalModTools()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("GERENCIAMENTO DE ARQUIVOS LOCAIS DE MODS", labelBoldStyle);
            GUILayout.Space(8);

            string modsDirectory = Path.Combine(Application.persistentDataPath, "Mods");
            GUILayout.Label($"Diretório de Mods: {modsDirectory}", labelRegularStyle);
            GUILayout.Space(8);

            GUILayout.Label("Você pode instalar modificações externas e criadas por você colocando arquivos '.zip', '.mod' ou pastas no diretório de Mods do Project Terra.", menuBtnSubtextStyle);
            GUILayout.Space(14);

            GUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 0.65f, 1.0f);
            if (IconBtn(TerraIcon.Tools, "Abrir Pasta de Mods no Windows Explorer", primaryActionBtnStyle, GUILayout.Height(36)))
            {
                OpenModsDirectoryInExplorer(modsDirectory);
            }
            GUI.color = Color.white;

            GUILayout.Space(10);
            if (IconBtn(TerraIcon.FastForward, "Recarregar Mods do Disco", actionBtnStyle, GUILayout.Width(200), GUILayout.Height(36)))
            {
                InitModHub();
                ShowNotification("Mods locais recarregados do disco!");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void OpenModsDirectoryInExplorer(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                Process.Start("explorer.exe", path);
                ShowNotification("Pasta de mods aberta no Windows Explorer.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[ModHub] Falha ao abrir diretório: {ex.Message}");
                ShowNotification($"Erro ao abrir pasta: {ex.Message}");
            }
        }
    }
}
