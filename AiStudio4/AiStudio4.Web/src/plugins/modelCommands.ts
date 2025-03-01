// src/plugins/modelCommands.ts
import { registerCommandGroup } from '@/commands/commandRegistry';
import { ChatService } from '@/services/ChatService';
import { Settings, GitBranch } from 'lucide-react'; // Use icons that we know exist
import React from 'react';

export function initializeModelCommands(
    handlers: {
        onModelSelect: (modelType: 'primary' | 'secondary', model: string) => void,
        getAvailableModels: () => string[]
    }
) {
    const models = handlers.getAvailableModels();

    registerCommandGroup({
        id: 'model',
        name: 'AI Models',
        priority: 80,
        commands: [
            // Generate a command for each model for primary selection
            ...models.map(model => ({
                id: `set-primary-model-${model}`,
                name: `Set Primary Model: ${model}`,
                description: `Change primary AI to ${model}`,
                keywords: ['model', 'primary', 'set', 'change', model.toLowerCase()],
                section: 'model',
                // Use createElement instead of JSX
                icon: React.createElement(Settings, { size: 16 }), // Use Settings icon
                execute: () => handlers.onModelSelect('primary', model)
            })),

            // Generate a command for each model for secondary selection
            ...models.map(model => ({
                id: `set-secondary-model-${model}`,
                name: `Set Secondary Model: ${model}`,
                description: `Change secondary AI to ${model}`,
                keywords: ['model', 'secondary', 'set', 'change', model.toLowerCase()],
                section: 'model',
                // Use createElement instead of JSX
                icon: React.createElement(GitBranch, { size: 16 }), // Use GitBranch icon
                execute: () => handlers.onModelSelect('secondary', model)
            }))
        ]
    });
}