// AiStudioClient/src/CommandInitializationPlugin.tsx
import { useEffect, useState } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { useModalStore } from '@/stores/useModalStore';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { registerUserPromptsAsCommands } from '@/commands/userPromptCommands';
import { useCommandStore } from '@/stores/useCommandStore';
import { useToolStore } from '@/stores/useToolStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { commandRegistry } from '@/services/commandRegistry';
import { createApiRequest } from '@/utils/apiUtils';
import { useConvStore } from '@/stores/useConvStore';

export function CommandInitializationPlugin() {
  const { prompts: systemPrompts } = useSystemPromptStore();
  const { prompts: userPrompts } = useUserPromptStore();
  const { togglePanel } = usePanelStore();
  const { openModal } = useModalStore();
  const { setActiveTools } = useToolStore();
  const { activeConvId } = useConvStore();
  
  // Apply default system prompt tools when a new conversation is created
  useEffect(() => {
    if (activeConvId) {
      // Get the default system prompt
      const applyDefaultPromptTools = async () => {
        try {
          const response = await createApiRequest('/api/getDefaultSystemPrompt', 'POST')({});
          if (response.success && response.prompt) {
            const defaultPrompt = response.prompt;
            
            // Apply associated tools if available
            if (defaultPrompt.associatedTools && defaultPrompt.associatedTools.length > 0) {
              setActiveTools(defaultPrompt.associatedTools);
              
            }
          }
        } catch (err) {
          console.error('Failed to apply default prompt tools for new conversation:', err);
        }
      };
      
      applyDefaultPromptTools();
    }
  }, [activeConvId, setActiveTools]);
  
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
    
    const unsubSystemPrompts = windowEventService.on(WindowEvents.SYSTEM_PROMPTS_UPDATED, handleSystemPromptsUpdate);
    const unsubUserPrompts = windowEventService.on(WindowEvents.USER_PROMPTS_UPDATED, handleUserPromptsUpdate);
    
    if (systemPrompts.length > 0) handleSystemPromptsUpdate();
    if (userPrompts.length > 0) handleUserPromptsUpdate();
    
    return () => {
      unsubSystemPrompts();
      unsubUserPrompts();
    };
  }, [systemPrompts.length, userPrompts.length]);
  
  // Handle system prompt selection and apply associated tools and user prompt
  useEffect(() => {
    const handleSystemPromptSelected = (data: { promptId: string }) => {
      const promptId = data.promptId;
      const selectedPrompt = systemPrompts.find(p => p.guid === promptId);
      
      if (selectedPrompt) {
        // Apply associated tools if available
        if (selectedPrompt.associatedTools && selectedPrompt.associatedTools.length > 0) {
          setActiveTools(selectedPrompt.associatedTools);
          
        }
        
        // Apply associated user prompt if available
        if (selectedPrompt.associatedUserPromptId) {
          const userPrompt = userPrompts.find(p => p.guid === selectedPrompt.associatedUserPromptId);
          if (userPrompt && userPrompt.content) {
            // Use the windowEventService to set the prompt content
            windowEventService.emit(WindowEvents.SET_PROMPT, { text: userPrompt.content });
            
          }
        }
      }
    };
    
    // Listen for system prompt selection events using windowEventService
    const unsubscribe = windowEventService.on(WindowEvents.SYSTEM_PROMPT_SELECTED, handleSystemPromptSelected);
    
    return () => {
      unsubscribe();
    };
  }, [systemPrompts, userPrompts, setActiveTools]);
  
  
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const commandsArray = commandRegistry.getAllCommands();

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
          commandRegistry.executeCommand(command.id);
          break;
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);
  
  return null; 
}