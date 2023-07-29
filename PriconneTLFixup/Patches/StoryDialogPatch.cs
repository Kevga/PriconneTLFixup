// ReSharper disable InconsistentNaming

using System.Collections;
using System.Text.RegularExpressions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using Object = Il2CppSystem.Object;

namespace PriconneTLFixup.Patches;

/**
 * This patch fixes an issue where the dialog text box gets cleared when it shouldn't. The game automatically does this
 * whenever the character name changes. If we translate it, it's registered as a change and the text box gets cleared.
 * We reimplement setPrintText while keeping track of the original name, ignoring changes from the translation plugin.
 */
[HarmonyPatch(typeof(StoryCommandPrint), nameof(StoryCommandPrint.setPrintText))]
[HarmonyWrapSafe]
public class StoryDialogPatch
{
    private static string currentName = "";
    private static string currentText = "";
    internal static long typeWriterFinishTime;
    
    public static bool Prefix(StoryCommandPrint __instance, EventDelegate.Callback _typewriteFinishAction)
    {
        var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        typeWriterFinishTime = (long) Math.Round(((__instance.textLabel.text.Length + __instance.newTextStr.Length) / (__instance.typewriteSpeed / 2f)) * 1000) + now;
        Log.Debug($"Delaying by {((__instance.textLabel.text.Length + __instance.newTextStr.Length) / (__instance.typewriteSpeed / 2f))}s");
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
            __instance.newTextStr = currentText + __instance.newTextStr;
        }
        
        currentName = __instance.newNameStr;
        currentText = __instance.newTextStr;
        __instance.NameText = __instance.newNameStr;
        __instance.nameLabel.SetText(__instance.newNameStr, new Il2CppReferenceArray<Object>(0L));

        if (!StoryManagerPatch.isPrintNext(__instance.storyManager.storyCommandList.ToArray(),
                __instance.storyManager.currentCommandIndex))
        {
            __instance.textLabel.SetText(currentText, new Il2CppReferenceArray<Object>(0L));
            __instance.Text = __instance.newTextStr;
            currentText = "";
        }
        
        //Log.Debug($"setPrintText: {__instance.newTextStr}");
        typewriterEffect.ResetToOffset(offset);
        _typewriteFinishAction.Invoke();
        typewriterEffect.Finish();
    }
}

[HarmonyPatch(typeof(StoryScene), nameof(StoryScene.onClickNextCommand))]
[HarmonyWrapSafe]
public class StoryAutoDelayPatch
{
    public static bool Prefix(StoryScene __instance, bool _autoClick)
    {
        if (!__instance.storyManager.NoVoice)
        {
            if (_autoClick && StoryDialogPatch.typeWriterFinishTime >
                DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
            {
                return false;
            }
            
            return true;
        }
        
        if (__instance.IsAutoPlay)
        {
            if ((__instance.lipSyncCoroutineList != null && __instance.isPlayLipsync && __instance.lipSyncCoroutineList.Count > __instance.lipSyncIndex) || StoryDialogPatch.typeWriterFinishTime > DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
            {
                return false;
            }
        }

        return true;
    }
}

/**
 * Remove color codes from the text before it's printed. Parse that color and apply directly to the label.
 * This keeps translation rules cleaner and allows for the same rule to be used for dialog and the log.
 */
[HarmonyPatch(typeof(StoryCommandPrint), nameof(StoryCommandPrint.SetCommandParam))]
[HarmonyWrapSafe]
public class StoryColorRemovalPatch
{
    internal static readonly Regex ColorCodeRegex = new(@"\[([0-9A-Fa-fsS]{6,8})\]", RegexOptions.Compiled);
    public static void Postfix(StoryCommandPrint __instance)
    {
        //Log.Debug("Before: " + __instance.newTextStr);
        var lastColorCode = "";
        var colorCodes = TranslationPreprocessorPatch.PostTranslationColorCodeRegex.Matches(__instance.newTextStr).ToList();
        if (colorCodes.Count > 0)
        {
            lastColorCode = colorCodes[^1].Groups[1].Value;
        }

        var textColor = Color.white;
        if (lastColorCode.Length == 6)
        {
            textColor = NGUIText.ParseColor(lastColorCode, 0);
        } 
        else if (lastColorCode.Length == 8)
        {
            textColor = NGUIText.ParseColor32(lastColorCode, 0);
        }
        else
        {
            Log.Warn("Invalid color code: " + lastColorCode);
        }
        //Log.Debug("Color: " + textColor);
        __instance.textLabel.color = textColor;
        __instance.newTextStr = ColorCodeRegex.Replace(__instance.newTextStr, "");
        //Log.Debug("After: " + __instance.newTextStr);
    }
}


/**
 * Pretranslate story text in the background to remove the MTL delay.
 */
[HarmonyPatch(typeof(StoryManager), nameof(StoryManager.StartStory))]
[HarmonyWrapSafe]
public static class StoryPretranslationPatch
{
    internal static CustomUILabel? PretranslationLabel;
    private static int currentTranslationIndex;
    private static Regex userNameImplement = new("\\{0\\}", RegexOptions.Compiled);
    private static Regex deleteDoubleQuotation = new("^\"(.*)\"$", RegexOptions.Compiled);

    public static void Postfix(StoryManager __instance)
    {
        if (PretranslationLabel == null)
        {
            var pretranslationGameObject = new GameObject();
            pretranslationGameObject.transform.localPosition = new Vector3(0, -1000, 0);
            PretranslationLabel = pretranslationGameObject.AddComponent<CustomUILabel>();
            PretranslationLabel.name = "PriconneTLFixupStoryPretranslationLabel";
            Log.Info("Created pre-translation label");
        }
        
        CoroutineStarter.Instance.StartCoroutine(PretranslationCoroutine(__instance).WrapToIl2Cpp());
    }
    
    internal static IEnumerator PretranslationCoroutine(StoryManager manager)
    {
        var commands = manager.storyCommandList?.ToArray();
        const int stallTime = 4000;
        Log.Debug("Starting pretranslation of story");
        yield return null;

        var currentText = "";
        var synopsisTranslated = false;
        var lastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        while (PretranslationLabel != null && manager != null && !manager.IsEndStory)
        {
            //If a video is playing, pause story pretranslation to make room for subtitle pretranslation
            if ((manager.movieManager != null && manager.movieManager.IsPlay()))
            {
                yield return null;
                continue;
            }
            
            //Wait for XUAT to translate and update the label before continuing. If not updated within stallTime, continue anyway.
            if (currentText != "" && currentText == PretranslationLabel.text && ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - lastUpdate) < stallTime)
            {
                yield return null;
                continue;
            }

            if (!synopsisTranslated)
            {
                lastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                var isInsertStory = manager.storyMenu.isInsertStorySkip();
                currentText = manager.StorySkipDialogParams.GetSynopsisLabelText(isInsertStory);
                PretranslationLabel.SetText(currentText);
                synopsisTranslated = true;
                //Log.Debug("Pretranslating synopsis: " + currentText);
                yield return null;
                continue;
            }

            //Advance the current translation index if the story has progressed beyond it
            if (manager.currentCommandIndex >= currentTranslationIndex)
            {
                currentTranslationIndex = manager.currentCommandIndex+1;
            }

            //Load the next story line
            var text = GetNextStringToTranslate(commands, currentTranslationIndex+1);
            text = ReplacePlayerName(text);
            text = StoryColorRemovalPatch.ColorCodeRegex.Replace(text, "");
            
            //If there is no more text, stop pretranslation
            if (text is null or "")
            {
                yield return null;
                break;
            }
            
            lastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            currentText = text;
            PretranslationLabel.SetText(currentText);
            //Log.Debug("Pretranslating: " + currentText);
            
            //rate limit sub translation requests
            yield return new WaitForSecondsRealtime(Util.GetTranslationDelayInSeconds()); 
        }
        
        Log.Debug("Pretranslation finished");
        if (PretranslationLabel != null)
        {
            PretranslationLabel.SetText("");
        }
    }
    
    internal static string ReplacePlayerName(string text)
    {
        var playerName = Singleton<UserData>.Instance.UserInfo.UserName;
        if (playerName == null)
        {
            return text;
        }
        return userNameImplement.Replace(text, playerName);
    }
    
    internal static string GetNextStringToTranslate(CommandStruct[] commands, int startIndex)
    {
        if (startIndex >= commands.Length)
        {
            return "";
        }

        var nextInteraction = GetNextInteractionIndex(commands, startIndex);
        currentTranslationIndex = nextInteraction;
        for (var i = startIndex; i < nextInteraction; i++)
        {
            if (commands[i].Number == CommandNumber.CHOICE)
            {
                currentTranslationIndex = i;
                return commands[i].Args.ToArray()[0];
            }
        }
        /*if (nextInteraction == startIndex && commands[nextInteraction].Number == CommandNumber.CHOICE)
        {
            //commands[nextInteraction].Args.ToArray()
            return "";
        }*/

        if (nextInteraction == startIndex)
        {
            return GetNextStringToTranslate(commands, nextInteraction + 1);
        }
        
        if (nextInteraction == -1)
        {
            return "";
        }
        
        return GetPrintTextBetween(commands, startIndex, nextInteraction);
    }
    
    internal static int GetNextInteractionIndex(CommandStruct[] commands, int index)
    {
        if (index >= commands.Length)
        {
            return -1;
        }
        
        for (var i = index + 1; i < commands.Length; i++)
        {
            if (commands[i].Number == CommandNumber.TOUCH || commands[i].Number == CommandNumber.TOUCH_TO_START || commands[i].Number == CommandNumber.CHOICE)
            {
                return i;
            }
        }
        
        return -1;
    }
    
    internal static string GetPrintTextBetween(CommandStruct[] commands, int startIndex, int endIndex)
    {
        if (startIndex >= commands.Length)
        {
            return "";
        }
        
        var text = "";
        for (var i = startIndex; i < commands.Length && i < endIndex; i++)
        {
            if (commands[i].Number == CommandNumber.PRINT)
            {
                text += commands[i].Args.ToArray()[1];
            }
        }
        
        text = text.Replace("\\n", "\n");
        
        return text;
    }
}

/**
 * This patch calls FeedPage whenever possible to skip all WAIT commands and finish the typewriting effect.
 * This allows the translation endpoint to translate the entire text at once, improving speed and quality.
 */
[HarmonyPatch(typeof(StoryManager), "execCommand")]
[HarmonyWrapSafe]
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

        var commands = __instance.storyCommandList.ToArray();
        Log.Debug("Command ["+_index+"]: " + commands[_index].Number + " - Args: " + string.Join(", ", commands[_index].Args.ToArray()));
        var currentCommand = commands[_index];
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
[HarmonyWrapSafe]
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
            //Log.Debug("End of story: " + _index);
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