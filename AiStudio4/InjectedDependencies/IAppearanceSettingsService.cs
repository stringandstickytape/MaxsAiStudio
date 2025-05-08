// InjectedDependencies/IAppearanceSettingsService.cs

namespace AiStudio4.InjectedDependencies
{
    public interface IAppearanceSettingsService
    {
        AppearanceSettings GetAppearanceSettings(string clientId);
        void UpdateAppearanceSettings(string clientId, AppearanceSettings settings);
        void LoadSettings();
        void SaveSettings();
    }
}