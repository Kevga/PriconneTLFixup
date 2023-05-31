using System.Text.RegularExpressions;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
// ReSharper disable InconsistentNaming

namespace PriconneTLFixup.Patches;

/**
 * Color code fix. They may be getting removed by automated translation, this patch adds them back.
 */
[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
[HarmonyWrapSafe]
public static class TranslationPreprocessorPatch
{
    private static readonly Regex ColorCodeRegex = new Regex(@"\[([0-9A-F]{8})\]", RegexOptions.Compiled);
    public static void Prefix(AutoTranslationPlugin __instance, object ui, ref string text, string originalText)
    {
        var colorCodeMatch = ColorCodeRegex.Matches(originalText);
        if (colorCodeMatch.Count > 0)
        {
            text = colorCodeMatch[^1].Value + text;
        }
    }
}