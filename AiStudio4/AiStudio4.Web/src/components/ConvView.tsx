import { MarkdownPane } from '@/components/markdown-pane';
import { useVirtualizer } from '@tanstack/react-virtual';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { useEffect, useMemo, useRef } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';

interface ConvViewProps {
  streamTokens: string[]; // Receive the array of tokens
}

export const ConvView = ({ streamTokens }: ConvViewProps) => {
  const { activeConvId, slctdMsgId, convs } = useConvStore();
  
  // Reference for the parent container
  const parentRef = useRef<HTMLDivElement>(null);

  // Get the message chain (active message plus its ancestors)
  const messageChain = useMemo(() => {
    if (!activeConvId) return [];

    const conv = convs[activeConvId];
    if (!conv || !conv.messages.length) return [];

    // Create a message graph from the conv messages
    const graph = new MessageGraph(conv.messages);

    // If we're actively streaming (generating new messages), always use the most recent message
    // Otherwise, if we have a selected message ID, use that as the starting point for the message chain
    const startingMessageId =
      streamTokens.length > 0
        ? conv.messages[conv.messages.length - 1].id
        : slctdMsgId || conv.messages[conv.messages.length - 1].id;

    console.log('ConvView: Building message chain from:', {
      startingMessageId,
      slctdMsgId,
      streamActive: streamTokens.length > 0,
      messageCount: conv.messages.length,
    });

    // Get the path from the starting message back to the root
    return graph.getMessagePath(startingMessageId);
  }, [activeConvId, slctdMsgId, convs, streamTokens.length]);

  // Virtualized list setup for better performance with large conversations
  const virtualizer = useVirtualizer({
    count: messageChain.length + (streamTokens.length > 0 ? 1 : 0),
    getScrollElement: () => parentRef.current,
    estimateSize: (index) => {
      // Use more adaptive size estimates based on content length
      if (index < messageChain.length) {
        const message = messageChain[index];
        // Calculate rough estimate based on content length
        const contentLength = message.content.length;
        // More accurate height estimation with character count and line breaks
        const lineBreaks = (message.content.match(/\n/g) || []).length;
        const baseHeight = Math.max(100, Math.min(1000, contentLength * 0.6));
        const lineBreakHeight = lineBreaks * 20; // Estimate 20px per line break
        return baseHeight + lineBreakHeight;
      }
      return 200; // Default for streaming tokens
    },
    overscan: 5, // Number of items to render outside of the visible window
    paddingStart: 16, // Add padding at the top
    paddingEnd: 16, // Add padding at the bottom
    measureElement: (element) => {
      // Get actual rendered height for more accurate virtualization
      return element.getBoundingClientRect().height;
    }
  });

  // Log message chain updates
  useEffect(() => {
    console.log('Message chain updated with length:', messageChain.length);
  }, [messageChain]);
  
  // Measure rendered items to improve virtualization accuracy
  useEffect(() => {
    if (!parentRef.current) return;
    
    // Create a MutationObserver to detect when new content is added to the DOM
    const mutationObserver = new MutationObserver(() => {
      // Wait for the next animation frame to ensure DOM updates have completed
      requestAnimationFrame(() => {
        virtualizer.measure();
      });
    });
    
    // Observe the container for any changes to its children
    mutationObserver.observe(parentRef.current, { 
      childList: true, 
      subtree: true,
      characterData: true,
      attributes: true
    });
    
    // Also set up a resize observer for the parent container
    const resizeObserver = new ResizeObserver(() => {
      virtualizer.measure();
    });
    
    // Observe the parent element to detect size changes
    resizeObserver.observe(parentRef.current);
    
    return () => {
      mutationObserver.disconnect();
      resizeObserver.disconnect();
    };
  }, [virtualizer]);

  if (!activeConvId) return null;
  if (!messageChain.length) {
    console.warn('No messages to display in conv:', activeConvId);
    return null;
  }

  return (
    <div className="w-full h-full overflow-auto" ref={parentRef}>
      <div className="conv-view w-full relative pb-4" style={{ height: `${virtualizer.getTotalSize()}px` }}>
        {/* Render only visible messages using virtualization */}
        {virtualizer.getVirtualItems().map((virtualItem) => {
          // Check if this is the streaming tokens item (last one)
          const isStreamingItem = streamTokens.length > 0 && virtualItem.index === messageChain.length;
          
          if (isStreamingItem) {
            return (
              <div 
                key="streaming-tokens"
                className="absolute top-0 left-0 w-full" 
                style={{
                  transform: `translateY(${virtualItem.start}px)`,
                  height: `${virtualItem.size}px`,
                  boxSizing: 'border-box'
                }}
                data-index={virtualItem.index}
                ref={virtualizer.measureElement}
              >
                <div className="p-4 mb-4 rounded bg-gray-800 clear-both break-words whitespace-normal w-full">
                  {streamTokens.map((token, index) => (
                    <LiveStreamToken key={index} token={token} />
                  ))}
                </div>
              </div>
            );
          }
          
          // Regular message item
          const message = messageChain[virtualItem.index];
          return (
            <div 
              key={message.id}
              className="absolute top-0 left-0 w-full" 
              style={{
                transform: `translateY(${virtualItem.start}px)`,
                height: `${virtualItem.size}px`,
                boxSizing: 'border-box'
              }}
              data-index={virtualItem.index}
              ref={virtualizer.measureElement}
            >
              <div
                className={`px-4 mb-4 rounded block cursor-pointer ${message.source === 'user' ? ' bg-blue-800' : ' bg-gray-800'} clear-both w-full h-auto overflow-hidden`}
                style={{ minHeight: '50px' }} // Ensure minimum height for messages
              >
                <MarkdownPane message={message.content} />
                {(message.tokenUsage || message.costInfo) && (
                  <div className="text-small-gray-400 mt-2 border-t border-gray-700 pt-1">
                    <div className="flex flex-wrap items-center gap-x-4">
                      {message.tokenUsage && (
                        <div className="flex items-center gap-x-2">
                          <span>
                            Tokens: {message.tokenUsage.inputTokens} in / {message.tokenUsage.outputTokens} out
                          </span>
                          {(message.tokenUsage.cacheCreationInputTokens > 0 ||
                            message.tokenUsage.cacheReadInputTokens > 0) && (
                            <span>
                              (Cache: {message.tokenUsage.cacheCreationInputTokens} created,{' '}
                              {message.tokenUsage.cacheReadInputTokens} read)
                            </span>
                          )}
                        </div>
                      )}
                      {message.costInfo && (
                        <div className="flex items-center gap-x-2">
                          <span className="flex items-center">Cost: ${message.costInfo.totalCost.toFixed(6)}</span>
                          <span className="text-gray-500">
                            (${message.costInfo.inputCostPer1M.toFixed(2)}/1M in, $
                            {message.costInfo.outputCostPer1M.toFixed(2)}/1M out)
                          </span>
                        </div>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};