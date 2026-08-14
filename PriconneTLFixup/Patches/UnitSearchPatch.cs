using BepInEx;
using Elements;
using HarmonyLib;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(UnitSort), nameof(UnitSort.MatchSearchFilter))]
[HarmonyWrapSafe]
public class DictPatch
{
    private static readonly Dictionary<string, string[]> NameDictWithoutSpaces = new(StringComparer.Ordinal);
    private static readonly HashSet<string> MissingNameWarnings = new(StringComparer.Ordinal);
    private static readonly HashSet<string> FavoriteUnitNames = new(StringComparer.Ordinal);
    private static string? _cachedFilter;
    private static string _cachedLowerCaseFilter = string.Empty;
    private static string _cachedCompactFilter = string.Empty;
    private static int _favoriteCacheFrame = -1;

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
        if (!NameDict.TryGetValue(_source, out var enSpellings) ||
            !NameDictWithoutSpaces.TryGetValue(_source, out var compactSpellings))
        {
            WarnMissingName(_source, "UnitSort.MatchSearchFilter");
            __result = false;
            return true;
        }

        var (lowerCaseInput, compactInput) = NormalizeFilter(_filter);
        if (lowerCaseInput == "fav")
        {
            __result = IsFavorited(_source);
            return false;
        }
        
        var match = false;
        for (var i = 0; i < enSpellings.Length; i++)
        {
            if (enSpellings[i].StartsWith(lowerCaseInput, true, UnitDefine.UNIT_SEARCH_REMOVE_STRING, UnitDefine.UnitNameSearchSplitString))
            {
                match = true;
                break;
            } 
            
            if (compactSpellings[i].StartsWith(compactInput, true, UnitDefine.UNIT_SEARCH_REMOVE_STRING, UnitDefine.UnitNameSearchSplitString))
            {
                match = true;
                break;
            }
        }
       
        __result = match;
        return false;
    }
    
    private static bool IsFavorited(string unitName)
    {
        var currentFrame = Time.frameCount;
        if (_favoriteCacheFrame == currentFrame)
        {
            return FavoriteUnitNames.Contains(unitName);
        }

        FavoriteUnitNames.Clear();
        var unitDataDict = Singleton<UserData>.Instance.UnitParameterDictionary;
        foreach (var unitParam in unitDataDict._values)
        {
            if (unitParam.UniqueData.FavoriteFlag == 1)
            {
                FavoriteUnitNames.Add(unitParam.MasterData.UnitName);
            }
        }

        _favoriteCacheFrame = currentFrame;
        return FavoriteUnitNames.Contains(unitName);
    }
    
    internal static readonly Dictionary<string, string[]> NameDict = new(StringComparer.Ordinal);

    internal static (string LowerCase, string Compact) NormalizeFilter(string filter)
    {
        if (!string.Equals(_cachedFilter, filter, StringComparison.Ordinal))
        {
            _cachedFilter = filter;
            _cachedLowerCaseFilter = filter.ToLower();
            _cachedCompactFilter = _cachedLowerCaseFilter.Replace(" ", "");
        }

        return (_cachedLowerCaseFilter, _cachedCompactFilter);
    }

    internal static void WarnMissingName(string source, string caller)
    {
        if (MissingNameWarnings.Add(source))
        {
            Log.Warn($"{caller}: source not found in dictionary: {source}");
        }
    }

    private static void ReadUnitNameFile()
    {
        var language = AutoTranslatorSettings.DestinationLanguage;
        var DictPath = Path.Join(Paths.BepInExRootPath, "Translation", language ?? "en", "Other", "unit_names.txt");
        
        if (!File.Exists(DictPath))
        {
            Log.Error($"Unit name dictionary file not found: {DictPath}");
            return;
        }
        
        using var file = new StreamReader(DictPath);
        while (file.ReadLine() is { } ln)
        {
            var separatorIndex = ln.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            var jp = ln[..separatorIndex];
            var en = ln[(separatorIndex + 1)..];
            if (en.Length == 0)
            {
                continue;
            }

            var normalizedVariants = en.Split(';')
                .Where(enVariant => enVariant.Length > 0)
                .Select(enVariant => enVariant.ToLower())
                .Where(enVariant => enVariant != "christmas")
                .ToArray();

            NameDict[jp] = normalizedVariants;
            NameDictWithoutSpaces[jp] = normalizedVariants.Select(enVariant => enVariant.Replace(" ", "")).ToArray();
        }
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
