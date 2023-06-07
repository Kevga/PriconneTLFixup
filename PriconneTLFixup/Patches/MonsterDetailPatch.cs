using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
// ReSharper disable InconsistentNaming

namespace PriconneTLFixup.Patches;

/**
 * This set of patches does the following:
 * - Combines the list of strings that were previously displayed separately in their own respective UILabel into one
 * - Enables wrapping of this one label
 * - Adds line breaks before each bullet point
 */

[HarmonyPatch(typeof(PartsDialogMonsterDetail), nameof(PartsDialogMonsterDetail.stringToLineStringList))]
[HarmonyWrapSafe]
public class MonsterDetailPatch
{
    public static bool Prefix(ref Il2CppSystem.Collections.Generic.List<string> __result, string _string)
    {
        __result = new Il2CppSystem.Collections.Generic.List<string>();
        __result.Add(_string);
        return false;
    }
}

[HarmonyPatch(typeof(PartsDialogMonsterDetail), nameof(PartsDialogMonsterDetail.InitializeParam))]
[HarmonyWrapSafe]
public class MonsterDetailScrollContainerPatch
{
    public static void Postfix(PartsDialogMonsterDetail __instance)
    {
        __instance.monsterDetailTextController.curUIScrollContent.curUIScrollView.enabled = true;
    }
}

[HarmonyPatch(typeof(PartsMonsterDetailTextPlate), nameof(PartsMonsterDetailTextPlate.Initialize))]
[HarmonyWrapSafe]
public class MonsterDetailOverflowPatch
{
    public static void Postfix(PartsMonsterDetailTextPlate __instance)
    {
        __instance.detailText.overflowMethod = UILabel.Overflow.ResizeHeight;
        CoroutineStarter.Instance.StartCoroutine(UpdateDetailTextPlate(__instance).WrapToIl2Cpp());
    }
    
    private static IEnumerator UpdateDetailTextPlate(PartsMonsterDetailTextPlate textPlate)
    {
        var text = "";
        while (textPlate.detailText != null)
        {
            if (textPlate.detailText.text != text)
            {
                var updatedText = textPlate.detailText.text;
                updatedText = updatedText.Replace("・", "\n・");
                updatedText = updatedText.Replace("\n\n・", "\n・");
                textPlate.detailText.text = updatedText;
                text = updatedText;
            }

            yield return null;
        }
    }
}