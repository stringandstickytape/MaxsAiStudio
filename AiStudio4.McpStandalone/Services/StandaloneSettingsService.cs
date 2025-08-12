using AiStudio4.Tools.Interfaces;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AiStudio4.McpStandalone.Services
{
    /// <summary>
    /// Minimal settings service for the standalone MCP server
    /// </summary>
    public class StandaloneSettingsService : IGeneralSettingsService
    {
        private readonly string _settingsPath;
        private StandaloneSettings _settings;

        public StandaloneSettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4.McpStandalone");
            
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
            
            LoadSettings();
        }

        public IGeneralSettings CurrentSettings => _settings;

        public string GetDecryptedYouTubeApiKey()
        {
            if (string.IsNullOrEmpty(_settings.EncryptedYouTubeApiKey))
                return null;
            
            try
            {
                var encryptedBytes = Convert.FromBase64String(_settings.EncryptedYouTubeApiKey);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return null;
            }
        }

        public string GetDecryptedAzureDevOpsPAT()
        {
            if (string.IsNullOrEmpty(_settings.EncryptedAzureDevOpsPAT))
                return null;
            
            try
            {
                var encryptedBytes = Convert.FromBase64String(_settings.EncryptedAzureDevOpsPAT);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return null;
            }
        }

        public string GetDecryptedGitHubToken()
        {
            if (string.IsNullOrEmpty(_settings.EncryptedGitHubToken))
                return null;
            
            try
            {
                var encryptedBytes = Convert.FromBase64String(_settings.EncryptedGitHubToken);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch
            {
                return null;
            }
        }

        public string GetProjectPath()
        {
            return _settings.ProjectPath;
        }

        public void SetYouTubeApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                _settings.EncryptedYouTubeApiKey = null;
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(apiKey);
                var encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                _settings.EncryptedYouTubeApiKey = Convert.ToBase64String(encryptedBytes);
            }
            SaveSettings();
        }

        public void SetAzureDevOpsPAT(string pat)
        {
            if (string.IsNullOrEmpty(pat))
            {
                _settings.EncryptedAzureDevOpsPAT = null;
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(pat);
                var encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                _settings.EncryptedAzureDevOpsPAT = Convert.ToBase64String(encryptedBytes);
            }
            SaveSettings();
        }

        public void SetGitHubToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _settings.EncryptedGitHubToken = null;
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(token);
                var encryptedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                _settings.EncryptedGitHubToken = Convert.ToBase64String(encryptedBytes);
            }
            SaveSettings();
        }

        public void SetProjectPath(string path)
        {
            _settings.ProjectPath = path;
            SaveSettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<StandaloneSettings>(json) ?? new StandaloneSettings();
                }
                catch
                {
                    _settings = new StandaloneSettings();
                }
            }
            else
            {
                _settings = new StandaloneSettings();
            }
        }

        private void SaveSettings()
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }

        private class StandaloneSettings : IGeneralSettings
        {
            public string ProjectPath { get; set; } = Environment.CurrentDirectory;
            public string EncryptedYouTubeApiKey { get; set; }
            public string EncryptedAzureDevOpsPAT { get; set; }
            public string EncryptedGitHubToken { get; set; }
        }
    }
}