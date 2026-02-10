using System;
using System.Windows;
using System.Windows.Controls;
using WinGameOS.Models;
using WinGameOS.ViewModels;

namespace WinGameOS.Views
{
    public partial class QuickSettingsOverlay : UserControl
    {
        public QuickSettingsOverlay()
        {
            InitializeComponent();
        }

        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        private void SetPerfMode(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string modeStr && VM != null)
            {
                if (Enum.TryParse<PerformanceMode>(modeStr, out var mode))
                {
                    VM.PerformanceMode = mode;
                }
            }
        }
    }
}
