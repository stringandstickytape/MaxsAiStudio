// src/utils/promptUtils.ts
import { useConversationStore } from '@/stores/useConversationStore';

/**
 * Utility functions for manipulating the chat prompt
 */

/**
 * Exposes a global window function to append text to the current prompt
 * Requires a reference to the setText function from InputBar
 */
export function setupPromptUtils(setTextFn) {
  // Make the function globally accessible
  window.appendToPrompt = (text, options = {}) => {
    const { newLine = true, replaceCurrent = false } = options;
    
    try {
      // Get the current input value first
      let currentInputValue = "";
      
      // If we have direct access to the internal state via closure, use it
      if (window._currentPromptText !== undefined) {
        currentInputValue = window._currentPromptText;
      }
      
      // Format the text with a newline if requested
      const formattedText = newLine && currentInputValue.length > 0 
        ? '\n' + text 
        : text;
        
      // Replace or append
      const newValue = replaceCurrent 
        ? formattedText 
        : currentInputValue + formattedText;
        
      // Update the text
      setTextFn(newValue);
      
      // Store the last value for potential retrieval
      window._currentPromptText = newValue;
      
      console.log(`Prompt ${replaceCurrent ? 'replaced' : 'updated'} successfully.`);
      return true;
    } catch (error) {
      console.error("Failed to append to prompt:", error);
      return false;
    }
  };
  
  // Function to read current prompt text
  window.getPromptText = () => {
    return window._currentPromptText || "";
  };
  
  console.log("Prompt utilities initialized. Use appendToPrompt() to modify the current prompt.");
}

/**
 * Clean up the global functions when component unmounts
 */
export function cleanupPromptUtils() {
  delete window.appendToPrompt;
  delete window.getPromptText;
  delete window._currentPromptText;
}