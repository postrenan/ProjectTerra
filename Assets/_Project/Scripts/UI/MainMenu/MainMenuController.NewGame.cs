using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Planet;
using ProjectTerra.Gameplay;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        private enum NewGameTab { FeaturedRegions, GlobeSelect }
        private NewGameTab currentNewGameTab = NewGameTab.FeaturedRegions;

        // Dados do Formulário
        private string campaignName = "Nova Partida";
        private StarterCareer initialCareer = StarterCareer.Farmer;
        private GameDifficulty selectedDifficulty = GameDifficulty.Balanced;
        private long startingBudget = 500000;
        private int selectedFeaturedIndex = 0;
        private Vector2 newGameScrollPos = Vector2.zero;

        private struct FeaturedRegion
        {
            public int regionId;
            public string name;
            public string country;
            public float lat;
            public float lon;
            public string climate;
            public string vocation;
            public string flag;
        }

        private readonly FeaturedRegion[] featuredRegions = new FeaturedRegion[]
        {
            new FeaturedRegion {
                regionId = 35,
                name = "São Paulo",
                country = "Brasil",
                lat = -23.55f,
                lon = -46.63f,
                climate = "Tropical de Altitude - Sol e Chuvas Sazonais",
                vocation = "Potência Agroindustrial - Grãos, Cana-de-Açúcar, Portos e Rotas de Carga",
                flag = ""
            },
            new FeaturedRegion {
                regionId = 20,
                name = "Kansas (Great Plains)",
                country = "Estados Unidos",
                lat = 38.50f,
                lon = -98.00f,
                climate = "Continental Temperado - Planícies Abertas",
                vocation = "Cinturão dos Grãos (Wheat & Corn Belt) - Pistas de Aviação e Rodovias",
                flag = ""
            },
            new FeaturedRegion {
                regionId = 9,
                name = "Baviera (Bayern)",
                country = "Alemanha",
                lat = 48.79f,
                lon = 11.49f,
                climate = "Temperado Centro-Europeu - Vales Fluviais e Alpes",
                vocation = "Silvicultura de Alta Precisão, Tratores Avançados e Manufatura Pesada",
                flag = ""
            },
            new FeaturedRegion {
                regionId = 47,
                name = "Hokkaido",
                country = "Japão",
                lat = 43.06f,
                lon = 141.35f,
                climate = "Boreal Úmido - Neve Invernal e Verões Férteis",
                vocation = "Agricultura Tecnológica, Pecuária Leiteira e Portos Pesqueiros",
                flag = ""
            },
            new FeaturedRegion {
                regionId = 52,
                name = "Queensland",
                country = "Austrália",
                lat = -20.91f,
                lon = 142.70f,
                climate = "Subtropical e Savana - Amplas Pastagens",
                vocation = "Mega Fazendas de Pecuária, Trenhões Rodoviários (Road Trains) e Mineração",
                flag = ""
            }
        };

        private readonly long[] BudgetOptions = { 100000, 250000, 500000, 1000000, 2500000, 5000000 };
        private readonly string[] BudgetLabels = { "$100k (Desafio)", "$250k (Realista)", "$500k (Padrão)", "$1M (Avançado)", "$2.5M (Empresário)", "$5M (Magnata)" };

        private void DrawNewGameScreen()
        {
            DrawModalHeader("CONFIGURAÇÃO DE NOVA EXPEDIÇÃO", "Personalize seu ponto de partida no globo terrestre e defina seus recursos iniciais");

            // Barra de abas
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Regiões em Destaque (Partida Rápida)", currentNewGameTab == NewGameTab.FeaturedRegions ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentNewGameTab = NewGameTab.FeaturedRegions;
            }
            if (GUILayout.Button("Explorar Globo 3D e Selecionar no Planeta", currentNewGameTab == NewGameTab.GlobeSelect ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentNewGameTab = NewGameTab.GlobeSelect;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            if (currentNewGameTab == NewGameTab.GlobeSelect)
            {
                DrawGlobeSelectInfo();
                return;
            }

            // Scroll de configuração
            newGameScrollPos = GUILayout.BeginScrollView(newGameScrollPos);

            // 1. NOME DA PARTIDA
            GUILayout.Label("1. Nome da Partida / Expedição:", sectionHeaderStyle);
            campaignName = GUILayout.TextField(campaignName, 40, modernTextFieldStyle, GUILayout.Height(28));
            GUILayout.Space(10);

            // 2. SELEÇÃO DA REGIÃO EM DESTAQUE
            GUILayout.Label("2. Escolha a Região de Início no Planeta:", sectionHeaderStyle);
            for (int i = 0; i < featuredRegions.Length; i++)
            {
                var reg = featuredRegions[i];
                bool isSelected = selectedFeaturedIndex == i;

                GUI.color = isSelected ? new Color(0.12f, 0.32f, 0.55f, 0.95f) : new Color(0.04f, 0.08f, 0.16f, 0.85f);
                GUILayout.BeginVertical(cardStyle);
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{reg.name} - {reg.country}", isSelected ? labelBoldStyle : labelRegularStyle);
                GUILayout.FlexibleSpace();

                if (isSelected)
                {
                    GUI.color = new Color(0.4f, 1.0f, 0.5f);
                    IconLbl(TerraIcon.Checkmark, "SELECIONADA", labelBoldStyle);
                    GUI.color = Color.white;
                }
                else
                {
                    if (GUILayout.Button("Selecionar", actionBtnStyle, GUILayout.Width(90), GUILayout.Height(22)))
                    {
                        selectedFeaturedIndex = i;
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                GUILayout.Label($"Clima: {reg.climate}", labelRegularStyle);
                GUILayout.Label($"Vocação: {reg.vocation}", menuBtnSubtextStyle);

                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.Space(10);

            // 3. ESCOLHA DA CARREIRA INICIAL
            GUILayout.Label("3. Carreira & Atividade Inicial:", sectionHeaderStyle);
            GUILayout.BeginHorizontal();

            DrawCareerButton("Agricultor", StarterCareer.Farmer, "Cultivo, plantio, colheita e operação de maquinário pesado agrícola.");
            DrawCareerButton("Logística", StarterCareer.Trucker, "Fretes rodoviários, transporte pesado de insumos e rotas intermunicipais.");
            DrawCareerButton("Aviador", StarterCareer.Aviator, "Pulverização aérea, aviação regional e voos executivos.");
            DrawCareerButton("Náutica", StarterCareer.Fisherman, "Operações portuárias, pesca costeira e logística de carga naval.");

            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            // 4. ORÇAMENTO INICIAL
            GUILayout.Label("4. Recursos Financeiros Iniciais:", sectionHeaderStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Saldo de Investimento:", labelRegularStyle);
            GUILayout.Label($"${startingBudget:N0}", valueHighlightStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (int b = 0; b < BudgetOptions.Length; b++)
            {
                bool isSelected = startingBudget == BudgetOptions[b];
                GUI.color = isSelected ? new Color(1.0f, 0.90f, 0.35f) : Color.white;
                if (GUILayout.Button(BudgetLabels[b], actionBtnStyle, GUILayout.Height(24)))
                {
                    startingBudget = BudgetOptions[b];
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            // 5. DIFICULDADE DA CAMPANHA
            GUILayout.Label("5. Dificuldade da Campanha:", sectionHeaderStyle);
            GUILayout.BeginHorizontal();
            DrawDifficultyButton("Relaxado", GameDifficulty.Relaxed);
            DrawDifficultyButton("Equilibrado", GameDifficulty.Balanced);
            DrawDifficultyButton("Hardcore", GameDifficulty.Hardcore);
            GUILayout.EndHorizontal();
            GUILayout.Label(DifficultySettingsCatalog.Get(selectedDifficulty).description, menuBtnSubtextStyle);

            GUILayout.Space(16);

            // 6. BOTÃO PRINCIPAL DE INÍCIO
            GUI.color = new Color(0.18f, 0.85f, 0.40f);
            if (IconBtn(TerraIcon.Airplane, "INICIAR PARTIDA & DESEMBARCAR NA TERRA", primaryActionBtnStyle, GUILayout.Height(44)))
            {
                StartNewFeaturedGame();
            }
            GUI.color = Color.white;

            GUILayout.EndScrollView();
        }

        private void DrawDifficultyButton(string label, GameDifficulty difficulty)
        {
            bool isCurrent = selectedDifficulty == difficulty;
            GUI.color = isCurrent ? new Color(1.0f, 0.65f, 0.25f) : Color.white;
            if (GUILayout.Button(label, isCurrent ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(30)))
            {
                selectedDifficulty = difficulty;
            }
            GUI.color = Color.white;
        }

        private void DrawCareerButton(string label, StarterCareer career, string tooltip)
        {
            bool isCurrent = initialCareer == career;
            GUI.color = isCurrent ? new Color(0.25f, 0.65f, 1.0f) : Color.white;
            if (IconBtn(CareerIcon(career), label, isCurrent ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(30)))
            {
                initialCareer = career;
            }
            GUI.color = Color.white;
        }

        private void DrawGlobeSelectInfo()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("MODO DE SELEÇÃO LIVRE NO GLOBO 3D", labelBoldStyle);
            GUILayout.Space(4);
            GUILayout.Label("Você pode fechar o menu e navegar livremente pelo planeta Terra em 3D.\n" +
                            "- Arraste o mouse para girar o globo.\n" +
                            "- Use a roda do mouse para aproximar de 20.000 km até 50 metros.\n" +
                            "- Clique em qualquer estado, província ou país para abrir o painel territorial e fundar sua base.", labelRegularStyle);
            GUILayout.Space(12);

            GUI.color = new Color(0.20f, 0.60f, 1.0f);
            if (IconBtn(TerraIcon.Compass, "IR PARA O GLOBO 3D AGORA", primaryActionBtnStyle, GUILayout.Height(38)))
            {
                ExploreGlobeFreely();
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
        }

        private void StartNewFeaturedGame()
        {
            var featured = featuredRegions[selectedFeaturedIndex];

            // Localiza ou sintetiza a região
            RegionData region = null;
            if (PlanetInteractionController.Instance != null)
            {
                region = PlanetInteractionController.Instance.GetRegionById(featured.regionId);
            }

            if (region == null)
            {
                region = new RegionData
                {
                    id = featured.regionId,
                    name = featured.name,
                    country = featured.country,
                    centerLat = featured.lat,
                    centerLon = featured.lon,
                    type = "Território Continental",
                    forestPercent = 25,
                    mineralsPercent = 20,
                    arablePercent = 45,
                    waterPercent = 10
                };
            }

            if (string.IsNullOrEmpty(campaignName))
            {
                campaignName = $"Partida {region.name}";
            }

            RegionSaveData save = null;
            if (SaveManager.Instance != null)
            {
                save = SaveManager.Instance.CreateNewSave(region, campaignName, startingBudget, initialCareer, selectedDifficulty);
            }

            isMenuOpen = false;
            currentScreen = MenuScreen.None;

            if (PlanetInteractionController.Instance != null && save != null)
            {
                PlanetInteractionController.Instance.LoadSaveGame(save);
            }
        }
    }
}
