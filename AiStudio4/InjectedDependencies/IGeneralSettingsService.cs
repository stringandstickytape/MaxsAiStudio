// InjectedDependencies/IGeneralSettingsService.cs
using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.InjectedDependencies
{
    public interface IGeneralSettingsService
    {
        GeneralSettings CurrentSettings { get; }
        void LoadSettings();
        void SaveSettings();
        void UpdateSettings(GeneralSettings newSettings);
        void UpdateDefaultModel(string modelName);
        void UpdateSecondaryModel(string modelName);
        void MigrateModelNamesToGuids();
        void AddModel(Model model);
        void UpdateModel(Model updatedModel);
        void DeleteModel(string modelGuid);
        void AddServiceProvider(ServiceProvider provider);
        void UpdateServiceProvider(ServiceProvider updatedProvider);
        void DeleteServiceProvider(string providerGuid);
        void UpdateYouTubeApiKey(string apiKey);
        void UpdateCondaPath(string path);
    }
}