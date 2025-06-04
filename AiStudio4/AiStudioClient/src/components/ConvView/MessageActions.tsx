// AiStudioClient\src\components\ConvView\MessageActions.tsx
import { Clipboard, Pencil, Save, ArrowUp } from 'lucide-react';
import React from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { saveCodeBlockAsFile } from '@/services/api/apiClient';

interface MessageActionsProps {
  message: any;
  onEdit: () => void;
}

export const MessageActions = ({ message, onEdit }: MessageActionsProps) => {
  return (
    <div 
      className="ConvView flex items-center gap-2 pt-2" // Removed mt-2, added pt-2 for padding above actions
    >
      {/* Ellipsis button and conditional rendering logic removed */}
      <div
        className="flex gap-2" // Changed: Removed overflow, whitespace, transition, etc.
        // Removed inline styles for maxWidth and opacity, making it always visible
      >
        <button
          onClick={() => navigator.clipboard.writeText(message.content)}
          className="ConvView p-1.5 rounded-full transition-all duration-200"
          style={{
            color: 'var(--convview-text-color, #9ca3af)',
            backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))', // Kept original style, may need adjustment
            ':hover': {
              color: 'var(--convview-text-color, #ffffff)',
              backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
            }
          }}
          title="Copy message"
        >
          <Clipboard size={16} />
        </button>
        <button
          onClick={onEdit}
          className="ConvView p-1.5 rounded-full transition-all duration-200"
          style={{
            color: 'var(--convview-text-color, #9ca3af)',
            backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))', // Kept original style
            ':hover': {
              color: 'var(--convview-text-color, #ffffff)',
              backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
            }
          }}
          title="Edit raw message"
        >
          <Pencil size={16} />
        </button>
        <button
          onClick={async () => {
            let suggestedFilename = `message.txt`;
            try {
              // const { saveCodeBlockAsFile } = await import('@/services/api/apiClient'); // Already imported at top
              await saveCodeBlockAsFile({ content: message.content, suggestedFilename });
            } catch (e) {
              console.error('Save As failed:', e);
            }
          }}
          className="ConvView p-1.5 rounded-full transition-all duration-200"
          style={{
            color: 'var(--convview-text-color, #9ca3af)',
            backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))', // Kept original style
            ':hover': {
              color: 'var(--convview-text-color, #ffffff)',
              backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
            }
          }}
          title="Save message as file"
        >
          <Save size={16} />
        </button>
        <button
          onClick={async () => {
            // Save conversation up to and including this message
            try {
              // const { saveCodeBlockAsFile } = await import('@/services/api/apiClient'); // Already imported at top
              // Get the conversation from the store
              const { convs, activeConvId } = useConvStore.getState();
              if (activeConvId && convs[activeConvId]) {
                const conv = convs[activeConvId];
                // Find the index of this message in the conversation
                const idx = conv.messages.findIndex(m => m.id === message.id);
                if (idx >= 0) {
                  // Get all messages up to and including this one
                  const upToMessages = conv.messages.slice(0, idx + 1);
                  // Format: include author/source and content for each message
                  const conversationText = upToMessages.map(m => {
                    const author = m.source === 'user' ? 'User' : (m.source === 'ai' ? 'AI' : (m.source || 'Unknown'));
                    const timestamp = m.timestamp ? new Date(m.timestamp).toLocaleString() : '';
                    return `---\n${author}${timestamp ? ` [${timestamp}]` : ''}:\n${m.content}\n`;
                  }).join('\n');
                  let suggestedFilename = `conversation.txt`;
                  await saveCodeBlockAsFile({ content: conversationText, suggestedFilename });
                }
              }
            } catch (e) {
              console.error('Save Conversation As failed:', e);
            }
          }}
          className="ConvView p-1.5 rounded-full transition-all duration-200"
          style={{
            color: 'var(--convview-text-color, #9ca3af)',
            backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))', // Kept original style
            ':hover': {
              color: 'var(--convview-text-color, #ffffff)',
              backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
            }
          }}
          title="Save conversation up to here as file"
        >
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 0 }}>
            <Save size={14} />
            <ArrowUp size={12} style={{ marginLeft: 1, marginTop: 2 }} />
          </span>
        </button>
        <button
          onClick={() => {
            // Find the element with data-message-id equal to message.id
            const el = document.querySelector(`[data-message-id='${message.id}']`);
            if (el) {
              el.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
          }}
          className="ConvView p-1.5 rounded-full transition-all duration-200"
          style={{
            color: 'var(--convview-text-color, #9ca3af)',
            backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
            ':hover': {
              color: 'var(--convview-text-color, #ffffff)',
              backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
            }
          }}
          title="Scroll to top of message"
        >
          <ArrowUp size={16} />
        </button>
      </div>
    </div>
  );
};