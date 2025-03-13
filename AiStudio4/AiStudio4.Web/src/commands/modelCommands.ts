// src/commands/modelCommands.ts
import { useCommandStore } from '@/stores/useCommandStore';
import { Cpu } from 'lucide-react';
import React from 'react';
import { Model } from '@/types/settings';

interface ModelCommandsConfig {
  getAvailableModels: () => Model[];
  selectPrimaryModel: (modelName: string) => void;
  selectSecondaryModel: (modelName: string) => void;
}

export function initializeModelCommands(config: ModelCommandsConfig) {
  const { getAvailableModels, selectPrimaryModel, selectSecondaryModel } = config;

  const availableModels = getAvailableModels();

  const primaryModelCommands = availableModels.map((model) => ({
    id: `select-primary-model-${model.modelName.toLowerCase().replace(/\s+/g, '-')}`,
    name: `${model.friendlyName} [Primary]`,
    description: `Set primary model to ${model.modelName} !`,
    keywords: ['model', 'primary', 'set', 'change', ...model.friendlyName.toLowerCase().split(' ')],
    section: 'model',
    execute: () => selectPrimaryModel(model.modelName),
    icon: React.createElement(Cpu, { size: 16, className: 'text-emerald-500' }),
  }));

  const secondaryModelCommands = availableModels.map((model) => ({
    id: `select-secondary-model-${model.modelName.toLowerCase().replace(/\s+/g, '-')}`,
    name: `${model.friendlyName} [Secondary]`,
      description: `Set secondary model to ${model.modelName} !`,
    keywords: ['model', 'secondary', 'set', 'change', ...model.friendlyName.toLowerCase().split(' ')],
    section: 'model',
    execute: () => selectSecondaryModel(model.modelName),
    icon: React.createElement(Cpu, { size: 16, className: 'text-blue-500' }),
  }));

  const { registerGroup } = useCommandStore.getState();

  registerGroup({
    id: 'primary-models',
    name: 'Primary Models',
    priority: 90,
    commands: primaryModelCommands,
  });

  registerGroup({
    id: 'secondary-models',
    name: 'Secondary Models',
    priority: 89,
    commands: secondaryModelCommands,
  });

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
        execute: () => {},
      },
    ],
  });
}