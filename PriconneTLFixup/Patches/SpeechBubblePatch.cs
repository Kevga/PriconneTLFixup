using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * Enables word-wrapping on speech bubbles, as well as disabling the typewriter effect on them.
 * To fix an issue with bubbles that disappear shortly after the typewriter effect has finished, a delay is added.
 */
[HarmonyPatch(typeof(PartsRoomBalloon), nameof(PartsRoomBalloon.ShowSpeakingText))]
[HarmonyWrapSafe]
public class SpeechBubblePatch
{
    public static void Postfix(PartsRoomBalloon __instance, string setText, RoomCharacter character)
    {
        if (setText.IsNullOrEmpty())
        {
            return;
        }
        
        character.StartCoroutine(UpdateSpeechBubble(__instance, setText).WrapToIl2Cpp());
    }

    private static IEnumerator UpdateSpeechBubble(PartsRoomBalloon balloon, string originalText)
    {
        var text = originalText;
        while (balloon.SpeakingActive)
        {
            if (balloon.speakingText.text != text)
            {
                text = balloon.speakingText.text;
                balloon.speakingBalloon.SetDimensions(balloon.speakingBalloon.width, balloon.speakingText.height + 30);
            }

            yield return null;
        }
    }
}

[HarmonyPatch(typeof(SpineDramaController), nameof(SpineDramaController.commandTextPlay))]
[HarmonyWrapSafe]
public class SpineDramaControllerBalloonPatch
{
    public static bool Prefix(SpineDramaController __instance)
    {
        if (__instance.isSkip)
        {
            return false;
        }
        
        var key = __instance.intParse(__instance.nextParam());
        
        var balloon = __instance.textBalloonDic[key];
        balloon.balloonSprite.width = 500;
        balloon.textLabel.width = balloon.balloonSprite.width - 40;
        balloon.textLabel.overflowMethod = UILabel.Overflow.ResizeHeight;
        balloon.textLabel.spacingY = 6;
        
        var text = __instance.nextParam();
        var charsPerSecond = __instance.intParse(__instance.nextParam());
        var tapSkip = __instance.boolParse(__instance.nextParam());
        var tapWait = __instance.boolParse(__instance.nextParam());
        var offsetX = __instance.floatParse(__instance.nextParam());
        var offsetY = __instance.floatParse(__instance.nextParam());
        var fadeoutDelayOffset = __instance.floatParse(__instance.nextParam());
        if (!__instance.textBalloonDic.ContainsKey(key))
        {
            return false;
        }
        
        var correctedDelay = fadeoutDelayOffset;
        if (text.Length > 0 && !tapSkip)
        {
            if (charsPerSecond > 0)
            {
                correctedDelay += text.Length / (float)charsPerSecond;
            }
            else
            {
                correctedDelay += 5;
                Log.Warn("charsPerSecond was 0, using default delay of 5 seconds");
            }
            
            Log.Debug($"Corrected delay: {correctedDelay} from {fadeoutDelayOffset} + {text.Length} / {charsPerSecond}");
        }
        else
        {
            Log.Debug($"Not correcting delay, text length: {text.Length}, tapSkip: {tapSkip}, tapWait: {tapWait}");
        }
        
        //Check if the label text setter was intercepted by XUAT
        balloon.textLabel.SetText(text);
        // ReSharper disable once RedundantCheckBeforeAssignment
        if (balloon.textLabel.text != text)
        {
            text = balloon.textLabel.text;
        }
        __instance.coroutineDic[__instance.CurrentCommandId] = __instance.coroutinePlayer.StartCoroutine(__instance.textBalloonDic[key].PlayCoroutine(text, charsPerSecond, tapSkip, tapWait, offsetX, offsetY, correctedDelay));
        __instance.coroutinePlayer.StartCoroutine(UpdateSpeechBubble(balloon).WrapToIl2Cpp());
        return false;
    }
    
    private static IEnumerator UpdateSpeechBubble(SpineDramaTextBalloon balloon)
    {
        var text = "";
        
        while (balloon.isActiveAndEnabled)
        {
            var newText = balloon.textLabel.text.Trim();
            if (text == "" && text != newText)
            {
                text = newText;
                continue;
            }
            
            if (newText != text)
            {
                text = newText;

                balloon.textLabel.ProcessText();
                var printedSize = balloon.textLabel.printedSize;
                printedSize.x += balloon.MARGIN + balloon.MARGIN;
                printedSize.y += balloon.MARGIN + balloon.MARGIN;
                printedSize.x = Mathf.Max(printedSize.x, balloon.MIN_SIZE.x);
                printedSize.y = Mathf.Max(printedSize.y, balloon.MIN_SIZE.y);
                balloon.balloonSprite.SetDimensions((int)printedSize.x, (int)printedSize.y);
                var pivot = balloon.balloonSprite.pivot;
                if (pivot == UIWidget.Pivot.Left)
                {
                    balloon.textLabel.transform.localPosition = new Vector3(balloon.MARGIN, printedSize.y / 2f - balloon.MARGIN);
                }
                else
                {
                    balloon.textLabel.transform.localPosition = new Vector3(-printedSize.x / 2f + balloon.MARGIN, printedSize.y - balloon.MARGIN);
                }
            }

            if (balloon.typewriterEffect.isActive)
            {
                balloon.typewriterEffect.Finish();
            }

            yield return null;
        }
    }
}