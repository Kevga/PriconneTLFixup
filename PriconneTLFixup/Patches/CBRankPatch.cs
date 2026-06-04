using Elements;
using HarmonyLib;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(PartsClanBattleRankingPlate), nameof(PartsClanBattleRankingPlate.SetRanking))]
public class CBRankPatch
{
    public static void Postfix(PartsClanBattleRankingPlate __instance, CustomUILabel _rankNumber,
        CustomUILabel _rankString, UISprite _rankBg, int _rank)
    {
        var suffix = Util.GetRankSuffix(_rank);
        _rankNumber.text = $"{_rank}{suffix}";

        var pos = _rankNumber.transform.localPosition;
        pos.x = -7.5f;
        _rankNumber.transform.localPosition = pos;
    }
}

[HarmonyPatch(typeof(PartsClanBattleBossRankingPlate), nameof(PartsClanBattleBossRankingPlate.setRanking))]
public class CBBossRankPatch
{
    public static void Postfix(PartsClanBattleBossRankingPlate __instance, CustomUILabel _rankNumber,
        CustomUILabel _rankString, UISprite _rankBg, int _rank)
    {
        var suffix = Util.GetRankSuffix(_rank);
        _rankNumber.text = $"{_rank}{suffix}";
        var pos = _rankNumber.transform.localPosition;
        pos.x = -2.5f;
        _rankNumber.transform.localPosition = pos;
    }
}

[HarmonyPatch(typeof(ViewClanBattleTop), nameof(ViewClanBattleTop.initialize))]
public class CBInitializeRankPatch
{
    public static void Postfix(ViewClanBattleTop __instance)
    {
        var clanBattleTempData = Singleton<ClanBattleTempData>.Instance;
        
        CBRankNumPatch.Postfix(__instance, clanBattleTempData.ClanBattleTopInfo.OwnRank, clanBattleTempData.ClanBattleTopInfo.PeriodRank, __instance.isAggregate);
    }
}

[HarmonyPatch(typeof(ViewClanBattleTop), nameof(ViewClanBattleTop.setRankNum))]
public class CBRankNumPatch
{
    public static void Postfix(ViewClanBattleTop __instance, int _rankInClan, int _rankAllClan, bool _isAggregate)
    {
        if (_isAggregate)
        {
            return;
        }
        
        if (_rankAllClan > 0)
        {
            var suffix = Util.GetRankSuffix(_rankAllClan);
            var textureNode = __instance.transform.Find("LeftNode/UIPanel/RankingNode/GUIGroup_Ranking/RankNoAll");
            var textNode = __instance.transform.Find("LeftNode/UIPanel/RankingNode/GUIGroup_Ranking/GUIGroup_Ranking_All/Label_exceptionAllClan");
            if (!textureNode || !textNode)
            {
                Log.Debug("RankNoAll not found");
                return;
            }

            textureNode.gameObject.SetActive(false);
            textNode.gameObject.SetActive(true);
            var label = textNode.gameObject.GetComponent<CustomUILabel>();
            label.text = $"{_rankAllClan}{suffix}";
        }

        if (_rankInClan > 0) // Clan rank
        {
            var suffix = Util.GetRankSuffix(_rankInClan);
            var textureNode = __instance.transform.Find("LeftNode/UIPanel/RankingNode/GUIGroup_Ranking/RankNoInClan");
            var textNode = __instance.transform.Find("LeftNode/UIPanel/RankingNode/GUIGroup_Ranking/GUIGroup_Ranking_Clan/Label_exceptionInClan");
            if (!textureNode || !textNode)
            {
                Log.Debug("RankNoInClan not found");
                return;
            }

            textureNode.gameObject.SetActive(false);
            textNode.gameObject.SetActive(true);
            var label = textNode.gameObject.GetComponent<CustomUILabel>();
            label.text = $"{_rankInClan}{suffix}";
        }
    }
}