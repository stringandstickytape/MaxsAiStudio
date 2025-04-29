// AiStudio4.Web/src/commands/settingsCommands.ts
import { Book, Database, Edit, Server, Settings } from 'lucide-react';
import React from 'react';
import { useModalStore } from '@/stores/useModalStore';
import { commandRegistry } from '@/services/commandRegistry';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

type CommandEvent = 'edit-model' | 'edit-provider' | 'settings-tab';

// Legacy commandEvents API - maintained for backward compatibility
export const commandEvents = {
  emit: (event: CommandEvent, data: any) => {
    const eventName = `command:${event}`;
    windowEventService.emit(eventName, data);
  },
  on: (event: CommandEvent, handler: (data: any) => void) => {
    const eventName = `command:${event}`;
    return windowEventService.on(eventName, handler);
  },
};

interface SettingsCommandsConfig {
  openSettings: () => void;
}

export function initializeSettingsCommands(config: SettingsCommandsConfig) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  commandRegistry.registerGroup({
    id: 'settings',
    name: 'Settings',
    priority: 85,
    commands: [
      [
        'toggle-settings-panel',
        'Settings Panel',
        'Open the settings panel',
        shortcut(','),
        ['settings', 'options', 'preferences', 'configure', 'setup', 'panel'],
        React.createElement(Settings, { size: 16 }),
        'models',
      ],
      [
        'edit-models',
        'Edit Models',
        'Manage AI models',
        shortcut('M'),
        ['models', 'edit', 'manage', 'AI', 'GPT', 'configure', 'model', 'settings'],
        React.createElement(Book, { size: 16 }),
        'models',
      ],
      [
        'edit-providers',
        'Edit Providers',
        'Manage service providers',
        shortcut('P'),
        ['providers', 'edit', 'manage', 'service', 'API', 'configure', 'settings'],
        React.createElement(Server, { size: 16 }),
        'providers',
      ],
    ].map(([id, name, description, shortcut, keywords, icon, tabName]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'settings',
      icon,
      execute: () => {
        windowEventService.emit(WindowEvents.COMMAND_SETTINGS_TAB, tabName);
        // config.openSettings(); // Original call using passed function
        useModalStore.getState().openModal('settings'); // Directly open the modal
      },
    })),
  });
}

export function registerModelCommands(
  models: { guid: string; friendlyName: string; modelName: string }[],
  openSettings: () => void,
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
      windowEventService.emit(WindowEvents.COMMAND_SETTINGS_TAB, 'models');
      windowEventService.emit(WindowEvents.COMMAND_EDIT_MODEL, model.guid);
      // openSettings(); // Original call using passed function
      useModalStore.getState().openModal('settings'); // Directly open the modal
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
  openSettings: () => void,
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
      windowEventService.emit(WindowEvents.COMMAND_SETTINGS_TAB, 'providers');
      windowEventService.emit(WindowEvents.COMMAND_EDIT_PROVIDER, provider.guid);
      // openSettings(); // Original call using passed function
      useModalStore.getState().openModal('settings'); // Directly open the modal
    },
  }));

  commandRegistry.registerGroup({
    id: 'edit-providers-list',
    name: 'Edit Providers',
    priority: 49,
    commands: providerCommands,
  });
}