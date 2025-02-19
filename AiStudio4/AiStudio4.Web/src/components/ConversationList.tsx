import { useSelector } from 'react-redux';
import { RootState } from '../store/store';

export const ConversationList = () => {
    const { conversations, activeConversationId } = useSelector(
        (state: RootState) => state.conversations
    );

    return (
        <div className="flex flex-col space-y-2">
            <h3 className="text-white text-lg font-semibold mb-4">Conversations</h3>
            {Object.entries(conversations).map(([id, conversation]) => (
                <div
                    key={id}
                    className={`p-3 rounded cursor-pointer transition-colors duration-200 ${
                        id === activeConversationId
                            ? 'bg-blue-600 text-white'
                            : 'bg-gray-700 text-gray-200 hover:bg-gray-600'
                    }`}
                >
                    <div className="text-sm truncate">
                        {/* Show first message content as title */}
                        {conversation.messages[conversation.rootMessageId]?.content || 'New Conversation'}
                    </div>
                </div>
            ))}
        </div>
    );
};