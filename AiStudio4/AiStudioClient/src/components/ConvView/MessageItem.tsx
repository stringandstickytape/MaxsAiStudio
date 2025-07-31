// AiStudioClient\src\components\ConvView\MessageItem.tsx
import React from 'react';
import { MessageAttachments } from '@/components/MessageAttachments';
import { MessageActions } from './MessageActions';
import { MessageEditor } from './MessageEditor';
import { BlockEditor } from './BlockEditor';
import { useState, useRef, useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { useMessageStream } from '@/hooks/useMessageStream';
import { contentBlockRendererRegistry } from '@/components/content/contentBlockRendererRegistry';
import { AnimatedStreamingContent } from './AnimatedStreamingContent';
import { MessageUtils } from '@/utils/messageUtils';
import { Pencil } from 'lucide-react';
import { formatModelDisplay } from '@/utils/modelUtils';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';

// MessageMetadata component moved inside MessageItem
interface MessageMetadataProps {
  message: any;
}

// Custom comparison function for MessageMetadata memoization
const areMetadataPropsEqual = (prevProps: MessageMetadataProps, nextProps: MessageMetadataProps) => {
  const prevMsg = prevProps.message;
  const nextMsg = nextProps.message;
  
  if (!prevMsg && !nextMsg) return true;
  if (!prevMsg || !nextMsg) return false;
  
  // Compare properties that affect metadata display
  if (prevMsg.id !== nextMsg.id) return false;
  if (prevMsg.timestamp !== nextMsg.timestamp) return false;
  if (prevMsg.durationMs !== nextMsg.durationMs) return false;
  if (prevMsg.temperature !== nextMsg.temperature) return false;
  if (prevMsg.source !== nextMsg.source) return false;
  if (prevMsg.cumulativeCost !== nextMsg.cumulativeCost) return false;
  
  // Deep compare costInfo object
  const prevCost = prevMsg.costInfo;
  const nextCost = nextMsg.costInfo;
  
  if (!prevCost && !nextCost) return true;
  if (!prevCost || !nextCost) return false;
  
  if (prevCost.totalCost !== nextCost.totalCost) return false;
  if (prevCost.inputCostPer1M !== nextCost.inputCostPer1M) return false;
  if (prevCost.outputCostPer1M !== nextCost.outputCostPer1M) return false;
  if (prevCost.modelGuid !== nextCost.modelGuid) return false;
  
  // Compare tokenUsage
  const prevTokens = prevCost.tokenUsage;
  const nextTokens = nextCost.tokenUsage;
  
  if (!prevTokens && !nextTokens) return true;
  if (!prevTokens || !nextTokens) return false;
  
  if (prevTokens.inputTokens !== nextTokens.inputTokens) return false;
  if (prevTokens.outputTokens !== nextTokens.outputTokens) return false;
  if (prevTokens.cacheCreationInputTokens !== nextTokens.cacheCreationInputTokens) return false;
  if (prevTokens.cacheReadInputTokens !== nextTokens.cacheReadInputTokens) return false;
  
  return true;
};

// Helper function to format duration in a human-readable format
const formatDuration = (message?: any, propName: string = 'durationMs') => {
  // Safety check for null/undefined message
  if (!message) {
    return 'Unknown';
  }
  
  // First try direct property access
  let durationMs = message[propName];
  
  // If that fails, try a few other approaches
  if (durationMs === undefined) {
    // Try Object.getOwnPropertyDescriptor
    const descriptor = Object.getOwnPropertyDescriptor(message, propName);
    if (descriptor) {
      durationMs = descriptor.value;
    }
  }
  
  // Return early if the value is undefined or null
  if (durationMs === undefined || durationMs === null) {
    return "Unknown";
  }
  
  // Ensure we're working with a number
  const duration = Number(durationMs);
  
  // Check if conversion resulted in a valid number
  if (isNaN(duration)) {
    return "Invalid";
  }
  
  // Handle zero case
  if (duration === 0) {
    return '0ms';
  }
  
  // Format based on duration length
  if (duration < 1000) {
    return `${duration}ms`;
  } else if (duration < 60000) {
    return `${(duration / 1000).toFixed(1)}s`;
  } else {
    const minutes = Math.floor(duration / 60000);
    const seconds = Math.floor((duration % 60000) / 1000);
    return `${minutes}m ${seconds}s`;
  }
};

// Helper function to format timestamp
const formatTimestamp = (timestamp?: number | null) => {
  if (!timestamp) return null;
  
  const date = new Date(timestamp);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })  + ' ' + 
        date.toLocaleDateString([], { month: 'short', day: 'numeric' });
};

// MessageMetadata component
const MessageMetadata = React.memo(({ message }: MessageMetadataProps) => {
    // Debug logging to verify values arriving at the component
    try {
        const out = message?.costInfo?.tokenUsage?.outputTokens;
        if (typeof message?.durationMs !== 'undefined') {
            console.debug('MessageMetadata:', message?.id, 'durationMs=', message?.durationMs, 'outputTokens=', out);
        }
    } catch {}
    const metadataItems = [];

    // Timestamp
    if (typeof message.timestamp === 'number' && message.timestamp > 0) {
        metadataItems.push(
            <span key="timestamp" title={new Date(message.timestamp).toLocaleString()}>
                {formatTimestamp(message.timestamp)}
            </span>
        );
    }

    if (message.costInfo && message.costInfo.modelGuid) {
        metadataItems.push(
            <div key="model">
                {formatModelDisplay(message.costInfo.modelGuid)}
            </div>
        );
    }

    // Token and Cost Info

    metadataItems.push(
        <span key="cost" className="flex items-center">
            ${message.costInfo?.totalCost?.toFixed(2)}  Σ {message.cumulativeCost?.toFixed(2)}
        </span>
    );

    // Duration (if available)
    if (typeof message.durationMs === 'number' && message.durationMs >= 0) {
        metadataItems.push(
            <span key="duration" title={`Processing time: ${message.durationMs} ms`}>
                {formatDuration(message, 'durationMs')}
            </span>
        );
    }

    if (message.costInfo && (message.costInfo.tokenUsage.cacheCreationInputTokens > 0 || message.costInfo.tokenUsage.cacheReadInputTokens > 0)) {
        metadataItems.push(
            <span key="cache">
                <span title="Input tokens">{message.costInfo.tokenUsage.inputTokens} ⬆️</span>{' '}
                <span title="Output tokens">{message.costInfo.tokenUsage.outputTokens} ⬇️</span> { '!! '}
                <span title="Cache creation input tokens">{message.costInfo.tokenUsage.cacheCreationInputTokens}🌟</span> {' '}
                <span title="Cache read input tokens">{message.costInfo.tokenUsage.cacheReadInputTokens}📖</span>
            </span>
        );
    } else if (message.costInfo) {
        metadataItems.push(
            <span key="tokens">
                {message.costInfo.tokenUsage.inputTokens} ⬆️ {message.costInfo.tokenUsage.outputTokens} ⬇️
            </span>
        );
    }

    // Tokens per second (output-only), only if durationMs > 0 and outputTokens available
    if (message.costInfo && typeof message.durationMs === 'number' && message.durationMs > 0) {
        const outputTokens = message.costInfo.tokenUsage?.outputTokens ?? 0;
        if (outputTokens > 0) {
            const seconds = message.durationMs / 1000;
            const tps = seconds > 0 ? (outputTokens / seconds) : 0;
            metadataItems.push(
                <span key="tps" title={`Output TPS over ${seconds.toFixed(2)}s`}>
                    {tps.toFixed(1)} t/s
                </span>
            );
        }
    }

    if (metadataItems.length === 0 && !message.id) {
        return null;
    }


  return (
      <div className="ConvView flex flex-row items-center text-[0.75rem] overflow-x-hidden max-h-[2rem]">
      {metadataItems.map((item, index) => (
        <React.Fragment key={index}>
          {item}
          {index < metadataItems.length - 1 && <span className="ConvView mx-2 text-gray-500">|</span>}
        </React.Fragment>
      ))}
    </div>
  );
}, areMetadataPropsEqual);

interface MessageItemProps {
  message: any;
  activeConvId: string;
  isStreamingTarget?: boolean; // New prop to indicate if this message is actively streaming
}

// Custom comparison function for better memoization
const arePropsEqual = (prevProps: MessageItemProps, nextProps: MessageItemProps) => {
  // Compare primitive props
  if (prevProps.activeConvId !== nextProps.activeConvId) return false;
  if (prevProps.isStreamingTarget !== nextProps.isStreamingTarget) return false;
  
  // Deep compare message properties that affect rendering
  const prevMsg = prevProps.message;
  const nextMsg = nextProps.message;
  
  if (!prevMsg && !nextMsg) return true;
  if (!prevMsg || !nextMsg) return false;
  
  // Compare key message properties
  if (prevMsg.id !== nextMsg.id) return false;
  if (prevMsg.source !== nextMsg.source) return false;
  if (prevMsg.timestamp !== nextMsg.timestamp) return false;
  
  // Ensure metadata changes trigger rerender
  if ((prevMsg.durationMs ?? null) !== (nextMsg.durationMs ?? null)) return false;
  if ((prevMsg.cumulativeCost ?? null) !== (nextMsg.cumulativeCost ?? null)) return false;
  const prevCost = prevMsg.costInfo;
  const nextCost = nextMsg.costInfo;
  if (!!prevCost !== !!nextCost) return false; // one is null, other not
  if (prevCost && nextCost) {
    if ((prevCost.totalCost ?? null) !== (nextCost.totalCost ?? null)) return false;
    if ((prevCost.inputCostPer1M ?? null) !== (nextCost.inputCostPer1M ?? null)) return false;
    if ((prevCost.outputCostPer1M ?? null) !== (nextCost.outputCostPer1M ?? null)) return false;
    if ((prevCost.modelGuid ?? null) !== (nextCost.modelGuid ?? null)) return false;
    const prevTok = prevCost.tokenUsage;
    const nextTok = nextCost.tokenUsage;
    if (!!prevTok !== !!nextTok) return false;
    if (prevTok && nextTok) {
      if ((prevTok.inputTokens ?? null) !== (nextTok.inputTokens ?? null)) return false;
      if ((prevTok.outputTokens ?? null) !== (nextTok.outputTokens ?? null)) return false;
      if ((prevTok.cacheCreationInputTokens ?? null) !== (nextTok.cacheCreationInputTokens ?? null)) return false;
      if ((prevTok.cacheReadInputTokens ?? null) !== (nextTok.cacheReadInputTokens ?? null)) return false;
    }
  }
  
  // Compare contentBlocks array
  if (prevMsg.contentBlocks?.length !== nextMsg.contentBlocks?.length) return false;
  if (prevMsg.contentBlocks) {
    for (let i = 0; i < prevMsg.contentBlocks.length; i++) {
      if (prevMsg.contentBlocks[i]?.content !== nextMsg.contentBlocks[i]?.content) return false;
    }
  }
  
  // Compare attachments array
  if (prevMsg.attachments?.length !== nextMsg.attachments?.length) return false;
  if (prevMsg.attachments) {
    for (let i = 0; i < prevMsg.attachments.length; i++) {
      if (prevMsg.attachments[i]?.id !== nextMsg.attachments[i]?.id) return false;
    }
  }
  
  return true;
};

export const MessageItem = React.memo(({ message, activeConvId, isStreamingTarget = false }: MessageItemProps) => {
  const { editingMessageId, editingBlock, cancelEditMessage, cancelEditBlock, updateMessage, updateMessageBlock, editBlock, editMessage } = useConvStore();
  const { searchResults, highlightedMessageId } = useSearchStore();
  const attachmentsById = useAttachmentStore(state => state.attachmentsById);
  const [editContent, setEditContent] = useState<string>('');
  const [hoveredBlockIndex, setHoveredBlockIndex] = useState<number | null>(null);
  const messageRef = useRef<HTMLDivElement>(null);
  
  // Calculate if this message is in edit mode
  const isThisMessageInEditMode = editingMessageId === message.id || editingBlock?.messageId === message.id;
  
  
  // Use the new streaming hook
  const { streamedContent, newContentInfo } = useMessageStream(message.id, isStreamingTarget);
  
  // --- NEW RENDER FUNCTION ---
  const renderContent = () => {
      // Handle live streaming content

      
    if (isStreamingTarget) {
      console.log(`🎯 MessageItem: Using streaming renderer for ${message.id}, content="${streamedContent}"`);
      // Use animated streaming content renderer
      return (
        <AnimatedStreamingContent
          content={streamedContent}
          messageId={message.id}
          newContentInfo={newContentInfo}
        />
      );
    }

    // Render all stored content blocks for the message
    if (!message.contentBlocks || message.contentBlocks.length === 0) {
      return null;
    }

    // Check if this message is being edited at block level
    const isBlockEditing = editingBlock?.messageId === message.id;

    return message.contentBlocks.map((block: any, index: number) => {
      const key = `${message.id}-block-${index}`;
      
      // If this specific block is being edited, render the BlockEditor
      if (isBlockEditing && editingBlock?.blockIndex === index) {
        return (
          <BlockEditor
            key={key}
            initialContent={block.content}
            onSave={(newContent) => {
              updateMessageBlock(activeConvId, message.id, index, newContent);
            }}
            onCancel={() => {
              cancelEditBlock();
            }}
          />
        );
      }

      // Otherwise, render the block with hover highlighting and click-to-edit when in edit mode
      const Renderer = contentBlockRendererRegistry.get(block.contentType);
      const isThisBlockHovered = hoveredBlockIndex === index;
      
      return (
        <div 
          key={key} 
          className={`relative ${isThisMessageInEditMode ? 'cursor-pointer' : ''}`}
          style={isThisMessageInEditMode && isThisBlockHovered ? {
            borderLeft: '4px solid var(--global-secondary-color, #6b7280)',
            paddingLeft: '8px',
            transition: 'border-left 0.05s ease'
          } : {
            transition: 'border-left 0.05s ease'
          }}
          onMouseEnter={() => isThisMessageInEditMode && setHoveredBlockIndex(index)}
          onMouseLeave={() => isThisMessageInEditMode && setHoveredBlockIndex(null)}
          onClick={() => {
            if (isThisMessageInEditMode) {
              // Close global edit mode and start block editing
              cancelEditMessage();
              editBlock(message.id, index);
            }
          }}
        >
          <Renderer block={block} messageId={message.id} />
        </div>
      );
    });
  };
  
  // Check if this message matches the search
  const isSearchMatch = searchResults?.some(result => 
    result.matchingMessageIds.includes(message.id)
  );
  
  // Check if this is the currently highlighted message
  const isHighlighted = highlightedMessageId === message.id;
  
  // Scroll to highlighted message
  useEffect(() => {
    if (isHighlighted && messageRef.current) {
      setTimeout(() => {
        messageRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 100);
    }
  }, [isHighlighted]);


  return (
    <div
      className="ConvView w-full group flex flex-col relative markdown-pane"
      data-message-id={message.id}
      ref={messageRef}>

      {/* Message bubble container using CSS Grid to force full width */}
      <div 
        className="w-full grid"
        style={{
          minWidth: '100%',
          gridTemplateColumns: message.source === 'user' ? '1fr auto' : 'auto 1fr',
          width: '100%'
        }}
      >
        
        <div 
          className={`ConvView message-container px-3 pt-2 shadow-md inline-block ${
            message.source === 'user' ? 'justify-self-end' : 'justify-self-start'
          }`}
          style={{
            background: message.source === 'user' 
              ? 'var(--global-user-message-background, #1e40af)' 
              : 'var(--global-ai-message-background, #1f2937)',
            color: message.source === 'user'
              ? 'var(--global-user-message-text-color, #ffffff)'
              : 'var(--global-ai-message-text-color, #ffffff)',
            borderRadius: 'var(--global-border-radius, 0.5rem)',
            borderColor: message.source === 'user'
              ? 'var(--global-user-message-border-color, rgba(55, 65, 81, 0.3))'
              : 'var(--global-ai-message-border-color, rgba(55, 65, 81, 0.3))',
            borderWidth: message.source === 'user'
              ? 'var(--global-user-message-border-width, 0px)'
              : 'var(--global-ai-message-border-width, 0px)',
            borderStyle: message.source === 'user'
              ? 'var(--global-user-message-border-style, solid)'
              : 'var(--global-ai-message-border-style, solid)',
            ...(isSearchMatch && {
              borderLeft: '3px solid var(--global-primary-color, #2563eb)',
              paddingLeft: '0.5rem',
              backgroundColor: message.source === 'user' 
                ? 'rgba(37, 99, 235, 0.2)' // Blue with opacity for user messages
                : 'rgba(37, 99, 235, 0.1)', // Lighter blue for AI messages
            }),
            ...(isHighlighted && {
              scrollMarginTop: '100px', // Ensures the element is visible when scrolled to
              boxShadow: '0 0 0 2px var(--convview-accent-color, #2563eb)'
            }),
            ...(window?.theme?.ConvView?.style || {})          }}
        >
          {/* Always render content with block editing capabilities */}
          {renderContent()}
          


                  <div className="flex justify-between items-center">
                      <MessageActions
                          message={message}
                          onEdit={() => {
                              // Get current state at click time to avoid stale closure
                              const currentState = useConvStore.getState();
                              const isCurrentlyEditing = currentState.editingMessageId === message.id || currentState.editingBlock?.messageId === message.id;

                              if (isCurrentlyEditing) {
                                  cancelEditMessage();
                                  cancelEditBlock();
                                  setHoveredBlockIndex(null); // Clear hover state when exiting edit mode
                              } else {
                                  editMessage(message.id);
                              }
                          }}
                          isInEditMode={isThisMessageInEditMode}
                      />

                      <MessageMetadata message={message} />
                  </div>
        </div>
      </div>      {/* Message attachments below the message */}
      {((message.attachments && message.attachments.length > 0) || (attachmentsById[message.id] && attachmentsById[message.id].length > 0)) && (
        <div className={`flex flex-col ${message.source === 'user' ? 'items-end' : 'items-start'} mt-1`}>
          <div className="ConvView mt-2 pt-2 border-t" style={{
            borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))'
          }}>
            <MessageAttachments attachments={attachmentsById[message.id] || message.attachments} />
          </div>
        </div>
      )}
    </div>
  );
}, arePropsEqual);