using UnityEngine;
using ProjectTerra.UI;

namespace ProjectTerra.Sandbox
{
    public partial class SandboxHUD
    {
        #region Estilização GUI

        private void InitStyles()
        {
            if (stylesReady) return;

            pillStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(8, 8, 8, 8)
            };
            pillStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.05f, 0.14f, 0.28f, 0.20f), new Color(0.35f, 0.65f, 0.95f, 0.35f), 1.5f, 8f, 32);

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

            missionCardStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(8, 8, 8, 8)
            };
            missionCardStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.05f, 0.14f, 0.28f, 0.20f), new Color(1f, 0.82f, 0.25f, 0.55f), 1.5f, 8f, 32);

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

            telemetryBoxStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(8, 8, 8, 8)
            };
            telemetryBoxStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.05f, 0.14f, 0.28f, 0.20f), new Color(0.35f, 0.65f, 0.95f, 0.35f), 1.5f, 8f, 32);

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

            hintBoxStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hintBoxStyle.normal.textColor = Color.white;

            compassLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            compassLabelStyle.normal.textColor = new Color(0.8f, 0.95f, 1f);

            modalBoxStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(10, 10, 10, 10)
            };
            modalBoxStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.05f, 0.07f, 0.11f, 0.98f), new Color(0.25f, 0.7f, 1f, 0.9f), 1.5f, 10f, 32);

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
            UIThemeHelper.ApplyModernButtonSkin(menuButtonStyle);

            menuButtonPrimaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            UIThemeHelper.ApplyPrimaryButtonSkin(menuButtonPrimaryStyle);
            menuButtonPrimaryStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);

            menuButtonDangerStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            UIThemeHelper.ApplyDangerButtonSkin(menuButtonDangerStyle);
            menuButtonDangerStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            toastStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);

            // Novos estilos do HUD
            hudBoxTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            hudBoxTitleStyle.normal.textColor = new Color(0.65f, 0.82f, 1f, 0.85f);

            hudBoxSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft
            };
            hudBoxSubStyle.normal.textColor = Color.white;

            hudSpeedBigStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hudSpeedBigStyle.normal.textColor = Color.white;

            hudGearTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hudGearTextStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            hudMiniValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight
            };
            hudMiniValueStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);

            hudKeyBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            hudKeyBadgeStyle.normal.textColor = new Color(1f, 0.88f, 0.45f);

            hudNeedValStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hudNeedValStyle.normal.textColor = new Color(0.92f, 0.97f, 1f);
            hudNeedValStyle.padding = new RectOffset(0, 0, 0, 0);
            hudNeedValStyle.margin = new RectOffset(0, 0, 0, 0);

            hudNeedColLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 8,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hudNeedColLabelStyle.normal.textColor = new Color(0.70f, 0.85f, 1f, 0.90f);
            hudNeedColLabelStyle.padding = new RectOffset(0, 0, 0, 0);
            hudNeedColLabelStyle.margin = new RectOffset(0, 0, 0, 0);

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
            UIThemeHelper.ApplyModernButtonSkin(phoneBackStyle);

            phoneUrlStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(8, 8, 3, 3)
            };
            phoneUrlStyle.normal.background = UIThemeHelper.CreateRoundedTexture(new Color(0.10f, 0.14f, 0.22f, 0.95f), new Color(0.25f, 0.55f, 0.85f, 0.70f), 1.2f, 8f, 32);
            phoneUrlStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);

            phoneSectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            phoneSectionStyle.normal.textColor = Color.white;

            phoneListButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 6, 4, 4)
            };
            UIThemeHelper.ApplyModernButtonSkin(phoneListButtonStyle);

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

            phoneCardStyle = UIThemeHelper.CreateRoundedCardStyle(
                new Color(0.04f, 0.08f, 0.14f, 0.65f),
                new Color(0.20f, 0.50f, 0.80f, 0.40f),
                radius: 8f,
                borderThickness: 1.2f,
                padding: new RectOffset(10, 10, 8, 8)
            );

            menuCardSectionStyle = UIThemeHelper.CreateRoundedCardStyle(
                new Color(0.06f, 0.10f, 0.16f, 0.85f),
                new Color(0.20f, 0.50f, 0.80f, 0.45f),
                radius: 8f,
                borderThickness: 1.2f,
                padding: new RectOffset(12, 12, 10, 10)
            );

            // Estilos da Barra Horizontal de Construção (Menu Inferior)
            buildBarTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            buildBarTitleStyle.normal.textColor = Color.white;

            buildTabStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buildTabStyle.normal.textColor = Color.white;

            buildSubtabStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            buildSubtabStyle.normal.textColor = Color.white;

            buildCloseBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyModernButtonSkin(buildCloseBtnStyle);

            buildArrowStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UIThemeHelper.ApplyModernButtonSkin(buildArrowStyle);

            buildCardNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            buildCardNameStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);

            buildCardPriceStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buildCardPriceStyle.normal.textColor = new Color(0.35f, 0.95f, 0.55f);

            buildCardLockedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buildCardLockedStyle.normal.textColor = new Color(1f, 0.72f, 0.3f);

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

            UIThemeHelper.DrawRoundedBox(rect, new Color(0.04f, 0.15f, 0.08f, 0.95f), new Color(0.3f, 1f, 0.5f, 0.90f), 8f, 1.5f);

            GUI.Label(rect, saveToastMessage, toastStyle);
        }

        #endregion
    }
}
