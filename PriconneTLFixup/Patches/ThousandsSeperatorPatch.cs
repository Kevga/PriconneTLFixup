using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Il2CppSystem;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

/**
 * This patch adds thousands seperators to number strings for easier readability of large numbers.
 */
[HarmonyPatch(typeof(UILabel), "text", MethodType.Setter)]
public class ThousandsSeperatorSoloNumberPatch
{
    private static Regex _hpRegex = new(@"^(\d+)/(\d{4,})$", RegexOptions.Compiled);

    private static Regex _gradientTextRegex =
        new(@"^(\[[0-9a-fA-F,-]+\])[x+×]?(\d{4,})(\[[0-9a-fA-F,-]+\])$", RegexOptions.Compiled);

    private static CultureInfo _culture = new("en-US");
    
    public static void Prefix(UILabel __instance, ref string value)
    {
        if (value == null)
        {
            return;
        }

        try
        {
            var name = __instance.name;
            if (name != null && (name.ToLower().Contains("input") || name.ToLower().Contains("condition")))
            {
                return;
            }
        }
        catch (System.Exception)
        {
            // ignored
        }

        var match = _hpRegex.Match(value);
        if (match.Success)
        {
            foreach (Group group in match.Groups)
            {
                if (!long.TryParse(group.Value, out var hp))
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
            if (long.TryParse(group.Value, out var hp))
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
        
        var plus = value.StartsWith('+');
        if (plus)
        {
            value = value.Substring(1);
        }
        
        if (!long.TryParse(value, out var intVal))
        {
            return;
        }
        
        if (__instance.overflowMethod == UILabel.Overflow.ClampContent)
        {
            __instance.overflowMethod = UILabel.Overflow.ShrinkContent;
            Plugin.Logger.LogDebug($"Changed overflow method to shrink content for {__instance.name}");
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
        if (plus)
        {
            formattedNumber = "+" + formattedNumber;
        }
        value = formattedNumber;
    }
}

[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
public class ThousandsSeperatorPostTranslationPatch
{
    private static readonly Regex NumberRegex = new(@"\d{4,}", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new(@"\d{2,4}[/\.\-]\d{2}[/\.\-]\d{2,4}", RegexOptions.Compiled);
    private static readonly CultureInfo Culture = new("en-US");
    
    public static void Prefix(ref string text)
    {
        if (text == null)
        {
            return;
        }

        if (text.Contains("Player ID"))
        {
            return;
        }

        if (DateRegex.IsMatch(text))
        {
            return;
        }
        
        //Find all matches in string
        var matches = NumberRegex.Matches(text);
        
        //For each match, check if number is in ConvertedNumberDictionary. If it is, convert it to the formatted string.
        foreach (Match match in matches)
        {
            if (!long.TryParse(match.Value, out var intVal))
            {
                continue;
            }

            if (ThousandsSeperatorDictionary32Patch.ConvertedNumberDictionary.TryGetValue(intVal, out var value1))
            {
                text = text.Replace(match.Value, intVal.ToString("#,0", Culture));
                Plugin.Logger.LogDebug($"Converted {match.Value} to {intVal.ToString("#,0", Culture)}");
                ThousandsSeperatorDictionary32Patch.ConvertedNumberDictionary.Remove(intVal);
            }
            else
            {
                Plugin.Logger.LogDebug($"Could not find {match.Value} in ConvertedNumberDictionary");
            }
        }
    }
}

[HarmonyPatch(typeof(Number), nameof(Number.FormatInt32))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionary32Patch
{
    internal static bool _initialized;
    internal static Dictionary<long, string> ConvertedNumberDictionary = new();
    
    public static void Postfix(ref string __result, int value, Il2CppSystem.ReadOnlySpan<char> format)
    {
        if (__result == null || format.Length > 0)
        {
            return;
        }

        ConvertedNumberDictionary[value] = __result;
        
        if (!_initialized)
        {
            CoroutineStarter.Instance.StartCoroutine(ClearConvertedNumberDictionary().WrapToIl2Cpp());
            _initialized = true;
        }
    }
    
    private static IEnumerator ClearConvertedNumberDictionary()
    {
        Plugin.Logger.LogDebug("Initialized ConvertedNumberDictionary");
        while (true)
        {
            ConvertedNumberDictionary.Clear();
            yield return null;
        }
    }
}

[HarmonyPatch(typeof(Number), nameof(Number.FormatInt64))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionary64Patch
{
    public static void Postfix(ref string __result, long value, Il2CppSystem.ReadOnlySpan<char> format)
    {
        if (__result == null || format.Length > 0)
        {
            return;
        }

        ThousandsSeperatorDictionary32Patch.ConvertedNumberDictionary[value] = __result;
    }
}

[HarmonyPatch(typeof(Il2CppSystem.Object), nameof(Il2CppSystem.Object.ToString))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionaryObjectPatch
{
    public static void Postfix(object __instance, string __result)
    {
        Plugin.Logger.LogDebug($"Object: {__instance.GetType()}: {__result}");
    }
}