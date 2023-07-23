using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(BattleHeaderController), nameof(BattleHeaderController.Init))]
public class UIMovePatch
{
    public static void Postfix(BattleHeaderController __instance)
    {
        if (__instance == null || __instance.battleManager == null)
        {
            return;
        }

        if (!__instance.battleManager.IsQuestAutoProgressing())
        {
            return;
        }
        
        var go = GameObject.Find("_Game(Clone)/UI Root/ViewsArea/View/ViewBattle(Clone)/Header/AnchorTopLeft/AutoProgressInfoParent/PartsEventQuestAutoProgressInBattleInfo(Clone)/GUIGroup_QuestAuto/questauto_text_auro/");
        if (go == null)
        {
            return;
        }
        
        var pos = go.transform.localPosition;
        pos.x = -25;
        go.transform.localPosition = pos;
    }
}