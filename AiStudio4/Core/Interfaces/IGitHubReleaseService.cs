// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Interfaces\IGitHubReleaseService.cs
using System.Threading.Tasks;
using AiStudio4.Core.Models;

namespace AiStudio4.Core.Interfaces
{
    public interface IGitHubReleaseService
    {
        Task<UpdateCheckResult> CheckForUpdatesAsync(string owner, string repo);
    }
}