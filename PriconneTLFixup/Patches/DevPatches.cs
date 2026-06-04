using Elements;
using HarmonyLib;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(LoadIndexReceiveParam), nameof(LoadIndexReceiveParam.ParseLoadIndexReceiveParam))]
[HarmonyWrapSafe]
public class BdayErryday
{
    public static void Postfix(ref LoadIndexReceiveParam __instance)
    {
        if (!Settings.BirthdayToggle.Value)
        {
            return;
        }
        
        string fullDate = DateTime.Now.ToString("yyyyMMdd");
        __instance.UserInfo.UserBirth = int.Parse(fullDate);
        
        string monthDay = DateTime.Now.ToString("MMdd");
        __instance.Voice.Birthday = int.Parse(monthDay);
        
    }
}