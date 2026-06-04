using System.Globalization;
using BepInEx;
using Cute;
using ExIni;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Extensions;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace PriconneTLFixup.Patches;

internal static class IniFileExtensions
{
    public static void Set<T>( this IniFile that, string section, string key, T value )
    {
        var typeOfT = typeof( T ).UnwrapNullable();
        var iniSection = that[ section ];
        var iniKey = iniSection[ key ];

        if( value == null )
        {
            iniKey.Value = string.Empty;
        }
        else
        {
            if( typeOfT.IsEnum )
            {
                iniKey.Value = EnumHelper.GetNames( typeOfT, value );
            }
            else
            {
                iniKey.Value = Convert.ToString( value, CultureInfo.InvariantCulture );
            }
        }
    }
}

[HarmonyPatch(typeof(BootApp), nameof(BootApp.Start))]
[HarmonyWrapSafe]
public class AutoTranslatorConfigUpdaterPatch
{
    public static void Postfix()
    {
        var configPath = Path.Combine(Paths.ConfigPath, "AutoTranslatorConfig.patch.ini");
        if( !File.Exists( configPath ) )
        {
            return;
        }
        
        var ini = IniFile.FromFile(configPath);
        if (ini == null)
        {
            return;
        }
        
        Array.ForEach(ini.Sections.ToArray(), section =>
        {
            Array.ForEach(section.Keys.ToArray(), key =>
            {
                var value = key.Value;
                Log.Debug($"Setting Auto Translator config: [{section.Section}] {key.Key} = {value}");
                PluginEnvironment.Current.Preferences.Set(section.Section, key.Key, value);
            });
        });
    }
}