using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.UI
{
    public partial class MainMenuController
    {
        public enum MultiplayerTab { ServerBrowser, HostServer, DirectConnect }
        private MultiplayerTab currentMultiplayerTab = MultiplayerTab.ServerBrowser;

        // Servidores simulados e dinâmicos
        private struct MultiplayerServerInfo
        {
            public string serverName;
            public string region;
            public string gameMode;
            public int currentPlayers;
            public int maxPlayers;
            public int pingMs;
            public bool hasPassword;
            public string hostName;
        }

        private List<MultiplayerServerInfo> serverList = new List<MultiplayerServerInfo>();
        private Vector2 serverScrollPos = Vector2.zero;

        // Formulário de Host
        private string hostServerName = "";
        private string hostRegionName = "";
        private int hostMaxPlayers = 16;
        private string hostGameMode = "";
        private string hostPassword = "";
        private int hostPort = 7777;

        // Formulário Conexão Direta
        private string directConnectIP = "";
        private string sessionCode = "";

        // Estado de conexão
        private bool isConnecting = false;
        private float connectTimer = 0f;
        private string connectingServerName = "";

        private void InitMultiplayer()
        {
            serverList.Clear();
        }

        private void UpdateMultiplayerTimers()
        {
            if (isConnecting)
            {
                connectTimer -= Time.deltaTime;
                if (connectTimer <= 0f)
                {
                    isConnecting = false;
                    ShowNotification($"Conectado com sucesso ao servidor '{connectingServerName}'!");
                    // Aqui pode disparar a inicialização da sessão de rede
                }
            }
        }

        private void DrawMultiplayerScreen()
        {
            DrawModalHeader("MULTIPLAYER - MUNDO COMPARTILHADO", "Jogue cooperativamente em tempo real com outros fazendeiros, pilotos e caminhoneiros");

            // Barra de abas
            GUILayout.BeginHorizontal();
            if (IconBtn(TerraIcon.Satellite, "Navegador de Servidores", currentMultiplayerTab == MultiplayerTab.ServerBrowser ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentMultiplayerTab = MultiplayerTab.ServerBrowser;
            }
            if (IconBtn(TerraIcon.House, "Hospedar Partida / Criar Sala", currentMultiplayerTab == MultiplayerTab.HostServer ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentMultiplayerTab = MultiplayerTab.HostServer;
            }
            if (IconBtn(TerraIcon.Battery, "Conexão Direta (IP/Código)", currentMultiplayerTab == MultiplayerTab.DirectConnect ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(28)))
            {
                currentMultiplayerTab = MultiplayerTab.DirectConnect;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            if (isConnecting)
            {
                DrawConnectingFeedback();
                return;
            }

            switch (currentMultiplayerTab)
            {
                case MultiplayerTab.ServerBrowser:
                    DrawServerBrowser();
                    break;
                case MultiplayerTab.HostServer:
                    DrawHostServer();
                    break;
                case MultiplayerTab.DirectConnect:
                    DrawDirectConnect();
                    break;
            }
        }

        private void DrawServerBrowser()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Servidores Disponíveis: {serverList.Count}", sectionHeaderStyle);
            GUILayout.FlexibleSpace();
            if (IconBtn(TerraIcon.FastForward, "Atualizar Lista", actionBtnStyle, GUILayout.Width(130), GUILayout.Height(24)))
            {
                InitMultiplayer();
                ShowNotification("Lista de servidores atualizada.");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            serverScrollPos = GUILayout.BeginScrollView(serverScrollPos);

            for (int i = 0; i < serverList.Count; i++)
            {
                var s = serverList[i];

                GUI.color = new Color(0.04f, 0.10f, 0.20f, 0.90f);
                GUILayout.BeginVertical(cardStyle);
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                GUILayout.Label(s.serverName, labelBoldStyle);
                GUILayout.FlexibleSpace();

                // Ping com código de cor
                Color pingColor = s.pingMs < 60 ? new Color(0.3f, 1f, 0.4f) :
                                  s.pingMs < 130 ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.4f, 0.4f);
                GUI.color = pingColor;
                GUILayout.Label($"{s.pingMs} ms", labelBoldStyle);
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                GUILayout.Label($"Região: {s.region} - Modo: {s.gameMode}", labelRegularStyle);
                GUILayout.Label($"Jogadores: {s.currentPlayers}/{s.maxPlayers} - Host: {s.hostName} {(s.hasPassword ? "Com Senha" : "Aberto")}", menuBtnSubtextStyle);

                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.color = new Color(0.2f, 0.85f, 0.45f);
                if (GUILayout.Button("Conectar e Entrar »", primaryActionBtnStyle, GUILayout.Width(170), GUILayout.Height(26)))
                {
                    StartConnectingToServer(s.serverName);
                }
                GUI.color = Color.white;

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(6);
            }

            if (serverList.Count == 0)
            {
                GUILayout.Space(40);
                GUILayout.Label("Nenhum servidor encontrado.", labelBoldStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawHostServer()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("CONFIGURAÇÕES DO SERVIDOR DEDICADO / SALA", labelBoldStyle);
            GUILayout.Space(8);

            GUILayout.Label("Nome da Sala / Servidor:", labelRegularStyle);
            hostServerName = GUILayout.TextField(hostServerName, 50, modernTextFieldStyle, GUILayout.Height(26));
            GUILayout.Space(6);

            GUILayout.Label("Região do Planeta:", labelRegularStyle);
            hostRegionName = GUILayout.TextField(hostRegionName, 50, modernTextFieldStyle, GUILayout.Height(26));
            GUILayout.Space(6);

            GUILayout.Label("Modo de Jogo:", labelRegularStyle);
            GUILayout.BeginHorizontal();
            string[] modes = { "Co-op Agrícola", "Logística Comercial", "Voo Livre", "Sandbox Total" };
            foreach (var m in modes)
            {
                bool isSelected = hostGameMode == m;
                GUI.color = isSelected ? new Color(0.25f, 0.65f, 1f) : Color.white;
                if (GUILayout.Button(m, isSelected ? activeTabButtonStyle : tabButtonStyle, GUILayout.Height(24)))
                {
                    hostGameMode = m;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            GUILayout.Label($"Máximo de Jogadores Simultâneos: {hostMaxPlayers}", labelRegularStyle);
            hostMaxPlayers = (int)GUILayout.HorizontalSlider(hostMaxPlayers, 2, 32);
            GUILayout.Space(6);

            GUILayout.Label("Senha da Sala (Opcional para amigos):", labelRegularStyle);
            hostPassword = GUILayout.PasswordField(hostPassword, '*', 20, modernTextFieldStyle, GUILayout.Height(26));
            GUILayout.Space(6);

            GUILayout.Label("Porta de Rede (Padrão 7777):", labelRegularStyle);
            string portStr = GUILayout.TextField(hostPort.ToString(), 6, modernTextFieldStyle, GUILayout.Height(26));
            if (int.TryParse(portStr, out int parsedPort)) hostPort = parsedPort;
            GUILayout.Space(16);

            GUI.color = new Color(0.2f, 0.85f, 0.45f);
            if (IconBtn(TerraIcon.Airplane, "INICIAR SERVIDOR & ABRIR SALA", primaryActionBtnStyle, GUILayout.Height(38)))
            {
                serverList.Insert(0, new MultiplayerServerInfo
                {
                    serverName = hostServerName,
                    region = hostRegionName,
                    gameMode = hostGameMode,
                    currentPlayers = 1,
                    maxPlayers = hostMaxPlayers,
                    pingMs = 1,
                    hasPassword = !string.IsNullOrEmpty(hostPassword),
                    hostName = "Você (Host Local)"
                });
                currentMultiplayerTab = MultiplayerTab.ServerBrowser;
                ShowNotification("Servidor iniciado com sucesso! Aguardando outros jogadores...");
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
        }

        private void DrawDirectConnect()
        {
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("CONEXÃO DIRETA POR ENDEREÇO IP OU CÓDIGO", labelBoldStyle);
            GUILayout.Space(8);

            GUILayout.Label("Endereço IP & Porta (ex: 192.168.1.100:7777):", labelRegularStyle);
            directConnectIP = GUILayout.TextField(directConnectIP, 30, modernTextFieldStyle, GUILayout.Height(26));
            GUILayout.Space(6);

            if (GUILayout.Button("Conectar por Endereço IP", primaryActionBtnStyle, GUILayout.Height(30)))
            {
                StartConnectingToServer(directConnectIP);
            }

            GUILayout.Space(14);
            GUI.color = new Color(0.35f, 0.75f, 1.0f, 0.25f);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            GUILayout.Space(10);

            GUILayout.Label("Ou digite o Código da Sessão de Amigo (ex: TERRA-BR84):", labelRegularStyle);
            sessionCode = GUILayout.TextField(sessionCode, 16, modernTextFieldStyle, GUILayout.Height(26));
            GUILayout.Space(6);

            if (GUILayout.Button("Conectar por Código de Sessão", primaryActionBtnStyle, GUILayout.Height(30)))
            {
                StartConnectingToServer($"Código {sessionCode}");
            }

            GUILayout.EndVertical();
        }

        private void StartConnectingToServer(string target)
        {
            isConnecting = true;
            connectTimer = 2.0f;
            connectingServerName = target;
        }

        private void DrawConnectingFeedback()
        {
            GUILayout.Space(40);
            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("ESTABELECENDO CONEXÃO DE REDE...", labelBoldStyle);
            GUILayout.Space(6);
            GUILayout.Label($"Negociando handshake com: {connectingServerName}", labelRegularStyle);
            GUILayout.Label("Sincronizando estado do mundo e dados territoriais com o servidor remoto...", menuBtnSubtextStyle);
            GUILayout.Space(14);

            // Barra de progresso animada
            float progress = 1.0f - (connectTimer / 2.0f);
            Rect progressRect = GUILayoutUtility.GetRect(GUILayoutUtility.GetLastRect().width, 16);
            GUI.color = new Color(0.1f, 0.2f, 0.3f);
            GUI.DrawTexture(progressRect, whiteTex);
            GUI.color = new Color(0.3f, 0.85f, 1.0f);
            GUI.DrawTexture(new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height), whiteTex);
            GUI.color = Color.white;

            GUILayout.Space(10);
            if (GUILayout.Button("Cancelar Conexão", actionBtnStyle, GUILayout.Width(140), GUILayout.Height(26)))
            {
                isConnecting = false;
            }
            GUILayout.EndVertical();
        }
    }
}
