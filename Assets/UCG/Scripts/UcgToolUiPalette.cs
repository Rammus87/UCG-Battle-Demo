using UnityEngine;

namespace UCG
{
    internal static class UcgToolUiPalette
    {
        public const string BrandPinkHex = "#E83F8C";
        public const string BrandPinkLightHex = "#FF63AA";
        public const string SoftWhiteHex = "#FFFFFF";
        public const string MutedWhiteHex = "#B9C4D0";

        public static readonly Color BrandPink = Rgba(232, 63, 140, 1f);
        public static readonly Color BrandPinkLight = Rgba(255, 99, 170, 1f);
        public static readonly Color FocusCyan = Rgba(109, 226, 255, 1f);
        public static readonly Color DeepGlass = Rgba(7, 12, 22, 0.78f);
        public static readonly Color ToastGlass = Rgba(15, 23, 42, 0.9f);
        public static readonly Color GlassBorder = Rgba(109, 226, 255, 0.32f);
        public static readonly Color SoftWhite = Rgba(255, 255, 255, 0.92f);
        public static readonly Color MutedWhite = Rgba(255, 255, 255, 0.68f);
        public static readonly Color WarningGold = Rgba(246, 200, 95, 1f);

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        static Color Rgba(int r, int g, int b, float a)
        {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }
    }
}
