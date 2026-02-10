using System;
using System.IO;
using System.Text.Json;
using WinGameOS.Models;

namespace WinGameOS.Services
{
    /// <summary>
    /// Manages application settings persistence.
    /// </summary>
    public class SettingsService
    {
        private static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinGameOS");

        private static readonly string SettingsFilePath = Path.Combine(AppDataPath, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private AppSettings _settings = new();

        public AppSettings Settings => _settings;

        public event EventHandler? SettingsChanged;

        public SettingsService()
        {
            EnsureDirectoryExists();
            Load();
        }

        /// <summary>
        /// Load settings from disk, or create defaults if none exist.
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                    LoggingService.Instance.Info("Settings loaded from disk.");
                }
                else
                {
                    _settings = new AppSettings();
                    Save(); // Create default file
                    LoggingService.Instance.Info("Default settings created.");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to load settings: {ex.Message}");
                _settings = new AppSettings();
            }
        }

        /// <summary>
        /// Save current settings to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                EnsureDirectoryExists();
                string json = JsonSerializer.Serialize(_settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
                LoggingService.Instance.Info("Settings saved to disk.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset settings to defaults.
        /// </summary>
        public void Reset()
        {
            _settings = new AppSettings();
            Save();
        }

        /// <summary>
        /// Update a setting and auto-save.
        /// </summary>
        public void Update(Action<AppSettings> updateAction)
        {
            updateAction(_settings);
            Save();
        }

        private static void EnsureDirectoryExists()
        {
            if (!Directory.Exists(AppDataPath))
                Directory.CreateDirectory(AppDataPath);
        }
    }
}
