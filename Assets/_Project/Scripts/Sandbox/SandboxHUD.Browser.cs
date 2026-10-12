using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// "TerraBrowser 95": um navegador de internet retrô em tela cheia que substitui a
    /// antiga loja do celular. Cada loja é um "site"com descrição, preço, cores/atributos
    /// e uma imagem estilizada dos itens: usados, novos (econômicos), luxo, terrenos e
    /// animais. Inclui cursor de setinha clássico e um botão [X] para voltar ao jogo.
    /// </summary>
    public partial class SandboxHUD
    {
        #region Estado do navegador

        public bool IsBrowserOpen { get; private set; } = false;

        // Localização atual: "home", "store:<id>", "item:<id>:<index>".
        private string browserLoc = "home";
        // Histórico de navegação (Voltar/Avançar).
        private readonly System.Collections.Generic.List<string> browserHistory = new System.Collections.Generic.List<string>();
        private int browserHistIdx = -1;

        private Vector2 browserScroll = Vector2.zero;
        private string browserAddress = "http://www.terra.net/";
        private int browserColorIdx = 0;
        private float browserLoadAnim = 1f; // barra de progresso falsa (0..1)

        private Texture2D retroCursorTex;
        private bool browserStylesReady = false;

        // Estilos do navegador
        private GUIStyle brTitleBarStyle, brTitleTextStyle, brWinBtnStyle;
        private GUIStyle brToolBtnStyle, brAddrStyle, brBookmarkStyle;
        private GUIStyle brPageH1Style, brPageH2Style, brBodyStyle, brSmallStyle;
        private GUIStyle brPriceStyle, brBuyStyle, brLinkCardStyle, brStatusStyle, brSpecStyle;

        #endregion

        #region Dados das lojas

        private enum ShopArt { Car, Truck, Plane, Boat, Tractor, Animal, Land }

        private class ShopItem
        {
            public string name;
            public string emoji;
            public string desc;
            public long price;
            public string[] specs; // atributos extra (ano/km, solo, temperamento...)
            public Color[] colors; // cores disponíveis (opcional)
            public string[] colorNames;
            public string action; // "vehicle:<tipo>", "animal:<Species>", "land"
            public ShopArt art;
            public Color artA, artB; // paleta da imagem desenhada
        }

        private class ShopSite
        {
            public string id;
            public string title;
            public string tagline;
            public string url;
            public Color brand;
            public ShopItem[] items;
        }

        private static readonly Color[] VehiclePalette =
        {
            new Color(0.80f, 0.12f, 0.14f), new Color(0.10f, 0.28f, 0.70f),
            new Color(0.08f, 0.08f, 0.10f), new Color(0.93f, 0.93f, 0.95f),
            new Color(0.10f, 0.52f, 0.30f), new Color(0.95f, 0.78f, 0.10f),
            new Color(0.55f, 0.55f, 0.60f), new Color(0.75f, 0.40f, 0.08f),
        };
        private static readonly string[] VehicleColorNames =
        { "Vermelho", "Azul", "Preto", "Branco", "Verde", "Amarelo", "Prata", "Laranja" };

        private ShopSite[] _sites;
        private ShopSite[] Sites => _sites ?? (_sites = BuildSites());

        private ShopSite[] BuildSites()
        {
            return new[]
            {
                new ShopSite
                {
                    id = "usados", title = "Zé das Latas — Seminovos", url = "http://www.zedaslatas.net/",
                    tagline = "Rodados, revisados e no precinho! Garantia de 30 dias (ou até a esquina).",
                    brand = new Color(0.62f, 0.40f, 0.12f),
                    items = new[]
                    {
                        new ShopItem { name = "Fusca Clássico", emoji = "", price = 18500, action = "vehicle:car",
                            art = ShopArt.Car, artA = new Color(0.55f,0.70f,0.35f), artB = new Color(0.2f,0.2f,0.2f),
                            desc = "Um clássico que recusa a morrer. Motor a ar, charme de sobra e um leve cheiro de gasolina.",
                            specs = new[]{ "Ano: 1975", "️ 198.400 km", "Consumo: alto", "Estado: 'roda bem'" },
                            colors = new[]{ new Color(0.55f,0.70f,0.35f), new Color(0.75f,0.72f,0.60f), new Color(0.30f,0.40f,0.65f) },
                            colorNames = new[]{ "Verde Musgo", "Bege", "Azul Desbotado" } },
                        new ShopItem { name = "Picape Batalhada", emoji = "", price = 34900, action = "vehicle:van",
                            art = ShopArt.Truck, artA = new Color(0.45f,0.46f,0.48f), artB = new Color(0.2f,0.2f,0.22f),
                            desc = "Carroceria amassada mas fiel. Já carregou de tudo: ração, móveis e segredos.",
                            specs = new[]{ "Ano: 1998", "️ 310.000 km", "Caçamba útil", "Estado: casca-grossa" },
                            colors = new[]{ new Color(0.45f,0.46f,0.48f), new Color(0.60f,0.30f,0.20f) },
                            colorNames = new[]{ "Cinza Fosco", "Marrom Ferrugem" } },
                        new ShopItem { name = "Trator Veterano", emoji = "", price = 46000, action = "vehicle:tractor",
                            art = ShopArt.Tractor, artA = new Color(0.70f,0.55f,0.10f), artB = new Color(0.15f,0.15f,0.15f),
                            desc = "Arou mais terra que muita fazenda inteira. Lento, teimoso e eterno.",
                            specs = new[]{ "Ano: 1989", "9.200 h de uso", "Engate traseiro OK", "Fumaça inclusa" },
                            colors = new[]{ new Color(0.70f,0.55f,0.10f), new Color(0.75f,0.20f,0.15f) },
                            colorNames = new[]{ "Amarelo Lavoura", "Vermelho Clássico" } },
                        new ShopItem { name = "Caminhão Reformado", emoji = "", price = 89000, action = "vehicle:truck",
                            art = ShopArt.Truck, artA = new Color(0.20f,0.35f,0.55f), artB = new Color(0.15f,0.15f,0.18f),
                            desc = "Baú recém-pintado por cima da ferrugem. Pega estrada numa boa, se Deus quiser.",
                            specs = new[]{ "Ano: 2005", "️ 540.000 km", "Até 1.5 t", "Revisão recente" },
                            colors = new[]{ new Color(0.20f,0.35f,0.55f), new Color(0.80f,0.80f,0.82f) },
                            colorNames = new[]{ "Azul Estrada", "Branco Geladeira" } },
                    }
                },
                new ShopSite
                {
                    id = "novos", title = "AutoPopular — 0 km", url = "http://www.autopopular.net/",
                    tagline = "Veículos novos, econômicos e sem dor de cabeça. Sai de fábrica, entra na sua garagem!",
                    brand = new Color(0.18f, 0.55f, 0.35f),
                    items = new[]
                    {
                        new ShopItem { name = "Hatch Popular", emoji = "", price = 62000, action = "vehicle:car",
                            art = ShopArt.Car, artA = new Color(0.90f,0.90f,0.92f), artB = new Color(0.2f,0.2f,0.25f),
                            desc = "O queridinho das cidades. Econômico, ágil e fácil de estacionar em qualquer lugar.",
                            specs = new[]{ "Ano: 0 km", "14 km/l", "Garantia 3 anos", "Ar-condicionado" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Sedan Econômico", emoji = "", price = 84000, action = "vehicle:sedan",
                            art = ShopArt.Car, artA = new Color(0.20f,0.45f,0.85f), artB = new Color(0.15f,0.15f,0.2f),
                            desc = "Porta-malas grande e banco traseiro folgado. Conforto de família sem pesar no bolso.",
                            specs = new[]{ "Ano: 0 km", "12 km/l", "Porta-malas 500 L", "Garantia 3 anos" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Van Utilitária", emoji = "", price = 98000, action = "vehicle:van",
                            art = ShopArt.Truck, artA = new Color(0.90f,0.90f,0.92f), artB = new Color(0.2f,0.2f,0.22f),
                            desc = "Trabalho pesado do dia a dia. Espaço de carga generoso para começar o negócio.",
                            specs = new[]{ "Ano: 0 km", "Até 800 kg", "10 km/l", "Garantia 2 anos" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Trator Compacto", emoji = "", price = 120000, action = "vehicle:tractor",
                            art = ShopArt.Tractor, artA = new Color(0.20f,0.55f,0.25f), artB = new Color(0.12f,0.12f,0.12f),
                            desc = "Novinho em folha para a pequena propriedade. Pronto para o engate de implementos.",
                            specs = new[]{ "Ano: 0 km", "75 cv", "Engate traseiro", "Garantia 2 anos" },
                            colors = new[]{ new Color(0.20f,0.55f,0.25f), new Color(0.80f,0.20f,0.15f), new Color(0.10f,0.35f,0.70f) },
                            colorNames = new[]{ "Verde", "Vermelho", "Azul" } },
                    }
                },
                new ShopSite
                {
                    id = "luxo", title = "Prestige Motors — Luxo", url = "http://www.prestige-motors.net/",
                    tagline = "Para quem chegou lá. Máquinas exclusivas, acabamento impecável e muita adrenalina.",
                    brand = new Color(0.55f, 0.30f, 0.65f),
                    items = new[]
                    {
                        new ShopItem { name = "Esportivo GT", emoji = "", price = 640000, action = "vehicle:sports",
                            art = ShopArt.Car, artA = new Color(0.85f,0.10f,0.12f), artB = new Color(0.08f,0.08f,0.08f),
                            desc = "0 a 100 antes de você ler esta frase. Potência bruta e postura baixa de pista.",
                            specs = new[]{ "620 cv", "0–100: 3,2 s", "Vmáx: 320 km/h", "[Luxo] Edição limitada" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "SUV Premium 4x4", emoji = "", price = 420000, action = "vehicle:suv",
                            art = ShopArt.Car, artA = new Color(0.10f,0.10f,0.12f), artB = new Color(0.25f,0.25f,0.3f),
                            desc = "Imponência no asfalto e tração total fora dele. Couro, silêncio e comando.",
                            specs = new[]{ "400 cv", "Tração 4x4", "Interior em couro", "Off-road total" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Scania Top de Linha", emoji = "", price = 890000, action = "vehicle:scania",
                            art = ShopArt.Truck, artA = new Color(0.10f,0.20f,0.45f), artB = new Color(0.1f,0.1f,0.12f),
                            desc = "O rei das estradas. Cabine-leito, modelo PBR realista e presença de sobra.",
                            specs = new[]{ "560 cv", "Cabine-leito", "Alta capacidade", "Modelo realista" },
                            colors = null, colorNames = null },
                        new ShopItem { name = "Aeronave Executiva", emoji = "", price = 1250000, action = "vehicle:plane",
                            art = ShopArt.Plane, artA = new Color(0.92f,0.92f,0.95f), artB = new Color(0.70f,0.15f,0.15f),
                            desc = "Encurte distâncias com estilo. Decole da sua própria propriedade rumo ao horizonte.",
                            specs = new[]{ "Monomotor", "️ Longo alcance", "4 lugares", "[Luxo] Acabamento premium" },
                            colors = new[]{ new Color(0.92f,0.92f,0.95f), new Color(0.10f,0.12f,0.18f) },
                            colorNames = new[]{ "Branco Pérola", "Grafite" } },
                        new ShopItem { name = "Iate de Luxo", emoji = "", price = 980000, action = "vehicle:boat",
                            art = ShopArt.Boat, artA = new Color(0.95f,0.95f,0.97f), artB = new Color(0.10f,0.30f,0.55f),
                            desc = "Navegue os rios e a costa da região com todo o conforto de um lar flutuante.",
                            specs = new[]{ "Casco em fibra", "Cabine ampla", "Pronto para zarpar", "[Luxo] Linha premium" },
                            colors = new[]{ new Color(0.95f,0.95f,0.97f), new Color(0.10f,0.15f,0.20f) },
                            colorNames = new[]{ "Branco Náutico", "Azul Profundo" } },
                    }
                },
                new ShopSite
                {
                    id = "terrenos", title = "Terra & Cia — Imóveis Rurais", url = "http://www.terraecia.net/",
                    tagline = "Invista na terra da região! Lotes, chácaras e fazendas com escritura na hora.",
                    brand = new Color(0.40f, 0.52f, 0.20f),
                    items = new[]
                    {
                        new ShopItem { name = "Lote Urbano", emoji = "", price = 80000, action = "land",
                            art = ShopArt.Land, artA = new Color(0.55f,0.55f,0.58f), artB = new Color(0.45f,0.60f,0.30f),
                            desc = "Terreno plano à beira da cidade. Ideal para galpão, garagem ou base de operações.",
                            specs = new[]{ " Área: 600 m²", "Zona urbana", " Água e luz", "Pronto p/ construir" } },
                        new ShopItem { name = "Chácara de Lazer", emoji = "", price = 240000, action = "land",
                            art = ShopArt.Land, artA = new Color(0.40f,0.60f,0.25f), artB = new Color(0.30f,0.50f,0.70f),
                            desc = "Verde, tranquila e com nascente. O refúgio perfeito longe do barulho.",
                            specs = new[]{ " Área: 2 ha", "Pomar formado", "Nascente própria", "Açude" } },
                        new ShopItem { name = "Fazenda Produtiva", emoji = "", price = 1800000, action = "land",
                            art = ShopArt.Land, artA = new Color(0.75f,0.65f,0.30f), artB = new Color(0.45f,0.60f,0.25f),
                            desc = "Hectares de solo fértil prontos para plantio e pastagem em escala.",
                            specs = new[]{ " Área: 120 ha", "Solo fértil", "Mecanizável", "Dois rios" } },
                        new ShopItem { name = "Terreno Beira-Rio", emoji = "", price = 320000, action = "land",
                            art = ShopArt.Land, artA = new Color(0.45f,0.60f,0.28f), artB = new Color(0.20f,0.45f,0.75f),
                            desc = "Frente ampla para o rio. Perfeito para doca, pesca e turismo náutico.",
                            specs = new[]{ " Área: 5 ha", "300 m de rio", "Doca possível", "Pesca farta" } },
                        new ShopItem { name = "Área Industrial", emoji = "", price = 2600000, action = "land",
                            art = ShopArt.Land, artA = new Color(0.50f,0.50f,0.52f), artB = new Color(0.60f,0.55f,0.35f),
                            desc = "Gleba plana com acesso a rodovia. Base ideal para logística e distribuição.",
                            specs = new[]{ " Área: 30 ha", "️ Frente p/ rodovia", "Alta tensão", "Zona logística" } },
                    }
                },
                new ShopSite
                {
                    id = "animais", title = "Rancho Online — Animais", url = "http://www.ranchoonline.net/",
                    tagline = "Do pasto à matilha: compre e receba o animal direto na sua propriedade.",
                    brand = new Color(0.60f, 0.40f, 0.22f),
                    items = new[]
                    {
                        new ShopItem { name = "Vaca Leiteira", emoji = "", price = 7500, action = "animal:Cow",
                            art = ShopArt.Animal, artA = new Color(0.55f,0.35f,0.22f), artB = new Color(0.92f,0.90f,0.88f),
                            desc = "Dócil e produtiva. Pasta tranquila e dá leite farto para a propriedade.",
                            specs = new[]{ "Alta produção", "Pastadora", "Temperamento dócil", " ~550 kg" } },
                        new ShopItem { name = "Cavalo de Montaria", emoji = "", price = 15000, action = "animal:Horse",
                            art = ShopArt.Animal, artA = new Color(0.32f,0.20f,0.12f), artB = new Color(0.15f,0.10f,0.08f),
                            desc = "Forte e elegante. Companheiro ideal para o trabalho no campo e longas cavalgadas.",
                            specs = new[]{ "Veloz", "Resistente", "Treinável", " ~450 kg" } },
                        new ShopItem { name = "Ovelha", emoji = "", price = 2200, action = "animal:Sheep",
                            art = ShopArt.Animal, artA = new Color(0.92f,0.90f,0.85f), artB = new Color(0.20f,0.18f,0.18f),
                            desc = "Rústica e tranquila. Lã, carne e um gramado sempre aparado de brinde.",
                            specs = new[]{ "Produz lã", "Pastadora", "Mansa", " ~70 kg" } },
                        new ShopItem { name = "Raposa (exótica)", emoji = "", price = 9800, action = "animal:Fox",
                            art = ShopArt.Animal, artA = new Color(0.85f,0.42f,0.12f), artB = new Color(0.95f,0.95f,0.95f),
                            desc = "Esperta e arisca. Animal exótico de estimação — mantenha longe do galinheiro!",
                            specs = new[]{ "Exótica", "Muito rápida", "Arisca", " ~7 kg" } },
                        new ShopItem { name = "Lobo (guarda)", emoji = "", price = 21000, action = "animal:Wolf",
                            art = ShopArt.Animal, artA = new Color(0.35f,0.35f,0.38f), artB = new Color(0.20f,0.20f,0.22f),
                            desc = "Imponente e territorial. Só para criadores experientes — respeito garantido.",
                            specs = new[]{ "Predador", "Noturno", "[Aviso] Selvagem", " ~45 kg" } },
                    }
                },
            };
        }

        private ShopSite FindSite(string id)
        {
            foreach (var s in Sites) if (s.id == id) return s;
            return null;
        }

        #endregion

        #region Abrir / fechar / navegar

        public void OpenVehicleBrowser()
        {
            CloseOptionsMenu(); // guarda o celular, se estiver aberto
            if (IsRegionalMapOpen) CloseRegionalMap();

            IsBrowserOpen = true;
            browserHistory.Clear();
            browserHistIdx = -1;
            Navigate("home", true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false; // escondemos o cursor do SO e desenhamos a setinha retrô
        }

        public void CloseBrowser()
        {
            IsBrowserOpen = false;
            RestoreGameplayCursor();
        }

        /// <summary>Vai para uma localização, empilhando no histórico (salvo navegação por Voltar/Avançar).</summary>
        private void Navigate(string loc, bool pushHistory)
        {
            browserLoc = loc;
            browserScroll = Vector2.zero;
            browserColorIdx = 0;
            browserLoadAnim = 0f;
            browserAddress = AddressFor(loc);

            if (pushHistory)
            {
                // Descarta "avançar"pendente ao navegar para um novo destino.
                if (browserHistIdx < browserHistory.Count - 1)
                    browserHistory.RemoveRange(browserHistIdx + 1, browserHistory.Count - browserHistIdx - 1);
                browserHistory.Add(loc);
                browserHistIdx = browserHistory.Count - 1;
            }
        }

        private string AddressFor(string loc)
        {
            if (loc == "home") return "http://www.terra.net/";
            if (loc.StartsWith("store:"))
            {
                var s = FindSite(loc.Substring(6));
                return s != null ? s.url : "http://www.terra.net/";
            }
            if (loc.StartsWith("item:"))
            {
                var parts = loc.Split(':');
                var s = FindSite(parts[1]);
                if (s != null && int.TryParse(parts[2], out int idx) && idx >= 0 && idx < s.items.Length)
                    return s.url + "produto/" + idx;
            }
            return "http://www.terra.net/";
        }

        private void BrowserBack()
        {
            if (browserHistIdx > 0)
            {
                browserHistIdx--;
                Navigate(browserHistory[browserHistIdx], false);
            }
        }

        private void BrowserForward()
        {
            if (browserHistIdx < browserHistory.Count - 1)
            {
                browserHistIdx++;
                Navigate(browserHistory[browserHistIdx], false);
            }
        }

        /// <summary>Processa teclas do navegador (chamado de HandleKeyInputs). Retorna true se consumiu o frame.</summary>
        private bool HandleBrowserInput()
        {
            if (!IsBrowserOpen) return false;

            if (TerraInput.GetKeyDown(Key.Escape))
            {
                if (browserLoc != "home") BrowserBack();
                else CloseBrowser();
            }
            return true; // enquanto o navegador está aberto, bloqueia os demais atalhos do HUD
        }

        #endregion

        #region Desenho do navegador

        private void DrawBrowser()
        {
            InitBrowserStyles();

            // Mantém o cursor do SO oculto (o PlayerController pode tentar reexibi-lo).
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None;

            browserLoadAnim = Mathf.MoveTowards(browserLoadAnim, 1f, Time.unscaledDeltaTime * 2.2f);

            Rect full = new Rect(0, 0, Screen.width, Screen.height);

            // Fundo "desktop" (azul-petróleo) para o caso de a janela não cobrir tudo.
            GUI.color = new Color(0.10f, 0.32f, 0.42f, 1f);
            GUI.DrawTexture(full, whiteTex);
            GUI.color = Color.white;

            // --- Barra de título ---
            float y = 0f;
            float titleH = 28f;
            Rect titleBar = new Rect(0, y, full.width, titleH);
            GUI.color = new Color(0.05f, 0.12f, 0.45f, 1f);
            GUI.DrawTexture(titleBar, whiteTex);
            GUI.color = new Color(0.20f, 0.40f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(0, y, full.width, 2f), whiteTex);
            GUI.color = Color.white;

            string pageTitle = CurrentPageTitle();
            GUI.Label(new Rect(10, y + 3, full.width - 160, titleH - 4), $"TerraBrowser 95 — {pageTitle}", brTitleTextStyle);

            // Botões de janela (_ , □ , X). Só o X é funcional.
            float bw = 30f, bh = 20f, by = y + 4f;
            GUI.Label(new Rect(full.width - bw * 3 - 14, by, bw, bh), "_", brWinBtnStyle);
            GUI.Label(new Rect(full.width - bw * 2 - 10, by, bw, bh), "[Max]", brWinBtnStyle);
            GUI.color = new Color(0.80f, 0.15f, 0.15f, 1f);
            if (GUI.Button(new Rect(full.width - bw - 6, by, bw, bh), "[X]", brWinBtnStyle))
            {
                GUI.color = Color.white;
                CloseBrowser();
                return;
            }
            GUI.color = Color.white;

            // --- Barra de ferramentas ---
            y += titleH;
            float toolH = 34f;
            Rect toolBar = new Rect(0, y, full.width, toolH);
            GUI.color = new Color(0.82f, 0.82f, 0.80f, 1f);
            GUI.DrawTexture(toolBar, whiteTex);
            GUI.color = Color.white;

            float tx = 8f;
            bool canBack = browserHistIdx > 0;
            bool canFwd = browserHistIdx < browserHistory.Count - 1;

            GUI.enabled = canBack;
            if (GUI.Button(new Rect(tx, y + 5, 70, toolH - 10), "◄ Voltar", brToolBtnStyle)) BrowserBack();
            tx += 74;
            GUI.enabled = canFwd;
            if (GUI.Button(new Rect(tx, y + 5, 78, toolH - 10), "Avançar ►", brToolBtnStyle)) BrowserForward();
            tx += 82;
            GUI.enabled = true;
            if (GUI.Button(new Rect(tx, y + 5, 36, toolH - 10), "⟳", brToolBtnStyle)) { browserLoadAnim = 0f; Navigate(browserLoc, false); }
            tx += 40;
            if (GUI.Button(new Rect(tx, y + 5, 40, toolH - 10), "", brToolBtnStyle)) Navigate("home", true);
            tx += 48;

            // Barra de endereço
            float goW = 44f;
            float addrW = full.width - tx - goW - 16f;
            GUI.SetNextControlName("addr");
            browserAddress = GUI.TextField(new Rect(tx, y + 6, addrW, toolH - 12), browserAddress, brAddrStyle);
            if (GUI.Button(new Rect(tx + addrW + 4, y + 6, goW, toolH - 12), "Ir", brToolBtnStyle))
            {
                TryNavigateAddress(browserAddress);
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "addr")
            {
                TryNavigateAddress(browserAddress);
                Event.current.Use();
            }

            // --- Barra de favoritos (atalhos das lojas) ---
            y += toolH;
            float bmH = 28f;
            Rect bmBar = new Rect(0, y, full.width, bmH);
            GUI.color = new Color(0.90f, 0.90f, 0.88f, 1f);
            GUI.DrawTexture(bmBar, whiteTex);
            GUI.color = Color.white;

            GUI.Label(new Rect(8, y + 5, 70, bmH - 8), "Favoritos:", brSmallStyle);
            float fx = 86f;
            string[] favIds = { "usados", "novos", "luxo", "terrenos", "animais" };
            string[] favLabels = { "Usados", "[Novo] Novos", "[Luxo] Luxo", "Terrenos", "Animais" };
            for (int i = 0; i < favIds.Length; i++)
            {
                float w = brBookmarkStyle.CalcSize(new GUIContent(favLabels[i])).x + 14f;
                if (GUI.Button(new Rect(fx, y + 3, w, bmH - 6), favLabels[i], brBookmarkStyle))
                    Navigate("store:" + favIds[i], true);
                fx += w + 5;
            }

            // Saldo à direita da barra de favoritos
            var save = CurrentSave;
            string money = save != null ? save.GetFormattedMoney() : "$--";
            GUI.Label(new Rect(full.width - 220, y + 4, 212, bmH - 6), $"Saldo: {money}", brPriceStyle);

            // --- Barra de carregamento ---
            y += bmH;
            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            GUI.DrawTexture(new Rect(0, y, full.width, 2f), whiteTex);
            GUI.color = new Color(0.20f, 0.55f, 0.95f, 1f);
            GUI.DrawTexture(new Rect(0, y, full.width * Mathf.Clamp01(browserLoadAnim), 2f), whiteTex);
            GUI.color = Color.white;
            y += 2f;

            // --- Área da página ---
            float statusH = 22f;
            Rect page = new Rect(0, y, full.width, full.height - y - statusH);
            GUI.color = Color.white;
            GUI.DrawTexture(page, whiteTex); // página branca
            // Bisel interno
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(page.x, page.y, page.width, 1f), whiteTex);
            GUI.color = Color.white;

            if (browserLoc == "home") DrawPortalHome(page);
            else if (browserLoc.StartsWith("store:")) DrawStorePage(page, FindSite(browserLoc.Substring(6)));
            else if (browserLoc.StartsWith("item:")) DrawItemPage(page);

            // --- Barra de status ---
            Rect statusBar = new Rect(0, full.height - statusH, full.width, statusH);
            GUI.color = new Color(0.82f, 0.82f, 0.80f, 1f);
            GUI.DrawTexture(statusBar, whiteTex);
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(new Rect(0, statusBar.y, full.width, 1f), whiteTex);
            GUI.color = Color.white;
            string statusTxt = browserLoadAnim < 1f ? "Carregando..." : "Concluído";
            GUI.Label(new Rect(8, statusBar.y + 3, 300, statusH - 4), statusTxt, brStatusStyle);
            GUI.Label(new Rect(full.width - 260, statusBar.y + 3, 252, statusH - 4), "[Bloqueado] Conexão segura TerraNet • [Esc] volta", brStatusStyle);

            // Cursor retrô por cima de tudo.
            DrawRetroCursor();
        }

        private string CurrentPageTitle()
        {
            if (browserLoc == "home") return "Portal TerraNet";
            if (browserLoc.StartsWith("store:"))
            {
                var s = FindSite(browserLoc.Substring(6));
                return s != null ? s.title : "Loja";
            }
            if (browserLoc.StartsWith("item:"))
            {
                var it = CurrentItem();
                return it != null ? it.name : "Produto";
            }
            return "TerraNet";
        }

        private ShopItem CurrentItem()
        {
            if (!browserLoc.StartsWith("item:")) return null;
            var parts = browserLoc.Split(':');
            var s = FindSite(parts[1]);
            if (s != null && int.TryParse(parts[2], out int idx) && idx >= 0 && idx < s.items.Length)
                return s.items[idx];
            return null;
        }

        private void TryNavigateAddress(string addr)
        {
            browserLoadAnim = 0f;
            string a = (addr ?? "").ToLowerInvariant();
            foreach (var s in Sites)
            {
                if (a.Contains(s.url.ToLowerInvariant().Replace("http://", "").Replace("/", "")) ||
                    a.Contains(s.id))
                {
                    Navigate("store:" + s.id, true);
                    return;
                }
            }
            Navigate("home", true);
        }

        // ---------- Portal inicial ----------
        private void DrawPortalHome(Rect page)
        {
            GUILayout.BeginArea(new Rect(page.x + 24, page.y + 16, page.width - 48, page.height - 24));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            GUILayout.Label("TerraNet — O Portal de Compras da Região", brPageH1Style);
            GUILayout.Label("Bem-vindo! Escolha uma loja abaixo para começar. Clique nos cartões ou use os Favoritos.", brBodyStyle);
            GUILayout.Space(16);

            float cardW = 250f, cardH = 150f, gap = 18f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((page.width - 48 + gap) / (cardW + gap)));
            int i = 0;
            foreach (var s in Sites)
            {
                if (i % cols == 0) GUILayout.BeginHorizontal();

                Rect card = GUILayoutUtility.GetRect(cardW, cardH, GUILayout.Width(cardW), GUILayout.Height(cardH));
                DrawPortalCard(card, s);

                if (i % cols == cols - 1) GUILayout.EndHorizontal();
                else GUILayout.Space(gap);
                i++;
            }
            if (i % cols != 0) GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("© 1998 TerraNet Inc. — Best viewed in TerraBrowser 95 at 800x600", brSmallStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPortalCard(Rect card, ShopSite s)
        {
            bool hover = card.Contains(Event.current.mousePosition);
            GUI.color = hover ? new Color(0.96f, 0.98f, 1f) : new Color(0.93f, 0.95f, 0.98f);
            GUI.DrawTexture(card, whiteTex);
            GUI.color = s.brand;
            GUI.DrawTexture(new Rect(card.x, card.y, card.width, 30f), whiteTex);
            GUI.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            DrawRectOutline(card, 1f);
            GUI.color = Color.white;

            GUI.Label(new Rect(card.x + 8, card.y + 5, card.width - 16, 22), s.title, brPageH2Style);
            GUI.Label(new Rect(card.x + 10, card.y + 38, card.width - 20, card.height - 74), s.tagline, brBodyStyle);

            if (GUI.Button(new Rect(card.x + 10, card.y + card.height - 30, card.width - 20, 24), "Entrar na loja ►", brBuyStyle))
                Navigate("store:" + s.id, true);
        }

        // ---------- Página de uma loja ----------
        private void DrawStorePage(Rect page, ShopSite s)
        {
            if (s == null) { GUI.Label(new Rect(page.x + 20, page.y + 20, 400, 30), "404 — Página não encontrada", brPageH1Style); return; }

            // Cabeçalho colorido da loja
            Rect banner = new Rect(page.x, page.y, page.width, 56f);
            GUI.color = s.brand;
            GUI.DrawTexture(banner, whiteTex);
            GUI.color = Color.white;
            GUI.Label(new Rect(banner.x + 20, banner.y + 6, banner.width - 40, 24), s.title, brPageH1Style);
            GUI.Label(new Rect(banner.x + 20, banner.y + 32, banner.width - 40, 18), s.tagline, brSmallStyle);

            GUILayout.BeginArea(new Rect(page.x + 20, page.y + 66, page.width - 40, page.height - 74));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            for (int idx = 0; idx < s.items.Length; idx++)
            {
                Rect card = GUILayoutUtility.GetRect(page.width - 44, 120f);
                DrawStoreItemCard(card, s, idx);
                GUILayout.Space(12);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStoreItemCard(Rect card, ShopSite s, int idx)
        {
            var it = s.items[idx];
            bool hover = card.Contains(Event.current.mousePosition);

            GUI.color = hover ? new Color(0.97f, 0.98f, 1f) : new Color(0.95f, 0.96f, 0.98f);
            GUI.DrawTexture(card, whiteTex);
            GUI.color = new Color(0.55f, 0.55f, 0.6f, 1f);
            DrawRectOutline(card, 1f);
            GUI.color = Color.white;

            // Imagem à esquerda
            Rect img = new Rect(card.x + 8, card.y + 8, 150f, card.height - 16);
            DrawProductImage(img, it);

            float tx = img.xMax + 14;
            GUI.Label(new Rect(tx, card.y + 8, card.width - (tx - card.x) - 16, 24), $"{it.emoji} {it.name}", brPageH2Style);
            GUI.Label(new Rect(tx, card.y + 34, card.width - (tx - card.x) - 170, card.height - 44), it.desc, brBodyStyle);

            // Preço + botões à direita
            float rx = card.xMax - 158;
            GUI.Label(new Rect(rx, card.y + 30, 150, 26), FormatPrice(it.price), brPriceStyle);

            var save = CurrentSave;
            bool canAfford = save != null && save.currentMoney >= it.price;

            if (GUI.Button(new Rect(rx, card.y + 60, 150, 26), "Detalhes", brToolBtnStyle))
                Navigate($"item:{s.id}:{idx}", true);

            GUI.enabled = canAfford;
            if (GUI.Button(new Rect(rx, card.y + 88, 150, 26), canAfford ? "Comprar" : "Sem saldo", brBuyStyle))
                PurchaseItem(it, 0);
            GUI.enabled = true;

            // Cartão inteiro clicável (vai para detalhes), menos onde há botões — simplificamos:
            if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                new Rect(card.x, card.y, rx - card.x - 8, card.height).Contains(Event.current.mousePosition))
            {
                Navigate($"item:{s.id}:{idx}", true);
                Event.current.Use();
            }
        }

        // ---------- Página de produto (detalhe) ----------
        private void DrawItemPage(Rect page)
        {
            var parts = browserLoc.Split(':');
            var s = FindSite(parts[1]);
            var it = CurrentItem();
            if (s == null || it == null) { GUI.Label(new Rect(page.x + 20, page.y + 20, 400, 30), "404 — Produto não encontrado", brPageH1Style); return; }

            GUILayout.BeginArea(new Rect(page.x + 24, page.y + 16, page.width - 48, page.height - 24));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            if (GUILayout.Button("◄ Voltar à loja", brToolBtnStyle, GUILayout.Width(140), GUILayout.Height(24)))
                Navigate("store:" + s.id, true);
            GUILayout.Space(10);

            GUILayout.Label($"{it.emoji} {it.name}", brPageH1Style);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();

            // Imagem grande
            Rect big = GUILayoutUtility.GetRect(320f, 220f, GUILayout.Width(320), GUILayout.Height(220));
            DrawProductImage(big, it);

            GUILayout.Space(24);

            // Coluna de informações
            GUILayout.BeginVertical(GUILayout.Width(page.width - 48 - 320 - 24));
            GUILayout.Label(FormatPrice(it.price), brPriceStyle);
            GUILayout.Space(6);
            GUILayout.Label(it.desc, brBodyStyle);
            GUILayout.Space(10);

            if (it.specs != null && it.specs.Length > 0)
            {
                GUILayout.Label("Ficha técnica", brPageH2Style);
                foreach (var sp in it.specs)
                    GUILayout.Label("• " + sp, brSpecStyle);
                GUILayout.Space(10);
            }

            // Cores disponíveis
            if (it.colors != null && it.colors.Length > 0)
            {
                GUILayout.Label("Cores disponíveis", brPageH2Style);
                browserColorIdx = Mathf.Clamp(browserColorIdx, 0, it.colors.Length - 1);
                Rect sw = GUILayoutUtility.GetRect(it.colors.Length * 44f, 44f);
                for (int c = 0; c < it.colors.Length; c++)
                {
                    Rect box = new Rect(sw.x + c * 44f, sw.y, 36f, 36f);
                    GUI.color = it.colors[c];
                    GUI.DrawTexture(box, whiteTex);
                    GUI.color = (c == browserColorIdx) ? new Color(0.1f, 0.5f, 1f) : new Color(0.3f, 0.3f, 0.3f);
                    DrawRectOutline(box, c == browserColorIdx ? 3f : 1f);
                    GUI.color = Color.white;
                    if (GUI.Button(box, "", GUIStyle.none)) browserColorIdx = c;
                }
                string cname = (it.colorNames != null && browserColorIdx < it.colorNames.Length) ? it.colorNames[browserColorIdx] : "";
                GUILayout.Label($"Selecionada: {cname}", brSmallStyle);
                GUILayout.Space(10);
            }
            else if (it.action.StartsWith("vehicle"))
            {
                GUILayout.Label("Cor: exclusiva de fábrica (modelo realista)", brSmallStyle);
                GUILayout.Space(10);
            }

            var save = CurrentSave;
            bool canAfford = save != null && save.currentMoney >= it.price;
            GUI.enabled = canAfford;
            if (GUILayout.Button(canAfford ? "Comprar agora" : "Saldo insuficiente", brBuyStyle, GUILayout.Height(40)))
                PurchaseItem(it, browserColorIdx);
            GUI.enabled = true;

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Compra

        private void PurchaseItem(ShopItem it, int colorIdx)
        {
            var save = CurrentSave;
            if (save == null) { ShowToast("[Aviso] Sem partida ativa."); return; }
            if (save.currentMoney < it.price) { ShowToast("Saldo insuficiente."); return; }

            if (it.action.StartsWith("vehicle:"))
            {
                string type = it.action.Substring("vehicle:".Length);
                var vm = VehicleManager.Instance;
                if (vm == null) { ShowToast("[Aviso] Gerenciador de veículos indisponível."); return; }

                var spawned = vm.SpawnVehicle(type);
                if (spawned == null) { ShowToast("[Aviso] Não foi possível entregar o veículo."); return; }

                if (it.colors != null && it.colors.Length > 0)
                {
                    int ci = Mathf.Clamp(colorIdx, 0, it.colors.Length - 1);
                    spawned.SetBodyColor(it.colors[ci]);
                }

                save.currentMoney -= it.price;
                PersistSave();
                ShowToast($" {it.name} entregue na sua posição!");
            }
            else if (it.action.StartsWith("animal:"))
            {
                string sp = it.action.Substring("animal:".Length);
                if (!System.Enum.TryParse(sp, out AnimalSpecies species))
                { ShowToast("[Aviso] Espécie desconhecida."); return; }

                Vector3 pos = AnimalDeliveryPosition();
                var animal = SceneObjectSpawner.SpawnPurchasedAnimal(species, pos, Terrain.activeTerrain);
                if (animal == null) { ShowToast("[Aviso] Não foi possível entregar o animal."); return; }

                save.currentMoney -= it.price;
                PersistSave();
                ShowToast($" {it.name} entregue na sua propriedade!");
            }
            else if (it.action == "land")
            {
                save.currentMoney -= it.price;
                save.reputation += 5;
                PersistSave();
                ShowToast($"Escritura emitida: {it.name} agora é sua! (+5 )");
            }
        }

        private Vector3 AnimalDeliveryPosition()
        {
            var player = PlayerCharacterController.Instance;
            if (player != null)
            {
                Vector3 fwd = player.transform.forward;
                return player.transform.position + fwd * 6f + Vector3.up * 0.5f;
            }
            return Vector3.zero;
        }

        #endregion

        #region Imagem procedural, cursor e utilitários de desenho

        private string FormatPrice(long p) => $"$ {p:N0}";

        private void DrawRectOutline(Rect r, float t)
        {
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), whiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), whiteTex);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), whiteTex);
        }

        private void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, whiteTex); GUI.color = Color.white; }

        /// <summary>Desenha uma "foto"estilizada do produto dentro de uma moldura.</summary>
        private void DrawProductImage(Rect frame, ShopItem it)
        {
            // Moldura e "céu"/fundo
            Fill(frame, new Color(0.86f, 0.90f, 0.96f));
            GUI.color = new Color(0.4f, 0.4f, 0.45f, 1f);
            DrawRectOutline(frame, 1f);
            GUI.color = Color.white;

            Rect in_ = new Rect(frame.x + 4, frame.y + 4, frame.width - 8, frame.height - 8);
            float cx = in_.x + in_.width * 0.5f;
            float groundY = in_.yMax - in_.height * 0.22f;

            // Chão
            Fill(new Rect(in_.x, groundY, in_.width, in_.yMax - groundY), new Color(0.55f, 0.52f, 0.45f));

            float uw = in_.width * 0.72f; // largura do "objeto"
            float ux = cx - uw * 0.5f;
            Color body = it.artA, dark = it.artB;

            switch (it.art)
            {
                case ShopArt.Car:
                {
                    float bodyH = in_.height * 0.22f;
                    float by = groundY - bodyH - in_.height * 0.06f;
                    Fill(new Rect(ux, by, uw, bodyH), body); // lataria
                    Fill(new Rect(ux + uw * 0.22f, by - bodyH * 0.7f, uw * 0.5f, bodyH * 0.8f), body); // teto
                    Fill(new Rect(ux + uw * 0.26f, by - bodyH * 0.55f, uw * 0.42f, bodyH * 0.55f), new Color(0.6f,0.8f,0.95f)); // vidro
                    DrawWheel(ux + uw * 0.2f, groundY - in_.height * 0.04f, in_.height * 0.14f, dark);
                    DrawWheel(ux + uw * 0.8f, groundY - in_.height * 0.04f, in_.height * 0.14f, dark);
                    break;
                }
                case ShopArt.Truck:
                {
                    float bodyH = in_.height * 0.30f;
                    float by = groundY - bodyH - in_.height * 0.07f;
                    Fill(new Rect(ux, by, uw * 0.55f, bodyH), new Color(0.85f,0.83f,0.80f)); // baú
                    Fill(new Rect(ux + uw * 0.55f, by + bodyH * 0.25f, uw * 0.45f, bodyH * 0.75f), body); // cabine
                    Fill(new Rect(ux + uw * 0.78f, by + bodyH * 0.35f, uw * 0.18f, bodyH * 0.4f), new Color(0.6f,0.8f,0.95f)); // vidro
                    DrawWheel(ux + uw * 0.18f, groundY - in_.height * 0.04f, in_.height * 0.15f, dark);
                    DrawWheel(ux + uw * 0.62f, groundY - in_.height * 0.04f, in_.height * 0.15f, dark);
                    DrawWheel(ux + uw * 0.86f, groundY - in_.height * 0.04f, in_.height * 0.15f, dark);
                    break;
                }
                case ShopArt.Tractor:
                {
                    float by = groundY - in_.height * 0.30f;
                    Fill(new Rect(ux + uw * 0.3f, by, uw * 0.5f, in_.height * 0.22f), body); // corpo
                    Fill(new Rect(ux + uw * 0.34f, by - in_.height * 0.14f, uw * 0.28f, in_.height * 0.16f), body); // cabine
                    DrawWheel(ux + uw * 0.72f, groundY - in_.height * 0.02f, in_.height * 0.22f, dark); // roda traseira grande
                    DrawWheel(ux + uw * 0.3f, groundY - in_.height * 0.02f, in_.height * 0.12f, dark); // dianteira
                    break;
                }
                case ShopArt.Plane:
                {
                    float cy = in_.y + in_.height * 0.42f;
                    Fill(new Rect(ux, cy, uw * 0.78f, in_.height * 0.14f), body); // fuselagem
                    Fill(new Rect(ux + uw * 0.3f, cy - in_.height * 0.16f, uw * 0.14f, in_.height * 0.46f), dark); // asa (vertical, perspectiva)
                    Fill(new Rect(ux + uw * 0.74f, cy - in_.height * 0.1f, uw * 0.1f, in_.height * 0.22f), dark); // leme
                    Fill(new Rect(ux + uw * 0.04f, cy + in_.height * 0.02f, uw * 0.12f, in_.height * 0.08f), new Color(0.6f,0.8f,0.95f)); // cabine
                    break;
                }
                case ShopArt.Boat:
                {
                    float wline = groundY;
                    Fill(new Rect(in_.x, wline, in_.width, in_.yMax - wline), new Color(0.20f, 0.45f, 0.75f)); // água
                    float hy = wline - in_.height * 0.14f;
                    Fill(new Rect(ux, hy, uw, in_.height * 0.16f), body); // casco
                    Fill(new Rect(ux + uw * 0.3f, hy - in_.height * 0.16f, uw * 0.4f, in_.height * 0.16f), new Color(0.9f,0.9f,0.92f)); // cabine
                    Fill(new Rect(ux + uw * 0.36f, hy - in_.height * 0.12f, uw * 0.28f, in_.height * 0.09f), new Color(0.6f,0.8f,0.95f)); // vidro
                    break;
                }
                case ShopArt.Animal:
                {
                    float by = groundY - in_.height * 0.26f;
                    Fill(new Rect(ux + uw * 0.2f, by, uw * 0.5f, in_.height * 0.2f), body); // corpo
                    Fill(new Rect(ux + uw * 0.62f, by - in_.height * 0.1f, uw * 0.18f, in_.height * 0.16f), body); // cabeça
                    // pernas
                    Fill(new Rect(ux + uw * 0.26f, by + in_.height * 0.18f, uw * 0.05f, in_.height * 0.12f), dark);
                    Fill(new Rect(ux + uw * 0.58f, by + in_.height * 0.18f, uw * 0.05f, in_.height * 0.12f), dark);
                    break;
                }
                case ShopArt.Land:
                {
                    // céu já pintado; sol
                    Fill(new Rect(in_.xMax - in_.width * 0.22f, in_.y + in_.height * 0.08f, in_.width * 0.12f, in_.width * 0.12f), new Color(0.98f, 0.85f, 0.25f));
                    // campo
                    Fill(new Rect(in_.x, groundY - in_.height * 0.1f, in_.width, in_.yMax - (groundY - in_.height * 0.1f)), it.artB);
                    // cerca
                    for (int f = 0; f < 5; f++)
                    {
                        float fx = in_.x + in_.width * (0.12f + f * 0.18f);
                        Fill(new Rect(fx, groundY - in_.height * 0.16f, 3f, in_.height * 0.16f), new Color(0.5f,0.35f,0.2f));
                    }
                    Fill(new Rect(in_.x + in_.width * 0.12f, groundY - in_.height * 0.12f, in_.width * 0.74f, 3f), new Color(0.5f,0.35f,0.2f));
                    break;
                }
            }

            // Emoji grande como "selo"no canto, reforçando o que é.
            GUI.Label(new Rect(in_.x + 2, in_.y + 2, 40, 40), it.emoji, brTitleTextStyle);
        }

        private void DrawWheel(float cx, float cy, float r, Color c)
        {
            Fill(new Rect(cx - r, cy - r, r * 2, r * 2), c);
            Fill(new Rect(cx - r * 0.4f, cy - r * 0.4f, r * 0.8f, r * 0.8f), new Color(0.6f, 0.6f, 0.62f));
        }

        private void EnsureRetroCursor()
        {
            if (retroCursorTex != null) return;
            // Setinha clássica 12x19, estilo Windows 9x (preto com borda branca).
            string[] rows =
            {
                "X...........",
                "XX..........",
                "XWX.........",
                "XWWX........",
                "XWWWX.......",
                "XWWWWX......",
                "XWWWWWX.....",
                "XWWWWWWX....",
                "XWWWWWWWX...",
                "XWWWWWWWWX..",
                "XWWWWWWWWWX.",
                "XWWWWWXXXXXX",
                "XWWXWWX.....",
                "XWX.XWWX....",
                "XX..XWWX....",
                "X....XWWX...",
                ".....XWWX...",
                "......XWX...",
                "......XX....",
            };
            int w = 12, h = rows.Length;
            retroCursorTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            retroCursorTex.filterMode = FilterMode.Point;
            for (int yy = 0; yy < h; yy++)
            {
                string row = rows[yy];
                for (int xx = 0; xx < w; xx++)
                {
                    char ch = xx < row.Length ? row[xx] : '.';
                    Color col = ch == 'X' ? Color.black : ch == 'W' ? Color.white : new Color(0, 0, 0, 0);
                    retroCursorTex.SetPixel(xx, h - 1 - yy, col); // inverte Y para espaço de textura
                }
            }
            retroCursorTex.Apply();
        }

        private void DrawRetroCursor()
        {
            EnsureRetroCursor();
            Vector2 m = Event.current.mousePosition;
            float scale = 1.6f;
            GUI.DrawTexture(new Rect(m.x, m.y, retroCursorTex.width * scale, retroCursorTex.height * scale), retroCursorTex);
        }

        #endregion

        #region Estilos do navegador

        private void InitBrowserStyles()
        {
            if (browserStylesReady) return;

            brTitleTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            brTitleTextStyle.normal.textColor = Color.white;

            brWinBtnStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            brWinBtnStyle.normal.textColor = Color.white;

            brToolBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            brAddrStyle = new GUIStyle(GUI.skin.textField) { fontSize = 12, alignment = TextAnchor.MiddleLeft };

            brBookmarkStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2) };

            brPageH1Style = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true };
            brPageH1Style.normal.textColor = new Color(0.10f, 0.12f, 0.22f);

            brPageH2Style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, wordWrap = true };
            brPageH2Style.normal.textColor = new Color(0.12f, 0.14f, 0.26f);

            brBodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            brBodyStyle.normal.textColor = new Color(0.15f, 0.16f, 0.20f);

            brSmallStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
            brSmallStyle.normal.textColor = new Color(0.30f, 0.32f, 0.38f);

            brSpecStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            brSpecStyle.normal.textColor = new Color(0.18f, 0.30f, 0.20f);

            brPriceStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            brPriceStyle.normal.textColor = new Color(0.10f, 0.45f, 0.18f);

            brBuyStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            brBuyStyle.normal.textColor = new Color(0.05f, 0.25f, 0.08f);

            brLinkCardStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            brLinkCardStyle.normal.textColor = new Color(0.10f, 0.25f, 0.80f);

            brStatusStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleLeft };
            brStatusStyle.normal.textColor = new Color(0.20f, 0.22f, 0.26f);

            browserStylesReady = true;
        }

        #endregion
    }
}
