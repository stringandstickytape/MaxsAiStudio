// src/commands/shortcutPromptExecutor.ts
import { useUserPromptStore } from '@/stores/useUserPromptStore';

/**
 * Utility to directly handle prompt shortcuts in the command bar input
 */
export function handlePromptShortcut(inputText: string): boolean {
  if (!inputText.startsWith('/')) return false;
  
  const shortcut = inputText.trim();
  const { prompts } = useUserPromptStore.getState();
  
  // Find the prompt with the matching shortcut
  const matchingPrompt = prompts.find(p => 
    p.shortcut && 
    (p.shortcut === shortcut || `/${p.shortcut}` === shortcut || p.shortcut === shortcut.substring(1))
  );
  
  if (matchingPrompt) {
    // Apply the prompt
    window.setPrompt(matchingPrompt.content);
    return true;
  }
  
  return false;
}
