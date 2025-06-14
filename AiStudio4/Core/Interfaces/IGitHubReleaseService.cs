// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Interfaces\IGitHubReleaseService.cs



namespace AiStudio4.Core.Interfaces
{
    public interface IGitHubReleaseService
    {
        Task<UpdateCheckResult> CheckForUpdatesAsync(string owner, string repo);
    }
}
