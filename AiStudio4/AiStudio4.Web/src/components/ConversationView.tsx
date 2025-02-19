import { useSelector } from 'react-redux';
import { RootState } from '../store/store';

const MessageNode = ({ messageId, conversationId }: { messageId: string; conversationId: string }) => {
    const message = useSelector((state: RootState) => 
        state.conversations.conversations[conversationId]?.messages[messageId]
    );

    if (!message) return null;

    return (
        <div className="ml-4">
            <div className={`p-2 rounded ${
                message.source === 'user' ? 'bg-blue-100' : 'bg-gray-100'
            }`}>
                {message.content}
            </div>
            {message.children.map((childId) => (
                <MessageNode 
                    key={childId} 
                    messageId={childId} 
                    conversationId={conversationId} 
                />
            ))}
        </div>
    );
};

export const ConversationView = () => {
    const { activeConversationId, conversations } = useSelector(
        (state: RootState) => state.conversations
    );

    if (!activeConversationId) return null;
    const conversation = conversations[activeConversationId];
    if (!conversation) return null;

    return (
        <div className="conversation-tree">
            <MessageNode 
                messageId={conversation.rootMessageId} 
                conversationId={activeConversationId} 
            />
        </div>
    );
};