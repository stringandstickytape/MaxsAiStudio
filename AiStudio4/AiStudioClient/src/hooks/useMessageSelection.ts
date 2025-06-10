// src/hooks/useMessageSelection.ts
import { useCallback } from 'react';
import { useConvStore } from '@/stores/useConvStore';

export function useMessageSelection() {
  // Use separate store calls to avoid object creation causing infinite re-renders
  const convs = useConvStore(state => state.convs);
  const activeConvId = useConvStore(state => state.activeConvId);

  /**
   * Selects a message in a conversation, making it the current head of the branch.
   * @param messageId The ID of the message to select.
   * @param convId The ID of the conversation. Defaults to the active conversation.
   */
  const selectMessage = useCallback((messageId: string, convId?: string) => {
    const targetConvId = convId || activeConvId;
    if (!targetConvId) return;

    // Get the selectMessage function directly from store to avoid subscription issues
    useConvStore.getState().selectMessage(targetConvId, messageId);

    // Update URL for deep linking
    window.history.pushState({}, '', `?messageId=${messageId}`);
  }, [activeConvId]);

  /**
   * Selects the most recent message in the active conversation.
   */
  const selectLatestMessage = useCallback(() => {
    if (!activeConvId) return;
    const conv = convs[activeConvId];
    if (!conv || conv.messages.length === 0) return;

    const latestMessage = conv.messages.reduce((latest, current) => 
      current.timestamp > latest.timestamp ? current : latest
    );

    selectMessage(latestMessage.id, activeConvId);
  }, [activeConvId, convs, selectMessage]);

  return { selectMessage, selectLatestMessage };
}