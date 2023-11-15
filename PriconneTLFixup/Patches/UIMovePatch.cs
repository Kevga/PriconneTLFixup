using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(PartsEventQuestAutoProgressInBattleInfo), nameof(PartsEventQuestAutoProgressInBattleInfo.Initialize))]
[HarmonyWrapSafe]
public class UIMovePatch
{
    public static void Postfix(PartsEventQuestAutoProgressInBattleInfo __instance)
    {
        if (__instance == null)
        {
            return;
        }

        var go = GameObject.Find("questauto_text_auro");
        if (go == null)
        {
            go = GameObject.Find("questauto_text_auto"); //In case they ever fix the typo lmao
        }
        if (go == null)
        {
            return;
        }
        
        var pos = go.transform.localPosition;
        pos.x = -25;
        go.transform.localPosition = pos;
    }
}

//PartsDialogUserProfile.InitializeParam
[HarmonyPatch(typeof(PartsDialogUserProfile), nameof(PartsDialogUserProfile.InitializeParam))]
[HarmonyWrapSafe]
public class UIMovePatch2
{
    public static void Postfix(PartsDialogUserProfile __instance)
    {
        if (__instance == null)
        {
            return;
        }

        //Tower label
        var go = GameObject.Find("PartsDialogUserProfile(Clone)/ProfileProgressGroup/ScrollView/PartsProfileProgress/GUIGroup_ContentsTower/GUIGroup_title_tower/Label");
        if (go == null)
        {
            return;
        }
        
        var pos = go.transform.localPosition;
        pos.x = 142;
        go.transform.localPosition = pos;
        
        //Unlocked label
        go = GameObject.Find("ProfileProgressGroup/ScrollView/PartsProfileProgress/GUIGroup_ContentsRelease/GUIGroup_title_Release/Label");
        if (go == null)
        {
            return;
        }

        var label = go.GetComponent<CustomUILabel>();
        if (label == null)
        {
            return;
        }

        label.lineWidth = 120;
        pos = go.transform.localPosition;
        pos.x = 136;
        go.transform.localPosition = pos;
    }
}