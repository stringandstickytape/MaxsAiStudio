// src/plugins/modelCommands.ts
import { useCommandStore } from '@/stores/useCommandStore';
import { ModelType } from '@/types/modelTypes';
import { Cpu } from 'lucide-react';
import React from 'react';

interface ModelCommandsConfig {
    getAvailableModels: () => string[];
    selectPrimaryModel: (modelName: string) => void;
    selectSecondaryModel: (modelName: string) => void;
}

export function initializeModelCommands(config: ModelCommandsConfig) {
    const { getAvailableModels, selectPrimaryModel, selectSecondaryModel } = config;
    
    const availableModels = getAvailableModels();

    // Create commands for selecting primary model
    const primaryModelCommands = availableModels.map(modelName => ({
        id: `select-primary-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`,
        name: `${modelName} [Primary]`,
        description: `Set primary model to ${modelName}`,
        keywords: ['model', 'primary', 'set', 'change', ...modelName.toLowerCase().split(' ')],
        section: 'model',
        execute: () => selectPrimaryModel(modelName),
        icon: React.createElement(Cpu, { size: 16, className: 'text-emerald-500' })
    }));

    // Create commands for selecting secondary model
    const secondaryModelCommands = availableModels.map(modelName => ({
        id: `select-secondary-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`,
        name: `${modelName} [Secondary]`,
        description: `Set secondary model to ${modelName}`,
        keywords: ['model', 'secondary', 'set', 'change', ...modelName.toLowerCase().split(' ')],
        section: 'model',
        execute: () => selectSecondaryModel(modelName),
        icon: React.createElement(Cpu, { size: 16, className: 'text-blue-500' })
    }));

    // Register all primary model commands
    const { registerGroup } = useCommandStore.getState();
    
    registerGroup({
        id: 'primary-models',
        name: 'Primary Models',
        priority: 90,
        commands: primaryModelCommands
    });

    // Register all secondary model commands
    registerGroup({
        id: 'secondary-models',
        name: 'Secondary Models',
        priority: 89,
        commands: secondaryModelCommands
    });

    // Register a generic model command category for showing model options
    registerGroup({
        id: 'model-actions',
        name: 'Model Actions',
        priority: 91,
        commands: [
            {
                id: 'select-model',
                name: 'Select Model',
                description: 'Change the active AI model',
                keywords: ['model', 'select', 'change', 'switch', 'ai', 'configure'],
                section: 'model',
                icon: React.createElement(Cpu, { size: 16 }),
                execute: () => {
                    // This is a generic entry point that will show more specific options
                    // It doesn't do anything itself, but will show the model-specific commands
                    // when searching for "model"
                }
            }
        ]
    });
}