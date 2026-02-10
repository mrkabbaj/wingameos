using System;
using System.Windows;
using System.Windows.Controls;
using WinGameOS.Models;
using WinGameOS.ViewModels;

namespace WinGameOS.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        private void PerfMode_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string modeStr && VM != null)
            {
                if (Enum.TryParse<PerformanceMode>(modeStr, out var mode))
                {
                    VM.PerformanceMode = mode;
                }
            }
        }
    }
}
