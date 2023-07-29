using System.Collections;
using System.Reflection;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;

namespace PriconneTLFixup.Patches;

/**
 * The Grotto button text is much smaller than the surrounding ones. Scale it up to match.
 */
[HarmonyPatch(typeof(ViewQuestTop), nameof(ViewQuestTop.StartView))]
[HarmonyWrapSafe]
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
[HarmonyWrapSafe]
public class SettingsButtonPatch
{
    public static void Postfix(ViewMenuTop __instance)
    {
        __instance.systemButton.GetChildUILabel().lineWidth = 115;
        __instance.cartoonButton.GetChildUILabel().lineWidth = 190;
    }
}

/**
 * Header underline length is determined by assuming a certain character width that works for CJK characters.
 * For the latin alphabet, we instead first set the text and then calculate the actual width of that label.
 */
[HarmonyPatch(typeof(PartsHeaderBackButton), nameof(PartsHeaderBackButton.SetTitleText))]
[HarmonyPriority(Priority.VeryLow)]
[HarmonyWrapSafe]
public class TitleTextPatch
{
    public static void Postfix(PartsHeaderBackButton __instance, string _setTitleText)
    {
        if (_setTitleText == null || _setTitleText.Length == 0)
        {
            return;
        }
        
        __instance.titleLabel.text = "";
        __instance.titleLabel2nd.text = _setTitleText;
        
        Log.Debug($"TitleTextPatch: _setTitleText = {_setTitleText}, text = {__instance.titleLabel2nd.text}, fontSize = {__instance.titleLabel2nd.fontSize}");

        var transform = __instance.titleLabel.transform;
        var pos = transform.localPosition;
        pos.x = __instance.backButton == null ? 22 : 72;
        transform.localPosition = pos;

         var newUnderlineWidth =
             Convert.ToInt16(__instance.titleLabel2nd.text.Length * __instance.titleLabel2nd.fontSize) + 20;
        __instance.underLine.width = newUnderlineWidth;
        var headerController = SingletonMonoBehaviour<HeaderController>.Instance;
        headerController.campaignIcons.SetIconPosition(
            headerController.viewManager.CurrentViewId,
            newUnderlineWidth 
        );
        CoroutineStarter.Instance.StartCoroutine(
            WaitForTranslationCoroutine(__instance.titleLabel2nd, __instance.underLine, __instance.titleLabel2nd.text).WrapToIl2Cpp());
    }

    private static IEnumerator WaitForTranslationCoroutine(UILabel label, UIWidget underline, string originalText)
    {
        Log.Debug($"Before: text = {label.text}, fontSize = {label.fontSize}");
        var yieldInstruction = new Util.WaitForSecondsOrPredicate(0.2f, () => originalText != label.text);
        while (yieldInstruction.keepWaiting)
        {
            yield return null;
        }

        var labelSize = label.mCalculatedSize;
        var newUnderlineWidth = labelSize.x + (label.fontSize == 30 ? 20 : 80);
        Log.Debug($"TitleTextCoroutine: newUnderlineWidth = {newUnderlineWidth}, oldUnderlineWidth = {underline.width}, text = {label.text}, fontSize = {label.fontSize}, labelWidth = {label.lineWidth}");
        underline.width = (int)Math.Round(newUnderlineWidth);
        var headerController = SingletonMonoBehaviour<HeaderController>.Instance;
        headerController.campaignIcons.SetIconPosition(headerController.viewManager.CurrentViewId, newUnderlineWidth);
    }
}

/**
 * "Special" battles have a mode indicator that the HP can overlap with, so we scale the HP text down.
 */
[HarmonyPatch(typeof(PartsBossGauge), nameof(PartsBossGauge.InitGauge))]
[HarmonyWrapSafe]
public class BossGaugePatch
{
    public static void Postfix(PartsBossGauge __instance)
    {
        if (__instance.battleManager.IsSpecialBattle)
        {
            __instance.hpLabel.fontSize = 16;
        }
    }
}

[HarmonyPatch(typeof(PartsDialogShopMemoryPieceDealConfirm),
    nameof(PartsDialogShopMemoryPieceDealConfirm.InitializeParam))]
[HarmonyWrapSafe]
public class MemoryPieceDealConfirmPatch
{
    public static void Postfix(PartsDialogShopMemoryPieceDealConfirm __instance)
    {
        __instance.alertLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
        for (var i = 0; i < __instance.alertObject.transform.childCount; i++)
        {
            var child = __instance.alertObject.transform.GetChild(i);
            if (child.name != "common_icon_alert")
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }
}

[HarmonyPatch]
[HarmonyWrapSafe]
public class UnitIconPatch
{
    static IEnumerable<MethodBase> TargetMethods()
   {
       yield return AccessTools.Method(typeof(UnitIcon), nameof(UnitIcon.updateTable));
   }
    
    public static void Postfix(UnitIcon __instance)
    {
        if (__instance.statusValueLabel == null)
        {
            return;
        }
        __instance.statusNameLabel.fontSize = 18;
        __instance.statusValueLabel.fontSize = 18;

        while (true)
        {
            var nameSize = __instance.statusNameLabel.mCalculatedSize;
            var valueSize = __instance.statusValueLabel.mCalculatedSize;
            var newValueX = nameSize.x;
            if (newValueX + valueSize.x > 122)
            {
                __instance.statusNameLabel.fontSize -= 1;
                __instance.statusValueLabel.fontSize -= 1;
                continue;
            }
            __instance.statusValueLabel.SetLocalPosX(newValueX);
            break;
        }
    }
}