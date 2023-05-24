using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PriconneTLFixup;

[BepInPlugin("PriconneTLFixup", "PriconneTLFixup by Dakari", "1.0.0")]
[BepInProcess("PrincessConnectReDive.exe")]
public class Plugin: BasePlugin
{
    internal static ManualLogSource Logger = null!;
    private readonly Harmony _harmony = new("PriconneTLFixup");
    
    public override void Load()
    {
        Logger = Log;
        _harmony.PatchAll();
        Logger.LogInfo("PriconneTLFixup loaded!");
    }
    
    public override bool Unload()
    {
        _harmony.UnpatchSelf();
        return true;
    }
}