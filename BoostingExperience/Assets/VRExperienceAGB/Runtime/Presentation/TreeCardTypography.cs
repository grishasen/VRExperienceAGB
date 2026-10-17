using TMPro;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    internal static class TreeCardTypography
    {
        private static TMP_FontAsset medium, semibold;
        public static void SetFont(TMP_Text text, bool heading = false)
        {
            if (medium == null) medium = Resources.Load<TMP_FontAsset>("TreeCardFonts/Inter-Medium");
            if (semibold == null) semibold = Resources.Load<TMP_FontAsset>("TreeCardFonts/Inter-SemiBold");
            var font = heading ? semibold : medium;
            if (font != null) { text.font = font; text.fontSharedMaterial = font.material; }
        }
        public static void Apply(TMP_Text text, bool heading = false)
        {
            SetFont(text,heading);
            text.fontStyle = FontStyles.Normal;
            text.enableAutoSizing = false;
            text.richText = false;
            text.color = new Color(.93f, .96f, .98f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.lineSpacing = 8;
        }
    }
}
