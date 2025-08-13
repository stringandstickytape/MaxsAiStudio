using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.Views;
using AiStudio4.McpStandalone.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.McpStandalone.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
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
        }
    }
}