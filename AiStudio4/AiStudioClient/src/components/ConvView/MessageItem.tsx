// AiStudioClient\src\components\ConvView\MessageItem.tsx
import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { MessageAttachments } from '@/components/MessageAttachments';
import { MessageMetadata } from './MessageMetadata';
import { MessageActions } from './MessageActions';
import { MessageEditor } from './MessageEditor';
import { useState, useRef, useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { useMessageStream } from '@/hooks/useMessageStream';

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
  if (prevMsg.content !== nextMsg.content) return false;
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
  const { editingMessageId, cancelEditMessage, updateMessage } = useConvStore();
  const { searchResults, highlightedMessageId } = useSearchStore();
  const attachmentsById = useAttachmentStore(state => state.attachmentsById);  const [editContent, setEditContent] = useState<string>('');

  // Helper: flattened text for rendering
  const flattenedContent = message.contentBlocks && message.contentBlocks.length
    ? (message.contentBlocks as any[]).map((cb: any) => cb.content).join('\n\n')
    : message.content;
  const messageRef = useRef<HTMLDivElement>(null);
  
  // Use the new streaming hook
  const { streamedContent } = useMessageStream(message.id, isStreamingTarget);
  
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

      <div 
        className={`ConvView message-container px-3 py-2 shadow-md w-full`}
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
        {editingMessageId === message.id ? (
          <MessageEditor 
            editContent={editContent} 
            setEditContent={setEditContent} 
            onSave={() => {
              if (activeConvId) {
                updateMessage({
                  convId: activeConvId,
                  messageId: message.id,
                  content: editContent
                });
                cancelEditMessage();
              }
            }}
            onCancel={() => cancelEditMessage()}
          />
        ) : (
          <>            <MarkdownPane message={(flattenedContent ?? '') + streamedContent} />
            {/* Only show actions if not actively streaming */}
            {!isStreamingTarget && (
              <MessageActions 
                message={message} 
                onEdit={() => {                  setEditContent(flattenedContent);
                  useConvStore.getState().editMessage(message.id);
                }} 
              />
            )}
          </>
        )}
        
      </div>
          <MessageMetadata message={message} />
      {((message.attachments && message.attachments.length > 0) || (attachmentsById[message.id] && attachmentsById[message.id].length > 0)) && (
        <div className="ConvView mt-3 pt-3 border-t" style={{
          borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))'
        }}>
          <MessageAttachments attachments={attachmentsById[message.id] || message.attachments} />
        </div>
      )}

      {/* MessageActions was here, moved inside the message-container div */}
    </div>
  );
}, arePropsEqual);