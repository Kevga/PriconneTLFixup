using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
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

[HarmonyPatch(typeof(PartsDialogAbyssBossResult), nameof(PartsDialogAbyssBossResult.StartShow))]
[HarmonyWrapSafe]
public class UIMovePatch3
{
    public static void Postfix(PartsDialogAbyssBossResult __instance)
    {
        if (__instance == null)
        {
            return;
        }

        var go = GameObject.Find("_Game(Clone)/UI Root/FrontArea/IgnoreBlurLayer/DialogArea/DialogCommonBig(Clone)/Main/PartsParent/PartsDialogAbyssBossResult(Clone)/Main/GUIGroup_gauge/ToNextRewardLabel");
        if (go == null)
        {
            return;
        }
        
        var pos = go.transform.localPosition;
        pos.x = -192.5f;
        go.transform.localPosition = pos;
    }
}

[HarmonyPatch(typeof(ViewAlcesTop), nameof(ViewAlcesTop.StartView))]
[HarmonyWrapSafe]
public class UIMovePatch4
{
    public static void Postfix(ViewAlcesTop __instance)
    {
        if (__instance == null)
        {
            return;
        }
        
        Log.Debug("Alces Top move patch");
        
        CoroutineStarter.Instance.StartCoroutine(WaitForAvailability().WrapToIl2Cpp());
    }
    
    public static IEnumerator WaitForAvailability()
    {
        var timeout = 2.0f;
        var now = Time.realtimeSinceStartup;
        var goldPath = "_Game(Clone)/UI Root/ViewsArea/View/ViewAlcesTop(Clone)/Right/TopRightAnchor/GUIGroup_Gold";
        var pointsPath = "_Game(Clone)/UI Root/ViewsArea/View/ViewAlcesTop(Clone)/Right/TopRightAnchor/GUIGroup_AlcesPt";
        while (GameObject.Find(goldPath) == null && Time.realtimeSinceStartup - now < timeout)
        {
            yield return null;
        }
        
        if (GameObject.Find(goldPath) == null)
        {
            Log.Debug("UIMovePatch4: Timeout");
            yield break;
        }
        
        Log.Debug("Found Alces Gold display after " + (Time.realtimeSinceStartup - now) + " seconds");
        
        var goldDisplay = GameObject.Find(goldPath);
        if (goldDisplay == null)
        {
            Log.Debug("Alces Gold display not found");
            yield break;
        }
        
        var goldPos = goldDisplay.transform.localPosition;
        goldPos.y += 15;
        goldDisplay.transform.localPosition = goldPos;
        
        var pointsDisplay = GameObject.Find(pointsPath);
        if (pointsDisplay == null)
        {
            Log.Debug("Alces Points display not found");
            yield break;
        }
        
        var alcesPos = pointsDisplay.transform.localPosition;
        alcesPos.x = goldPos.x;
        alcesPos.y = goldPos.y - 40;
        pointsDisplay.transform.localPosition = alcesPos;
    }
}