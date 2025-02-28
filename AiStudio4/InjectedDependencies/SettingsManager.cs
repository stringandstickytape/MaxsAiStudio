using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SharedClasses.Providers;
using System.IO;
using System.Text.Json;

namespace AiStudio4.InjectedDependencies
{

    public class Studio4Settings
    {
        public List<Model> ModelList { get; set; } = new List<Model>();
        public List<ServiceProvider> ServiceProviders { get; set; } = new List<ServiceProvider>();

        public float Temperature { get; set; } = 0.9f;

        public bool UseEmbeddings { get; set; } = false;

        public bool UsePromptCaching { get; set; } = true;

        public bool StreamResponses { get; set; } = false;

        public string EmbeddingsFilename { get; set; }

        public string EmbeddingModel { get; internal set; } = "mxbai-embed-large";

        public ApiSettings ToApiSettings()
        {
            return new ApiSettings
            {
                Temperature = Temperature,
                UsePromptCaching = UsePromptCaching,
                StreamResponses = StreamResponses,
                EmbeddingModel = EmbeddingModel,
                EmbeddingsFilename = EmbeddingsFilename,
                UseEmbeddings = UseEmbeddings
            };
        }
    }


    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private Studio4Settings _currentSettings;
        private DefaultSettings _defaultSettings;

        public Studio4Settings CurrentSettings => _currentSettings;
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
                    _currentSettings = JsonConvert.DeserializeObject<Studio4Settings>(jsonContent);
            }
            else
            {
                _currentSettings = new Studio4Settings();
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

        public void UpdateSettings(Studio4Settings newSettings)
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