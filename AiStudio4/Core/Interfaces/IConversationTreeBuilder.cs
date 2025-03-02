using AiStudio4.InjectedDependencies;

namespace AiStudio4.Core.Interfaces
{
    public interface IConversationTreeBuilder
    {
        dynamic BuildHistoricalConversationTree(v4BranchedConversation conversation);
        List<v4BranchedConversationMessage> GetMessageHistory(v4BranchedConversation conversation, string messageId);
        dynamic BuildTreeNode(v4BranchedConversationMessage message);
    }
}