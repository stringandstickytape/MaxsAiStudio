// src/commands/toolCommands.ts
import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { Tool } from '@/types/toolTypes';

// Define some basic tool-related commands
interface ToolCommandsConfig {
  openToolPanel: () => void;
  createNewTool: () => void;
  importTools: () => void;
  exportTools: () => void;
}

export function initializeToolCommands(config: ToolCommandsConfig) {
  const { registerGroup } = useCommandStore.getState();
  
  registerGroup({
    id: 'tools',
    name: 'Tools',
    priority: 80,
    commands: [
      {
        id: 'import-tools',
        name: 'Import Tools',
        description: 'Import tools from a file or URL', 
        shortcut: 'Ctrl+I',
        keywords: ['import', 'tools', 'json'],
        section: 'utility',
        execute: () => {
          config.importTools();
        }
      },
      {
        id: 'export-tools',
        name: 'Export Tools',
        description: 'Export current tools to a file',
        shortcut: 'Ctrl+E',
        keywords: ['export', 'tools', 'json'],
        section: 'utility',
        execute: () => {
          config.exportTools();
        }
      },
      {
        id: 'manage-tools',
        name: 'Manage Tools',
        description: 'Open the tool management panel', 
        shortcut: '',
        keywords: ['manage', 'tools', 'panel', 'settings', 'configure'],
        section: 'utility',
        execute: () => {
          config.openToolPanel();
        }
      }
    ]
  });
}
export function registerToolsAsCommands(
  tools: Tool[], 
  activeTools: string[], 
  toggleTool: (toolId: string, activate: boolean) => void
) {
  useCommandStore.getState().unregisterGroup('tools-list');
  
  const toolCommands = tools.map(tool => ({
    id: `tool-${tool.guid}`,
    name: tool.name,
    description: tool.description,
    keywords: ['tool', ...tool.name.toLowerCase().split(' '), ...tool.description.toLowerCase().split(' ').slice(0, 5)],
    section: 'tools',
    icon: () => {
      // Without using JSX, we'll use a simple text icon instead
      return React.createElement('div', { className: 'text-blue-500 font-bold' }, 'T');
    },
    active: activeTools.includes(tool.guid),
    execute: () => {
      const isCurrentlyActive = activeTools.includes(tool.guid);
      toggleTool(tool.guid, !isCurrentlyActive);
    }
  }));

  useCommandStore.getState().registerGroup({
    id: 'tools-list',
    name: 'Available Tools',
    priority: 75, // Just below the main tools group
    commands: toolCommands
  });
}