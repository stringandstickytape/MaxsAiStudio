// AiStudio4/Services/Interfaces/INotificationFacade.cs
using System.Threading.Tasks;

namespace AiStudio4.Services.Interfaces
{
    /// <summary>
    /// Facade interface for notification and status messaging.
    /// </summary>
    public interface INotificationFacade
    {
        Task SendStatusMessageAsync(string clientId, string message);
        Task ClearStatusMessageAsync(string clientId);
        Task NotifyConvUpdate(string clientId, object update);
        Task NotifyStreamingUpdate(string clientId, object update);
        Task NotifyConvList( object convs);
        Task NotifyTranscription(string transcriptionText);
    }
}