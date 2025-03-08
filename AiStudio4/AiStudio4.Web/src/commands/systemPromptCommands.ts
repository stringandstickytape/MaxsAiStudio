// src/commands/systemPromptCommands.ts
import React from 'react';
import { registerCommandGroup } from './commandRegistry';
import { MessageSquare, Pencil, PlusCircle } from 'lucide-react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { commandRegistry } from './commandRegistry';
import { useConversationStore } from '@/stores/useConversationStore';

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
                    // Get current prompt information from Zustand store
                    const { prompts, defaultPromptId, conversationPrompts, currentPrompt } = useSystemPromptStore.getState();
                    
                    const { activeConversationId: currentConversationId } = useConversationStore.getState();
                    
                    let promptToEdit = null;

                    // First try to use the current prompt from the store
                    if (currentPrompt) {
                        promptToEdit = currentPrompt;
                    }
                    // Next try to find a conversation-specific prompt
                    else if (currentConversationId) {
                        const promptId = conversationPrompts[currentConversationId];
                        if (promptId) {
                            promptToEdit = prompts.find(p => p.guid === promptId);
                        }
                    }
                    
                    // Fall back to default prompt
                    if (!promptToEdit && defaultPromptId) {
                        promptToEdit = prompts.find(p => p.guid === defaultPromptId);
                    }
                    
                    // Last resort: just use the first prompt
                    if (!promptToEdit && prompts.length > 0) {
                        promptToEdit = prompts[0];
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
    // Unregister any previous prompt commands to avoid duplicates
    commandRegistry.unregisterCommandGroup('system-prompts-list');
    
    // Get prompts from Zustand store
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
            // Set this prompt as the current prompt and open the library
            setCurrentPrompt(prompt);
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