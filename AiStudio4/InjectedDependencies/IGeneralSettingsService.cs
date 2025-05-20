// InjectedDependencies/IGeneralSettingsService.cs
using AiStudio4.Core.Models;
using SharedClasses.Providers;
using System;

namespace AiStudio4.InjectedDependencies
{
    public interface IGeneralSettingsService
    {
        GeneralSettings CurrentSettings { get; }
        
        /// <summary>
        /// Event that is raised when settings are changed
        /// </summary>
        event EventHandler SettingsChanged;
        
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
        void UpdateGitHubApiKey(string apiKey);
        void UpdateAzureDevOpsPAT(string pat);
        void UpdateCondaPath(string path);
        void UpdateUseExperimentalCostTracking(bool value);
        void UpdateConversationZipRetentionDays(int days);
        void UpdateConversationDeleteZippedRetentionDays(int days);
    }
}