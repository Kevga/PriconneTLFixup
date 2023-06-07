// ReSharper disable InconsistentNaming

using System.Text.RegularExpressions;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Object = Il2CppSystem.Object;

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
        return false;
    }
    
    internal static void setPrintText(StoryCommandPrint __instance, EventDelegate.Callback _typewriteFinishAction)
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
        __instance.nameLabel.SetText(__instance.newNameStr, new Il2CppReferenceArray<Object>(0L));
        __instance.textLabel.SetText(__instance.newTextStr, new Il2CppReferenceArray<Object>(0L));
        __instance.NameText = __instance.newNameStr;
        __instance.Text = __instance.newTextStr;
        typewriterEffect.ResetToOffset(offset);
        if (__instance.textLabel.text.Length == __instance.newTextStr.Length)
        {
            _typewriteFinishAction.Invoke();
            return;
        }
        EventDelegate.Add(typewriterEffect.onFinished, _typewriteFinishAction);
        typewriterEffect.Finish();
    }
}

/**
 * This patch calls FeedPage whenever possible to skip all WAIT commands and finish the typewriting effect.
 * This allows the translation endpoint to translate the entire text at once, improving speed and quality.
 */
[HarmonyPatch(typeof(StoryManager), "execCommand")]
public static class StoryManagerPatch
{
    private static bool shouldFeedPage;
    public static void Prefix(StoryManager __instance, int _index)
    {
        var count = __instance.storyCommandList.Count;
        if (_index >= count)
        {
            return;
        }

        var currentCommand = __instance.storyCommandList.ToArray()[_index];
        if (currentCommand.Number == CommandNumber.PRINT)
        {
            shouldFeedPage = true;
        }
    }
    
    public static void Postfix(StoryManager __instance, int _index)
    {
        var count = __instance.storyCommandList.Count;
        if (_index >= count)
        {
            return;
        }

        var commands = __instance.storyCommandList.ToArray();
        var currentCommand = commands[_index];
        if (currentCommand.Number != CommandNumber.PRINT)
        {
            return;
        }
        
        var args = currentCommand.Args.ToArray();
        var text = args[1];
        
        //Check if text is empty or only contains color tags
        text = Regex.Replace(text, @"\[([0-9A-F]{8})\]", "");
        if (text.Length == 0)
        {
            return;
        }
        
        if (!shouldFeedPage)
        {
            return;
        }
        
        if (!isPrintNext(commands, _index))
        {
            return;
        }
        
        shouldFeedPage = false;
        __instance.FeedPage();
        __instance.SetTouchEnabled(true);
        __instance.touchDelegateList.Clear();
    }
    
    internal static bool isPrintNext(CommandStruct[] commands, int index)
    {
        if (index >= commands.Length)
        {
            return false;
        }
        
        //Check all following commands. Return false if a CHOICE command is found before the next print, or if there is no more PRINT command.
        for (var i = index + 1; i < commands.Length; i++)
        {
            if (commands[i].Number == CommandNumber.CHOICE)
            {
                return false;
            }
            if (commands[i].Number == CommandNumber.TOUCH || commands[i].Number == CommandNumber.TOUCH_TO_START)
            {
                return false;
            }
            if (commands[i].Number == CommandNumber.PRINT)
            {
                return true;
            }
        }
        
        return false;
    }
}

/**
 * Same as above, but for tutorial stories. Should probably be refactored to use Harmony's TargetMethods.
 */
[HarmonyPatch(typeof(TutorialStoryManager), "execCommand")]
public static class TutorialStoryManagerPatch
{
    private static bool shouldFeedPage;
    public static void Prefix(TutorialStoryManager __instance, int _commndIndex)
    {
        var count = __instance.storyCommandList.Count;
        if (_commndIndex >= count)
        {
            return;
        }

        var currentCommand = __instance.storyCommandList.ToArray()[_commndIndex];
        if (currentCommand.Number == CommandNumber.PRINT)
        {
            shouldFeedPage = true;
        }
    }
    
    public static void Postfix(TutorialStoryManager __instance, int _commndIndex)
    {
        var count = __instance.storyCommandList.Count;
        if (_commndIndex >= count)
        {
            //Plugin.Logger.LogDebug("End of story: " + _index);
            return;
        }

        var commands = __instance.storyCommandList.ToArray();
        var currentCommand = commands[_commndIndex];
        if (currentCommand.Number != CommandNumber.PRINT)
        {
            return;
        }
        
        var args = currentCommand.Args.ToArray();
        var text = args[1];
        
        //Check if text is empty or only contains color tags
        text = Regex.Replace(text, @"\[([0-9A-F]{8})\]", "");
        if (text.Length == 0)
        {
            return;
        }
        
        if (!shouldFeedPage)
        {
            return;
        }
        
        if (!StoryManagerPatch.isPrintNext(commands, _commndIndex))
        {
            return;
        }
        
        shouldFeedPage = false;
        __instance.FeedPage();
        __instance.SetTouchEnabled(true);
        __instance.touchDelegateList.Clear();
    }
}