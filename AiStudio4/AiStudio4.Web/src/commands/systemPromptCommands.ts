// src/commands/systemPromptCommands.ts
import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { MessageSquare, Pencil, PlusCircle } from 'lucide-react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useConversationStore } from '@/stores/useConversationStore';

interface SystemPromptCommandsConfig {
    toggleLibrary: () => void;
    createNewPrompt: () => void;
    editPrompt: (promptId: string) => void;
}

export function initializeSystemPromptCommands(config: SystemPromptCommandsConfig) {
    const mac = navigator.platform.indexOf('Mac') !== -1;
    const shortcut = (key: string) => mac ? `⌘+${key}` : `Ctrl+${key}`;

    const { registerGroup } = useCommandStore.getState();

    registerGroup({
        id: 'system-prompts',
        name: 'System Prompts',
        priority: 85,
        commands: [
            {
                id: 'toggle-system-prompts-library',
                name: 'Show System Prompts Library',
                description: 'Browse and manage your collection of system prompts',
                shortcut: shortcut('P'),
                keywords: ['system', 'prompt', 'library', 'collection', 'manage', 'browse'],
                section: 'utility',
                icon: React.createElement(MessageSquare, { size: 16 }),
                execute: () => {
                    config.toggleLibrary();
                }
            },
            {
                id: 'create-new-system-prompt',
                name: 'Create New System Prompt',
                description: 'Create a new custom system prompt',
                keywords: ['system', 'prompt', 'create', 'new', 'custom'],
                section: 'utility',
                icon: React.createElement(PlusCircle, { size: 16 }),
                execute: () => {
                    config.createNewPrompt();
                }
            },
            {
                id: 'edit-current-system-prompt',
                name: 'Edit Current System Prompt',
                description: 'Edit the currently active system prompt',
                keywords: ['system', 'prompt', 'edit', 'modify', 'current'],
                section: 'utility',
                icon: React.createElement(Pencil, { size: 16 }),
                execute: () => {
                    const { prompts, defaultPromptId, conversationPrompts, currentPrompt } = useSystemPromptStore.getState();

                    const { activeConversationId: currentConversationId } = useConversationStore.getState();

                    let promptToEdit = null;

                    if (currentPrompt) {
                        promptToEdit = currentPrompt;
                    }
                    else if (currentConversationId) {
                        const promptId = conversationPrompts[currentConversationId];
                        if (promptId) {
                            promptToEdit = prompts.find(p => p.guid === promptId);
                        }
                    }

                    if (!promptToEdit && defaultPromptId) {
                        promptToEdit = prompts.find(p => p.guid === defaultPromptId);
                    }

                    if (!promptToEdit && prompts.length > 0) {
                        promptToEdit = prompts[0];
                    }

                    if (promptToEdit) {
                        config.editPrompt(promptToEdit.guid);
                    } else {
                        config.createNewPrompt();
                    }
                }
            }
        ]
    });
}

export function registerSystemPromptsAsCommands(toggleLibrary: () => void) {
    useCommandStore.getState().unregisterGroup('system-prompts-list');

    const { prompts, defaultPromptId, setCurrentPrompt } = useSystemPromptStore.getState();

    const promptCommands = prompts.map(prompt => ({
        id: `apply-system-prompt-${prompt.guid}`,
        name: `Apply Prompt: ${prompt.title}`,
        description: prompt.description || `Apply the "${prompt.title}" system prompt`,
        keywords: ['system', 'prompt', 'apply', ...prompt.title.toLowerCase().split(' ')],
        section: 'utility',
        icon: React.createElement(MessageSquare, {
            size: 16,
            className: prompt.guid === defaultPromptId ? 'text-blue-500' : undefined
        }),
        execute: () => {
            setCurrentPrompt(prompt);
            toggleLibrary();
        }
    }));

    if (promptCommands.length > 0) {
        useCommandStore.getState().registerGroup({
            id: 'system-prompts-list',
            name: 'Available System Prompts',
            priority: 84,
            commands: promptCommands
        });
    }
}