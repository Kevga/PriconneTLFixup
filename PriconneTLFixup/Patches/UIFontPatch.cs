using System;
using System.Text.RegularExpressions;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * Allows to setup base font and fonts for replacement.
 */
[HarmonyPatch(typeof(CustomUILabel), nameof(CustomUILabel.Awake))]
[HarmonyWrapSafe]
public class UIFontPatch
{
    private static bool _initialized = false;
    private static Font? _baseFont;
    private static string _baseFontName = "font_base";
    private static string _fontFolder = "BepInEx/Translation/en/Font/";
    private static string _labelFontPairsPath = "BepInEx/Translation/en/Text/_01.font.txt";
    private static Dictionary<string, string> _fontNameByLabel = new Dictionary<string, string>();
    private static Dictionary<string, Font?> _fontByName = new Dictionary<string, Font?>();

    private static void Prefix(CustomUILabel __instance)
    {
        if (!Settings.EnableFontReplacement.Value)
        {
            return;
        }
        
        if (!_initialized)
        {
            _baseFont = LoadFont(_baseFontName);
            if (!File.Exists(_labelFontPairsPath))
            {
                File.Create(_labelFontPairsPath).Close();
            }

            var labelFontPairs = File.ReadAllLines(_labelFontPairsPath);
            var uniqueFontNames = new List<string>();
            foreach (var pair in labelFontPairs.Where(x => x.Contains('=')))
            {
                string labelFullName = pair.Split('=')[0];
                string fontName = pair.Split("=")[1];
                _fontNameByLabel[SpecialRegexEscape(labelFullName)] = fontName;
                if (!uniqueFontNames.Contains(fontName))
                {
                    uniqueFontNames.Add(fontName);
                }
            }

            foreach (var fontName in uniqueFontNames)
            {
                Font? font = LoadFont(fontName);
                if (font != null)
                {
                    _fontByName[fontName] = font;
                }
            }

            _initialized = true;
            Log.Info("UIFontPatch is initialized");
        }

        var goFullName = GetFullName(__instance.gameObject);
        var label = _fontNameByLabel.Keys.FirstOrDefault(x => Regex.IsMatch(goFullName, x));
        if (label != null)
        {
            var fontName = _fontNameByLabel[label];
            // It can randomly unload it
            if (_fontByName[fontName] == null)
            {
                Log.Info("Reloading Font: " + fontName);
                _fontByName[fontName] = LoadFont(fontName);
            }
           
            __instance.trueTypeFont = _fontByName[fontName];
        }
        else
        {
            // It can randomly unload it
            if (_baseFont == null)
            {
                Log.Info("Reloading Font: " + _baseFontName);
                _baseFont = LoadFont(_baseFontName);
            }

            __instance.trueTypeFont = _baseFont;
        }
    }

    private static Font? LoadFont(string fontName)
    {
        var fontPath = _fontFolder + fontName + ".unity3d";
        if (!File.Exists(fontPath))
        {
            return null;
        }

        AssetBundle assetBundle = AssetBundle.LoadFromFile(fontPath);
        string[] allAssetNames = assetBundle.GetAllAssetNames();
        Font? font = null;
        foreach (string text in allAssetNames)
        {
            if (text.Contains("ttf"))
            {
                font = assetBundle.LoadAsset(text).Cast<Font>();
                break;
            }
        }

        assetBundle.Unload(false);
        return font;
    }

    /// <summary>
    /// Returns the full hierarchy name of the game object.
    /// </summary>
    /// <param name="go">The game object.</param>
    private static string GetFullName(GameObject go)
    {
        string name = go.name;
        while (go.transform.parent != null)
        {

            go = go.transform.parent.gameObject;
            name = go.name + "/" + name;
        }

        // "/" for XUAT path compatibility
        return "/" + name;
    }

    private static string SpecialRegexEscape(string text)
    {
        text = text.Replace("(", @"\(");
        text = text.Replace(")", @"\)");
        return text;
    }
}
