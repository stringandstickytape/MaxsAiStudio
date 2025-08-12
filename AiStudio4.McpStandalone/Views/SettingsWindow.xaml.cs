using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.Services;

namespace AiStudio4.McpStandalone.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly StandaloneSettingsService _settingsService;
        private bool _showingYouTubeKey = false;
        private bool _showingAzurePat = false;
        private bool _showingGitHubToken = false;

        public SettingsWindow(StandaloneSettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load existing settings
            var youtubeKey = _settingsService.GetDecryptedYouTubeApiKey();
            var azurePat = _settingsService.GetDecryptedAzureDevOpsPAT();
            var githubToken = _settingsService.GetDecryptedGitHubToken();
            var projectPath = _settingsService.GetProjectPath();

            // Set placeholders for existing keys (don't show actual values)
            if (!string.IsNullOrEmpty(youtubeKey))
            {
                YouTubeApiKeyBox.Password = "********";
            }
            if (!string.IsNullOrEmpty(azurePat))
            {
                AzureDevOpsPATBox.Password = "********";
            }
            if (!string.IsNullOrEmpty(githubToken))
            {
                GitHubTokenBox.Password = "********";
            }

            ProjectPathBox.Text = projectPath ?? string.Empty;
        }

        private void ShowYouTubeKey_Click(object sender, RoutedEventArgs e)
        {
            if (_showingYouTubeKey)
            {
                // Hide the key
                YouTubeApiKeyBox.Password = "********";
                ShowYouTubeKeyButton.Content = "Show";
                _showingYouTubeKey = false;
            }
            else
            {
                // Show the actual key
                var key = _settingsService.GetDecryptedYouTubeApiKey();
                if (!string.IsNullOrEmpty(key))
                {
                    YouTubeApiKeyBox.Password = key;
                    ShowYouTubeKeyButton.Content = "Hide";
                    _showingYouTubeKey = true;
                }
            }
        }

        private void ShowAzurePat_Click(object sender, RoutedEventArgs e)
        {
            if (_showingAzurePat)
            {
                // Hide the PAT
                AzureDevOpsPATBox.Password = "********";
                ShowAzurePatButton.Content = "Show";
                _showingAzurePat = false;
            }
            else
            {
                // Show the actual PAT
                var pat = _settingsService.GetDecryptedAzureDevOpsPAT();
                if (!string.IsNullOrEmpty(pat))
                {
                    AzureDevOpsPATBox.Password = pat;
                    ShowAzurePatButton.Content = "Hide";
                    _showingAzurePat = true;
                }
            }
        }

        private void ShowGitHubToken_Click(object sender, RoutedEventArgs e)
        {
            if (_showingGitHubToken)
            {
                // Hide the token
                GitHubTokenBox.Password = "********";
                ShowGitHubTokenButton.Content = "Show";
                _showingGitHubToken = false;
            }
            else
            {
                // Show the actual token
                var token = _settingsService.GetDecryptedGitHubToken();
                if (!string.IsNullOrEmpty(token))
                {
                    GitHubTokenBox.Password = token;
                    ShowGitHubTokenButton.Content = "Hide";
                    _showingGitHubToken = true;
                }
            }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Working Directory",
                InitialDirectory = ProjectPathBox.Text
            };

            if (dialog.ShowDialog() == true)
            {
                ProjectPathBox.Text = dialog.FolderName;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Only save if the value was changed (not placeholder)
            if (YouTubeApiKeyBox.Password != "********" && !string.IsNullOrEmpty(YouTubeApiKeyBox.Password))
            {
                _settingsService.SetYouTubeApiKey(YouTubeApiKeyBox.Password);
            }
            else if (string.IsNullOrEmpty(YouTubeApiKeyBox.Password))
            {
                // Clear the key if empty
                _settingsService.SetYouTubeApiKey(null);
            }

            if (AzureDevOpsPATBox.Password != "********" && !string.IsNullOrEmpty(AzureDevOpsPATBox.Password))
            {
                _settingsService.SetAzureDevOpsPAT(AzureDevOpsPATBox.Password);
            }
            else if (string.IsNullOrEmpty(AzureDevOpsPATBox.Password))
            {
                _settingsService.SetAzureDevOpsPAT(null);
            }

            if (GitHubTokenBox.Password != "********" && !string.IsNullOrEmpty(GitHubTokenBox.Password))
            {
                _settingsService.SetGitHubToken(GitHubTokenBox.Password);
            }
            else if (string.IsNullOrEmpty(GitHubTokenBox.Password))
            {
                _settingsService.SetGitHubToken(null);
            }

            _settingsService.SetProjectPath(ProjectPathBox.Text);

            MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}