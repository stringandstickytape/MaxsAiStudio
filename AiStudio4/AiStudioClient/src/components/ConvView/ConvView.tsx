// AiStudioClient\src\components\ConvView\ConvView.tsx
import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { StickToBottom } from 'use-stick-to-bottom';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

import { MessageItem } from './MessageItem';
import { ScrollToBottomButton } from './ScrollToBottomButton';
// import { ConversationControls } from './ConversationControls';

// Define themeable properties for the ConvView component
export const themeableProps = {
};

interface ConvViewProps {
    isCancelling?: boolean;
}

export const ConvView = ({
    isCancelling = false
}: ConvViewProps) => {
    // Create a ref for the scroll container
    const scrollContainerRef = useRef<HTMLDivElement>(null);
    const [isStickingEnabled, setIsStickingEnabled] = useState(true);
    
    // Optimize store subscription - only get what we need
    const activeConvId = useConvStore(state => state.activeConvId);
    const slctdMsgId = useConvStore(state => state.slctdMsgId);
    const convs = useConvStore(state => state.convs);

    // Get search results from search store
    const { searchResults } = useSearchStore();
    
    // Import WebSocket store to check streaming status
    const { hasActiveStreaming, isMessageStreaming } = useWebSocketStore();
    
    // Determine which message is the streaming target
    const lastMessage = useMemo(() => {
        const conv = activeConvId ? convs[activeConvId] : null;
        if (!conv || conv.messages.length === 0) return null;
        return conv.messages[conv.messages.length - 1];
    }, [activeConvId, convs]);

    // Add scroll event listener to detect manual scrolling
    useEffect(() => {
        // Common handler function
        const handleScrollEvent = (e: Event) => {
            const target = e.target as HTMLElement;
            // Check if this is a ConvView-related scroll
            let isConvViewScroll = false;

            // Check if the target is the ConvView or a child of it
            if (scrollContainerRef.current && scrollContainerRef.current.contains(target)) {
                isConvViewScroll = true;
            }

            // Only process if it's a ConvView scroll
            if (isConvViewScroll) {
                // If sticking was enabled, disable it upon manual scroll
                if (isStickingEnabled) {
                    setIsStickingEnabled(false);
                }
            }
        };

        // Add event listeners to all possible elements
        if (scrollContainerRef.current) {
            scrollContainerRef.current.addEventListener('scroll', handleScrollEvent, { capture: true });
        }

        // this doesn't work because race condition
        const convViewElement2 = document.querySelector('.ConvViewMain');
        if (convViewElement2) {
            convViewElement2.addEventListener('scroll', handleScrollEvent, { capture: true });
        }

        document.addEventListener('scroll', handleScrollEvent, { capture: true });

        return () => {
            if (scrollContainerRef.current) {
                scrollContainerRef.current.removeEventListener('scroll', handleScrollEvent, { capture: true });
            }
            if (convViewElement2) {
                convViewElement2.removeEventListener('scroll', handleScrollEvent, { capture: true });
            }
            document.removeEventListener('scroll', handleScrollEvent, { capture: true });
            window.removeEventListener('scroll', handleScrollEvent, { capture: true });
        };
    }, [isStickingEnabled]); // Re-run effect if isStickingEnabled changes

    // Calculate message chain based on selected message or latest message
    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

        // Get the starting message ID
        const startingMessageId = slctdMsgId || conv.messages[conv.messages.length - 1].id;

        const graph = new MessageGraph(conv.messages);
        const path = graph.getMessagePath(startingMessageId);

        return path;
    }, [activeConvId, slctdMsgId, convs]);

    // No need to update visibleCount when conversation changes, always show all messages


    // Removed stream:clear event handling - no longer needed with new streaming system

    // No need for handleLoadMore, always show all messages


    if (!activeConvId) return null;
    if (!messageChain.length) return null;

    // Get the current conversation's search results if any
    const currentConvSearchResult = searchResults?.find(result => result.conversationId === activeConvId);

    // Filter messages based on search results if we have them
    let visibleMessages = messageChain;
    if (searchResults && currentConvSearchResult) {
        visibleMessages = visibleMessages.filter(message =>
            currentConvSearchResult.matchingMessageIds.includes(message.id)
        );
    }

    return (
        <StickToBottom 
            className="ConvView ConvViewMain h-full relative overflow-y-auto w-full"
            ref={scrollContainerRef}
            stickToBottom={isStickingEnabled}
            style={{
                backgroundColor: 'var(--global-background-color, transparent)',
                color: 'var(--global-text-color, #ffffff)',
                borderColor: 'var(--global-border-color, rgba(55, 65, 81, 0.3))',
                fontFamily: 'var(--global-font-family, inherit)',
                fontSize: 'var(--global-font-size, inherit)',
                borderRadius: 'var(--global-border-radius, 0)',
                boxShadow: 'var(--global-box-shadow, none)',
                width: '100%',
                minWidth: '100%',
                ...(window?.theme?.ConvView?.style || {})
            }}
        >
            <StickToBottom.Content className="ConvView flex flex-col gap-4 w-full" style={{width: '100%', minWidth: '100%'}}>
                {/* ConversationControls removed: always show all messages */}

                {visibleMessages.map((message) => {
                    // Skip system messages
                    if (message.source === 'system') return null;

                    // Determine if this message item is the target for the current stream
                    const isStreamingTarget = isMessageStreaming(message.id) && (message.source === 'ai' || message.source === 'assistant');
                    
                    return (
                        <MessageItem
                            key={message.id}
                            message={message}
                            activeConvId={activeConvId}
                            isStreamingTarget={isStreamingTarget}
                        />
                    );
                })}
            </StickToBottom.Content>
            
            {/* Add scroll to bottom button */}
            <ScrollToBottomButton onActivateSticking={() => setIsStickingEnabled(true)} />
        </StickToBottom>
    );
};