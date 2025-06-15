// AiStudioClient\src\components\ConvView\MessageItem.tsx
import React from 'react';
import { MessageAttachments } from '@/components/MessageAttachments';
import { MessageMetadata } from './MessageMetadata';
import { MessageActions } from './MessageActions';
import { MessageEditor } from './MessageEditor';
import { BlockEditor } from './BlockEditor';
import { useState, useRef, useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { useMessageStream } from '@/hooks/useMessageStream';
import { contentBlockRendererRegistry } from '@/components/content/contentBlockRendererRegistry';
import { MessageUtils } from '@/utils/messageUtils';
import { Pencil } from 'lucide-react';

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
  const { streamedContent } = useMessageStream(message.id, isStreamingTarget);
  
  // --- NEW RENDER FUNCTION ---
  const renderContent = () => {
    // Handle live streaming content
    if (isStreamingTarget) {
      const Renderer = contentBlockRendererRegistry.get('text'); // Stream is always text
      return <Renderer block={{ content: streamedContent, contentType: 'text' }} />;
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
          <Renderer block={block} />
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
          className={`ConvView message-container px-3 py-2 shadow-md inline-block ${
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
            ...(window?.theme?.ConvView?.style || {})
          }}>
          {/* <MessageMetadata message={message} /> Moved to bottom */}
          {/* Always render content with block editing capabilities */}
          {renderContent()}

          {!isStreamingTarget && (
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
          )}
        </div>
      </div>
      
      {/* Message metadata and attachments below the message */}
      <div className={`flex flex-col ${message.source === 'user' ? 'items-end' : 'items-start'} mt-1`}>
        <MessageMetadata message={message} />
        
        {((message.attachments && message.attachments.length > 0) || (attachmentsById[message.id] && attachmentsById[message.id].length > 0)) && (
          <div className="ConvView mt-2 pt-2 border-t" style={{
            borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))'
          }}>
            <MessageAttachments attachments={attachmentsById[message.id] || message.attachments} />
          </div>
        )}
      </div>
    </div>
  );
}, arePropsEqual);