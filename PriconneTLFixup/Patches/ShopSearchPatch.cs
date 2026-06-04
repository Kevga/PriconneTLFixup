using Elements;
using Elements.Data;
using HarmonyLib;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(PartsShopFooter), nameof(PartsShopFooter._MaterialFilterBySearchText_b__109_0))]
[HarmonyWrapSafe]
public class ShopSearchPatch
{
    public static bool Prefix(PartsShopFooter __instance, ShopItem item, ref bool __result)
    {
        if (item.Type != eInventoryType.Item)
        {
            __result = false;
            return false;
        }
        
        var source = __instance.masterDataManager.masterItemData.Get(item.ItemId)?.MaterialSearchText;
        if (source == null)
        {
            __result = false;
            return false;
        }
        
        var filteredSource = source.Replace("のメモリーピース", "").Replace("のピュアメモリーピース", "");
        if (filteredSource.IsNullOrEmpty())
        {
            __result = false;
            return false;
        }
        
        var found = DictPatch.NameDict.TryGetValue(filteredSource, out var enSpellings);
        if (!found || enSpellings == null)
        {
            Log.Warn($"UnitSort.MatchSearchFilter: {nameof(source)} not found in dictionary: {filteredSource}");
            __result = false;
            return true;
        }

        var lowerCaseInput = __instance.LastMaterialSearchText.ToLower();
        var match = false;
        for (var i = 0; i < enSpellings.Length; i++)
        {
            var enSpelling = enSpellings[i];
            if (enSpelling.ToLower().StartsWith(lowerCaseInput, true, null, UnitDefine.UnitNameSearchSplitString))
            {
                match = true;
                break;
            }
        }
       
        __result = match;
        return false;
    }
}

[HarmonyPatch(typeof(PartsDialogItemSelect), nameof(PartsDialogItemSelect.searchItemExec))]
[HarmonyWrapSafe]
public class ItemSelectSearchPatch
{
    private static bool MatchesEnglishSearch(MasterItemData masterItemData, int itemId, string lowerCaseSearch)
    {
        var source = masterItemData.Get(itemId)?.MaterialSearchText;
        if (source == null) return false;

        var filteredSource = source.Replace("のメモリーピース", "").Replace("のピュアメモリーピース", "");
        if (filteredSource.IsNullOrEmpty()) return false;

        if (!DictPatch.NameDict.TryGetValue(filteredSource, out var enSpellings) || enSpellings == null)
        {
            Log.Warn($"ItemSelectSearchPatch: source not found in dictionary: {filteredSource}");
            return false;
        }

        for (var i = 0; i < enSpellings.Length; i++)
        {
            if (enSpellings[i].ToLower().StartsWith(lowerCaseSearch, true, null, UnitDefine.UnitNameSearchSplitString))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Prefix(PartsDialogItemSelect __instance)
    {
        __instance.searchItemList.Clear();
        var lowerCaseSearch = __instance.searchWord.ToLower();

        switch (__instance.tabIndex)
        {
            case PartsDialogItemSelect.eTabIndex.MEMORY_PIECE:
            {
                __instance.sortMaterial();
                MasterItemData masterItemData = __instance.masterDataMgr.masterItemData;
                for (int j = 0; j < __instance.dispMaterialParamList.Count; j++)
                {
                    int itemId = __instance.dispMaterialParamList[j].ItemId;
                    if (MatchesEnglishSearch(masterItemData, itemId, lowerCaseSearch))
                    {
                        __instance.searchItemList.Add(__instance.dispMaterialParamList[j]);
                    }
                }
                __instance.dispMaterialParamList = __instance.searchItemList;
                return false;
            }
            case PartsDialogItemSelect.eTabIndex.SUPER_MEMORY_PIECE:
            {
                __instance.sortSuperMaterial();
                MasterItemData masterItemData = __instance.masterDataMgr.masterItemData;
                for (int k = 0; k < __instance.dispSuperMaterialParamList.Count; k++)
                {
                    int itemId = __instance.dispSuperMaterialParamList[k].ItemId;
                    if (MatchesEnglishSearch(masterItemData, itemId, lowerCaseSearch))
                    {
                        __instance.searchItemList.Add(__instance.dispSuperMaterialParamList[k]);
                    }
                }
                __instance.dispSuperMaterialParamList = __instance.searchItemList;
                return false;
            }
            default:
                return true;
        }
    }
}

//This function filters out the parentheses from unit names.
//However, our dictionary contains them, so we return the original string.
[HarmonyPatch(typeof(ShopUtility), nameof(ShopUtility.GetMaterialSearchTextFormat))]
[HarmonyWrapSafe]
public class ShopSearchFormatPatch
{
    public static bool Prefix(string _source, ref string __result)
    {
        __result = _source;
        return false;
    }
}