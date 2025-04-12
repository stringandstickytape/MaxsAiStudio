import { useEffect, useState } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { useModalStore } from '@/stores/useModalStore';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { registerUserPromptsAsCommands } from '@/commands/userPromptCommands';
import { useCommandStore } from '@/stores/useCommandStore';

export function CommandInitializationPlugin() {
  const { prompts: systemPrompts } = useSystemPromptStore();
  const { prompts: userPrompts } = useUserPromptStore();
  const { togglePanel } = usePanelStore();
  const { openModal } = useModalStore();
  
  useEffect(() => {
    if (systemPrompts.length > 0) {
      registerSystemPromptsAsCommands(() => useModalStore.getState().openModal('systemPrompt'));
    }
  }, [systemPrompts]);
  
  
  useEffect(() => {
    if (userPrompts.length > 0) {
      registerUserPromptsAsCommands(() => useModalStore.getState().openModal('userPrompt'));
    }
  }, [userPrompts]);
  
  
  useEffect(() => {
    const handleSystemPromptsUpdate = () => {
      registerSystemPromptsAsCommands(() => useModalStore.getState().openModal('systemPrompt'));
    };
    
    const handleUserPromptsUpdate = () => {
      registerUserPromptsAsCommands(() => useModalStore.getState().openModal('userPrompt'));
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
  
  
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const { commands } = useCommandStore.getState();
      const commandsArray = Array.from(commands.values());

      
      if (
        ['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement)?.tagName) ||
        (e.ctrlKey && ['c', 'v', 'x', 'a', 'z', 'f'].includes(e.key.toLowerCase()))
      ) {
        return;
      }

      
      const shortcutKey = [];
      if (e.ctrlKey) shortcutKey.push('Ctrl');
      if (e.altKey) shortcutKey.push('Alt');
      if (e.shiftKey) shortcutKey.push('Shift');
      if (e.metaKey) shortcutKey.push('⌘');

      
      if (e.key === ' ') {
        shortcutKey.push('Space');
      } else if (e.key.length === 1) {
        shortcutKey.push(e.key.toUpperCase());
      } else {
        
        shortcutKey.push(e.key);
      }

      const shortcut = shortcutKey.join('+');

      
      for (const command of commandsArray) {
        if (!command.shortcut) continue;

        
        const normalizedCommandShortcut = command.shortcut
          .replace('⌘', 'Meta')
          .replace('?', 'Alt'); 

        
        const shortcutVariations = [
          shortcut,
          shortcutKey.join('+')
        ];

        if (shortcutVariations.some(s => 
          s.toLowerCase() === normalizedCommandShortcut.toLowerCase() ||
          s.toLowerCase() === command.shortcut.toLowerCase()
        )) {
          e.preventDefault();
          useCommandStore.getState().executeCommand(command.id);
          break;
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);
  
  return null; 
}