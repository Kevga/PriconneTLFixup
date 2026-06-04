using System.Collections;
using System.Diagnostics;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cute;
using Elements;
using HarmonyLib;
using UnityEngine;

namespace PriconneTLFixup.Patches;

/**
 * Technically not a problem with the translation patch, but this allows to scale up the game window to full resolution.
 */
[HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.getOptimizedWindowSize))]
public class ResolutionPatch
{
    public static bool Prefix(StandaloneWindowResize __instance, ref Vector3 __result, int _width, int _height)
    {
        Log.Debug($"Original resolution: {_width}x{_height}");
        if (_width == 128)
        {
            _width = __instance.windowLastWidth;
        }

        if (_height == 72)
        {
            _height = __instance.windowLastHeight;
        }
        
        float num = _width / (float)_height;

        __result = new Vector3(_width, _height, num);
        Log.Debug($"Optimized resolution: {_width}x{_height}");
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

[HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.WndProc))]
public class WndProcPatch
{
    private const uint WM_NCHITTEST = 0x0084;
    private const uint WM_SETCURSOR = 0x0020;
    
    // Hit test return values
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    
    // Cursor types
    private const uint IDC_ARROW = 32512;
    private const uint IDC_SIZENWSE = 32642;  // ↖↘
    private const uint IDC_SIZENESW = 32643;  // ↗↙
    private const uint IDC_SIZEWE = 32644;    // ↔
    private const uint IDC_SIZENS = 32645;    // ↕
    
    private const int RESIZE_HANDLE_SIZE = 8;
    private const int CORNER_HANDLE_SIZE = 12; // Larger grab area for corners
    
    private static int lastHitTest = HTCLIENT;

    // Compute hit test for a given screen position
    private static int ComputeHitTest(IntPtr hWnd, int x, int y)
    {
        RECT rect = new RECT();
        if (!GetWindowRect(hWnd, ref rect)) return HTCLIENT;
        int dLeft = x - rect.Left;
        int dRight = rect.Right - x;
        int dTop = y - rect.Top;
        int dBottom = rect.Bottom - y;

        bool nearLeft = dLeft >= 0 && dLeft < RESIZE_HANDLE_SIZE;
        bool nearRight = dRight >= 0 && dRight < RESIZE_HANDLE_SIZE;
        bool nearTop = dTop >= 0 && dTop < RESIZE_HANDLE_SIZE;
        bool nearBottom = dBottom >= 0 && dBottom < RESIZE_HANDLE_SIZE;

        bool inTopLeftCorner = dLeft >= 0 && dLeft < CORNER_HANDLE_SIZE && dTop >= 0 && dTop < CORNER_HANDLE_SIZE;
        bool inTopRightCorner = dRight >= 0 && dRight < CORNER_HANDLE_SIZE && dTop >= 0 && dTop < CORNER_HANDLE_SIZE;
        bool inBottomLeftCorner = dLeft >= 0 && dLeft < CORNER_HANDLE_SIZE && dBottom >= 0 && dBottom < CORNER_HANDLE_SIZE;
        bool inBottomRightCorner = dRight >= 0 && dRight < CORNER_HANDLE_SIZE && dBottom >= 0 && dBottom < CORNER_HANDLE_SIZE;

        if (inTopLeftCorner) return HTTOPLEFT;
        if (inTopRightCorner) return HTTOPRIGHT;
        if (inBottomLeftCorner) return HTBOTTOMLEFT;
        if (inBottomRightCorner) return HTBOTTOMRIGHT;
        if (nearLeft) return HTLEFT;
        if (nearRight) return HTRIGHT;
        if (nearTop) return HTTOP;
        if (nearBottom) return HTBOTTOM;
        return HTCLIENT;
    }

    public static bool Prefix(IntPtr _hWnd, uint _msg, IntPtr _wParam, IntPtr lParam, ref IntPtr __result)
    {
        // Start timing
        var sw = Stopwatch.StartNew();
        
        // Handle WM_SETCURSOR to set the appropriate cursor ourselves
        if (_msg == WM_SETCURSOR)
        {
            POINT pt;
            if (GetCursorPos(out pt))
            {
                int ht = ComputeHitTest(_hWnd, pt.X, pt.Y);
                lastHitTest = ht;
                IntPtr cursor = IntPtr.Zero;
                switch (ht)
                {
                    case HTTOPLEFT:
                    case HTBOTTOMRIGHT:
                        cursor = LoadCursor(IntPtr.Zero, IDC_SIZENWSE);
                        break;
                    case HTTOPRIGHT:
                    case HTBOTTOMLEFT:
                        cursor = LoadCursor(IntPtr.Zero, IDC_SIZENESW);
                        break;
                    case HTLEFT:
                    case HTRIGHT:
                        cursor = LoadCursor(IntPtr.Zero, IDC_SIZEWE);
                        break;
                    case HTTOP:
                    case HTBOTTOM:
                        cursor = LoadCursor(IntPtr.Zero, IDC_SIZENS);
                        break;
                }
                if (cursor != IntPtr.Zero)
                {
                    SetCursor(cursor);
                    __result = (IntPtr)1; // handled
                    sw.Stop();
                    return false;
                }
            }
            // Not on a resize zone -> let original decide cursor
        }

        // Call the original window procedure first
        var callStart = Stopwatch.StartNew();
        __result = WindowsAPI.CallWindowProc(StandaloneWindowResize.oldWndProcPtr, _hWnd, _msg, _wParam, lParam);
        callStart.Stop();

        // Intercept WM_NCHITTEST to provide proper resize border detection
        if (_msg == WM_NCHITTEST)
        {
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
            int ht = ComputeHitTest(_hWnd, x, y);
            if (ht != HTCLIENT)
            {
                __result = (IntPtr)ht;
                lastHitTest = ht;
            }
            else
            {
                lastHitTest = __result.ToInt32();
            }
        }

        sw.Stop();
        
        // Skip original method since we already called the window procedure
        return false;
    }
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, uint lpCursorName);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr hCursor);
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}

[HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.WndProcStatic))]
public class WndProcStaticPatch
{
   
    public static bool Prefix(IntPtr _hWnd, uint _msg, IntPtr _wParam, IntPtr _lParam, ref IntPtr __result)
    {
        // Redirect to our WndProcPatch
        WndProcPatch.Prefix(_hWnd, _msg, _wParam, _lParam, ref __result);
        return false;
    }
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr LoadCursor(IntPtr hInstance, uint lpCursorName);
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetCursor(IntPtr hCursor);
    
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

// If the maximize button is disabled, re-enable it
// Also enable window resizing by setting WS_SIZEBOX
[HarmonyPatch(typeof(BootApp), nameof(BootApp.Start))]
[HarmonyWrapSafe]
public class BootAppPatch
{
    public static void Postfix()
    {
        var activeWindow = WindowsAPI.GetActiveWindow();
        
        // Set window styles (GWL_STYLE = -16)
        long style = WindowsAPI.GetWindowLong(activeWindow, -16);
        
        // Enable maximize button (WS_MAXIMIZEBOX = 0x10000)
        if ((style & 0x10000) == 0L)
        {
            Log.Debug("Enabling maximize button");
            style |= 0x10000;
        }
        
        // Enable window resizing (WS_SIZEBOX/WS_THICKFRAME = 0x40000)
        if ((style & 0x40000) == 0L)
        {
            Log.Debug("Enabling window resizing");
            style |= 0x40000;
        }
        
        WindowsAPI.SetWindowLong(activeWindow, -16, style);
        
        // Set extended window styles (GWL_EXSTYLE = -20)
        long exStyle = WindowsAPI.GetWindowLong(activeWindow, -20);
        
        // Add WS_EX_WINDOWEDGE (0x00000100) for proper border appearance
        if ((exStyle & 0x100) == 0L)
        {
            Log.Debug("Enabling WS_EX_WINDOWEDGE");
            exStyle |= 0x100;
        }
        
        WindowsAPI.SetWindowLong(activeWindow, -20, exStyle);
        
        Log.Debug($"Window styles set: Style=0x{style:X}, ExStyle=0x{exStyle:X}");
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