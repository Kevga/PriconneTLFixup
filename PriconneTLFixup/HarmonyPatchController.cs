using System.Reflection;
using HarmonyLib;

namespace PriconneTLFixup;

public class HarmonyPatchController
{
    private List<PatchClassProcessor>? _patchClassProcessorList;
    private Harmony _harmonyInstance = null!;
    private const string HarmonyID = "com.github.kevga.priconnetlfixup";

    private void InitPatches()
    {
        _harmonyInstance = new Harmony(HarmonyID);

        _patchClassProcessorList = new List<PatchClassProcessor>();
        AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Do<Type>(type =>
            {
                if (type.FullName?.StartsWith("PriconneTLFixup.Patches") ?? false)
                {
                    try
                    {
                        _patchClassProcessorList.Add(_harmonyInstance.CreateClassProcessor(type));  
                    } catch (Exception e)
                    {
                        Plugin.Logger.LogError(e);
                    }
                    
                }
            }
        );
    }

    internal void PatchAll()
    {
        InitPatches();

        _patchClassProcessorList?.ForEach(patchClassProcessor =>
        {
            try
            {
                patchClassProcessor.Patch();
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError(e);
            }
        });
    }

    internal void UnpatchSelf()
    {
        _harmonyInstance.UnpatchSelf();
    }
}