using System.Diagnostics;
using Cute;
using HarmonyLib;
using SugoiOfflineTranslator;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(SugoiOfflineTranslatorEndpoint), "StartProcess")]
public class SugoiExitPatch
{
    internal static Process? process;
    
    public static void Postfix(Process ___process)
    {
        Log.Info("SugoiOfflineTranslatorEndpoint.StartProcess");
        process = ___process;
    }
}

[HarmonyPatch(typeof(Toolbox), nameof(Toolbox.ApplicationQuit))]
public class SugoiExitPatch2
{
    public static void Prefix()
    {
        Log.Info("Application quitting");
        SugoiExitPatch.process?.Kill();
        SugoiExitPatch.process = null;
    }
}