namespace WinGameOS.Models
{
    /// <summary>
    /// Represents a display mode (resolution + refresh rate).
    /// </summary>
    public class DisplayMode
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRate { get; set; }
        public int BitsPerPixel { get; set; } = 32;

        public string Resolution => $"{Width}x{Height}";
        public string FullDescription => $"{Width}x{Height} @ {RefreshRate}Hz";

        public override string ToString() => FullDescription;

        public override bool Equals(object? obj)
        {
            if (obj is DisplayMode other)
                return Width == other.Width && Height == other.Height && RefreshRate == other.RefreshRate;
            return false;
        }

        public override int GetHashCode() => HashCode.Combine(Width, Height, RefreshRate);
    }

    /// <summary>
    /// Audio device information.
    /// </summary>
    public class AudioDeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// System performance snapshot for overlay display.
    /// </summary>
    public class PerformanceSnapshot
    {
        public float CpuUsage { get; set; }
        public float GpuUsage { get; set; }
        public float CpuTemperature { get; set; }
        public float GpuTemperature { get; set; }
        public float RamUsedGB { get; set; }
        public float RamTotalGB { get; set; }
        public int BatteryPercent { get; set; }
        public bool IsCharging { get; set; }
        public TimeSpan BatteryTimeRemaining { get; set; }
        public int Fps { get; set; }
    }
}
