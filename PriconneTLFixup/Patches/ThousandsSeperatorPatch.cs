using System.Globalization;
using System.Text.RegularExpressions;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

/**
 * This patch adds thousands seperators to number strings for easier readability of large numbers.
 */
[HarmonyPatch(typeof(UILabel), "text", MethodType.Setter)]
public class ThousandsSeperatorPatch
{
    private static CultureInfo _culture = new("en-US");
    
    public static void Prefix(ref string value)
    {
        if (value == null)
        {
            return;
        }
        
        var x = value.StartsWith("x");
        if (x)
        {
            value = value.Substring(1);
        }
        
        var times = value.StartsWith('×');
        if (times)
        {
            value = value.Substring(1);
        }
        
        if (!int.TryParse(value, out var intVal))
        {
            return;
        }
        
        string formattedNumber = intVal.ToString("#,0", _culture);
        
        if (x)
        {
            formattedNumber = "x" + formattedNumber;
        }
        if (times)
        {
            formattedNumber = "×" + formattedNumber;
        }
        value = formattedNumber;
    }
}

/**
 * This patch adds thousands seperators to numbers that are contained in translated strings.
 * It's important to do this after translation, since otherwise regex matches for translations will fail.
 */
[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
public class ThousandsSeperatorPostTranslationPatch
{
    private static Regex _numberRegex = new(@"\d{4,}", RegexOptions.Compiled);
    private static Regex _dateRegex = new(@"\d{2,4}[/\.\-]\d{2}[/\.\-]\d{2,4}", RegexOptions.Compiled);
    private static CultureInfo _culture = new("en-US");
    
    public static void Prefix(ref string text)
    {
        if (text == null)
        {
            return;
        }
        
        if (_dateRegex.IsMatch(text))
        {
            return;
        }
        
        var matches = _numberRegex.Matches(text);
        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Value, out var intVal))
            {
                continue;
            }
            
            string formattedNumber = intVal.ToString("#,0", _culture);
            text = text.Replace(match.Value, formattedNumber);
        }
    }
}