// AiStudio4/Core/Interfaces/IConversationArchivingService.cs
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IConversationArchivingService
    {
        Task ArchiveAndPruneConversationsAsync();
    }
}