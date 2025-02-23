using AiStudio4.InjectedDependencies;
using AiTool3.Conversations;

namespace AiStudio4.Core.Interfaces
{
    public interface IConversationTreeBuilder
    {
        dynamic BuildCachedConversationTree(v4BranchedConversation conversation);
        List<v4BranchedConversationMessage> GetMessageHistory(v4BranchedConversation conversation, string messageId);
        dynamic BuildTreeNode(v4BranchedConversationMessage message);
    }
}