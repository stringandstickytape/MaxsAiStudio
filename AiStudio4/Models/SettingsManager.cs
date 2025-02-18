using AiTool3;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Text.Json;

namespace AiStudio4
{
    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private SettingsSet _currentSettings;

        public SettingsSet CurrentSettings => _currentSettings;

        public SettingsManager(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "settings.json");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));

            LoadSettings();
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
    }
}