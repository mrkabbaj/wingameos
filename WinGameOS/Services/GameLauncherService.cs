using System;
using System.Diagnostics;
using WinGameOS.Models;

namespace WinGameOS.Services
{
    /// <summary>
    /// Handles launching games from the library on different platforms.
    /// </summary>
    public class GameLauncherService
    {
        public event EventHandler<Game>? GameLaunched;
        public event EventHandler<(Game Game, string Error)>? GameLaunchFailed;

        /// <summary>
        /// Launches a game based on its platform and configuration.
        /// </summary>
        public bool LaunchGame(Game game)
        {
            try
            {
                LoggingService.Instance.Info($"Launching game: {game.Title} ({game.Platform})");

                bool success = game.Platform switch
                {
                    GamePlatform.Steam => LaunchSteamGame(game),
                    GamePlatform.EpicGames => LaunchEpicGame(game),
                    GamePlatform.XboxGamePass => LaunchXboxGame(game),
                    GamePlatform.Manual => LaunchManualGame(game),
                    _ => LaunchManualGame(game)
                };

                if (success)
                {
                    game.LastPlayed = DateTime.Now;
                    GameLaunched?.Invoke(this, game);
                    LoggingService.Instance.Info($"Game launched successfully: {game.Title}");
                }

                return success;
            }
            catch (Exception ex)
            {
                string error = $"Failed to launch {game.Title}: {ex.Message}";
                LoggingService.Instance.Error(error, ex);
                GameLaunchFailed?.Invoke(this, (game, error));
                return false;
            }
        }

        private bool LaunchSteamGame(Game game)
        {
            // Prefer Steam URI protocol for reliable launching
            if (!string.IsNullOrEmpty(game.LaunchUri))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = game.LaunchUri,
                    UseShellExecute = true
                });
                return true;
            }

            // Fallback to steam:// protocol with AppId
            if (!string.IsNullOrEmpty(game.PlatformId))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{game.PlatformId}",
                    UseShellExecute = true
                });
                return true;
            }

            // Final fallback: launch executable directly
            return LaunchManualGame(game);
        }

        private bool LaunchEpicGame(Game game)
        {
            // Use Epic Games launch URI
            if (!string.IsNullOrEmpty(game.LaunchUri))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = game.LaunchUri,
                    UseShellExecute = true
                });
                return true;
            }

            // Fallback to executable
            return LaunchManualGame(game);
        }

        private bool LaunchXboxGame(Game game)
        {
            // Xbox games use MS protocol URIs or shell:AppsFolder
            if (!string.IsNullOrEmpty(game.LaunchUri))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = game.LaunchUri,
                    UseShellExecute = true
                });
                return true;
            }

            // Try shell:AppsFolder with PlatformId
            if (!string.IsNullOrEmpty(game.PlatformId))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"shell:AppsFolder\\{game.PlatformId}",
                    UseShellExecute = true
                });
                return true;
            }

            return false;
        }

        private bool LaunchManualGame(Game game)
        {
            if (!string.IsNullOrEmpty(game.ExecutablePath) &&
                System.IO.File.Exists(game.ExecutablePath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = game.ExecutablePath,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(game.ExecutablePath) ?? ""
                };

                if (!string.IsNullOrEmpty(game.LaunchArguments))
                    startInfo.Arguments = game.LaunchArguments;

                Process.Start(startInfo);
                return true;
            }

            string error = $"Executable not found: {game.ExecutablePath}";
            LoggingService.Instance.Error(error);
            GameLaunchFailed?.Invoke(this, (game, error));
            return false;
        }
    }
}
