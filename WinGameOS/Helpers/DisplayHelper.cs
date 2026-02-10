using System;
using System.Collections.Generic;
using WinGameOS.Models;
using static WinGameOS.Helpers.NativeApi;

namespace WinGameOS.Helpers
{
    /// <summary>
    /// Helper methods for display mode enumeration and switching.
    /// </summary>
    public static class DisplayHelper
    {
        /// <summary>
        /// Enumerates all supported display modes for the primary monitor.
        /// </summary>
        public static List<DisplayMode> GetSupportedDisplayModes()
        {
            var modes = new List<DisplayMode>();
            var seen = new HashSet<string>();
            var dm = new DEVMODE();
            dm.dmSize = (short)System.Runtime.InteropServices.Marshal.SizeOf(typeof(DEVMODE));

            int modeIndex = 0;
            while (EnumDisplaySettings(null, modeIndex++, ref dm))
            {
                if (dm.dmBitsPerPel == 32) // Only 32-bit color modes
                {
                    string key = $"{dm.dmPelsWidth}x{dm.dmPelsHeight}@{dm.dmDisplayFrequency}";
                    if (seen.Add(key))
                    {
                        modes.Add(new DisplayMode
                        {
                            Width = dm.dmPelsWidth,
                            Height = dm.dmPelsHeight,
                            RefreshRate = dm.dmDisplayFrequency,
                            BitsPerPixel = dm.dmBitsPerPel
                        });
                    }
                }
            }

            modes.Sort((a, b) =>
            {
                int cmp = (b.Width * b.Height).CompareTo(a.Width * a.Height);
                return cmp != 0 ? cmp : b.RefreshRate.CompareTo(a.RefreshRate);
            });

            return modes;
        }

        /// <summary>
        /// Gets the current display mode.
        /// </summary>
        public static DisplayMode? GetCurrentDisplayMode()
        {
            var dm = new DEVMODE();
            dm.dmSize = (short)System.Runtime.InteropServices.Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm))
            {
                return new DisplayMode
                {
                    Width = dm.dmPelsWidth,
                    Height = dm.dmPelsHeight,
                    RefreshRate = dm.dmDisplayFrequency,
                    BitsPerPixel = dm.dmBitsPerPel
                };
            }
            return null;
        }

        /// <summary>
        /// Changes the display resolution and refresh rate.
        /// Returns true if successful.
        /// </summary>
        public static bool ChangeDisplayMode(DisplayMode mode)
        {
            var dm = new DEVMODE();
            dm.dmSize = (short)System.Runtime.InteropServices.Marshal.SizeOf(typeof(DEVMODE));
            dm.dmPelsWidth = mode.Width;
            dm.dmPelsHeight = mode.Height;
            dm.dmDisplayFrequency = mode.RefreshRate;
            dm.dmBitsPerPel = mode.BitsPerPixel;
            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL;

            // Test first
            int testResult = ChangeDisplaySettings(ref dm, CDS_TEST);
            if (testResult != DISP_CHANGE_SUCCESSFUL)
                return false;

            // Apply
            int result = ChangeDisplaySettings(ref dm, CDS_UPDATEREGISTRY);
            return result == DISP_CHANGE_SUCCESSFUL;
        }

        /// <summary>
        /// Gets the primary screen dimensions.
        /// </summary>
        public static (int Width, int Height) GetScreenSize()
        {
            return (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));
        }
    }
}
