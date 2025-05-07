// AiStudioClient\src\utils\debugUtils.ts
import { useConvStore } from '@/stores/useConvStore';
import { v4 as uuidv4 } from 'uuid';
import { dispatchWebSocketEvent } from '@/services/websocket/websocketEvents';

/**
 * Initializes debug utilities for the application
 * These are attached to the window object for console access
 */
export function initDebugUtils() {
  // Keep track of any active message generation process
  let activeMessageGeneration: {
    timeoutIds: NodeJS.Timeout[];
    isRunning: boolean;
  } | null = null;

  /**
   * Adds a random number of messages to the current conversation at random intervals
   * with streaming tokens between messages to simulate live typing
   * @param count Number of messages to add (default: random between 5-20)
   * @param minDelay Minimum delay between messages in ms (default: 100)
   * @param maxDelay Maximum delay between messages in ms (default: 1000)
   * @param chunkSize Size of each streaming token chunk in characters (default: 15)
   * @returns A function to stop the message generation
   */
  window.addRandomMessages = async (count?: number, minDelay = 100, maxDelay = 1000, chunkSize = 15) => {
    // If already running, stop the previous generation
    if (activeMessageGeneration?.isRunning) {
      console.log('Stopping previous message generation');
      stopMessageGeneration();
    }

    // Get the conversation store
    const { activeConvId, convs } = useConvStore.getState();
    
    if (!activeConvId) {
      console.error('No active conversation');
      return () => {};
    }

    // Get the current conversation
    const conv = convs[activeConvId];
    if (!conv) {
      console.error('Active conversation not found');
      return () => {};
    }

    // Determine how many messages to add
    const messagesToAdd = count || Math.floor(Math.random() * 15) + 5; // 5-20 messages
    console.log(`Adding ${messagesToAdd} random messages with delays between ${minDelay}-${maxDelay}ms`);

    // Keep track of timeouts so we can cancel them if needed
    const timeoutIds: NodeJS.Timeout[] = [];
    activeMessageGeneration = {
      timeoutIds,
      isRunning: true
    };

    // Add messages with random delays
    let previousMessageId = '';
    
    // Get the last message ID to use as parent for the first message
    const lastMessage = conv.messages[conv.messages.length - 1];
    let parentId = lastMessage?.id || '';
    
    // Process messages one by one, waiting for each to complete before starting the next
    for (let i = 0; i < messagesToAdd; i++) {
      if (!activeMessageGeneration?.isRunning) break;
      
      const delay = Math.floor(Math.random() * (maxDelay - minDelay)) + minDelay;
      
      // Wait for the random delay before starting this message
      await new Promise(resolve => setTimeout(resolve, delay));
      
      if (!activeMessageGeneration?.isRunning) break;

      const messageId = uuidv4();
      const timestamp = Date.now();
      
      // Alternate between user and AI messages
      const source = i % 2 === 0 ? 'user' : 'ai';
      const content = source === 'user' 
        ? `Test user message #${i + 1} (added for scroll debugging)` 
        : `This is a test AI response message #${i + 1} generated for debugging scroll behavior. It contains multiple lines\n\nof text to ensure it takes up vertical space\n\nand helps identify any scrolling issues.`;

      // For AI messages, simulate streaming tokens before sending the final message
      if (source === 'ai') {
        // First dispatch stream:token events to simulate typing
        await simulateStreamingTokens(content, chunkSize, 20);
        
        if (!activeMessageGeneration?.isRunning) break;
        
        // Dispatch a stream:end event to signal the end of streaming
        dispatchWebSocketEvent('stream:end', {
          type: 'end',
          content: null
        });
        
        // Small delay after stream:end before sending the final message
        await new Promise(resolve => setTimeout(resolve, 100));
        
        if (!activeMessageGeneration?.isRunning) break;
        
        // Then dispatch the conv:upd event with the complete message
        dispatchWebSocketEvent('conv:upd', {
          type: 'message',
          content: {
            id: messageId,
            content,
            source,
            timestamp,
            parentId,
            convId: activeConvId,
            durationMs: Math.floor(Math.random() * 500) + 100 // Random duration between 100-600ms
          }
        });
        
        console.log(`Added AI message ${i + 1}/${messagesToAdd} with parent ${parentId}`);
      } else {
        // For user messages, just dispatch the message event directly
        dispatchWebSocketEvent('conv:upd', {
          type: 'message',
          content: {
            id: messageId,
            content,
            source,
            timestamp,
            parentId,
            convId: activeConvId,
            durationMs: Math.floor(Math.random() * 500) + 100 // Random duration between 100-600ms
          }
        });
        
        console.log(`Added user message ${i + 1}/${messagesToAdd} with parent ${parentId}`);
      }
      
      // Update parentId for the next message
      parentId = messageId;
    }

    console.log('Finished adding random messages');
    if (activeMessageGeneration) {
      activeMessageGeneration.isRunning = false;
    }

    // Return a function to stop the message generation
    return stopMessageGeneration;
  };

  /**
   * Simulates streaming tokens for AI messages
   * @param content The full content to stream
   * @param chunkSize Size of each chunk in characters
   * @param delayBetweenTokens Delay between token events in ms
   * @returns Promise that resolves when all tokens have been streamed
   */
  async function simulateStreamingTokens(content: string, chunkSize: number, delayBetweenTokens: number): Promise<void> {
    // Split the content into chunks of the specified size
    const chunks: string[] = [];
    
    for (let i = 0; i < content.length; i += chunkSize) {
      chunks.push(content.substring(i, Math.min(i + chunkSize, content.length)));
    }
    
    // Dispatch token events with delays, waiting for each one
    for (let i = 0; i < chunks.length; i++) {
      if (!activeMessageGeneration?.isRunning) break;
      
      // Dispatch a stream:token event for this chunk
      dispatchWebSocketEvent('stream:token', {
        type: 'fragment',
        content: chunks[i]
      });
      
      console.log(`Streamed token ${i + 1}/${chunks.length}: ${chunks[i].length} chars`);
      
      // Wait for the delay before sending the next token
      await new Promise(resolve => setTimeout(resolve, delayBetweenTokens));
    }
  }

  /**
   * Stops any active message generation
   */
  function stopMessageGeneration() {
    if (activeMessageGeneration) {
      activeMessageGeneration.isRunning = false;
      activeMessageGeneration.timeoutIds.forEach(clearTimeout);
      activeMessageGeneration = null;
      console.log('Message generation stopped');
    }
  }

  // Also expose the stop function globally
  window.stopRandomMessages = stopMessageGeneration;

  console.log('Debug utilities initialized. Available commands:\n' +
    '- window.addRandomMessages(count?, minDelay?, maxDelay?, chunkSize?)\n' +
    '- window.stopRandomMessages()'
  );
}

// Add TypeScript declarations for the global functions
declare global {
  interface Window {
    addRandomMessages: (count?: number, minDelay?: number, maxDelay?: number, chunkSize?: number) => Promise<() => void>;
    stopRandomMessages: () => void;
  }
}