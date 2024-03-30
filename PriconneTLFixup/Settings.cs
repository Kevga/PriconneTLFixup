using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity;
using UnityEngine;

namespace PriconneTLFixup;

public static class Settings
{
    internal static ConfigFile Config = null!;

    public static ConfigEntry<bool> EnableLargeNumberSeparators { get; private set; } = null!;
    public static ConfigEntry<bool> EnableFontReplacement { get; private set; } = null!;

    public static void Init(ConfigFile config)
    {
        Config = config;
        config.SaveOnConfigSet = true;
        
        EnableLargeNumberSeparators = config.Bind("General", "EnableLargeNumberSeparators", true, new ConfigDescription("Enable large number separators (e.g. 1,000,000 instead of 1000000)."));
        EnableFontReplacement = config.Bind("General", "EnableFontReplacement", true, new ConfigDescription("Replaces the game's fonts with the font used in the English version of the game."));
    }
    
}