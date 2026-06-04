using System.Text;
using Cute;
using Elements;
using Elements.Data;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

public struct ProcessedItem
{
    public PartsUnitSkillDetailTextPlate.ePlateType PlateType;
    public int GroupId;
    public String Text;

    public ProcessedItem(PartsUnitSkillDetailTextPlate.ePlateType plateType, String text, int groupId)
    {
        PlateType = plateType;
        Text = text;
        GroupId = groupId;
    }
}

[HarmonyPatch(typeof(PartsUnitSkillDetailTextController), nameof(PartsUnitSkillDetailTextController.Initialize))]
[HarmonyWrapSafe]
public class SkillTextStorePatch
{
    // Stores the original texts from the Initialize method's _detailTextList argument.
    public static readonly System.Collections.Generic.List<ProcessedItem> StoredOriginalTexts =
        new System.Collections.Generic.List<ProcessedItem>();

    public static void Prefix(
        Il2CppSystem.Collections.Generic.List<
            Il2CppSystem.ValueTuple<PartsUnitSkillDetailTextPlate.ePlateType, System.String>> _detailTextList)
    {
        if (!Util.IsTranslationEnabled())
        {
            return;
        }

        Log.Debug("[SkillTextStorePatch] PartsUnitSkillDetailTextController.Initialize Prefix called.");
        StoredOriginalTexts.Clear(); // Clear for each new skill display

        if (_detailTextList == null)
        {
            Log.Warn("[SkillTextStorePatch] _detailTextList is null. Cannot store original texts.");
            return;
        }

        bool detailSectionReached = false;
        Log.Debug($"[SkillTextStorePatch] Attempting to store {_detailTextList.Count} original text items.");
        var removed = 0;
        var itemsInGroup = 0;
        for (int i = 0; i < _detailTextList.Count; i++)
        {
            itemsInGroup++;
            var tuple = _detailTextList.ToArray()[i];
            if (tuple.Item2 == "スキル効果")
            {
                itemsInGroup = 1;
                detailSectionReached = true;
                Log.Debug("[SkillTextStorePatch] Reached detail section, setting detailSectionReached to true.");
            }

            if (itemsInGroup > 2)
            {
                var previousItem = StoredOriginalTexts.Count > 0
                    ? StoredOriginalTexts[StoredOriginalTexts.Count - 1]
                    : default;
                previousItem.Text += tuple.Item2;
                StoredOriginalTexts[StoredOriginalTexts.Count - 1] = previousItem;
                Log.Debug(
                    $"[SkillTextStorePatch] Appending text to previous item: PlateType: {previousItem.PlateType}, Text: \"{previousItem.Text ?? "null"}\", Group: {(detailSectionReached ? '1' : '0')}");
            }
            else
            {
                StoredOriginalTexts.Add(new ProcessedItem(tuple.Item1, tuple.Item2, detailSectionReached ? 1 : 0));
                Log.Debug(
                    $"[SkillTextStorePatch] Stored Original Item {i + removed} - PlateType: {tuple.Item1}, Text: \"{tuple.Item2 ?? "null"}\", Group: {(detailSectionReached ? '1' : '0')}");
            }

            if (itemsInGroup > 2)
            {
                _detailTextList.RemoveAt(i);
                Log.Debug($"[SkillTextStorePatch] Removing the following plate:");
                removed++;
                i--;
            }

            Log.Info($"[SkillTextStorePatch] Successfully stored {StoredOriginalTexts.Count} original text items.");
        }
    }
}

[HarmonyPatch(typeof(PartsDialogUnitSkillDetail), nameof(PartsDialogUnitSkillDetail.display))]
[HarmonyWrapSafe]
public class SkillPopupPatch
{
    private const float VerticalSpacing = 7f;

    public static void Postfix(PartsDialogUnitSkillDetail __instance)
    {
        if (!Util.IsTranslationEnabled())
        {
            return;
        }

        Log.Debug("[SkillPopupPatch] PartsDialogUnitSkillDetail.display Postfix called.");
        if (__instance == null)
        {
            Log.Error("[SkillPopupPatch] PartsDialogUnitSkillDetail instance is null. Cannot proceed.");
            return;
        }

        Transform content = __instance.transform.Find("ScrollContent/ScrollView/WrapContent");
        if (content == null)
        {
            Log.Error("[SkillPopupPatch] ScrollContent/ScrollView/WrapContent not found. Cannot proceed.");
            return;
        }

        Log.Debug($"[SkillPopupPatch] Found WrapContent with {content.childCount} children.");
        Log.Debug(
            $"[SkillPopupPatch] {SkillTextStorePatch.StoredOriginalTexts.Count} original texts available for processing.");

        CustomUILabel firstActiveDetailLabelTextComponentInGroup = null!;
        var currentGroupDetailText = new StringBuilder();
        var platesToDisable = new System.Collections.Generic.List<GameObject>();
        var originalTextsQueue =
            new System.Collections.Generic.Queue<ProcessedItem>(SkillTextStorePatch.StoredOriginalTexts);

        for (int i = 0; i < content.childCount; i++)
        {
            Transform plate = content.GetChild(i);
            if (plate == null) continue;

            Transform titleLabelTransform = plate.Find("TitleLabel");
            CustomUILabel titleTextComponent = null!;
            bool isTitleActive = false;
            if (titleLabelTransform != null && titleLabelTransform.gameObject.activeSelf)
            {
                titleTextComponent = titleLabelTransform.GetComponent<CustomUILabel>();
                if (titleTextComponent != null)
                {
                    isTitleActive = true;
                }
                else
                {
                    Log.Warn(
                        $"[SkillPopupPatch] Plate '{plate.name}' has active TitleLabel but no CustomUILabel component.");
                }
            }

            Transform detailLabelTransform = plate.Find("DetailLabel");
            CustomUILabel detailTextComponent = null!;
            bool isDetailActive = false;
            if (detailLabelTransform && detailLabelTransform.gameObject.activeSelf)
            {
                detailTextComponent = detailLabelTransform.GetComponent<CustomUILabel>();
                if (detailTextComponent)
                {
                    isDetailActive = true;
                }
                else
                {
                    Log.Warn(
                        $"[SkillPopupPatch] Plate '{plate.name}' has active DetailLabel but no CustomUILabel component.");
                }
            }

            ProcessedItem originalItem = default;
            bool originalItemRetrieved = false;
            if (isTitleActive || isDetailActive)
            {
                if (originalTextsQueue.Count > 0)
                {
                    originalItem = originalTextsQueue.Dequeue();
                    originalItemRetrieved = true;
                }
                else
                {
                    Log.Warn(
                        $"[SkillPopupPatch] Plate '{plate.name}' (Child {i}) is active, but no more original texts in queue.");
                }
            }

            if (isTitleActive)
            {
                string originalTitleText =
                    originalItemRetrieved ? (originalItem.Text ?? "N/A") : "N/A (Original Missing)";
                Log.Debug(
                    $"[SkillPopupPatch] Plate '{plate.name}' (Child {i}): Active TitleLabel. UI Text: '{titleTextComponent?.text ?? "N/A"}'. Original Text: '{originalTitleText}'");

                if (firstActiveDetailLabelTextComponentInGroup != null && currentGroupDetailText.Length > 0)
                {
                    Log.Debug(
                        $"[SkillPopupPatch] Finalizing previous detail group. Combined original text: '{currentGroupDetailText.ToString()}' for {firstActiveDetailLabelTextComponentInGroup.transform.parent.name}");
                    firstActiveDetailLabelTextComponentInGroup.text = currentGroupDetailText.ToString()
                        .Replace("　", "").Replace("\u3000", "");
                    /*foreach (GameObject ptd in platesToDisable)
                    {
                        Log.Debug($"[SkillPopupPatch] Disabling plate: {ptd.name}");
                        ptd.SetActive(false);
                    }*/
                }

                currentGroupDetailText.Clear();
                platesToDisable.Clear();
                firstActiveDetailLabelTextComponentInGroup = null!;
                // if (originalItemRetrieved && titleTextComponent != null) titleTextComponent.text = originalItem.Text; // Optionally restore original title text
            }
            else if (isDetailActive)
            {
                if (!originalItemRetrieved)
                {
                    Log.Warn(
                        $"[SkillPopupPatch] Plate '{plate.name}' (Child {i}): Active DetailLabel, but no original text retrieved. UI Text: '{detailTextComponent?.text ?? "N/A"}'. Skipping for concatenation.");
                    if (firstActiveDetailLabelTextComponentInGroup != null)
                    {
                        platesToDisable.Add(plate.gameObject);
                        Log.Debug(
                            $"[SkillPopupPatch] Marking plate '{plate.name}' for disabling as it's a subsequent detail and original text is missing.");
                    }

                    continue;
                }

                string currentOriginalDetailText = originalItem.Text ?? "";
                Log.Debug(
                    $"[SkillPopupPatch] Plate '{plate.name}' (Child {i}): Active DetailLabel. Using original text: '{currentOriginalDetailText}'. UI Text: '{detailTextComponent?.text ?? "N/A"}'");

                if (firstActiveDetailLabelTextComponentInGroup == null)
                {
                    firstActiveDetailLabelTextComponentInGroup = detailTextComponent!;
                    if (detailTextComponent != null) // Null check for safety
                    {
                        detailTextComponent.overflowMethod = CustomUILabel.Overflow.ResizeHeight;
                        detailTextComponent.pivot = UIWidget.Pivot.Top;
                        detailTextComponent.fontSize = 17;
                    }

                    currentGroupDetailText.Append(currentOriginalDetailText.Replace("\u3000", ""));
                    Log.Debug(
                        $"[SkillPopupPatch] Started new detail group with plate '{plate.name}'. Original Text: '{currentOriginalDetailText}'");
                }
                else
                {
                    currentGroupDetailText.Append(currentOriginalDetailText.Replace("\u3000", ""));
                    platesToDisable.Add(plate.gameObject);
                    Log.Debug(
                        $"[SkillPopupPatch] Appended to detail group from plate '{plate.name}'. Original Text (after replace): '{currentOriginalDetailText.Replace("\u3000", "")}'. Marked for disabling.");
                }
            }
            else
            {
                if (originalItemRetrieved)
                {
                    Log.Debug(
                        $"[SkillPopupPatch] Plate '{plate.name}' (Child {i}): Inactive or no label component. Original item (Type: {originalItem.PlateType}, Text: '{originalItem.Text}') was dequeued but not used for this plate.");
                }
                else if ((titleLabelTransform != null && titleLabelTransform.gameObject.activeSelf) ||
                         (detailLabelTransform != null && detailLabelTransform.gameObject.activeSelf))
                {
                    // Handled by originalItemRetrieved being false and logged above
                }
                else
                {
                    Log.Debug(
                        $"[SkillPopupPatch] Plate '{plate.name}' (Child {i}): Neither TitleLabel nor DetailLabel is active, or no Text component found. Skipping.");
                }
            }
        }

        if (firstActiveDetailLabelTextComponentInGroup != null && currentGroupDetailText.Length > 0)
        {
            Log.Debug(
                $"[SkillPopupPatch] Finalizing last detail group after loop. Combined original text: '{currentGroupDetailText.ToString()}' for {firstActiveDetailLabelTextComponentInGroup.transform.parent.name}");
            firstActiveDetailLabelTextComponentInGroup.text =
                currentGroupDetailText.ToString();
            /*foreach (GameObject ptd in platesToDisable)
            {
                Log.Debug($"[SkillPopupPatch] Disabling plate from last group: {ptd.name}");
                ptd.SetActive(false);
            }*/
        }

        if (originalTextsQueue.Count > 0)
        {
            Log.Warn(
                $"[SkillPopupPatch] Finished processing UI plates, but {originalTextsQueue.Count} original texts remain in the queue.");
        }

        // Reposition active plates
        Log.Debug("[SkillPopupPatch] Repositioning active plates.");
        float currentYOffset = 0f; // Assuming top-down layout, Y decreases. Start at top.
        for (int i = 0; i < content.childCount; i++)
        {
            Transform plateTransform = content.GetChild(i);
            if (plateTransform == null || !plateTransform.gameObject.activeSelf)
            {
                continue;
            }

            // Set position using the plateTransform directly
            plateTransform.localPosition = new Vector3(plateTransform.localPosition.x, currentYOffset,
                plateTransform.localPosition.z);
            Log.Debug($"[SkillPopupPatch] Repositioned '{plateTransform.name}' to Y: {currentYOffset}");

            // Calculate height for next offset
            float plateHeight = 0f;
            float nextOffset = 0f;
            CustomUILabel activeLabel = null;

            Transform titleLabelTransform = plateTransform.Find("TitleLabel");
            if (titleLabelTransform != null && titleLabelTransform.gameObject.activeSelf)
            {
                activeLabel = titleLabelTransform.GetComponent<CustomUILabel>();
                nextOffset = 0f;
            }

            if (activeLabel == null) // If title was not active or had no label, check detail
            {
                Transform detailLabelTransform = plateTransform.Find("DetailLabel");
                if (detailLabelTransform != null && detailLabelTransform.gameObject.activeSelf)
                {
                    activeLabel = detailLabelTransform.GetComponent<CustomUILabel>();
                    var pos = activeLabel.transform.localPosition;
                    pos.y = 12;
                    activeLabel.transform.localPosition = pos;
                    nextOffset = 15f;
                }
            }

            if (activeLabel != null)
            {
                // Ensure the label's geometry is up-to-date before getting its size
                // This might be necessary if text changes affect its height and it's not immediately recalculated.
                plateHeight = activeLabel.localSize.y;
                Log.Debug(
                    $"[SkillPopupPatch] Plate '{plateTransform.name}' height (from CustomUILabel.localSize.y): {plateHeight}");
            }
            else
            {
                Log.Warn(
                    $"[SkillPopupPatch] Plate '{plateTransform.name}' is active, but could not determine height from an active CustomUILabel. Height defaults to 0 for layout purposes.");
            }
            
            if (plateHeight <= 0 && plateTransform.gameObject.activeSelf)
            {
                Log.Warn(
                    $"[SkillPopupPatch] Plate '{plateTransform.name}' calculated height is {plateHeight}. This might lead to incorrect layout.");
                // Consider a default minimum height for visible plates if this is problematic:
                // plateHeight = Mathf.Max(plateHeight, MinimumPlateHeightConstant); 
            }


            currentYOffset -= plateHeight; // Y decreases as we go down
            currentYOffset -= VerticalSpacing; // Add spacing for the next element
            currentYOffset -= nextOffset; // detail should be closer to the title
        }

        __instance.textController.curUIScrollContent.curUIScrollView.enabled = true;
        __instance.textController.curUIScrollContent.curUIScrollBar.enabled = true;
        

        Log.Debug("[SkillPopupPatch] Text combination and repositioning process finished.");
    }
}