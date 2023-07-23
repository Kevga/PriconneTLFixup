using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace PriconneTLFixup;

[BepInPlugin("PriconneTLFixup", "PriconneTLFixup by Dakari", "1.2.0")]
[BepInProcess("PrincessConnectReDive.exe")]
public class Plugin: BasePlugin
{
    private readonly HarmonyPatchController _harmonyController = new();
    
    public override void Load()
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        } catch (Exception e)
        {
            PriconneTLFixup.Log.Error("Failed to set console encoding to UTF8. Japanese characters may not display correctly.");
            PriconneTLFixup.Log.Error(e);
        }

        PriconneTLFixup.Log.BieLogger = Log;
        _harmonyController.PatchAll();
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