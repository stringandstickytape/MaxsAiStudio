// src/plugins/modelCommands.ts
import { registerCommandGroup } from '@/commands/commandRegistry';
import { ModelType } from '@/types/modelTypes';
import { Cpu } from 'lucide-react';
import React from 'react';
import { useModelStore } from '@/stores/useModelStore';

interface ModelCommandsConfig {
    getAvailableModels: () => string[];
    selectPrimaryModel: (modelName: string) => void;
    selectSecondaryModel: (modelName: string) => void;
}

export function initializeModelCommands(config: ModelCommandsConfig) {
    const { getAvailableModels, selectPrimaryModel, selectSecondaryModel } = config;

    // Create commands for selecting primary model
    const primaryModelCommands = getAvailableModels().map(modelName => ({
        id: `select-primary-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`,
        name: `${modelName} [Primary]`,
        description: `Set primary model to ${modelName}`,
        keywords: ['model', 'primary', 'set', 'change', ...modelName.toLowerCase().split(' ')],
        section: 'model',
        execute: () => selectPrimaryModel(modelName),
        icon: React.createElement(Cpu, { size: 16, className: 'text-emerald-500' })
    }));

    // Create commands for selecting secondary model
    const secondaryModelCommands = getAvailableModels().map(modelName => ({
        id: `select-secondary-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`,
        name: `${modelName} [Secondary]`,
        description: `Set secondary model to ${modelName}`,
        keywords: ['model', 'secondary', 'set', 'change', ...modelName.toLowerCase().split(' ')],
        section: 'model',
        execute: () => selectSecondaryModel(modelName),
        icon: React.createElement(Cpu, { size: 16, className: 'text-blue-500' })
    }));

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