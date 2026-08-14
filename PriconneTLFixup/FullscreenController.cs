using UnityEngine;

namespace PriconneTLFixup;

public static class FullscreenController
{
    private static bool isFullscreened = false;
    private static int LastWidth = 1280;
    private static int LastHeight = 720;

    public static void Init()
    {
        if (Settings.Fullscreen.Value)
        {
            ToggleFullscreen(true);
        }
        
        CoroutineStarter.OnUpdate -= Update;
        CoroutineStarter.OnUpdate += Update;
        
        Log.Debug("HotkeyController initialized");
    }
    
    public static void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11) ||
            (Input.GetKeyDown(KeyCode.F4) && !Input.GetKey(KeyCode.LeftAlt)) ||
            (Input.GetKeyDown(KeyCode.Return) && Input.GetKey(KeyCode.LeftAlt)))
        {
            ToggleFullscreen();
        }
    }
    
    private static void ToggleFullscreen(bool initial = false)
    {
        Log.Debug("ToggleFullscreen");
        var mode = FullScreenMode.FullScreenWindow;
        
        if (Settings.FullscreenMode.Value == "FullscreenWindow")
        {
            mode = FullScreenMode.FullScreenWindow;
        } 
        else if (Settings.FullscreenMode.Value == "ExclusiveFullscreen")
        {
            mode = FullScreenMode.ExclusiveFullScreen;
        } 
        else if (Settings.FullscreenMode.Value == "MaximizedWindow")
        {
            mode = FullScreenMode.MaximizedWindow;
        }
        
        if (isFullscreened)
        {
            Screen.SetResolution(LastWidth, LastHeight, FullScreenMode.Windowed);
            isFullscreened = false;
        }
        else
        {
            if (!initial)
            {
                LastWidth = Screen.width;
                LastHeight = Screen.height;
            }
#if DEBUG
            Screen.SetResolution(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, mode);
#else            
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, mode);
#endif
            isFullscreened = true;
        }
        
        if (Settings.Fullscreen.Value != isFullscreened)
        {
            Settings.Fullscreen.Value = isFullscreened;
        }
    }
}
