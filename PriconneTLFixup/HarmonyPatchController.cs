using System.Reflection;
using HarmonyLib;

namespace PriconneTLFixup;

public class HarmonyPatchController
{
    private Dictionary<Type, PatchClassProcessor>? _patchClassProcessorList;
    private Harmony _harmonyInstance = null!;
    private readonly string _harmonyID;
    private readonly string _namespacePrefix;

    public HarmonyPatchController(string harmonyId, string namespacePrefix)
    {
        _harmonyID = harmonyId;
        _namespacePrefix = namespacePrefix;
    }
    
    private void InitPatches()
    {
        _harmonyInstance = new Harmony(_harmonyID);

        _patchClassProcessorList = new Dictionary<Type, PatchClassProcessor>();
        AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Do<Type>(type =>
            {
                if (!(type.FullName?.StartsWith(_namespacePrefix) ?? false) ||
                    (type.FullName?.Contains("+<") ?? true)) return;
                
                try
                {
                    Log.Debug("Creating class processor for " + type.FullName);
                    _patchClassProcessorList.Add(type, _harmonyInstance.CreateClassProcessor(type));  
                } catch (Exception e)
                {
                    Log.Error("Failed to create class processor for " + type.FullName);
                    Log.Error(e);
                }
            }
        );
    }

    internal void PatchAll()
    {
        InitPatches();

        if (!_patchClassProcessorList?.Any() ?? true)
        {
            Log.Error("No patches found");
            return;
        }
        
        foreach(var entry in _patchClassProcessorList!)
        {
            try
            {
                Log.Debug("Patching " + entry.Key.FullName);
                entry.Value.Patch();
            }
            catch (Exception e)
            {
                Log.Error("Failed to install patch " + entry.Key.FullName);
                Log.Error(e);
            }
        }
    }

    internal void UnpatchSelf()
    {
        _harmonyInstance.UnpatchSelf();
    }
}