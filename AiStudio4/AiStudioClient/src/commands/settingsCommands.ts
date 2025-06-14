// AiStudioClient/src/commands/settingsCommands.ts
import { Book, Database, Edit, Server, Settings, Palette } from 'lucide-react';
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import { commandRegistry } from '@/services/commandRegistry';

export function initializeSettingsCommands() {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  commandRegistry.registerGroup({
    id: 'settings',
    name: 'Settings',
    priority: 85,
    commands: [
      [
        'open-models-dialog',
        'Models',
        'Manage AI models',
        shortcut(','),
        ['models', 'settings', 'options', 'preferences', 'configure', 'setup'],
        React.createElement(Book, { size: 16 }),
        'models',
      ],
      [
        'open-providers-dialog',
        'Service Providers',
        'Manage service providers',
        '',
        ['providers', 'settings', 'options', 'preferences', 'configure', 'setup'],
        React.createElement(Server, { size: 16 }),
        'providers',
      ],
      [
        'open-appearance-dialog',
        'Appearance',
        'Customize appearance settings',
        '',
        ['appearance', 'theme', 'settings', 'options', 'preferences', 'configure', 'setup'],
        React.createElement(Palette, { size: 16 }),
        'appearance',
      ],
      // Removed duplicate commands since we now have the main commands above
    ].map(([id, name, description, shortcut, keywords, icon, tabName]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'settings',
      icon,
      execute: () => {
        // Open the specific modal based on the tab name
        if (tabName === 'models') {
          useModalStore.getState().openModal('models', {});
        } else if (tabName === 'providers') {
          useModalStore.getState().openModal('providers', {});
        } else if (tabName === 'appearance') {
          useModalStore.getState().openModal('appearance', {});
        }
      },
    })),
  });
}

export function registerModelCommands(
  models: { guid: string; friendlyName: string; modelName: string }[],
) {
  try {
    commandRegistry.unregisterGroup('edit-models-list');
  } catch (e) {}

  const modelCommands = models.map((model) => ({
    id: `edit-model-${model.guid}`,
    name: `Edit Model: ${model.friendlyName}`,
    description: `Edit settings for ${model.modelName}`,
    keywords: ['edit', 'model', 'settings', ...model.friendlyName.toLowerCase().split(' ')],
    section: 'settings',
    icon: React.createElement(Edit, { size: 16 }),
    execute: () => {
      // Directly open the modal with the correct context
      useModalStore.getState().openModal('models', { editModelId: model.guid });
    },
  }));

  commandRegistry.registerGroup({
    id: 'edit-models-list',
    name: 'Edit Models',
    priority: 50,
    commands: modelCommands,
  });
}

export function registerProviderCommands(
  providers: { guid: string; friendlyName: string; serviceName: string }[],
) {
  try {
    commandRegistry.unregisterGroup('edit-providers-list');
  } catch (e) {}

  const providerCommands = providers.map((provider) => ({
    id: `edit-provider-${provider.guid}`,
    name: `Edit Provider: ${provider.friendlyName}`,
    description: `Edit settings for ${provider.serviceName} provider`,
    keywords: ['edit', 'provider', 'settings', 'API', ...provider.friendlyName.toLowerCase().split(' ')],
    section: 'settings',
    icon: React.createElement(Database, { size: 16 }),
    execute: () => {
      // Directly open the modal with the correct context
      useModalStore.getState().openModal('providers', { editProviderId: provider.guid });
    },
  }));

  commandRegistry.registerGroup({
    id: 'edit-providers-list',
    name: 'Edit Providers',
    priority: 49,
    commands: providerCommands,
  });
}