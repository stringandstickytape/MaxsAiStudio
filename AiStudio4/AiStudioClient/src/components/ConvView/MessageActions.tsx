// AiStudioClient\src\components\ConvView\MessageActions.tsx
import { Clipboard, Pencil, Save, ArrowUp } from 'lucide-react';
import React from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { saveCodeBlockAsFile } from '@/services/api/apiClient';
import { MessageUtils } from '@/utils/messageUtils';

interface MessageActionsProps {
  // The message object carries contentBlocks (preferred) and a legacy content string
  message: any;
}

// Custom comparison function for MessageActions memoization
const areActionsPropsEqual = (prevProps: MessageActionsProps, nextProps: MessageActionsProps) => {
  const prevMsg = prevProps.message;
  const nextMsg = nextProps.message;
  
  if (!prevMsg && !nextMsg) return true;
  if (!prevMsg || !nextMsg) return false;
  
  // Compare properties that affect actions
  if (prevMsg.id !== nextMsg.id) return false;
  
  // Compare contentBlocks array
  if (prevMsg.contentBlocks?.length !== nextMsg.contentBlocks?.length) return false;
  if (prevMsg.contentBlocks) {
    for (let i = 0; i < prevMsg.contentBlocks.length; i++) {
      if (prevMsg.contentBlocks[i]?.content !== nextMsg.contentBlocks[i]?.content) return false;
    }
  }
  
  // Note: We don't compare onEdit callback as it's expected to be stable
  // If the parent doesn't memoize it properly, this optimization is still beneficial
  
  return true;
};

export const MessageActions = React.memo(({ message }: MessageActionsProps) => {
  return (
    <div 
      className="ConvView flex items-center gap-2" // Removed mt-2, added pt-2 for padding above actions
    >
      {/* Ellipsis button and conditional rendering logic removed */}
      <div
        className="flex gap-2" // Changed: Removed overflow, whitespace, transition, etc.
        // Removed inline styles for maxWidth and opacity, making it always visible
      >
        <button        // Copy concatenated text of all blocks using MessageUtils
        onClick={() => {
          const flat = MessageUtils.getContent(message);
          navigator.clipboard.writeText(flat);
        }}
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
          onClick={async () => {
            let suggestedFilename = `message.txt`;
            try {
              // const { saveCodeBlockAsFile } = await import('@/services/api/apiClient'); 
                // Already imported at top              
                const flat = MessageUtils.getContent(message);
              await saveCodeBlockAsFile({ content: flat, suggestedFilename });
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
                    return `---\n${author}${timestamp ? ` [${timestamp}]` : ''}:\n${MessageUtils.getContent(m)}\n`;
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
}, areActionsPropsEqual);