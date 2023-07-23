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

    public static void Prefix(AutoTranslationPlugin __instance, object ui, ref string text, string originalText)
    {
        if (originalText == null)
        {
            return;
        }

        var preTLColorMatches = PostTranslationColorCodeRegex.Matches(originalText);
        var distinctPreTLColorMatches = preTLColorMatches.OfType<Match>().GroupBy(x => x.Value).Select(x =>x.First()).ToList();
        var postTLColorMatches = PostTranslationColorCodeRegex.Matches(text);
        
        foreach (var preTLColorMatch in distinctPreTLColorMatches)
        {
            Levenshtein Levenshtein = new(preTLColorMatch.Value);
            foreach (Match postTLColorMatch in postTLColorMatches)
            {
                if (postTLColorMatch.Value == preTLColorMatch.Value)
                {
                    continue;
                }
                
                if (Levenshtein.DistanceFrom(postTLColorMatch.Value) <= COLOR_DISTANCE_THRESHOLD )
                {
                    Log.Debug("Replacing color due to Levenshtein match: " + postTLColorMatch.Value + " -> " + preTLColorMatch.Value);
                    text = text.Replace(postTLColorMatch.Value, preTLColorMatch.Value);
                }
            }
        }

        var colorGradientMatchesPre = ColorGradientRegex.Matches(originalText);
        var colorGradientMatchsPost = ColorGradientRegex.Matches(text);
        var distinctPreTLGradientMatches = colorGradientMatchesPre.OfType<Match>().GroupBy(x => x.Value).Select(x =>x.First()).ToList();
        
        foreach (var preTLGradientMatch in distinctPreTLGradientMatches)
        {
            Levenshtein Levenshtein = new(preTLGradientMatch.Value);
            foreach (Match postTLGradientMatch in colorGradientMatchsPost)
            {
                if (postTLGradientMatch.Value == preTLGradientMatch.Value)
                {
                    continue;
                }
                
                if (Levenshtein.DistanceFrom(postTLGradientMatch.Value) <= GRADIENT_DISTANCE_THRESHOLD)
                {
                    Log.Debug("Replacing gradient due to Levenshtein match: " + postTLGradientMatch.Value + " -> " + preTLGradientMatch.Value);
                    text = text.Replace(postTLGradientMatch.Value, preTLGradientMatch.Value);
                }
                
                if (preTLGradientMatch.Value == postTLGradientMatch.Value.Replace(" ", ""))
                {
                    Log.Debug("Removing spaces from gradient match: " + postTLGradientMatch.Value + " -> " + postTLGradientMatch.Value.Replace(" ", ""));
                    text = text.Replace(postTLGradientMatch.Value, postTLGradientMatch.Value.Replace(" ", ""));
                }
            }
        }

        text = text.Replace("[- ]", "[-]");
        text = text.Replace("[--]", "[-]");
        text = text.Replace("⁇", "");
        text = text.Replace("unk>", "");
        text = text.Trim();
    }
}