using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using Cute;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime.Runtime;
using Spine;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using XUnity.AutoTranslator.Plugin.Core;
using Random = UnityEngine.Random;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(BootApp), nameof(BootApp.Start))]
[HarmonyWrapSafe]
public class AtlasInitPatch
{
    private static readonly string AtlasPath =
        Path.Join(Paths.BepInExRootPath, "Translation", "en", "Other", "atlases");

    public static readonly Dictionary<string, UIAtlas> Atlases = new();
    internal const string NameSuffix = " (Fixup)";

    public static void Postfix()
    {
        string[] jsonFiles;
        try
        {
            jsonFiles = Directory.GetFiles(AtlasPath, "*.json");
        } catch (System.Exception)
        {
            Log.Error("AtlasInitPatch: AtlasPath not found: " + AtlasPath);
            return;
        }

        if (jsonFiles.Length == 0)
        {
            Log.Error("No atlases found in " + AtlasPath);
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

        var files = Directory.EnumerateFiles(AtlasPath);
        var imageFiles = files.ToList().FindAll(f => f.EndsWith(".png")).Select(Path.GetFileNameWithoutExtension)
            .ToList();
        if (!imageFiles.Any())
        {
            Log.Error("No pngs found in " + AtlasPath);
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
            var matches = imageFiles.Where(f => f != null && f.StartsWith(atlasName));
            var matchList = matches.ToList();
            if (!matchList.Any() || matchList[0] == null)
            {
                Log.Error("No png found for " + atlasName + " (json: " + jsonFile + ")");
                continue;
            }

            if (matchList.Count > 1 && !Regex.IsMatch(matchList[0]!, "^" + atlasName + @"[ \.]"))
            {
                Log.Error("Multiple pngs found for " + jsonFile);
                foreach (var match in matchList)
                {
                    Log.Error(match);
                }
                continue;
            }

            var texturePath = Path.Join(AtlasPath, matchList[0]+".png");
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
            var atlasSize = GetAtlasSize(atlas);
            
            var textureData = File.ReadAllBytes(texturePath);
            var texture = new Texture2D(atlasSize, atlasSize, TextureFormat.RGBA32, false);

            var material = new Material(shader);
            material.renderQueue = 3054;
            atlas.material = material;
            material.name = atlasName + NameSuffix;
            material.mainTexture = texture;
            texture.requestedMipmapLevel = 0;
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(textureData);
            texture.name = atlasName + NameSuffix;

            Log.Debug("Loaded atlas " + atlas.name + " (" + texture.width + "x" + texture.height + ") in " +
                      totalSw.ElapsedMilliseconds + "ms");
            Atlases.Add(atlasName, atlas);
            totalSw.Stop();
        }

        Log.Info("Loaded " + Atlases.Count + " atlases. Took " + allAtlasesSw.ElapsedMilliseconds + "ms");
    }

    private static int GetAtlasSize(UIAtlas atlas)
    {
        var maxX = 0;
        var maxY = 0;
        
        foreach (var sprite in atlas.spriteList)
        {
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
    public static void Prefix(UISprite __instance)
    {
        if (__instance.atlas == null)
        {
            return;
        }

        //TODO: Make Atlases a dictionary with name as key to speed this up
        AtlasInitPatch.Atlases.TryGetValue(__instance.atlas.name, out var replacementAtlas);
        if (replacementAtlas == null)
        {
            return;
        }

        if (replacementAtlas.GetSprite(__instance.mSpriteName) != null)
        {
            __instance.atlas = replacementAtlas;
        }
        else
        {
            Log.Warn($"Sprite {__instance.mSpriteName} not found in atlas {replacementAtlas.name}");
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
    //List of previously dumped atlases
    private static readonly List<string> DumpedAtlases = new();

    public static void Prefix(UISprite __instance)
    {
        var atlas = __instance.atlas;
        if (atlas == null)
        {
            return;
        }

        if (DumpedAtlases.Contains(atlas.name) || AtlasInitPatch.Atlases.ContainsValue(atlas) || atlas.name.Contains(AtlasInitPatch.NameSuffix))
        {
            return;
        }

        if (!XUnity.AutoTranslator.Plugin.Core.Configuration.Settings.EnableTextureDumping)
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
        var jsonPath = Path.Join(dumpPath, atlas.name + ".json");
        File.WriteAllText(jsonPath, json);
        Log.Debug("Dumped atlas " + atlas.name + " to " + jsonPath);
        DumpedAtlases.Add(atlas.name);
    }
}