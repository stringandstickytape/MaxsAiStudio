// InjectedDependencies/AppearanceSettingsService.cs
using Microsoft.Extensions.Configuration;






namespace AiStudio4.InjectedDependencies
{
    public class AppearanceSettingsService : IAppearanceSettingsService
    {
        private readonly string _settingsFilePath;
        private readonly object _lock = new();
        private AppearanceSettings _userAppearanceSettings = new();

        public AppearanceSettingsService(IConfiguration configuration)
        {
            _settingsFilePath = PathHelper.GetProfileSubPath("settings.json");
            
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath));
            LoadSettings();
        }

        public void LoadSettings()
        {
            lock (_lock)
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _userAppearanceSettings = new AppearanceSettings();
                    SaveSettings();
                    return;
                }
                var json = JObject.Parse(File.ReadAllText(_settingsFilePath));
                var section = json["appearanceSettings"];
                if (section != null)
                {
                    _userAppearanceSettings = section.ToObject<AppearanceSettings>() ?? new AppearanceSettings();
                }
                else
                {
                    _userAppearanceSettings = new AppearanceSettings();
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

        public AppearanceSettings GetAppearanceSettings()
        {
                return _userAppearanceSettings;
        }

        public void UpdateAppearanceSettings(AppearanceSettings settings)
        {
                _userAppearanceSettings = settings;
                SaveSettings();
        }
    }
}
