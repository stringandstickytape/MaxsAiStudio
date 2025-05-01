// AiStudioClient\src\components\ConvView\MessageItem.tsx
import { MarkdownPane } from '@/components/MarkdownPane';
import { MessageAttachments } from '@/components/MessageAttachments';
import { MessageMetadata } from './MessageMetadata';
import { MessageActions } from './MessageActions';
import { MessageEditor } from './MessageEditor';
import { useState } from 'react';
import { useConvStore } from '@/stores/useConvStore';

interface MessageItemProps {
  message: any;
  activeConvId: string;
}

export const MessageItem = ({ message, activeConvId }: MessageItemProps) => {
  const { editingMessageId, cancelEditMessage, updateMessage } = useConvStore();
  const [editContent, setEditContent] = useState<string>('');

  return (
    <div
      className="ConvView w-full group flex flex-col relative markdown-pane"
      data-message-id={message.id}
    >
      <div 
        className={`ConvView message-container px-4 py-3 shadow-md w-full`}
        style={{
          background: message.source === 'user' 
            ? 'var(--user-message-background, #1e40af)' 
            : 'var(--ai-message-background, #1f2937)',
          color: 'var(--convview-text-color, #ffffff)',
          borderRadius: 'var(--convview-border-radius, 0.5rem)',
          borderColor: message.source === 'user'
            ? 'var(--user-message-border-color, rgba(55, 65, 81, 0.3))'
            : 'var(--ai-message-border-color, rgba(55, 65, 81, 0.3))',
          borderWidth: message.source === 'user'
            ? 'var(--user-message-border-width, 0px)'
            : 'var(--ai-message-border-width, 0px)',
          borderStyle: message.source === 'user'
            ? 'var(--user-message-border-style, solid)'
            : 'var(--ai-message-border-style, solid)',
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
      {message.attachments && message.attachments.length > 0 && (
        <div className="ConvView mt-3 pt-3 border-t" style={{
          borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))'
        }}>
          <MessageAttachments attachments={message.attachments} />
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