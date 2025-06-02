// AiStudio4/InjectedDependencies/IGeneralSettingsService.cs
using AiStudio4.Core.Models; // If your Model class is here
using SharedClasses.Providers; // If your Model/ServiceProvider classes are here
using System;

namespace AiStudio4.InjectedDependencies
{
    public interface IGeneralSettingsService
    {
        GeneralSettings CurrentSettings { get; }
        event EventHandler SettingsChanged;

        void LoadSettings();
        void SaveSettings();
        void UpdateSettings(GeneralSettings newSettings);
        void UpdateDefaultModel(string modelNameOrGuid);
        void UpdateSecondaryModel(string modelNameOrGuid);
        void MigrateModelNamesToGuids(); // Keep if still relevant

        void AddModel(Model model);
        void UpdateModel(Model updatedModel);
        void DeleteModel(string modelGuid);
        void AddServiceProvider(ServiceProvider provider);
        void UpdateServiceProvider(ServiceProvider updatedProvider);
        void DeleteServiceProvider(string providerGuid);

        // --- MODIFIED/NEW API KEY METHODS ---
        void UpdateYouTubeApiKey(string plaintextApiKey);
        string GetDecryptedYouTubeApiKey();

        void UpdateGitHubApiKey(string plaintextApiKey);
        string GetDecryptedGitHubApiKey();

        void UpdateAzureDevOpsPAT(string plaintextPat);
        string GetDecryptedAzureDevOpsPAT();
        // --- END MODIFIED/NEW API KEY METHODS ---

        void UpdateCondaPath(string path);
        void UpdateUseExperimentalCostTracking(bool value);
        void UpdateConversationZipRetentionDays(int days);
        void UpdateConversationDeleteZippedRetentionDays(int days);

        void UpdateTopP(float topP);
    }
}