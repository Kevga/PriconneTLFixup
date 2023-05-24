// ReSharper disable InconsistentNaming

using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

/**
 * This patch fixes an issue where the dialog text box gets cleared when it shouldn't. The game automatically does this
 * whenever the character name changes. If we translate it, it's registered as a change and the text box gets cleared.
 * We reimplement setPrintText while keeping track of the original name, ignoring changes from the translation plugin.
 */
[HarmonyPatch(typeof(StoryCommandPrint), nameof(StoryCommandPrint.setPrintText))]
public class StoryDialogPatch
{
    private static string currentName = "";
    
    public static bool Prefix(StoryCommandPrint __instance, EventDelegate.Callback _typewriteFinishAction)
    {
        setPrintText(__instance, _typewriteFinishAction);
        /*if (!__instance.needWait())
        {
            __instance.storyManager.FeedPage();
            Plugin.Logger.LogDebug("StoryCommandPrint.setPrintText fedPage");
        }*/

        return false;
    }
    
    private static void setPrintText(StoryCommandPrint __instance, EventDelegate.Callback _typewriteFinishAction)
    {
        var offset = 0;
        var typewriterEffect = __instance.textLabel.gameObject.GetComponent<TypewriterEffect>();
        if (typewriterEffect == null)
        {
            typewriterEffect = __instance.textLabel.gameObject.AddComponent<TypewriterEffect>();
            typewriterEffect.charsPerSecond = __instance.typewriteSpeed;
        }
        else
        {
            EventDelegate.Remove(typewriterEffect.onFinished, _typewriteFinishAction);
            typewriterEffect.Finish();
        }
        if (!__instance.isBetweenCommand && currentName == __instance.newNameStr)
        {
            offset = __instance.textLabel.text.Length;
            __instance.newTextStr = __instance.textLabel.text + __instance.newTextStr;
        }
        
        currentName = __instance.newNameStr;
        __instance.nameLabel.SetText(__instance.newNameStr, new Il2CppReferenceArray<Il2CppSystem.Object>(0L));
        __instance.textLabel.SetText(__instance.newTextStr, new Il2CppReferenceArray<Il2CppSystem.Object>(0L));
        __instance.NameText = __instance.newNameStr;
        __instance.Text = __instance.newTextStr;
        typewriterEffect.ResetToOffset(offset);
        if (__instance.textLabel.text.Length == __instance.newTextStr.Length)
        {
            _typewriteFinishAction.Invoke();
            return;
        }
        EventDelegate.Add(typewriterEffect.onFinished, _typewriteFinishAction);
    }
}

/**
 * Color code fix. They may be getting removed by automated translation, this patch adds them back.
 */
[HarmonyPatch(typeof(AutoTranslationPlugin), "SetText")]
public static class DisableNameTranslationPatch
{
    public static bool Prefix(AutoTranslationPlugin __instance, object ui, ref string text, string originalText)
    {
        if (originalText.Contains("[3C404EFF]"))
        {
            text = "[3C404EFF]" + text;
        }
        
        return true;
    }
}