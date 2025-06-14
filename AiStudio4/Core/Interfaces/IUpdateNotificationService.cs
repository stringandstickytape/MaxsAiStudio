// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Core\Interfaces\IUpdateNotificationService.cs


namespace AiStudio4.Core.Interfaces
{
    public interface IUpdateNotificationService
    {
        bool IsUpdateAvailable { get; }
        string UpdateUrl { get; }
        string UpdateVersion { get; }
        void SetUpdateInfo(UpdateCheckResult updateResult);
        void ClearUpdateInfo();
    }
}
