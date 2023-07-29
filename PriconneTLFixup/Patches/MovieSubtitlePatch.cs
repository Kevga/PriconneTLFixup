using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * This forces on subtitles on movies, even if they were downloaded with audio. Usually subs are hidden in that case.
 * Additionally, when a movie starts, XUAT is used to translate the subtitles before they are even displayed.
 */

[HarmonyPatch(typeof(MovieManager), nameof(MovieManager.dispSubTitle))]
[HarmonyWrapSafe]
public class MovieDisplaySubtitlePatch
{
    public static void Postfix(MovieManager __instance, MovieManager.MoviePlayerInfo _playerInfo)
    {
        if (__instance.subtitle == null || !__instance.subtitle.IsShow)
        {
            return;
        }
        
        __instance.disposeSubTitleCoroutine();
        __instance.subTitleCoroutine = __instance.showSubtitle(_playerInfo.Handle);
        __instance.StartCoroutine(__instance.subTitleCoroutine);
    }
}

[HarmonyPatch(typeof(MovieManager), nameof(MovieManager.Load), new []{typeof(eMovieType), typeof(long), typeof(bool), typeof(Il2CppSystem.Action), typeof(bool), typeof(long), typeof(bool), typeof(bool), typeof(bool), typeof(float), typeof(float), typeof(int)})]
[HarmonyWrapSafe]
public class MovieLoadSubtitlePatch
{
    public static void Prefix(MovieManager __instance, ref bool _isShowSubtitle, eMovieType _movieType)
    {
        var movieTypesWithSubs = new HashSet<eMovieType>
        {
            eMovieType.EVENT,
            eMovieType.STORY,
            eMovieType.WAC,
            eMovieType.MAX,
        };
        if (!movieTypesWithSubs.Contains(_movieType))
        {
            return;
        }
        
        Log.Debug("Showing subtitles for movie type: " + _movieType);
        _isShowSubtitle = true;
    }
}

[HarmonyPatch(typeof(SubtitleManager), nameof(SubtitleManager.Initialize))]
[HarmonyWrapSafe]
public class SubtitlePretranslationPatch
{
    internal static CustomUILabel? PretranslationLabel;
    public static void Postfix(SubtitleManager __instance, long _movieId)
    {
        if (__instance.data == null || __instance.data.Count == 0)
        {
            Log.Warn("SubtitleManager.Initialize: data is null or empty");
            return;
        }
        if (PretranslationLabel == null)
        {
            var pretranslationGameObject = new GameObject();
            pretranslationGameObject.transform.localPosition = new Vector3(0, -1000, 0);
            PretranslationLabel = pretranslationGameObject.AddComponent<CustomUILabel>();
            Log.Info("Created pre-translation label");
        }
        
        CoroutineStarter.Instance.StartCoroutine(PretranslationCoroutine(__instance).WrapToIl2Cpp());
    }
    
    private static IEnumerator PretranslationCoroutine(SubtitleManager manager)
    {
        const int stallTime = 3100;
        Log.Debug("Starting pretranslation of subtitles");
        yield return null;

        var recordList = manager.data.recordList.ToArray();

        var i = 0;
        var currentText = "";
        var lastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        while (PretranslationLabel != null && manager.data != null && i < manager.data.Count)
        {
            //Wait for XUAT to translate and update the label before continuing. If not updated within stallTime, continue anyway.
            if (currentText != "" && currentText == PretranslationLabel.text && ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - lastUpdate < stallTime))
            {
                yield return null;
                continue;
            }

            //Load the next subtitle line
            var record = recordList[i];
            if (record?.text == null)
            {
                yield return null;
                continue;
            }
            
            lastUpdate = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            currentText = record.text;
            PretranslationLabel.SetText(currentText);
            i++;
            
            //rate limit sub translation requests
            yield return new WaitForSecondsRealtime(Util.GetTranslationDelayInSeconds()); 
        }
        
        Log.Debug("Pretranslation finished");
        if (PretranslationLabel != null)
        {
            PretranslationLabel.SetText("");
        }
    }
}