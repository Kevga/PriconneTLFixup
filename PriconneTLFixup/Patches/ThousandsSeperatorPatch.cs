using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

/**
 * This patch adds thousands seperators to number strings for easier readability of large numbers.
 */
[HarmonyPatch(typeof(UILabel), "text", MethodType.Setter)]
[HarmonyWrapSafe]
public class ThousandsSeperatorSoloNumberPatch
{
    private static Regex _hpRegex = new(@"^(\d+)/(\d{4,})$", RegexOptions.Compiled);

    private static Regex _gradientTextRegex =
        new(@"^(\[[0-9a-fA-F,-]+\])[x+×]?(\d{4,})(\[[0-9a-fA-F,-]+\])$", RegexOptions.Compiled);

    private static CultureInfo _culture = new("en-US");
    
    public static void Prefix(UILabel __instance, ref string value)
    {
        if (value == null || __instance == null)
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
        
        if (!long.TryParse(value, out var intVal))
        {
            return;
        }

        if (__instance.overflowMethod == UILabel.Overflow.ClampContent)
        {
            __instance.overflowMethod = UILabel.Overflow.ShrinkContent;
            Log.Debug($"Changed overflow method to shrink content for {__instance.name}");
        }

        var formattedNumber = intVal.ToString("#,0", _culture);
        value = formattedNumber;
    }
}

[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
[HarmonyWrapSafe]
public class ThousandsSeperatorPostTranslationPatch
{
    private static readonly Regex NumberRegex = new(@"\d{4,}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex DateRegex = new(@"\d{2,4}[/\.\-]\d{2}[/\.\-]\d{2,4}", RegexOptions.Compiled);
    internal static readonly CultureInfo Culture = new("en-US");

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

        var matches = NumberRegex.Matches(text);
        foreach (Match match in matches)
        {
            if (!long.TryParse(match.Value, out var intVal))
            {
                continue;
            }

            if (!ThousandsSeperatorDictionary32Patch.ConvertedNumberDictionary.TryGetValue(intVal, out _))
            {
                continue;
            }

            text = text.Replace(match.Value, intVal.ToString("#,0", Culture));
        }
    }
}

[HarmonyPatch(typeof(Number), nameof(Number.FormatInt32))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionary32Patch
{
    internal static bool Initialized;
    internal static readonly Dictionary<long, string> ConvertedNumberDictionary = new();
    
    public static void Postfix(ref string __result, int value, Il2CppSystem.ReadOnlySpan<char> format)
    {
        if (__result == null || format.Length > 0)
        {
            return;
        }

        ConvertedNumberDictionary[value] = __result;

        if (Initialized)
        {
            return;
        }

        CoroutineStarter.Instance.StartCoroutine(ClearConvertedNumberDictionary().WrapToIl2Cpp());
        Initialized = true;
    }

    private static IEnumerator ClearConvertedNumberDictionary()
    {
        //This is intended to run the entire duration of the game
        //We want to clear the dictionaries every frame to only keep values that were converted in the current frame
        while (true)
        {
            ConvertedNumberDictionary.Clear();
            ThousandsSeperatorDictionaryCustomUILabelPatch.NumberDictionary.Clear();
            yield return null;
        }
        // ReSharper disable once IteratorNeverReturns
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

[HarmonyPatch(typeof(CustomUILabel), nameof(CustomUILabel.SetText), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionaryCustomUILabelPatch
{
    internal static readonly Dictionary<CustomUILabel, long> NumberDictionary = new();
    public static void Prefix(CustomUILabel __instance, Il2CppReferenceArray<Il2CppSystem.Object> _args)
    {
        if (_args.Length == 0)
        {
            return;
        }
        
        foreach (var arg in _args)
        {
            if (arg == null)
            {
                continue;
            }
            var cpptype = arg.GetIl2CppType();
            var actualType = System.Type.GetType(cpptype.AssemblyQualifiedName);
            if (actualType == typeof(int))
            {
                var value = arg.Unbox<int>();
                NumberDictionary.TryAdd(__instance, value);
            } 
            else if (actualType == typeof(long))
            {
                var value = arg.Unbox<long>();
                NumberDictionary.TryAdd(__instance, value);
            }
        }
    }

    public static void Postfix(CustomUILabel __instance, Il2CppReferenceArray<Il2CppSystem.Object> _args)
    {
        var exists = NumberDictionary.TryGetValue(__instance, out var value);
        if (!exists)
        {
            return;
        }
        var formattedNumberString = value.ToString("#,0", ThousandsSeperatorPostTranslationPatch.Culture);
        if (formattedNumberString == value.ToString())
        {
            return;
        }
        __instance.text = __instance.text.Replace(value.ToString(), formattedNumberString);
    }
}