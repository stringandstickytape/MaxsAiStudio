using System;
using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.Views;
using AiStudio4.McpStandalone.Services;
using AiStudio4.McpStandalone.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace AiStudio4.McpStandalone.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private StandaloneSettingsService? _settingsService;
        
        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the settings service and load settings
            var app = System.Windows.Application.Current as App;
            if (app?.ServiceProvider != null)
            {
                _settingsService = app.ServiceProvider.GetRequiredService<StandaloneSettingsService>();
                LoadSettings();
            }
        }
        
        private void LoadSettings()
        {
            if (_settingsService == null) return;
            
            // Server Configuration
            ServerNameBox.Text = _settingsService.GetServerName();
            ServerDescriptionBox.Text = _settingsService.GetServerDescription();
            McpPortBox.Value = _settingsService.GetMcpServerPort();
            OAuthPortBox.Value = _settingsService.GetOAuthServerPort();
            
            // API Keys
            YouTubeApiKeyBox.Password = _settingsService.GetDecryptedYouTubeApiKey() ?? "";
            AzureDevOpsPATBox.Password = _settingsService.GetDecryptedAzureDevOpsPAT() ?? "";
            GitHubTokenBox.Password = _settingsService.GetDecryptedGitHubToken() ?? "";
            
            // Working Directory
            ProjectPathBox.Text = _settingsService.GetProjectPath();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService == null) return;
            
            try
            {
                // Save Server Configuration
                _settingsService.SetServerName(ServerNameBox.Text);
                _settingsService.SetServerDescription(ServerDescriptionBox.Text);
                _settingsService.SetMcpServerPort((int)(McpPortBox.Value ?? 7071));
                _settingsService.SetOAuthServerPort((int)(OAuthPortBox.Value ?? 5000));
                
                // Save API Keys
                if (!string.IsNullOrWhiteSpace(YouTubeApiKeyBox.Password))
                {
                    _settingsService.SetYouTubeApiKey(YouTubeApiKeyBox.Password);
                }
                
                if (!string.IsNullOrWhiteSpace(AzureDevOpsPATBox.Password))
                {
                    _settingsService.SetAzureDevOpsPAT(AzureDevOpsPATBox.Password);
                }
                
                if (!string.IsNullOrWhiteSpace(GitHubTokenBox.Password))
                {
                    _settingsService.SetGitHubToken(GitHubTokenBox.Password);
                }
                
                // Save Working Directory
                _settingsService.SetProjectPath(ProjectPathBox.Text);
                
                // Settings are automatically saved by each Set method
                
                // Update the view model if it's bound
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.SelectedServer.Name = ServerNameBox.Text;
                    viewModel.SelectedServer.Description = ServerDescriptionBox.Text;
                    viewModel.McpServerPort = (int)(McpPortBox.Value ?? 7071);
                    viewModel.OAuthServerPort = (int)(OAuthPortBox.Value ?? 5000);
                }
                
                // Show success message (could use a snackbar or notification)
                System.Windows.MessageBox.Show("Settings saved successfully!", "Success", 
                    System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Project Directory"
            };
            
            if (!string.IsNullOrWhiteSpace(ProjectPathBox.Text))
            {
                dialog.InitialDirectory = ProjectPathBox.Text;
            }
            
            if (dialog.ShowDialog() == true)
            {
                ProjectPathBox.Text = dialog.FolderName;
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            // This method is no longer needed since we're removing the settings dialog
        }
    }
}