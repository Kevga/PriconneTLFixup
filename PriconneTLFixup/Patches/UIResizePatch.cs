using Elements;
using HarmonyLib;

namespace PriconneTLFixup.Patches;

/**
 * The Grotto button text is much smaller than the surrounding ones. Scale it up to match.
 */
[HarmonyPatch(typeof(ViewQuestTop), nameof(ViewQuestTop.StartView))]
public class GrottoButtonPatch
{
    public static void Postfix(ViewQuestTop __instance)
    {
        __instance.buttonSpecialQuest.Button.GetChildUILabel().lineWidth = 95;
    }
}

/**
 * Same with the settings button.
 */
[HarmonyPatch(typeof(ViewMenuTop), nameof(ViewMenuTop.StartView))]
public class SettingsButtonPatch
{
    public static void Postfix(ViewMenuTop __instance)
    {
        __instance.systemButton.GetChildUILabel().lineWidth = 95;
        __instance.cartoonButton.GetChildUILabel().lineWidth = 190;
    }
}

/**
 * Header underline length is determined by assuming a certain character width that works for CJK characters.
 * For the latin alphabet, character width is smaller, so we scale down the underline length.
 */
[HarmonyPatch(typeof(PartsHeaderBackButton), nameof(PartsHeaderBackButton.SetTitleText))]
public class HeaderUnderlinePatch
{
    public static void Postfix(PartsHeaderBackButton __instance)
    {
        var newUnderlineWidth = Convert.ToInt16(__instance.underLine.width * 0.65f) + 20;
        __instance.underLine.width = newUnderlineWidth;
        var headerController = SingletonMonoBehaviour<HeaderController>.Instance;
        headerController.campaignIcons.SetIconPosition(headerController.viewManager.CurrentViewId, newUnderlineWidth - 20);
    }
}