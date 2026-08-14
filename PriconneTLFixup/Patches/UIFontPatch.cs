using System;
using System.Text.RegularExpressions;
using System.Text;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

/**
 * Allows to setup base font and fonts for replacement.
 */
[HarmonyPatch(typeof(CustomUILabel), nameof(CustomUILabel.Awake))]
[HarmonyWrapSafe]
public class UIFontPatch
{
    private static bool _initialized;
    private static Font? _baseFont;
    private const string BaseFontName = "font_base";
    private static readonly string FontFolder = Path.Join(BepInEx.Paths.BepInExRootPath, "Translation", AutoTranslatorSettings.DestinationLanguage ?? "en", "Font");
    private static readonly string LabelFontPairsPath = Path.Join(BepInEx.Paths.BepInExRootPath, "Translation", AutoTranslatorSettings.DestinationLanguage ?? "en", "Text", "_01.font.txt");
    private static readonly Dictionary<string, Font?> FontByName = new(StringComparer.Ordinal);
    private static readonly List<(Regex Pattern, string FontName)> FontRules = new();

    private static void Prefix(CustomUILabel __instance)
    {
        if (!Settings.EnableFontReplacement.Value || !Util.IsTranslationEnabled())
        {
            return;
        }

        if (!_initialized)
        {
            Log.Debug("Initializing UIFontPatch");
            _baseFont = LoadFont(BaseFontName);
            if (!File.Exists(LabelFontPairsPath))
            {
                File.Create(LabelFontPairsPath).Close();
            }

            var fontNameByPattern = new Dictionary<string, string>(StringComparer.Ordinal);
            var uniqueFontNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in File.ReadLines(LabelFontPairsPath))
            {
                var separatorIndex = pair.IndexOf('=');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var labelFullName = pair[..separatorIndex];
                var fontName = pair[(separatorIndex + 1)..];
                fontNameByPattern[SpecialRegexEscape(labelFullName)] = fontName;
                uniqueFontNames.Add(fontName);
            }

            foreach (var (pattern, fontName) in fontNameByPattern)
            {
                FontRules.Add((new Regex(pattern, RegexOptions.Compiled), fontName));
            }

            Log.Debug($"Found {FontRules.Count} label-font pairs for UIFontPatch");

            foreach (var fontName in uniqueFontNames)
            {
                Log.Debug("Loading Font: " + fontName);
                Font? font = LoadFont(fontName);
                if (font != null)
                {
                    FontByName[fontName] = font;
                }
                else
                {
                    Log.Warn("Could not load font: " + fontName);
                }
            }

            _initialized = true;
            Log.Info("UIFontPatch is initialized");
        }

        string? replacementFontName = null;
        if (FontRules.Count > 0)
        {
            var goFullName = GetFullName(__instance.gameObject);
            foreach (var rule in FontRules)
            {
                if (rule.Pattern.IsMatch(goFullName))
                {
                    replacementFontName = rule.FontName;
                    break;
                }
            }
        }

        if (replacementFontName != null)
        {
            // It can randomly unload it
            if (!FontByName.TryGetValue(replacementFontName, out var font) || font == null)
            {
                Log.Info("Reloading Font: " + replacementFontName);
                font = LoadFont(replacementFontName);
                FontByName[replacementFontName] = font;
            }

            __instance.trueTypeFont = font;
        }
        else
        {
            // It can randomly unload it
            if (_baseFont == null)
            {
                Log.Info("Reloading Font: " + BaseFontName);
                _baseFont = LoadFont(BaseFontName);
            }

            __instance.trueTypeFont = _baseFont;
        }
    }

    private static Font? LoadFont(string fontName)
    {
        Font? font = null;
        AssetBundle assetBundle = null!;
        var fontPath = Path.Join(FontFolder, fontName + ".unity3d");
        if (!File.Exists(fontPath))
        {
            Log.Warn("Font file does not exist: " + fontPath);
            return null;
        }

        try
        {
            Log.Debug("Loading font from: " + fontPath);
            assetBundle = AssetBundle.LoadFromFile(fontPath);
            Log.Debug("Loaded asset bundle: " + assetBundle.name);
        }
        catch (Exception e)
        {
            Log.Error("Error loading asset bundle: " + e.Message);
            return null;
        }

        try

        {
            string[] allAssetNames = assetBundle.GetAllAssetNames();
            Log.Debug("Assets in bundle: " + string.Join(", ", allAssetNames));
            foreach (string text in allAssetNames)
            {
                if (text.Contains("ttf"))
                {
                    Log.Debug("Loading font asset: " + text);
                    font = assetBundle.LoadAsset(text, Il2CppType.Of<Font>()).Cast<Font>();
                    Log.Debug("Loaded font asset: " + font.name);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("Error loading font asset: " + e.Message);
            assetBundle.Unload(false);
            return null;
        }
        finally
        {
            Log.Debug("Unloading asset bundle");
            assetBundle.Unload(false);
            Log.Debug("Unloaded asset bundle");
        }

        return font;
    }

    /// <summary>
    /// Returns the full hierarchy name of the game object.
    /// </summary>
    /// <param name="go">The game object.</param>
    private static string GetFullName(GameObject go)
    {
        var path = new StringBuilder();
        AppendFullName(path, go.transform);
        return path.ToString();
    }

    private static void AppendFullName(StringBuilder path, Transform transform)
    {
        var parent = transform.parent;
        if (parent != null)
        {
            AppendFullName(path, parent);
        }

        path.Append('/').Append(transform.gameObject.name);
    }

    private static string SpecialRegexEscape(string text)
    {
        text = text.Replace("(", @"\(");
        text = text.Replace(")", @"\)");
        return text;
    }
}
