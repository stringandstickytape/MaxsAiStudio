import { MarkdownPane } from '@/components/markdown-pane';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { useEffect, useMemo } from 'react';
import { Message } from '@/types/conversation';
import { MessageGraph } from '@/utils/messageGraph';
import { useConversationStore } from '@/stores/useConversationStore';

interface ConversationViewProps {
    streamTokens: string[]; // Receive the array of tokens
}

export const ConversationView = ({ streamTokens }: ConversationViewProps) => {
    const { activeConversationId, selectedMessageId, conversations, getActiveConversation } = useConversationStore();

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
            (selectedMessageId || conversation.messages[conversation.messages.length - 1].id);

        console.log('ConversationView: Building message chain from:', {
            startingMessageId,
            selectedMessageId,
            streamActive: streamTokens.length > 0,
            messageCount: conversation.messages.length
        });

        // Get the path from the starting message back to the root
        return graph.getMessagePath(startingMessageId);

    }, [activeConversationId, selectedMessageId, conversations, streamTokens.length]);

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
                            className={`px-4 mb-4 rounded block cursor-pointer ${message.source === 'user' ? ' bg-blue-800' : ' bg-gray-800'} clear-both`}
                        >
                            <MarkdownPane message={message.content} />
                            {message.tokenUsage && (
                                <div className="text-xs text-gray-400 mt-2 border-t border-gray-700 pt-1">
                                    <span>Tokens: {message.tokenUsage.inputTokens} in / {message.tokenUsage.outputTokens} out</span>
                                    {(message.tokenUsage.cacheCreationInputTokens > 0 || message.tokenUsage.cacheReadInputTokens > 0) && (
                                        <span className="ml-2">(Cache: {message.tokenUsage.cacheCreationInputTokens} created, {message.tokenUsage.cacheReadInputTokens} read)</span>
                                    )}
                                </div>
                            )}
                        </div>
                    </div>
                ))}
                {streamTokens.length > 0 && (
                    <div className="p-4 mb-4 rounded bg-gray-800  clear-both break-words whitespace-normal w-full">
                        {streamTokens.map((token, index) => (
                            <LiveStreamToken key={index} token={token} />
                        ))}
                    </div>
                )}
            </div>
        </div>
    );
};