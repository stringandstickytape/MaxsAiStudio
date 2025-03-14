import { useEffect } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { registerUserPromptsAsCommands } from '@/commands/userPromptCommands';

export function CommandInitializationPlugin() {
  const { prompts: systemPrompts } = useSystemPromptStore();
  const { prompts: userPrompts } = useUserPromptStore();
  const { togglePanel } = usePanelStore();
  
  
  useEffect(() => {
    if (systemPrompts.length > 0) {
      registerSystemPromptsAsCommands(() => window.dispatchEvent(new CustomEvent('open-system-prompt-library')));
      console.log(`Re-registered ${systemPrompts.length} system prompts as commands`);
    }
  }, [systemPrompts]);
  
  
  useEffect(() => {
    if (userPrompts.length > 0) {
      registerUserPromptsAsCommands(() => window.dispatchEvent(new CustomEvent('open-user-prompt-library')));
      console.log(`Re-registered ${userPrompts.length} user prompts as commands`);
    }
  }, [userPrompts]);
  
  
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
    
    
    if (systemPrompts.length > 0) handleSystemPromptsUpdate();
    if (userPrompts.length > 0) handleUserPromptsUpdate();
    
    return () => {
      window.removeEventListener('system-prompts-updated', handleSystemPromptsUpdate);
      window.removeEventListener('user-prompts-updated', handleUserPromptsUpdate);
    };
  }, [systemPrompts.length, userPrompts.length]);
  
  return null; 
}


