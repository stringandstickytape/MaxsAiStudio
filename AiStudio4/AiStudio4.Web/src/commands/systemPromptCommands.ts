// src/commands/systemPromptCommands.ts
import React from 'react';
import { registerCommandGroup } from './commandRegistry';
import { MessageSquare, Pencil, PlusCircle } from 'lucide-react';
import { setCurrentPrompt } from '@/store/systemPromptSlice';
import { store } from '@/store/store';
import { useGetSystemPromptsQuery } from '@/services/api/systemPromptApi';

interface SystemPromptCommandsConfig {
    toggleLibrary: () => void;
    createNewPrompt: () => void;
    editPrompt: (promptId: string) => void;
}

export function initializeSystemPromptCommands(config: SystemPromptCommandsConfig) {
    const mac = navigator.platform.indexOf('Mac') !== -1;
    const shortcut = (key: string) => mac ? `⌘+${key}` : `Ctrl+${key}`;

    registerCommandGroup({
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
                    // RTK Query will handle data fetching automatically
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
                    const state = store.getState();
                    const currentConversation = state.conversations.activeConversationId;
                    let promptToEdit = null;

                    if (currentConversation) {
                        // Try to get the conversation-specific prompt
                        const promptId = state.systemPrompts.conversationPrompts[currentConversation];
                        if (promptId) {
                            promptToEdit = state.systemPrompts.prompts.find(p => p.guid === promptId);
                        }
                    }

                    if (!promptToEdit) {
                        // Fallback to default prompt
                        promptToEdit = state.systemPrompts.prompts.find(p => p.guid === state.systemPrompts.defaultPromptId);
                    }

                    if (!promptToEdit && state.systemPrompts.prompts.length > 0) {
                        // Just take the first prompt if nothing else is available
                        promptToEdit = state.systemPrompts.prompts[0];
                    }

                    if (promptToEdit) {
                        config.editPrompt(promptToEdit.guid);
                    } else {
                        // If no prompts exist, create a new one
                        config.createNewPrompt();
                    }
                }
            }
        ]
    });
}

// This function registers each system prompt as a command so users can quickly apply them
export function registerSystemPromptsAsCommands(toggleLibrary: () => void) {
    const systemPrompts = store.getState().systemPrompts.prompts;
    const defaultPromptId = store.getState().systemPrompts.defaultPromptId;

    const promptCommands = systemPrompts.map(prompt => ({
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
            // Set this prompt as the current prompt and open the library
            store.dispatch(setCurrentPrompt(prompt));
            toggleLibrary();
        }
    }));

    if (promptCommands.length > 0) {
        registerCommandGroup({
            id: 'system-prompts-list',
            name: 'Available System Prompts',
            priority: 84,
            commands: promptCommands
        });
    }
}