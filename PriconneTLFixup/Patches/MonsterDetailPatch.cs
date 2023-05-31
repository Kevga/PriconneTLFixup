using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
// ReSharper disable InconsistentNaming

namespace PriconneTLFixup.Patches;

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
        
        //TODO: This coroutine unfortunately only runs once. It needs to run the entire time the dialog is open.
        //The issue does not seem to lie in the coroutine itself.
        __instance.StartCoroutine(UpdateDetailTextPlate(__instance).WrapToIl2Cpp());
    }
    
    private static IEnumerator UpdateDetailTextPlate(PartsMonsterDetailTextPlate textPlate)
    {
        var text = "";
        while (textPlate.detailText.isActiveAndEnabled)
        {
            if (textPlate.detailText.text != text)
            {
                var updatedText = textPlate.detailText.text;
                updatedText = updatedText.Replace("・", "\n・");
                textPlate.detailText.text = updatedText;
                text = updatedText;
            }

            yield return null;
        }
    }
}