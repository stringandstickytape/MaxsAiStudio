using AiStudio4.InjectedDependencies;

namespace AiStudio4.Core.Interfaces
{
    public interface IConversationStorage
    {
        Task<v4BranchedConversation> LoadConversation(string conversationId);
        Task SaveConversation(v4BranchedConversation conversation);
        Task<v4BranchedConversation> FindConversationByMessageId(string messageId);
    }
}