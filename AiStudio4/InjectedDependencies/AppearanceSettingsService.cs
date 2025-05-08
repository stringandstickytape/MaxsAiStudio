// InjectedDependencies/AppearanceSettingsService.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class AppearanceSettingsService : IAppearanceSettingsService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        private Dictionary<string, AppearanceSettings> _userAppearanceSettings = new();

        public AppearanceSettingsService(IConfiguration configuration)
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
                    _userAppearanceSettings = new Dictionary<string, AppearanceSettings>();
                    SaveSettings();
                    return;
                }
                var json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                var section = json["appearanceSettings"];
                if (section != null)
                {
                    _userAppearanceSettings = section.ToObject<Dictionary<string, AppearanceSettings>>() ?? new Dictionary<string, AppearanceSettings>();
                }
                else
                {
                    _userAppearanceSettings = new Dictionary<string, AppearanceSettings>();
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
                json["appearanceSettings"] = JToken.FromObject(_userAppearanceSettings);
                File.WriteAllText(_settingsFilePath, json.ToString(Formatting.Indented));
            }
        }

        public AppearanceSettings GetAppearanceSettings(string clientId)
        {
            lock (_lock)
            {
                if (!_userAppearanceSettings.TryGetValue(clientId, out var settings))
                {
                    settings = new AppearanceSettings();
                    _userAppearanceSettings[clientId] = settings;
                    SaveSettings();
                }
                return settings;
            }
        }

        public void UpdateAppearanceSettings(string clientId, AppearanceSettings settings)
        {
            lock (_lock)
            {
                _userAppearanceSettings[clientId] = settings;
                SaveSettings();
            }
        }
    }
}