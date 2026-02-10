using System;
using System.Windows.Input;
using System.Windows.Threading;
using WinGameOS.Models;
using WinGameOS.Services;

namespace WinGameOS.ViewModels
{
    /// <summary>
    /// Main ViewModel orchestrating navigation, mode switching, and performance monitoring.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // Services
        private readonly SettingsService _settingsService;
        private readonly TaskbarService _taskbarService;
        private readonly PowerService _powerService;
        private readonly HotkeyService _hotkeyService;
        private readonly DisplayService _displayService;
        private readonly AudioService _audioService;
        private readonly GameLauncherService _gameLauncherService;

        // Child ViewModels
        public GameLibraryViewModel GameLibrary { get; }
        public SettingsViewModel Settings { get; }

        // Navigation
        private string _currentView = "Home";
        private bool _isGameMode = true;
        private bool _isQuickSettingsOpen;
        private bool _isPerformanceOverlayVisible;

        // Performance
        private readonly DispatcherTimer _performanceTimer;
        private int _batteryPercent;
        private bool _isCharging;
        private float _cpuUsage;
        private float _cpuTemp;
        private float _ramUsedGB;
        private float _ramTotalGB;
        private string _currentTime = "";

        // Toast notification
        private string _toastMessage = "";
        private bool _isToastVisible;

        // --- Navigation Properties ---
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public bool IsGameMode
        {
            get => _isGameMode;
            set => SetProperty(ref _isGameMode, value);
        }

        public bool IsQuickSettingsOpen
        {
            get => _isQuickSettingsOpen;
            set => SetProperty(ref _isQuickSettingsOpen, value);
        }

        public bool IsPerformanceOverlayVisible
        {
            get => _isPerformanceOverlayVisible;
            set => SetProperty(ref _isPerformanceOverlayVisible, value);
        }

        // --- Performance Properties ---
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

        public float CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public float CpuTemp
        {
            get => _cpuTemp;
            set => SetProperty(ref _cpuTemp, value);
        }

        public float RamUsedGB
        {
            get => _ramUsedGB;
            set => SetProperty(ref _ramUsedGB, value);
        }

        public float RamTotalGB
        {
            get => _ramTotalGB;
            set => SetProperty(ref _ramTotalGB, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        // --- Toast ---
        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set => SetProperty(ref _isToastVisible, value);
        }

        // --- Commands ---
        public ICommand NavigateCommand { get; }
        public ICommand ToggleGameModeCommand { get; }
        public ICommand ToggleQuickSettingsCommand { get; }
        public ICommand TogglePerformanceOverlayCommand { get; }
        public ICommand SleepCommand { get; }
        public ICommand ShutdownCommand { get; }

        public MainViewModel()
        {
            // Initialize services
            _settingsService = new SettingsService();
            _taskbarService = new TaskbarService();
            _powerService = new PowerService();
            _hotkeyService = new HotkeyService();
            _displayService = new DisplayService();
            _audioService = new AudioService();
            _gameLauncherService = new GameLauncherService();

            // Initialize child ViewModels
            GameLibrary = new GameLibraryViewModel(_gameLauncherService, _settingsService);
            Settings = new SettingsViewModel(_displayService, _audioService, _powerService, _settingsService);

            // Commands
            NavigateCommand = new RelayCommand(p => Navigate(p as string ?? "Home"));
            ToggleGameModeCommand = new RelayCommand(ToggleGameMode);
            ToggleQuickSettingsCommand = new RelayCommand(() => IsQuickSettingsOpen = !IsQuickSettingsOpen);
            TogglePerformanceOverlayCommand = new RelayCommand(() => IsPerformanceOverlayVisible = !IsPerformanceOverlayVisible);
            SleepCommand = new RelayCommand(() => _powerService.Sleep());
            ShutdownCommand = new RelayCommand(() => _powerService.Shutdown());

            // Hotkey events
            _hotkeyService.GameModeToggleRequested += (s, e) =>
                System.Windows.Application.Current?.Dispatcher.Invoke(ToggleGameMode);
            _hotkeyService.QuickSettingsRequested += (s, e) =>
                System.Windows.Application.Current?.Dispatcher.Invoke(() => IsQuickSettingsOpen = !IsQuickSettingsOpen);
            _hotkeyService.PerformanceOverlayRequested += (s, e) =>
                System.Windows.Application.Current?.Dispatcher.Invoke(() => IsPerformanceOverlayVisible = !IsPerformanceOverlayVisible);

            // Performance monitoring timer
            _performanceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _performanceTimer.Tick += OnPerformanceTick;
            _performanceTimer.Start();

            // Initial state
            UpdatePerformance();
            UpdateTime();

            // Enter game mode if configured
            if (_settingsService.Settings.StartInGameMode)
                EnterGameMode();

            LoggingService.Instance.Info("MainViewModel initialized.");
        }

        public void InitializeHotkeys(IntPtr windowHandle)
        {
            _hotkeyService.Initialize(windowHandle);
        }

        private void Navigate(string view)
        {
            CurrentView = view;
            LoggingService.Instance.Info($"Navigated to: {view}");
        }

        public void ToggleGameMode()
        {
            if (IsGameMode)
                ExitGameMode();
            else
                EnterGameMode();
        }

        private void EnterGameMode()
        {
            IsGameMode = true;

            if (_settingsService.Settings.HideTaskbarInGameMode)
                _taskbarService.HideTaskbar();

            if (_settingsService.Settings.SuppressNotifications)
                _taskbarService.SuppressNotifications();

            ShowToast("🎮 Game Mode Activated");
            LoggingService.Instance.Info("Entered Game Mode.");
        }

        private void ExitGameMode()
        {
            IsGameMode = false;
            _taskbarService.ShowTaskbar();
            _taskbarService.RestoreNotifications();

            ShowToast("🖥️ Desktop Mode");
            LoggingService.Instance.Info("Exited Game Mode.");
        }

        private void OnPerformanceTick(object? sender, EventArgs e)
        {
            UpdatePerformance();
            UpdateTime();
        }

        private void UpdatePerformance()
        {
            try
            {
                var (percent, isCharging, _) = _powerService.GetBatteryStatus();
                BatteryPercent = percent;
                IsCharging = isCharging;

                // Lightweight CPU/RAM check
                var snapshot = _powerService.GetPerformanceSnapshot();
                CpuUsage = snapshot.CpuUsage;
                CpuTemp = snapshot.CpuTemperature;
                RamUsedGB = snapshot.RamUsedGB;
                RamTotalGB = snapshot.RamTotalGB;
            }
            catch { }
        }

        private void UpdateTime()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm");
        }

        public void ShowToast(string message)
        {
            ToastMessage = message;
            IsToastVisible = true;

            // Auto-hide after 3 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) =>
            {
                IsToastVisible = false;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// Cleanup on application exit.
        /// </summary>
        public void Cleanup()
        {
            _performanceTimer.Stop();
            _taskbarService.RestoreAll();
            _hotkeyService.Dispose();
            _audioService.Dispose();
            _settingsService.Save();
            LoggingService.Instance.Info("Application cleanup completed.");
        }
    }
}
