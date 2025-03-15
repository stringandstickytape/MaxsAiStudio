import { useEffect } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { registerUserPromptsAsCommands } from '@/commands/userPromptCommands';
import { useCommandStore } from '@/stores/useCommandStore';

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
  
  // Add global keyboard shortcut handler
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const { commands } = useCommandStore.getState();
      const commandsArray = Array.from(commands.values());

      // Skip if focused on input/textarea or using meta keys for browser functions
      if (
        ['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement)?.tagName) ||
        (e.ctrlKey && ['c', 'v', 'x', 'a', 'z', 'f'].includes(e.key.toLowerCase()))
      ) {
        return;
      }

      // Parse shortcut keys from event
      const shortcutKey = [];
      if (e.ctrlKey) shortcutKey.push('Ctrl');
      if (e.altKey) shortcutKey.push('Alt');
      if (e.shiftKey) shortcutKey.push('Shift');
      if (e.metaKey) shortcutKey.push('⌘');

      // Add the key itself (normalize special keys)
      if (e.key === ' ') {
        shortcutKey.push('Space');
      } else if (e.key.length === 1) {
        shortcutKey.push(e.key.toUpperCase());
      } else {
        // For special keys like 'ArrowUp', 'Enter', etc.
        shortcutKey.push(e.key);
      }

      const shortcut = shortcutKey.join('+');

      // Check if any command matches this shortcut
      for (const command of commandsArray) {
        if (!command.shortcut) continue;

        // Normalize command shortcut format
        const normalizedCommandShortcut = command.shortcut
          .replace('⌘', 'Meta')
          .replace('?', 'Alt'); // macOS notation

        // Try different formats for comparison
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


