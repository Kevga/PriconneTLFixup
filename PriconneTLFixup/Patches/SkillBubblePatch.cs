using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;

// ReSharper disable InconsistentNaming
namespace PriconneTLFixup.Patches;

/**
 * This patch fixes the issue that skill bubbles never disappear after they have been translated.
 * The cause for this is that the base game checks if the label text matches the skill name before ticking down the timer,
 * and only the label is being translated, causing a mismatch.
 * Additionally, in cases where auto-translation generates very long names, we truncate them.
 * Bubble width is scaled down to match the lower character width for latin characters.
 */
[HarmonyPatch(typeof(LifeGaugeController), nameof(LifeGaugeController.IndicateSkillName))]
public class SkillBubblePatch
{
    private static readonly int maxSkillNameLength = 40;

    private static IEnumerator updateSkillBalloon(LifeGaugeController controller, string _skillName)
    {
        float timer = 1.5f;
        while (!_skillName.IsNullOrEmpty())
        {
            var translatedName = controller.skillNameLabel.text;
            if (translatedName.Length > (maxSkillNameLength + 3))
            {
                translatedName = translatedName.Substring(0, maxSkillNameLength)+"...";
            }
            controller.skillNameBalloon.width = GetBubbleWidth(translatedName);
            controller.skillNameLabel.text = translatedName;
            
            timer -= controller.battleManager.DeltaTime;
            if (timer < 0f || (controller.owner.IsFront && controller.battleManager.BlackOutUnitList.Count != 0))
            {
                controller.skillNameBalloon.gameObject.SetActive(false);
                if (!controller.isMoving && !controller.skillBalloonVisible && controller.iconCount == 0)
                {
                    controller.gameObject.SetActive(false);
                }

                yield break;
            }

            yield return null;
        }
    }

    public static bool Prefix(LifeGaugeController __instance, string _skillName)
    {
        if (string.IsNullOrEmpty(_skillName))
        {
            return false;
        }

        if (__instance.owner.IsFront && __instance.battleManager.BlackOutUnitList.Count != 0)
        {
            return false;
        }
        
        if (!__instance.gameObject.activeSelf)
        {
            __instance.gameObject.SetActive(true);
        }

        __instance.skillNameLabel.SetText(eTextId.NUM, _skillName);
        __instance.skillBalloonVisible = true; 
        __instance.skillNameBalloon.gameObject.SetActive(true);
        __instance.skillNameBalloon.width = GetBubbleWidth(_skillName);

        __instance.battleManager.AppendCoroutine(updateSkillBalloon(__instance, _skillName).WrapToIl2Cpp(), ePauseType.IGNORE_BLACK_OUT, null);
        return false;
    }
    
    private static int GetBubbleWidth(string text)
    {
        return text.Length * 14 + 48;
    }
}