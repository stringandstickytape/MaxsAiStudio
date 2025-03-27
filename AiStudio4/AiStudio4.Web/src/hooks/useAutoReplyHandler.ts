import { useEffect } from 'react';
import { useAutoReplyStore } from '@/stores/useAutoReplyStore';
import { useChatManagement } from './useChatManagement';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

/**
 * Hook that handles auto-reply functionality by listening to WebSocket events
 * instead of relying on UI component state changes
 */
export function useAutoReplyHandler() {
  const { enabled } = useAutoReplyStore();
  const { sendMessage } = useChatManagement();
  const { currentRequest } = useWebSocketStore();

  useEffect(() => {
    console.log('Setting up auto-reply handler, enabled:', enabled);
    
    // Listen for stream:end events which indicate an AI message has completed
    const unsubscribe = listenToWebSocketEvent('stream:end', (detail) => {
      console.log('Received stream:end event:', detail);
      
      if (!enabled || !currentRequest) {
        console.log('Auto-reply not enabled or no current request');
        return;
      }
      
      console.log('Auto-reply is enabled, preparing to send "." message');
      
      // The message ID in the event should be the AI's response message
      const aiMessageId = detail.content?.messageId;
      if (!aiMessageId) {
        console.error('Cannot send auto-reply: No AI message ID in stream:end event');
        return;
      }
      
      // Wait a bit to ensure UI is updated
      setTimeout(() => {
        console.log('Sending auto-reply with parent:', aiMessageId);
        sendMessage({
          convId: currentRequest.convId,
          parentMessageId: aiMessageId,
          message: '.',
          model: detail.content?.model || 'default',
          toolIds: [],
          // Don't set autoReply flag to prevent infinite loop
        }).catch(err => console.error('Error sending auto-reply:', err));
      }, 5000);
    });
    
    return unsubscribe;
  }, [enabled, sendMessage, currentRequest]);

  return null;
}