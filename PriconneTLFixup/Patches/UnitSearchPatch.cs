using System.Collections;
using BepInEx;
using Cute;
using Elements;
using HarmonyLib;
using PriconneTLFixup;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(UnitSort), nameof(UnitSort.MatchSearchFilter))]
[HarmonyWrapSafe]
public class DictPatch
{
    private static readonly string DictPath = Path.Join(Paths.BepInExRootPath, "Translation", "en", "Other", "unit_names.txt");

    public static void Prepare()
    {
        //For some reason Prepare is called twice
        if (NameDict.Count > 0)
        {
            return;
        }
        
        ReadUnitNameFile();
        Log.Info("Loaded names for " + NameDict.Count + " characters.");
    }
    
    public static bool Prefix(ref bool __result, string _source, string _filter)
    {
        var found = NameDict.TryGetValue(_source, out var enSpellings);
        if (!found || enSpellings == null)
        {
            Log.Warn($"UnitSort.MatchSearchFilter: {nameof(_source)} not found in dictionary: {_source}");
            __result = false;
            return true;
        }

        var lowerCaseInput = _filter.ToLower();
        var match = false;
        for (var i = 0; i < enSpellings.Length; i++)
        {
            var enSpelling = enSpellings[i];
            if (enSpelling.ToLower().StartsWith(lowerCaseInput, true, UnitDefine.UNIT_SEARCH_REMOVE_STRING, UnitDefine.UnitNameSearchSplitString))
            {
                match = true;
                break;
            }
        }
       
        __result = match;
        return false;
    }
    
    internal static readonly Dictionary<string, string[]> NameDict = new();

    private static void ReadUnitNameFile()
    {
        if (!File.Exists(DictPath))
        {
            Log.Error($"Unit name dictionary file not found: {DictPath}");
            return;
        }
        
        var file = new StreamReader(DictPath);
        while (file.ReadLine() is { } ln)
        {
            var parts = ln.Split("=");
            if (parts.Length != 2)
            {
                continue;
            }

            var jp = parts[0];
            var en = parts[1];
            if (en.Length == 0)
            {
                continue;
            }

            var enVariants = en.Split(";");

            NameDict[jp] = enVariants.Where(enVariant => enVariant.Length > 0).ToArray();
        }

        file.Close();
    }
}

[HarmonyPatch(typeof(SearchUnitNamePlate), nameof(SearchUnitNamePlate.Initialize))]
public class AutoFocusSearchPatch
{
    public static void Postfix(SearchUnitNamePlate __instance)
    {
        __instance.input.OnSelect(true);
    }
}