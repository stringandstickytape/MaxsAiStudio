using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using AiStudio4.McpStandalone.Services;

namespace AiStudio4.McpStandalone.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly StandaloneSettingsService _settingsService;
        private string? _originalYouTubeKey;
        private string? _originalAzurePat;
        private string? _originalGitHubToken;

        public SettingsWindow(StandaloneSettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load existing settings
            _originalYouTubeKey = _settingsService.GetDecryptedYouTubeApiKey();
            _originalAzurePat = _settingsService.GetDecryptedAzureDevOpsPAT();
            _originalGitHubToken = _settingsService.GetDecryptedGitHubToken();
            var projectPath = _settingsService.GetProjectPath();
            var mcpPort = _settingsService.GetMcpServerPort();
            var oauthPort = _settingsService.GetOAuthServerPort();
            var serverName = _settingsService.GetServerName();
            var serverDescription = _settingsService.GetServerDescription();

            // Load the actual values into the controls
            ServerNameBox.Text = serverName ?? string.Empty;
            ServerDescriptionBox.Text = serverDescription ?? string.Empty;
            YouTubeApiKeyBox.Password = _originalYouTubeKey ?? string.Empty;
            AzureDevOpsPATBox.Password = _originalAzurePat ?? string.Empty;
            GitHubTokenBox.Password = _originalGitHubToken ?? string.Empty;
            ProjectPathBox.Text = projectPath ?? string.Empty;
            McpPortBox.Value = mcpPort;
            OAuthPortBox.Value = oauthPort;
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
            // Save server configuration
            _settingsService.SetServerName(ServerNameBox.Text);
            _settingsService.SetServerDescription(ServerDescriptionBox.Text);
            
            // Save YouTube API Key if changed
            if (YouTubeApiKeyBox.Password != _originalYouTubeKey)
            {
                _settingsService.SetYouTubeApiKey(string.IsNullOrEmpty(YouTubeApiKeyBox.Password) ? null : YouTubeApiKeyBox.Password);
            }

            // Save Azure DevOps PAT if changed
            if (AzureDevOpsPATBox.Password != _originalAzurePat)
            {
                _settingsService.SetAzureDevOpsPAT(string.IsNullOrEmpty(AzureDevOpsPATBox.Password) ? null : AzureDevOpsPATBox.Password);
            }

            // Save GitHub Token if changed
            if (GitHubTokenBox.Password != _originalGitHubToken)
            {
                _settingsService.SetGitHubToken(string.IsNullOrEmpty(GitHubTokenBox.Password) ? null : GitHubTokenBox.Password);
            }

            _settingsService.SetProjectPath(ProjectPathBox.Text);
            
            // Save port settings
            if (McpPortBox.Value.HasValue)
            {
                _settingsService.SetMcpServerPort((int)McpPortBox.Value.Value);
            }
            if (OAuthPortBox.Value.HasValue)
            {
                _settingsService.SetOAuthServerPort((int)OAuthPortBox.Value.Value);
            }

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