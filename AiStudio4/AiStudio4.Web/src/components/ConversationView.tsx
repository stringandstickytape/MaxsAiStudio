import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { MarkdownPane } from './markdown-pane';

const MessageNode = ({ messageId, conversationId }: { messageId: string; conversationId: string }) => {
    const message = useSelector((state: RootState) => 
        state.conversations.conversations[conversationId]?.messages[messageId]
    );

    if (!message) return null;

    return (
        <div className="ml-4 mt-4">
            <div className={`p-2 rounded ${message.source === 'user' ? 'bg-blue-800' : 'bg-gray-800'}`}>
                <MarkdownPane message={JSON.stringify(message.content)} />
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