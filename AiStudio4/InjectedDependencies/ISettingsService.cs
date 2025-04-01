using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.InjectedDependencies
{
    public interface ISettingsService
    {
        Studio4Settings CurrentSettings { get; }
        DefaultSettings DefaultSettings { get; }

        void LoadSettings();
        void SaveSettings();
        void UpdateSettings(Studio4Settings newSettings);
        void UpdateDefaultModel(string modelName);
        void UpdateSecondaryModel(string modelName);
        void AddModel(Model model);
        void UpdateModel(Model updatedModel);
        void DeleteModel(string modelGuid);
        void AddServiceProvider(ServiceProvider provider);
        void UpdateServiceProvider(ServiceProvider updatedProvider);
        void DeleteServiceProvider(string providerGuid);
        AppearanceSettings GetAppearanceSettings(string clientId);
        void UpdateAppearanceSettings(string clientId, AppearanceSettings settings);
        void AddProjectPathToHistory(string path); // Added method signature
    }
}
