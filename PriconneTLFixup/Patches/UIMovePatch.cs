using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(PartsEventQuestAutoProgressInBattleInfo), nameof(PartsEventQuestAutoProgressInBattleInfo.Initialize))]
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