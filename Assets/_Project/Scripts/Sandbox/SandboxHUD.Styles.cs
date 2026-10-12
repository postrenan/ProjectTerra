using UnityEngine;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Estilização GUI

        private void InitStyles()
        {
            if (stylesReady) return;

            pillStyle = new GUIStyle(GUI.skin.box);
            pillStyle.normal.background = whiteTex;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = Color.white;

            moneyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            moneyStyle.normal.textColor = new Color(0.35f, 1f, 0.55f);

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal
            };
            subtitleStyle.normal.textColor = new Color(0.75f, 0.88f, 1f);

            timeWarpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            timeWarpStyle.normal.textColor = new Color(1f, 0.85f, 0.25f);

            missionCardStyle = new GUIStyle(GUI.skin.box);
            missionCardStyle.normal.background = whiteTex;

            missionTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            missionTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.3f);

            missionDescStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                wordWrap = true
            };
            missionDescStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

            telemetryBoxStyle = new GUIStyle(GUI.skin.box);
            telemetryBoxStyle.normal.background = whiteTex;

            speedValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            speedValueStyle.normal.textColor = Color.white;

            speedUnitStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.LowerLeft
            };
            speedUnitStyle.normal.textColor = new Color(0.4f, 0.85f, 1f);

            hintBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hintBoxStyle.normal.background = whiteTex;
            hintBoxStyle.normal.textColor = Color.white;

            compassLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            compassLabelStyle.normal.textColor = new Color(0.8f, 0.95f, 1f);

            modalBoxStyle = new GUIStyle(GUI.skin.box);
            modalBoxStyle.normal.background = whiteTex;

            modalTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            modalTitleStyle.normal.textColor = Color.white;

            menuButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            menuButtonPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            menuButtonPrimaryStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);

            menuButtonDangerStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            menuButtonDangerStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            toastStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            toastStyle.normal.background = whiteTex;
            toastStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);

            // ---- Estilos do Celular ----
            phoneStatusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            phoneStatusStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

            phoneHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            phoneHeaderStyle.normal.textColor = Color.white;

            phoneIconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter
            };
            phoneIconStyle.normal.textColor = Color.white;

            phoneAppLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false
            };
            phoneAppLabelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

            phoneBackStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            phoneUrlStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
            phoneUrlStyle.normal.background = whiteTex;
            phoneUrlStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

            phoneSectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            phoneSectionStyle.normal.textColor = new Color(0.4f, 0.85f, 1f);

            phoneListButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 6, 4, 4)
            };

            phoneLinkStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            phoneLinkStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);

            phoneBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
            phoneBodyStyle.normal.textColor = new Color(0.88f, 0.93f, 1f);

            phoneSmallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true
            };
            phoneSmallStyle.normal.textColor = new Color(0.68f, 0.76f, 0.88f);

            phoneLockStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            phoneLockStyle.normal.textColor = new Color(1f, 0.72f, 0.3f);

            phoneStatusRightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            phoneStatusRightStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

            phoneClockWidgetStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            phoneClockWidgetStyle.normal.textColor = Color.white;

            phoneClockSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            phoneClockSubStyle.normal.textColor = new Color(0.7f, 0.85f, 1f, 0.85f);

            stylesReady = true;
        }

        #endregion

        #region Notificação Toast

        public void ShowToast(string message, float duration = 2.5f)
        {
            saveToastMessage = message;
            saveToastTimer = duration;
        }

        private void DrawToastNotification()
        {
            if (string.IsNullOrEmpty(saveToastMessage) || saveToastTimer <= 0f) return;

            float w = 320f;
            float h = 34f;
            Rect rect = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.2f, w, h);

            GUI.color = new Color(0.04f, 0.15f, 0.08f, 0.95f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = new Color(0.3f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, w, 2), whiteTex);
            GUI.color = Color.white;

            GUI.Label(rect, saveToastMessage, toastStyle);
        }

        #endregion
    }
}
