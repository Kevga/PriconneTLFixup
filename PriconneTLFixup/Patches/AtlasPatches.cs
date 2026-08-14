using System.Diagnostics;
using BepInEx;
using Cute;
using HarmonyLib;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(BootApp), nameof(BootApp.Start))]
[HarmonyWrapSafe]
public class AtlasInitPatch
{
    public static readonly Dictionary<string, UIAtlas> Atlases = new(StringComparer.Ordinal);
    public static readonly Dictionary<string, UIAtlas> OriginalAtlases = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, HashSet<string>> SpriteNamesByAtlas = new(StringComparer.Ordinal);
    internal const string NameSuffix = " (Fixup)";

    public static void Postfix()
    {
        string[] jsonFiles;
        var language = AutoTranslatorSettings.DestinationLanguage;
        var atlasPath = Path.Join(Paths.BepInExRootPath, "Translation", language ?? "en", "Other", "atlases");
        
        try
        {
            jsonFiles = Directory.GetFiles(atlasPath, "*.json");
        } catch (System.Exception)
        {
            Log.Error("AtlasInitPatch: atlasPath not found: " + atlasPath);
            return;
        }

        if (jsonFiles.Length == 0)
        {
            Log.Error("No atlases found in " + atlasPath);
            return;
        }
        
        var shader = Shader.Find("Cygames/Unlit/Transparent Colored");
        if (shader == null)
        {
            Log.Error("Shader not found!");
            return;
        }

        //The json files are serialized MonoBehaviours. Create a gameobject and attach one monobehaviour per file.
        //Then, deserialize the json into the monobehaviour, overwriting the properties.

        var imageFiles = Directory.EnumerateFiles(atlasPath, "*.png").Select(Path.GetFileNameWithoutExtension)
            .ToList();
        if (!imageFiles.Any())
        {
            Log.Error("No pngs found in " + atlasPath);
            return;
        }

        foreach (var f in imageFiles) 
        {
            Log.Debug("Found file " + f);
        }
        
        var allAtlasesSw = Stopwatch.StartNew();

        foreach (var jsonFile in jsonFiles)
        {
            var atlasName = Path.GetFileNameWithoutExtension(jsonFile);
            var matches = imageFiles.Where(f => f != null && MatchesAtlasName(f, atlasName));
            var matchList = matches.ToList();
            if (!matchList.Any() || matchList[0] == null)
            {
                Log.Error("No png found for " + atlasName + " (json: " + jsonFile + ")");
                continue;
            }
            
            var textureFileName = matchList[0];
            var texturePath = Path.Join(atlasPath, textureFileName+".png");

            if (matchList.Count > 1)
            {
                Log.Debug("Multiple pngs found for " + jsonFile);
                
                //Select file that has the most recent "modified" date
                var mostRecentFile = matchList[0];
                var mostRecentDate = File.GetLastWriteTime(Path.Join(atlasPath, mostRecentFile + ".png"));
                foreach (var match in matchList)
                {
                    Log.Debug("Checking " + match);
                    var date = File.GetLastWriteTime(Path.Join(atlasPath, match + ".png"));
                    if (date > mostRecentDate)
                    {
                        mostRecentFile = match;
                        mostRecentDate = date;
                    }
                }
                
                textureFileName = mostRecentFile;
                texturePath = Path.Join(atlasPath, textureFileName+".png");
                Log.Debug("Selected " + textureFileName + " as the most recent file");
            }

            if (!File.Exists(texturePath))
            {
                Log.Error("No png found for " + jsonFile + ": " + texturePath);
                continue;
            }

            var totalSw = Stopwatch.StartNew();
            var json = File.ReadAllText(jsonFile);
            var atlasGO = new GameObject
            {
                name = atlasName + NameSuffix
            };
            UnityEngine.Object.DontDestroyOnLoad(atlasGO);
            var atlas = atlasGO.AddComponent<UIAtlas>();
            atlas.name = atlasName + NameSuffix;
            JsonUtility.FromJsonInternal(json, atlas, atlas.GetIl2CppType());
            var atlasSize = GetAtlasSize(atlas, out var spriteNames);
            Log.Debug("Atlas " + atlas.name + " size calculated as " + atlasSize + "x" + atlasSize);
            
            var textureData = File.ReadAllBytes(texturePath);
            var texture = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);

            var material = new Material(shader);
            material.renderQueue = 3054;
            atlas.material = material;
            material.name = atlasName + NameSuffix;
            material.mainTexture = texture;
            texture.requestedMipmapLevel = 0;
            texture.filterMode = FilterMode.Trilinear;
            // Replacement atlases are never read from the CPU after loading. Releasing the
            // CPU-side copy avoids retaining another full uncompressed copy of every atlas.
            texture.LoadImage(textureData, true);
            texture.name = atlasName + NameSuffix;

            Log.Debug("Loaded atlas " + atlas.name + " (" + texture.width + "x" + texture.height + ") in " +
                      totalSw.ElapsedMilliseconds + "ms");
            Atlases.Add(atlasName, atlas);
            SpriteNamesByAtlas.Add(atlasName, spriteNames);
            totalSw.Stop();
        }

        Log.Info("Loaded " + Atlases.Count + " atlases. Took " + allAtlasesSw.ElapsedMilliseconds + "ms");
    }

    private static bool MatchesAtlasName(string imageFileName, string atlasName)
    {
        if (string.Equals(imageFileName, atlasName, StringComparison.Ordinal))
        {
            return true;
        }

        // Optional XUAT hash suffix: " [0123456789ABCDEF]" (10 hex characters).
        if (imageFileName.Length != atlasName.Length + 13 ||
            !imageFileName.StartsWith(atlasName, StringComparison.Ordinal) ||
            !char.IsWhiteSpace(imageFileName[atlasName.Length]) ||
            imageFileName[atlasName.Length + 1] != '[' ||
            imageFileName[^1] != ']')
        {
            return false;
        }

        for (var i = atlasName.Length + 2; i < imageFileName.Length - 1; i++)
        {
            var c = imageFileName[i];
            if (c is not (>= '0' and <= '9') && c is not (>= 'A' and <= 'F'))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool ContainsSprite(string atlasName, string spriteName)
    {
        return SpriteNamesByAtlas.TryGetValue(atlasName, out var spriteNames) && spriteNames.Contains(spriteName);
    }

    private static int GetAtlasSize(UIAtlas atlas, out HashSet<string> spriteNames)
    {
        var maxX = 0;
        var maxY = 0;
        spriteNames = new HashSet<string>(StringComparer.Ordinal);
        
        foreach (var sprite in atlas.spriteList)
        {
            if (sprite.name != null)
            {
                spriteNames.Add(sprite.name);
            }

            var x = sprite.x + sprite.width;
            var y = sprite.y + sprite.height;
            if (x > maxX)
            {
                maxX = x;
            }

            if (y > maxY)
            {
                maxY = y;
            }
        }
        
        //Round to nearest power of two that is equal or larger than maxX or maxY
        var size = Mathf.Max(maxX, maxY);
        var powerOfTwo = Mathf.NextPowerOfTwo(size);
        return powerOfTwo;
    }
}

[HarmonyPatch(typeof(UISprite), nameof(UISprite.OnInit))]
[HarmonyPriority(Priority.Last)]
[HarmonyWrapSafe]
public class WidgetPatch
{
    private static readonly HashSet<string> DisallowedSpriteNames = new(StringComparer.Ordinal)
    {
        "common_tab_selected",
        "common_iframe_bg",
        "common_scroll_bar",
        "common_scroll_bg",
        "common_icon_star_on",
        "common_icon_star_off",
        "common_bg_price",
        "common_line_text",
        "common_info_area",
        "common_dt_bg",
    };

    public static void Prefix(UISprite __instance)
    {
        if (!Util.IsTranslationEnabled())
        {
            return;
        }
        
        var originalAtlas = __instance.atlas;
        if (originalAtlas == null)
        {
            return;
        }

        var atlasName = originalAtlas.name;
        if (!AtlasInitPatch.Atlases.TryGetValue(atlasName, out var replacementAtlas))
        {
            return;
        }

        var spriteName = __instance.mSpriteName;
        if (IsBlacklisted(spriteName))
        {
            return;
        }

        if (AtlasInitPatch.ContainsSprite(atlasName, spriteName))
        {
            AtlasInitPatch.OriginalAtlases.TryAdd(atlasName, originalAtlas);
            __instance.mAtlas = replacementAtlas;
        }
        else
        {
            Log.Warn($"Sprite {spriteName} not found in atlas {replacementAtlas.name}");
        }
    }

    public static bool IsBlacklisted(string spriteName)
    {
        return DisallowedSpriteNames.Contains(spriteName);
    }
}

[HarmonyPatch(typeof(UISprite), nameof(UISprite.spriteName), MethodType.Setter)]
[HarmonyWrapSafe]
public class SpriteNameUpdatePatch
{
    public static void Prefix(UISprite __instance, string value)
    {
        if (!Util.IsTranslationEnabled())
        {
            return;
        }
        
        var currentAtlas = __instance.atlas;
        if (currentAtlas == null)
        {
            return;
        }

        var currentAtlasName = currentAtlas.name;
        var isReplacementAtlas = currentAtlasName.EndsWith(AtlasInitPatch.NameSuffix, StringComparison.Ordinal);
        var atlasName = isReplacementAtlas
            ? currentAtlasName[..^AtlasInitPatch.NameSuffix.Length]
            : currentAtlasName;

        if (!AtlasInitPatch.Atlases.TryGetValue(atlasName, out var replacementAtlas) ||
            WidgetPatch.IsBlacklisted(value))
        {
            return;
        }

        if (AtlasInitPatch.ContainsSprite(atlasName, value))
        {
            if (currentAtlas.Pointer != replacementAtlas.Pointer)
            {
                __instance.mAtlas = replacementAtlas; 
            }
        }
        else if (isReplacementAtlas)
        {
            Log.Debug($"Sprite {value} not found in atlas {replacementAtlas.name}");
            if (AtlasInitPatch.OriginalAtlases.TryGetValue(atlasName, out var originalAtlas))
            {
                __instance.mAtlas = originalAtlas;
                Log.Debug("Reverting to original atlas");
            }
            else
            {
                Log.Warn($"Original atlas {atlasName} not found");
            }
        }
    }
}

//This patch is intended to fix an issue when using upscaled textures, which causes some icons to appear too large.
//The issue with this patch is that it causes incorrect scaling for some sprites.
/*[HarmonyPatch]
[HarmonyWrapSafe]
public class SpriteNamePatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(UISprite), nameof(UISprite.OnUpdate));
    }

    public static void Postfix(UISprite __instance)
    {
        var atlasSprite = __instance.GetAtlasSprite();
        if (atlasSprite == null)
        {
            return;
        }

        if (__instance.width != atlasSprite.width)
        {
            return;
        }

        var num = Mathf.RoundToInt(__instance.pixelSize *
                                   (atlasSprite.width + atlasSprite.paddingLeft + atlasSprite.paddingRight));
        var num2 = Mathf.RoundToInt(__instance.pixelSize *
                                    (atlasSprite.height + atlasSprite.paddingTop + atlasSprite.paddingBottom));
        if ((num & 1) == 1)
        {
            num++;
        }

        if ((num2 & 1) == 1)
        {
            num2++;
        }

        __instance.width = num;
        __instance.height = num2;
    }
}*/

[HarmonyPatch(typeof(UISprite), nameof(UISprite.OnInit))]
[HarmonyPriority(Priority.High)]
[HarmonyWrapSafe]
public class AtlasDumpPatch
{
    private static readonly HashSet<string> DumpedAtlases = new(StringComparer.Ordinal);

    public static void Prefix(UISprite __instance)
    {
        if (!XUnity.AutoTranslator.Plugin.Core.Configuration.Settings.EnableTextureDumping)
        {
            return;
        }

        var atlas = __instance.atlas;
        if (atlas == null)
        {
            return;
        }

        var atlasName = atlas.name;
        if (DumpedAtlases.Contains(atlasName) || atlasName.EndsWith(AtlasInitPatch.NameSuffix, StringComparison.Ordinal))
        {
            return;
        }

        var dumpPath = XUnity.AutoTranslator.Plugin.Core.Configuration.Settings.TexturesPath;
        if (dumpPath == null)
        {
            Log.Warn("Texture dump path not set");
            return;
        }

        var json = JsonUtility.ToJson(atlas);
        var jsonPath = Path.Join(dumpPath, atlasName + ".json");
        File.WriteAllText(jsonPath, json);
        Log.Debug("Dumped atlas " + atlasName + " to " + jsonPath);
        DumpedAtlases.Add(atlasName);
    }
}
