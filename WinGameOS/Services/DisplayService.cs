using System;
using System.Collections.Generic;
using WinGameOS.Helpers;
using WinGameOS.Models;

namespace WinGameOS.Services
{
    /// <summary>
    /// Manages display resolution, refresh rate, and brightness.
    /// </summary>
    public class DisplayService
    {
        private DisplayMode? _originalMode;

        public DisplayService()
        {
            // Save original mode for safe restoration
            _originalMode = DisplayHelper.GetCurrentDisplayMode();
            LoggingService.Instance.Info($"Display service initialized. Current: {_originalMode}");
        }

        /// <summary>
        /// Gets all supported display modes.
        /// </summary>
        public List<DisplayMode> GetSupportedModes()
        {
            return DisplayHelper.GetSupportedDisplayModes();
        }

        /// <summary>
        /// Gets unique resolutions (without duplicating refresh rates).
        /// </summary>
        public List<string> GetUniqueResolutions()
        {
            var resolutions = new HashSet<string>();
            foreach (var mode in GetSupportedModes())
                resolutions.Add(mode.Resolution);
            return new List<string>(resolutions);
        }

        /// <summary>
        /// Gets available refresh rates for a given resolution.
        /// </summary>
        public List<int> GetRefreshRates(int width, int height)
        {
            var rates = new List<int>();
            foreach (var mode in GetSupportedModes())
            {
                if (mode.Width == width && mode.Height == height)
                    rates.Add(mode.RefreshRate);
            }
            rates.Sort((a, b) => b.CompareTo(a));
            return rates;
        }

        /// <summary>
        /// Gets the current display mode.
        /// </summary>
        public DisplayMode? GetCurrentMode()
        {
            return DisplayHelper.GetCurrentDisplayMode();
        }

        /// <summary>
        /// Changes the display mode. Returns success status and message.
        /// </summary>
        public (bool Success, string Message) ChangeMode(DisplayMode mode)
        {
            try
            {
                bool success = DisplayHelper.ChangeDisplayMode(mode);
                if (success)
                {
                    string msg = $"Resolution changed to {mode.FullDescription}";
                    LoggingService.Instance.Info(msg);
                    return (true, msg);
                }
                else
                {
                    string msg = $"Failed to change resolution to {mode.FullDescription}. Mode not supported.";
                    LoggingService.Instance.Warning(msg);
                    return (false, msg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Error changing display mode: {ex.Message}";
                LoggingService.Instance.Error(msg, ex);
                return (false, msg);
            }
        }

        /// <summary>
        /// Restores the original display mode captured at startup.
        /// </summary>
        public void RestoreOriginalMode()
        {
            if (_originalMode != null)
            {
                ChangeMode(_originalMode);
                LoggingService.Instance.Info("Original display mode restored.");
            }
        }

        /// <summary>
        /// Gets screen brightness (0-100) via WMI.
        /// </summary>
        public int GetBrightness()
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "root\\WMI",
                    "SELECT CurrentBrightness FROM WmiMonitorBrightness");
                foreach (var obj in searcher.Get())
                {
                    return Convert.ToInt32(obj["CurrentBrightness"]);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to get brightness", ex);
            }
            return -1;
        }

        /// <summary>
        /// Sets screen brightness (0-100) via WMI.
        /// </summary>
        public bool SetBrightness(int level)
        {
            level = Math.Clamp(level, 0, 100);
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "root\\WMI",
                    "SELECT * FROM WmiMonitorBrightnessMethods");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    obj.InvokeMethod("WmiSetBrightness", new object[] { 1, level });
                    LoggingService.Instance.Info($"Brightness set to {level}%");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to set brightness", ex);
            }
            return false;
        }
    }
}
