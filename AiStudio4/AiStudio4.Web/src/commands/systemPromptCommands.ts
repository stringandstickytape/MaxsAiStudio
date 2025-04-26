import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { MessageSquare, Pencil, PlusCircle } from 'lucide-react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useConvStore } from '@/stores/useConvStore';
import { selectSystemPromptStandalone } from '@/hooks/useSystemPromptSelection';

import { useModalStore } from '@/stores/useModalStore';

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
                () => useModalStore.getState().openModal('systemPrompt'),
            ],
            [
                'create-new-system-prompt',
                'Create New System Prompt',
                'Create a new custom system prompt',
                '',
                ['system', 'prompt', 'create', 'new', 'custom'],
                React.createElement(PlusCircle, { size: 16 }),
                () => {
                    window.localStorage.setItem('systemPrompt_action', 'create');
                    window.dispatchEvent(new CustomEvent('open-system-prompt-library'));
                },
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
    
    // Create a function that will select the system prompt using our standalone function
    const selectPromptFromCommand = async (prompt) => {
        const { activeConvId } = useConvStore.getState();
        
        if (activeConvId) {
            await selectSystemPromptStandalone(prompt, { convId: activeConvId });
        }
    };

    const promptCommands = prompts.map((prompt) => {
        
        const contentPreview = prompt.content.length > 100
            ? prompt.content.substring(0, 100) + '...'
            : prompt.content;

        return {
            id: `apply-system-prompt-${prompt.guid}`,
            name: `${prompt.title} [System Prompt]`,
            description: `${prompt.description || 'No description'} \nContent: ${contentPreview}`,
            keywords: [
                'system', 'prompt', 'apply',
                ...prompt.title.toLowerCase().split(' '),
                ...prompt.content.toLowerCase().split(/\s+/).slice(0, 30) 
            ],
            section: 'utility',
            icon: React.createElement(MessageSquare, {
                size: 16,
                className: prompt.guid === defaultPromptId ? 'text-blue-500' : undefined,
            }),
            execute: () => {
                // First select the prompt using our hook to ensure tools and user prompts are loaded
                selectPromptFromCommand(prompt);
                
                // Then open the modal if needed
                setCurrentPrompt(prompt);
                useModalStore.getState().openModal('systemPrompt');
            },
        };
    });

    
    useCommandStore.getState().registerGroup({
        id: 'system-prompts-list',
        name: 'Available System Prompts',
        priority: 95, 
        commands: promptCommands,
    });

    
}