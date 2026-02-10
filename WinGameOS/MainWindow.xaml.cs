using System;
using System.Windows;
using System.Windows.Interop;
using WinGameOS.ViewModels;

namespace WinGameOS
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel = null!;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize global hotkeys with window handle
            var handle = new WindowInteropHelper(this).Handle;
            _viewModel.InitializeHotkeys(handle);

            // Default: show Home view
            ShowView("Home");

            // Scan games on startup
            _viewModel.GameLibrary.ScanAllPlatforms();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Cleanup();
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.CommandParameter is string view)
            {
                ShowView(view);
            }
        }

        private void ShowView(string viewName)
        {
            // Hide all views
            GameModeView.Visibility = Visibility.Collapsed;
            LibraryView.Visibility = Visibility.Collapsed;
            SettingsViewPanel.Visibility = Visibility.Collapsed;

            // Show requested view
            switch (viewName)
            {
                case "Home":
                    GameModeView.Visibility = Visibility.Visible;
                    break;
                case "Library":
                    LibraryView.Visibility = Visibility.Visible;
                    break;
                case "Settings":
                    SettingsViewPanel.Visibility = Visibility.Visible;
                    _viewModel.Settings.RefreshAll();
                    break;
            }
        }
    }
}
