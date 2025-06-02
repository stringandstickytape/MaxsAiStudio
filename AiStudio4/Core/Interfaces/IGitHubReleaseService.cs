// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Interfaces\IGitHubReleaseService.cs
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IGitHubReleaseService
    {
        Task CheckAndLogLatestReleaseAsync(string owner, string repo);
    }
}