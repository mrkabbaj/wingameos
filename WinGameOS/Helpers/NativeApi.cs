using System;
using System.Runtime.InteropServices;

namespace WinGameOS.Helpers
{
    /// <summary>
    /// Windows native API P/Invoke declarations for system hardware control.
    /// </summary>
    public static class NativeApi
    {
        // ═══════════════════════════════════════════════════════════════
        //  USER32.DLL — Window management, display, hotkeys
        // ═══════════════════════════════════════════════════════════════

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        // ═══════════════════════════════════════════════════════════════
        //  SHELL32.DLL — Taskbar & notification area
        // ═══════════════════════════════════════════════════════════════

        [DllImport("shell32.dll")]
        public static extern int SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        // ═══════════════════════════════════════════════════════════════
        //  POWRPROF.DLL — Power management
        // ═══════════════════════════════════════════════════════════════

        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        public static extern uint PowerReadFriendlyName(
            IntPtr RootPowerKey, ref Guid SchemeGuid,
            IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid,
            IntPtr Buffer, ref uint BufferSize);

        // ═══════════════════════════════════════════════════════════════
        //  KERNEL32.DLL — System info
        // ═══════════════════════════════════════════════════════════════

        [DllImport("kernel32.dll")]
        public static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        // ═══════════════════════════════════════════════════════════════
        //  Constants
        // ═══════════════════════════════════════════════════════════════

        // ShowWindow commands
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;
        public const int SW_SHOWMAXIMIZED = 3;

        // Hotkey modifiers
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // Virtual key codes
        public const uint VK_G = 0x47;
        public const uint VK_S = 0x53;
        public const uint VK_P = 0x50;

        // Display settings
        public const int ENUM_CURRENT_SETTINGS = -1;
        public const int ENUM_REGISTRY_SETTINGS = -2;
        public const int CDS_UPDATEREGISTRY = 0x01;
        public const int CDS_TEST = 0x02;
        public const int CDS_FULLSCREEN = 0x04;
        public const int DISP_CHANGE_SUCCESSFUL = 0;

        // Window messages
        public const uint WM_HOTKEY = 0x0312;
        public const uint WM_APPCOMMAND = 0x0319;
        public const int APPCOMMAND_VOLUME_MUTE = 8;
        public const int APPCOMMAND_VOLUME_DOWN = 9;
        public const int APPCOMMAND_VOLUME_UP = 10;

        // SetWindowPos flags
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // AppBar messages
        public const int ABM_SETSTATE = 0x0000000A;
        public const int ABM_GETSTATE = 0x00000004;
        public const int ABM_GETTASKBARPOS = 0x00000005;
        public const int ABS_AUTOHIDE = 0x01;
        public const int ABS_ALWAYSONTOP = 0x02;

        // System metrics
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        // Mutex
        public const uint ERROR_ALREADY_EXISTS = 183;

        // Well-known power plan GUIDs
        public static readonly Guid GUID_POWER_SAVER = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");
        public static readonly Guid GUID_BALANCED = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
        public static readonly Guid GUID_HIGH_PERFORMANCE = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        public static readonly Guid GUID_ULTIMATE_PERF = new Guid("e9a42b02-d5df-448d-aa00-03f14749eb61");

        // ═══════════════════════════════════════════════════════════════
        //  Structs
        // ═══════════════════════════════════════════════════════════════

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        // DEVMODE field flags
        public const int DM_PELSWIDTH = 0x80000;
        public const int DM_PELSHEIGHT = 0x100000;
        public const int DM_BITSPERPEL = 0x40000;
        public const int DM_DISPLAYFREQUENCY = 0x400000;
    }
}
