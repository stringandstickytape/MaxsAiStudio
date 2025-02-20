import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { MarkdownPane } from './markdown-pane';

const MessageNode = ({ messageId, conversationId }: { messageId: string; conversationId: string }) => {
    const message = useSelector((state: RootState) => 
        state.conversations.conversations[conversationId]?.messages[messageId]
    );

    if (!message) return null;

    return (
        
        <div className="mt-4 message-node">
            <div className={`p-4 rounded inline-block max-w-[80%] ${message.source === 'user' ? 'float-right bg-blue-800' : 'float-left bg-gray-800'} clear-both`}>
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
        <div className="flex justify-center w-full">
            <div className="conversation-view w-[900px] min-w-[900px] max-w-[900px]">
                {/* Conversation Tree */}
                <MessageNode
                    messageId={conversation.rootMessageId}
                    conversationId={activeConversationId}
                />
            </div>
        </div>
    );
}; 