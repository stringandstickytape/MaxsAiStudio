// AiStudioClient/src/services/windowEvents.ts

/**
 * Registry of window events used throughout the application.
 * This helps document the events and their purposes.
 */
export const WindowEvents = {
  // System prompt events
  SYSTEM_PROMPT_SELECTED: 'system-prompt-selected',
  SYSTEM_PROMPTS_UPDATED: 'system-prompts-updated',
  OPEN_SYSTEM_PROMPT_LIBRARY: 'open-system-prompt-library',
  
  // User prompt events
  USER_PROMPTS_UPDATED: 'user-prompts-updated',
  OPEN_USER_PROMPT_LIBRARY: 'open-user-prompt-library',
  
  // Command events
  
  // Input events
  APPEND_TO_PROMPT: 'append-to-prompt',
  SET_PROMPT: 'set-prompt',
  
  
  // Model events
  SELECT_PRIMARY_MODEL: 'select-primary-model',
  SELECT_SECONDARY_MODEL: 'select-secondary-model',
  
  
  
  // Stream events
  STREAM_IGNORE: 'stream:ignore',
  STREAM_ALLOW: 'stream:allow',
  REQUEST_CANCELLED: 'request:cancelled',
  
  // Scroll events
  SCROLL_TO_BOTTOM: 'scroll:to-bottom',
};

/**
 * Interface for modal opening event payload
 */
export interface OpenModalEventDetail {
  createNew?: boolean;
  editPromptId?: string | null;
  convId?: string; // For context if SystemPromptModal needs it
}

/**
 * Type for the CustomEvent detail
 */
export type ModalCustomEvent = CustomEvent<OpenModalEventDetail | undefined>;

/**
 * Helper functions for working with window events
 */
export const windowEventService = {
  /**
   * Emit a window event
   * @param eventName The name of the event to emit
   * @param data Optional data to include with the event
   */
  emit: (eventName: string, data?: any) => {
    const event = new CustomEvent(eventName, { detail: data });
    window.dispatchEvent(event);
  },
  
  /**
   * Listen to a window event
   * @param eventName The name of the event to listen for
   * @param handler The function to call when the event is emitted
   * @returns A function that can be called to remove the event listener
   */
  on: (eventName: string, handler: (data: any) => void) => {
    const wrappedHandler = (e: Event) => {
      const customEvent = e as CustomEvent;
      handler(customEvent.detail);
    };
    window.addEventListener(eventName, wrappedHandler);
    return () => window.removeEventListener(eventName, wrappedHandler);
  }
};