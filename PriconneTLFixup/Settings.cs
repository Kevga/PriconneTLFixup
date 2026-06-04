using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity;
using UnityEngine;

namespace PriconneTLFixup;

public static class Settings
{
    internal static ConfigFile Config = null!;

    public static ConfigEntry<bool> Fullscreen { get; private set; } = null!;
    public static ConfigEntry<string> FullscreenMode { get; private set; } = null!;
    public static ConfigEntry<bool> EnableLargeNumberSeparators { get; private set; } = null!;
    public static ConfigEntry<bool> EnableFontReplacement { get; private set; } = null!;
    public static ConfigEntry<bool> BirthdayToggle { get; private set; } = null!;

    public static void Init(ConfigFile config)
    {
        Config = config;
        config.SaveOnConfigSet = true;
        
        EnableLargeNumberSeparators = config.Bind("General", "EnableLargeNumberSeparators", true, new ConfigDescription("Enable large number separators (e.g. 1,000,000 instead of 1000000)."));
        EnableFontReplacement = config.Bind("General", "EnableFontReplacement", true, new ConfigDescription("Replaces the game's fonts with the font used in the English version of the game."));
        Fullscreen = config.Bind("General", "EnableFullscreen", false, new ConfigDescription("Enable fullscreen mode. You can toggle fullscreen at any time by pressing Alt+Enter or F11 while playing."));
        FullscreenMode = config.Bind("General", "FullscreenMode", "BorderlessWindow", new ConfigDescription("Set the fullscreen mode. Options are 'FullscreenWindow', 'MaximizedWindow' and 'ExclusiveFullscreen'."));
        
        BirthdayToggle = config.Bind("Z_Development", "BirthdayToggle", false, new ConfigDescription("Do not change. This is used by translators to translate birthday dialogues."));
    }
    
}