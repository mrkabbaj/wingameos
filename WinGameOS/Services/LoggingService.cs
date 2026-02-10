using System;
using System.IO;

namespace WinGameOS.Services
{
    /// <summary>
    /// Internal logging service with daily log file rotation.
    /// </summary>
    public class LoggingService
    {
        private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());
        public static LoggingService Instance => _instance.Value;

        private readonly string _logDirectory;
        private readonly object _lock = new();

        public enum LogLevel { Info, Warning, Error, Debug }

        private LoggingService()
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinGameOS", "logs");

            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);
        }

        public void Info(string message) => Write(LogLevel.Info, message);
        public void Warning(string message) => Write(LogLevel.Warning, message);
        public void Error(string message) => Write(LogLevel.Error, message);
        public void Debug(string message) => Write(LogLevel.Debug, message);

        public void Error(string message, Exception ex)
        {
            Write(LogLevel.Error, $"{message}\n  Exception: {ex.GetType().Name}: {ex.Message}\n  StackTrace: {ex.StackTrace}");
        }

        private void Write(LogLevel level, string message)
        {
            try
            {
                lock (_lock)
                {
                    string fileName = $"wingameos_{DateTime.Now:yyyy-MM-dd}.log";
                    string filePath = Path.Combine(_logDirectory, fileName);
                    string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] [{level,-7}] {message}";

                    File.AppendAllText(filePath, logEntry + Environment.NewLine);

                    // Cleanup old logs (keep last 7 days)
                    CleanOldLogs();
                }
            }
            catch
            {
                // Logging should never crash the app
            }
        }

        private void CleanOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(_logDirectory, "wingameos_*.log");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < DateTime.Now.AddDays(-7))
                        fileInfo.Delete();
                }
            }
            catch { }
        }
    }
}
