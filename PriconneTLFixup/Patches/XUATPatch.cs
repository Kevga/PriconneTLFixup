using Cute;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(AutoTranslationPlugin), nameof(AutoTranslationPlugin.Initialize))]
[HarmonyWrapSafe]
public class XUATInstancePatch
{
    public static void Postfix(AutoTranslationPlugin __instance)
    {
        Plugin.AutoTranslatorPlugin = __instance;
        
        if (__instance.TranslationManager == null || __instance.TranslationManager.CurrentEndpoint == null)
        {
            Log.Info("No XUAT endpoint set");
            return;
        }
        
        var enpoint = Plugin.AutoTranslatorPlugin?.TranslationManager?.CurrentEndpoint;
        Log.Info($"Current endpoint: {enpoint?.Endpoint.Id}");
        Log.Info($"Delay: {enpoint?.TranslationDelay}");
    }
}