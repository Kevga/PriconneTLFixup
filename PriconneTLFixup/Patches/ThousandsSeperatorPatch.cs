using System.Globalization;
using System.Text.RegularExpressions;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

/**
 * This patch adds thousands seperators to number strings for easier readability of large numbers.
 */
[HarmonyPatch(typeof(UILabel), "text", MethodType.Setter)]
[HarmonyWrapSafe]
public class ThousandsSeperatorSoloNumberPatch
{
    private static readonly Regex HpRegex = new(@"^(\d+)/(\d{4,})$", RegexOptions.Compiled);

    private static readonly Regex GradientTextRegex =
        new(@"^(\[[0-9a-fA-F,-]+\])[x+×]?(\d{4,})(\[[0-9a-fA-F,-]+\])$", RegexOptions.Compiled);

    private static readonly CultureInfo Culture = new("en-US");
    
    public static void Prefix(UILabel __instance, ref string value)
    {
        if (!Settings.EnableLargeNumberSeparators.Value || !Util.IsTranslationEnabled())
        {
            return;
        }
        
        if (value == null || __instance == null)
        {
            return;
        }

        try
        {
            var name = __instance.name;
            if (name != null &&
                (name.IndexOf("input", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.IndexOf("condition", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return;
            }
        }
        catch (System.Exception)
        {
            // ignored
        }

        if (long.TryParse(value, out var intVal))
        {
            if (__instance.overflowMethod == UILabel.Overflow.ClampContent)
            {
                __instance.overflowMethod = UILabel.Overflow.ShrinkContent;
                Log.Debug($"Changed overflow method to shrink content for {__instance.name}");
            }

            value = intVal.ToString("#,0", Culture);
            return;
        }

        // Most label text is neither a plain number nor either of these special formats.
        // Avoid entering the regex engine unless the delimiters make a match possible.
        if (value.IndexOf('/') >= 0)
        {
            var match = HpRegex.Match(value);
            if (match.Success)
            {
                var numerator = long.TryParse(match.Groups[1].Value, out var numeratorValue)
                    ? numeratorValue.ToString("#,0", Culture)
                    : match.Groups[1].Value;
                var denominator = long.TryParse(match.Groups[2].Value, out var denominatorValue)
                    ? denominatorValue.ToString("#,0", Culture)
                    : match.Groups[2].Value;
                value = numerator + "/" + denominator;

                return;
            }
        }

        if (value.Length == 0 || value[0] != '[')
        {
            return;
        }

        var gradientMatch = GradientTextRegex.Match(value);
        if (gradientMatch.Success)
        {
            var group = gradientMatch.Groups[2];
            if (long.TryParse(group.Value, out var hp))
            {
                value = value[..group.Index] + hp.ToString("#,0", Culture) + value[(group.Index + group.Length)..];
            }
        }
    }
}

[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
[HarmonyWrapSafe]
public class ThousandsSeperatorPostTranslationPatch
{
    private static readonly Regex NumberRegex = new(@"[1-9]\d{3,}", RegexOptions.Compiled | RegexOptions.Singleline);
    internal static readonly Regex DateRegex = new(@"\d{2,4}[/\.\-]\d{2}[/\.\-]\d{2,4}", RegexOptions.Compiled);
    internal static readonly CultureInfo Culture = new("en-US");

    public static void Prefix(ref string text)
    {
        if (!Settings.EnableLargeNumberSeparators.Value || !Util.IsTranslationEnabled())
        {
            return;
        }
        
        if (text == null)
        {
            return;
        }

        if (text.Contains("Player ID", System.StringComparison.Ordinal) || !ContainsPotentialLargeNumber(text))
        {
            return;
        }

        if (DateRegex.IsMatch(text))
        {
            return;
        }

        text = NumberRegex.Replace(text, static match =>
        {
            if (!long.TryParse(match.Value, out var intVal))
            {
                return match.Value;
            }

            if (!ThousandsSeperatorDictionary32Patch.WasConvertedThisFrame(intVal))
            {
                return match.Value;
            }

            return intVal.ToString("#,0", Culture);
        });
    }

    private static bool ContainsPotentialLargeNumber(string text)
    {
        var consecutiveDigits = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is >= '0' and <= '9')
            {
                if (consecutiveDigits == 0 && c == '0')
                {
                    continue;
                }

                if (++consecutiveDigits >= 4)
                {
                    return true;
                }
            }
            else
            {
                consecutiveDigits = 0;
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(Number), nameof(Number.FormatInt32))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionary32Patch
{
    private static readonly HashSet<long> ConvertedNumbers = new();
    private static int _convertedNumberFrame = -1;
    
    public static void Postfix(ref string __result, int value, Il2CppSystem.ReadOnlySpan<char> format)
    {
        if (!Settings.EnableLargeNumberSeparators.Value || !Util.IsTranslationEnabled())
        {
            return;
        }
        
        if (__result == null || format.Length > 0)
        {
            return;
        }

        RecordConvertedNumber(value);
    }

    internal static bool WasConvertedThisFrame(long value)
    {
        return _convertedNumberFrame == Time.frameCount && ConvertedNumbers.Contains(value);
    }

    internal static void RecordConvertedNumber(long value)
    {
        var currentFrame = Time.frameCount;
        if (_convertedNumberFrame != currentFrame)
        {
            ConvertedNumbers.Clear();
            _convertedNumberFrame = currentFrame;
        }

        ConvertedNumbers.Add(value);
    }
}

[HarmonyPatch(typeof(Number), nameof(Number.FormatInt64))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionary64Patch
{
    public static void Postfix(ref string __result, long value, Il2CppSystem.ReadOnlySpan<char> format)
    {
        if (!Settings.EnableLargeNumberSeparators.Value || !Util.IsTranslationEnabled())
        {
            return;
        }
        
        if (__result == null || format.Length > 0)
        {
            return;
        }

        ThousandsSeperatorDictionary32Patch.RecordConvertedNumber(value);
    }
}

[HarmonyPatch(typeof(CustomUILabel), nameof(CustomUILabel.SetText), typeof(string), typeof(Il2CppReferenceArray<Il2CppSystem.Object>))]
[HarmonyWrapSafe]
public class ThousandsSeperatorDictionaryCustomUILabelPatch
{
    public readonly struct NumberArgumentState
    {
        public NumberArgumentState(long value)
        {
            HasValue = true;
            Value = value;
        }

        public bool HasValue { get; }
        public long Value { get; }
    }

    public static void Prefix(Il2CppReferenceArray<Il2CppSystem.Object> _args, out NumberArgumentState __state)
    {
        __state = default;
        if (!Settings.EnableLargeNumberSeparators.Value || !Util.IsTranslationEnabled())
        {
            return;
        }
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_args == null || _args.Length == 0)
        {
            return;
        }
        
        foreach (var arg in _args)
        {
            if (arg == null)
            {
                continue;
            }
            var objectClass = arg.ObjectClass;
            if (objectClass == Il2CppClassPointerStore<int>.NativeClassPtr)
            {
                __state = new NumberArgumentState(arg.Unbox<int>());
                return;
            } 

            if (objectClass == Il2CppClassPointerStore<long>.NativeClassPtr)
            {
                __state = new NumberArgumentState(arg.Unbox<long>());
                return;
            }
        }
    }

    public static void Postfix(CustomUILabel __instance, NumberArgumentState __state)
    {
        if (!Settings.EnableLargeNumberSeparators.Value || !Util.IsTranslationEnabled())
        {
            return;
        }

        if (!__state.HasValue)
        {
            return;
        }

        var labelText = __instance.text;
        if (labelText == null || ThousandsSeperatorPostTranslationPatch.DateRegex.IsMatch(labelText))
        {
            Log.Debug($"Skipping {__instance.name} because it contains a date");
            return;
        }

        var value = __state.Value;
        var valueString = value.ToString();
        var formattedNumberString = value.ToString("#,0", ThousandsSeperatorPostTranslationPatch.Culture);
        if (formattedNumberString == valueString)
        {
            return;
        }

        __instance.text = labelText.Replace(valueString, formattedNumberString);
        Log.Debug($"Replaced {value} with {formattedNumberString} in {__instance.name} (CustomUILabel.SetText)");
    }
}
