using System;
using System.Threading;
using System.Windows;
using WinGameOS.Helpers;
using WinGameOS.Services;

namespace WinGameOS
{
    public partial class App : Application
    {
        private Mutex? _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Single instance check
            _singleInstanceMutex = new Mutex(true, "WinGameOS_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("WinGameOS is already running.", "WinGameOS",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Current.Shutdown();
                return;
            }

            LoggingService.Instance.Info("═══════════════════════════════════════════");
            LoggingService.Instance.Info("WinGameOS starting up...");
            LoggingService.Instance.Info($"Version: {typeof(App).Assembly.GetName().Version}");
            LoggingService.Instance.Info($"OS: {Environment.OSVersion}");
            LoggingService.Instance.Info("═══════════════════════════════════════════");

            // Global exception handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LoggingService.Instance.Error("Unhandled exception",
                    args.ExceptionObject as Exception ?? new Exception("Unknown error"));
            };
            DispatcherUnhandledException += (s, args) =>
            {
                LoggingService.Instance.Error("UI thread exception", args.Exception);
                args.Handled = true; // Prevent crash
            };

            base.OnStartup(e);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            LoggingService.Instance.Info("WinGameOS shutting down...");
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        }
    }
}
