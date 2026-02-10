using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinGameOS.Models;

namespace WinGameOS.Services.GameScanners
{
    /// <summary>
    /// Scans for installed Epic Games Store games by reading manifest files.
    /// </summary>
    public class EpicLibraryScanner
    {
        private static readonly string ManifestsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Epic", "EpicGamesLauncher", "Data", "Manifests");

        /// <summary>
        /// Scans Epic Games manifests for installed games.
        /// </summary>
        public List<Game> ScanGames()
        {
            var games = new List<Game>();

            try
            {
                if (!Directory.Exists(ManifestsPath))
                {
                    LoggingService.Instance.Info("Epic Games manifests directory not found.");
                    return games;
                }

                var manifestFiles = Directory.GetFiles(ManifestsPath, "*.item");
                foreach (var file in manifestFiles)
                {
                    var game = ParseManifest(file);
                    if (game != null)
                        games.Add(game);
                }

                LoggingService.Instance.Info($"Epic scanner found {games.Count} games.");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Epic Games library scan failed", ex);
            }

            return games;
        }

        private Game? ParseManifest(string manifestPath)
        {
            try
            {
                string json = File.ReadAllText(manifestPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string displayName = GetStringProp(root, "DisplayName");
                string installLocation = GetStringProp(root, "InstallLocation");
                string launchExecutable = GetStringProp(root, "LaunchExecutable");
                string catalogItemId = GetStringProp(root, "CatalogItemId");
                string appName = GetStringProp(root, "AppName");

                if (string.IsNullOrEmpty(displayName))
                    return null;

                // Skip non-game apps (e.g., Unreal Engine, launchers)
                if (displayName.Contains("Unreal Engine", StringComparison.OrdinalIgnoreCase) ||
                    displayName.Contains("DirectX", StringComparison.OrdinalIgnoreCase))
                    return null;

                string exePath = string.Empty;
                if (!string.IsNullOrEmpty(installLocation) && !string.IsNullOrEmpty(launchExecutable))
                    exePath = Path.Combine(installLocation, launchExecutable);

                return new Game
                {
                    Title = displayName,
                    Platform = GamePlatform.EpicGames,
                    PlatformId = catalogItemId,
                    InstallDirectory = installLocation,
                    ExecutablePath = exePath,
                    LaunchUri = $"com.epicgames.launcher://apps/{appName}?action=launch&silent=true",
                    IsInstalled = !string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation)
                };
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to parse Epic manifest: {manifestPath}", ex);
                return null;
            }
        }

        private static string GetStringProp(JsonElement root, string propertyName)
        {
            if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? string.Empty;
            return string.Empty;
        }
    }
}
