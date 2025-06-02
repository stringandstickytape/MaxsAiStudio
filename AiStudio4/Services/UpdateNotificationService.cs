// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Services\UpdateNotificationService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;

namespace AiStudio4.Services
{
    public class UpdateNotificationService : IUpdateNotificationService
    {
        private readonly object _lock = new object();
        private bool _isUpdateAvailable = false;
        private string _updateUrl = string.Empty;
        private string _updateVersion = string.Empty;

        public bool IsUpdateAvailable 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _isUpdateAvailable; 
                } 
            } 
        }

        public string UpdateUrl 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _updateUrl; 
                } 
            } 
        }

        public string UpdateVersion 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _updateVersion; 
                } 
            } 
        }

        public void SetUpdateInfo(UpdateCheckResult updateResult)
        {
            if (updateResult == null) return;

            lock (_lock)
            {
                _isUpdateAvailable = updateResult.IsUpdateAvailable;
                _updateUrl = updateResult.ReleaseUrl ?? string.Empty;
                _updateVersion = updateResult.LatestVersion ?? string.Empty;
            }
        }

        public void ClearUpdateInfo()
        {
            lock (_lock)
            {
                _isUpdateAvailable = false;
                _updateUrl = string.Empty;
                _updateVersion = string.Empty;
            }
        }
    }
}