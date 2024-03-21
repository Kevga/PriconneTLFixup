using System.Diagnostics;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup;

[BepInPlugin("PriconneTLFixup", "PriconneTLFixup by Dakari", "1.4.3")]
[BepInProcess("PrincessConnectReDive.exe")]
public class Plugin: BasePlugin
{
    private readonly HarmonyPatchController _harmonyController = new("com.github.kevga.priconnetlfixup", "PriconneTLFixup.Patches");
    internal static AutoTranslationPlugin? AutoTranslatorPlugin;
    
    public override void Load()
    {
        PriconneTLFixup.Log.BieLogger = Log;
        Settings.Init(Config);
        
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        } 
        catch (Exception e)
        {
            PriconneTLFixup.Log.Warn("Failed to set console encoding to UTF8. Japanese characters may not display correctly.");
            PriconneTLFixup.Log.Warn(e);
        }

        var sw = Stopwatch.StartNew();
        _harmonyController.PatchAll();
        sw.Stop();
        PriconneTLFixup.Log.Debug($"Patching took {sw.ElapsedMilliseconds}ms");
        
        PriconneTLFixup.Log.Info("PriconneTLFixup loaded!");
        PriconneTLFixup.Log.Info("If you want to contribute to this project, please visit:");
        PriconneTLFixup.Log.Info("https://github.com/Kevga/PriconneTLFixup");
    }
    
    public override bool Unload()
    {
        PriconneTLFixup.Log.Info("Shutting down...");
        _harmonyController.UnpatchSelf();
        return true;
    }
}
