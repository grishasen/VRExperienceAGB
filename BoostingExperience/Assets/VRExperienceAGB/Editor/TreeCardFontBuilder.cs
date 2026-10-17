using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace VRExperienceAGB.Editor
{
    /// <summary>Creates project-owned Inter assets without changing the shared TMP defaults.</summary>
    public static class TreeCardFontBuilder
    {
        private const string Destination = "Assets/VRExperienceAGB/Resources/TreeCardFonts";

        [MenuItem("VRExperienceAGB/Build tree card fonts")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building fonts.");
            Directory.CreateDirectory(Destination);
            AssetDatabase.Refresh();
            var fallback = Create("Inter-Medium", "Inter-Fallback", false);
            foreach (string name in new[] { "Inter-Medium", "Inter-SemiBold" })
            {
                var font = Create(name, name, true);
                font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };
                TrimUnusedGlyphFeatures(font);
                EditorUtility.SetDirty(font);
            }
            AssetDatabase.SaveAssets();
        }

        private static void TrimUnusedGlyphFeatures(TMP_FontAsset font)
        {
            var glyphs = new HashSet<uint>(font.glyphTable.Select(g => g.index));
            // Unity 6 exposes these lists through TMP but keeps their TextCore record types internal.
            // Keep kerning for baked glyphs without serializing the entire source font's feature tables.
            foreach (var property in font.fontFeatureTable.GetType().GetProperties())
            {
                if (!(property.GetValue(font.fontFeatureTable) is IList records)) continue;
                string[] paths;
                switch (property.Name)
                {
                    case "multipleSubstitutionRecords": paths = new[] { "targetGlyphID", "substituteGlyphIDs" }; break;
                    case "ligatureRecords": paths = new[] { "componentGlyphIDs", "ligatureGlyphID" }; break;
                    case "glyphPairAdjustmentRecords": paths = new[] { "firstAdjustmentRecord.glyphIndex", "secondAdjustmentRecord.glyphIndex" }; break;
                    case "MarkToBaseAdjustmentRecords": paths = new[] { "baseGlyphID", "markGlyphID" }; break;
                    case "MarkToMarkAdjustmentRecords": paths = new[] { "baseMarkGlyphID", "combiningMarkGlyphID" }; break;
                    default: continue;
                }
                for (int i = records.Count - 1; i >= 0; i--)
                    if (paths.Any(path => !ContainsBakedGlyphs(records[i], path, glyphs))) records.RemoveAt(i);
            }
            font.ReadFontAssetDefinition();
        }

        private static bool ContainsBakedGlyphs(object record, string path, HashSet<uint> glyphs)
        {
            object value = record;
            foreach (string part in path.Split('.')) value = value.GetType().GetProperty(part).GetValue(value);
            return value is uint[] indices ? indices.All(glyphs.Contains) : glyphs.Contains((uint)value);
        }

        private static TMP_FontAsset Create(string source, string name, bool bake)
        {
            string path = Destination + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) {
                if (!bake) {
                    var settings = new SerializedObject(existing);
                    settings.FindProperty("m_AtlasRenderMode").intValue = (int)GlyphRenderMode.SDFAA;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                return existing;
            }
            var ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/VRExperienceAGB/Fonts/Inter/" + source + ".ttf");
            if (ttf == null) throw new InvalidOperationException("Missing official Inter font: " + source);
            // Keep comparable padding ratios. The dynamic fallback uses fast rasterization for new identifiers.
            var font = TMP_FontAsset.CreateFontAsset(ttf, bake ? 80 : 86, bake ? 8 : 9,
                bake ? GlyphRenderMode.SDF16 : GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            font.name = name;
            if (bake)
            {
                var characters = Enumerable.Range(32, 95).Concat(Enumerable.Range(160, 96))
                    .Concat(new[] { 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2026, 0x2190, 0x2192, 0x2212, 0x2264, 0x2265 })
                    .Select(c => (uint)c).ToArray();
                if (!font.TryAddCharacters(characters, out uint[] missing, true))
                    throw new InvalidOperationException("Inter atlas did not fit all required glyphs: " + string.Join(",", missing));
                font.atlasPopulationMode = AtlasPopulationMode.Static;
            }
            else
            {
                var serialized = new SerializedObject(font);
                serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            // Use the mobile SDF shader, without glow, outline, synthetic weight or sharpened edges.
            font.material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            font.material.SetFloat("_FaceDilate", 0);
            font.material.SetFloat("_OutlineWidth", 0);
            font.material.SetFloat("_OutlineSoftness", 0);
            font.material.SetFloat("_WeightNormal", 0);
            AssetDatabase.CreateAsset(font, path);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            EditorUtility.SetDirty(font);
            return font;
        }
    }
}
