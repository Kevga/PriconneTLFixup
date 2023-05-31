using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

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
        
        int key = __instance.intParse(__instance.nextParam());
        
        var balloon = __instance.textBalloonDic[key];
        balloon.balloonSprite.width = 500;
        balloon.textLabel.width = balloon.balloonSprite.width - 40;
        balloon.textLabel.overflowMethod = UILabel.Overflow.ResizeHeight;
        balloon.textLabel.spacingY = 6;
        
        string text = __instance.nextParam();
        int charsPerSecond = __instance.intParse(__instance.nextParam());
        bool tapSkip = __instance.boolParse(__instance.nextParam());
        bool tapWait = __instance.boolParse(__instance.nextParam());
        float offsetX = __instance.floatParse(__instance.nextParam());
        float offsetY = __instance.floatParse(__instance.nextParam());
        float fadeoutDelayOffset = __instance.floatParse(__instance.nextParam());
        if (!__instance.textBalloonDic.ContainsKey(key))
        {
            return false;
        }
        __instance.coroutineDic[__instance.CurrentCommandId] = __instance.coroutinePlayer.StartCoroutine(__instance.textBalloonDic[key].PlayCoroutine(text, charsPerSecond, tapSkip, tapWait, offsetX, offsetY, fadeoutDelayOffset));
        __instance.coroutinePlayer.StartCoroutine(UpdateSpeechBubble(balloon, text).WrapToIl2Cpp());
        return false;
    }
    
    private static IEnumerator UpdateSpeechBubble(SpineDramaTextBalloon balloon, string originalText)
    {
        var text = originalText;
        while (balloon.isActiveAndEnabled)
        {
            if (balloon.textLabel.text != text)
            {
                text = balloon.textLabel.text;

                if (text.Length < 2)
                {
                    yield return null;
                }

                balloon.textLabel.ProcessText();
                Vector2 printedSize = balloon.textLabel.printedSize;
                printedSize.x += balloon.MARGIN + balloon.MARGIN;
                printedSize.y += balloon.MARGIN + balloon.MARGIN;
                printedSize.x = Mathf.Max(printedSize.x, balloon.MIN_SIZE.x);
                printedSize.y = Mathf.Max(printedSize.y, balloon.MIN_SIZE.y);
                balloon.balloonSprite.SetDimensions((int)printedSize.x, (int)printedSize.y);
                UIWidget.Pivot pivot = balloon.balloonSprite.pivot;
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