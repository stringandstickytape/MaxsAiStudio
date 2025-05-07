// AiStudioClient\src\components\ConvView\StreamingMessage.tsx
import { useWebSocketStore } from '@/stores/useWebSocketStore';

interface StreamingMessageProps {
  streamTokens: string[];
  isStreaming: boolean;
  lastStreamedContent: string;
}

export const StreamingMessage = ({ 
  streamTokens, 
  isStreaming, 
  lastStreamedContent 
}: StreamingMessageProps) => {
  const { isCancelling: isCancel } = useWebSocketStore();
  
  // Hide the component when not streaming, regardless of streamTokens
  if (!isStreaming) {
    return null;
  }
  
  // Also hide if there are no tokens to display
  if (!streamTokens.length) {
    return null;
  }
  
  return (
    <div key="streaming-message"
      className="ConvView w-full group flex flex-col relative mb-4">
      
      <div className="ConvView message-container px-4 py-1 shadow-md w-full break-words" style={{
        background: 'var(--ai-message-background, #1f2937)',
        color: 'var(--convview-text-color, #ffffff)',
        borderRadius: '0.5rem',
        borderColor: 'var(--ai-message-border-color, rgba(55, 65, 81, 0.3))',
        borderWidth: 'var(--ai-message-border-width, 0px)',
        borderStyle: 'var(--ai-message-border-style, solid)',
        ...(window?.theme?.ConvView?.style || {})
      }}>
        {(isCancel) && (
          <div className="ConvView mb-2 p-2 rounded border text-sm" style={{
            backgroundColor: 'rgba(146, 64, 14, 0.2)',
            borderColor: 'var(--convview-border-color, rgba(146, 64, 14, 0.5))',
            color: 'var(--convview-accent-color, #fbbf24)'
          }}>
            Cancelling request...
          </div>
        )}
        <div className="w-full mb-4">
          {streamTokens.length > 0 ? (
            <div className="streaming-content">
              {/* Render all tokens as a single string instead of individual components */}
              <span className="whitespace-pre-wrap">{streamTokens.join('')}</span>
            </div>
          ) : isStreaming ? (
            <div className="streaming-content">
              {lastStreamedContent ? (
                <span className="whitespace-pre-wrap">{lastStreamedContent}</span>
              ) : (
                <div/>
              )}
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
};