using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using WinGameOS.Models;
using WinGameOS.ViewModels;

namespace WinGameOS.Views
{
    public partial class GameLibraryView : UserControl
    {
        public GameLibraryView()
        {
            InitializeComponent();
        }

        private GameLibraryViewModel? VM => DataContext as GameLibraryViewModel;

        private void AddGameButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Game Executable",
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                VM?.AddManualGame(fileName, dialog.FileName);
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filter && VM != null)
            {
                VM.SelectedPlatformFilter = filter;
            }
        }

        private void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Game game && VM != null)
            {
                VM.SelectedGame = game;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Game game && VM != null)
            {
                VM.SelectedGame = game;
                VM.LaunchGameCommand.Execute(null);
            }
        }
    }
}
