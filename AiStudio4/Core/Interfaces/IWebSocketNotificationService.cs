




namespace AiStudio4.Core.Interfaces
{
    public interface IWebSocketNotificationService : IDisposable
    {
        
        
        
        
        
        Task NotifyConvUpdate(string clientId, ConvUpdateDto update);

        
        
        
        
        
        Task NotifyStreamingUpdate(string clientId, StreamingUpdateDto update);

        
        
        
        
        
        Task NotifyConvList( ConvListDto convs);

        
        
        
        
        
        Task NotifyTranscription(string transcriptionText);

        
        
        
        
        
        Task NotifyStatusMessage(string clientId, string message);
        
        
        
        
        
        
        Task NotifyFileSystemChanges(IReadOnlyList<string> directories, IReadOnlyList<string> files);
        Task NotifyConvPlaceholderUpdate(string clientId, v4BranchedConv conv, v4BranchedConvMessage placeholderMessage);
    }
}
