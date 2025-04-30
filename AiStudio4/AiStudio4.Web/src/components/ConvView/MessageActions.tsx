// AiStudio4.Web\src\components\ConvView\MessageActions.tsx
import { Clipboard, Pencil, Save, ArrowUp } from 'lucide-react';

interface MessageActionsProps {
  message: any;
  onEdit: () => void;
}

export const MessageActions = ({ message, onEdit }: MessageActionsProps) => {
  return (
    <div className="ConvView absolute top-3 right-3 flex gap-2 opacity-0 group-hover:opacity-100 transition-all duration-200">
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
  );
};