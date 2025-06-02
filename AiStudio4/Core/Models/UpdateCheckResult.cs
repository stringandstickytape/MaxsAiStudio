// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Models\UpdateCheckResult.cs
namespace AiStudio4.Core.Models
{
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string ReleaseUrl { get; set; }
        public string ReleaseName { get; set; }
        public bool CheckSuccessful { get; set; }
        public string ErrorMessage { get; set; }
    }
}