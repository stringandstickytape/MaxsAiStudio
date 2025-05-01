// AiStudioClient\src\components\ConvView\ConvView.tsx
import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { StickToBottom } from 'use-stick-to-bottom';

import { MessageItem } from './MessageItem';
import { StreamingMessage } from './StreamingMessage';
import { ConversationControls } from './ConversationControls';
import { ScrollManager } from './ScrollManager';

// Define themeable properties for the ConvView component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--convview-bg',
    description: 'Background color for the conversation view',
    default: 'transparent',
  },
  textColor: {
    cssVar: '--convview-text-color',
    description: 'Text color for messages',
    default: '#ffffff',
  },
  borderColor: {
    cssVar: '--convview-border-color',
    description: 'Border color for dividers',
    default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
  },
  accentColor: {
    cssVar: '--convview-accent-color',
    description: 'Accent color for buttons and highlights',
    default: '#2563eb', // blue-600
  },
  style: {
    description: 'Arbitrary CSS style for the root container',
    default: {},
  },
  userMessageBackground: {
    cssVar: '--user-message-background',
    description: 'Background for user messages (supports gradients, images, etc.)',
    default: '#1e40af', // blue-800
  },
  aiMessageBackground: {
    cssVar: '--ai-message-background',
    description: 'Background for assistant messages (supports gradients, images, etc.)',
    default: '#1f2937', // gray-800
  },
  userMessageBorderColor: {
    cssVar: '--user-message-border-color',
    description: 'Border color for user messages',
    default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
  },
  aiMessageBorderColor: {
    cssVar: '--ai-message-border-color',
    description: 'Border color for assistant messages',
    default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
  },
  userMessageBorderWidth: {
    cssVar: '--user-message-border-width',
    description: 'Border width for user messages',
    default: '0px',
  },
  aiMessageBorderWidth: {
    cssVar: '--ai-message-border-width',
    description: 'Border width for assistant messages',
    default: '0px',
  },
  userMessageBorderStyle: {
    cssVar: '--user-message-border-style',
    description: 'Border style for user messages',
    default: 'solid',
  },
  aiMessageBorderStyle: {
    cssVar: '--ai-message-border-style',
    description: 'Border style for assistant messages',
    default: 'solid',
  }
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
  // Get necessary state from stores
  const { activeConvId, slctdMsgId, convs } = useConvStore();
  
  // Ref for StickToBottom component
  const stickToBottomRef = useRef<any>(null);
  
  // State for visible message count
  const [visibleCount, setVisibleCount] = useState(20);
  const [debouncedScrollTrigger, setDebouncedScrollTrigger] = useState(0);

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

  // Update visible count when conversation changes
  useEffect(() => {
    setVisibleCount(Math.min(20, messageChain.length));
  }, [activeConvId, slctdMsgId, messageChain]);

  // Listen for stream:clear event to reset streamTokens
  useEffect(() => {
    const handleClearStream = () => {
      console.log("handleClearStream");
      if (streamTokens.length > 0) {
        // This will clear the displayed tokens in the parent component
        console.log("handleClearStream2");
        streamTokens = [];
      }
    };
    
    window.addEventListener('stream:clear', handleClearStream);
    return () => {
      window.removeEventListener('stream:clear', handleClearStream);
    };
  }, [streamTokens]);

  // Handle loading more messages
  const handleLoadMore = useCallback(() => {
    setVisibleCount(prev => Math.min(prev + 10, messageChain.length));
  }, [messageChain.length]);

  if (!activeConvId) return null;
  if (!messageChain.length) return null;

  const visibleMessages = messageChain.slice(-visibleCount);
  const hasMoreToLoad = visibleCount < messageChain.length;

  return (
    <StickToBottom 
      className="ConvView h-full relative overflow-y-auto" 
      resize="smooth" 
      initial="smooth"
      ref={stickToBottomRef}
      style={{
        backgroundColor: 'var(--convview-bg, var(--global-background-color, transparent))',
        ...(window?.theme?.ConvView?.style || {})
      }}
      // Add throttle option to reduce ResizeObserver frequency during streaming
      throttle={isStreaming ? 100 : 0}
    >
      <StickToBottom.Content className="ConvView flex flex-col gap-4 p-4">
        <ConversationControls 
          hasMoreToLoad={hasMoreToLoad}
          messageChainLength={messageChain.length}
          visibleCount={visibleCount}
          onLoadMore={handleLoadMore}
        />

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
      </StickToBottom.Content>
      
      <ScrollManager 
        isStreaming={isStreaming} 
        streamTokens={streamTokens} 
      />
    </StickToBottom>
  );
};