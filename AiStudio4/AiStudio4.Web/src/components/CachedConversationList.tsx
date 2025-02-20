import { useState, useEffect } from 'react';
import { useWebSocketMessage } from '@/hooks/useWebSocketMessage';

interface CachedConversation {
    convGuid: string;
    summary: string;
    fileName: string;
    lastModified: string;
    highlightColour?: string;
}

export const CachedConversationList = () => {
    const [conversations, setConversations] = useState<CachedConversation[]>([]);

    const handleNewCachedConversation = (conversation: CachedConversation) => {
        setConversations(prevConversations => {
            // Check if conversation already exists
            const exists = prevConversations.some(conv => conv.convGuid === conversation.convGuid);
            if (exists) {
                // Update existing conversation
                return prevConversations.map(conv => 
                    conv.convGuid === conversation.convGuid ? conversation : conv
                );
            }
            // Add new conversation at the beginning of the list
            return [conversation, ...prevConversations];
        });
    };

    // Subscribe to cached conversation messages
    useWebSocketMessage('cachedconversation', handleNewCachedConversation);


    return (
        <div className="flex flex-col space-y-2">
            {conversations.map((conversation) => (
                <div
                    key={conversation.convGuid}
                    className={`p-3 rounded cursor-pointer transition-colors duration-200`}
                    style={{
                        backgroundColor: conversation.highlightColour || '#374151',
                        color: conversation.highlightColour ? '#000' : '#fff'
                    }}
                >
                    <div className="text-sm">
                        <div className="font-medium truncate mb-1">
                            {conversation.summary}
                        </div>
                        <div className="text-xs opacity-70">
                            {new Date(conversation.lastModified).toLocaleDateString()}
                        </div>
                    </div>
                </div>
            ))}
        </div>
    );
};