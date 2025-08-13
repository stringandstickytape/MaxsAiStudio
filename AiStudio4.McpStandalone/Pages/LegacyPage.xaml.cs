using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.Views;
using AiStudio4.McpStandalone.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.McpStandalone.Pages
{
    /// <summary>
    /// Interaction logic for LegacyPage.xaml
    /// </summary>
    public partial class LegacyPage : Page
    {
        public LegacyPage()
        {
            InitializeComponent();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the settings service from the application's service provider
                var app = Application.Current as App;
                if (app?.ServiceProvider != null)
                {
                    var settingsService = app.ServiceProvider.GetRequiredService<StandaloneSettingsService>();
                    var settingsWindow = new SettingsWindow(settingsService);
                    settingsWindow.Owner = Window.GetWindow(this);
                    settingsWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Unable to access application services.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyClaudeCommand_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                Clipboard.SetText(viewModel.ClaudeInstallCommand);
                // Could add a notification here that text was copied
            }
        }
    }
}