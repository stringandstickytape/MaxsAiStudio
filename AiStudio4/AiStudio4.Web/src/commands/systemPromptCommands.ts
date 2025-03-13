// src/commands/systemPromptCommands.ts
import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { MessageSquare, Pencil, PlusCircle } from 'lucide-react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useConvStore } from '@/stores/useConvStore';

interface SystemPromptCommandsConfig {
  toggleLibrary: () => void;
  createNewPrompt: () => void;
  editPrompt: (promptId: string) => void;
}

export function initializeSystemPromptCommands(config: SystemPromptCommandsConfig) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  const { registerGroup } = useCommandStore.getState();

  registerGroup({
    id: 'system-prompts',
    name: 'System Prompts',
    priority: 85,
    commands: [
      [
        'toggle-system-prompts-library',
        'Show System Prompts Library',
        'Browse and manage your collection of system prompts',
        shortcut('P'),
        ['system', 'prompt', 'library', 'collection', 'manage', 'browse'],
        React.createElement(MessageSquare, { size: 16 }),
        () => config.toggleLibrary(),
      ],
      [
        'create-new-system-prompt',
        'Create New System Prompt',
        'Create a new custom system prompt',
        '',
        ['system', 'prompt', 'create', 'new', 'custom'],
        React.createElement(PlusCircle, { size: 16 }),
        () => config.createNewPrompt(),
      ],
      [
        'edit-current-system-prompt',
        'Edit Current System Prompt',
        'Edit the currently active system prompt',
        '',
        ['system', 'prompt', 'edit', 'modify', 'current'],
        React.createElement(Pencil, { size: 16 }),
        () => {
          const { prompts, defaultPromptId, convPrompts, currentPrompt } = useSystemPromptStore.getState();
          const { activeConvId: currentConvId } = useConvStore.getState();
          let promptToEdit = currentPrompt;

          if (!promptToEdit && currentConvId) {
            const promptId = convPrompts[currentConvId];
            if (promptId) promptToEdit = prompts.find((p) => p.guid === promptId);
          }

          if (!promptToEdit && defaultPromptId) promptToEdit = prompts.find((p) => p.guid === defaultPromptId);
          if (!promptToEdit && prompts.length > 0) promptToEdit = prompts[0];

          promptToEdit ? config.editPrompt(promptToEdit.guid) : config.createNewPrompt();
        },
      ],
    ].map(([id, name, description, shortcut, keywords, icon, fn]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'utility',
      icon,
      execute: fn,
    })),
  });
}

export function registerSystemPromptsAsCommands(toggleLibrary: () => void) {
  useCommandStore.getState().unregisterGroup('system-prompts-list');

  const { prompts, defaultPromptId, setCurrentPrompt } = useSystemPromptStore.getState();

  const promptCommands = prompts.map((prompt) => {
    // Extract a preview of the prompt content (first 100 characters)
    const contentPreview = prompt.content.length > 100 
      ? prompt.content.substring(0, 100) + '...' 
      : prompt.content;
      
    return {
      id: `apply-system-prompt-${prompt.guid}`,
      name: `Apply Prompt: ${prompt.title}`,
      description: `${prompt.description || 'No description'} \n\nContent: ${contentPreview}`,
      keywords: [
        'system', 'prompt', 'apply', 
        ...prompt.title.toLowerCase().split(' '),
        ...prompt.content.toLowerCase().split(/\s+/).slice(0, 30) // Add content words as keywords
      ],
      section: 'utility',
      icon: React.createElement(MessageSquare, {
        size: 16,
        className: prompt.guid === defaultPromptId ? 'text-blue-500' : undefined,
      }),
      execute: () => {
        setCurrentPrompt(prompt);
        toggleLibrary();
      },
    };
  });

  if (promptCommands.length > 0) {
    useCommandStore.getState().registerGroup({
      id: 'system-prompts-list',
      name: 'Available System Prompts',
      priority: 84,
      commands: promptCommands,
    });
  }
}
