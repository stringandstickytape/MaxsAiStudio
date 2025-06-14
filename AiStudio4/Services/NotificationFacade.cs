// AiStudio4/Services/NotificationFacade.cs

using AiStudio4.Services.Interfaces;



namespace AiStudio4.Services
{
    /// <summary>
    /// Facade implementation for notification and status messaging.
    /// </summary>
    public class NotificationFacade : INotificationFacade
    {
        private readonly IStatusMessageService _statusMessageService;
        private readonly IWebSocketNotificationService _webSocketNotificationService;

        public NotificationFacade(IStatusMessageService statusMessageService, IWebSocketNotificationService webSocketNotificationService)
        {
            _statusMessageService = statusMessageService;
            _webSocketNotificationService = webSocketNotificationService;
        }

        public Task SendStatusMessageAsync(string clientId, string message)
            => _statusMessageService.SendStatusMessageAsync(clientId, message);

        public Task ClearStatusMessageAsync(string clientId)
            => _statusMessageService.ClearStatusMessageAsync(clientId);

        public Task NotifyConvUpdate(string clientId, object update)
            => _webSocketNotificationService.NotifyConvUpdate(clientId, (ConvUpdateDto)update);

        public Task NotifyStreamingUpdate(string clientId, object update)
            => _webSocketNotificationService.NotifyStreamingUpdate(clientId, (StreamingUpdateDto)update);

        public Task NotifyConvList(object convs)
            => _webSocketNotificationService.NotifyConvList((ConvListDto)convs);

        public Task NotifyTranscription(string transcriptionText)
            => _webSocketNotificationService.NotifyTranscription(transcriptionText);
    }
}
