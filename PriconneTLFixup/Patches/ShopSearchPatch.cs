using Elements;
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

//Only works for master shop. Has the side effect that the placeholder vanishes after switching tabs. Not ideal.
/*[HarmonyPatch(typeof(PartsShopFooter), nameof(PartsShopFooter.SetTab))]
[HarmonyWrapSafe]
public class ShopSearchFocusPatch
{
    public static void Postfix(PartsShopFooter __instance)
    {
        if (!__instance.itemSearchGroupOrDefault.ItemNameInput.selected)
        {
            __instance.itemSearchGroupOrDefault.ItemNameInput.OnSelect(true);    
        }
    }
}*/

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