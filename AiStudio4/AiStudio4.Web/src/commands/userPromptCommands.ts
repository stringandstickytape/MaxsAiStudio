// src/commands/userPromptCommands.ts
import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { BookMarked, Pencil, PlusCircle } from 'lucide-react';
import { useUserPromptStore } from '@/stores/useUserPromptStore';

interface UserPromptCommandsConfig {
  toggleLibrary: () => void;
  createNewPrompt: () => void;
  editPrompt: (promptId: string) => void;
}

export function initializeUserPromptCommands(config: UserPromptCommandsConfig) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  const { registerGroup } = useCommandStore.getState();

  registerGroup({
    id: 'user-prompts',
    name: 'User Prompts',
    priority: 85,
    commands: [
      [
        'toggle-user-prompts-library',
        'Show User Prompts Library',
        'Browse and manage your collection of user prompts',
        shortcut('U'),
        ['user', 'prompt', 'library', 'collection', 'manage', 'browse', 'template', 'snippet'],
        React.createElement(BookMarked, { size: 16 }),
        () => config.toggleLibrary(),
      ],
      [
        'create-new-user-prompt',
        'Create New User Prompt',
        'Create a new custom user prompt',
        '',
        ['user', 'prompt', 'create', 'new', 'custom', 'template', 'snippet'],
        React.createElement(PlusCircle, { size: 16 }),
        () => config.createNewPrompt(),
      ],
      [
        'edit-current-user-prompt',
        'Edit Current User Prompt',
        'Edit the currently active user prompt',
        '',
        ['user', 'prompt', 'edit', 'modify', 'current', 'template', 'snippet'],
        React.createElement(Pencil, { size: 16 }),
        () => {
          const { prompts, currentPrompt } = useUserPromptStore.getState();
          let promptToEdit = currentPrompt;

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

export function registerUserPromptsAsCommands(toggleLibrary: () => void) {
  useCommandStore.getState().unregisterGroup('user-prompts-list');

  const { prompts, setCurrentPrompt } = useUserPromptStore.getState();

  const promptCommands = prompts.map((prompt) => {
    // Extract a preview of the prompt content (first 100 characters)
    const contentPreview = prompt.content.length > 100 
      ? prompt.content.substring(0, 100) + '...' 
      : prompt.content;
    
    // Create a display name that includes the shortcut if available
    const displayName = prompt.shortcut 
      ? `Use Prompt: ${prompt.title} [${prompt.shortcut}]` 
      : `Use Prompt: ${prompt.title}`;
    
    return {
      id: `apply-user-prompt-${prompt.guid}`,
      name: displayName,
      description: `${prompt.description || 'No description'} \n\nContent: ${contentPreview}`,
      keywords: [
        'user', 'prompt', 'apply', 'template', 'snippet', 
        ...(prompt.shortcut ? [prompt.shortcut.toLowerCase()] : []),
        ...prompt.title.toLowerCase().split(' '),
        ...prompt.content.toLowerCase().split(/\s+/).slice(0, 30) // Add content words as keywords
      ],
      section: 'utility',
      icon: React.createElement(BookMarked, { 
        size: 16,
        className: prompt.isFavorite ? 'text-red-500' : undefined
      }),
      execute: () => {
        setCurrentPrompt(prompt);
        // Set the prompt content to the input field
        window.setPrompt(prompt.content);
        // No need to open the library since we're applying directly
      },
    };
  });

  if (promptCommands.length > 0) {
    useCommandStore.getState().registerGroup({
      id: 'user-prompts-list',
      name: 'Available User Prompts',
      priority: 84,
      commands: promptCommands,
    });
  }
}
