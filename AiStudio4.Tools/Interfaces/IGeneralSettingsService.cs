namespace AiStudio4.Tools.Interfaces
{
    /// <summary>
    /// Minimal settings service interface for tools to access configuration
    /// </summary>
    public interface IGeneralSettingsService
    {
        /// <summary>
        /// Gets the decrypted YouTube API key
        /// </summary>
        string GetDecryptedYouTubeApiKey();

        /// <summary>
        /// Gets the decrypted Azure DevOps PAT
        /// </summary>
        string GetDecryptedAzureDevOpsPAT();

        /// <summary>
        /// Gets the decrypted GitHub token
        /// </summary>
        string GetDecryptedGitHubToken();

        /// <summary>
        /// Gets the current project path
        /// </summary>
        string GetProjectPath();

        /// <summary>
        /// Gets the current settings object (minimal version)
        /// </summary>
        IGeneralSettings CurrentSettings { get; }
    }

    /// <summary>
    /// Minimal settings interface
    /// </summary>
    public interface IGeneralSettings
    {
        string ProjectPath { get; }
    }
}