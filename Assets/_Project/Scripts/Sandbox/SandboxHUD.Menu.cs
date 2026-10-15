using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Estado do Celular (Smartphone estilo GTA V)

        /// <summary>Apps/telas disponíveis dentro do celular do personagem.</summary>
        public enum PhoneApp
        {
            Home,
            Jobs, // Mercado de Fretes (FS/ETS2) + contrato ativo
            Finances, // Finanças e Banco (empréstimos)
            Licenses, // Licenças / Habilitações
            Garage, // Garagem / Frota
            Dealer, // Concessionária / Loja
            FuelStation, // Posto de combustível
            GoalCenter, // Metas de campanha
            Stats, // Estatísticas
            Construction, // Menu de Construção e Obras
            Internet,
            Contacts,
            Messages, // Mensagens / SMS da campanha
            Settings,
            GameSystem
        }

        // App atualmente aberto na tela do celular.
        private PhoneApp currentApp = PhoneApp.Home;

        // Progresso da animação de deslizar (0 = escondido, 1 = totalmente levantado).
        private float phoneAnim = 0f;

        // Dimensões do aparelho (canto inferior direito).
        private const float PhoneWidth = 300f;
        private const float PhoneHeight = 600f;

        // Scroll das telas de app mais longas.
        private Vector2 phoneScroll = Vector2.zero;

        // Feedback de "página aberta"no app Internet.
        private string internetPage = "home";

        // Atalho para o save ativo (economia/progressão).
        private RegionSaveData CurrentSave =>
            SaveManager.Instance != null ? SaveManager.Instance.ActiveSave : activeSave;

        private void PersistSave()
        {
            var s = CurrentSave;
            if (s != null && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveToFile(s);
            }
        }

        /// <summary>
        /// Licença efetiva do jogador (perícia global do perfil ∪ carreira local do save).
        /// Passa pelo SaveManager quando disponível; senão cai no save atual.
        /// </summary>
        private bool PlayerHasLicense(ProjectTerra.Gameplay.DriverLicense lic)
        {
            if (SaveManager.Instance != null) return SaveManager.Instance.HasLicense(lic);
            var s = CurrentSave;
            return s == null || s.HasLicense(lic);
        }

        #endregion

        #region Gestão do Celular / Menu (API pública preservada)

        /// <summary>Saca/guarda o celular ([Alt Esquerdo]).</summary>
        public void ToggleOptionsMenu()
        {
            if (IsSettingsOpen) CloseSettingsMenu();
            IsPhoneOpen = !IsPhoneOpen;
            if (IsPhoneOpen)
            {
                if (IsRegionalMapOpen) IsRegionalMapOpen = false;
                // O mundo NÃO pausa: o personagem apenas saca o celular, como em GTA V.
                // O input de gameplay segue bloqueado pela flag IsOptionsMenuOpen,
                // então o veículo não dispara sozinho enquanto o cursor opera a tela.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                currentApp = PhoneApp.Home;
                internetPage = "home";
                phoneScroll = Vector2.zero;
            }
            else
            {
                CloseOptionsMenu();
            }
        }

        /// <summary>Abre/fecha o menu de configurações ([Esc]).</summary>
        public void ToggleSettingsMenu()
        {
            if (IsSettingsOpen)
            {
                CloseSettingsMenu();
                return;
            }
            IsPhoneOpen = false;
            if (IsRegionalMapOpen) IsRegionalMapOpen = false;
            IsSettingsOpen = true;
            settingsScroll = Vector2.zero;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseSettingsMenu()
        {
            IsSettingsOpen = false;
            RestoreGameplayCursor();
        }

        /// <summary>Fecha celular e configurações.</summary>
        public void CloseOptionsMenu()
        {
            IsPhoneOpen = false;
            IsSettingsOpen = false;
            IsConstructionBarOpen = false;
            RestoreGameplayCursor();
        }

        /// <summary>Volta para a tela anterior do celular, ou guarda o aparelho se já estiver na inicial.</summary>
        public void PhoneBack()
        {
            if (IsPhoneOpen && currentApp != PhoneApp.Home)
            {
                // Finanças e Posto são abertos pela Internet: voltam para lá.
                if (currentApp == PhoneApp.Finances || currentApp == PhoneApp.FuelStation || currentApp == PhoneApp.GoalCenter)
                {
                    currentApp = PhoneApp.Internet;
                    internetPage = "home";
                }
                else
                {
                    currentApp = PhoneApp.Home;
                }
                phoneScroll = Vector2.zero;
            }
            else
            {
                ToggleOptionsMenu();
            }
        }

        private void OpenApp(PhoneApp app)
        {
            if (app == PhoneApp.Internet)
            {
                OpenVehicleBrowser(); // tela 2D cheia com os sites (guarda o celular)
                return;
            }
            if (app == PhoneApp.Construction)
            {
                OpenConstructionBar(); // retrai o celular e abre barra horizontal inferior
                return;
            }
            currentApp = app;
            phoneScroll = Vector2.zero;
        }


        public void SaveCurrentGame()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                SaveManager.Instance.SaveToFile(SaveManager.Instance.ActiveSave);
                saveToastMessage = "[OK] Partida salva com sucesso!";
                saveToastTimer = 3.5f;
            }
        }

        public void ReturnToGlobe()
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            if (SaveManager.Instance != null && SaveManager.Instance.ActiveSave != null)
            {
                SaveManager.Instance.SaveToFile(SaveManager.Instance.ActiveSave);
            }
            SceneManager.LoadScene("MainEarthScene");
        }

        #endregion

        #region Renderização do Celular [Alt Esquerdo]

        private void DrawPhone()
        {
            float ease = Mathf.SmoothStep(0f, 1f, phoneAnim);

            // Leve escurecimento do fundo (o mundo continua visível atrás do aparelho).
            GUI.color = new Color(0f, 0f, 0f, 0.38f * ease);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float w = PhoneWidth;
            float h = Mathf.Min(PhoneHeight, Screen.height - 40f);
            float x = Screen.width - w - 28f;
            float yRest = Screen.height - h - 22f;
            // Desliza de baixo para cima.
            float y = Mathf.Lerp(Screen.height + 30f, yRest, ease);

            Rect body = new Rect(x, y, w, h);

            // Corpo do aparelho com cantos arredondados modernos (smartphone contemporâneo)
            UIThemeHelper.DrawRoundedBox(body, new Color(0.02f, 0.02f, 0.035f, 0.99f), new Color(0.25f, 0.7f, 1f, 0.9f), 18f, 2f);

            // Alto-falante / câmera (detalhe no topo arredondado).
            UIThemeHelper.DrawRoundedBox(new Rect(body.x + w * 0.5f - 22f, body.y + 7f, 44f, 4f), new Color(0.12f, 0.14f, 0.18f, 1f), Color.clear, 2f, 0f);
            UIThemeHelper.DrawRoundedBox(new Rect(body.x + w * 0.5f + 28f, body.y + 7f, 5f, 5f), new Color(0.12f, 0.14f, 0.18f, 1f), Color.clear, 2.5f, 0f);

            // Tela interna com cantos arredondados suaves.
            float pad = 9f;
            float screenTop = body.y + 18f;
            float homeBarH = 24f;
            Rect screenRect = new Rect(body.x + pad, screenTop, w - pad * 2f, body.height - (screenTop - body.y) - homeBarH);
            UIThemeHelper.DrawRoundedBox(screenRect, new Color(0.06f, 0.09f, 0.14f, 1f), new Color(0.15f, 0.25f, 0.40f, 0.40f), 12f, 1f);

            // Barra de status (hora + sinal + bateria).
            DrawPhoneStatusBar(screenRect);

            // Conteúdo abaixo da barra de status.
            Rect content = new Rect(screenRect.x + 8f, screenRect.y + 24f, screenRect.width - 16f, screenRect.height - 32f);

            switch (currentApp)
            {
                case PhoneApp.Home: DrawPhoneHome(content); break;
                case PhoneApp.Jobs: DrawPhoneJobs(content); break;
                case PhoneApp.Finances: DrawPhoneFinances(content); break;
                case PhoneApp.Licenses: DrawPhoneLicenses(content); break;
                case PhoneApp.Garage: DrawPhoneGarage(content); break;
                case PhoneApp.Dealer: DrawPhoneDealer(content); break;
                case PhoneApp.FuelStation: DrawPhoneFuelStation(content); break;
                case PhoneApp.GoalCenter: DrawPhoneGoals(content); break;
                case PhoneApp.Stats: DrawPhoneStats(content); break;
                case PhoneApp.Construction: DrawPhoneConstruction(content); break;
                case PhoneApp.Internet: DrawPhoneInternet(content); break;
                case PhoneApp.Contacts: DrawPhoneContacts(content); break;
                case PhoneApp.Messages: DrawPhoneMessages(content); break;
                case PhoneApp.Settings: DrawPhoneHome(content); break;
                case PhoneApp.GameSystem: DrawPhoneGameSystem(content); break;
            }

            // Barra "home"inferior com pílula arredondada: toque para voltar à tela inicial / guardar o celular.
            Rect homeBar = new Rect(body.x, body.y + body.height - homeBarH, w, homeBarH);
            UIThemeHelper.DrawRoundedBox(new Rect(homeBar.x + w * 0.5f - 45f, homeBar.y + homeBarH - 8f, 90f, 4f), new Color(0.4f, 0.55f, 0.7f, 0.55f), Color.clear, 2f, 0f);
            if (GUI.Button(homeBar, "", GUIStyle.none))
            {
                PhoneBack();
            }
        }

        private void DrawPhoneStatusBar(Rect screenRect)
        {
            string timeStr = FormattedInGameTimeString;

            Rect barRect = new Rect(screenRect.x + 8f, screenRect.y + 4f, screenRect.width - 16f, 18f);
            GUI.Label(new Rect(barRect.x, barRect.y, 80f, barRect.height), timeStr, phoneStatusStyle);

            Rect rightRect = new Rect(barRect.x + barRect.width - 90f, barRect.y, 90f, barRect.height);
            GUI.Label(rightRect, "TerraNet 4G 87%", phoneStatusRightStyle);

            // Linha separadora.
            GUI.color = new Color(0.25f, 0.7f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(screenRect.x + 6f, screenRect.y + 23f, screenRect.width - 12f, 1f), whiteTex);
            GUI.color = Color.white;
        }

        // ---- Tela inicial: grade de apps ----
        private static readonly string[] HomeLabels = { "Mapa", "Frete", "Finanças", "Licenças", "Garagem", "Loja", "Estatíst.", "Construção", "Internet", "Contatos", "Mensagens", "Jogo" };
        private static readonly PhoneApp[] HomeTargets =
        {
            PhoneApp.Home, PhoneApp.Jobs, PhoneApp.Finances, PhoneApp.Licenses, PhoneApp.Garage,
            PhoneApp.Dealer, PhoneApp.Stats, PhoneApp.Construction, PhoneApp.Internet, PhoneApp.Contacts, PhoneApp.Messages, PhoneApp.GameSystem
        };

        // Ícones (PNG Lucide via TerraIcons) na mesma ordem de HomeLabels/HomeTargets.
        private static readonly ProjectTerra.UI.TerraIcon[] HomeIcons =
        {
            ProjectTerra.UI.TerraIcon.LocationPin,   // Mapa
            ProjectTerra.UI.TerraIcon.CargoBox,      // Frete
            ProjectTerra.UI.TerraIcon.Wallet,        // Finanças
            ProjectTerra.UI.TerraIcon.DriverLicense, // Licenças
            ProjectTerra.UI.TerraIcon.Tools,         // Garagem
            ProjectTerra.UI.TerraIcon.Shop,          // Loja
            ProjectTerra.UI.TerraIcon.Analytics,     // Estatísticas
            ProjectTerra.UI.TerraIcon.Tools,         // Obras / Construções
            ProjectTerra.UI.TerraIcon.Satellite,     // Internet
            ProjectTerra.UI.TerraIcon.PhoneCall,     // Contatos
            ProjectTerra.UI.TerraIcon.Phone,         // Mensagens
            ProjectTerra.UI.TerraIcon.Flag           // Jogo
        };

        private void DrawPhoneHome(Rect content)
        {
            float headerH = 96f; // relógio + região + saldo sem sobrepor a grade
            Rect headerBoxRect = new Rect(content.x, content.y, content.width, headerH - 4f);
            UIThemeHelper.DrawRoundedBox(headerBoxRect, new Color(0.04f, 0.08f, 0.14f, 0.65f), new Color(0.20f, 0.50f, 0.80f, 0.35f), 10f, 1.2f);

            GUILayout.BeginArea(new Rect(content.x, content.y + 4f, content.width, headerH - 8f));
            GUILayout.Space(1);
            string timeStr = FormattedInGameTimeString;
            GUILayout.Label(timeStr, phoneClockWidgetStyle);
            string reg = CurrentSave != null ? CurrentSave.regionName : "Região";
            string country = CurrentSave != null ? CurrentSave.countryName : "Mundo";
            GUILayout.Label($"{reg}, {country}", phoneClockSubStyle);
            GUILayout.Space(2);
            string money = CurrentSave != null ? CurrentSave.GetFormattedMoney() : "$500,000";
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(money, moneyStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Finanças e Loja ficam dentro do app Internet: fora da grade inicial.
            var visible = new System.Collections.Generic.List<int>();
            for (int k = 0; k < HomeTargets.Length; k++)
            {
                if (HomeTargets[k] == PhoneApp.Finances || HomeTargets[k] == PhoneApp.Dealer) continue;
                visible.Add(k);
            }

            // Grade 3 colunas, dimensionada para caber toda a grade perfeitamente dentro da tela (3x3 = 9 apps).
            const int cols = 3;
            float gap = 8f;
            float gridTop = content.y + headerH + 4f;
            float gridAvail = content.height - headerH - 8f;
            int count = visible.Count;
            int rows = Mathf.CeilToInt(count / (float)cols);

            const float labelBand = 26f; // espaço do texto abaixo do ícone
            float cellW = (content.width - gap * (cols - 1)) / cols;
            float maxTileByHeight = (gridAvail - gap * (rows - 1)) / rows - labelBand;
            float tile = Mathf.Max(24f, Mathf.Min(cellW * 0.62f, maxTileByHeight));
            float cellH = tile + labelBand;

            Color[] palette =
            {
                new Color(0.18f, 0.55f, 0.35f), // Mapa
                new Color(0.72f, 0.45f, 0.15f), // Frete
                new Color(0.20f, 0.60f, 0.35f), // Finanças
                new Color(0.45f, 0.35f, 0.70f), // Licenças
                new Color(0.30f, 0.45f, 0.65f), // Garagem
                new Color(0.65f, 0.30f, 0.30f), // Loja
                new Color(0.25f, 0.55f, 0.60f), // Estatísticas
                new Color(0.85f, 0.55f, 0.20f), // Obras / Construções
                new Color(0.18f, 0.42f, 0.75f), // Internet
                new Color(0.60f, 0.42f, 0.18f), // Contatos
                new Color(0.22f, 0.55f, 0.72f), // Mensagens
                new Color(0.55f, 0.25f, 0.55f)  // Jogo
            };

            for (int vi = 0; vi < visible.Count; vi++)
            {
                int i = visible[vi];
                int row = vi / cols;
                int col = vi % cols;
                float ix = content.x + col * (cellW + gap);
                float iy = gridTop + row * (cellH + gap);
                Rect cellRect = new Rect(ix, iy, cellW, cellH);

                if (DrawAppIcon(cellRect, tile, HomeIcons[i], HomeLabels[i], palette[i]))
                {
                    if (HomeTargets[i] == PhoneApp.Home) // Mapa: fecha o celular e abre o mapa tático
                    {
                        CloseOptionsMenu();
                        ToggleRegionalMap();
                    }
                    else if (HomeTargets[i] == PhoneApp.Dealer) // Loja: abre o navegador retrô em tela cheia
                    {
                        OpenVehicleBrowser();
                    }
                    else
                    {
                        OpenApp(HomeTargets[i]);
                    }
                }
            }
        }

        private bool DrawAppIcon(Rect cell, float tile, ProjectTerra.UI.TerraIcon icon, string label, Color tileColor)
        {
            float tileX = cell.x + (cell.width - tile) * 0.5f;
            Rect tileRect = new Rect(tileX, cell.y, tile, tile);

            // Borda arredondada suave e moderna no ícone de cada app do celular
            UIThemeHelper.DrawRoundedBox(tileRect, tileColor, new Color(1f, 1f, 1f, 0.25f), 10f, 1.2f);

            float inset = tile * 0.2f;
            ProjectTerra.UI.TerraIcons.Draw(new Rect(tileRect.x + inset, tileRect.y + inset, tile - inset * 2f, tile - inset * 2f), icon, Color.white);

            // Altura generosa + respiro acima: evita cortar as "pernas" das letras (g, p, q, j).
            GUI.Label(new Rect(cell.x - 4f, cell.y + tile + 3f, cell.width + 8f, 20f), label, phoneAppLabelStyle);

            return GUI.Button(cell, "", GUIStyle.none);
        }

        // ---- Cabeçalho comum dos apps (seta voltar + título) ----
        private void DrawAppHeader(Rect content, string title)
        {
            if (embeddedInBrowser) return;
            Rect backRect = new Rect(content.x, content.y, 34f, 22f);
            if (GUI.Button(backRect, "◀", phoneBackStyle))
            {
                PhoneBack();
            }
            GUI.Label(new Rect(content.x + 38f, content.y, content.width - 38f, 22f), title, phoneHeaderStyle);
            GUI.color = new Color(0.25f, 0.7f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(content.x, content.y + 26f, content.width, 1f), whiteTex);
            GUI.color = Color.white;
        }

        // ---- App: Internet (navegador fictício TerraNet) ----
        private void DrawPhoneInternet(Rect content)
        {
            DrawAppHeader(content, "TerraNet");
            Rect area = new Rect(content.x + 8f, content.y + 34f, content.width - 16f, content.height - 38f);
            GUILayout.BeginArea(area);

            GUI.color = new Color(1f, 1f, 1f, 0.08f);
            GUILayout.Box("[Bloqueado] www.terranet.world", phoneUrlStyle, GUILayout.Height(24));
            GUI.color = Color.white;
            GUILayout.Space(12);

            if (internetPage == "home")
            {
                GUILayout.Label("Portais Disponíveis", phoneSectionStyle);
                GUILayout.Space(8);

                if (GUILayout.Button("Notícias da Região", phoneListButtonStyle, GUILayout.Height(34))) internetPage = "news";
                GUILayout.Space(8);
                if (GUILayout.Button("Mercado Agrícola", phoneListButtonStyle, GUILayout.Height(34))) internetPage = "market";
                GUILayout.Space(8);
                if (GUILayout.Button("Loja (Veículos, Terrenos, Animais)", phoneListButtonStyle, GUILayout.Height(34)))
                {
                    OpenVehicleBrowser();
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Finanças e Banco", phoneListButtonStyle, GUILayout.Height(34)))
                {
                    OpenApp(PhoneApp.Finances);
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Posto de Combustível", phoneListButtonStyle, GUILayout.Height(34)))
                {
                    OpenApp(PhoneApp.FuelStation);
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Metas da Campanha", phoneListButtonStyle, GUILayout.Height(34)))
                {
                    OpenApp(PhoneApp.GoalCenter);
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Menu de Obras & Construções", phoneListButtonStyle, GUILayout.Height(34)))
                {
                    OpenApp(PhoneApp.Construction);
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Guia de Rodovias", phoneListButtonStyle, GUILayout.Height(34))) internetPage = "roads";
            }
            else
            {
                if (GUILayout.Button("◀ Voltar aos portais", phoneLinkStyle, GUILayout.Height(24))) internetPage = "home";
                GUILayout.Space(10);

                string reg = activeSave != null ? activeSave.regionName : "a região";
                string country = activeSave != null ? activeSave.countryName : "o país";

                GUILayout.BeginVertical(phoneCardStyle ?? GUI.skin.box);
                if (internetPage == "news")
                {
                    GUILayout.Label("Notícias Regionais", phoneSectionStyle);
                    GUILayout.Space(6);
                    GUILayout.Label($"Clima estável em {reg}. Produtores de {country} relatam excelente safra nesta temporada.", phoneBodyStyle);
                    GUILayout.Space(10);
                    GUILayout.Label("Obras de pavimentação e recapeamento avançam nas rodovias regionais.", phoneBodyStyle);
                }
                else if (internetPage == "market")
                {
                    GUILayout.Label("Mercado Agrícola", phoneSectionStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("Grãos: [Alta] em alta\nFretes: estáveis\nCombustível: moderado\nInsumos: estáveis", phoneBodyStyle);
                }
                else if (internetPage == "roads")
                {
                    GUILayout.Label("Guia de Rodovias", phoneSectionStyle);
                    GUILayout.Space(6);
                    GUILayout.Label("Abra o app Mapa para traçar rotas, acompanhar relevo e marcar waypoints com um clique.", phoneBodyStyle);
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndArea();
        }

        // ---- App: Contatos ----
        private void DrawPhoneContacts(Rect content)
        {
            DrawAppHeader(content, "Contatos");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);

            string[] names = { "Mecânico", "Cooperativa", "Concessionária", "Capataz", "Emergência" };

            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);
            foreach (var n in names)
            {
                if (GUILayout.Button(n, phoneListButtonStyle, GUILayout.Height(32)))
                {
                    ShowToast($"Ligando para {n.Substring(n.IndexOf(' ') + 1)}...");
                }
                GUILayout.Space(4);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // ---- App: Jogo (salvar / sair) ----
        private void DrawPhoneGameSystem(Rect content)
        {
            DrawAppHeader(content, "Jogo");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            string reg = activeSave != null ? activeSave.regionName : "Região";
            string country = activeSave != null ? activeSave.countryName : "Mundo";

            GUILayout.BeginVertical(phoneCardStyle);
            GUILayout.Label("SESSÃO ATUAL", phoneSectionStyle);
            GUILayout.Label($"{reg}, {country}", phoneBodyStyle);
            GUILayout.Label(activeSave != null ? $"Dia {activeSave.gameDay} • Saldo {activeSave.GetFormattedMoney()}" : "Partida ativa", phoneSmallStyle);
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginVertical(phoneCardStyle);
            GUILayout.Label("AÇÕES", phoneSectionStyle);
            if (GUILayout.Button("Continuar", menuButtonPrimaryStyle, GUILayout.Height(34)))
            {
                CloseOptionsMenu();
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Salvar Jogo", menuButtonStyle, GUILayout.Height(32)))
            {
                SaveCurrentGame();
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Retornar ao Globo", menuButtonStyle, GUILayout.Height(32)))
            {
                ReturnToGlobe();
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Salvar e Sair", menuButtonDangerStyle, GUILayout.Height(32)))
            {
                ReturnToGlobe();
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion

        #region Menu de Configurações [Esc]

        private Vector2 settingsScroll = Vector2.zero;

        private void DrawSettingsMenu()
        {
            // Escurece o fundo.
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float w = 440f;
            float h = Mathf.Min(580f, Screen.height - 40f);
            Rect panel = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            // Painel modal com bordas arredondadas e glow ciano
            UIThemeHelper.DrawRoundedBox(panel, new Color(0.04f, 0.07f, 0.12f, 0.98f), new Color(0.25f, 0.7f, 1f, 0.9f), 14f, 1.5f);

            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, w - 32f, 28f), "CONFIGURAÇÕES DO JOGO", phoneHeaderStyle);

            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 44f, w - 32f, h - 56f));
            settingsScroll = GUILayout.BeginScrollView(settingsScroll);

            // Caixa 1: Áudio
            GUILayout.BeginVertical(menuCardSectionStyle);
            GUILayout.Label($"Áudio • Volume Geral: {Mathf.RoundToInt(AudioListener.volume * 100f)}%", phoneSectionStyle);
            AudioListener.volume = GUILayout.HorizontalSlider(AudioListener.volume, 0f, 1f);
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Caixa 2: Interface (HUD)
            GUILayout.BeginVertical(menuCardSectionStyle);
            GUILayout.Label("Interface (HUD)", phoneSectionStyle);
            ShowCompass = GUILayout.Toggle(ShowCompass, " Bússola 360°");
            GUILayout.Space(3);
            ShowMissionTracker = GUILayout.Toggle(ShowMissionTracker, " Rastreador de Missões");
            GUILayout.Space(3);
            ShowTelemetry = GUILayout.Toggle(ShowTelemetry, " Telemetria do Veículo");
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Caixa 3: Qualidade Gráfica
            GUILayout.BeginVertical(menuCardSectionStyle);
            {
                string[] qualityNames = QualitySettings.names;
                // Nomes em PT-BR alinhados com os níveis do projeto (ver ProjectSettings/QualitySettings.asset).
                // Usa o nome bruto da Unity como fallback caso a quantidade de níveis mude.
                string[] qualityNamesPt = { "Muito Baixo", "Baixo", "Médio", "Alto", "Muito Alto", "Ultra" };
                int currentQuality = QualitySettings.GetQualityLevel();

                string QualityLabel(int i)
                {
                    if (i < 0 || i >= qualityNames.Length) return "?";
                    return qualityNames.Length == qualityNamesPt.Length ? qualityNamesPt[i] : qualityNames[i];
                }

                GUILayout.Label($"Gráficos • Qualidade: {QualityLabel(currentQuality)}", phoneSectionStyle);

                Color prevColor = GUI.color;
                for (int qi = 0; qi < qualityNames.Length; qi++)
                {
                    bool isActive = qi == currentQuality;
                    GUI.color = isActive ? new Color(0.45f, 0.85f, 1f) : prevColor;
                    string label = isActive ? $"► {QualityLabel(qi)}" : QualityLabel(qi);
                    if (GUILayout.Button(label, phoneListButtonStyle, GUILayout.Height(26)) && !isActive)
                    {
                        QualitySettings.SetQualityLevel(qi, true);
                    }
                }
                GUI.color = prevColor;
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);

            // Caixa 4: Ações da Sessão
            GUILayout.BeginVertical(menuCardSectionStyle);
            GUILayout.Label("Sessão & Jogo", phoneSectionStyle);
            if (GUILayout.Button("Continuar", menuButtonPrimaryStyle, GUILayout.Height(34)))
            {
                CloseSettingsMenu();
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Salvar Jogo", menuButtonStyle, GUILayout.Height(32)))
            {
                SaveCurrentGame();
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Retornar ao Globo", menuButtonStyle, GUILayout.Height(32)))
            {
                ReturnToGlobe();
            }
            GUILayout.Space(6);
            if (GUILayout.Button("Salvar e Sair", menuButtonDangerStyle, GUILayout.Height(32)))
            {
                ReturnToGlobe();
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #endregion
    }
}
