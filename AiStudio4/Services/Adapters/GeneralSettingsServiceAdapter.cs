using AiStudio4.InjectedDependencies;

namespace AiStudio4.Services.Adapters
{
    /// <summary>
    /// Adapter that bridges between the main app's GeneralSettingsService and the shared library's minimal interface
    /// </summary>
    public class GeneralSettingsServiceAdapter : AiStudio4.Tools.Interfaces.IGeneralSettingsService
    {
        private readonly IGeneralSettingsService _originalService;

        public GeneralSettingsServiceAdapter(IGeneralSettingsService originalService)
        {
            _originalService = originalService;
        }

        public string GetDecryptedYouTubeApiKey()
        {
            return _originalService.GetDecryptedYouTubeApiKey();
        }

        public string GetDecryptedAzureDevOpsPAT()
        {
            return _originalService.GetDecryptedAzureDevOpsPAT();
        }

        public string GetDecryptedGitHubToken()
        {
            // Main app uses GetDecryptedGitHubApiKey, not GetDecryptedGitHubToken
            return _originalService.GetDecryptedGitHubApiKey();
        }

        public string GetProjectPath()
        {
            return _originalService.CurrentSettings?.ProjectPath;
        }

        public AiStudio4.Tools.Interfaces.IGeneralSettings CurrentSettings => 
            new GeneralSettingsAdapter(_originalService.CurrentSettings);

        private class GeneralSettingsAdapter : AiStudio4.Tools.Interfaces.IGeneralSettings
        {
            private readonly GeneralSettings _original;

            public GeneralSettingsAdapter(GeneralSettings original)
            {
                _original = original;
            }

            public string ProjectPath => _original?.ProjectPath;
        }
    }
}