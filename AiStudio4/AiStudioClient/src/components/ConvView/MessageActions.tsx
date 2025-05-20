// AiStudioClient\src\components\ConvView\MessageActions.tsx
import { Clipboard, Pencil, Save, ArrowUp, Ellipsis } from 'lucide-react';
import React, { useState, useRef, useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { saveCodeBlockAsFile } from '@/services/api/apiClient';

interface MessageActionsProps {
  message: any;
  onEdit: () => void;
}

export const MessageActions = ({ message, onEdit }: MessageActionsProps) => {
  const [showActions, setShowActions] = useState(false);
  const actionsContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (actionsContainerRef.current) {
      if (showActions) {
        actionsContainerRef.current.style.maxWidth = actionsContainerRef.current.scrollWidth + 'px';
        actionsContainerRef.current.style.opacity = '1';
      } else {
        actionsContainerRef.current.style.maxWidth = '0px';
        actionsContainerRef.current.style.opacity = '0';
      }
    }
  }, [showActions]);

  return (
    <div 
      className="ConvView absolute flex items-center gap-2"
      style={{ top: '-16px', right: '-16px' }} // Position the entire action group
    >
      <button
        onClick={() => setShowActions(!showActions)}
        className="ConvView p-1.5 rounded-full transition-all duration-200 opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto" // Ellipsis button appears on hover
        style={{
          color: 'var(--convview-text-color, #9ca3af)',
          backgroundColor: 'var(--convview-bg, rgba(255, 255, 255, 0.5))',
          backdropFilter: 'blur(2px)',
          WebkitBackdropFilter: 'blur(2px)',
          border: '1px solid rgba(255, 255, 255, 0.2)',
          ':hover': {
            color: 'var(--convview-text-color, #ffffff)',
            backgroundColor: 'var(--convview-bg, rgba(255, 255, 255, 0.8))'
          }
        }}
        title="Toggle message actions"
      >
        <Ellipsis size={16} />
      </button>
      <div
        ref={actionsContainerRef}
        className="flex gap-2 overflow-hidden whitespace-nowrap transition-all duration-300 ease-in-out"
        style={{
          maxWidth: '0px',
          opacity: '0',
        }}>
      <button
        onClick={() => navigator.clipboard.writeText(message.content)}
        className="ConvView p-1.5 rounded-full transition-all duration-200"
        style={{
          color: 'var(--convview-text-color, #9ca3af)',
          backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
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
          backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
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
            const { saveCodeBlockAsFile } = await import('@/services/api/apiClient');
            await saveCodeBlockAsFile({ content: message.content, suggestedFilename });
          } catch (e) {
            console.error('Save As failed:', e);
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
        title="Save message as file"
      >
        <Save size={16} />
      </button>
      <button
        onClick={async () => {
          // Save conversation up to and including this message
          try {
            const { saveCodeBlockAsFile } = await import('@/services/api/apiClient');
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
          backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
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
      </div>
    </div>
  );
};