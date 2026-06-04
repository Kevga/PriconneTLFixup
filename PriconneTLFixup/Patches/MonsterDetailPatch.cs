using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace PriconneTLFixup.Patches;

/**
 * This set of patches does the following:
 * - Combines the list of strings that were previously displayed separately in their own respective UILabel into one
 * - Enables wrapping of this one label
 * - Adds line breaks before each bullet point
 */
[HarmonyPatch(typeof(PartsMonsterDetailTextController), nameof(PartsMonsterDetailTextController.Initialize))]
[HarmonyWrapSafe]
public class SkillDescriptionPatch
{
    public static void Prefix(ref Il2CppSystem.Collections.Generic.List<string> _monsterDetailTextList)
    {
        if (!Util.IsTranslationEnabled())
        {
            return;
        }

        Log.Debug("Combining monster detail text list into one string");
        _monsterDetailTextList.ToArray().ForEach(new Action<string>(s => Log.Debug("Original string: " + s)));
        var finalList = new Il2CppSystem.Collections.Generic.List<string>();
        var _string = _monsterDetailTextList.ToArray().Join(null, "\n");
        finalList.Add(_string);
        _monsterDetailTextList = finalList;
    }
}

[HarmonyPatch(typeof(PartsDialogMonsterDetail), nameof(PartsDialogMonsterDetail.InitializeParam))]
[HarmonyWrapSafe]
public class MonsterDetailScrollContainerPatch
{
    public static void Postfix(PartsDialogMonsterDetail __instance)
    {
        __instance.monsterDetailTextController.curUIScrollContent.curUIScrollView.enabled = true;
    }
}

[HarmonyPatch(typeof(PartsMonsterDetailTextPlate), nameof(PartsMonsterDetailTextPlate.SetText))]
[HarmonyWrapSafe]
public class MonsterDetailOverflowPatch
{
    internal static Coroutine? coroutine;

    public static void Postfix(PartsMonsterDetailTextPlate __instance)
    {
        __instance.detailText.overflowMethod = UILabel.Overflow.ResizeHeight;
        if (coroutine != null)
        {
            CoroutineStarter.Instance.StopCoroutine(coroutine);
        }

        coroutine = CoroutineStarter.Instance.StartCoroutine(UpdateDetailTextPlate(__instance).WrapToIl2Cpp());
    }

    private static IEnumerator UpdateDetailTextPlate(PartsMonsterDetailTextPlate textPlate)
    {
        var gameObject = textPlate.gameObject;
        var parent = gameObject.transform.parent;
        Transform lowestChild = null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (lowestChild == null && child != gameObject.transform || child.position.y < lowestChild?.position.y)
            {
                lowestChild = child;
            }
        }

        if (lowestChild == null)
        {
            Log.Debug("Could not find lowest child of PartsMonsterDetailTextPlate");
        }
        else
        {
            Log.Debug("Found lowest child of PartsMonsterDetailTextPlate");
        }

        var text = "";
        //var separators = new[] {"・", "·"};
        while (textPlate.detailText != null)
        {
            if (textPlate.detailText.text != text)
            {
                Log.Debug("Updating PartsMonsterDetailTextPlate text");
                Log.Debug(textPlate.detailText.text);
                text = textPlate.detailText.text;

                yield return null;
                if (lowestChild != null)
                {
                    var textHeight = textPlate.detailText.height;
                    Log.Debug($"Lowest child y: {lowestChild.localPosition.y}, height: {textHeight}");
                    gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x,
                        lowestChild.localPosition.y - textHeight / 2, gameObject.transform.localPosition.z);
                }

                yield return null;
                yield return null;
                yield return null;

                UIDragScrollView scrollRect = null!;
                scrollRect = textPlate.transform.GetComponentInChildren<UIDragScrollView>();
                if (scrollRect != null)
                {
                    Log.Debug("Calling CallOnPress on " + scrollRect.gameObject.name);
                    scrollRect.CallOnPress();
                }

                scrollRect = textPlate.transform.parent.GetComponentInChildren<UIDragScrollView>();
                if (scrollRect != null)
                {
                    Log.Debug("Calling CallOnPress on " + scrollRect.gameObject.name);
                    scrollRect.CallOnPress();
                }

                var scrollView = textPlate;
                if (scrollView != null)
                {
                    scrollRect = scrollView.transform.GetComponent<UIDragScrollView>();
                    Log.Debug("Calling CallOnPress on " + scrollRect.gameObject.name);
                    scrollRect.CallOnPress();
                }
                else
                {
                    Log.Debug("Could not find UIDragScrollView");
                }
            }

            yield return null;
        }

        // Log.Debug("PartsMonsterDetailTextPlate destroyed");
    }
}

[HarmonyPatch(typeof(PartsDialogMonsterDetail), nameof(PartsDialogMonsterDetail.OnDestroy))]
[HarmonyWrapSafe]
public class MonsterDetailScrollContainerPatch2
{
    public static void Postfix()
    {
        Log.Debug("PartsDialogMonsterDetail destroyed");
        if (MonsterDetailOverflowPatch.coroutine != null)
        {
            CoroutineStarter.Instance.StopCoroutine(MonsterDetailOverflowPatch.coroutine);
        }
    }
}