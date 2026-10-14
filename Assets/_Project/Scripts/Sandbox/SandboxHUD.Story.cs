using System.Collections.Generic;
using UnityEngine;
using ProjectTerra.Gameplay;

namespace ProjectTerra.Sandbox
{
    /// <summary>
    /// Camada narrativa do celular: recebimento de SMS da campanha, app Mensagens e o
    /// overlay de ligação / conversa com NPCs (acionado pelo CareerStoryManager).
    /// </summary>
    public partial class SandboxHUD
    {
        // ---- Conversa ativa (ligação ou conversa no mundo) ----
        private readonly Queue<string> convoLines = new Queue<string>();
        private DialogueData convoContact;
        private System.Action convoOnFinished;
        private bool convoActive = false;
        private bool convoAnswered = false;
        private string convoCurrentLine = "";

        /// <summary>True enquanto uma ligação/conversa toma a tela (bloqueia gameplay).</summary>
        public bool IsConversationActive => convoActive;

        /// <summary>Recebe uma mensagem de texto da campanha na caixa de entrada.</summary>
        public void ReceiveStorySms(DialogueData d, RegionSaveData save)
        {
            if (d == null || save == null) return;
            if (d.lines != null)
            {
                foreach (var line in d.lines)
                    save.AddSms(d.contactName, d.portrait, line);
            }
            ShowToast($"✉ {d.contactName}: nova mensagem", 3f);
            if (SaveManager.Instance != null) SaveManager.Instance.SaveToFile(save);
        }

        /// <summary>Abre o overlay de ligação / conversa. onFinished dispara ao encerrar.</summary>
        public void ShowConversation(DialogueData d, System.Action onFinished)
        {
            if (d == null || d.lines == null || d.lines.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            convoLines.Clear();
            foreach (var line in d.lines) convoLines.Enqueue(line);
            convoContact = d;
            convoOnFinished = onFinished;
            convoActive = true;
            convoAnswered = d.channel != DialogueChannel.Call; // conversa no mundo já "atendida"
            convoCurrentLine = convoAnswered ? convoLines.Dequeue() : "";

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void AdvanceConversation()
        {
            if (!convoAnswered)
            {
                convoAnswered = true;
                convoCurrentLine = convoLines.Count > 0 ? convoLines.Dequeue() : "";
                return;
            }

            if (convoLines.Count > 0)
            {
                convoCurrentLine = convoLines.Dequeue();
                return;
            }

            // Fim da conversa.
            convoActive = false;
            convoContact = null;
            var cb = convoOnFinished;
            convoOnFinished = null;
            if (!IsPhoneOpen && !IsSettingsOpen) RestoreGameplayCursor();
            cb?.Invoke();
        }

        /// <summary>Desenha o overlay modal de ligação/conversa (por cima de tudo).</summary>
        private void DrawConversationOverlay()
        {
            if (!convoActive || convoContact == null) return;

            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = Color.white;

            float w = 460f;
            float h = 230f;
            Rect card = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            bool isCall = convoContact.channel == DialogueChannel.Call;
            Color accent = convoContact.urgent ? new Color(0.95f, 0.35f, 0.3f)
                        : isCall ? new Color(0.3f, 0.85f, 0.5f)
                        : new Color(0.3f, 0.65f, 1f);

            GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.99f);
            GUI.DrawTexture(card, whiteTex);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(card.x, card.y, w, 3f), whiteTex);
            GUI.color = Color.white;

            // Retrato.
            Rect portrait = new Rect(card.x + 18f, card.y + 18f, 64f, 64f);
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.22f);
            GUI.DrawTexture(portrait, whiteTex);
            GUI.color = Color.white;
            GUI.Label(portrait, string.IsNullOrEmpty(convoContact.portrait) ? "📞" : convoContact.portrait, phoneIconStyle);

            // Cabeçalho: nome + canal.
            string channelLabel = isCall ? (convoContact.urgent ? "LIGAÇÃO URGENTE" : "LIGAÇÃO") : "CONVERSA";
            GUI.Label(new Rect(card.x + 94f, card.y + 20f, w - 110f, 20f), channelLabel, phoneSmallStyle);
            GUI.Label(new Rect(card.x + 94f, card.y + 38f, w - 110f, 28f), convoContact.contactName, phoneHeaderStyle);

            // Corpo: linha atual (ou "chamando..." antes de atender).
            Rect bodyRect = new Rect(card.x + 20f, card.y + 96f, w - 40f, 84f);
            string text = convoAnswered ? convoCurrentLine : $"{convoContact.contactName} está ligando…";
            GUI.Label(bodyRect, text, phoneBodyStyle);

            // Botões.
            Rect btn = new Rect(card.x + w - 170f, card.y + h - 42f, 150f, 30f);
            if (!convoAnswered)
            {
                if (GUI.Button(btn, "Atender", menuButtonPrimaryStyle)) AdvanceConversation();
                Rect decline = new Rect(card.x + 20f, card.y + h - 42f, 130f, 30f);
                if (GUI.Button(decline, "Ignorar", phoneListButtonStyle))
                {
                    // Ignorar mesmo assim segue a estória (não trava a campanha).
                    convoLines.Clear();
                    convoAnswered = true;
                    AdvanceConversation();
                }
            }
            else
            {
                bool last = convoLines.Count == 0;
                if (GUI.Button(btn, last ? "Encerrar" : "Continuar", menuButtonPrimaryStyle))
                    AdvanceConversation();
            }
        }

        // ---- App: Mensagens ----
        private void DrawPhoneMessages(Rect content)
        {
            DrawAppHeader(content, "Mensagens");
            Rect area = new Rect(content.x, content.y + 32f, content.width, content.height - 32f);
            GUILayout.BeginArea(area);
            phoneScroll = GUILayout.BeginScrollView(phoneScroll);

            var save = CurrentSave;
            if (save == null || save.inbox == null || save.inbox.Count == 0)
            {
                GUILayout.Label("Nenhuma mensagem ainda.", phoneSmallStyle);
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            // Mais recentes primeiro.
            for (int i = save.inbox.Count - 1; i >= 0; i--)
            {
                var sms = save.inbox[i];
                if (sms == null) continue;

                string head = $"{(string.IsNullOrEmpty(sms.portrait) ? "✉" : sms.portrait)} {sms.contactName}";
                if (!sms.read) head = "● " + head;
                GUILayout.Label(head, phoneSectionStyle);
                GUILayout.Label(sms.body, phoneBodyStyle);
                GUILayout.Label($"Dia {sms.day}", phoneSmallStyle);
                GUILayout.Space(4);
                DrawPhoneDivider();
                GUILayout.Space(5);

                sms.read = true; // marca como lida ao visualizar
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
