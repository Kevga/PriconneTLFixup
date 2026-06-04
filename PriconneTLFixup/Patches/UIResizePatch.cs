using System.Collections;
using System.Text.RegularExpressions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

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

[HarmonyPatch(typeof(StoryScene), nameof(StoryScene.ViewPlaceName))]
[HarmonyPriority(Priority.VeryLow)]
[HarmonyWrapSafe]
public class StoryPlaceNamePatch2
{
    public static bool Prefix(StoryScene __instance, string _placeName)
    {
        var storyIcon = __instance.placeIcon;
        storyIcon.placeObjAnimation.enabled = false;
        storyIcon.placeHeaderLabel.gameObject.SetActive(false);
        storyIcon.placeSubLabel.SetText(_placeName);
        var transform = storyIcon.placeSubLabel.transform;
        var pos = transform.localPosition;
        pos.x = 40;
        transform.localPosition = pos;
        storyIcon.lineSprite.width = 83 + storyIcon.placeSubLabel.width;
        storyIcon.StartAnimation();
        return false;
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
    public static bool Prefix(PartsHeaderBackButton __instance, string _setTitleText)
    {
        if (_setTitleText.IsNullOrEmpty())
        {
            __instance.subTitleLabel.SetActiveWithCheck(false);
            return false;
        }

        var activeSelf = __instance.gameObject.activeSelf;
        __instance.gameObject.SetActive(false);
        __instance.gameObject.SetActive(true);
        __instance.titleLabel.SetText("");
        __instance.titleLabel2nd.SetText(_setTitleText.Replace("\n", " ").Replace("  ", " "));
        __instance.titleLabel2nd.SetText(__instance.titleLabel2nd.text.Replace("\n", " ").Replace("  ", " "));
        __instance.titleLabel.SetActive(false);
        __instance.titleLabel2nd.SetActive(false);
        __instance.titleLabel.SetActive(true);
        __instance.titleLabel2nd.SetActive(true);
        __instance.gameObject.SetActive(activeSelf);
        var matchCollection = Regex.Matches(__instance.titleLabel2nd.text, "[a-zA-Z0-9]");
        var num = __instance.titleLabel2nd.text.Length - matchCollection.Count;
        var fontSize = __instance.titleLabel2nd.fontSize;
        var num2 = Mathf.CeilToInt(fontSize * 0.75f);
        var num3 = __instance.titleLabel.text.Length * __instance.titleLabel.fontSize + num * fontSize +
                   matchCollection.Count * num2;
        __instance.underLine.width = __instance.leftOffset + num3 + __instance.rightOffset;
        __instance.underLine.gameObject.SetActive(true);
        __instance.subTitleLabel.SetActiveWithCheck(false);

        Log.Debug(
            $"TitleTextPatch: _setTitleText = {_setTitleText}, text = {__instance.titleLabel2nd.text}, fontSize = {__instance.titleLabel2nd.fontSize}");

        var transform = __instance.titleLabel.transform;
        var pos = transform.localPosition;
        pos.x = __instance.backButton == null ? 22 : 72;
        var offset = __instance.backButton == null ? 50 : 0;
        transform.localPosition = pos;

        if (!__instance.titleLabel2nd.text.IsJapanese())
        {
            __instance.titleLabel2nd.ProcessText();
            var labelSize = __instance.titleLabel2nd.mCalculatedSize;
            var newUnderlineWidth = labelSize.x + 20 + offset;
            __instance.underLine.width = (int)Math.Round(newUnderlineWidth);
            var headerController = SingletonMonoBehaviour<HeaderController>.Instance;

            headerController.campaignIcons.SetIconPosition(
                headerController.viewManager.CurrentViewId,
                newUnderlineWidth
            );
        }
        else
        {
            Log.Debug("TitleTextPatch: Japanese text");
        }

        CoroutineStarter.Instance.StartCoroutine(
            WaitForTranslationCoroutine(__instance.titleLabel2nd, __instance.underLine, _setTitleText, offset).WrapToIl2Cpp());

        return false;
    }

    public static IEnumerator WaitForTranslationCoroutine(UILabel label, UIWidget underline, string originalText, int offset)
    {
        Log.Debug($"Before: text = {label.text}, fontSize = {label.fontSize}");
        var yieldInstruction = new Util.WaitForSecondsOrPredicate(5.0f, () => originalText != label.text);
        while (yieldInstruction.keepWaiting)
        {
            yield return null;
        }

        if (!label.text.IsJapanese())
        {
            label.ProcessText();
            var labelSize = label.mCalculatedSize;
            var newUnderlineWidth = labelSize.x + 20 + offset;
            Log.Debug(
                $"TitleTextCoroutine: newUnderlineWidth = {newUnderlineWidth}, oldUnderlineWidth = {underline.width}, text = {label.text}, fontSize = {label.fontSize}, labelWidth = {labelSize.x}");
            underline.width = (int)Math.Round(newUnderlineWidth);
            var headerController = SingletonMonoBehaviour<HeaderController>.Instance;
            headerController.campaignIcons.SetIconPosition(headerController.viewManager.CurrentViewId,
                newUnderlineWidth);
        }
        else
        {
            Log.Debug("TitleTextCoroutine: Japanese text");
        }
    }
}

[HarmonyPatch(typeof(PartsHeaderBackButton), nameof(PartsHeaderBackButton.SetSubTitleText))]
[HarmonyWrapSafe]
public class SubTitleTextPatch
{
    public static void Postfix(PartsHeaderBackButton __instance, string _setSubTitleText)
    {
        if (__instance.subTitleLabel == null || _setSubTitleText.IsNullOrEmpty())
        {
            return;
        }
        
        var offset = __instance.backButton == null ? 50 : 0;

        if (!__instance.subTitleLabel.text.IsJapanese())
        {
            __instance.subTitleLabel.ProcessText();
            var labelSize = __instance.subTitleLabel.mCalculatedSize;
            var newUnderlineWidth = labelSize.x + 20 + offset;
            __instance.underLine.width = (int)Math.Round(newUnderlineWidth);
            var headerController = SingletonMonoBehaviour<HeaderController>.Instance;

            headerController.campaignIcons.SetIconPosition(
                headerController.viewManager.CurrentViewId,
                newUnderlineWidth
            );
        }
        else
        {
            Log.Debug("TitleTextPatch: Japanese text");
        }

        CoroutineStarter.Instance.StartCoroutine(
            TitleTextPatch.WaitForTranslationCoroutine(__instance.subTitleLabel, __instance.underLine, _setSubTitleText, offset).WrapToIl2Cpp());
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

[HarmonyPatch(typeof(PartsProfileCard), nameof(PartsProfileCard.Initialize))]
[HarmonyWrapSafe]
public class ProfileCardPatch
{
    public static void Postfix(PartsProfileCard __instance)
    {
        var labelGo =
            GameObject.Find(
                "_Game(Clone)/UI Root/ViewsArea/View/ViewMyProfileCard(Clone)/TopProfileImage/MyProfileCard/ProfileLayer/ProfileNode/Profile/Tower/ReachingFloor/common_dt_bg_pink/Label");
        if (!labelGo)
        {
            Log.Debug("Profile tower label GO not found");
            return;
        }

        var label = labelGo.GetComponent<CustomUILabel>();
        if (!label)
        {
            Log.Debug("Profile tower label not found");
            return;
        }

        label.lineWidth = 180;
        var pos = labelGo.transform.localPosition;
        pos.x = 3.4f;
        labelGo.transform.localPosition = pos;
        Log.Debug("Fixed profile tower label");
    }
}

[HarmonyPatch(typeof(PartsGoldShopPlate), nameof(PartsGoldShopPlate.SetUseJewel))]
[HarmonyWrapSafe]
public class GoldShopPlatePatch
{
    public static void Postfix(PartsGoldShopPlate __instance)
    {
        var label = __instance.useJewelTypeLabel;
        label.overflowMethod = UILabel.Overflow.ResizeFreely;
    }
}

[HarmonyPatch(typeof(PartsClanBattleRankingSelf), nameof(PartsClanBattleRankingSelf.SetData))]
[HarmonyWrapSafe]
public class ClanBattleRankingPatch
{
    public static void Postfix(PartsClanBattleRankingSelf __instance)
    {
        var label = __instance.clanTotalDamage;
        label.lineWidth = 180;
    }
}

//PartsEquipmentDetail.SetItemId
[HarmonyPatch(typeof(PartsEquipmentDetail), nameof(PartsEquipmentDetail.setInfoStatusAndUI))]
[HarmonyWrapSafe]
public class EquipmentDetailPatch
{
    public static void Postfix(PartsEquipmentDetail __instance)
    {
        var labelGos = __instance.gameObject.transform.GetComponentsInChildren<CustomUILabel>();
        if (labelGos == null)
        {
            return;
        }

        var labelArr = labelGos.ToArray();
        for (var i = 0; i < labelArr.Length; i++)
        {
            var label = labelArr[i];
            if (label == null)
            {
                continue;
            }

            if (label.curTextId == eTextId.EQUIP_STATUS_LABEL || label.text == "Equipment Stats")
            {
                label.overflowMethod = UILabel.Overflow.ResizeFreely;
            }
        }
    }
}

//ViewGrandArenaTop.SetRanking
[HarmonyPatch(typeof(ViewGrandArenaTop), nameof(ViewGrandArenaTop.StartView))]
[HarmonyWrapSafe]
public class GrandArenaRankingPatch
{
    public static void Postfix(ViewGrandArenaTop __instance)
    {
        __instance.newPlayerInfo.DefenseUnitButton.GetChildUILabel().overflowMethod = UILabel.Overflow.ResizeFreely;
        __instance.newPlayerInfo.BattleHistoryButton.GetChildUILabel().overflowMethod = UILabel.Overflow.ResizeFreely;
    }
}

[HarmonyPatch(typeof(ViewNormalArenaTop), nameof(ViewNormalArenaTop.StartView))]
[HarmonyWrapSafe]
public class NormalArenaRankingPatch
{
    public static void Postfix(ViewNormalArenaTop __instance)
    {
        __instance.defenseUnitButton.GetChildUILabel().overflowMethod = UILabel.Overflow.ResizeFreely;
        __instance.battleHistoryButton.GetChildUILabel().overflowMethod = UILabel.Overflow.ResizeFreely;
    }
}

[HarmonyPatch(typeof(UnitRarityUp), nameof(UnitRarityUp.Initialize))]
[HarmonyWrapSafe]
public class UnitRarityUpPatch
{
    public static void Postfix(UnitRarityUp __instance)
    {
        var howToObtain = __instance.howtoGetUnitMaterialButton.GetChildUILabel();
        howToObtain.multiLine = false;
        howToObtain.lineWidth = 180;

        var ascension = __instance.rarityUpButton.GetChildUILabel();
        ascension.fontSize = 28;
    }
}

[HarmonyPatch(typeof(UILabel), nameof(UILabel.ProcessText), typeof(bool), typeof(bool))]
[HarmonyWrapSafe]
public class UnclampPatch
{
    public static void Postfix(UILabel __instance)
    {
        var filteredProcessedText = Regex.Replace(__instance.mProcessedText ?? "", @"[\n ]*", "");
        var filteredText = Regex.Replace(__instance.mText ?? "", @"[\n ]*", "");
        if (
            __instance.isValid &&
            __instance.mChanged &&
            !string.IsNullOrEmpty(filteredProcessedText) &&
            __instance.overflowMethod == UILabel.Overflow.ClampContent &&
            filteredText != filteredProcessedText &&
            __instance.maxLineCount <= 3 &&
            __instance.name != "DetailLabel" &&
            __instance.name != "Label_item_name" &&
            __instance.height < 50 &&
            __instance.lineWidth < 300 &&
            !Regex.Matches(__instance.text, @"[\n]").Any()
        )
        {
            if (__instance.alignment == NGUIText.Alignment.Left)
            {
                __instance.pivot = UIWidget.Pivot.Left;
            }

            Log.Debug($"UnclampPatch: {__instance.name} - {__instance.mText} != {__instance.mProcessedText}");
            Log.Debug($"{__instance.maxLineCount} - {__instance.overflowMethod}");
            __instance.overflowMethod = UILabel.Overflow.ResizeFreely;
            __instance.ProcessText();
        }
    }
}

[HarmonyPatch(typeof(PartsTalentWeaknessIcons), nameof(PartsTalentWeaknessIcons.SetIcon), typeof(int))]
[HarmonyWrapSafe]
public class TalentWeaknessPatch
{
    public static void Postfix(PartsTalentWeaknessIcons __instance)
    {
        var labelObj = __instance.transform.Find("GUIGroup_Difficulty_Normal/Label");
        var label = labelObj.gameObject.GetComponentInChildren<CustomUILabel>();
        if (label == null)
        {
            Log.Debug("TalentWeaknessPatch: Label not found");
            return;
        }

        label.fontSize = 10;
    }
}

[HarmonyPatch(typeof(ViewQuestTop), nameof(ViewQuestTop.StartView))]
[HarmonyWrapSafe]
public class MirageButtonPatch
{
    public static void Postfix(ViewQuestTop __instance)
    {
        Log.Debug("MirageButtonPatch: Postfix");
        var uiLabel = __instance.buttonMirageQuest.Button.GetChildUILabel();
        uiLabel.lineWidth = 145;
        uiLabel.fontSize = 22;
        uiLabel.overflowMethod = UILabel.Overflow.ResizeHeight;
        var pos = uiLabel.transform.localPosition;
        pos.y = -50;
        pos.x = 0;
        uiLabel.transform.localPosition = pos;
    }
}

[HarmonyPatch(typeof(ViewMirageTop), nameof(ViewMirageTop.StartView))]
[HarmonyWrapSafe]
public class MirageAlcesButtonPatch
{
    public static void Postfix(ViewMirageTop __instance)
    {
        Log.Debug("MirageAlcesButtonPatch: Postfix");
        var uiLabelPath = "_Game(Clone)/UI Root/ViewsArea/View/ViewMirageTop(Clone)/RightNode/PartsMirageTopRightNode/AnchorTopRight/HeaderIconButton/Alces/Label";
        CoroutineStarter.Instance.StartCoroutine(WaitForAvailability(uiLabelPath).WrapToIl2Cpp());
    }
    
    public static IEnumerator WaitForAvailability(string path)
    {
        var timeout = 2.0f;
        var now = Time.realtimeSinceStartup;
        while (GameObject.Find(path) == null && Time.realtimeSinceStartup - now < timeout)
        {
            yield return null;
        }
        
        if (GameObject.Find(path) == null)
        {
            Log.Debug("MirageAlcesButtonPatch: Timeout");
            yield break;
        }
        
        var uiLabel = GameObject.Find(path)?.GetComponent<CustomUILabel>();
        if (!uiLabel)
        {
            Log.Debug("MirageAlcesButtonPatch: Label not found");
            yield break;
        }
        uiLabel!.lineWidth = 80;
        uiLabel.fontSize = 15;
        uiLabel.overflowMethod = UILabel.Overflow.ResizeHeight;
    }
}

