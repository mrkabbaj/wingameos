using System;
using System.Runtime.InteropServices;
using WinGameOS.Helpers;

namespace WinGameOS.Services
{
    /// <summary>
    /// Controls Windows taskbar and notification suppression for Game Mode.
    /// </summary>
    public class TaskbarService
    {
        private bool _taskbarHidden;

        public bool IsTaskbarHidden => _taskbarHidden;

        public TaskbarService()
        {
            LoggingService.Instance.Info("Taskbar service initialized.");
        }

        /// <summary>
        /// Hides the Windows taskbar.
        /// </summary>
        public void HideTaskbar()
        {
            try
            {
                IntPtr taskbar = NativeApi.FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero)
                {
                    NativeApi.ShowWindow(taskbar, NativeApi.SW_HIDE);
                    _taskbarHidden = true;
                    LoggingService.Instance.Info("Taskbar hidden.");
                }

                // Also hide the secondary taskbar on multi-monitor setups
                IntPtr secondaryTaskbar = NativeApi.FindWindow("Shell_SecondaryTrayWnd", null);
                if (secondaryTaskbar != IntPtr.Zero)
                {
                    NativeApi.ShowWindow(secondaryTaskbar, NativeApi.SW_HIDE);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to hide taskbar", ex);
            }
        }

        /// <summary>
        /// Shows the Windows taskbar.
        /// </summary>
        public void ShowTaskbar()
        {
            try
            {
                IntPtr taskbar = NativeApi.FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero)
                {
                    NativeApi.ShowWindow(taskbar, NativeApi.SW_SHOW);
                    _taskbarHidden = false;
                    LoggingService.Instance.Info("Taskbar shown.");
                }

                IntPtr secondaryTaskbar = NativeApi.FindWindow("Shell_SecondaryTrayWnd", null);
                if (secondaryTaskbar != IntPtr.Zero)
                {
                    NativeApi.ShowWindow(secondaryTaskbar, NativeApi.SW_SHOW);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to show taskbar", ex);
            }
        }

        /// <summary>
        /// Enables Focus Assist (Do Not Disturb) to suppress notifications.
        /// Uses registry approach for broad compatibility.
        /// </summary>
        public void SuppressNotifications()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", true);
                if (key != null)
                {
                    key.SetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED", 0,
                        Microsoft.Win32.RegistryValueKind.DWord);
                    LoggingService.Instance.Info("Notifications suppressed.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to suppress notifications", ex);
            }
        }

        /// <summary>
        /// Restores normal notification behavior.
        /// </summary>
        public void RestoreNotifications()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings", true);
                if (key != null)
                {
                    key.SetValue("NOC_GLOBAL_SETTING_TOASTS_ENABLED", 1,
                        Microsoft.Win32.RegistryValueKind.DWord);
                    LoggingService.Instance.Info("Notifications restored.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to restore notifications", ex);
            }
        }

        /// <summary>
        /// Ensures taskbar and notifications are restored on app exit.
        /// </summary>
        public void RestoreAll()
        {
            if (_taskbarHidden)
                ShowTaskbar();
            RestoreNotifications();
        }
    }
}
