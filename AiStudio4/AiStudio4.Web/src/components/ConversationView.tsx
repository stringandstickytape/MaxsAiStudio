import { useSelector } from 'react-redux';
import { RootState } from '../store/store';
import { MarkdownPane } from './markdown-pane';

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
                {conversation.messages.map((message) => (
                    <div key={message.id} className="mt-4">
                        <div className={`p-4 rounded inline-block max-w-[80%] ${message.source === 'user' ? 'float-right bg-blue-800' : 'float-left bg-gray-800'} clear-both`}>
                            <MarkdownPane message={message.content} />
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};