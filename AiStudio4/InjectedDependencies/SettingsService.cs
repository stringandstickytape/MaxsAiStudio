using AiStudio4.Core.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SharedClasses.Providers;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class Studio4Settings
    {
        public List<Model> ModelList { get; set; } = new();
        public List<ServiceProvider> ServiceProviders { get; set; } = new();
        public float Temperature { get; set; } = 0.9f;
        public bool UseEmbeddings { get; set; } = false;
        public bool UsePromptCaching { get; set; } = true;
        public bool StreamResponses { get; set; } = false;
        public string EmbeddingsFilename { get; set; }
        public string EmbeddingModel { get; set; } = "mxbai-embed-large";
        public string DefaultSystemPromptId { get; set; }

        public List<string> ProjectPathHistory { get; set; } = new();

        public string ProjectPath { get; set; } = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4";

        // Appearance settings
        public Dictionary<string, AppearanceSettings> UserAppearanceSettings { get; set; } = new();

        public ApiSettings ToApiSettings() => new()
        {
            Temperature = Temperature,
            UsePromptCaching = UsePromptCaching,
            StreamResponses = StreamResponses,
            EmbeddingModel = EmbeddingModel,
            EmbeddingsFilename = EmbeddingsFilename,
            UseEmbeddings = UseEmbeddings,
            DefaultSystemPromptId = DefaultSystemPromptId
        };
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private Studio4Settings _currentSettings;
        private DefaultSettings _defaultSettings;

        public Studio4Settings CurrentSettings => _currentSettings;
        public DefaultSettings DefaultSettings => _defaultSettings;

        public SettingsService(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "settings.json");
            var defaultSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "defaultSettings.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));

            LoadSettings();
            LoadDefaultSettings(defaultSettingsPath);
        }

        public void LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                _currentSettings = new Studio4Settings();
                SaveSettings();
                return;
            }

            string jsonContent = File.ReadAllText(_settingsFilePath);
            _currentSettings = JsonConvert.DeserializeObject<Studio4Settings>(jsonContent);

            // Ensure ProjectPathHistory is initialized
            if (_currentSettings.ProjectPathHistory == null)
            {
                _currentSettings.ProjectPathHistory = new List<string>();
                SaveSettings(); // Save if we had to initialize it
            }
        }

        private void LoadDefaultSettings(string defaultSettingsPath)
        {
            if (!File.Exists(defaultSettingsPath))
            {
                _defaultSettings = new DefaultSettings();
                SaveDefaultSettings(defaultSettingsPath);
                return;
            }

            string jsonContent = File.ReadAllText(defaultSettingsPath);
            _defaultSettings = JsonConvert.DeserializeObject<DefaultSettings>(jsonContent) ?? new DefaultSettings(); // Handle potential null deserialization
        }

        private void SaveDefaultSettings(string defaultSettingsPath)
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_defaultSettings);
                File.WriteAllText(defaultSettingsPath, jsonContent);
            }
            catch { /* Handle or log error as needed */ }
        }

        public void SaveSettings()
        {
            try
            {
                string jsonContent = JsonConvert.SerializeObject(_currentSettings);
                File.WriteAllText(_settingsFilePath, jsonContent);
            }
            catch { /* Handle or log error as needed */ }
        }

        public void UpdateSettings(Studio4Settings newSettings)
        {
            _currentSettings = newSettings;
            SaveSettings();
        }

        public void UpdateDefaultModel(string modelName)
        {
            _defaultSettings ??= new DefaultSettings();
            _defaultSettings.DefaultModel = modelName;
            SaveDefaultSettings(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "defaultSettings.json"));
        }

        public void UpdateSecondaryModel(string modelName)
        {
            _defaultSettings ??= new DefaultSettings();
            _defaultSettings.SecondaryModel = modelName;
            SaveDefaultSettings(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "defaultSettings.json"));
        }

        public void AddModel(Model model)
        {
            _currentSettings.ModelList.Add(model);
            SaveSettings();
        }

        public void UpdateModel(Model updatedModel)
        {
            var existingModel = _currentSettings.ModelList.FirstOrDefault(m => m.Guid == updatedModel.Guid);
            if (existingModel != null)
            {
                _currentSettings.ModelList[_currentSettings.ModelList.IndexOf(existingModel)] = updatedModel;
                SaveSettings();
            }
        }

        public void DeleteModel(string modelGuid)
        {
            var modelToRemove = _currentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuid);
            if (modelToRemove != null)
            {
                _currentSettings.ModelList.Remove(modelToRemove);
                SaveSettings();
            }
        }

        public void AddServiceProvider(ServiceProvider provider)
        {
            _currentSettings.ServiceProviders.Add(provider);
            SaveSettings();
        }

        public void UpdateServiceProvider(ServiceProvider updatedProvider)
        {
            var existingProvider = _currentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == updatedProvider.Guid);
            if (existingProvider != null)
            {
                _currentSettings.ServiceProviders[_currentSettings.ServiceProviders.IndexOf(existingProvider)] = updatedProvider;
                SaveSettings();
            }
        }

        public void DeleteServiceProvider(string providerGuid)
        {
            var providerToRemove = _currentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == providerGuid);
            if (providerToRemove != null)
            {
                _currentSettings.ServiceProviders.Remove(providerToRemove);
                SaveSettings();
            }
        }

        /// <summary>
        /// Gets the appearance settings for a specific client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <returns>The appearance settings for the client, or default settings if none exist</returns>
        public AppearanceSettings GetAppearanceSettings(string clientId)
        {
            if (_currentSettings.UserAppearanceSettings == null)
            {
                _currentSettings.UserAppearanceSettings = new Dictionary<string, AppearanceSettings>();
            }

            if (!_currentSettings.UserAppearanceSettings.TryGetValue(clientId, out var settings))
            {
                settings = new AppearanceSettings();
                _currentSettings.UserAppearanceSettings[clientId] = settings;
                SaveSettings();
            }

            return settings;
        }

        /// <summary>
        /// Updates the appearance settings for a specific client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="settings">The new appearance settings</param>
        public void UpdateAppearanceSettings(string clientId, AppearanceSettings settings)
        {
            if (_currentSettings.UserAppearanceSettings == null)
            {
                _currentSettings.UserAppearanceSettings = new Dictionary<string, AppearanceSettings>();
            }

            _currentSettings.UserAppearanceSettings[clientId] = settings;
            SaveSettings();
        }
        // Method to add/update the project path history
        public void AddProjectPathToHistory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;
            // Ensure the list is initialized
            _currentSettings.ProjectPathHistory ??= new List<string>();
            // Remove existing instance if it exists
            _currentSettings.ProjectPathHistory.Remove(path);
            // Insert at the beginning
            _currentSettings.ProjectPathHistory.Insert(0, path);
            // Keep only the top 10 most recent paths
            const int maxHistoryItems = 10;
            if (_currentSettings.ProjectPathHistory.Count > maxHistoryItems)
            {
                _currentSettings.ProjectPathHistory = _currentSettings.ProjectPathHistory.Take(maxHistoryItems).ToList();
            }
            SaveSettings();
        }

        // Implementation for the new interface method
        public void UpdateYouTubeApiKey(string apiKey)
        {
            _defaultSettings ??= new DefaultSettings();
            _defaultSettings.YouTubeApiKey = apiKey ?? string.Empty;
            SaveDefaultSettings(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "defaultSettings.json"));
        }
    }
}