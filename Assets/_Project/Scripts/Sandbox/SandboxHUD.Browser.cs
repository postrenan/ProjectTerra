using UnityEngine;
using UnityEngine.InputSystem;
using ProjectTerra.Core;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// "TerraBrowser": Navegador de internet in-game moderno, limpo e responsivo.
    /// Interface minimalista contemporânea (estilo Chrome/Edge/Arc), com abas limpas,
    /// ícones vetoriais nativos (TerraIcons / Lucide), sem emojis, sem poluição visual
    /// e com cursor de mouse estável sem flickering.
    /// </summary>
    public partial class SandboxHUD
    {
        #region Estado do Navegador

        public bool IsBrowserOpen { get; private set; } = false;

        // Localização atual: "home", "store:<id>", "item:<id>:<index>", "svc:<id>", "landmap".
        private string browserLoc = "home";
        private readonly System.Collections.Generic.List<string> browserHistory = new System.Collections.Generic.List<string>();
        private int browserHistIdx = -1;

        private Vector2 browserScroll = Vector2.zero;
        private string browserAddress = "https://terranet.com/";
        private int browserColorIdx = 0;
        private float browserLoadAnim = 1f;
        private string portalCategoryFilter = "all"; // "all", "vehicles", "land", "services"

        private bool browserStylesReady = false;

        // Cores Modernas (Design System Clean)
        private static readonly Color UiBgDark = new Color(0.94f, 0.95f, 0.97f);
        private static readonly Color UiCardBg = new Color(1f, 1f, 1f);
        private static readonly Color UiCardHover = new Color(0.97f, 0.98f, 1f);
        private static readonly Color UiBorderLight = new Color(0.86f, 0.88f, 0.92f);
        private static readonly Color UiBorderFocus = new Color(0.20f, 0.45f, 0.95f);
        private static readonly Color UiPrimaryBlue = new Color(0.12f, 0.42f, 0.90f);
        private static readonly Color UiPrimaryBlueHover = new Color(0.20f, 0.50f, 0.98f);
        private static readonly Color UiSuccessGreen = new Color(0.06f, 0.62f, 0.35f);
        private static readonly Color UiTextPrimary = new Color(0.08f, 0.10f, 0.15f);
        private static readonly Color UiTextSecondary = new Color(0.40f, 0.44f, 0.52f);
        private static readonly Color UiTextMuted = new Color(0.58f, 0.62f, 0.70f);

        // Estilos do navegador
        private GUIStyle brTabActiveStyle, brTabInactiveStyle, brWinBtnStyle;
        private GUIStyle brToolBtnStyle, brAddrStyle, brBookmarkStyle, brBookmarkActiveStyle;
        private GUIStyle brPageH1Style, brPageH2Style, brPageH3Style, brBodyStyle, brSmallStyle;
        private GUIStyle brPriceStyle, brBuyPrimaryStyle, brBuySecondaryStyle;
        private GUIStyle brBadgeStyle, brFilterChipStyle, brFilterChipActiveStyle;

        #endregion

        #region Dados das Lojas

        private enum ShopArt { Car, Truck, Plane, Boat, Tractor, Animal, Land }

        private class ShopItem
        {
            public string name;
            public string badge;
            public string desc;
            public long price;
            public string[] specs;
            public Color[] colors;
            public string[] colorNames;
            public string action;
            public ShopArt art;
            public Color artA, artB;
        }

        private class ShopSite
        {
            public string id;
            public string title;
            public string subtitle;
            public string tagline;
            public string url;
            public string category;
            public Color brand;
            public ShopItem[] items;
        }

        private static readonly Color[] VehiclePalette =
        {
            new Color(0.85f, 0.18f, 0.18f), new Color(0.12f, 0.35f, 0.85f),
            new Color(0.12f, 0.12f, 0.14f), new Color(0.95f, 0.95f, 0.97f),
            new Color(0.12f, 0.55f, 0.35f), new Color(0.95f, 0.75f, 0.12f),
            new Color(0.60f, 0.62f, 0.68f), new Color(0.85f, 0.45f, 0.12f),
        };
        private static readonly string[] VehicleColorNames =
        { "Vermelho Rubro", "Azul Metálico", "Preto Noite", "Branco Pérola", "Verde Esmeralda", "Amarelo Ouro", "Prata Platina", "Laranja Solar" };

        private ShopSite[] _sites;
        private ShopSite[] Sites => _sites ?? (_sites = BuildSites());

        private ShopSite[] BuildSites()
        {
            return new[]
            {
                new ShopSite
                {
                    id = "usados",
                    title = "Zé das Latas Seminovos",
                    subtitle = "Veículos usados, utilitários e tratores revisados",
                    category = "vehicles",
                    url = "https://zedaslatas.terranet.com/",
                    tagline = "Transparência total, laudo de procedência e entrega imediata na sua posição.",
                    brand = new Color(0.92f, 0.40f, 0.15f),
                    items = new[]
                    {
                        new ShopItem { name = "Fusca 1300 Clássico", badge = "Seminovo", price = 18500, action = "vehicle:car",
                            art = ShopArt.Car, artA = new Color(0.55f,0.70f,0.35f), artB = new Color(0.2f,0.2f,0.2f),
                            desc = "Ícone clássico restaurado. Motor a ar 1300 revisado, tapeçaria original e mecânica confiável para rodar no dia a dia.",
                            specs = new[]{ "Ano 1975", "198.400 km", "Câmbio Manual", "Gasolina" },
                            colors = new[]{ new Color(0.55f,0.70f,0.35f), new Color(0.75f,0.72f,0.60f), new Color(0.30f,0.40f,0.65f) },
                            colorNames = new[]{ "Verde Musgo", "Bege Areia", "Azul Desbotado" } },
                        new ShopItem { name = "Picape Batalhada 4x2", badge = "Utilitário", price = 34900, action = "vehicle:van",
                            art = ShopArt.Truck, artA = new Color(0.45f,0.46f,0.48f), artB = new Color(0.2f,0.2f,0.22f),
                            desc = "Estrutura robusta para trabalho contínuo. Excelente para transporte de insumos, ferramentas e apoio à fazenda.",
                            specs = new[]{ "Ano 1998", "310.000 km", "Carga útil: 750 kg", "Diesel" },
                            colors = new[]{ new Color(0.45f,0.46f,0.48f), new Color(0.60f,0.30f,0.20f) },
                            colorNames = new[]{ "Cinza Urbano", "Marrom Rústico" } },
                        new ShopItem { name = "Trator Valmet Agrícola", badge = "Campo", price = 46000, action = "vehicle:tractor",
                            art = ShopArt.Tractor, artA = new Color(0.70f,0.55f,0.10f), artB = new Color(0.15f,0.15f,0.15f),
                            desc = "Trator clássico de alta resistência. Tomada de força funcional, engate traseiro e manutenção simples e econômica.",
                            specs = new[]{ "Ano 1989", "9.200 horas", "Tração traseira", "Diesel agrícola" },
                            colors = new[]{ new Color(0.70f,0.55f,0.10f), new Color(0.75f,0.20f,0.15f) },
                            colorNames = new[]{ "Amarelo Campo", "Vermelho Clássico" } },
                        new ShopItem { name = "Caminhão Toco Baú", badge = "Carga", price = 89000, action = "vehicle:truck",
                            art = ShopArt.Truck, artA = new Color(0.20f,0.35f,0.55f), artB = new Color(0.15f,0.15f,0.18f),
                            desc = "Baú vedado em alumínio, cabine reformada e freio a ar calibrado. Ideal para iniciar fretes comerciais lucrativos.",
                            specs = new[]{ "Ano 1996", "540.000 km", "Capacidade: 6 t", "Baú alumínio" },
                            colors = new[]{ new Color(0.20f,0.35f,0.55f), new Color(0.80f,0.80f,0.82f) },
                            colorNames = new[]{ "Azul Estradeiro", "Branco Polar" } },
                    }
                },
                new ShopSite
                {
                    id = "novos",
                    title = "AutoPopular 0km",
                    subtitle = "Concessionária oficial e veículos zero quilômetro",
                    category = "vehicles",
                    url = "https://autopopular.terranet.com/",
                    tagline = "Garantia de 3 anos de fábrica, alta eficiência energética e entrega direta de fábrica.",
                    brand = new Color(0.08f, 0.55f, 0.35f),
                    items = new[]
                    {
                        new ShopItem { name = "Hatch Compacto 1.0", badge = "0 km • Econômico", price = 62000, action = "vehicle:car",
                            art = ShopArt.Car, artA = new Color(0.90f,0.90f,0.92f), artB = new Color(0.2f,0.2f,0.25f),
                            desc = "Ágil, moderno e extremamente econômico. Média de 15,2 km/l com ar condicionado digital e direção assistida.",
                            specs = new[]{ "Zero km", "15,2 km/l", "Garantia 3 anos", "Ar Condicionado" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Sedan Touring 1.6", badge = "0 km • Conforto", price = 84000, action = "vehicle:sedan",
                            art = ShopArt.Car, artA = new Color(0.20f,0.45f,0.85f), artB = new Color(0.15f,0.15f,0.2f),
                            desc = "Espaço interno generoso com porta-malas de 510 litros. Excelente estabilidade e conforto acústico em rodovias.",
                            specs = new[]{ "Zero km", "102 cv", "Porta-malas: 510 L", "Garantia 3 anos" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Van Furgão Express", badge = "0 km • Comercial", price = 98000, action = "vehicle:van",
                            art = ShopArt.Truck, artA = new Color(0.90f,0.90f,0.92f), artB = new Color(0.2f,0.2f,0.22f),
                            desc = "Utilitário espaçoso para encomendas e entregas urbanas ou rurais. Porta lateral deslizante e assoalho reforçado.",
                            specs = new[]{ "Zero km", "Carga: 1.200 kg", "Volume: 7,5 m³", "Garantia 2 anos" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Trator AgroTech 4x4", badge = "0 km • Eficiência", price = 120000, action = "vehicle:tractor",
                            art = ShopArt.Tractor, artA = new Color(0.20f,0.55f,0.25f), artB = new Color(0.12f,0.12f,0.12f),
                            desc = "Alta tração e baixo consumo para operações modernas no campo. Compatível com implementos triponto e hidráulicos.",
                            specs = new[]{ "Zero km", "95 cv Turbo", "Tração 4x4", "Garantia 2 anos" },
                            colors = new[]{ new Color(0.20f,0.55f,0.25f), new Color(0.80f,0.20f,0.15f), new Color(0.10f,0.35f,0.70f) },
                            colorNames = new[]{ "Verde Agro", "Vermelho Rubi", "Azul Campeiro" } },
                    }
                },
                new ShopSite
                {
                    id = "luxo",
                    title = "Prestige Motors VIP",
                    subtitle = "Veículos premium, esportivos e mobilidade executiva",
                    category = "vehicles",
                    url = "https://prestigemotors.terranet.com/",
                    tagline = "Alto desempenho, acabamento exclusivo em couro nobre e engenharia refinada.",
                    brand = new Color(0.15f, 0.18f, 0.25f),
                    items = new[]
                    {
                        new ShopItem { name = "Superesportivo GT V8", badge = "Alta Performance", price = 640000, action = "vehicle:sports",
                            art = ShopArt.Car, artA = new Color(0.85f,0.10f,0.12f), artB = new Color(0.08f,0.08f,0.08f),
                            desc = "Aceleração instantânea de 0 a 100 km/h em 3,2 segundos. Chassi esportivo rebaixado e aerodinâmica de ponta.",
                            specs = new[]{ "V8 620 cv", "0–100: 3,2s", "Vmáx: 325 km/h", "Interior Alcantara" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "SUV Blindada Premium 4x4", badge = "Segurança VIP", price = 420000, action = "vehicle:suv",
                            art = ShopArt.Car, artA = new Color(0.10f,0.10f,0.12f), artB = new Color(0.25f,0.25f,0.3f),
                            desc = "Imponência e sofisticação fora de estrada. Tração integral sob demanda com suspensão adaptativa inteligente.",
                            specs = new[]{ "V6 400 cv", "Blindado Nível III-A", "Tração AWD", "Som Hi-Fi" },
                            colors = VehiclePalette, colorNames = VehicleColorNames },
                        new ShopItem { name = "Cavalo Mecânico Scania V8", badge = "Linha Pesada", price = 890000, action = "vehicle:scania",
                            art = ShopArt.Truck, artA = new Color(0.10f,0.20f,0.45f), artB = new Color(0.1f,0.1f,0.12f),
                            desc = "Potência incomparável para transporte interestadual. Cabine leito completa com ar e freio retarder.",
                            specs = new[]{ "580 cv V8", "Torque: 2.700 Nm", "Cabine Leito Luxo", "Freio Retarder" },
                            colors = null, colorNames = null },
                        new ShopItem { name = "Aeronave Turboélice Executiva", badge = "Aviação", price = 1250000, action = "vehicle:plane",
                            art = ShopArt.Plane, artA = new Color(0.92f,0.92f,0.95f), artB = new Color(0.70f,0.15f,0.15f),
                            desc = "Deslocamentos regionais rápidos com autonomia de longo alcance. Opera em pistas pavimentadas ou de grama.",
                            specs = new[]{ "Alcance: 2.400 km", "Cruzeiro: 480 km/h", "6 Passageiros", "Aviônica Digital" },
                            colors = new[]{ new Color(0.92f,0.92f,0.95f), new Color(0.10f,0.12f,0.18f) },
                            colorNames = new[]{ "Branco Pérola", "Grafite Metálico" } },
                        new ShopItem { name = "Iate Náutico Fluvial 42ft", badge = "Náutica", price = 980000, action = "vehicle:boat",
                            art = ShopArt.Boat, artA = new Color(0.95f,0.95f,0.97f), artB = new Color(0.10f,0.30f,0.55f),
                            desc = "Navegue os rios e lagos da região com máximo conforto. Duas suítes privativas climatizadas e deck superior amplo.",
                            specs = new[]{ "42 pés", "2x Motores Diesel", "2 Suítes", "Flybridge" },
                            colors = new[]{ new Color(0.95f,0.95f,0.97f), new Color(0.10f,0.15f,0.20f) },
                            colorNames = new[]{ "Branco Náutico", "Azul Marinho" } },
                    }
                },
                new ShopSite
                {
                    id = "terrenos",
                    title = "Terra & Cia Imóveis Rurais",
                    subtitle = "Glebas, chácaras e fazendas com mapa satelital",
                    category = "land",
                    url = "https://terraecia.terranet.com/",
                    tagline = "Registro de propriedade digital, dados cadastrais e demarcação georreferenciada.",
                    brand = new Color(0.20f, 0.45f, 0.22f),
                    items = new ShopItem[0]
                },
                new ShopSite
                {
                    id = "animais",
                    title = "Rancho Online Agropecuária",
                    subtitle = "Genética selecionada, matrizes e animais de trabalho",
                    category = "animals",
                    url = "https://ranchoonline.terranet.com/",
                    tagline = "Sanidade atestada, pedigree conferido e entrega direta na cerca da sua propriedade.",
                    brand = new Color(0.60f, 0.38f, 0.18f),
                    items = new[]
                    {
                        new ShopItem { name = "Vaca Leiteira Holandesa", badge = "Lactação", price = 7500, action = "animal:Cow",
                            art = ShopArt.Animal, artA = new Color(0.55f,0.35f,0.22f), artB = new Color(0.92f,0.90f,0.88f),
                            desc = "Alta produtividade de leite diário, dócil e bem adaptada a pastagens regionais.",
                            specs = new[]{ "~28 L/dia", "Dócil", "580 kg", "Vacinada" } },
                        new ShopItem { name = "Cavalo Manga-Larga", badge = "Montaria", price = 15000, action = "animal:Horse",
                            art = ShopArt.Animal, artA = new Color(0.32f,0.20f,0.12f), artB = new Color(0.15f,0.10f,0.08f),
                            desc = "Marcha suave e excelente resistência física para deslocamentos no campo e manejo de gado.",
                            specs = new[]{ "Marcha suave", "Treinado", "~460 kg", "Resistente" } },
                        new ShopItem { name = "Ovelha Santa Inês", badge = "Rústico", price = 2200, action = "animal:Sheep",
                            art = ShopArt.Animal, artA = new Color(0.92f,0.90f,0.85f), artB = new Color(0.20f,0.18f,0.18f),
                            desc = "Mansa e resistente. Produz lã de qualidade e atua no manejo natural das pastagens.",
                            specs = new[]{ "Lã e carne", "Mansa", "~70 kg", "Fácil manejo" } },
                        new ShopItem { name = "Raposa do Campo", badge = "Silvestre", price = 9800, action = "animal:Fox",
                            art = ShopArt.Animal, artA = new Color(0.85f,0.42f,0.12f), artB = new Color(0.95f,0.95f,0.95f),
                            desc = "Animal silvestre catalogado pelo serviço ambiental regional. Rápida e curiosa.",
                            specs = new[]{ "Silvestre", "Ágil", "~7 kg", "Catalogada" } },
                        new ShopItem { name = "Lobo da Estepe (Guarda)", badge = "Proteção", price = 21000, action = "animal:Wolf",
                            art = ShopArt.Animal, artA = new Color(0.35f,0.35f,0.38f), artB = new Color(0.20f,0.20f,0.22f),
                            desc = "Grande porte e instinto protetor aguçado para vigilância perimetral da sede da fazenda.",
                            specs = new[]{ "Guarda", "Territorial", "~48 kg", "Vigoroso" } },
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

        #region Navegação

        public void OpenVehicleBrowser()
        {
            CloseOptionsMenu();
            if (IsRegionalMapOpen) CloseRegionalMap();

            IsBrowserOpen = true;
            browserHistory.Clear();
            browserHistIdx = -1;
            Navigate("home", true);

            if (!Cursor.visible) Cursor.visible = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
        }

        public void CloseBrowser()
        {
            IsBrowserOpen = false;
            RestoreGameplayCursor();
        }

        private void Navigate(string loc, bool pushHistory)
        {
            if (loc == "store:terrenos") loc = "landmap";
            browserLoc = loc;
            browserScroll = Vector2.zero;
            browserColorIdx = 0;
            browserLoadAnim = 0.2f;
            browserAddress = AddressFor(loc);

            if (pushHistory)
            {
                if (browserHistIdx < browserHistory.Count - 1)
                    browserHistory.RemoveRange(browserHistIdx + 1, browserHistory.Count - browserHistIdx - 1);
                browserHistory.Add(loc);
                browserHistIdx = browserHistory.Count - 1;
            }
        }

        private string AddressFor(string loc)
        {
            if (loc == "home") return "https://terranet.com/";
            if (loc == "landmap") return "https://terraecia.terranet.com/mapa";
            if (loc.StartsWith("svc:"))
            {
                var sv = FindService(loc.Substring(4));
                return sv != null ? sv.url : "https://terranet.com/";
            }
            if (loc.StartsWith("store:"))
            {
                var s = FindSite(loc.Substring(6));
                return s != null ? s.url : "https://terranet.com/";
            }
            if (loc.StartsWith("item:"))
            {
                var parts = loc.Split(':');
                var s = FindSite(parts[1]);
                if (s != null && int.TryParse(parts[2], out int idx) && idx >= 0 && idx < s.items.Length)
                    return s.url + "p/" + idx;
            }
            return "https://terranet.com/";
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

        private bool HandleBrowserInput()
        {
            if (!IsBrowserOpen) return false;

            if (TerraInput.GetKeyDown(Key.Escape))
            {
                if (browserLoc != "home") BrowserBack();
                else CloseBrowser();
            }
            return true;
        }

        #endregion

        #region Desenho da Janela Moderna (Clean Browser Chrome)

        private void DrawBrowser()
        {
            InitBrowserStyles();

            // Garante cursor estável sem chamadas redundantes a cada frame
            if (!Cursor.visible) Cursor.visible = true;
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;

            browserLoadAnim = Mathf.MoveTowards(browserLoadAnim, 1f, Time.unscaledDeltaTime * 4f);

            Rect full = new Rect(0, 0, Screen.width, Screen.height);

            // Fundo externo suave
            Fill(full, UiBgDark);

            // --- 1. BARRA DE ABAS SUPERIOR (34px) ---
            float tabH = 34f;
            Rect tabBar = new Rect(0, 0, full.width, tabH);
            Fill(tabBar, new Color(0.88f, 0.90f, 0.93f));

            // Aba Ativa (Clean White Tab)
            float tabW = Mathf.Clamp(full.width * 0.28f, 180f, 280f);
            Rect tabActive = new Rect(8f, 4f, tabW, tabH - 4f);
            Fill(tabActive, Color.white);
            DrawOutline(tabActive, UiBorderLight);

            // Ícone da aba nativo
            TerraIcons.Draw(new Rect(tabActive.x + 8f, tabActive.y + 7f, 16f, 16f), TerraIcon.Shop, UiPrimaryBlue);

            string titleTxt = CurrentPageTitle();
            GUI.Label(new Rect(tabActive.x + 28f, tabActive.y + 4f, tabActive.width - 54f, tabActive.height - 4f),
                titleTxt, brTabActiveStyle);

            if (GUI.Button(new Rect(tabActive.xMax - 22f, tabActive.y + 5f, 18f, 18f), "✕", brWinBtnStyle))
            {
                CloseBrowser();
                return;
            }

            // Controles de Janela Minimalistas à direita: [ — ] [ □ ] [ ✕ ]
            float ctrlW = 34f, ctrlH = tabH - 4f;
            float cx = full.width - ctrlW * 3f - 6f;

            if (GUI.Button(new Rect(cx, 2f, ctrlW, ctrlH), "—", brWinBtnStyle)) { }
            if (GUI.Button(new Rect(cx + ctrlW, 2f, ctrlW, ctrlH), "□", brWinBtnStyle)) { }
            if (GUI.Button(new Rect(cx + ctrlW * 2f, 2f, ctrlW, ctrlH), "✕", brWinBtnStyle))
            {
                CloseBrowser();
                return;
            }

            // --- 2. BARRA DE NAVEGAÇÃO & OMNIBOX (46px) ---
            float navH = 46f;
            Rect navBar = new Rect(0, tabH, full.width, navH);
            Fill(navBar, Color.white);
            Fill(new Rect(0, navBar.yMax - 1f, full.width, 1f), UiBorderLight);

            float nx = 12f;
            bool canBack = browserHistIdx > 0;
            bool canFwd = browserHistIdx < browserHistory.Count - 1;

            GUI.enabled = canBack;
            if (GUI.Button(new Rect(nx, navBar.y + 8f, 32f, 30f), "←", brToolBtnStyle)) BrowserBack();
            nx += 36f;

            GUI.enabled = canFwd;
            if (GUI.Button(new Rect(nx, navBar.y + 8f, 32f, 30f), "→", brToolBtnStyle)) BrowserForward();
            nx += 36f;

            GUI.enabled = true;
            if (GUI.Button(new Rect(nx, navBar.y + 8f, 32f, 30f), "↻", brToolBtnStyle))
            {
                browserLoadAnim = 0.2f;
                Navigate(browserLoc, false);
            }
            nx += 36f;

            if (GUI.Button(new Rect(nx, navBar.y + 8f, 32f, 30f), "⌂", brToolBtnStyle))
            {
                Navigate("home", true);
            }
            nx += 42f;

            // Saldo do usuário em Chip com ícone vetorial CreditCard
            var save = CurrentSave;
            string moneyStr = save != null ? save.GetFormattedMoney() : "$ --";
            float moneyChipW = Mathf.Clamp(full.width * 0.16f, 140f, 180f);
            Rect moneyChip = new Rect(full.width - moneyChipW - 14f, navBar.y + 8f, moneyChipW, 30f);
            Fill(moneyChip, new Color(0.92f, 0.98f, 0.94f));
            DrawOutline(moneyChip, new Color(0.70f, 0.90f, 0.78f));

            TerraIcons.Draw(new Rect(moneyChip.x + 8f, moneyChip.y + 7f, 16f, 16f), TerraIcon.CreditCard, UiSuccessGreen);
            GUI.Label(new Rect(moneyChip.x + 28f, moneyChip.y + 2f, moneyChip.width - 32f, moneyChip.height - 4f), moneyStr, brPriceStyle);

            // Omnibox / Campo de URL com ícone Lock vetorial
            float omniW = Mathf.Max(180f, moneyChip.x - nx - 16f);
            Rect omniBox = new Rect(nx, navBar.y + 8f, omniW, 30f);
            Fill(omniBox, new Color(0.96f, 0.97f, 0.98f));
            DrawOutline(omniBox, UiBorderLight);

            TerraIcons.Draw(new Rect(omniBox.x + 8f, omniBox.y + 8f, 14f, 14f), TerraIcon.Lock, UiTextSecondary);

            GUI.SetNextControlName("omniField");
            browserAddress = GUI.TextField(
                new Rect(omniBox.x + 28f, omniBox.y + 5f, omniBox.width - 72f, 20f),
                browserAddress, brAddrStyle);

            if (GUI.Button(new Rect(omniBox.xMax - 40f, omniBox.y + 3f, 36f, 24f), "Ir", brToolBtnStyle))
            {
                TryNavigateAddress(browserAddress);
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "omniField")
            {
                TryNavigateAddress(browserAddress);
                Event.current.Use();
            }

            // Barra de Progresso Fina no topo
            if (browserLoadAnim < 1f)
            {
                Fill(new Rect(0, navBar.yMax - 2f, full.width * Mathf.Clamp01(browserLoadAnim), 2f), UiPrimaryBlue);
            }

            // --- 3. BARRA DE ATALHOS / FAVORITOS (36px) COM ÍCONES VETORIAIS ---
            float bmH = 36f;
            Rect bmBar = new Rect(0, navBar.yMax, full.width, bmH);
            Fill(bmBar, new Color(0.98f, 0.98f, 0.99f));
            Fill(new Rect(0, bmBar.yMax - 1f, full.width, 1f), UiBorderLight);

            float bx = 14f;
            (TerraIcon icon, string label, string target)[] modernLinks =
            {
                (TerraIcon.Shop, "Início", "home"),
                (TerraIcon.UtilityVehicle, "Seminovos", "store:usados"),
                (TerraIcon.SportsCar, "Novos 0km", "store:novos"),
                (TerraIcon.LuxuryYacht, "Luxo VIP", "store:luxo"),
                (TerraIcon.House, "Terrenos", "landmap"),
                (TerraIcon.Cow, "Animais", "store:animais"),
                (TerraIcon.Newspaper, "Notícias", "svc:noticias"),
                (TerraIcon.TrendingUp, "Cotações", "svc:mercado"),
                (TerraIcon.Bank, "Banco", "svc:banco"),
                (TerraIcon.Fuel, "Posto", "svc:posto"),
                (TerraIcon.ReputationStar, "Metas", "svc:metas"),
                (TerraIcon.Compass, "Rodovias", "svc:rodovias")
            };

            foreach (var link in modernLinks)
            {
                bool isActive = browserLoc == link.target || (link.target.StartsWith("store:") && browserLoc.StartsWith(link.target));
                float textW = brBookmarkStyle.CalcSize(new GUIContent(link.label)).x;
                float btnW = textW + 34f;
                if (bx + btnW > full.width - 16f) break;

                Rect bRect = new Rect(bx, bmBar.y + 5f, btnW, 26f);
                if (isActive)
                {
                    Fill(bRect, new Color(0.90f, 0.94f, 1f));
                    DrawOutline(bRect, new Color(0.75f, 0.85f, 0.98f));
                }

                Color iconColor = isActive ? UiPrimaryBlue : UiTextSecondary;
                TerraIcons.Draw(new Rect(bRect.x + 6f, bRect.y + 6f, 14f, 14f), link.icon, iconColor);

                GUI.Label(new Rect(bRect.x + 24f, bRect.y + 3f, textW + 4f, 20f), link.label, isActive ? brBookmarkActiveStyle : brBookmarkStyle);

                if (GUI.Button(bRect, "", GUIStyle.none))
                {
                    Navigate(link.target, true);
                }
                bx += btnW + 6f;
            }

            // --- 4. VIEWPORT DA PÁGINA WEB ---
            float contentY = bmBar.yMax;
            Rect page = new Rect(0, contentY, full.width, full.height - contentY);
            Fill(page, UiBgDark);

            if (browserLoc == "home") DrawModernPortalHome(page);
            else if (browserLoc == "landmap") DrawLandMarket(page);
            else if (browserLoc.StartsWith("svc:")) DrawServicePage(page, FindService(browserLoc.Substring(4)));
            else if (browserLoc.StartsWith("store:")) DrawModernStorePage(page, FindSite(browserLoc.Substring(6)));
            else if (browserLoc.StartsWith("item:")) DrawModernItemPage(page);
        }

        private string CurrentPageTitle()
        {
            if (browserLoc == "home") return "TerraNet • Início";
            if (browserLoc == "landmap") return "Terra & Cia • Mapa de Terrenos";
            if (browserLoc.StartsWith("svc:"))
            {
                var sv = FindService(browserLoc.Substring(4));
                return sv != null ? sv.title : "Serviços";
            }
            if (browserLoc.StartsWith("store:"))
            {
                var s = FindSite(browserLoc.Substring(6));
                return s != null ? s.title : "Loja";
            }
            if (browserLoc.StartsWith("item:"))
            {
                var it = CurrentItem();
                return it != null ? it.name : "Detalhes";
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
            browserLoadAnim = 0.2f;
            string a = (addr ?? "").ToLowerInvariant();

            if (a.Contains("mapa") || a.Contains("terrenos")) { Navigate("landmap", true); return; }

            foreach (var s in Sites)
            {
                if (a.Contains(s.url.ToLowerInvariant().Replace("https://", "").Replace("/", "")) || a.Contains(s.id))
                {
                    Navigate("store:" + s.id, true);
                    return;
                }
            }
            foreach (var sv in ServiceSites)
            {
                if (a.Contains(sv.url.ToLowerInvariant().Replace("https://", "").Replace("/", "")) || a.Contains(sv.id))
                {
                    Navigate("svc:" + sv.id, true);
                    return;
                }
            }
            Navigate("home", true);
        }

        #endregion

        #region Portal Inicial Moderno e Clean

        private void DrawModernPortalHome(Rect page)
        {
            float maxContainerW = Mathf.Min(1160f, page.width - 48f);
            float startX = page.x + (page.width - maxContainerW) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, page.y + 20f, maxContainerW, page.height - 30f));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            var save = CurrentSave;
            string reg = save != null ? save.regionName : "Bacia Regional";
            long day = save != null ? save.gameDay : 1;

            // --- HERO BANNER MODERNO ---
            Rect hero = GUILayoutUtility.GetRect(maxContainerW, 110f);
            Fill(hero, UiCardBg);
            DrawOutline(hero, UiBorderLight);

            GUI.Label(new Rect(hero.x + 24f, hero.y + 18f, hero.width - 48f, 30f), "TerraNet Hub", brPageH1Style);
            GUI.Label(new Rect(hero.x + 24f, hero.y + 50f, hero.width - 48f, 24f),
                "Portal regional integrado de comércio, terrenos, logística e serviços bancários.", brBodyStyle);

            // Métricas no hero com ícones nativos
            float hx = hero.x + 24f;
            float hy = hero.y + 78f;

            TerraIcons.Draw(new Rect(hx, hy + 2f, 14f, 14f), TerraIcon.LocationPin, UiTextMuted);
            hx += 18f;
            GUI.Label(new Rect(hx, hy, 160f, 20f), reg, brSmallStyle);
            hx += 150f;

            TerraIcons.Draw(new Rect(hx, hy + 2f, 14f, 14f), TerraIcon.Calendar, UiTextMuted);
            hx += 18f;
            GUI.Label(new Rect(hx, hy, 100f, 20f), $"Dia {day}", brSmallStyle);
            hx += 90f;

            Fill(new Rect(hx, hy + 5f, 8f, 8f), UiSuccessGreen);
            hx += 14f;
            GUI.Label(new Rect(hx, hy, 180f, 20f), "Serviços On-line Ativos", brSmallStyle);

            GUILayout.Space(20);

            // --- FILTROS DE CATEGORIA ---
            GUILayout.BeginHorizontal();
            (string label, string key)[] filters =
            {
                ("Todos", "all"),
                ("Veículos", "vehicles"),
                ("Imóveis & Terras", "land"),
                ("Serviços Financeiros", "services")
            };

            foreach (var f in filters)
            {
                bool isSel = portalCategoryFilter == f.key;
                float fw = brFilterChipStyle.CalcSize(new GUIContent(f.label)).x + 24f;
                Rect fRect = GUILayoutUtility.GetRect(fw, 32f, GUILayout.Width(fw), GUILayout.Height(32f));

                Fill(fRect, isSel ? UiPrimaryBlue : UiCardBg);
                DrawOutline(fRect, isSel ? UiPrimaryBlue : UiBorderLight);

                if (GUI.Button(fRect, f.label, isSel ? brFilterChipActiveStyle : brFilterChipStyle))
                {
                    portalCategoryFilter = f.key;
                }
                GUILayout.Space(8);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            // --- GRID DE CARTÕES MODERNOS COM ÍCONES VETORIAIS ---
            int cols = maxContainerW >= 740f ? 2 : 1;
            float gap = 16f;
            float cardW = (maxContainerW - (cols - 1) * gap) / cols;
            float cardH = 150f;

            var cardList = new System.Collections.Generic.List<(TerraIcon icon, string title, string subtitle, string tag, string desc, string navTarget, Color accent)>();

            if (portalCategoryFilter == "all" || portalCategoryFilter == "vehicles")
            {
                cardList.Add((TerraIcon.UtilityVehicle, "Zé das Latas", "Seminovos e utilitários de trabalho", "Veículos", "Carros, picapes e caminhões revisados com entrega imediata.", "store:usados", new Color(0.92f, 0.40f, 0.15f)));
                cardList.Add((TerraIcon.SportsCar, "AutoPopular", "Linha 0km com garantia de fábrica", "Concessionária", "Veículos novos, compactos e tratores modernos com 3 anos de garantia.", "store:novos", new Color(0.08f, 0.55f, 0.35f)));
                cardList.Add((TerraIcon.LuxuryYacht, "Prestige Motors", "Veículos de luxo, esportivos e aviação", "Exclusivo", "Superesportivos V8, SUVs blindadas e aeronaves executivas.", "store:luxo", new Color(0.18f, 0.22f, 0.32f)));
                cardList.Add((TerraIcon.Cow, "Rancho Online", "Animais selecionados e montaria", "Agropecuária", "Vacas leiteiras, cavalos Manga-Larga e matrizes de alta linhagem.", "store:animais", new Color(0.60f, 0.38f, 0.18f)));
            }

            if (portalCategoryFilter == "all" || portalCategoryFilter == "land")
            {
                cardList.Add((TerraIcon.House, "Terra & Cia", "Mapa de glebas e imóveis rurais", "Imóveis", "Adquira chácaras, lotes e fazendas na região com escritura pública.", "landmap", new Color(0.20f, 0.45f, 0.22f)));
            }

            if (portalCategoryFilter == "all" || portalCategoryFilter == "services")
            {
                cardList.Add((TerraIcon.Bank, "TerraBank", "Internet banking e crédito rural", "Finanças", "Controle seu saldo, quite parcelas e contrate linhas de financiamento.", "svc:banco", new Color(0.08f, 0.45f, 0.30f)));
                cardList.Add((TerraIcon.Fuel, "Rede TerraFuel", "Postos e tele-abastecimento", "Combustível", "Abasteça seu veículo remotamente e confira os preços na estrada.", "svc:posto", new Color(0.85f, 0.30f, 0.12f)));
                cardList.Add((TerraIcon.TrendingUp, "AgroBolsa", "Cotações diárias de commodities", "Mercado", "Preços atualizados em tempo real de soja, milho, gado e diesel.", "svc:mercado", new Color(0.75f, 0.55f, 0.12f)));
                cardList.Add((TerraIcon.Newspaper, "Gazeta Regional", "Notícias e acontecimentos locais", "Editorial", "Manchetes do agronegócio, estado das rodovias e mercado regional.", "svc:noticias", new Color(0.30f, 0.35f, 0.45f)));
                cardList.Add((TerraIcon.ReputationStar, "Metas da Campanha", "Programa de recompensas e conquistas", "Desafios", "Cumpra objetivos operacionais e fature bônus financeiros.", "svc:metas", new Color(0.45f, 0.25f, 0.70f)));
                cardList.Add((TerraIcon.Compass, "Guia de Rodovias", "Rotas, distâncias e assistência", "Transporte", "Tabela completa de distâncias quilométricas e telefones de socorro.", "svc:rodovias", new Color(0.18f, 0.40f, 0.65f)));
            }

            int i = 0;
            foreach (var item in cardList)
            {
                if (i % cols == 0) GUILayout.BeginHorizontal();

                Rect cRect = GUILayoutUtility.GetRect(cardW, cardH, GUILayout.Width(cardW), GUILayout.Height(cardH));
                DrawModernPortalCard(cRect, item.icon, item.title, item.subtitle, item.tag, item.desc, item.navTarget, item.accent);

                if (i % cols == cols - 1) GUILayout.EndHorizontal();
                else GUILayout.Space(gap);
                i++;
            }
            if (i % cols != 0) GUILayout.EndHorizontal();

            GUILayout.Space(30);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawModernPortalCard(Rect r, TerraIcon icon, string title, string subtitle, string tag, string desc, string target, Color accent)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, hover ? UiCardHover : UiCardBg);
            DrawOutline(r, hover ? UiBorderFocus : UiBorderLight);

            Fill(new Rect(r.x, r.y, r.width, 3f), accent);

            // Ícone nativo vetorial
            Rect iconBox = new Rect(r.x + 16f, r.y + 14f, 20f, 20f);
            TerraIcons.Draw(iconBox, icon, accent);

            // Tag/Badge Minimalista
            float tagW = brBadgeStyle.CalcSize(new GUIContent(tag)).x + 16f;
            Rect tagRect = new Rect(iconBox.xMax + 8f, r.y + 14f, tagW, 20f);
            Fill(tagRect, new Color(accent.r, accent.g, accent.b, 0.12f));
            GUI.Label(tagRect, tag, brBadgeStyle);

            GUI.Label(new Rect(r.x + 16f, r.y + 40f, r.width - 32f, 24f), title, brPageH2Style);
            GUI.Label(new Rect(r.x + 16f, r.y + 64f, r.width - 32f, 18f), subtitle, brSmallStyle);
            GUI.Label(new Rect(r.x + 16f, r.y + 86f, r.width - 32f, 38f), desc, brBodyStyle);

            Rect btnRect = new Rect(r.xMax - 100f, r.yMax - 34f, 86f, 26f);
            if (GUI.Button(btnRect, "Acessar →", brBuySecondaryStyle))
            {
                Navigate(target, true);
            }

            if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Navigate(target, true);
                Event.current.Use();
            }
        }

        #endregion

        #region Vitrine de Loja Moderna

        private void DrawModernStorePage(Rect page, ShopSite s)
        {
            if (s == null) return;

            float maxContainerW = Mathf.Min(1160f, page.width - 48f);
            float startX = page.x + (page.width - maxContainerW) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, page.y + 16f, maxContainerW, page.height - 24f));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            Rect header = GUILayoutUtility.GetRect(maxContainerW, 90f);
            Fill(header, UiCardBg);
            DrawOutline(header, UiBorderLight);

            Fill(new Rect(header.x, header.y, 4f, header.height), s.brand);

            GUI.Label(new Rect(header.x + 20f, header.y + 14f, header.width - 40f, 28f), s.title, brPageH1Style);
            GUI.Label(new Rect(header.x + 20f, header.y + 44f, header.width - 40f, 20f), s.subtitle, brBodyStyle);

            TerraIcons.Draw(new Rect(header.x + 20f, header.y + 68f, 14f, 14f), TerraIcon.Checkmark, UiSuccessGreen);
            GUI.Label(new Rect(header.x + 38f, header.y + 66f, header.width - 58f, 18f), s.tagline, brSmallStyle);

            GUILayout.Space(20);

            for (int idx = 0; idx < s.items.Length; idx++)
            {
                DrawModernItemCard(maxContainerW, s, idx);
                GUILayout.Space(14);
            }

            GUILayout.Space(24);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawModernItemCard(float containerW, ShopSite s, int idx)
        {
            var it = s.items[idx];
            float cardH = containerW >= 640f ? 140f : 230f;
            Rect card = GUILayoutUtility.GetRect(containerW, cardH);

            bool hover = card.Contains(Event.current.mousePosition);
            Fill(card, hover ? UiCardHover : UiCardBg);
            DrawOutline(card, hover ? UiBorderFocus : UiBorderLight);

            var save = CurrentSave;
            bool canAfford = save != null && save.currentMoney >= it.price;

            if (containerW >= 640f)
            {
                float imgW = 160f;
                Rect imgFrame = new Rect(card.x + 12f, card.y + 12f, imgW, cardH - 24f);
                DrawCleanProductImage(imgFrame, it);

                float btnColW = 160f;
                float tx = imgFrame.xMax + 18f;
                float infoW = card.width - (tx - card.x) - btnColW - 20f;

                GUI.Label(new Rect(tx, card.y + 12f, infoW, 24f), it.name, brPageH2Style);

                string specsStr = (it.specs != null && it.specs.Length > 0) ? string.Join("  •  ", it.specs) : "";
                GUI.Label(new Rect(tx, card.y + 38f, infoW, 20f), specsStr, brSmallStyle);

                GUI.Label(new Rect(tx, card.y + 62f, infoW, cardH - 74f), it.desc, brBodyStyle);

                float rx = card.xMax - btnColW - 16f;
                GUI.Label(new Rect(rx, card.y + 16f, btnColW, 26f), FormatPrice(it.price), brPriceStyle);

                if (GUI.Button(new Rect(rx, card.y + 50f, btnColW, 30f), "Ficha Técnica", brBuySecondaryStyle))
                {
                    Navigate($"item:{s.id}:{idx}", true);
                }

                GUI.enabled = canAfford;
                string buyTxt = canAfford ? "Comprar Agora" : "Saldo Insuficiente";
                if (GUI.Button(new Rect(rx, card.y + 88f, btnColW, 36f), buyTxt, brBuyPrimaryStyle))
                {
                    PurchaseItem(it, 0);
                }
                GUI.enabled = true;
            }
            else
            {
                float imgW = 140f;
                Rect imgFrame = new Rect(card.x + 12f, card.y + 12f, imgW, 95f);
                DrawCleanProductImage(imgFrame, it);

                float tx = imgFrame.xMax + 14f;
                float topW = card.width - tx - 12f;
                GUI.Label(new Rect(tx, card.y + 12f, topW, 22f), it.name, brPageH2Style);
                GUI.Label(new Rect(tx, card.y + 36f, topW, 24f), FormatPrice(it.price), brPriceStyle);

                if (GUI.Button(new Rect(tx, card.y + 66f, topW, 28f), "Detalhes", brBuySecondaryStyle))
                {
                    Navigate($"item:{s.id}:{idx}", true);
                }

                GUI.Label(new Rect(card.x + 14f, card.y + 115f, card.width - 28f, 55f), it.desc, brBodyStyle);

                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(card.x + 14f, card.yMax - 42f, card.width - 28f, 34f), canAfford ? "Comprar Agora" : "Saldo Insuficiente", brBuyPrimaryStyle))
                {
                    PurchaseItem(it, 0);
                }
                GUI.enabled = true;
            }
        }

        #endregion

        #region Ficha Técnica e Detalhes do Produto

        private void DrawModernItemPage(Rect page)
        {
            var parts = browserLoc.Split(':');
            var s = FindSite(parts[1]);
            var it = CurrentItem();
            if (s == null || it == null) return;

            float maxContainerW = Mathf.Min(1160f, page.width - 48f);
            float startX = page.x + (page.width - maxContainerW) * 0.5f;

            GUILayout.BeginArea(new Rect(startX, page.y + 16f, maxContainerW, page.height - 24f));
            browserScroll = GUILayout.BeginScrollView(browserScroll);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("← Voltar para a loja", brBuySecondaryStyle, GUILayout.Width(160), GUILayout.Height(30)))
            {
                Navigate("store:" + s.id, true);
            }
            GUILayout.Space(12);
            GUILayout.Label($"{s.title}  /  {it.name}", brSmallStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(16);

            Rect mainBox = GUILayoutUtility.GetRect(maxContainerW, 360f);
            Fill(mainBox, UiCardBg);
            DrawOutline(mainBox, UiBorderLight);

            if (maxContainerW >= 720f)
            {
                float col1W = Mathf.Clamp(maxContainerW * 0.44f, 280f, 440f);
                float col2W = maxContainerW - col1W - 40f;

                Rect imgFrame = new Rect(mainBox.x + 20f, mainBox.y + 20f, col1W, 240f);
                DrawCleanProductImage(imgFrame, it);

                if (it.colors != null && it.colors.Length > 0)
                {
                    GUI.Label(new Rect(mainBox.x + 20f, imgFrame.yMax + 14f, col1W, 20f), "Cores disponíveis:", brSmallStyle);
                    for (int c = 0; c < it.colors.Length; c++)
                    {
                        Rect cBox = new Rect(mainBox.x + 20f + c * 34f, imgFrame.yMax + 38f, 28f, 28f);
                        Fill(cBox, it.colors[c]);
                        DrawOutline(cBox, c == browserColorIdx ? UiPrimaryBlue : UiBorderLight);
                        if (c == browserColorIdx)
                        {
                            Fill(new Rect(cBox.x + 10, cBox.y + 10, 8, 8), Color.white);
                        }
                        if (GUI.Button(cBox, "", GUIStyle.none)) browserColorIdx = c;
                    }
                }

                float rx = imgFrame.xMax + 24f;
                float ry = mainBox.y + 20f;

                GUI.Label(new Rect(rx, ry, col2W, 30f), it.name, brPageH1Style); ry += 34f;
                GUI.Label(new Rect(rx, ry, col2W, 28f), FormatPrice(it.price), brPriceStyle); ry += 36f;
                GUI.Label(new Rect(rx, ry, col2W, 60f), it.desc, brBodyStyle); ry += 68f;

                if (it.specs != null && it.specs.Length > 0)
                {
                    GUI.Label(new Rect(rx, ry, col2W, 20f), "Especificações principais:", brPageH3Style); ry += 24f;
                    foreach (var sp in it.specs)
                    {
                        TerraIcons.Draw(new Rect(rx, ry + 3f, 13f, 13f), TerraIcon.Checkmark, UiSuccessGreen);
                        GUI.Label(new Rect(rx + 18f, ry, col2W - 18f, 20f), sp, brSmallStyle);
                        ry += 20f;
                    }
                    ry += 10f;
                }

                var save = CurrentSave;
                bool canAfford = save != null && save.currentMoney >= it.price;
                GUI.enabled = canAfford;
                string btnTxt = canAfford ? $"Comprar por {FormatPrice(it.price)}" : "Saldo bancário insuficiente";
                if (GUI.Button(new Rect(rx, ry, Mathf.Min(320f, col2W), 44f), btnTxt, brBuyPrimaryStyle))
                {
                    PurchaseItem(it, browserColorIdx);
                }
                GUI.enabled = true;
            }
            else
            {
                Rect imgFrame = new Rect(mainBox.x + 16f, mainBox.y + 16f, mainBox.width - 32f, 180f);
                DrawCleanProductImage(imgFrame, it);

                float py = imgFrame.yMax + 14f;
                GUI.Label(new Rect(mainBox.x + 16f, py, mainBox.width - 32f, 26f), it.name, brPageH1Style); py += 30f;
                GUI.Label(new Rect(mainBox.x + 16f, py, mainBox.width - 32f, 26f), FormatPrice(it.price), brPriceStyle); py += 34f;
                GUI.Label(new Rect(mainBox.x + 16f, py, mainBox.width - 32f, 50f), it.desc, brBodyStyle); py += 54f;

                var save = CurrentSave;
                bool canAfford = save != null && save.currentMoney >= it.price;
                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(mainBox.x + 16f, py, mainBox.width - 32f, 40f), canAfford ? "Comprar Agora" : "Saldo Insuficiente", brBuyPrimaryStyle))
                {
                    PurchaseItem(it, browserColorIdx);
                }
                GUI.enabled = true;
            }

            GUILayout.Space(24);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Compra de Produtos

        private void PurchaseItem(ShopItem it, int colorIdx)
        {
            var save = CurrentSave;
            if (save == null) { ShowToast("Sem partida ativa."); return; }
            if (save.currentMoney < it.price) { ShowToast("Saldo insuficiente."); return; }

            if (it.action.StartsWith("vehicle:"))
            {
                string type = it.action.Substring("vehicle:".Length);
                var vm = VehicleManager.Instance;
                if (vm == null) { ShowToast("Gerenciador de veículos indisponível."); return; }

                var spawned = vm.SpawnVehicle(type);
                if (spawned == null) { ShowToast("Não foi possível entregar o veículo."); return; }

                if (it.colors != null && it.colors.Length > 0)
                {
                    int ci = Mathf.Clamp(colorIdx, 0, it.colors.Length - 1);
                    spawned.SetBodyColor(it.colors[ci]);
                }

                save.currentMoney -= it.price;
                PersistSave();
                ShowToast($"Veículo {it.name} entregue na sua posição!");
            }
            else if (it.action.StartsWith("animal:"))
            {
                string sp = it.action.Substring("animal:".Length);
                if (!System.Enum.TryParse(sp, out AnimalSpecies species))
                { ShowToast("Espécie desconhecida."); return; }

                Vector3 pos = AnimalDeliveryPosition();
                var animal = SceneObjectSpawner.SpawnPurchasedAnimal(species, pos, Terrain.activeTerrain);
                if (animal == null) { ShowToast("Não foi possível entregar o animal."); return; }

                save.currentMoney -= it.price;
                PersistSave();
                ShowToast($"Animal {it.name} entregue na sua fazenda!");
            }
            else if (it.action == "land")
            {
                save.currentMoney -= it.price;
                save.reputation += 5;
                PersistSave();
                ShowToast($"Escritura emitida: {it.name} adquirida! (+5 Rep)");
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

        #region Utilitários Gráficos Clean

        private string FormatPrice(long p) => $"$ {p:N0}";

        private void Fill(Rect r, Color c)
        {
            if (r.width <= 4f || r.height <= 4f)
            {
                GUI.color = c;
                GUI.DrawTexture(r, whiteTex);
                GUI.color = Color.white;
            }
            else
            {
                float rad = Mathf.Min(8f, Mathf.Min(r.width, r.height) * 0.35f);
                UIThemeHelper.DrawRoundedBox(r, c, Color.clear, rad, 0f);
            }
        }

        private void DrawOutline(Rect r, Color c)
        {
            if (r.width <= 4f || r.height <= 4f)
            {
                GUI.color = c;
                GUI.DrawTexture(r, whiteTex);
                GUI.color = Color.white;
            }
            else
            {
                float rad = Mathf.Min(8f, Mathf.Min(r.width, r.height) * 0.35f);
                UIThemeHelper.DrawRoundedBorder(r, c, 1.2f, rad);
            }
        }

        private void DrawCleanProductImage(Rect frame, ShopItem it)
        {
            Fill(frame, new Color(0.95f, 0.96f, 0.98f));
            DrawOutline(frame, UiBorderLight);

            Rect in_ = new Rect(frame.x + 6, frame.y + 6, frame.width - 12, frame.height - 12);
            float cx = in_.x + in_.width * 0.5f;
            float groundY = in_.yMax - in_.height * 0.20f;

            Fill(new Rect(in_.x, groundY, in_.width, 1f), new Color(0.85f, 0.86f, 0.90f));

            float uw = in_.width * 0.70f;
            float ux = cx - uw * 0.5f;
            Color body = it.artA, dark = it.artB;

            switch (it.art)
            {
                case ShopArt.Car:
                {
                    float bodyH = in_.height * 0.22f;
                    float by = groundY - bodyH - in_.height * 0.06f;
                    Fill(new Rect(ux, by, uw, bodyH), body);
                    Fill(new Rect(ux + uw * 0.22f, by - bodyH * 0.7f, uw * 0.5f, bodyH * 0.8f), body);
                    Fill(new Rect(ux + uw * 0.26f, by - bodyH * 0.55f, uw * 0.42f, bodyH * 0.55f), new Color(0.75f, 0.85f, 0.95f));
                    DrawWheel(ux + uw * 0.2f, groundY - in_.height * 0.04f, in_.height * 0.13f, dark);
                    DrawWheel(ux + uw * 0.8f, groundY - in_.height * 0.04f, in_.height * 0.13f, dark);
                    break;
                }
                case ShopArt.Truck:
                {
                    float bodyH = in_.height * 0.30f;
                    float by = groundY - bodyH - in_.height * 0.07f;
                    Fill(new Rect(ux, by, uw * 0.55f, bodyH), new Color(0.88f, 0.88f, 0.90f));
                    Fill(new Rect(ux + uw * 0.55f, by + bodyH * 0.25f, uw * 0.45f, bodyH * 0.75f), body);
                    Fill(new Rect(ux + uw * 0.78f, by + bodyH * 0.35f, uw * 0.18f, bodyH * 0.4f), new Color(0.75f, 0.85f, 0.95f));
                    DrawWheel(ux + uw * 0.18f, groundY - in_.height * 0.04f, in_.height * 0.14f, dark);
                    DrawWheel(ux + uw * 0.62f, groundY - in_.height * 0.04f, in_.height * 0.14f, dark);
                    DrawWheel(ux + uw * 0.86f, groundY - in_.height * 0.04f, in_.height * 0.14f, dark);
                    break;
                }
                case ShopArt.Tractor:
                {
                    float by = groundY - in_.height * 0.30f;
                    Fill(new Rect(ux + uw * 0.3f, by, uw * 0.5f, in_.height * 0.22f), body);
                    Fill(new Rect(ux + uw * 0.34f, by - in_.height * 0.14f, uw * 0.28f, in_.height * 0.16f), body);
                    DrawWheel(ux + uw * 0.72f, groundY - in_.height * 0.02f, in_.height * 0.20f, dark);
                    DrawWheel(ux + uw * 0.3f, groundY - in_.height * 0.02f, in_.height * 0.11f, dark);
                    break;
                }
                case ShopArt.Plane:
                {
                    float cy = in_.y + in_.height * 0.42f;
                    Fill(new Rect(ux, cy, uw * 0.78f, in_.height * 0.14f), body);
                    Fill(new Rect(ux + uw * 0.3f, cy - in_.height * 0.16f, uw * 0.14f, in_.height * 0.46f), dark);
                    Fill(new Rect(ux + uw * 0.74f, cy - in_.height * 0.1f, uw * 0.1f, in_.height * 0.22f), dark);
                    Fill(new Rect(ux + uw * 0.04f, cy + in_.height * 0.02f, uw * 0.12f, in_.height * 0.08f), new Color(0.75f, 0.85f, 0.95f));
                    break;
                }
                case ShopArt.Boat:
                {
                    float wline = groundY;
                    Fill(new Rect(in_.x, wline, in_.width, in_.yMax - wline), new Color(0.80f, 0.90f, 0.98f));
                    float hy = wline - in_.height * 0.14f;
                    Fill(new Rect(ux, hy, uw, in_.height * 0.16f), body);
                    Fill(new Rect(ux + uw * 0.3f, hy - in_.height * 0.16f, uw * 0.4f, in_.height * 0.16f), new Color(0.95f, 0.95f, 0.97f));
                    break;
                }
                case ShopArt.Animal:
                {
                    float by = groundY - in_.height * 0.26f;
                    Fill(new Rect(ux + uw * 0.2f, by, uw * 0.5f, in_.height * 0.2f), body);
                    Fill(new Rect(ux + uw * 0.62f, by - in_.height * 0.1f, uw * 0.18f, in_.height * 0.16f), body);
                    Fill(new Rect(ux + uw * 0.26f, by + in_.height * 0.18f, uw * 0.05f, in_.height * 0.12f), dark);
                    Fill(new Rect(ux + uw * 0.58f, by + in_.height * 0.18f, uw * 0.05f, in_.height * 0.12f), dark);
                    break;
                }
                case ShopArt.Land:
                {
                    Fill(new Rect(in_.x, groundY - in_.height * 0.12f, in_.width, in_.yMax - (groundY - in_.height * 0.12f)), it.artB);
                    break;
                }
            }
        }

        private void DrawWheel(float cx, float cy, float r, Color c)
        {
            Fill(new Rect(cx - r, cy - r, r * 2, r * 2), c);
            Fill(new Rect(cx - r * 0.4f, cy - r * 0.4f, r * 0.8f, r * 0.8f), new Color(0.85f, 0.85f, 0.88f));
        }

        #endregion

        #region Estilos Modernos

        private void InitBrowserStyles()
        {
            if (browserStylesReady) return;

            brTabActiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            brTabActiveStyle.normal.textColor = UiTextPrimary;

            brTabInactiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            brTabInactiveStyle.normal.textColor = UiTextSecondary;

            brWinBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };

            brToolBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            brAddrStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(8, 8, 4, 4)
            };
            brAddrStyle.normal.background = UIThemeHelper.CreateRoundedTexture(Color.white, UiBorderLight, 1f, 6f, 32);
            brAddrStyle.normal.textColor = UiTextPrimary;

            brBookmarkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            brBookmarkStyle.normal.textColor = UiTextSecondary;

            brBookmarkActiveStyle = new GUIStyle(brBookmarkStyle)
            {
                fontStyle = FontStyle.Bold
            };
            brBookmarkActiveStyle.normal.textColor = UiPrimaryBlue;

            brPageH1Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            brPageH1Style.normal.textColor = UiTextPrimary;

            brPageH2Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            brPageH2Style.normal.textColor = UiTextPrimary;

            brPageH3Style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            brPageH3Style.normal.textColor = UiTextSecondary;

            brBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            brBodyStyle.normal.textColor = UiTextSecondary;

            brSmallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            brSmallStyle.normal.textColor = UiTextMuted;

            brPriceStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            brPriceStyle.normal.textColor = UiSuccessGreen;

            brBuyPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyRoundedStyle(brBuyPrimaryStyle,
                normalBg: UiPrimaryBlue, normalBorder: UiPrimaryBlueHover,
                hoverBg: UiPrimaryBlueHover, hoverBorder: UiBorderFocus,
                activeBg: new Color(0.06f, 0.28f, 0.70f), activeBorder: UiBorderFocus,
                radius: 6f, borderThickness: 1f, sliceBorder: 6);
            brBuyPrimaryStyle.normal.textColor = Color.white;

            brBuySecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyRoundedStyle(brBuySecondaryStyle,
                normalBg: Color.white, normalBorder: UiBorderLight,
                hoverBg: UiCardHover, hoverBorder: UiBorderFocus,
                activeBg: new Color(0.92f, 0.94f, 0.98f), activeBorder: UiBorderFocus,
                radius: 6f, borderThickness: 1f, sliceBorder: 6);
            brBuySecondaryStyle.normal.textColor = UiTextPrimary;

            brBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            brBadgeStyle.normal.textColor = UiPrimaryBlue;

            brFilterChipStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyRoundedStyle(brFilterChipStyle,
                normalBg: Color.white, normalBorder: UiBorderLight,
                hoverBg: UiCardHover, hoverBorder: UiBorderFocus,
                activeBg: new Color(0.92f, 0.94f, 0.98f), activeBorder: UiBorderFocus,
                radius: 12f, borderThickness: 1f, sliceBorder: 6);
            brFilterChipStyle.normal.textColor = UiTextSecondary;

            brFilterChipActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyRoundedStyle(brFilterChipActiveStyle,
                normalBg: UiPrimaryBlue, normalBorder: UiPrimaryBlueHover,
                hoverBg: UiPrimaryBlueHover, hoverBorder: UiBorderFocus,
                activeBg: new Color(0.06f, 0.28f, 0.70f), activeBorder: UiBorderFocus,
                radius: 12f, borderThickness: 1f, sliceBorder: 6);
            brFilterChipActiveStyle.normal.textColor = Color.white;

            browserStylesReady = true;
        }

        #endregion
    }
}
