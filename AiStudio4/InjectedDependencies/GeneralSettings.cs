// AiStudio4/InjectedDependencies/GeneralSettings.cs
 // If you have a shared Model class
using SharedClasses.Providers; // If your Model/ServiceProvider classes are here




namespace AiStudio4.InjectedDependencies
{
    public class GeneralSettings
    {
        public List<Model> ModelList { get; set; } = new();
        public List<ServiceProvider> ServiceProviders { get; set; } = new();
        public float Temperature { get; set; } = 0.2f;
        public float TopP { get; set; } = 0.9f; // Added TopP
        public bool UseEmbeddings { get; set; } = false;
        public bool UsePromptCaching { get; set; } = true;
        public string EmbeddingsFilename { get; set; }
        public string EmbeddingModel { get; set; } = "mxbai-embed-large";
        public string DefaultSystemPromptId { get; set; }
        public string ProjectPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "AiStudio4TestProject");

        // --- MODIFIED API KEY PROPERTIES ---
        public string EncryptedYouTubeApiKey { get; set; }
        public string EncryptedGitHubApiKey { get; set; }
        public string EncryptedAzureDevOpsPAT { get; set; }
        public string EncryptedGoogleCustomSearchApiKey { get; set; }
        // --- END MODIFIED API KEY PROPERTIES ---

        public string CondaPath { get; set; }
        public bool AllowConnectionsOutsideLocalhost { get; set; } = false;

        public string DefaultModelGuid { get; set; } = string.Empty;
        public string SecondaryModelGuid { get; set; } = string.Empty;
        public int ConversationZipRetentionDays { get; set; } = 30;
        public int ConversationDeleteZippedRetentionDays { get; set; } = 90;
        public int NextTipNumber { get; set; } = 0;

        public List<string> PackerIncludeFileTypes { get; set; } = new List<string>();
        public List<string> PackerExcludeFilenames { get; set; } = new List<string>();
        public List<string> PackerExcludeFolderNames { get; set; } = new List<string>();
        public string LastPackerOutputFile { get; set; }

        // Wiki System Prompt Sync Settings
        public bool EnableWikiSystemPromptSync { get; set; } = false;
        public string WikiSyncAdoOrganization { get; set; } = string.Empty;
        public string WikiSyncAdoProject { get; set; } = string.Empty;
        public string WikiSyncWikiIdentifier { get; set; } = string.Empty;
        public string WikiSyncPagePath { get; set; } = string.Empty;
        public string WikiSyncTargetSystemPromptGuid { get; set; } = string.Empty;

        // Remove obsolete plaintext properties if you are doing a clean break
        // Otherwise, keep them for migration (see GeneralSettingsService.cs)
        [Obsolete("Use EncryptedYouTubeApiKey. This property is for migration only.")]
        public string YouTubeApiKey { get; set; }
        [Obsolete("Use EncryptedGitHubApiKey. This property is for migration only.")]
        public string GitHubApiKey { get; set; }
        [Obsolete("Use EncryptedAzureDevOpsPAT. This property is for migration only.")]
        public string AzureDevOpsPAT { get; set; }
        [Obsolete("Use EncryptedGoogleCustomSearchApiKey. This property is for migration only.")]
        public string GoogleCustomSearchApiKey { get; set; }
        
        [Obsolete("Use DefaultModelGuid instead")]
        public string DefaultModel { get; set; } = string.Empty;
        [Obsolete("Use SecondaryModelGuid instead")]
        public string SecondaryModel { get; set; } = string.Empty;


        public ApiSettings ToApiSettings() => new()
        {
            Temperature = this.Temperature,
            TopP = this.TopP, // Added TopP
            UsePromptCaching = this.UsePromptCaching,
            EmbeddingModel = this.EmbeddingModel,
            EmbeddingsFilename = this.EmbeddingsFilename,
            UseEmbeddings = this.UseEmbeddings,
            DefaultSystemPromptId = this.DefaultSystemPromptId
        };
    }
}
