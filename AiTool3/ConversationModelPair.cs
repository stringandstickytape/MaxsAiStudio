using AiTool3.ApiManagement;
using AiTool3.Conversations;

namespace AiTool3
{
    internal record struct ConversationModelPair(Conversation conversation, Model model)
    {
        public static implicit operator (Conversation conversation, Model model)(ConversationModelPair value)
        {
            return (value.conversation, value.model);
        }

        public static implicit operator ConversationModelPair((Conversation conversation, Model model) value)
        {
            return new ConversationModelPair(value.conversation, value.model);
        }
    }
}
