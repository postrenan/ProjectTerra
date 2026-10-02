using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public struct ConsoleMessage
    {
        public string text;
        public Color color;
        public string timestamp;

        public ConsoleMessage(string text, Color color)
        {
            this.text = text;
            this.color = color;
            this.timestamp = DateTime.Now.ToString("HH:mm:ss");
        }
    }

    /// <summary>
    /// Console de Comandos e Chat In-Game da Província:
    /// - Atalho [/]: Abre o console com foco imediato no campo de digitação.
    /// - Histórico com Setas Cima/Baixo e Auto-complete com [Tab].
    /// - Suporta mais de 25 comandos funcionais: /spawn, /time, /weather, /tp, /money, /fly, /speed, /refuel, /repair, etc.
    /// </summary>
    public partial class InGameCommandConsole : MonoBehaviour
    {
        public static InGameCommandConsole Instance { get; private set; }

        public bool IsOpen { get; private set; } = false;

        private string inputCommand = "";
        private readonly List<string> commandHistory = new List<string>();
        private int historyIndex = -1;

        private readonly List<ConsoleMessage> messageLog = new List<ConsoleMessage>();
        private Vector2 scrollPos = Vector2.zero;
        private Texture2D whiteTex;

        // Estilos GUI
        private GUIStyle windowStyle;
        private GUIStyle inputStyle;
        private GUIStyle logStyle;
        private GUIStyle hintStyle;
        private GUIStyle titleStyle;
        private GUIStyle btnStyle;
        private bool stylesReady = false;

        private bool focusPending = false;
        private const string InputControlName = "CommandChatInput";

        // Comandos disponíveis para autocomplete e help
        private readonly List<string> availableCommands = new List<string>
        {
            "/help",
            "/spawn",
            "/time",
            "/timescale",
            "/weather",
            "/tp",
            "/speed",
            "/fly",
            "/god",
            "/money",
            "/setmoney",
            "/refuel",
            "/repair",
            "/heal",
            "/unflip",
            "/cargo",
            "/mission",
            "/reputation",
            "/light",
            "/camera",
            "/vehicles",
            "/switch",
            "/clearvehicles",
            "/coords",
            "/pos",
            "/hud",
            "/map",
            "/save",
            "/clear"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();

            LogMessage("🌍 Bem-vindo ao Console de Comandos de Project Terra!", new Color(0.3f, 0.85f, 1.0f));
            LogMessage("Pressione [/] a qualquer momento para abrir ou digite /help para a lista completa.", new Color(0.85f, 0.85f, 0.85f));
        }

        private void Update()
        {
            // Atalho [/] para abrir o console
            if (!IsOpen)
            {
                if (Input.GetKeyDown(KeyCode.Slash) || Input.GetKeyDown(KeyCode.KeypadDivide))
                {
                    OpenConsole();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseConsole();
                }
            }
        }

        public void OpenConsole()
        {
            IsOpen = true;
            inputCommand = "/";
            historyIndex = -1;
            focusPending = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseConsole()
        {
            IsOpen = false;
            inputCommand = "";
            historyIndex = -1;

            if (SandboxHUD.Instance == null || (!SandboxHUD.Instance.IsRegionalMapOpen && !SandboxHUD.Instance.IsOptionsMenuOpen))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void ToggleConsole()
        {
            if (IsOpen) CloseConsole();
            else OpenConsole();
        }

        public void LogMessage(string text, Color color)
        {
            messageLog.Add(new ConsoleMessage(text, color));
            if (messageLog.Count > 100) messageLog.RemoveAt(0);
            scrollPos.y = float.MaxValue;
        }

        private void SubmitCommand()
        {
            string cmd = (inputCommand ?? "").Trim();

            if (string.IsNullOrEmpty(cmd) || cmd == "/")
            {
                CloseConsole();
                return;
            }

            // Adiciona ao histórico
            commandHistory.Insert(0, cmd);
            if (commandHistory.Count > 40) commandHistory.RemoveAt(commandHistory.Count - 1);

            // Loga o comando emitido
            LogMessage($"> {cmd}", new Color(0.95f, 0.95f, 0.95f));

            // Executa
            ExecuteCommand(cmd);

            // Fecha o console para retornar ao gameplay com feedback visível
            CloseConsole();
        }

        private void NavigateHistory(int direction)
        {
            if (commandHistory.Count == 0) return;

            historyIndex = Mathf.Clamp(historyIndex + direction, -1, commandHistory.Count - 1);
            if (historyIndex >= 0)
            {
                inputCommand = commandHistory[historyIndex];
            }
            else
            {
                inputCommand = "/";
            }
            focusPending = true;
        }

        private void ApplyAutocomplete()
        {
            if (string.IsNullOrEmpty(inputCommand) || !inputCommand.StartsWith("/")) return;

            string lower = inputCommand.ToLower();
            foreach (var cmd in availableCommands)
            {
                if (cmd.StartsWith(lower))
                {
                    inputCommand = cmd + " ";
                    focusPending = true;
                    break;
                }
            }
        }
    }
}
