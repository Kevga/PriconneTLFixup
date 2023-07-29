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
        var enpoint = Plugin.AutoTranslatorPlugin.TranslationManager.CurrentEndpoint;
        Log.Info($"Current endpoint: {enpoint.Endpoint.Id}");
        Log.Info($"Delay: {enpoint.TranslationDelay}");
    }
}