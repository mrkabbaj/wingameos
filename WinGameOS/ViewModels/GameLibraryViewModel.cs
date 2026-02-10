using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WinGameOS.Models;
using WinGameOS.Services;
using WinGameOS.Services.GameScanners;

namespace WinGameOS.ViewModels
{
    /// <summary>
    /// ViewModel for the Game Library view.
    /// </summary>
    public class GameLibraryViewModel : ViewModelBase
    {
        private readonly GameLauncherService _launcher;
        private readonly SettingsService _settings;

        private ObservableCollection<Game> _allGames = new();
        private ObservableCollection<Game> _filteredGames = new();
        private Game? _selectedGame;
        private string _searchQuery = string.Empty;
        private string _selectedPlatformFilter = "All";
        private bool _isScanning;
        private string _statusMessage = string.Empty;

        public ObservableCollection<Game> FilteredGames
        {
            get => _filteredGames;
            set => SetProperty(ref _filteredGames, value);
        }

        public Game? SelectedGame
        {
            get => _selectedGame;
            set => SetProperty(ref _selectedGame, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set { SetProperty(ref _searchQuery, value); ApplyFilters(); }
        }

        public string SelectedPlatformFilter
        {
            get => _selectedPlatformFilter;
            set { SetProperty(ref _selectedPlatformFilter, value); ApplyFilters(); }
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public List<string> PlatformFilters { get; } = new()
        {
            "All", "Steam", "Epic Games", "Xbox Game Pass", "Manual"
        };

        public int TotalGamesCount => _allGames.Count;

        // Commands
        public ICommand ScanGamesCommand { get; }
        public ICommand LaunchGameCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }
        public ICommand AddManualGameCommand { get; }
        public ICommand RefreshCommand { get; }

        public GameLibraryViewModel(GameLauncherService launcher, SettingsService settings)
        {
            _launcher = launcher;
            _settings = settings;

            ScanGamesCommand = new RelayCommand(ScanAllPlatforms);
            LaunchGameCommand = new RelayCommand(LaunchSelectedGame, () => SelectedGame != null);
            ToggleFavoriteCommand = new RelayCommand(ToggleFavorite);
            AddManualGameCommand = new RelayCommand(AddManualGame);
            RefreshCommand = new RelayCommand(ScanAllPlatforms);

            _launcher.GameLaunched += (s, game) => StatusMessage = $"🎮 {game.Title} launched!";
            _launcher.GameLaunchFailed += (s, e) => StatusMessage = $"❌ {e.Error}";

            // Load manual games from settings
            foreach (var game in _settings.Settings.ManualGames)
                _allGames.Add(game);

            ApplyFilters();
        }

        /// <summary>
        /// Scans all platforms for installed games.
        /// </summary>
        public void ScanAllPlatforms()
        {
            IsScanning = true;
            StatusMessage = "Scanning for games...";

            try
            {
                // Keep manual games
                var manualGames = _allGames.Where(g => g.Platform == GamePlatform.Manual).ToList();
                _allGames.Clear();
                foreach (var mg in manualGames)
                    _allGames.Add(mg);

                // Scan Steam
                StatusMessage = "Scanning Steam library...";
                var steamScanner = new SteamLibraryScanner();
                foreach (var game in steamScanner.ScanGames())
                    _allGames.Add(game);

                // Scan Epic Games
                StatusMessage = "Scanning Epic Games library...";
                var epicScanner = new EpicLibraryScanner();
                foreach (var game in epicScanner.ScanGames())
                    _allGames.Add(game);

                // Scan Xbox / Game Pass
                StatusMessage = "Scanning Xbox / Game Pass library...";
                var xboxScanner = new XboxLibraryScanner();
                foreach (var game in xboxScanner.ScanGames())
                    _allGames.Add(game);

                ApplyFilters();
                StatusMessage = $"✅ Found {_allGames.Count} games across all platforms.";
                OnPropertyChanged(nameof(TotalGamesCount));
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Scan error: {ex.Message}";
                LoggingService.Instance.Error("Game scan failed", ex);
            }
            finally
            {
                IsScanning = false;
            }
        }

        private void LaunchSelectedGame()
        {
            if (SelectedGame != null)
                _launcher.LaunchGame(SelectedGame);
        }

        private void ToggleFavorite()
        {
            if (SelectedGame != null)
            {
                SelectedGame.IsFavorite = !SelectedGame.IsFavorite;
                OnPropertyChanged(nameof(SelectedGame));
                ApplyFilters();
            }
        }

        public void AddManualGame()
        {
            // This will be called from the View with a file dialog
        }

        /// <summary>
        /// Adds a manually specified game to the library.
        /// </summary>
        public void AddManualGame(string title, string executablePath, string arguments = "")
        {
            var game = new Game
            {
                Title = title,
                ExecutablePath = executablePath,
                LaunchArguments = arguments,
                Platform = GamePlatform.Manual,
                IsInstalled = System.IO.File.Exists(executablePath)
            };

            _allGames.Add(game);
            _settings.Settings.ManualGames.Add(game);
            _settings.Save();
            ApplyFilters();
            StatusMessage = $"✅ Added: {title}";
            OnPropertyChanged(nameof(TotalGamesCount));
        }

        private void ApplyFilters()
        {
            var filtered = _allGames.AsEnumerable();

            // Platform filter
            if (_selectedPlatformFilter != "All")
            {
                GamePlatform? platform = _selectedPlatformFilter switch
                {
                    "Steam" => GamePlatform.Steam,
                    "Epic Games" => GamePlatform.EpicGames,
                    "Xbox Game Pass" => GamePlatform.XboxGamePass,
                    "Manual" => GamePlatform.Manual,
                    _ => null
                };
                if (platform.HasValue)
                    filtered = filtered.Where(g => g.Platform == platform.Value);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                filtered = filtered.Where(g =>
                    g.Title.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Sort: favorites first, then alphabetically
            filtered = filtered
                .OrderByDescending(g => g.IsFavorite)
                .ThenBy(g => g.Title);

            FilteredGames = new ObservableCollection<Game>(filtered);
        }

        /// <summary>
        /// Get recently played games (for the home screen).
        /// </summary>
        public IEnumerable<Game> GetRecentGames(int count = 6)
        {
            return _allGames
                .Where(g => g.LastPlayed.HasValue)
                .OrderByDescending(g => g.LastPlayed)
                .Take(count);
        }

        /// <summary>
        /// Get favorite games.
        /// </summary>
        public IEnumerable<Game> GetFavoriteGames()
        {
            return _allGames.Where(g => g.IsFavorite);
        }
    }
}
