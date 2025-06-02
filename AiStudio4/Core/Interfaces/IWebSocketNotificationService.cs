using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IWebSocketNotificationService
    {
        
        
        
        
        
        Task NotifyConvUpdate(string clientId, ConvUpdateDto update);

        
        
        
        
        
        Task NotifyStreamingUpdate(string clientId, StreamingUpdateDto update);

        
        
        
        
        
        Task NotifyConvList( ConvListDto convs);

        
        
        
        
        
        Task NotifyTranscription(string transcriptionText);

        
        
        
        
        
        Task NotifyStatusMessage(string clientId, string message);
        
        
        
        
        
        
        Task NotifyFileSystemChanges(IReadOnlyList<string> directories, IReadOnlyList<string> files);
    }
}
