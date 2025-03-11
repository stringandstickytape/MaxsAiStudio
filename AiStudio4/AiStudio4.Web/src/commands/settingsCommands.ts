// src/commands/settingsCommands.ts
import { useCommandStore } from '@/stores/useCommandStore';
import { Book, Database, Edit, Server, Settings } from 'lucide-react';
import React from 'react';

// Create a global event system for command communication
type CommandEvent = 'edit-model' | 'edit-provider' | 'settings-tab';
export const commandEvents = {
  emit: (event: CommandEvent, data: any) => {
    const customEvent = new CustomEvent(`command:${event}`, { detail: data });
    window.dispatchEvent(customEvent);
    console.log(`Emitted command:${event} with data:`, data);
  },
  on: (event: CommandEvent, handler: (data: any) => void) => {
    const eventName = `command:${event}`;
    window.addEventListener(eventName, (e: any) => handler(e.detail));
    return () => window.removeEventListener(eventName, (e: any) => handler(e.detail));
  },
};

interface SettingsCommandsConfig {
  openSettings: () => void;
}

export function initializeSettingsCommands(config: SettingsCommandsConfig) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  const { registerGroup } = useCommandStore.getState();

  registerGroup({
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
        commandEvents.emit('settings-tab', tabName);
        config.openSettings();
      },
    })),
  });
}

// Register commands for each individual model in the system
export function registerModelCommands(
  models: { guid: string; friendlyName: string; modelName: string }[],
  openSettings: () => void,
) {
  // Unregister previous commands to avoid duplicates
  try {
    // This might fail silently if the group doesn't exist yet, which is fine
    useCommandStore.getState().unregisterGroup('edit-models-list');
  } catch (e) {}

  const modelCommands = models.map((model) => ({
    id: `edit-model-${model.guid}`,
    name: `Edit Model: ${model.friendlyName}`,
    description: `Edit settings for ${model.modelName}`,
    keywords: ['edit', 'model', 'settings', ...model.friendlyName.toLowerCase().split(' ')],
    section: 'settings',
    icon: React.createElement(Edit, { size: 16 }),
    execute: () => {
      // Open the settings panel and emit an event to edit this model
      commandEvents.emit('settings-tab', 'models');
      commandEvents.emit('edit-model', model.guid);
      openSettings();
    },
  }));

  useCommandStore.getState().registerGroup({
    id: 'edit-models-list',
    name: 'Edit Models',
    priority: 50,
    commands: modelCommands,
  });
}

// Register commands for each individual provider in the system
export function registerProviderCommands(
  providers: { guid: string; friendlyName: string; serviceName: string }[],
  openSettings: () => void,
) {
  // Unregister previous commands to avoid duplicates
  try {
    // This might fail silently if the group doesn't exist yet, which is fine
    useCommandStore.getState().unregisterGroup('edit-providers-list');
  } catch (e) {}

  const providerCommands = providers.map((provider) => ({
    id: `edit-provider-${provider.guid}`,
    name: `Edit Provider: ${provider.friendlyName}`,
    description: `Edit settings for ${provider.serviceName} provider`,
    keywords: ['edit', 'provider', 'settings', 'API', ...provider.friendlyName.toLowerCase().split(' ')],
    section: 'settings',
    icon: React.createElement(Database, { size: 16 }),
    execute: () => {
      // Open the settings panel and emit an event to edit this provider
      commandEvents.emit('settings-tab', 'providers');
      commandEvents.emit('edit-provider', provider.guid);
      openSettings();
    },
  }));

  useCommandStore.getState().registerGroup({
    id: 'edit-providers-list',
    name: 'Edit Providers',
    priority: 49,
    commands: providerCommands,
  });
}
