using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Interface unificada e interativa de interação com as estruturas comerciais e funcionais.
    /// Abre ao pressionar [F] quando próximo a qualquer instalação compatível com a região.
    /// </summary>
    public class StructureInteractionUI : MonoBehaviour
    {
        public static StructureInteractionUI Instance { get; private set; }

        public bool IsOpen { get; private set; } = false;
        public RegionalGameStructure CurrentStructure { get; private set; }

        private Vector2 scrollPos;
        private int selectedTab = 0; // 0 = Serviços & Comércio, 1 = Veículos/Frota, 2 = Contratos & Missões

        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle cardStyle;
        private GUIStyle buttonPrimaryStyle;
        private GUIStyle buttonSecondaryStyle;
        private GUIStyle buttonDangerStyle;
        private GUIStyle labelStyle;
        private GUIStyle badgeStyle;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            // Bloquear se console ou celular estiverem abertos
            if (InGameCommandConsole.Instance != null && InGameCommandConsole.Instance.IsOpen) return;
            if (SandboxHUD.Instance != null && SandboxHUD.Instance.IsPhoneOpen) return;

            Vector3 checkPos = player.isDriving && player.currentVehicle != null
                ? player.currentVehicle.transform.position
                : player.transform.position;

            var nearest = RegionalStructuresManager.GetNearestStructure(checkPos, out float dist);

            // Se a janela estiver aberta e o jogador se afastar muito (> 45m), fecha automaticamente
            if (IsOpen)
            {
                if (CurrentStructure == null || dist > 45f || TerraInput.GetKeyDown(Key.Escape))
                {
                    Close();
                    return;
                }
            }

            // Tecla [F] para abrir ou fechar
            if (TerraInput.GetKeyDown(Key.F))
            {
                if (IsOpen)
                {
                    Close();
                }
                else if (nearest != null && dist <= nearest.interactionRadius)
                {
                    Open(nearest);
                }
            }
        }

        public void Open(RegionalGameStructure structure)
        {
            if (structure == null) return;
            CurrentStructure = structure;
            IsOpen = true;
            selectedTab = 0;
            scrollPos = Vector2.zero;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            IsOpen = false;
            CurrentStructure = null;

            if (SandboxHUD.Instance != null)
            {
                SandboxHUD.Instance.RestoreGameplayCursor();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // 1. Prompt de Interação no HUD quando próximo
            DrawNearbyPrompt();

            // 2. Janela Modal de Interação
            if (IsOpen && CurrentStructure != null)
            {
                DrawInteractionWindow();
            }
        }

        private void DrawNearbyPrompt()
        {
            if (IsOpen) return;

            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            Vector3 checkPos = player.isDriving && player.currentVehicle != null
                ? player.currentVehicle.transform.position
                : player.transform.position;

            var nearest = RegionalStructuresManager.GetNearestStructure(checkPos, out float dist);
            if (nearest == null || dist > nearest.interactionRadius) return;

            var def = nearest.Definition;
            if (def == null) return;

            float w = 420f;
            float h = 64f;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height - 180f;
            Rect rect = new Rect(x, y, w, h);

            Color bg = new Color(0.06f, 0.12f, 0.22f, 0.88f);
            Color border = def.themeColor;
            UIThemeHelper.DrawRoundedBox(rect, bg, border, radius: 10f, borderThickness: 2.0f);

            GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 10f, w - 32f, h - 20f));
            GUILayout.BeginHorizontal();

            // Ícone da profissão
            GUILayout.Label(def.iconSymbol, badgeStyle, GUILayout.Width(44f), GUILayout.Height(44f));
            GUILayout.Space(10);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("[F] ACESSAR ESTRUTURA", subtitleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(def.shortName, subtitleStyle);
            GUILayout.EndHorizontal();

            GUILayout.Label(def.title, titleStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawInteractionWindow()
        {
            var def = CurrentStructure.Definition;
            if (def == null) return;

            float w = Mathf.Min(820f, Screen.width - 60f);
            float h = Mathf.Min(620f, Screen.height - 60f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect winRect = new Rect(x, y, w, h);

            // Fundo escuro com borda na cor da profissão
            Color bg = new Color(0.04f, 0.08f, 0.14f, 0.96f);
            UIThemeHelper.DrawRoundedBox(winRect, bg, def.themeColor, radius: 12f, borderThickness: 2.5f);

            GUILayout.BeginArea(new Rect(winRect.x + 20f, winRect.y + 16f, w - 40f, h - 32f));

            // Cabeçalho
            DrawHeader(def);

            GUILayout.Space(10);

            // Barra de Abas
            DrawTabBar(def);

            GUILayout.Space(12);

            // Conteúdo da Aba Ativa
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            switch (selectedTab)
            {
                case 0:
                    DrawServicesAndTradeTab(def);
                    break;
                case 1:
                    DrawVehiclesTab(def);
                    break;
                case 2:
                    DrawContractsTab(def);
                    break;
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Rodapé com botão de fechar
            GUILayout.BeginHorizontal();
            var save = SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : null;
            if (save != null)
            {
                GUILayout.Label($"Saldo Disponível: R$ {save.currentMoney:N0}  •  Reputação: {save.reputation} pts", subtitleStyle);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Fechar [Esc / F]", buttonSecondaryStyle, GUILayout.Width(140f), GUILayout.Height(32f)))
            {
                Close();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawHeader(GameStructureDefinition def)
        {
            GUILayout.BeginHorizontal();

            // Badge com ícone
            UIThemeHelper.DrawRoundedBox(new Rect(0, 0, 48, 48), def.themeColor * 0.3f, def.themeColor, 8f, 1.5f);
            GUILayout.Label(def.iconSymbol, badgeStyle, GUILayout.Width(48f), GUILayout.Height(48f));
            GUILayout.Space(12);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(def.title.ToUpper(), titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(def.targetCareer.ToString().ToUpper(), subtitleStyle);
            GUILayout.EndHorizontal();

            GUILayout.Label(def.description, labelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawTabBar(GameStructureDefinition def)
        {
            GUILayout.BeginHorizontal();

            bool hasVehicles = (def.services & StructureServices.VehicleSales) != 0;
            bool hasContracts = (def.services & (StructureServices.LogisticsContracts | StructureServices.FlightSchool)) != 0;

            if (GUILayout.Toggle(selectedTab == 0, "  Serviços & Comércio", buttonSecondaryStyle, GUILayout.Height(32f))) selectedTab = 0;
            if (hasVehicles)
            {
                if (GUILayout.Toggle(selectedTab == 1, "  Veículos & Equipamentos", buttonSecondaryStyle, GUILayout.Height(32f))) selectedTab = 1;
            }
            if (hasContracts)
            {
                if (GUILayout.Toggle(selectedTab == 2, "  Contratos & Missões", buttonSecondaryStyle, GUILayout.Height(32f))) selectedTab = 2;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawServicesAndTradeTab(GameStructureDefinition def)
        {
            var player = PlayerCharacterController.Instance;
            var vehicle = player != null ? player.currentVehicle : null;
            var save = SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : null;

            // ==========================================
            // SEÇÃO 1: COMÉRCIO E VENDA DE CARGAS
            // ==========================================
            if ((def.services & (StructureServices.CargoSell | StructureServices.RetailSales | StructureServices.IndustrialProcessing)) != 0)
            {
                GUILayout.Label("COMÉRCIO & COMPRA DE MATÉRIA-PRIMA", sectionStyle);
                GUILayout.BeginVertical(cardStyle);

                if (vehicle != null && vehicle.cargoFillPercent > 0.1f && vehicle.cargoItemName != "Vazio")
                {
                    float currentCargoKg = (vehicle.cargoFillPercent / 100f) * vehicle.maxCargoCapacityKg;
                    long payout = CalculateCargoPayout(def, vehicle.cargoItemName, currentCargoKg);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Carga Identificada no Veículo: <b>{vehicle.cargoItemName}</b> ({currentCargoKg:N0} kg)", labelStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Cotação de Venda: <color=#3df285>R$ {payout:N0}</color>", labelStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    if (GUILayout.Button($"Descarregar e Vender Safra/Carga (+R$ {payout:N0})", buttonPrimaryStyle, GUILayout.Height(36f)))
                    {
                        vehicle.UnloadCargo();
                        if (save != null)
                        {
                            save.currentMoney += payout;
                            save.reputation += 12;
                            save.completedDeliveries++;
                        }
                        SandboxHUD.Instance?.ShowToast($"Carga vendida com sucesso na {def.shortName}! +R$ {payout:N0}", 3.5f);
                    }
                }
                else
                {
                    GUILayout.Label($"Ponto de recebimento operacional. Estacione com sua carga de colheitas, toras, fardos ou pescados para vender com valor agregado.", labelStyle);
                    GUILayout.Space(4);
                    DrawDirectSellShortcuts(def, save);
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // ==========================================
            // SEÇÃO 2: ABASTECIMENTO DE COMBUSTÍVEL
            // ==========================================
            if ((def.services & StructureServices.FuelRefuel) != 0)
            {
                GUILayout.Label("POSTO DE ABASTECIMENTO", sectionStyle);
                GUILayout.BeginVertical(cardStyle);

                if (vehicle != null)
                {
                    float neededLiters = ((100f - vehicle.fuelPercent) / 100f) * vehicle.fuelTankLiters;
                    float pricePerL = save != null ? save.EffectiveFuelPrice : 6.5f;
                    long fuelCost = (long)(neededLiters * pricePerL);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Tanque do Veículo: {vehicle.fuelPercent:F0}% ({vehicle.fuelTankLiters - neededLiters:F0}/{vehicle.fuelTankLiters:F0} L)", labelStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Preço do Litro: R$ {pricePerL:F2}", labelStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    if (neededLiters > 2f)
                    {
                        if (GUILayout.Button($"Abastecer Tanque Completo (-R$ {fuelCost:N0})", buttonPrimaryStyle, GUILayout.Height(34f)))
                        {
                            if (save == null || save.currentMoney >= fuelCost)
                            {
                                if (save != null)
                                {
                                    save.currentMoney -= fuelCost;
                                    save.totalExpenses += fuelCost;
                                }
                                vehicle.Refuel(100f);
                                SandboxHUD.Instance?.ShowToast($"Tanque cheio! Abastecido {neededLiters:F0} litros.", 3.0f);
                            }
                            else
                            {
                                SandboxHUD.Instance?.ShowToast("Saldo insuficiente para abastecer!", 2.5f);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("O tanque de combustível do seu veículo já se encontra abastecido.", labelStyle);
                    }
                }
                else
                {
                    GUILayout.Label("Traga seu veículo (caminhão, trator, barco ou avião) para a bomba de combustível para abastecer.", labelStyle);
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // ==========================================
            // SEÇÃO 3: OFICINA MECÂNICA E REPARO
            // ==========================================
            if ((def.services & StructureServices.VehicleRepair) != 0)
            {
                GUILayout.Label("OFICINA & REVISÃO MECÂNICA", sectionStyle);
                GUILayout.BeginVertical(cardStyle);

                if (vehicle != null)
                {
                    float wear = 100f - vehicle.conditionPercent;
                    long repairCost = (long)(wear * 180f);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Condição Mecânica: {vehicle.conditionPercent:F0}% (Desgaste: {wear:F0}%)", labelStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"Custo da Manutenção: R$ {repairCost:N0}", labelStyle);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    if (wear > 1f)
                    {
                        if (GUILayout.Button($"Efetuar Revisão e Reparo Completo (-R$ {repairCost:N0})", buttonPrimaryStyle, GUILayout.Height(34f)))
                        {
                            if (save == null || save.currentMoney >= repairCost)
                            {
                                if (save != null)
                                {
                                    save.currentMoney -= repairCost;
                                    save.totalExpenses += repairCost;
                                }
                                vehicle.Repair(100f);
                                SandboxHUD.Instance?.ShowToast("Veículo revisado e 100% reparado!", 3.0f);
                            }
                            else
                            {
                                SandboxHUD.Instance?.ShowToast("Saldo insuficiente para a manutenção!", 2.5f);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("O veículo está em perfeito estado de funcionamento.", labelStyle);
                    }
                }
                else
                {
                    GUILayout.Label("Aproxime seu veículo da oficina para serviços de borracharia, molas ou manutenção mecânica.", labelStyle);
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // ==========================================
            // SEÇÃO 4: LOJA DE SUPRIMENTOS E INSUMOS
            // ==========================================
            if ((def.services & StructureServices.SuppliesStore) != 0)
            {
                GUILayout.Label("LOJA DE INSUMOS & SUPRIMENTOS", sectionStyle);
                GUILayout.BeginVertical(cardStyle);
                DrawSuppliesStore(def, vehicle, save);
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // ==========================================
            // SEÇÃO 5: COMÉRCIO DE ANIMAIS VIVOS
            // ==========================================
            if ((def.services & StructureServices.LivestockTrading) != 0)
            {
                GUILayout.Label("NEGOCIAÇÃO PECUÁRIA (ANIMAIS VIVOS)", sectionStyle);
                GUILayout.BeginVertical(cardStyle);
                DrawLivestockTrading(vehicle, save);
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }
        }

        private void DrawDirectSellShortcuts(GameStructureDefinition def, RegionSaveData save)
        {
            // Atalhos diretos de venda rápida de sacas e produtos
            string[] items;
            long[] prices;

            switch (def.targetCareer)
            {
                case StarterCareer.Farmer:
                    items = new[] { "Lote de Soja (1.000 kg)", "Lote de Milho (1.000 kg)", "Fardos de Feno (10 un)", "Leite Fresco (500 L)" };
                    prices = new[] { 4500L, 3800L, 2600L, 2900L };
                    break;
                case StarterCareer.Fisherman:
                    items = new[] { "Caixa de Sardinha (200 kg)", "Atum Fresco (150 kg)", "Lagosta & Frutos do Mar (50 kg)" };
                    prices = new[] { 3200L, 5400L, 8800L };
                    break;
                default:
                    items = new[] { "Lote de Sucata / Recicláveis", "Material de Construção Sobressalente" };
                    prices = new[] { 2200L, 3500L };
                    break;
            }

            GUILayout.Label("Simulação rápida de entrega avulsa de produtos:", subtitleStyle);
            for (int i = 0; i < items.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(items[i], labelStyle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Vender (+R$ {prices[i]:N0})", buttonSecondaryStyle, GUILayout.Width(180f), GUILayout.Height(26f)))
                {
                    if (save != null)
                    {
                        save.currentMoney += prices[i];
                        save.reputation += 8;
                    }
                    SandboxHUD.Instance?.ShowToast($"Venda realizada: {items[i]} (+R$ {prices[i]:N0})", 3.0f);
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawSuppliesStore(GameStructureDefinition def, VehicleController vehicle, RegionSaveData save)
        {
            (string name, long price, string cargoType, float kg)[] catalog;

            if (def.targetCareer == StarterCareer.Farmer)
            {
                catalog = new[]
                {
                    ("Sementes de Soja Certificadas (Sacos)", 2200L, "Sementes de Soja", 1000f),
                    ("Fertilizante Mineral NPK a Granel", 3100L, "Fertilizante NPK", 1500f),
                    ("Ração Concentrada Pecuária", 2600L, "Ração Pecuária", 1200f),
                    ("Calcário Agrícola (Correção de Solo)", 1800L, "Calcário Agrícola", 2000f)
                };
            }
            else if (def.targetCareer == StarterCareer.Fisherman)
            {
                catalog = new[]
                {
                    ("Gelo em Escamas para Conservação", 1200L, "Gelo em Escamas", 1000f),
                    ("Kit de Redes e Covos de Pesca", 2800L, "Apetrechos Náuticos", 400f),
                    ("Iscas Vivas e Conservas", 1500L, "Iscas Vivas", 300f),
                    ("Caixas Térmicas Industriais Náuticas", 2200L, "Caixas Térmicas", 500f)
                };
            }
            else
            {
                catalog = new[]
                {
                    ("Kit de Ferramentas Rodoviárias Pesadas", 2500L, "Ferramentas", 200f),
                    ("Peças de Desgaste Rápido e Correias", 3400L, "Peças Sobressalentes", 350f)
                };
            }

            for (int i = 0; i < catalog.Length; i++)
            {
                var item = catalog[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"• {item.name}", labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"R$ {item.price:N0}", labelStyle);
                GUILayout.Space(10);

                if (GUILayout.Button("Comprar e Carregar", buttonSecondaryStyle, GUILayout.Width(170f), GUILayout.Height(26f)))
                {
                    if (save != null && save.currentMoney < item.price)
                    {
                        SandboxHUD.Instance?.ShowToast("Saldo insuficiente para comprar este suprimento!", 2.5f);
                    }
                    else
                    {
                        if (save != null)
                        {
                            save.currentMoney -= item.price;
                            save.totalExpenses += item.price;
                        }

                        if (vehicle != null)
                        {
                            vehicle.LoadCargo(item.cargoType, item.kg);
                            SandboxHUD.Instance?.ShowToast($"Suprimento carregado no veículo: {item.cargoType} ({item.kg} kg)!", 3.0f);
                        }
                        else
                        {
                            SandboxHUD.Instance?.ShowToast($"Comprou {item.name}! Entregue na sua base.", 3.0f);
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawLivestockTrading(VehicleController vehicle, RegionSaveData save)
        {
            var animals = new (string name, long buy, long sell)[]
            {
                ("Novilho Nelore (Gado de Corte)", 3800L, 4200L),
                ("Lote de Ovelhas Laníferas (5 un)", 4500L, 5100L),
                ("Suínos Matrizes Selecionadas (3 un)", 3200L, 3700L),
                ("Cavalo de Lida Quarto de Milha", 8500L, 9500L)
            };

            for (int i = 0; i < animals.Length; i++)
            {
                var a = animals[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"• {a.name}", labelStyle);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button($"Comprar (R$ {a.buy:N0})", buttonSecondaryStyle, GUILayout.Width(160f), GUILayout.Height(26f)))
                {
                    if (save != null && save.currentMoney >= a.buy)
                    {
                        save.currentMoney -= a.buy;
                        if (vehicle != null) vehicle.LoadCargo(a.name, 1200f);
                        SandboxHUD.Instance?.ShowToast($"Adquiriu {a.name}!", 3.0f);
                    }
                    else
                    {
                        SandboxHUD.Instance?.ShowToast("Saldo insuficiente!", 2.0f);
                    }
                }

                if (GUILayout.Button($"Vender (R$ {a.sell:N0})", buttonPrimaryStyle, GUILayout.Width(160f), GUILayout.Height(26f)))
                {
                    if (save != null)
                    {
                        save.currentMoney += a.sell;
                        save.reputation += 10;
                    }
                    if (vehicle != null) vehicle.UnloadCargo();
                    SandboxHUD.Instance?.ShowToast($"Vendeu {a.name} com lucro!", 3.0f);
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawVehiclesTab(GameStructureDefinition def)
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : null;
            GUILayout.Label("CONCESSIONÁRIA & CATÁLOGO DE MÁQUINAS", sectionStyle);

            (string name, long price, string desc, VehicleCategory cat)[] vehicleList;

            switch (def.targetCareer)
            {
                case StarterCareer.Farmer:
                    vehicleList = new[]
                    {
                        ("Trator Agrícola TerraMaster 110 cv", 120000L, "Trator robusto com tração 4x4 e engate hidráulico de arado.", VehicleCategory.Tractor),
                        ("Colheitadeira de Grãos GoldenHarvest", 380000L, "Capacidade para 8.000 litros e plataforma de soja e milho.", VehicleCategory.Tractor)
                    };
                    break;

                case StarterCareer.Trucker:
                    vehicleList = new[]
                    {
                        ("Caminhão Pesado Titan 6x4 540 cv", 280000L, "Cavalo mecânico de longa distância com alta capacidade de carga.", VehicleCategory.Truck),
                        ("Caminhão Urbano CargoExpress 260 cv", 160000L, "Distribuição rápida com baú e ótimo consumo rodoviário.", VehicleCategory.Truck)
                    };
                    break;

                case StarterCareer.Fisherman:
                    vehicleList = new[]
                    {
                        ("Traineira Oceânica MarProfundo 38 pés", 195000L, "Porão frigorífico de 5 toneladas e guincho de redes marinhas.", VehicleCategory.Boat),
                        ("Bote Costeiro Pescador Ágil", 65000L, "Navegação ágil em rios, enseadas e lagoas costeiras.", VehicleCategory.Boat)
                    };
                    break;

                case StarterCareer.Aviator:
                default:
                    vehicleList = new[]
                    {
                        ("Monomotor Utilitário SkyCourier 200", 250000L, "Aeronave de asa alta para frete aéreo e pousos em pistas rurais.", VehicleCategory.Plane),
                        ("Avião Agrícola Pulverizador AirTractor", 340000L, "Tanque hopper de 1.200 litros com barras de pulverização rápida.", VehicleCategory.Plane)
                    };
                    break;
            }

            for (int i = 0; i < vehicleList.Length; i++)
            {
                var v = vehicleList[i];
                GUILayout.BeginVertical(cardStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{v.name}</b>", labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color=#3df285>R$ {v.price:N0}</color>", labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.Label(v.desc, subtitleStyle);
                GUILayout.Space(8);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Comprar Veículo Novo", buttonPrimaryStyle, GUILayout.Width(220f), GUILayout.Height(32f)))
                {
                    if (save != null && save.currentMoney < v.price)
                    {
                        SandboxHUD.Instance?.ShowToast("Saldo insuficiente para comprar este veículo!", 3.0f);
                    }
                    else
                    {
                        if (save != null)
                        {
                            save.currentMoney -= v.price;
                            save.totalExpenses += v.price;
                        }

                        // Spawn do veículo próximo à estrutura
                        Vector3 spawnPos = CurrentStructure.transform.position + CurrentStructure.transform.forward * 18f;
                        if (RegionalSandboxManager.Instance != null)
                        {
                            spawnPos.y = RegionalSandboxManager.Instance.GetTerrainHeight(spawnPos) + 1.0f;
                        }

                        VehicleController spawned = null;
                        switch (v.cat)
                        {
                            case VehicleCategory.Tractor: spawned = VehicleBuilder.CreateTractor(spawnPos); break;
                            case VehicleCategory.Truck: spawned = VehicleBuilder.CreateTruck(spawnPos); break;
                            case VehicleCategory.Boat: spawned = VehicleBuilder.CreateBoat(spawnPos); break;
                            case VehicleCategory.Plane: spawned = VehicleBuilder.CreatePlane(spawnPos); break;
                        }

                        if (spawned != null && VehicleManager.Instance != null)
                        {
                            VehicleManager.Instance.RegisterVehicle(spawned);
                        }

                        SandboxHUD.Instance?.ShowToast($"Parabéns! {v.name} entregue no pátio da concessionária!", 4.0f);
                    }
                }

                GUILayout.Space(8);
                if (GUILayout.Button("Alugar por Dia (-R$ 3.500)", buttonSecondaryStyle, GUILayout.Width(180f), GUILayout.Height(32f)))
                {
                    if (save != null && save.currentMoney >= 3500L)
                    {
                        save.currentMoney -= 3500L;
                        SandboxHUD.Instance?.ShowToast($"Veículo {v.name} alugado por 24 horas in-game!", 3.0f);
                    }
                    else
                    {
                        SandboxHUD.Instance?.ShowToast("Saldo insuficiente para locação!", 2.5f);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(8);
            }
        }

        private void DrawContractsTab(GameStructureDefinition def)
        {
            var save = SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : null;
            GUILayout.Label("QUADRO DE CONTRATOS & MISSÕES DEDICADAS", sectionStyle);

            if (def.type == GameStructureType.FlyingClub)
            {
                GUILayout.BeginVertical(cardStyle);
                GUILayout.Label("<b>Curso e Homologação de Piloto Civil</b>", labelStyle);
                GUILayout.Label("Obtenha a carteira de habilitação aeronáutica oficial para pilotar jatos e turboélices.", subtitleStyle);
                GUILayout.Space(6);
                if (save != null && save.HasLicense(DriverLicense.Aviation))
                {
                    GUILayout.Label("✓ Licença de Piloto Concedida e Ativa nesta província.", subtitleStyle);
                }
                else
                {
                    if (GUILayout.Button("Realizar Curso de Voo e Homologar Carteira (R$ 120.000)", buttonPrimaryStyle, GUILayout.Height(34f)))
                    {
                        if (save != null && save.currentMoney >= 120000L)
                        {
                            save.currentMoney -= 120000L;
                            save.AddLicense(DriverLicense.Aviation);
                            SandboxHUD.Instance?.ShowToast("Licença de Aviação homologada com sucesso!", 3.5f);
                        }
                        else
                        {
                            SandboxHUD.Instance?.ShowToast("Saldo insuficiente para o curso de piloto!", 2.5f);
                        }
                    }
                }
                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            // Gerador de contrato especializado originado desta estrutura
            GUILayout.BeginVertical(cardStyle);
            string contractTitle;
            string contractDesc;
            long reward;

            switch (def.type)
            {
                case GameStructureType.RescueHelipadHospital:
                    contractTitle = "Resgate Aeromédico de Emergência (Medevac)";
                    contractDesc = "Transporte urgente de equipe médica e paciente crítico até o centro de trauma.";
                    reward = 48000L;
                    break;
                case GameStructureType.AgroSprayingBase:
                    contractTitle = "Contrato de Pulverização Aérea Safra 500 Ha";
                    contractDesc = "Aplicação defensiva em lavoura de soja antes das chuvas da tarde.";
                    reward = 36000L;
                    break;
                case GameStructureType.DryPortContainerTerminal:
                    contractTitle = "Transporte de Contêiner Marítimo 40 Pés";
                    contractDesc = "Frete pesado intermodal do porto seco até o entreposto regional com lacre alfandegário.";
                    reward = 42000L;
                    break;
                case GameStructureType.QuarryConstructionYard:
                    contractTitle = "Carga Pesada de Brita e Vigas para Obra Pública";
                    contractDesc = "Transporte de 24 toneladas de brita graduada para pavimentação rodoviária.";
                    reward = 28000L;
                    break;
                case GameStructureType.PassengerAirTerminal:
                    contractTitle = "Voo Executivo de Táxi Aéreo";
                    contractDesc = "Voo direto para investidores corporativos com exigência de máxima suavidade.";
                    reward = 52000L;
                    break;
                default:
                    contractTitle = $"Frete Dedicado - {def.shortName}";
                    contractDesc = $"Coleta e transporte prioritário de mercadorias da instalação para outros entrepostos.";
                    reward = 25000L;
                    break;
            }

            GUILayout.Label($"<b>{contractTitle}</b>", labelStyle);
            GUILayout.Label(contractDesc, subtitleStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Remuneração Estimada: <color=#3df285>R$ {reward:N0}</color>", labelStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Aceitar Contrato Agora", buttonPrimaryStyle, GUILayout.Width(200f), GUILayout.Height(32f)))
            {
                if (MissionManager.Instance != null)
                {
                    var c = new CommercialContract
                    {
                        title = contractTitle,
                        description = contractDesc,
                        cargoType = "Carga Dedicada",
                        cargoWeightKg = 3500f,
                        rewardMoney = reward,
                        rewardReputation = 25,
                        pickupLocation = CurrentStructure.transform.position,
                        deliveryLocation = CurrentStructure.transform.position + new Vector3(8000f, 0f, 6000f),
                        destinationName = "Entreposto Central"
                    };
                    MissionManager.Instance.currentContract = c;
                    MissionManager.Instance.hasActiveContract = true;
                    SandboxHUD.Instance?.ShowToast($"Contrato aceito: {contractTitle}!", 3.5f);
                    Close();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private long CalculateCargoPayout(GameStructureDefinition def, string cargoName, float kg)
        {
            float ratePerKg = 3.2f;

            string lower = cargoName.ToLower();
            if (lower.Contains("soja") || lower.Contains("milho") || lower.Contains("trigo"))
            {
                ratePerKg = 4.5f;
            }
            else if (lower.Contains("feno") || lower.Contains("fardo"))
            {
                ratePerKg = 3.8f;
            }
            else if (lower.Contains("madeira") || lower.Contains("tora"))
            {
                ratePerKg = 2.8f;
            }
            else if (lower.Contains("lagosta") || lower.Contains("salmão"))
            {
                ratePerKg = 18.0f; // Peixe nobre no restaurante
            }
            else if (lower.Contains("peixe") || lower.Contains("pescado") || lower.Contains("sardinha"))
            {
                ratePerKg = 6.5f;
            }
            else if (lower.Contains("leite") || lower.Contains("queijo"))
            {
                ratePerKg = 5.2f;
            }

            // Bonificação se a estrutura for especializada naquilo (ex: Restaurante para Frutos do Mar)
            if (def.type == GameStructureType.SeafoodRestaurant && (lower.Contains("peixe") || lower.Contains("lagosta")))
            {
                ratePerKg *= 2.2f;
            }
            else if (def.type == GameStructureType.GrainElevatorCoop && lower.Contains("soja"))
            {
                ratePerKg *= 1.35f;
            }

            return (long)(kg * ratePerKg);
        }

        private void InitStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.7f, 0.82f, 0.95f) }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.78f, 0.25f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.90f, 0.94f) }
            };

            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(14, 14, 12, 12),
                margin = new RectOffset(0, 0, 4, 8)
            };

            buttonPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            buttonSecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            buttonDangerStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };
        }
    }
}
