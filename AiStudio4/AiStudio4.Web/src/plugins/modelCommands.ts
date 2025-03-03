// src/plugins/modelCommands.ts
import { registerCommandGroup } from '@/commands/commandRegistry';
import { ModelType } from '@/types/modelTypes';
import { Cpu } from 'lucide-react';
import React from 'react';

interface ModelCommandsConfig {
    onModelSelect: (modelType: ModelType, model: string) => void;
    getAvailableModels: () => string[];
}

export function initializeModelCommands(config: ModelCommandsConfig) {
    const { onModelSelect, getAvailableModels } = config;

    // Create commands for selecting primary model
    const primaryModelCommands = getAvailableModels().map(modelName => ({
        id: `select-primary-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`,
        name: `Primary: ${modelName}`,
        description: `Set primary model to ${modelName}`,
        keywords: ['model', 'primary', 'set', 'change', ...modelName.toLowerCase().split(' ')],
        section: 'model',
        execute: () => onModelSelect('primary', modelName),
        icon: React.createElement(Cpu, { size: 16, className: 'text-emerald-500' })
    }));

    // Create commands for selecting secondary model
    const secondaryModelCommands = getAvailableModels().map(modelName => ({
        id: `select-secondary-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`,
        name: `Secondary: ${modelName}`,
        description: `Set secondary model to ${modelName}`,
        keywords: ['model', 'secondary', 'set', 'change', ...modelName.toLowerCase().split(' ')],
        section: 'model',
        execute: () => onModelSelect('secondary', modelName),
        icon: React.createElement(Cpu, { size: 16, className: 'text-blue-500' })
    }));

    // Register main category commands
    registerCommandGroup({
        id: 'model-selection',
        name: 'Model Selection',
        priority: 95,
        commands: [
            {
                id: 'select-primary-model',
                name: 'Select Primary Model',
                description: 'Choose which model to use as your primary AI',
                keywords: ['model', 'primary', 'set', 'change', 'select'],
                section: 'model',
                icon: React.createElement(Cpu, { size: 16, className: 'text-emerald-500' }),
                execute: () => {
                    // This command just serves as a category placeholder
                    // Individual model commands handle the selection
                }
            },
            {
                id: 'select-secondary-model',
                name: 'Select Secondary Model',
                description: 'Choose which model to use as your secondary AI',
                keywords: ['model', 'secondary', 'set', 'change', 'select'],
                section: 'model',
                icon: React.createElement(Cpu, { size: 16, className: 'text-blue-500' }),
                execute: () => {
                    // This command just serves as a category placeholder
                    // Individual model commands handle the selection
                }
            }
        ]
    });

    // Register all primary model commands
    registerCommandGroup({
        id: 'primary-models',
        name: 'Primary Models',
        priority: 90,
        commands: primaryModelCommands
    });

    // Register all secondary model commands
    registerCommandGroup({
        id: 'secondary-models',
        name: 'Secondary Models',
        priority: 89,
        commands: secondaryModelCommands
    });
}