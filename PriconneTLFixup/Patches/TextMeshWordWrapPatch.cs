using System.Collections;
using FLATOUT.Main;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * This adds word-wrapping for TextMeshes, as well as reducing the font size.
 */
[HarmonyPatch(typeof(TextMesh), nameof(TextMesh.text), MethodType.Setter)]
public class TextMeshWordWrapPatch
{
    private static bool _resizeInProgress;
    private const float MaxWidth = 1.7f;

    public static void Postfix(TextMesh __instance, string value)
    {
        if (_resizeInProgress)
        {
            return;
        }
        
        if (value.Length < 30)
        {
            return;
        }
        
        _resizeInProgress = true;

        if (__instance.fontSize == 24)
        {
            __instance.fontSize = 19;
        }
        
        var textSize = new TextSize(__instance);
        if (textSize.Width <= MaxWidth)
        {
            _resizeInProgress = false;
            return;
        }

        
        textSize.FitToWidth(MaxWidth);

        _resizeInProgress = false;
    }
}

[HarmonyPatch(typeof(FlTextParameter), nameof(FlTextParameter._ApplyData))]
[HarmonyWrapSafe]
public class FlTextParameterApplyDataFontSizePatch
{
    public static void Postfix(FlTextParameter __instance)
    {
        if (__instance._fontSize != 24)
        {
            return;
        }   
        __instance._fontSize = 19;
    }
}