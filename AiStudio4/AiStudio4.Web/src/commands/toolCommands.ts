// AiStudio4.Web/src/commands/toolCommands.ts
import React from 'react';
import { Tool } from '@/types/toolTypes';
import { commandRegistry } from '@/services/commandRegistry';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

interface ToolCommandsConfig {
  openToolLibrary: () => void;
  createNewTool: () => void;
  importTools: () => void;
  exportTools: () => void;
}

export function initializeToolCommands(config: ToolCommandsConfig) {
  commandRegistry.registerGroup({
    id: 'tools',
    name: 'Tools',
    priority: 80,
    commands: [
      [
        'export-tools',
        'Export Tools',
        'Export current tools to a file',
        'Ctrl+E',
        ['export', 'tools', 'json'],
        config.exportTools,
      ],
      [
        'manage-tools',
        'Manage Tools',
        'Open the tool library',
        '',
        ['manage', 'tools', 'panel', 'settings', 'configure'],
        config.openToolLibrary,
      ],
    ].map(([id, name, description, shortcut, keywords, fn]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'utility',
      execute: () => fn(),
    })),
  });
}

export function registerToolsAsCommands(
  tools: Tool[],
  activeTools: string[],
  toggleTool: (toolId: string, activate: boolean) => void,
) {
  commandRegistry.unregisterGroup('tools-list');

  const toolCommands = tools.map((tool) => ({
    id: `tool-${tool.guid}`,
    name: tool.name,
    description: tool.description,
    keywords: ['tool', ...tool.name.toLowerCase().split(' '), ...tool.description.toLowerCase().split(' ').slice(0, 5)],
    section: 'tools',
    icon: () => {
      return React.createElement('div', { className: 'text-blue-500 font-bold' }, 'T');
    },
    active: activeTools.includes(tool.guid),
    execute: () => {
      const isCurrentlyActive = activeTools.includes(tool.guid);
      toggleTool(tool.guid, !isCurrentlyActive);
    },
  }));

  commandRegistry.registerGroup({
    id: 'tools-list',
    name: 'Available Tools',
    priority: 75, 
    commands: toolCommands,
  });
}