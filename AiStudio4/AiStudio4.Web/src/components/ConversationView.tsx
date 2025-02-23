import { useSelector } from 'react-redux';
import { RootState, store } from '../store/store';
import { setActiveConversation } from '../store/conversationSlice';
import { MarkdownPane } from './markdown-pane';
import { LiveStreamToken } from './LiveStreamToken';
import { useEffect } from 'react';

interface ConversationViewProps {
    streamTokens: string[]; // Receive the array of tokens
}

export const ConversationView = ({ streamTokens }: ConversationViewProps) => {
    // Create separate selectors to ensure component updates when these change
    const activeConversationId = useSelector((state: RootState) => state.conversations.activeConversationId);
    const selectedMessageId = useSelector((state: RootState) => state.conversations.selectedMessageId);
    const conversations = useSelector((state: RootState) => state.conversations.conversations);
    useEffect(() => {
        console.log('ConversationView update:', { activeConversationId, selectedMessageId });
    }, [activeConversationId, selectedMessageId]);

    if (!activeConversationId) return null;
    const conversation = conversations[activeConversationId];
    if (!conversation) return null;

    return (
        <div className="w-full">
            <div className="conversation-view w-full">
                {(() => {
                    let messagesToShow = conversation.messages;
                    
                    // If there's a selected message, show only that message and its ancestors
                    if (selectedMessageId) {
                        const messageChain = [];
                        let currentId = selectedMessageId;
                        
                        // Build chain from selected message to root
                        while (currentId) {
                            const currentMessage = conversation.messages.find(m => m.id === currentId);
                            if (currentMessage) {
                                messageChain.unshift(currentMessage);
                                currentId = currentMessage.parentId;
                            } else {
                                break;
                            }
                        }
                        
                        messagesToShow = messageChain;
                    }

                    return messagesToShow.map((message) => (
                        <div key={message.id} className="">
                            <div 
                                className={`px-4 mb-4 rounded inline-block cursor-pointer ${message.source === 'user' ? 'float-right bg-blue-800' : 'float-left bg-gray-800'} ${message.id === selectedMessageId ? 'ring-2 ring-blue-500' : ''} clear-both`}
                                onClick={() => {
                                    store.dispatch(setActiveConversation({
                                        conversationId: activeConversationId,
                                        selectedMessageId: message.id
                                    }));
                                }}
                            >
                                <MarkdownPane message={message.content} />
                            </div>
                        </div>
                    ));
                })()}
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