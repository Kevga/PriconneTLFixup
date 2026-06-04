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
        Log.Debug("dispSubTitle: " + _playerInfo.State);
        if (__instance.subtitle == null || !__instance.subtitle.IsShow)
        {
            Log.Debug("dispSubTitle: Subtitle not available");
            return;
        }
        
        __instance.disposeSubTitleCoroutine();
        __instance.subTitleCoroutine = __instance.showSubtitle(_playerInfo.Handle);
        __instance.StartCoroutine(__instance.subTitleCoroutine);
    }
}

[HarmonyPatch(typeof(MovieManager), nameof(MovieManager.Load), typeof(eMovieType), typeof(long), typeof(bool), typeof(Il2CppSystem.Action), typeof(bool), typeof(long), typeof(bool), typeof(bool), typeof(bool), typeof(float), typeof(float), typeof(int))]
[HarmonyWrapSafe]
public class MovieLoadSubtitlePatch
{
    public static void Prefix(MovieManager __instance, long _movieId, eMovieType _movieType)
    {
        if (__instance.subtitle != null)
        {
            if (_movieType != eMovieType.EVENT && _movieType != eMovieType.STORY && _movieType != eMovieType.NONE)
            {
                Log.Info("Not showing subtitles for movie type: " + _movieType);
                return;
            }
            
            Log.Info("Showing subtitles for movie type: " + _movieType);
            __instance.subtitle.Initialize(_movieId);
            __instance.subtitle.IsShow = true;
        }
        else
        {
            Log.Debug("SubtitleManager not found");
        }
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
            PretranslationLabel.name = "SubtitlePretranslationLabel";
            Log.Info("Created pre-translation label");
        }
        else
        {
            Log.Debug("Pre-translation label already exists");
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
            yield return new WaitForSecondsRealtime(Util.GetTranslationDelayInSeconds().HasValue ? Util.GetTranslationDelayInSeconds()!.Value : 2.5f); 
        }
        
        Log.Debug("Subtitle pretranslation finished");
        if (PretranslationLabel != null)
        {
            PretranslationLabel.SetText("");
        }
    }
}