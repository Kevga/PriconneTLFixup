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
    private static Regex _hpRegex = new(@"^(\d+)/(\d{4,})$", RegexOptions.Compiled);

    private static Regex _gradientTextRegex =
        new(@"^(\[[0-9a-fA-F,-]+\])(\d{4,})(\[[0-9a-fA-F,-]+\])$", RegexOptions.Compiled);

    private static CultureInfo _culture = new("en-US");
    
    public static void Prefix(ref string value)
    {
        if (value == null)
        {
            return;
        }

        var match = _hpRegex.Match(value);
        if (match.Success)
        {
            foreach (Group group in match.Groups)
            {
                if (!int.TryParse(group.Value, out var hp))
                {
                    continue;
                }

                value = value.Replace(group.Value, hp.ToString("#,0", _culture));
            }

            return;
        }

        var gradientMatch = _gradientTextRegex.Match(value);
        if (gradientMatch.Success)
        {
            var group = gradientMatch.Groups[2];
            if (int.TryParse(group.Value, out var hp))
            {
                value = value.Replace(group.Value, hp.ToString("#,0", _culture));
            }
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