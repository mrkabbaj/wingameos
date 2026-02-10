using System;
using System.Text.Json.Serialization;

namespace WinGameOS.Models
{
    /// <summary>
    /// Represents a game in the library.
    /// </summary>
    public class Game
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string InstallDirectory { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string BannerPath { get; set; } = string.Empty;
        public string Category { get; set; } = "Uncategorized";

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GamePlatform Platform { get; set; } = GamePlatform.Manual;

        public DateTime? LastPlayed { get; set; }
        public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;
        public bool IsFavorite { get; set; }
        public bool IsInstalled { get; set; } = true;

        /// <summary>
        /// Platform-specific app ID (Steam AppId, Epic CatalogItemId, etc.)
        /// </summary>
        public string PlatformId { get; set; } = string.Empty;

        /// <summary>
        /// Launch arguments for the game executable.
        /// </summary>
        public string LaunchArguments { get; set; } = string.Empty;

        /// <summary>
        /// URI for platform-based launches (e.g., steam://rungameid/12345)
        /// </summary>
        public string LaunchUri { get; set; } = string.Empty;

        public override string ToString() => $"{Title} ({Platform})";
    }
}
