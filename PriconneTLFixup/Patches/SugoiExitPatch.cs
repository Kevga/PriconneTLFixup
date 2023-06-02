using System.Diagnostics;
using Cute;
using HarmonyLib;
using SugoiOfflineTranslator;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(SugoiOfflineTranslatorEndpoint), "StartProcess")]
public class SugoiExitPatch
{
    internal static Process? process;
    
    public static void Postfix(SugoiOfflineTranslatorEndpoint __instance, Process ___process)
    {
        Plugin.Logger.LogInfo("SugoiOfflineTranslatorEndpoint.StartProcess");
        process = ___process;
    }
}

[HarmonyPatch(typeof(Toolbox), nameof(Toolbox.ApplicationQuit))]
public class SugoiExitPatch2
{
    public static void Prefix()
    {
        Plugin.Logger.LogInfo("Toolbox.ApplicationQuit");
        SugoiExitPatch.process?.Kill();
    }
}