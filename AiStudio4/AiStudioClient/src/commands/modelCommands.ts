
import { useCommandStore } from '@/stores/useCommandStore';
import { Cpu } from 'lucide-react';
import React from 'react';
import { Model } from '@/types/settings';

interface ModelCommandsConfig {
  getAvailableModels: () => Model[];
  selectPrimaryModel: (guid: string) => void;
  selectSecondaryModel: (guid: string) => void;
  handleModelSelect?: (modelType: 'primary' | 'secondary', modelGuid: string, isGuid?: boolean) => Promise<boolean>;
}

export function initializeModelCommands(config: ModelCommandsConfig) {
  const { getAvailableModels, selectPrimaryModel, selectSecondaryModel, handleModelSelect } = config;

  const availableModels = getAvailableModels();

  const primaryModelCommands = availableModels.map((model) => ({
    id: `select-primary-model-${model.guid.toLowerCase().replace(/\s+/g, '-')}`,
    name: `${model.friendlyName} [Primary]`,
    description: `Set primary model to ${model.modelName}`,
    keywords: ['model', 'primary', 'set', 'change', ...model.friendlyName.toLowerCase().split(' ')],
    section: 'model',
    execute: async () => {
      // Use handleModelSelect if provided (which makes the API call), otherwise fall back to local update
      if (handleModelSelect) {
        await handleModelSelect('primary', model.guid, true);
      } else {
        selectPrimaryModel(model.guid);
      }
    },
    icon: React.createElement(Cpu, { size: 16, className: 'text-emerald-500' }),
  }));

  const secondaryModelCommands = availableModels.map((model) => ({
    id: `select-secondary-model-${model.guid.toLowerCase().replace(/\s+/g, '-')}`,
    name: `${model.friendlyName} [Secondary]`,
    description: `Set secondary model to ${model.modelName}`,
    keywords: ['model', 'secondary', 'set', 'change', ...model.friendlyName.toLowerCase().split(' ')],
    section: 'model',
    execute: async () => {
      // Use handleModelSelect if provided (which makes the API call), otherwise fall back to local update
      if (handleModelSelect) {
        await handleModelSelect('secondary', model.guid, true);
      } else {
        selectSecondaryModel(model.guid);
      }
    },
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