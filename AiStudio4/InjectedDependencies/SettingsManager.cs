using AiTool3;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Text.Json;

namespace AiStudio4.InjectedDependencies
{
    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private SettingsSet _currentSettings;
        private DefaultSettings _defaultSettings;

        public SettingsSet CurrentSettings => _currentSettings;
        public DefaultSettings DefaultSettings => _defaultSettings;

        public SettingsManager(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "settings.json");

            // Path for default settings
            var defaultSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "defaultSettings.json");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));

            LoadSettings();
            LoadDefaultSettings(defaultSettingsPath);
        }

        public void LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                    string jsonContent = File.ReadAllText(_settingsFilePath);
                    _currentSettings = JsonConvert.DeserializeObject<SettingsSet>(jsonContent);
            }
            else
            {
                _currentSettings = new SettingsSet();
                SaveSettings();
            }
        }

        private void LoadDefaultSettings(string defaultSettingsPath)
        {
            if (File.Exists(defaultSettingsPath))
            {
                string jsonContent = File.ReadAllText(defaultSettingsPath);
                _defaultSettings = JsonConvert.DeserializeObject<DefaultSettings>(jsonContent);
            }
            else
            {
                _defaultSettings = new DefaultSettings();
                SaveDefaultSettings(defaultSettingsPath);
            }
        }

        private void SaveDefaultSettings(string defaultSettingsPath)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_defaultSettings);
                File.WriteAllText(defaultSettingsPath, jsonContent);
            }
            catch (Exception)
            {
                // Handle or log error as needed
            }
        }

        public void SaveSettings()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_currentSettings);
                File.WriteAllText(_settingsFilePath, jsonContent);
            }
            catch (Exception)
            {
                // Handle or log error as needed
            }
        }

        public void UpdateSettings(SettingsSet newSettings)
        {
            _currentSettings = newSettings;
            SaveSettings();
        }

        public void UpdateDefaultModel(string modelName)
        {
            if (_defaultSettings == null)
            {
                _defaultSettings = new DefaultSettings();
            }
            
            _defaultSettings.DefaultModel = modelName;
            
            string defaultSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "defaultSettings.json");
                
            SaveDefaultSettings(defaultSettingsPath);
        }

        public void UpdateSecondaryModel(string modelName)
        {
            if (_defaultSettings == null)
            {
                _defaultSettings = new DefaultSettings();
            }
            
            _defaultSettings.SecondaryModel = modelName;
            
            string defaultSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "defaultSettings.json");
                
            SaveDefaultSettings(defaultSettingsPath);
        }
    }
}