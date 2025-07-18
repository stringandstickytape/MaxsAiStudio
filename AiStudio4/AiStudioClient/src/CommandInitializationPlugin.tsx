﻿// AiStudioClient/src/CommandInitializationPlugin.tsx
import { useEffect } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { useToolStore } from '@/stores/useToolStore';
import { SystemPrompt } from '@/types/systemPrompt';
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { commandRegistry } from '@/services/commandRegistry';
import { createApiRequest } from '@/utils/apiUtils';
import { useConvStore } from '@/stores/useConvStore';
import { useInputBarStore } from '@/stores/useInputBarStore';

export function CommandInitializationPlugin() {
  const { prompts: systemPrompts } = useSystemPromptStore();
  const { prompts: userPrompts } = useUserPromptStore();
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
            // Use the input bar store directly to set the prompt content
            useInputBarStore.getState().setInputText(userPrompt.content);
            
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