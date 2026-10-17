using System;
using UnityEngine;
using VRExperienceAGB.Import;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Identifier colors shared by labels, composition strips and the source legend.</summary>
    public static class PredictorPalette
    {
        public static string Family(string id) => id.StartsWith("py", StringComparison.Ordinal) ? "py" : ForestExplanation.Family(id);
        public static Color ForFeature(string id) => ForFamily(Family(id));
        public static Color ForFamily(string family)
        {
            switch (family) {
                case "Customer": return new Color32(103, 213, 245, 255);
                case "py": return new Color32(186, 173, 255, 255);
                case "IH": return new Color32(236, 195, 131, 255);
                case "Audience": return new Color32(114, 224, 176, 255);
                case "Param": return new Color32(245, 170, 131, 255);
                case "Unqualified": return new Color32(238, 146, 182, 255);
                case "Score": return new Color32(236, 168, 201, 255);
            }
            // Stable across imports, tree order and platforms. Unknown sources stay chromatic.
            uint hash = 2166136261;
            foreach (char c in family) hash = unchecked((hash ^ c) * 16777619);
            return Color.HSVToRGB((hash % 3600) / 3600f, .46f + ((hash >> 12) % 15) / 100f, .95f);
        }
    }
}
