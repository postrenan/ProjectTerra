using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTerra.UI
{
    /// <summary>
    /// Identificador dos ícones de interface do Project Terra.
    /// Baseado no catálogo aberto Lucide Icons (licença comercial gratuita ISC / MIT).
    /// </summary>
    public enum TerraIcon
    {
        // Carreiras & Veículos
        Tractor,
        HeavyTruck,
        Airplane,
        FishingBoat,
        UtilityVehicle,
        SportsCar,
        Van,
        Pickup,
        LuxuryYacht,
        SpeedBoat,
        Sailboat,

        // Economia & Finanças
        Wallet,
        Bank,
        CreditCard,
        CargoBox,
        Analytics,
        DriverLicense,
        ReputationStar,
        TrendingUp,
        TrendingDown,
        Briefcase,
        Contract,
        Shop,

        // Geografia & Recursos
        Crops,
        Forest,
        Mining,
        WaterDrop,
        Harbor,
        Airport,
        LocationPin,
        Mountain,
        Volcano,
        Desert,
        Park,
        Orchard,
        Seedling,
        Historic,
        Metropolis,
        House,
        Industrial,
        Market,

        // Animais & Pecuária
        Cow,
        Horse,
        Sheep,
        Milk,
        Wool,
        Fish,
        Fox,
        Wolf,

        // HUD, Sistema & Telemetria
        Headlights,
        WarningAlert,
        IgnitionKey,
        Lock,
        Battery,
        Satellite,
        Phone,
        PhoneCall,
        Volume,
        Clock,
        Pause,
        FastForward,
        CancelCross,
        Checkmark,
        Shield,
        Search,
        Compass,
        PaintBrush,
        Tools,
        Wrench,
        Fuel,
        Calendar,
        Radio,
        Newspaper,
        Trash,
        Flag,
        Settings,
        Info
    }

    /// <summary>
    /// Serviço de ícones de interface do Project Terra:
    /// - Carrega os ícones oficiais Lucide (PNGs de alta resolução 64x64 com fundo transparente).
    /// - Mantém cache estático de Texture2D e GUIContent para zero GC.Alloc por frame no OnGUI.
    /// - Fornece fallback procedural automático caso um arquivo não esteja presente no disco.
    /// </summary>
    public static class TerraIcons
    {
        private static readonly Dictionary<TerraIcon, Texture2D> iconCache = new Dictionary<TerraIcon, Texture2D>();
        private static readonly Dictionary<(string, TerraIcon), GUIContent> contentCache = new Dictionary<(string, TerraIcon), GUIContent>();
        private static string iconFolderPath;

        static TerraIcons()
        {
            iconFolderPath = Path.Combine(Application.dataPath, "_Project/Textures/Icons/png");
        }

        /// <summary>
        /// Obtém a Texture2D para o ícone solicitado (do Lucide PNG ou gerada sob demanda).
        /// </summary>
        public static Texture2D Get(TerraIcon icon)
        {
            if (iconCache.TryGetValue(icon, out var tex) && tex != null)
                return tex;

            // 1. Tenta carregar o PNG oficial Lucide
            try
            {
                string pngFile = Path.Combine(iconFolderPath, $"{icon}.png");
                if (File.Exists(pngFile))
                {
                    byte[] rawData = File.ReadAllBytes(pngFile);
                    var lucideTex = new Texture2D(64, 64, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        name = $"Lucide_{icon}"
                    };

                    if (ImageConversion.LoadImage(lucideTex, rawData))
                    {
                        iconCache[icon] = lucideTex;
                        return lucideTex;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TerraIcons] Falha ao carregar PNG do Lucide para {icon}: {ex.Message}");
            }

            // 2. Fallback procedural de segurança
            tex = GenerateFallbackTexture(icon);
            iconCache[icon] = tex;
            return tex;
        }

        /// <summary>
        /// Retorna um GUIContent combinando texto e ícone nativo sem alocar novo objeto por frame se já em cache.
        /// </summary>
        public static GUIContent Content(string text, TerraIcon icon, string tooltip = "")
        {
            var key = (text, icon);
            if (contentCache.TryGetValue(key, out var content) && content != null)
                return content;

            content = new GUIContent(text, Get(icon), tooltip);
            contentCache[key] = content;
            return content;
        }

        /// <summary>
        /// Desenha o ícone na posição e dimensão indicada com tint opcional.
        /// </summary>
        public static void Draw(Rect rect, TerraIcon icon, Color? tint = null)
        {
            var prev = GUI.color;
            if (tint.HasValue)
                GUI.color = tint.Value;

            GUI.DrawTexture(rect, Get(icon), ScaleMode.ScaleToFit);

            if (tint.HasValue)
                GUI.color = prev;
        }

        /// <summary>
        /// Remove quaisquer emojis remanescentes de uma string para evitar caracteres corrompidos em fontes IMGUI.
        /// </summary>
        public static string CleanString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new System.Text.StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                int code = char.ConvertToUtf32(text, i);
                if (char.IsSurrogatePair(text, i))
                    i++;

                // Ignora faixas de emojis Unicode comuns
                if ((code >= 0x1F300 && code <= 0x1FAD6) ||
                    (code >= 0x2600 && code <= 0x27BF) ||
                    (code >= 0x2300 && code <= 0x23FF) ||
                    (code >= 0x2B50 && code <= 0x2B55) ||
                    (code >= 0x203C && code <= 0x2049))
                {
                    continue;
                }

                sb.Append(char.ConvertFromUtf32(code));
            }
            return sb.ToString().TrimStart();
        }

        #region Fallback Procedural de Segurança

        private static Texture2D GenerateFallbackTexture(TerraIcon icon)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"TerraIconFallback_{icon}"
            };

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            Color accent = new Color(0.4f, 0.85f, 1f, 1f);
            for (int dy = 6; dy < 26; dy++)
            {
                for (int dx = 6; dx < 26; dx++)
                {
                    if (dx == 6 || dx == 25 || dy == 6 || dy == 25)
                        pixels[dy * size + dx] = accent;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return tex;
        }

        #endregion
    }
}
