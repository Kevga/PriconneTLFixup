using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;

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