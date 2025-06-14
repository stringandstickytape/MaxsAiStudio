
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { useInputBarStore } from '@/stores/useInputBarStore';

/**
 * Utility to directly handle prompt shortcuts in the command bar input
 */
export function handlePromptShortcut(inputText: string): boolean {
  if (!inputText.startsWith('/')) return false;
  
  const shortcut = inputText.trim();
  const { prompts } = useUserPromptStore.getState();
  
  
  const matchingPrompt = prompts.find(p => 
    p.shortcut && 
    (p.shortcut === shortcut || `/${p.shortcut}` === shortcut || p.shortcut === shortcut.substring(1))
  );
  
  if (matchingPrompt) {
    
    useInputBarStore.getState().setInputText(matchingPrompt.content);
    return true;
  }
  
  return false;
}
