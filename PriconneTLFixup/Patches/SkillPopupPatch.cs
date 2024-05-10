using Cute;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(DialogManager), nameof(DialogManager.OpenDialogPopUpSkill))]
[HarmonyWrapSafe]
public class SkillPopupPatch
{
    public static void Postfix(DialogManager __instance)
    {
        Log.Debug("DialogManager.OpenDialogPopUpSkill");
        var label = GameObject.Find("DialogArea/DialogPopUp(Clone)/Main/Table/SubPop/subDescriptionLabel");
        if (label == null)
        {
            Log.Debug("DialogPopUp subDescriptionLabel not found");
            return;
        }

        var uiLabel = label.GetComponent<CustomUILabel>();
        if (uiLabel == null)
        {
            Log.Debug("DialogPopUp subDescriptionLabel CustomUILabel not found");
            return;
        }

        uiLabel.lineWidth = 470;
        label.transform.localPosition = new Vector3(15f, label.transform.localPosition.y, label.transform.localPosition.z);
    }
}