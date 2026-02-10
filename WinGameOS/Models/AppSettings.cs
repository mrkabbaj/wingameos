using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WinGameOS.Models
{
    /// <summary>
    /// Persistent application settings saved to JSON.
    /// </summary>
    public class AppSettings
    {
        // --- Game Mode ---
        public bool StartInGameMode { get; set; } = true;
        public bool LaunchAtWindowsStartup { get; set; } = false;
        public bool HideTaskbarInGameMode { get; set; } = true;
        public bool SuppressNotifications { get; set; } = true;

        // --- Hotkeys ---
        public string ToggleGameModeHotkey { get; set; } = "Ctrl+Alt+G";
        public string QuickSettingsHotkey { get; set; } = "Ctrl+Alt+S";
        public string PerformanceOverlayHotkey { get; set; } = "Ctrl+Alt+P";

        // --- Display ---
        public string PreferredResolution { get; set; } = string.Empty;
        public int PreferredRefreshRate { get; set; } = 0;
        public int Brightness { get; set; } = 80;

        // --- Audio ---
        public int MasterVolume { get; set; } = 75;
        public bool IsMuted { get; set; } = false;
        public string PreferredAudioDevice { get; set; } = string.Empty;

        // --- Performance ---
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PerformanceMode PerformanceMode { get; set; } = PerformanceMode.Balanced;

        // --- UI ---
        public string AccentColor { get; set; } = "#6C5CE7";
        public double UIScale { get; set; } = 1.0;
        public bool ShowPerformanceOverlay { get; set; } = false;
        public bool ShowFPS { get; set; } = true;
        public bool ShowTemperature { get; set; } = true;
        public bool ShowBattery { get; set; } = true;

        // --- Game Library ---
        public List<string> CustomGameDirectories { get; set; } = new();
        public List<Game> ManualGames { get; set; } = new();
        public string LastSelectedCategory { get; set; } = "All";
        public string LastSelectedPlatform { get; set; } = "All";
    }
}
