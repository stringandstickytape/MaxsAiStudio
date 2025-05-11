// InjectedDependencies/IAppearanceSettingsService.cs

namespace AiStudio4.InjectedDependencies
{
    public interface IAppearanceSettingsService
    {
        AppearanceSettings GetAppearanceSettings();
        void UpdateAppearanceSettings(AppearanceSettings settings);
        void LoadSettings();
        void SaveSettings();
    }
}