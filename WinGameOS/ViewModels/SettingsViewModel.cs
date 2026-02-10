using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WinGameOS.Models;
using WinGameOS.Services;

namespace WinGameOS.ViewModels
{
    /// <summary>
    /// ViewModel for the Settings panel.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly DisplayService _displayService;
        private readonly AudioService _audioService;
        private readonly PowerService _powerService;
        private readonly SettingsService _settingsService;

        // --- Display ---
        private ObservableCollection<DisplayMode> _displayModes = new();
        private DisplayMode? _currentDisplayMode;
        private DisplayMode? _selectedDisplayMode;
        private int _brightness;

        // --- Audio ---
        private int _volume;
        private bool _isMuted;
        private ObservableCollection<AudioDeviceInfo> _audioDevices = new();
        private AudioDeviceInfo? _selectedAudioDevice;

        // --- Performance ---
        private PerformanceMode _performanceMode;
        private int _batteryPercent;
        private bool _isCharging;
        private string _batteryTimeRemaining = "";

        // --- Display Properties ---
        public ObservableCollection<DisplayMode> DisplayModes
        {
            get => _displayModes;
            set => SetProperty(ref _displayModes, value);
        }

        public DisplayMode? CurrentDisplayMode
        {
            get => _currentDisplayMode;
            set => SetProperty(ref _currentDisplayMode, value);
        }

        public DisplayMode? SelectedDisplayMode
        {
            get => _selectedDisplayMode;
            set => SetProperty(ref _selectedDisplayMode, value);
        }

        public int Brightness
        {
            get => _brightness;
            set
            {
                if (SetProperty(ref _brightness, value))
                {
                    _displayService.SetBrightness(value);
                    _settingsService.Update(s => s.Brightness = value);
                }
            }
        }

        // --- Audio Properties ---
        public int Volume
        {
            get => _volume;
            set
            {
                if (SetProperty(ref _volume, value))
                {
                    _audioService.SetVolume(value);
                    _settingsService.Update(s => s.MasterVolume = value);
                }
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (SetProperty(ref _isMuted, value))
                {
                    _audioService.SetMute(value);
                    _settingsService.Update(s => s.IsMuted = value);
                }
            }
        }

        public ObservableCollection<AudioDeviceInfo> AudioDevices
        {
            get => _audioDevices;
            set => SetProperty(ref _audioDevices, value);
        }

        public AudioDeviceInfo? SelectedAudioDevice
        {
            get => _selectedAudioDevice;
            set => SetProperty(ref _selectedAudioDevice, value);
        }

        // --- Performance Properties ---
        public PerformanceMode PerformanceMode
        {
            get => _performanceMode;
            set
            {
                if (SetProperty(ref _performanceMode, value))
                {
                    var result = _powerService.SetPerformanceMode(value);
                    StatusMessage = result.Message;
                    _settingsService.Update(s => s.PerformanceMode = value);
                }
            }
        }

        public int BatteryPercent
        {
            get => _batteryPercent;
            set => SetProperty(ref _batteryPercent, value);
        }

        public bool IsCharging
        {
            get => _isCharging;
            set => SetProperty(ref _isCharging, value);
        }

        public string BatteryTimeRemaining
        {
            get => _batteryTimeRemaining;
            set => SetProperty(ref _batteryTimeRemaining, value);
        }

        // --- General ---
        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _launchAtStartup;
        public bool LaunchAtStartup
        {
            get => _launchAtStartup;
            set
            {
                if (SetProperty(ref _launchAtStartup, value))
                {
                    SetAutoStart(value);
                    _settingsService.Update(s => s.LaunchAtWindowsStartup = value);
                }
            }
        }

        private bool _hideTaskbar;
        public bool HideTaskbar
        {
            get => _hideTaskbar;
            set
            {
                if (SetProperty(ref _hideTaskbar, value))
                    _settingsService.Update(s => s.HideTaskbarInGameMode = value);
            }
        }

        private bool _suppressNotifications;
        public bool SuppressNotifications
        {
            get => _suppressNotifications;
            set
            {
                if (SetProperty(ref _suppressNotifications, value))
                    _settingsService.Update(s => s.SuppressNotifications = value);
            }
        }

        public Array PerformanceModes => Enum.GetValues(typeof(PerformanceMode));

        // Commands
        public ICommand ApplyDisplayModeCommand { get; }
        public ICommand ToggleMuteCommand { get; }
        public ICommand RefreshBatteryCommand { get; }
        public ICommand SleepCommand { get; }
        public ICommand ShutdownCommand { get; }
        public ICommand RestartCommand { get; }

        public SettingsViewModel(
            DisplayService displayService,
            AudioService audioService,
            PowerService powerService,
            SettingsService settingsService)
        {
            _displayService = displayService;
            _audioService = audioService;
            _powerService = powerService;
            _settingsService = settingsService;

            ApplyDisplayModeCommand = new RelayCommand(ApplyDisplayMode, () => SelectedDisplayMode != null);
            ToggleMuteCommand = new RelayCommand(() => IsMuted = !IsMuted);
            RefreshBatteryCommand = new RelayCommand(RefreshBattery);
            SleepCommand = new RelayCommand(() => _powerService.Sleep());
            ShutdownCommand = new RelayCommand(() => _powerService.Shutdown());
            RestartCommand = new RelayCommand(() => _powerService.Restart());

            LoadSettings();
            RefreshAll();
        }

        private void LoadSettings()
        {
            var s = _settingsService.Settings;
            _hideTaskbar = s.HideTaskbarInGameMode;
            _suppressNotifications = s.SuppressNotifications;
            _launchAtStartup = s.LaunchAtWindowsStartup;
        }

        /// <summary>
        /// Refreshes all hardware readings.
        /// </summary>
        public void RefreshAll()
        {
            RefreshDisplayModes();
            RefreshAudio();
            RefreshBattery();
            RefreshPerformanceMode();
        }

        private void RefreshDisplayModes()
        {
            try
            {
                var modes = _displayService.GetSupportedModes();
                DisplayModes = new ObservableCollection<DisplayMode>(modes);
                CurrentDisplayMode = _displayService.GetCurrentMode();
                SelectedDisplayMode = CurrentDisplayMode;
                Brightness = _displayService.GetBrightness();
                if (Brightness < 0) Brightness = _settingsService.Settings.Brightness;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to refresh display modes", ex);
            }
        }

        private void RefreshAudio()
        {
            try
            {
                _volume = _audioService.GetVolume();
                OnPropertyChanged(nameof(Volume));
                _isMuted = _audioService.IsMuted();
                OnPropertyChanged(nameof(IsMuted));

                var devices = _audioService.GetOutputDevices();
                AudioDevices = new ObservableCollection<AudioDeviceInfo>(devices);
                SelectedAudioDevice = devices.Find(d => d.IsDefault);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to refresh audio", ex);
            }
        }

        private void RefreshBattery()
        {
            try
            {
                var (percent, isCharging, timeRemaining) = _powerService.GetBatteryStatus();
                BatteryPercent = percent;
                IsCharging = isCharging;
                BatteryTimeRemaining = isCharging
                    ? "Charging"
                    : timeRemaining.TotalMinutes > 0
                        ? $"{(int)timeRemaining.TotalHours}h {timeRemaining.Minutes}m remaining"
                        : "Calculating...";
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to refresh battery", ex);
            }
        }

        private void RefreshPerformanceMode()
        {
            try
            {
                _performanceMode = _powerService.GetCurrentPerformanceMode();
                OnPropertyChanged(nameof(PerformanceMode));
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to refresh performance mode", ex);
            }
        }

        private void ApplyDisplayMode()
        {
            if (SelectedDisplayMode != null)
            {
                var result = _displayService.ChangeMode(SelectedDisplayMode);
                StatusMessage = result.Message;
                if (result.Success)
                    CurrentDisplayMode = SelectedDisplayMode;
            }
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key != null)
                {
                    if (enable)
                        key.SetValue("WinGameOS", $"\"{appPath}\"");
                    else
                        key.DeleteValue("WinGameOS", false);

                    StatusMessage = enable
                        ? "✅ WinGameOS will launch at Windows startup"
                        : "WinGameOS removed from startup";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to change startup setting: {ex.Message}";
                LoggingService.Instance.Error("Failed to set auto-start", ex);
            }
        }
    }
}
