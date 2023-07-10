using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * Technically not a problem with the translation patch, but this allows to scale up the game window to full resolution.
 */
[HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.getOptimizedWindowSize))]
public class ResolutionPatch
{
    public static bool Prefix(ref Vector3 __result, int _width, int _height)
    {
        var resolution = Screen.currentResolution;
        var aspectRatio = 1.7777778f;
        var num = _width / (float)_height;
        if (num < aspectRatio)
        {
            _height = Mathf.Clamp(_height, (int)((float)resolution.height / 10), resolution.height);
            _width = Mathf.RoundToInt(_height * aspectRatio);
        }
        else if (num > aspectRatio)
        {
            _width = Mathf.Clamp(_width, (int)((float)resolution.width / 10), resolution.width);
            _height = Mathf.RoundToInt(_width / aspectRatio);
        }

        __result = new Vector3(_width, _height, num);
        return false;
    }
}

[HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.DisableMaximizebox))]
public class MaximizePatch
{
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(HeaderController), nameof(HeaderController.RestoreSlideIn))]
public class HeaderPatch
{
    public static bool Prefix(HeaderController __instance, float _time = 0.1f)
    {
        var tweenPosition = TweenPosition.Begin(__instance.slideObjTop, _time, Vector3.zero);
        CoroutineStarter.Instance.StartCoroutine(WaitForTween(tweenPosition).WrapToIl2Cpp());
        return false;
    }
    
    public static IEnumerator WaitForTween(TweenPosition tween)
    {
        while (tween.enabled)
        {
            yield return null;
        }

        Log.Debug("Tween finished");
        UIRoot.Broadcast("UpdateAnchors");
        var func = UICamera.onScreenResize;
        if (func == null) yield break;
        func.Invoke();
    } 
}