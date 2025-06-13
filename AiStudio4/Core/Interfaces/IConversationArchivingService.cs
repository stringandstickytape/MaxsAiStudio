// AiStudio4/Core/Interfaces/IConversationArchivingService.cs


namespace AiStudio4.Core.Interfaces
{
    public interface IConversationArchivingService
    {
        Task ArchiveAndPruneConversationsAsync();
    }
}
