using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using WinGameOS.Models;

namespace WinGameOS.Services.GameScanners
{
    /// <summary>
    /// Scans for installed Steam games by reading Steam library configuration files.
    /// </summary>
    public class SteamLibraryScanner
    {
        private static readonly string DefaultSteamPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Steam");

        /// <summary>
        /// Scans all Steam library folders for installed games.
        /// </summary>
        public List<Game> ScanGames()
        {
            var games = new List<Game>();

            try
            {
                var libraryPaths = GetLibraryPaths();
                foreach (var libraryPath in libraryPaths)
                {
                    var steamAppsPath = Path.Combine(libraryPath, "steamapps");
                    if (!Directory.Exists(steamAppsPath)) continue;

                    var manifests = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");
                    foreach (var manifest in manifests)
                    {
                        var game = ParseManifest(manifest, steamAppsPath);
                        if (game != null)
                            games.Add(game);
                    }
                }

                LoggingService.Instance.Info($"Steam scanner found {games.Count} games.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Steam library scan failed", ex);
            }

            return games;
        }

        private List<string> GetLibraryPaths()
        {
            var paths = new List<string>();

            // Default Steam installation path
            if (Directory.Exists(DefaultSteamPath))
                paths.Add(DefaultSteamPath);

            // Read libraryfolders.vdf for additional library paths
            var vdfPath = Path.Combine(DefaultSteamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                try
                {
                    string content = File.ReadAllText(vdfPath);
                    // Match "path" entries in VDF format
                    var pathRegex = new Regex(@"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
                    foreach (Match match in pathRegex.Matches(content))
                    {
                        string libPath = match.Groups[1].Value.Replace("\\\\", "\\");
                        if (Directory.Exists(libPath) && !paths.Contains(libPath))
                            paths.Add(libPath);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Error("Failed to parse libraryfolders.vdf", ex);
                }
            }

            // Also check registry for custom Steam install location
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\WOW6432Node\Valve\Steam");
                string? regPath = key?.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(regPath) && Directory.Exists(regPath) && !paths.Contains(regPath))
                    paths.Add(regPath);
            }
            catch { }

            return paths;
        }

        private Game? ParseManifest(string manifestPath, string steamAppsPath)
        {
            try
            {
                string content = File.ReadAllText(manifestPath);

                string? appId = ExtractValue(content, "appid");
                string? name = ExtractValue(content, "name");
                string? installDir = ExtractValue(content, "installdir");

                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(name))
                    return null;

                // Skip Steamworks redistributables, tools, etc.
                if (name.Contains("Redistributable", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Proton", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Steam Linux Runtime", StringComparison.OrdinalIgnoreCase))
                    return null;

                string gameDir = Path.Combine(steamAppsPath, "common", installDir ?? name);

                return new Game
                {
                    Title = name,
                    Platform = GamePlatform.Steam,
                    PlatformId = appId,
                    InstallDirectory = gameDir,
                    LaunchUri = $"steam://rungameid/{appId}",
                    IsInstalled = Directory.Exists(gameDir),
                    IconPath = GetSteamIconPath(appId)
                };
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to parse Steam manifest: {manifestPath}", ex);
                return null;
            }
        }

        private string? ExtractValue(string content, string key)
        {
            var regex = new Regex($@"""{key}""\s+""([^""]+)""", RegexOptions.IgnoreCase);
            var match = regex.Match(content);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string GetSteamIconPath(string appId)
        {
            // Steam caches game icons in the appcache/librarycache folder
            string cachePath = Path.Combine(DefaultSteamPath, "appcache", "librarycache");
            string headerPath = Path.Combine(cachePath, $"{appId}_header.jpg");
            if (File.Exists(headerPath)) return headerPath;

            string iconPath = Path.Combine(cachePath, $"{appId}_icon.jpg");
            if (File.Exists(iconPath)) return iconPath;

            return string.Empty;
        }
    }
}
