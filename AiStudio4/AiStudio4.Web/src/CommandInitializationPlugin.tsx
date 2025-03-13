import { useEffect } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { registerUserPromptsAsCommands } from '@/commands/userPromptCommands';

/**
 * Component that ensures prompts are properly registered with the command system
 * when prompts are loaded or changed
 */
export function CommandInitializationPlugin() {
  const { prompts: systemPrompts } = useSystemPromptStore();
  const { prompts: userPrompts } = useUserPromptStore();
  const { togglePanel } = usePanelStore();
  
  // Register system prompts whenever they change
  useEffect(() => {
    if (systemPrompts.length > 0) {
      registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
      console.log(`Re-registered ${systemPrompts.length} system prompts as commands`);
    }
  }, [systemPrompts, togglePanel]);
  
  // Register user prompts whenever they change
  useEffect(() => {
    if (userPrompts.length > 0) {
      registerUserPromptsAsCommands(() => togglePanel('userPrompts'));
      console.log(`Re-registered ${userPrompts.length} user prompts as commands`);
    }
  }, [userPrompts, togglePanel]);
  
  // Add event listeners for explicit prompt registration requests
  useEffect(() => {
    const handleSystemPromptsUpdate = () => {
      registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
      console.log('System prompts registered from event');
    };
    
    const handleUserPromptsUpdate = () => {
      registerUserPromptsAsCommands(() => togglePanel('userPrompts'));
      console.log('User prompts registered from event');
    };
    
    window.addEventListener('system-prompts-updated', handleSystemPromptsUpdate);
    window.addEventListener('user-prompts-updated', handleUserPromptsUpdate);
    
    // Immediate registration attempt
    if (systemPrompts.length > 0) handleSystemPromptsUpdate();
    if (userPrompts.length > 0) handleUserPromptsUpdate();
    
    return () => {
      window.removeEventListener('system-prompts-updated', handleSystemPromptsUpdate);
      window.removeEventListener('user-prompts-updated', handleUserPromptsUpdate);
    };
  }, [systemPrompts.length, userPrompts.length, togglePanel]);
  
  return null; // This is a utility component with no UI
}
