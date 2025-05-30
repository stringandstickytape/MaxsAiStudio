﻿// AiStudioClient\src\components\ConvView\MessageItem.tsx
import { MarkdownPane } from '@/components/MarkdownPane';
import { MessageAttachments } from '@/components/MessageAttachments';
import { MessageMetadata } from './MessageMetadata';
import { MessageActions } from './MessageActions';
import { MessageEditor } from './MessageEditor';
import { useState, useRef, useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useAttachmentStore } from '@/stores/useAttachmentStore';

interface MessageItemProps {
  message: any;
  activeConvId: string;
}

export const MessageItem = ({ message, activeConvId }: MessageItemProps) => {
  const { editingMessageId, cancelEditMessage, updateMessage } = useConvStore();
  const { searchResults, highlightedMessageId } = useSearchStore();
  const attachmentsById = useAttachmentStore(state => state.attachmentsById);
  const [editContent, setEditContent] = useState<string>('');
  const messageRef = useRef<HTMLDivElement>(null);
  
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
          <MarkdownPane message={message.content} />
        )}
        
        <MessageMetadata message={message} />
      </div>

      {/* Display message attachments */}
      {/* Get attachments from either the message prop or the centralized store */}
      {((message.attachments && message.attachments.length > 0) || (attachmentsById[message.id] && attachmentsById[message.id].length > 0)) && (
        <div className="ConvView mt-3 pt-3 border-t" style={{
          borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))'
        }}>
          <MessageAttachments attachments={attachmentsById[message.id] || message.attachments} />
        </div>
      )}

      <MessageActions 
        message={message} 
        onEdit={() => {
          setEditContent(message.content);
          useConvStore.getState().editMessage(message.id);
        }} 
      />
    </div>
  );
};