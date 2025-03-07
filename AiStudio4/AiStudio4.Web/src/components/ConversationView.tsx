import { useSelector } from 'react-redux';
import { RootState, store } from '@/store/store';
import { setActiveConversation } from '@/store/conversationSlice';
import { MarkdownPane } from '@/components/markdown-pane';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { useEffect, useMemo } from 'react';
import { Message } from '@/types/conversation';
import { MessageGraph } from '@/utils/messageGraph';

interface ConversationViewProps {
    streamTokens: string[]; // Receive the array of tokens
}

export const ConversationView = ({ streamTokens }: ConversationViewProps) => {
    const activeConversationId = useSelector((state: RootState) => state.conversations.activeConversationId);
    const activeMessageId = useSelector((state: RootState) => state.conversations.selectedMessageId);
    const conversations = useSelector((state: RootState) => state.conversations.conversations);

    // Get the message chain (active message plus its ancestors)
    const messageChain = useMemo(() => {
        if (!activeConversationId) return [];

        const conversation = conversations[activeConversationId];
        if (!conversation || !conversation.messages.length) return [];

        // Create a message graph from the conversation messages
        const graph = new MessageGraph(conversation.messages);

        // If we're actively streaming (generating new messages), always use the most recent message
        // Otherwise, if we have a selected message ID, use that as the starting point for the message chain
        const startingMessageId = streamTokens.length > 0 ?
            conversation.messages[conversation.messages.length - 1].id :
            (activeMessageId || conversation.messages[conversation.messages.length - 1].id);

        console.log('ConversationView: Building message chain from:', {
            startingMessageId,
            activeMessageId,
            streamActive: streamTokens.length > 0,
            messageCount: conversation.messages.length
        });

        // Get the path from the starting message back to the root
        return graph.getMessagePath(startingMessageId);

    }, [activeConversationId, activeMessageId, conversations, streamTokens.length]);

    useEffect(() => {
        console.log("Message chain updated with length:", messageChain.length);
    }, [messageChain]);

    if (!activeConversationId) return null;
    if (!messageChain.length) {
        console.warn('No messages to display in conversation:', activeConversationId);
        return null;
    }

    return (
        <div className="w-full">
            <div className="conversation-view w-full">
                {messageChain.map((message) => (
                    <div key={message.id} className="">
                        <div
                            className={`px-4 mb-4 rounded inline-block cursor-pointer ${message.source === 'user' ? 'float-right bg-blue-800' : 'float-left bg-gray-800'} ${message.id === activeMessageId ? 'ring-2 ring-blue-500' : ''} clear-both`}
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