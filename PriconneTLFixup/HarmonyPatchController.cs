using System.Reflection;
using HarmonyLib;

namespace PriconneTLFixup;

public class HarmonyPatchController
{
    private Dictionary<Type, PatchClassProcessor>? _patchClassProcessorList;
    private Harmony _harmonyInstance = null!;
    private const string HarmonyID = "com.github.kevga.priconnetlfixup";

    private void InitPatches()
    {
        _harmonyInstance = new Harmony(HarmonyID);

        _patchClassProcessorList = new Dictionary<Type, PatchClassProcessor>();
        AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Do<Type>(type =>
            {
                if (type.FullName?.StartsWith("PriconneTLFixup.Patches") ?? false)
                {
                    try
                    {
                        _patchClassProcessorList.Add(type, _harmonyInstance.CreateClassProcessor(type));  
                    } catch (Exception e)
                    {
                        Plugin.Logger.LogError("Failed to create class processor for " + type.FullName);
                        Plugin.Logger.LogError(e);
                    }
                    
                }
            }
        );
    }

    internal void PatchAll()
    {
        InitPatches();

        if (!_patchClassProcessorList?.Any() ?? true)
        {
            Plugin.Logger.LogError("No patches found");
            return;
        }
        
        foreach(var entry in _patchClassProcessorList!)
        {
            try
            {
                entry.Value.Patch();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError("Failed to install patch " + entry.Key.FullName);
                Plugin.Logger.LogError(e);
            }
        }
    }

    internal void UnpatchSelf()
    {
        _harmonyInstance.UnpatchSelf();
    }
}