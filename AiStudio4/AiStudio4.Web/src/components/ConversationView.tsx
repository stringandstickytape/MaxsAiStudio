import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { MarkdownPane } from './markdown-pane';

interface ConversationViewProps {
    liveStreamContent?: string;
}

export const ConversationView = ({ liveStreamContent }: ConversationViewProps) => {
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
                {/* Live Stream Markdown Pane */}
                {liveStreamContent && (
                    <div className="">
                        <div className="px-4 mb-4 rounded inline-block float-left bg-gray-800 clear-both">
                            <MarkdownPane message={liveStreamContent} />
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};