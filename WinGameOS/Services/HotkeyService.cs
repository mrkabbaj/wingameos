using System;
using System.Windows.Interop;
using WinGameOS.Helpers;

namespace WinGameOS.Services
{
    /// <summary>
    /// Manages global hotkey registration for mode toggling.
    /// </summary>
    public class HotkeyService : IDisposable
    {
        private IntPtr _windowHandle;
        private HwndSource? _hwndSource;

        // Hotkey IDs
        public const int HOTKEY_TOGGLE_GAMEMODE = 1;
        public const int HOTKEY_QUICK_SETTINGS = 2;
        public const int HOTKEY_PERF_OVERLAY = 3;

        public event EventHandler? GameModeToggleRequested;
        public event EventHandler? QuickSettingsRequested;
        public event EventHandler? PerformanceOverlayRequested;

        public void Initialize(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _hwndSource = HwndSource.FromHwnd(windowHandle);
            _hwndSource?.AddHook(WndProc);

            RegisterDefaultHotkeys();
            LoggingService.Instance.Info("Hotkey service initialized.");
        }

        private void RegisterDefaultHotkeys()
        {
            // Ctrl+Alt+G — Toggle Game Mode
            bool result1 = NativeApi.RegisterHotKey(_windowHandle, HOTKEY_TOGGLE_GAMEMODE,
                NativeApi.MOD_CONTROL | NativeApi.MOD_ALT | NativeApi.MOD_NOREPEAT, NativeApi.VK_G);

            // Ctrl+Alt+S — Quick Settings
            bool result2 = NativeApi.RegisterHotKey(_windowHandle, HOTKEY_QUICK_SETTINGS,
                NativeApi.MOD_CONTROL | NativeApi.MOD_ALT | NativeApi.MOD_NOREPEAT, NativeApi.VK_S);

            // Ctrl+Alt+P — Performance Overlay
            bool result3 = NativeApi.RegisterHotKey(_windowHandle, HOTKEY_PERF_OVERLAY,
                NativeApi.MOD_CONTROL | NativeApi.MOD_ALT | NativeApi.MOD_NOREPEAT, NativeApi.VK_P);

            LoggingService.Instance.Info($"Hotkeys registered: GameMode={result1}, QuickSettings={result2}, PerfOverlay={result3}");
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeApi.WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                switch (hotkeyId)
                {
                    case HOTKEY_TOGGLE_GAMEMODE:
                        GameModeToggleRequested?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case HOTKEY_QUICK_SETTINGS:
                        QuickSettingsRequested?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                    case HOTKEY_PERF_OVERLAY:
                        PerformanceOverlayRequested?.Invoke(this, EventArgs.Empty);
                        handled = true;
                        break;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            NativeApi.UnregisterHotKey(_windowHandle, HOTKEY_TOGGLE_GAMEMODE);
            NativeApi.UnregisterHotKey(_windowHandle, HOTKEY_QUICK_SETTINGS);
            NativeApi.UnregisterHotKey(_windowHandle, HOTKEY_PERF_OVERLAY);
            _hwndSource?.RemoveHook(WndProc);
            LoggingService.Instance.Info("Hotkeys unregistered.");
        }
    }
}
