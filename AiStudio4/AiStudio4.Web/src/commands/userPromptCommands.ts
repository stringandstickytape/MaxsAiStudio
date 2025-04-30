// AiStudio4.Web/src/commands/userPromptCommands.ts
import React from 'react';
import { BookMarked, Pencil, PlusCircle } from 'lucide-react';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { useModalStore } from '@/stores/useModalStore';
import { commandRegistry } from '@/services/commandRegistry';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

interface UserPromptCommandsConfig {
  toggleLibrary: () => void;
  createNewPrompt: () => void;
  editPrompt: (promptId: string) => void;
}

export function initializeUserPromptCommands(config: UserPromptCommandsConfig) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  commandRegistry.registerGroup({
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
        () => useModalStore.getState().openModal('userPrompt'),
      ],
      [
        'create-new-user-prompt',
        'Create New User Prompt',
        'Create a new custom user prompt',
        '',
        ['user', 'prompt', 'create', 'new', 'custom', 'template', 'snippet'],
        React.createElement(PlusCircle, { size: 16 }),
        () => {
          useModalStore.getState().openModal('userPrompt', { createNew: true });
        },
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

          if (promptToEdit) {
            useModalStore.getState().openModal('userPrompt', { editPromptId: promptToEdit.guid });
          } else {
            useModalStore.getState().openModal('userPrompt', { createNew: true });
          }
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
  commandRegistry.unregisterGroup('user-prompts-list');

  const { prompts, setCurrentPrompt } = useUserPromptStore.getState();

  const promptCommands = prompts.map((prompt) => {
    
    const contentPreview = prompt.content.length > 100 
      ? prompt.content.substring(0, 100) + '...' 
      : prompt.content;
    
    
    const displayName = prompt.shortcut 
      ? `${prompt.title} [Prompt Template] [${prompt.shortcut}]` 
      : `${prompt.title} [Prompt Template]`;
    
    return {
      id: `apply-user-prompt-${prompt.guid}`,
      name: displayName,
      description: `${prompt.description || 'No description'} \nContent: ${contentPreview}`,
      keywords: [
        'user', 'prompt', 'apply', 'template', 'snippet', 
        ...(prompt.shortcut ? [prompt.shortcut.toLowerCase()] : []),
        ...prompt.title.toLowerCase().split(' '),
        ...prompt.content.toLowerCase().split(/\s+/).slice(0, 30) 
      ],
      section: 'utility',
      icon: React.createElement(BookMarked, { 
        size: 16,
        className: prompt.isFavorite ? 'text-red-500' : undefined
      }),
      execute: () => {
        setCurrentPrompt(prompt);
        windowEventService.emit(WindowEvents.SET_PROMPT, { text: prompt.content });
      },
    };
  });

  commandRegistry.registerGroup({
    id: 'user-prompts-list',
    name: 'Available User Prompts',
    priority: 96, 
    commands: promptCommands,
  });
}