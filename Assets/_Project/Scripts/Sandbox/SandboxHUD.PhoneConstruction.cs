using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Implementação da interface do App de Construção do Celular:
    /// - Navegação por Categorias e Submenus (1. Estruturas, 2. Operações, 3. Biologia, 4. Decoração, 5. Paisagismo)
    /// - Filtro por Profissão desbloqueada (Agricultor, Caminhoneiro, Piloto, Pescador)
    /// - Exibição de itens bloqueados com badge de requisitos técnicos
    /// - Painel de detalhes com custos, especificações e botão de ação ("Construir" / "Modificar Solo / Terreno")
    /// </summary>
    public partial class SandboxHUD
    {
        #region Estado do Menu de Construção

        private BuildCategory buildSelectedCategory = BuildCategory.Estruturas;
        private string buildSelectedSubcategory = "";
        private StarterCareer? buildCareerFilter = null; // null = Todas as carreiras do jogador
        private BuildItemDefinition buildSelectedItem = null;
        private Vector2 buildScroll = Vector2.zero;
        private Vector2 buildHorizontalScroll = Vector2.zero;

        #endregion

        #region Abertura e Fechamento da Barra de Construção

        /// <summary>
        /// Chamado ao selecionar o app/menu de Construção:
        /// - O celular retrai na tela (phoneAnim desliza para baixo)
        /// - Abre a barra horizontal na parte inferior da tela
        /// </summary>
        public void OpenConstructionBar()
        {
            IsPhoneOpen = false;
            IsConstructionBarOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var subcats = ConstructionCatalog.GetSubcategories(buildSelectedCategory);
            if (string.IsNullOrEmpty(buildSelectedSubcategory) && subcats.Count > 0)
            {
                buildSelectedSubcategory = subcats[0];
            }
        }

        /// <summary>
        /// Volta para o celular (acionado por Esc ou pelo botão de 'X' da barra horizontal).
        /// </summary>
        public void CloseConstructionBarToPhone()
        {
            IsConstructionBarOpen = false;
            IsPhoneOpen = true;
            currentApp = PhoneApp.Home;
            buildSelectedItem = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #endregion

        #region Renderização do App Construção

        private void DrawPhoneConstruction(Rect content)
        {
            DrawAppHeader(content, "Construções");

            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            buildScroll = GUILayout.BeginScrollView(buildScroll);

            var save = CurrentSave;

            // 1. BARRA DE PROFISSÕES / FILTRO
            GUILayout.Label("FILTRO POR PROFISSÃO", phoneSectionStyle);
            GUILayout.BeginHorizontal();

            bool isAll = (buildCareerFilter == null);
            GUI.color = isAll ? new Color(0.25f, 0.75f, 1f) : Color.white;
            if (GUILayout.Button("Todas", phoneListButtonStyle, GUILayout.Height(24)))
            {
                buildCareerFilter = null;
            }

            GUI.color = (buildCareerFilter == StarterCareer.Farmer) ? new Color(0.25f, 0.75f, 1f) : Color.white;
            if (GUILayout.Button("Fazenda", phoneListButtonStyle, GUILayout.Height(24)))
            {
                buildCareerFilter = StarterCareer.Farmer;
            }

            // Profissões ainda não liberadas: filtros travados com pop-up de futuras adições.
            GUI.color = new Color(0.55f, 0.58f, 0.65f, 0.75f);
            if (GUILayout.Button("Carga [Em breve]", phoneListButtonStyle, GUILayout.Height(24)))
            {
                ShowToast("[Em breve] Profissão 'Carga' em desenvolvimento. Novas carreiras em futuras atualizações!", 4f);
            }

            GUI.color = new Color(0.55f, 0.58f, 0.65f, 0.75f);
            if (GUILayout.Button("Aéreo [Em breve]", phoneListButtonStyle, GUILayout.Height(24)))
            {
                ShowToast("[Em breve] Profissão 'Aéreo' em desenvolvimento. Novas carreiras em futuras atualizações!", 4f);
            }

            GUI.color = new Color(0.55f, 0.58f, 0.65f, 0.75f);
            if (GUILayout.Button("Pesca [Em breve]", phoneListButtonStyle, GUILayout.Height(24)))
            {
                ShowToast("[Em breve] Profissão 'Pesca' em desenvolvimento. Novas carreiras em futuras atualizações!", 4f);
            }

            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);

            // 2. CATEGORIAS PRINCIPAIS
            GUILayout.Label("CATEGORIAS", phoneSectionStyle);

            string[] catNames = { "1. Estruturas", "2. Operações", "3. Cultivo", "4. Decoração", "5. Paisagismo" };
            BuildCategory[] cats = {
                BuildCategory.Estruturas,
                BuildCategory.OperacoesProducao,
                BuildCategory.BiologiaCultivo,
                BuildCategory.Decoracao,
                BuildCategory.Paisagismo
            };

            for (int i = 0; i < cats.Length; i++)
            {
                bool isCatActive = (buildSelectedCategory == cats[i]);
                GUI.color = Color.white;

                string prefix = isCatActive ? "▶ " : "  ";
                if (GUILayout.Button(prefix + catNames[i], phoneListButtonStyle, GUILayout.Height(26)))
                {
                    buildSelectedCategory = cats[i];
                    buildSelectedSubcategory = "";
                    buildSelectedItem = null;
                }
            }
            GUI.color = Color.white;

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);

            // 3. SUBMENUS DA CATEGORIA SELECIONADA
            var subcats = ConstructionCatalog.GetSubcategories(buildSelectedCategory);
            if (string.IsNullOrEmpty(buildSelectedSubcategory) && subcats.Count > 0)
            {
                buildSelectedSubcategory = subcats[0];
            }

            GUILayout.Label($"SUBMENUS • {buildSelectedCategory}", phoneSectionStyle);
            foreach (var sub in subcats)
            {
                bool isSubActive = (buildSelectedSubcategory == sub);
                GUI.color = isSubActive ? new Color(0.3f, 0.7f, 1f) : Color.white;

                string arrow = isSubActive ? "• " : "  ";
                if (GUILayout.Button($"{arrow}{sub}", phoneListButtonStyle, GUILayout.Height(24)))
                {
                    buildSelectedSubcategory = sub;
                    buildSelectedItem = null;
                }
            }
            GUI.color = Color.white;

            GUILayout.Space(6);
            DrawPhoneDivider();
            GUILayout.Space(4);

            // 4. ITENS DO SUBMENU COM STATUS DE DESBLOQUEIO POR PROFISSÃO
            GUILayout.Label($"CONSTRUÇÕES • {buildSelectedSubcategory}", phoneSectionStyle);

            var items = ConstructionCatalog.GetItems(buildSelectedCategory, buildSelectedSubcategory);
            int renderedCount = 0;

            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];

                // Filtro de carreira opcional do topo
                if (buildCareerFilter.HasValue && item.requiredCareer != buildCareerFilter.Value)
                {
                    continue;
                }

                renderedCount++;
                bool isUnlocked = item.IsUnlocked(save);
                bool isSelected = (buildSelectedItem == item);

                string lockTag = isUnlocked ? "[OK]" : "[BLOQUEADO]";
                string titleText = $"{lockTag} {item.name}";

                GUI.color = isSelected ? new Color(0.25f, 0.95f, 0.85f) : (isUnlocked ? Color.white : new Color(0.7f, 0.65f, 0.55f));

                if (GUILayout.Button(titleText, phoneListButtonStyle, GUILayout.Height(28)))
                {
                    buildSelectedItem = item;
                }

                GUI.color = Color.white;
                GUILayout.Label($"  Custo: ${item.cost:N0} • {item.iconSymbol}", phoneSmallStyle);

                if (!isUnlocked)
                {
                    GUILayout.Label($"  Exige: {item.GetRequirementText()}", phoneLockStyle);
                }

                GUILayout.Space(2);
            }

            if (renderedCount == 0)
            {
                GUILayout.Label("Nenhuma construção para este filtro.", phoneSmallStyle);
            }

            // 5. PAINEL DE DETALHES DO ITEM SELECIONADO
            if (buildSelectedItem != null)
            {
                GUILayout.Space(8);
                GUILayout.BeginVertical(phoneCardStyle);
                GUILayout.Label("DETALHES DO PROJETO", phoneSectionStyle);
                GUILayout.Label(buildSelectedItem.name, phoneBodyStyle);
                GUILayout.Label(buildSelectedItem.description, phoneSmallStyle);
                GUILayout.Label($"Investimento: ${buildSelectedItem.cost:N0}", phoneSmallStyle);

                bool canBuild = buildSelectedItem.IsUnlocked(save);
                bool hasMoney = (save == null) || (save.currentMoney >= buildSelectedItem.cost);

                if (!canBuild)
                {
                    GUILayout.Label($"[Bloqueado] {buildSelectedItem.GetRequirementText()}", phoneLockStyle);
                    GUILayout.Label("Desbloqueie a licença no App Licenças.", phoneSmallStyle);
                }
                else if (!hasMoney)
                {
                    GUILayout.Label($"[Bloqueado] Saldo insuficiente (${save.currentMoney:N0} de ${buildSelectedItem.cost:N0})", phoneLockStyle);
                }
                else
                {
                    // SEÇÃO DE MODO DE CONSTRUÇÃO: AUTOMÁTICA VS MANUAL
                    GUILayout.Space(4);
                    GUILayout.Label("MODO DE CONSTRUÇÃO", phoneSectionStyle);

                    if (buildSelectedItem.category == BuildCategory.Paisagismo)
                    {
                        if (GUILayout.Button($"Aplicar no Mundo (${buildSelectedItem.cost:N0})", menuButtonPrimaryStyle, GUILayout.Height(30)))
                        {
                            ExecuteConstruction(buildSelectedItem, save, isAutomatic: true);
                        }
                    }
                    else
                    {
                        // 1. CONSTRUÇÃO AUTOMÁTICA (Instantânea)
                        GUILayout.BeginVertical();
                        GUILayout.Label("• <b>Construção Automática</b>: Fica pronta instantaneamente.", phoneSmallStyle);
                        if (GUILayout.Button($"Construção Automática (${buildSelectedItem.cost:N0})", menuButtonPrimaryStyle, GUILayout.Height(28)))
                        {
                            ExecuteConstruction(buildSelectedItem, save, isAutomatic: true);
                        }
                        GUILayout.EndVertical();

                        GUILayout.Space(6);

                        // 2. CONSTRUÇÃO MANUAL (Fantasma / Canteiro de Obras + Recursos)
                        GUILayout.BeginVertical();
                        GUILayout.Label("• <b>Construção Manual</b>: Posiciona o fantasma no local. Requer entrega de recursos obtidos ou comprados na gameplay.", phoneSmallStyle);
                        
                        var reqs = buildSelectedItem.GetResourceRequirements();
                        string reqSummary = "";
                        for (int r = 0; r < reqs.Count; r++)
                        {
                            reqSummary += $"{reqs[r].amountRequired}x {BuildResourceRequirement.GetName(reqs[r].type)}{(r < reqs.Count - 1 ? ", " : "")}";
                        }
                        GUILayout.Label($"  Recursos Necessários: {reqSummary}", phoneSmallStyle);

                        if (GUILayout.Button("Posicionar Fantasma (Manual)", phoneListButtonStyle, GUILayout.Height(28)))
                        {
                            ExecuteConstruction(buildSelectedItem, save, isAutomatic: false);
                        }
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndVertical();
            }

            // 6. SEÇÃO DE CANTEIRO DE OBRAS PRÓXIMO (Se houver um fantasma por perto)
            DrawNearbyConstructionSite(save);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNearbyConstructionSite(RegionSaveData save)
        {
            var player = PlayerCharacterController.Instance;
            if (player == null) return;

            // Busca por canteiros de obras fantasmas num raio de 25m
            var ghosts = UnityEngine.Object.FindObjectsByType<ConstructionSiteGhost>();
            ConstructionSiteGhost nearest = null;
            float minDist = 25f;

            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] == null || ghosts[i].IsCompleted) continue;
                float d = Vector3.Distance(player.transform.position, ghosts[i].transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = ghosts[i];
                }
            }

            if (nearest != null)
            {
                GUILayout.Space(8);
                GUILayout.BeginVertical(phoneCardStyle);
                GUILayout.Label("CANTEIRO DE OBRAS PRÓXIMO", phoneSectionStyle);
                GUILayout.Label($"Obra: <b>{nearest.itemDefinition?.name}</b> ({minDist:F1}m)", phoneBodyStyle);
                GUILayout.Label($"Progresso da Obra: <b>{nearest.progressPercent:F0}%</b>", phoneSmallStyle);
                GUILayout.Space(4);

                for (int r = 0; r < nearest.requirements.Count; r++)
                {
                    var req = nearest.requirements[r];
                    string status = req.IsFulfilled ? "[OK]" : $"{req.amountDelivered}/{req.amountRequired}";
                    GUILayout.Label($"• {BuildResourceRequirement.GetName(req.type)}: {status}", phoneSmallStyle);

                    if (!req.IsFulfilled)
                    {
                        GUILayout.BeginHorizontal();
                        // Opção A: Entregar material obtido na gameplay (trabalho/coleta/transporte)
                        if (GUILayout.Button($"Entregar 10x (Coletado)", phoneListButtonStyle, GUILayout.Height(22)))
                        {
                            nearest.DeliverResource(req.type, 10, out int delivered);
                            ShowToast($"Entregue {delivered}x {BuildResourceRequirement.GetName(req.type)} na obra.");
                        }

                        // Opção B: Comprar materiais diretamente
                        long unitP = BuildResourceRequirement.GetUnitPrice(req.type);
                        int toBuy = Mathf.Min(req.Remaining, 10);
                        long cost = toBuy * unitP;
                        if (GUILayout.Button($"Comprar {toBuy}x (${cost:N0})", phoneListButtonStyle, GUILayout.Height(22)))
                        {
                            nearest.BuyResource(req.type, toBuy, save, out string fb);
                            ShowToast(fb);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }
        }

        private void ExecuteConstruction(BuildItemDefinition item, RegionSaveData save, bool isAutomatic)
        {
            if (item == null) return;

            // Determina a posição de construção: posição do jogador ou alvo
            Vector3 targetPos = Vector3.zero;
            Quaternion rot = Quaternion.identity;

            if (PlayerCharacterController.Instance != null)
            {
                var p = PlayerCharacterController.Instance;
                targetPos = p.transform.position + p.transform.forward * 10f;
                rot = Quaternion.LookRotation(p.transform.forward, Vector3.up);
            }

            var builder = ConstructionPlacementManager.Instance;
            if (builder == null)
            {
                var go = new GameObject("ConstructionPlacementManager");
                builder = go.AddComponent<ConstructionPlacementManager>();
            }

            if (builder.ExecuteBuild(item, targetPos, rot, isAutomatic, out string feedback))
            {
                // Se for automático, desconta o valor integral imediatamente
                if (isAutomatic && save != null)
                {
                    save.currentMoney -= item.cost;
                    save.totalExpenses += item.cost;
                    PersistSave();
                }

                ShowToast(feedback);
            }
            else
            {
                ShowToast($"Erro: {feedback}");
            }
        }

        private void DrawWorldConstructionSitePrompt(PlayerCharacterController player)
        {
            if (player == null || Camera.main == null) return;

            var ghosts = UnityEngine.Object.FindObjectsByType<ConstructionSiteGhost>();
            if (ghosts == null || ghosts.Length == 0) return;

            for (int i = 0; i < ghosts.Length; i++)
            {
                var ghost = ghosts[i];
                if (ghost == null || ghost.IsCompleted) continue;

                float dist = Vector3.Distance(player.transform.position, ghost.transform.position);
                if (dist > 35f) continue;

                // Projeta a posição 3D do canteiro para a tela 2D
                Vector3 worldAnchor = ghost.transform.position + Vector3.up * 2.5f;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldAnchor);

                if (screenPos.z > 0f)
                {
                    float sx = screenPos.x;
                    float sy = Screen.height - screenPos.y;

                    float w = 240f;
                    float h = 64f;
                    Rect r = new Rect(sx - w * 0.5f, sy - h * 0.5f, w, h);

                    UIThemeHelper.DrawRoundedBox(r, new Color(0.04f, 0.12f, 0.22f, 0.85f), new Color(0.2f, 0.75f, 1f, 0.9f), 6f, 1.5f);

                    GUILayout.BeginArea(new Rect(r.x + 8f, r.y + 4f, r.width - 16f, r.height - 8f));
                    GUILayout.Label($"<b>Canteiro: {ghost.itemDefinition?.name}</b>", phoneSectionStyle);
                    GUILayout.Label($"Progresso: <b>{ghost.progressPercent:F0}%</b> • Dist: {dist:F0}m", phoneSmallStyle);
                    GUILayout.Label($"[Alt] Celular > Obras para abastecer recursos", hudKeyBadgeStyle);
                    GUILayout.EndArea();
                }
            }
        }

        #endregion

        #region Barra Horizontal de Construção (Menu Inferior estilo Cities/Simulador)

        private void DrawHorizontalConstructionBar()
        {
            InitStyles();
            var save = CurrentSave;

            float ease = Mathf.SmoothStep(0f, 1f, constructionBarAnim);
            float barW = Mathf.Clamp(Screen.width - 48f, 720f, 1380f);
            float barH = 205f;
            float barX = (Screen.width - barW) * 0.5f;
            float yRest = Screen.height - barH - 12f;
            float barY = Mathf.Lerp(Screen.height + 25f, yRest, ease);

            Rect barRect = new Rect(barX, barY, barW, barH);

            // Container principal externo (borda arredondada suave)
            UIThemeHelper.DrawRoundedBox(barRect, new Color(0.03f, 0.05f, 0.09f, 0.96f), new Color(0.28f, 0.65f, 0.95f, 0.85f), 14f, 1.8f);

            // 1. TOPO DA BARRA: Título "Categorias" + Abas de Categorias + Botão "X"
            float headerY = barRect.y + 8f;
            float headerH = 28f;

            // Título "Categorias"
            GUI.Label(new Rect(barRect.x + 16f, headerY + 2f, 95f, headerH), "Categorias", buildBarTitleStyle);

            // Abas das 5 Categorias Principais
            string[] catNames = { "1. Estruturas", "2. Operações", "3. Cultivo", "4. Decoração", "5. Paisagismo" };
            BuildCategory[] cats = {
                BuildCategory.Estruturas,
                BuildCategory.OperacoesProducao,
                BuildCategory.BiologiaCultivo,
                BuildCategory.Decoracao,
                BuildCategory.Paisagismo
            };

            float tabX = barRect.x + 115f;
            float tabW = 108f;
            float tabGap = 6f;

            for (int i = 0; i < cats.Length; i++)
            {
                bool isCatActive = (buildSelectedCategory == cats[i]);
                Rect tabRect = new Rect(tabX + i * (tabW + tabGap), headerY, tabW, headerH);

                Color tabBg = isCatActive ? new Color(0.12f, 0.38f, 0.68f, 0.95f) : new Color(0.06f, 0.12f, 0.20f, 0.85f);
                Color tabBorder = isCatActive ? new Color(0.35f, 0.95f, 1f, 0.95f) : new Color(0.20f, 0.40f, 0.65f, 0.50f);
                UIThemeHelper.DrawRoundedBox(tabRect, tabBg, tabBorder, 6f, isCatActive ? 1.8f : 1f);

                Color prevColor = GUI.contentColor;
                GUI.contentColor = Color.white;

                if (GUI.Button(tabRect, catNames[i], buildTabStyle))
                {
                    buildSelectedCategory = cats[i];
                    buildSelectedSubcategory = "";
                    buildSelectedItem = null;
                    buildHorizontalScroll = Vector2.zero;
                }

                GUI.contentColor = prevColor;
            }

            // Botão "X" de Fechar / Voltar ao Celular no canto superior direito
            Rect closeBtnRect = new Rect(barRect.x + barRect.width - 38f, headerY, 28f, 28f);
            if (GUI.Button(closeBtnRect, "✕", buildCloseBtnStyle))
            {
                CloseConstructionBarToPhone();
            }

            // 2. SUBMENUS / FILTROS DA CATEGORIA SELECIONADA (Pílulas horizontais)
            var subcats = ConstructionCatalog.GetSubcategories(buildSelectedCategory);
            float subcatY = barRect.y + 40f;
            float subcatH = 22f;

            float pillX = barRect.x + 16f;
            bool isAllSub = string.IsNullOrEmpty(buildSelectedSubcategory);

            // Pílula "Todas"
            float allW = 60f;
            Rect allRect = new Rect(pillX, subcatY, allW, subcatH);
            Color allBg = isAllSub ? new Color(0.15f, 0.45f, 0.75f, 0.9f) : new Color(0.05f, 0.10f, 0.18f, 0.75f);
            Color allBorder = isAllSub ? new Color(0.4f, 0.9f, 1f, 0.9f) : new Color(0.2f, 0.35f, 0.55f, 0.45f);
            UIThemeHelper.DrawRoundedBox(allRect, allBg, allBorder, 5f, 1f);

            Color prevSubColor = GUI.contentColor;
            GUI.contentColor = Color.white;
            if (GUI.Button(allRect, "Todas", buildSubtabStyle))
            {
                buildSelectedSubcategory = "";
                buildSelectedItem = null;
                buildHorizontalScroll = Vector2.zero;
            }

            pillX += allW + 6f;

            for (int s = 0; s < subcats.Count; s++)
            {
                string subName = subcats[s];
                bool isSubActive = (buildSelectedSubcategory == subName);
                float pillW = Mathf.Max(64f, GUI.skin.label.CalcSize(new GUIContent(subName)).x + 16f);

                if (pillX + pillW > barRect.x + barRect.width - 40f) break; // evita extrapolar largura

                Rect pRect = new Rect(pillX, subcatY, pillW, subcatH);
                Color pBg = isSubActive ? new Color(0.15f, 0.45f, 0.75f, 0.9f) : new Color(0.05f, 0.10f, 0.18f, 0.75f);
                Color pBorder = isSubActive ? new Color(0.4f, 0.9f, 1f, 0.9f) : new Color(0.2f, 0.35f, 0.55f, 0.45f);
                UIThemeHelper.DrawRoundedBox(pRect, pBg, pBorder, 5f, 1f);

                if (GUI.Button(pRect, subName, buildSubtabStyle))
                {
                    buildSelectedSubcategory = subName;
                    buildSelectedItem = null;
                    buildHorizontalScroll = Vector2.zero;
                }

                pillX += pillW + 6f;
            }
            GUI.contentColor = prevSubColor;

            // 3. PAINEL INTERNO DOS CARDS HORIZONTAIS
            Rect cardsBoxRect = new Rect(barRect.x + 14f, barRect.y + 68f, barRect.width - 28f, 126f);
            UIThemeHelper.DrawRoundedBox(cardsBoxRect, new Color(0.02f, 0.04f, 0.07f, 0.85f), new Color(0.18f, 0.45f, 0.75f, 0.35f), 10f, 1.2f);

            var items = ConstructionCatalog.GetItems(buildSelectedCategory, buildSelectedSubcategory);

            // Suporte a rolagem com Scroll do Mouse sobre o painel
            if (Event.current.type == EventType.ScrollWheel && cardsBoxRect.Contains(Event.current.mousePosition))
            {
                buildHorizontalScroll.x += Event.current.delta.y * 55f;
                Event.current.Use();
            }

            // Botão de Seta para Esquerda ◀
            Rect leftArrowRect = new Rect(cardsBoxRect.x + 4f, cardsBoxRect.y + 36f, 26f, 54f);
            if (buildHorizontalScroll.x > 8f)
            {
                if (GUI.Button(leftArrowRect, "◀", buildArrowStyle))
                {
                    buildHorizontalScroll.x = Mathf.Max(0f, buildHorizontalScroll.x - 260f);
                }
            }

            // Botão de Seta para Direita ▶
            Rect rightArrowRect = new Rect(cardsBoxRect.x + cardsBoxRect.width - 30f, cardsBoxRect.y + 36f, 26f, 54f);

            float scrollAreaX = cardsBoxRect.x + 34f;
            float scrollAreaW = cardsBoxRect.width - 68f;
            Rect scrollAreaRect = new Rect(scrollAreaX, cardsBoxRect.y + 6f, scrollAreaW, cardsBoxRect.height - 12f);

            float cardW = 120f;
            float gap = 8f;
            float totalW = items.Count * (cardW + gap);
            float maxScroll = Mathf.Max(0f, totalW - scrollAreaW);
            buildHorizontalScroll.x = Mathf.Clamp(buildHorizontalScroll.x, 0f, maxScroll);

            if (buildHorizontalScroll.x < maxScroll - 8f && items.Count > 0)
            {
                if (GUI.Button(rightArrowRect, "▶", buildArrowStyle))
                {
                    buildHorizontalScroll.x = Mathf.Min(maxScroll, buildHorizontalScroll.x + 260f);
                }
            }

            // Exibição dos cards dentro do ScrollView
            buildHorizontalScroll = GUI.BeginScrollView(
                scrollAreaRect,
                buildHorizontalScroll,
                new Rect(0, 0, totalW, scrollAreaRect.height - 4f),
                GUIStyle.none,
                GUIStyle.none
            );

            for (int idx = 0; idx < items.Count; idx++)
            {
                var item = items[idx];
                float cx = idx * (cardW + gap);
                Rect cardRect = new Rect(cx, 0, cardW, 114f);

                bool isSelected = (buildSelectedItem == item);
                bool isUnlocked = item.IsUnlocked(save);

                // Estilo visual do Card
                Color cardBg;
                Color cardBorder;

                if (isSelected)
                {
                    cardBg = new Color(0.10f, 0.22f, 0.38f, 0.95f);
                    cardBorder = new Color(0.35f, 0.95f, 1f, 1f);
                }
                else if (isUnlocked)
                {
                    cardBg = new Color(0.04f, 0.08f, 0.15f, 0.85f);
                    cardBorder = new Color(0.22f, 0.50f, 0.80f, 0.55f);
                }
                else
                {
                    cardBg = new Color(0.05f, 0.05f, 0.07f, 0.75f);
                    cardBorder = new Color(0.45f, 0.35f, 0.22f, 0.55f);
                }

                UIThemeHelper.DrawRoundedBox(cardRect, cardBg, cardBorder, 8f, isSelected ? 2f : 1.2f);

                // Clique para selecionar
                if (GUI.Button(cardRect, "", GUIStyle.none))
                {
                    buildSelectedItem = (buildSelectedItem == item ? null : item);
                }

                // A. Topo do Card: Nome
                Rect nameRect = new Rect(cardRect.x + 4f, cardRect.y + 4f, cardRect.width - 8f, 26f);
                GUI.Label(nameRect, TruncateCardName(item.name, 20), buildCardNameStyle);

                // B. Centro do Card: Imagem (Box com ícone)
                Rect imgRect = new Rect(cardRect.x + (cardW - 68f) * 0.5f, cardRect.y + 32f, 68f, 52f);
                UIThemeHelper.DrawRoundedBox(imgRect, new Color(0.02f, 0.04f, 0.08f, 0.9f), new Color(0.18f, 0.40f, 0.65f, 0.4f), 6f, 1f);

                TerraIcon icon = GetBuildItemIcon(item);
                Rect iconRect = new Rect(imgRect.x + 16f, imgRect.y + 8f, 36f, 36f);
                TerraIcons.Draw(iconRect, icon, isUnlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.7f));

                if (!isUnlocked)
                {
                    TerraIcons.Draw(new Rect(imgRect.x + imgRect.width - 16f, imgRect.y + 3f, 13f, 13f), TerraIcon.Lock, new Color(1f, 0.75f, 0.3f));
                }

                // C. Base do Card: Preço
                Rect priceRect = new Rect(cardRect.x + 4f, cardRect.y + 88f, cardRect.width - 8f, 20f);
                string priceStr = isUnlocked ? $"${item.cost:N0}" : $"[Lock] ${item.cost:N0}";
                GUI.Label(priceRect, priceStr, isUnlocked ? buildCardPriceStyle : buildCardLockedStyle);
            }

            GUI.EndScrollView();

            // 4. POPUP DE DETALHES E AÇÃO DO ITEM SELECIONADO (Sobreposto logo acima da barra)
            if (buildSelectedItem != null)
            {
                DrawSelectedItemActionPopup(barRect, save);
            }
        }

        private void DrawSelectedItemActionPopup(Rect barRect, RegionSaveData save)
        {
            float popW = Mathf.Clamp(barRect.width * 0.68f, 540f, 800f);
            float popH = 120f;
            float popX = barRect.x + (barRect.width - popW) * 0.5f;
            float popY = barRect.y - popH - 10f;
            Rect popRect = new Rect(popX, popY, popW, popH);

            UIThemeHelper.DrawRoundedBox(popRect, new Color(0.03f, 0.06f, 0.12f, 0.98f), new Color(0.35f, 0.85f, 1f, 0.95f), 12f, 1.8f);

            GUILayout.BeginArea(new Rect(popRect.x + 12f, popRect.y + 8f, popRect.width - 24f, popRect.height - 16f));

            // Topo do popup com título e botão fechar
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{buildSelectedItem.name}</b> • <color=#55ccff>{buildSelectedItem.subcategory}</color>", hudBoxTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", phoneListButtonStyle, GUILayout.Width(24), GUILayout.Height(20)))
            {
                buildSelectedItem = null;
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(buildSelectedItem.description, phoneSmallStyle);

            bool isUnlocked = buildSelectedItem.IsUnlocked(save);
            bool hasMoney = (save == null) || (save.currentMoney >= buildSelectedItem.cost);

            if (!isUnlocked)
            {
                GUILayout.Label($"[BLOQUEADO] Exige: {buildSelectedItem.GetRequirementText()}", phoneLockStyle);
            }
            else if (!hasMoney)
            {
                GUILayout.Label($"[SALDO INSUFICIENTE] Custo: ${buildSelectedItem.cost:N0} (Saldo: ${save.currentMoney:N0})", phoneLockStyle);
            }
            else
            {
                GUILayout.BeginHorizontal();
                if (buildSelectedItem.category == BuildCategory.Paisagismo)
                {
                    if (GUILayout.Button($"Aplicar no Mundo (${buildSelectedItem.cost:N0})", menuButtonPrimaryStyle, GUILayout.Height(28)))
                    {
                        ExecuteConstruction(buildSelectedItem, save, isAutomatic: true);
                    }
                }
                else
                {
                    if (GUILayout.Button($"Construção Automática (${buildSelectedItem.cost:N0})", menuButtonPrimaryStyle, GUILayout.Height(28)))
                    {
                        ExecuteConstruction(buildSelectedItem, save, isAutomatic: true);
                    }

                    GUILayout.Space(8);

                    if (GUILayout.Button("Posicionar Fantasma (Manual)", phoneListButtonStyle, GUILayout.Height(28)))
                    {
                        ExecuteConstruction(buildSelectedItem, save, isAutomatic: false);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private string TruncateCardName(string text, int maxLen = 18)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLen) return text;
            return text.Substring(0, maxLen - 1) + "…";
        }

        private TerraIcon GetBuildItemIcon(BuildItemDefinition item)
        {
            if (item == null) return TerraIcon.Tools;
            string id = item.id.ToLowerInvariant();

            // Veículos / Estruturas de transporte
            if (id.Contains("hangar") || id.Contains("pista") || id.Contains("heliponto") || id.Contains("torre"))
                return TerraIcon.Airport;
            if (id.Contains("pier") || id.Contains("marina") || id.Contains("porto") || id.Contains("ancoradouro") || id.Contains("boathouse"))
                return TerraIcon.Harbor;
            if (id.Contains("gar") || id.Contains("estacionamento") || id.Contains("patio"))
                return item.requiredCareer == StarterCareer.Farmer ? TerraIcon.Tractor : TerraIcon.HeavyTruck;

            // Armazenamento e Indústria
            if (id.Contains("silo") || id.Contains("armazem") || id.Contains("deposito") || id.Contains("estoque"))
                return TerraIcon.CargoBox;
            if (id.Contains("tanque") || id.Contains("combustivel") || id.Contains("posto"))
                return TerraIcon.Fuel;
            if (id.Contains("fabrica") || id.Contains("industria") || id.Contains("moinho") || id.Contains("processamento") || id.Contains("usina"))
                return TerraIcon.Industrial;
            if (id.Contains("oficina") || id.Contains("manutencao") || id.Contains("lavador"))
                return TerraIcon.Wrench;

            // Biologia e Animais
            if (id.Contains("vaca") || id.Contains("gado") || id.Contains("curral") || id.Contains("bovino"))
                return TerraIcon.Cow;
            if (id.Contains("cavalo") || id.Contains("haras") || id.Contains("equino"))
                return TerraIcon.Horse;
            if (id.Contains("ovelha") || id.Contains("ovino") || id.Contains("la"))
                return TerraIcon.Sheep;
            if (id.Contains("estufa") || id.Contains("horta") || id.Contains("cultivo") || id.Contains("granj"))
                return TerraIcon.Crops;
            if (id.Contains("leite") || id.Contains("laticinio"))
                return TerraIcon.Milk;
            if (id.Contains("peixe") || id.Contains("pesca") || id.Contains("aquacultura"))
                return TerraIcon.Fish;

            // Decoração e Urbano
            if (id.Contains("poste") || id.Contains("luz") || id.Contains("iluminacao"))
                return TerraIcon.Headlights;
            if (id.Contains("cerca") || id.Contains("muro") || id.Contains("alambrado") || id.Contains("portao"))
                return TerraIcon.Shield;
            if (id.Contains("banco") || id.Contains("praca") || id.Contains("quiosque") || id.Contains("gazebo"))
                return TerraIcon.Park;
            if (id.Contains("escultura") || id.Contains("monumento") || id.Contains("chafariz"))
                return TerraIcon.Historic;
            if (id.Contains("placa") || id.Contains("sinal"))
                return TerraIcon.Info;

            // Paisagismo e Terreno
            if (id.Contains("arvore") || id.Contains("bosque") || id.Contains("floresta"))
                return TerraIcon.Forest;
            if (id.Contains("arbusto") || id.Contains("muda") || id.Contains("planta"))
                return TerraIcon.Seedling;
            if (id.Contains("grama") || id.Contains("capim") || id.Contains("mato"))
                return TerraIcon.Crops;
            if (id.Contains("agua") || id.Contains("lago") || id.Contains("canal") || id.Contains("rio"))
                return TerraIcon.WaterDrop;
            if (id.Contains("terra") || id.Contains("relevo") || id.Contains("montanha") || id.Contains("plato"))
                return TerraIcon.Mountain;
            if (id.Contains("areia") || id.Contains("duna") || id.Contains("praia"))
                return TerraIcon.Desert;
            if (id.Contains("pincel") || id.Contains("solo") || id.Contains("pintar"))
                return TerraIcon.PaintBrush;

            // Fallbacks baseados em categoria
            switch (item.category)
            {
                case BuildCategory.Estruturas: return TerraIcon.House;
                case BuildCategory.OperacoesProducao: return TerraIcon.Industrial;
                case BuildCategory.BiologiaCultivo: return TerraIcon.Seedling;
                case BuildCategory.Decoracao: return TerraIcon.Park;
                case BuildCategory.Paisagismo: return TerraIcon.Mountain;
                default: return TerraIcon.Tools;
            }
        }

        #endregion
    }
}
