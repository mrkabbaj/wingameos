using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinGameOS.Models;

namespace WinGameOS.Services.GameScanners
{
    /// <summary>
    /// Scans for installed Xbox / Microsoft Store / Game Pass games.
    /// </summary>
    public class XboxLibraryScanner
    {
        /// <summary>
        /// Scans for Xbox / Game Pass games via known installation paths and registry.
        /// </summary>
        public List<Game> ScanGames()
        {
            var games = new List<Game>();

            try
            {
                // Method 1: Scan Xbox game install directories
                ScanXboxGameDirectories(games);

                // Method 2: Check registry for installed games
                ScanRegistryForGames(games);

                // Remove duplicates by title
                games = games
                    .GroupBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                LoggingService.Instance.Info($"Xbox scanner found {games.Count} games.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Xbox/Game Pass library scan failed", ex);
            }

            return games;
        }

        private void ScanXboxGameDirectories(List<Game> games)
        {
            // Xbox games are typically installed in XboxGames directory
            var possibleDrives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => d.RootDirectory.FullName);

            foreach (var drive in possibleDrives)
            {
                string xboxDir = Path.Combine(drive, "XboxGames");
                if (Directory.Exists(xboxDir))
                {
                    try
                    {
                        foreach (var gameDir in Directory.GetDirectories(xboxDir))
                        {
                            string gameName = Path.GetFileName(gameDir);
                            // Skip content directory
                            string contentDir = Path.Combine(gameDir, "Content");
                            if (!Directory.Exists(contentDir)) continue;

                            games.Add(new Game
                            {
                                Title = gameName,
                                Platform = GamePlatform.XboxGamePass,
                                InstallDirectory = gameDir,
                                IsInstalled = true,
                                LaunchUri = $"ms-xbl-{gameName.ToLower().Replace(" ", "")}://"
                            });
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        LoggingService.Instance.Warning($"Cannot access Xbox games directory: {xboxDir}");
                    }
                }

                // Also check WindowsApps for MS Store games
                string windowsApps = Path.Combine(drive, "Program Files", "WindowsApps");
                if (Directory.Exists(windowsApps))
                {
                    ScanWindowsApps(windowsApps, games);
                }
            }
        }

        private void ScanWindowsApps(string windowsAppsPath, List<Game> games)
        {
            try
            {
                // Known gaming publisher prefixes
                string[] gamingPublishers = {
                    "Microsoft.Xbox", "BethesdaSoftworks", "ElectronicArts",
                    "Ubisoft", "SquareEnix", "CAPCOM", "Bandai",
                    "505Games", "DeepSilver", "FocusHome",
                    "PlaydaysStudios", "RiotGames", "Playground"
                };

                foreach (var dir in Directory.GetDirectories(windowsAppsPath))
                {
                    string folderName = Path.GetFileName(dir);
                    if (gamingPublishers.Any(p => folderName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Extract readable game name
                        string gameName = ExtractGameName(folderName);
                        if (!string.IsNullOrEmpty(gameName))
                        {
                            games.Add(new Game
                            {
                                Title = gameName,
                                Platform = GamePlatform.XboxGamePass,
                                PlatformId = folderName,
                                InstallDirectory = dir,
                                IsInstalled = true
                            });
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // WindowsApps is often restricted
                LoggingService.Instance.Warning("Cannot access WindowsApps directory (access denied).");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error scanning WindowsApps", ex);
            }
        }

        private void ScanRegistryForGames(List<Game> games)
        {
            try
            {
                // Check for Xbox games via GamingServices registry
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\GamingServices\PackageRepository\Package");
                if (key != null)
                {
                    foreach (var subKeyName in key.GetSubKeyNames())
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        string? root = subKey?.GetValue("Root") as string;
                        if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                        {
                            string gameName = ExtractGameName(Path.GetFileName(root));
                            if (!string.IsNullOrEmpty(gameName))
                            {
                                games.Add(new Game
                                {
                                    Title = gameName,
                                    Platform = GamePlatform.XboxGamePass,
                                    InstallDirectory = root,
                                    IsInstalled = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to scan Xbox registry", ex);
            }
        }

        private string ExtractGameName(string folderName)
        {
            // Remove version numbers and publisher prefixes
            var parts = folderName.Split('_');
            if (parts.Length > 0)
            {
                string name = parts[0];
                // Remove publisher prefix (e.g., "BethesdaSoftworks.Starfield" -> "Starfield")
                int dotIndex = name.LastIndexOf('.');
                if (dotIndex >= 0 && dotIndex < name.Length - 1)
                    name = name.Substring(dotIndex + 1);

                // Add spaces before capital letters
                return System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1").Trim();
            }
            return folderName;
        }
    }
}
