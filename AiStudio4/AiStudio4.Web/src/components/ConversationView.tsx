import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { MarkdownPane } from './markdown-pane';
import { LiveStreamToken } from './LiveStreamToken'; // Import the new component

interface ConversationViewProps {
    streamTokens: string[]; // Receive the array of tokens
}

export const ConversationView = ({ streamTokens }: ConversationViewProps) => {
    const { activeConversationId, conversations } = useSelector(
        (state: RootState) => state.conversations
    );

    if (!activeConversationId) return null;
    const conversation = conversations[activeConversationId];
    if (!conversation) return null;

    return (
        <div className="w-full">
            <div className="conversation-view w-full">
                {conversation.messages.map((message) => (
                    <div key={message.id} className="">
                        <div className={`px-4 mb-4 rounded inline-block ${message.source === 'user' ? 'float-right bg-blue-800' : 'float-left bg-gray-800'} clear-both`}>
                            <MarkdownPane message={message.content} />
                        </div>
                    </div>
                ))}
                {streamTokens.length > 0 && (
                    <div className="p-4 mb-4 rounded bg-gray-800 float-left clear-both break-words whitespace-normal w-full">
                        {streamTokens.map((token, index) => (
                            <LiveStreamToken key={index} token={token} />
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
};