import { MarkdownPane } from '@/components/markdown-pane';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { useEffect, useMemo } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';

interface ConvViewProps {
    streamTokens: string[]; // Receive the array of tokens
}

export const ConvView = ({ streamTokens }: ConvViewProps) => {
    const { activeConvId, selectedMessageId, convs } = useConvStore();

    // Get the message chain (active message plus its ancestors)
    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

        // Create a message graph from the conv messages
        const graph = new MessageGraph(conv.messages);

        // If we're actively streaming (generating new messages), always use the most recent message
        // Otherwise, if we have a selected message ID, use that as the starting point for the message chain
        const startingMessageId = streamTokens.length > 0 ?
            conv.messages[conv.messages.length - 1].id :
            (selectedMessageId || conv.messages[conv.messages.length - 1].id);

        console.log('ConvView: Building message chain from:', {
            startingMessageId,
            selectedMessageId,
            streamActive: streamTokens.length > 0,
            messageCount: conv.messages.length
        });

        // Get the path from the starting message back to the root
        return graph.getMessagePath(startingMessageId);

    }, [activeConvId, selectedMessageId, convs, streamTokens.length]);

    useEffect(() => {
        console.log("Message chain updated with length:", messageChain.length);
    }, [messageChain]);

    if (!activeConvId) return null;
    if (!messageChain.length) {
        console.warn('No messages to display in conv:', activeConvId);
        return null;
    }
    
    return (
        <div className="w-full">
            <div className="conv-view w-full">
                {messageChain.map((message) => (
                    <div key={message.id} className="">
                        <div
                            className={`px-4 mb-4 rounded block cursor-pointer ${message.source === 'user' ? ' bg-blue-800' : ' bg-gray-800'} clear-both`}
                        >
                            <MarkdownPane message={message.content} />
                            {(message.tokenUsage || message.costInfo) && (
                                <div className="text-small-gray-400 mt-2 border-t border-gray-700 pt-1">
                                    <div className="flex flex-wrap items-center gap-x-4">
                                        {message.tokenUsage && (
                                            <div className="flex items-center gap-x-2">
                                                <span>Tokens: {message.tokenUsage.inputTokens} in / {message.tokenUsage.outputTokens} out</span>
                                                {(message.tokenUsage.cacheCreationInputTokens > 0 || message.tokenUsage.cacheReadInputTokens > 0) && (
                                                    <span>(Cache: {message.tokenUsage.cacheCreationInputTokens} created, {message.tokenUsage.cacheReadInputTokens} read)</span>
                                                )}
                                            </div>
                                        )}
                                        {message.costInfo && (
                                            <div className="flex items-center gap-x-2">
                                                <span className="flex items-center">Cost: ${message.costInfo.totalCost.toFixed(6)}</span>
                                                <span className="text-gray-500">(${message.costInfo.inputCostPer1M.toFixed(2)}/1M in, ${message.costInfo.outputCostPer1M.toFixed(2)}/1M out)</span>
                                            </div>
                                        )}
                                    </div>
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