using System.Text.RegularExpressions;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using Fastenshtein;

// ReSharper disable InconsistentNaming

namespace PriconneTLFixup.Patches;

/**
 * Color code fixes. They frequently get mangled by the MTL. We try to restore them.
 */
[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
[HarmonyWrapSafe]
public static class TranslationPreprocessorPatch
{
    internal static readonly Regex PostTranslationColorCodeRegex = new(@"[\[\(]([0-9A-Fa-fsS]{6,10})[\]\)]", RegexOptions.Compiled);
    private static readonly Regex ColorGradientRegex = new(@"[\[\(]([0-9A-Fa-f,sS\s]{13,20})[\]\)]", RegexOptions.Compiled);
    private const int COLOR_DISTANCE_THRESHOLD = 3;
    private const int GRADIENT_DISTANCE_THRESHOLD = 5;

    public static void Prefix(ref string text, string originalText)
    {
        if (originalText == null || text == null)
        {
            return;
        }

        if (MayContainColorCode(originalText) && MayContainColorCode(text))
        {
            RepairColorCodes(originalText, ref text);
        }

        if (text.Contains("[- ]", StringComparison.Ordinal))
        {
            text = text.Replace("[- ]", "[-]");
        }

        if (text.Contains("[--]", StringComparison.Ordinal)) text = text.Replace("[--]", "[-]");
        if (text.Contains('⁇')) text = text.Replace("⁇", "");
        if (text.Contains("unk>", StringComparison.Ordinal)) text = text.Replace("unk>", "");
        text = text.Trim();
    }

    private static bool MayContainColorCode(string value)
    {
        return value.IndexOf('[') >= 0 || value.IndexOf('(') >= 0;
    }

    private static void RepairColorCodes(string originalText, ref string text)
    {
        var postTLColorMatches = PostTranslationColorCodeRegex.Matches(text);
        var seenColorCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match preTLColorMatch in PostTranslationColorCodeRegex.Matches(originalText))
        {
            if (!seenColorCodes.Add(preTLColorMatch.Value))
            {
                continue;
            }

            var levenshtein = new Levenshtein(preTLColorMatch.Value);
            foreach (Match postTLColorMatch in postTLColorMatches)
            {
                if (postTLColorMatch.Value == preTLColorMatch.Value)
                {
                    continue;
                }

                if (levenshtein.DistanceFrom(postTLColorMatch.Value) <= COLOR_DISTANCE_THRESHOLD)
                {
                    Log.Debug("Replacing color due to Levenshtein match: " + postTLColorMatch.Value + " -> " + preTLColorMatch.Value);
                    text = text.Replace(postTLColorMatch.Value, preTLColorMatch.Value);
                }
            }
        }

        var postTLGradientMatches = ColorGradientRegex.Matches(text);
        var seenGradients = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match preTLGradientMatch in ColorGradientRegex.Matches(originalText))
        {
            if (!seenGradients.Add(preTLGradientMatch.Value))
            {
                continue;
            }

            var levenshtein = new Levenshtein(preTLGradientMatch.Value);
            foreach (Match postTLGradientMatch in postTLGradientMatches)
            {
                if (postTLGradientMatch.Value == preTLGradientMatch.Value)
                {
                    continue;
                }

                if (levenshtein.DistanceFrom(postTLGradientMatch.Value) <= GRADIENT_DISTANCE_THRESHOLD)
                {
                    Log.Debug("Replacing gradient due to Levenshtein match: " + postTLGradientMatch.Value + " -> " + preTLGradientMatch.Value);
                    text = text.Replace(postTLGradientMatch.Value, preTLGradientMatch.Value);
                }

                var postWithoutSpaces = postTLGradientMatch.Value.Replace(" ", "");
                if (preTLGradientMatch.Value == postWithoutSpaces)
                {
                    Log.Debug("Removing spaces from gradient match: " + postTLGradientMatch.Value + " -> " + postWithoutSpaces);
                    text = text.Replace(postTLGradientMatch.Value, postWithoutSpaces);
                }
            }
        }
    }
}
