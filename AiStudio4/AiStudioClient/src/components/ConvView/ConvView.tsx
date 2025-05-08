// AiStudioClient\src\components\ConvView\ConvView.tsx
import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { useProjectPotatoStore } from '@/stores/useProjectPotatoStore';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
// StickToBottom removed

import { MessageItem } from './MessageItem';
import { StreamingMessage } from './StreamingMessage';
// import { ConversationControls } from './ConversationControls';
import { ScrollManager } from './ScrollManager';

// Define themeable properties for the ConvView component
export const themeableProps = {
};

interface ConvViewProps {
  streamTokens: string[];
  isCancelling?: boolean;
  isStreaming?: boolean;
  lastStreamedContent?: string;
}

export const ConvView = ({ 
  streamTokens, 
  isCancelling = false, 
  isStreaming = false, 
  lastStreamedContent = '' 
}: ConvViewProps) => {
  // Create a ref for the scroll container
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  // Get necessary state from stores
  const { activeConvId, slctdMsgId, convs } = useConvStore();
  
  // Get search results from search store
  const { searchResults } = useSearchStore();
  
  // Add scroll event listener to detect manual scrolling
  useEffect(() => {
    console.log('Setting up scroll detection - effect running');
    
    // Try multiple approaches to detect scrolling
    
    // 1. Direct ref approach
    const scrollContainer = scrollContainerRef.current;
    console.log('Scroll container ref:', scrollContainer);
    
    // 2. Query selector approach
    const convViewElement = document.querySelector('.ConvView');
    console.log('ConvView element from querySelector:', convViewElement);
    
    // 3. Document/window approach
    console.log('Will also try document and window events');
    
    // Common handler function
    const handleScroll = (e: Event) => {
      const target = e.target as HTMLElement;
      console.log('SCROLL EVENT FIRED!', target);
      console.log('Target classList:', target.classList ? Array.from(target.classList) : 'no classes');
      console.log('Target id:', target.id || 'no id');
      
      // Check if this is a ConvView-related scroll
      let isConvViewScroll = false;
      
      // Check if the target is the ConvView or a child of it
      if (scrollContainer && (target === scrollContainer || scrollContainer.contains(target))) {
        isConvViewScroll = true;
      }
      
      // Also check if the target has ConvView class or is a child of an element with ConvView class
      if (target.classList?.contains('ConvView')) {
        isConvViewScroll = true;
      }
      
      let parent = target.parentElement;
      while (parent && !isConvViewScroll) {
        if (parent.classList?.contains('ConvView')) {
          isConvViewScroll = true;
        }
        parent = parent.parentElement;
      }
      
      console.log('Is ConvView scroll:', isConvViewScroll);
      
      // Only process if it's a ConvView scroll
      if (isConvViewScroll) {
        // Get the current state
        const { isEnabled } = useProjectPotatoStore.getState();
        console.log('Current ProjectPotato state:', isEnabled);
        
        // If ProjectPotato is enabled, disable it on ConvView scroll
        if (isEnabled) {
          console.log('Disabling ProjectPotato due to ConvView scroll');
          useProjectPotatoStore.getState().setIsEnabled(false);
          console.log('New ProjectPotato state:', useProjectPotatoStore.getState().isEnabled);
        }
      }
    };
    
    // Add event listeners to all possible elements
    if (scrollContainer) {
      console.log('Adding listener to scrollContainer');
      scrollContainer.addEventListener('scroll', handleScroll, { capture: true });
    }
    
    if (convViewElement) {
      console.log('Adding listener to convViewElement');
      convViewElement.addEventListener('scroll', handleScroll, { capture: true });
    }
    
    console.log('Adding listener to document');
    document.addEventListener('scroll', handleScroll, { capture: true });
    
    console.log('Adding listener to window');
    window.addEventListener('scroll', handleScroll, { capture: true });
    
    return () => {
      console.log('Cleaning up scroll event listeners');
      if (scrollContainer) {
        scrollContainer.removeEventListener('scroll', handleScroll, { capture: true });
      }
      if (convViewElement) {
        convViewElement.removeEventListener('scroll', handleScroll, { capture: true });
      }
      document.removeEventListener('scroll', handleScroll, { capture: true });
      window.removeEventListener('scroll', handleScroll, { capture: true });
    };
  }, []);
  
  // Log when component renders
  console.log('ConvView rendered, scrollContainerRef:', scrollContainerRef.current);
  
  // No need for visibleCount or debouncedScrollTrigger, always show all messages


  // Calculate message chain based on selected message or latest message
  const messageChain = useMemo(() => {
    if (!activeConvId) return [];

    const conv = convs[activeConvId];
    if (!conv || !conv.messages.length) return [];

    // Create copies of messages with explicit properties to avoid loss during graph processing
    const messages = conv.messages.map(msg => ({
      ...msg,
      // Explicitly include these properties to ensure they're not lost
      id: msg.id,
      content: msg.content,
      source: msg.source,
      timestamp: msg.timestamp,
      parentId: msg.parentId,
      durationMs: msg.durationMs,
        costInfo: msg.costInfo,
        cumulativeCost: msg.cumulativeCost,
      attachments: msg.attachments
    }));
    // Get the starting message ID
    const startingMessageId = streamTokens.length > 0
      ? conv.messages[conv.messages.length - 1].id
      : slctdMsgId || conv.messages[conv.messages.length - 1].id;

    const graph = new MessageGraph(messages);
    const path = graph.getMessagePath(startingMessageId);

    return path;
  }, [activeConvId, slctdMsgId, convs, streamTokens.length]);

  // No need to update visibleCount when conversation changes, always show all messages


  // Listen for stream:clear event to reset streamTokens
  useEffect(() => {
    const handleClearStream = () => {
      if (streamTokens.length > 0) {
        // This will clear the displayed tokens in the parent component
        streamTokens = [];
      }
    };
    
    window.addEventListener('stream:clear', handleClearStream);
    return () => {
      window.removeEventListener('stream:clear', handleClearStream);
    };
  }, [streamTokens]);

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
    <div 
      className="ConvView h-full relative overflow-y-auto"
      ref={scrollContainerRef}
      style={{
        backgroundColor: 'var(--global-background-color, transparent)',
        color: 'var(--global-text-color, #ffffff)',
        borderColor: 'var(--global-border-color, rgba(55, 65, 81, 0.3))',
        fontFamily: 'var(--global-font-family, inherit)',
        fontSize: 'var(--global-font-size, inherit)',
        borderRadius: 'var(--global-border-radius, 0)',
        boxShadow: 'var(--global-box-shadow, none)',
        ...(window?.theme?.ConvView?.style || {})
      }}
    >
      <div className="ConvView flex flex-col gap-4 p-4">
        {/* ConversationControls removed: always show all messages */}

        {visibleMessages.map((message) => {
          // Skip system messages
          if (message.source === 'system') return null;
          
          // Force add durationMs property to the message if it doesn't exist
          const enhancedMessage = message;

          // If the message comes from the tree data, it might have the property in the raw data
          if (enhancedMessage.durationMs === undefined) {
            // Check if the message has a matching message in the original conv data
            const conv = convs[activeConvId];
            if (conv) {
              const originalMsg = conv.messages.find(m => m.id === message.id);
              if (originalMsg && 'durationMs' in originalMsg) {
                // Force the property to exist on our message
                Object.defineProperty(enhancedMessage, 'durationMs', {
                  value: originalMsg.durationMs,
                  enumerable: true,
                  configurable: true,
                  writable: true
                });
              }
            }
          }
          
          return (
            <MessageItem 
              key={message.id} 
              message={enhancedMessage} 
              activeConvId={activeConvId} 
            />
          );
        })}

        <StreamingMessage 
          streamTokens={streamTokens}
          isStreaming={isStreaming}
          lastStreamedContent={lastStreamedContent}
        />
      </div>
      
    </div>
  );
};