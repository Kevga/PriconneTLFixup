using BepInEx;
using CodeStage.AntiCheat.ObscuredTypes;
using Elements;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PriconneTLFixup.Patches;

[HarmonyPatch(typeof(ConstTextData), nameof(ConstTextData.CreateInstanceAndLoadInitialize))]
[HarmonyWrapSafe]
public class TextIdPatch2
{
    private static readonly string DictPath =
        Path.Join(Paths.BepInExRootPath, "Translation", "en", "Other", "text_id.txt");
    
    public static void Postfix()
    {
        if (!File.Exists(DictPath))
        {
            Log.Error($"Text id dictionary file not found: {DictPath}");
            return;
        }

        var file = new StreamReader(DictPath);
        while (file.ReadLine() is { } ln)
        {
            var parts = ln.Split("=");
            if (parts.Length != 2)
            {
                continue;
            }

            var textId = parts[0];
            var en = parts[1];
            if (en.Length == 0)
            {
                continue;
            }

            var tryParse = Enum.TryParse<eTextId>(textId, out var enumKey);
            if (!tryParse)
            {
                Log.Warn($"TextIdPatch2: Failed to parse text id: {textId}");
                continue;
            }

            Singleton<ConstTextData>.Instance.scriptableObject.DataDictionary[enumKey] = en;
            Log.Debug($"TextIdPatch2: Added {enumKey} with text {en} to dictionary");
        }

        file.Close();
    }
}
