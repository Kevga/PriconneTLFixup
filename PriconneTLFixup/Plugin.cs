using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace PriconneTLFixup;

[BepInPlugin("PriconneTLFixup", "PriconneTLFixup by Dakari", "1.2.0")]
[BepInProcess("PrincessConnectReDive.exe")]
public class Plugin: BasePlugin
{
    internal static ManualLogSource Logger = null!;
    private readonly HarmonyPatchController _harmonyController = new();
    
    public override void Load()
    {
        Logger = Log;
        _harmonyController.PatchAll();
        Logger.LogInfo("PriconneTLFixup loaded!");
    }
    
    public override bool Unload()
    {
        Logger.LogInfo("Shutting down...");
        _harmonyController.UnpatchSelf();
        return true;
    }
}