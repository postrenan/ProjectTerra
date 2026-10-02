using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class InGameCommandConsole
    {
        private void InitStyles()
        {
            if (stylesReady) return;

            windowStyle = new GUIStyle(GUI.skin.box);
            windowStyle.normal.background = whiteTex;

            inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            inputStyle.normal.textColor = Color.white;

            logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Italic
            };
            hintStyle.normal.textColor = new Color(0.7f, 0.85f, 1.0f, 0.85f);

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.2f, 0.8f, 1f);

            btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            stylesReady = true;
        }

        private void OnGUI()
        {
            // Atalho de caractere alternativo para teclados internacionais (ex: ABNT2)
            if (!IsOpen && Event.current.type == EventType.KeyDown && Event.current.character == '/')
            {
                OpenConsole();
                Event.current.Use();
                return;
            }

            if (!IsOpen) return;

            InitStyles();

            float w = Mathf.Min(680f, Screen.width * 0.9f);
            float h = 330f;
            float x = 20f;
            float y = Screen.height - h - 45f;
            Rect consoleRect = new Rect(x, y, w, h);

            // Fundo escuro aerodinâmico com borda ciano
            GUI.color = new Color(0.03f, 0.06f, 0.10f, 0.94f);
            GUI.DrawTexture(consoleRect, whiteTex);
            GUI.color = new Color(0.2f, 0.75f, 1.0f, 0.9f);
            GUI.DrawTexture(new Rect(consoleRect.x, consoleRect.y, consoleRect.width, 2), whiteTex);
            GUI.DrawTexture(new Rect(consoleRect.x, consoleRect.y + 28f, consoleRect.width, 1), whiteTex);
            GUI.color = Color.white;

            // Barra de Título
            GUI.Label(new Rect(x + 12f, y + 4f, 450f, 22f), "💬 CONSOLE DE COMANDOS REGIONAIS  •  [ENTER] Executar  •  [ESC] Fechar", titleStyle);
            if (GUI.Button(new Rect(x + w - 30f, y + 4f, 24f, 20f), "✖", btnStyle))
            {
                CloseConsole();
                return;
            }

            // Histórico de Mensagens / Logs
            Rect logAreaRect = new Rect(x + 10f, y + 32f, w - 20f, h - 85f);
            GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.85f);
            GUI.DrawTexture(logAreaRect, whiteTex);
            GUI.color = Color.white;

            GUILayout.BeginArea(logAreaRect);
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var msg in messageLog)
            {
                logStyle.normal.textColor = msg.color;
                GUILayout.Label($"<color=#708090>[{msg.timestamp}]</color> {msg.text}", logStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Sugestão de Autocomplete / Sintaxe
            string hint = GetAutocompleteHint(inputCommand);
            if (!string.IsNullOrEmpty(hint))
            {
                GUI.Label(new Rect(x + 12f, y + h - 50f, w - 24f, 18f), $"💡 Sugestão: {hint}  <color=#888888>(Pressione TAB para completar)</color>", hintStyle);
            }

            // Barra de Entrada de Texto
            Rect inputRect = new Rect(x + 10f, y + h - 30f, w - 100f, 24f);
            GUI.SetNextControlName(InputControlName);
            inputCommand = GUI.TextField(inputRect, inputCommand, inputStyle);

            if (focusPending)
            {
                GUI.FocusControl(InputControlName);
                // Move o cursor de edição para o fim da string
                var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (te != null)
                {
                    te.cursorIndex = inputCommand.Length;
                    te.selectIndex = inputCommand.Length;
                }
                focusPending = false;
            }

            // Botão Executar
            Rect submitRect = new Rect(x + w - 85f, y + h - 30f, 75f, 24f);
            if (GUI.Button(submitRect, "Executar", btnStyle))
            {
                SubmitCommand();
            }

            // Tratamento de Teclas Especiais no Console
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    SubmitCommand();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Tab)
                {
                    ApplyAutocomplete();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    NavigateHistory(1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.DownArrow)
                {
                    NavigateHistory(-1);
                    e.Use();
                }
            }
        }

        private string GetAutocompleteHint(string currentInput)
        {
            if (string.IsNullOrEmpty(currentInput) || !currentInput.StartsWith("/")) return "";

            string lower = currentInput.ToLower();
            foreach (var cmd in availableCommands)
            {
                if (cmd.StartsWith(lower) && cmd != lower)
                {
                    return cmd;
                }
            }

            // Dicas de parâmetros comuns
            if (lower.StartsWith("/spawn "))
            {
                return "/spawn [tractor | truck | plane | boat | car | sports | suv | police | ambulance | firetruck | taxi | van | speeder | all]";
            }
            if (lower.StartsWith("/time "))
            {
                return "/time [0-24 | day | night | noon | midnight | sunset | sunrise | cycle <on/off>]";
            }
            if (lower.StartsWith("/weather "))
            {
                return "/weather [clear | fog | densefog | overcast | storm]";
            }
            if (lower.StartsWith("/tp "))
            {
                return "/tp [town | base | market | airport | port | origin | <x> <z> | <x> <y> <z>]";
            }
            if (lower.StartsWith("/money "))
            {
                return "/money [quantia]  (ex: /money 100000)";
            }
            if (lower.StartsWith("/speed "))
            {
                return "/speed [multiplicador]  (ex: /speed 2.5)";
            }

            return "";
        }
    }
}
