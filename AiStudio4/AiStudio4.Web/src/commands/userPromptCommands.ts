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

  const promptCommands = prompts.map((prompt) => ({
    id: `apply-user-prompt-${prompt.guid}`,
    name: `Use Prompt: ${prompt.title}`,
    description: prompt.description || `Use the "${prompt.title}" prompt template`,
    keywords: ['user', 'prompt', 'apply', 'template', 'snippet', ...prompt.title.toLowerCase().split(' ')],
    section: 'utility',
    icon: React.createElement(BookMarked, { size: 16 }),
    execute: () => {
      setCurrentPrompt(prompt);
      // Set the prompt content to the input field
      window.setPrompt(prompt.content);
      toggleLibrary();
    },
  }));

  if (promptCommands.length > 0) {
    useCommandStore.getState().registerGroup({
      id: 'user-prompts-list',
      name: 'Available User Prompts',
      priority: 84,
      commands: promptCommands,
    });
  }
}
