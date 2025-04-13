// InjectedDependencies/GeneralSettingsService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettingsService : IGeneralSettingsService
    {
        private readonly string _settingsFilePath;
        public GeneralSettings CurrentSettings { get; private set; } = new();
        private readonly object _lock = new();

        public GeneralSettingsService(IConfiguration configuration)
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
            LoadSettings();
        }

        public void LoadSettings()
        {
            lock (_lock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    CurrentSettings = new GeneralSettings();
                    SaveSettings();
                    return;
                }
                var text = File.ReadAllText(_settingsFilePath);
                var json = JObject.Parse(text);
                var section = json["generalSettings"];
                if (section != null)
                {
                    CurrentSettings = section.ToObject<GeneralSettings>() ?? new GeneralSettings();
                }
                else
                {
                    CurrentSettings = new GeneralSettings();
                    SaveSettings();
                }
            }
        }

        public void SaveSettings()
        {
            lock (_lock)
            {
                JObject json;
                if (File.Exists(_settingsFilePath))
                {
                    json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                }
                else
                {
                    json = new JObject();
                }
                json["generalSettings"] = JToken.FromObject(CurrentSettings);
                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
            }
        }

        public void UpdateSettings(GeneralSettings newSettings)
        {
            CurrentSettings = newSettings;
            SaveSettings();
        }

        public void UpdateDefaultModel(string modelName)
        {
            CurrentSettings.DefaultModel = modelName;
            SaveSettings();
        }

        public void UpdateSecondaryModel(string modelName)
        {
            // For now, treat same as default prompt id
            CurrentSettings.SecondaryModel = modelName;
            SaveSettings();
        }

        public void AddModel(Model model)
        {
            CurrentSettings.ModelList.Add(model);
            SaveSettings();
        }

        public void UpdateModel(Model updatedModel)
        {
            var existing = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == updatedModel.Guid);
            if (existing != null)
            {
                var idx = CurrentSettings.ModelList.IndexOf(existing);
                CurrentSettings.ModelList[idx] = updatedModel;
                SaveSettings();
            }
        }

        public void DeleteModel(string modelGuid)
        {
            var existing = CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuid);
            if (existing != null)
            {
                CurrentSettings.ModelList.Remove(existing);
                SaveSettings();
            }
        }

        public void AddServiceProvider(ServiceProvider provider)
        {
            CurrentSettings.ServiceProviders.Add(provider);
            SaveSettings();
        }

        public void UpdateServiceProvider(ServiceProvider updatedProvider)
        {
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == updatedProvider.Guid);
            if (existing != null)
            {
                var idx = CurrentSettings.ServiceProviders.IndexOf(existing);
                CurrentSettings.ServiceProviders[idx] = updatedProvider;
                SaveSettings();
            }
        }

        public void DeleteServiceProvider(string providerGuid)
        {
            var existing = CurrentSettings.ServiceProviders.FirstOrDefault(p => p.Guid == providerGuid);
            if (existing != null)
            {
                CurrentSettings.ServiceProviders.Remove(existing);
                SaveSettings();
            }
        }

        public void UpdateYouTubeApiKey(string apiKey)
        {
            CurrentSettings.YouTubeApiKey = apiKey;
            SaveSettings();
        }

        public void UpdateCondaPath(string path)
        {
            CurrentSettings.CondaPath = path;
            SaveSettings();
        }
    }
}