// src/commands/toolCommands.ts
import React from 'react';
import { commandRegistry } from './commandRegistry';

import { registerCommandGroup } from './commandRegistry';
import { Tool } from '@/types/toolTypes';
import { useToolStore } from '@/stores/useToolStore';

// Define some basic tool-related commands
interface ToolCommandsConfig {
  openToolPanel: () => void;
  createNewTool: () => void;
  importTools: () => void;
  exportTools: () => void;
}

export function initializeToolCommands(config: ToolCommandsConfig) {
  // Register main tool commands group
  registerCommandGroup({
    id: 'tools',
    name: 'Tools',
    priority: 80,
    commands: [
      {
        id: 'create-new-tool',
        name: 'Create New Tool',
        description: 'Open the editor to create a new tool',
        shortcut: 'Ctrl+Shift+T',
        keywords: ['tool', 'new', 'create', 'editor'],
        section: 'utility',
        execute: () => {
          // Implementation: Trigger opening of the tool editor dialog
          config.createNewTool();
        }
      },
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

// Function to register each individual tool as a command
export function registerToolsAsCommands(
  tools: Tool[], 
  activeTools: string[], 
  toggleTool: (toolId: string, activate: boolean) => void
) {
  // First unregister any previous tool-specific commands
  commandRegistry.unregisterCommandGroup('tools-list');
  
  // Create commands for each tool
  const toolCommands = tools.map(tool => ({
    id: `tool-${tool.guid}`,
    name: tool.name,
    description: tool.description,
    keywords: ['tool', ...tool.name.toLowerCase().split(' '), ...tool.description.toLowerCase().split(' ').slice(0, 5)],
    section: 'tools',
    // Use a function that returns the icon element needed by the command registry
    icon: () => {
      // Without using JSX, we'll use a simple text icon instead
      return React.createElement('div', { className: 'text-blue-500 font-bold' }, 'T');
    },
    active: activeTools.includes(tool.guid),
    execute: () => {
      // Toggle the tool's active state
      const isCurrentlyActive = activeTools.includes(tool.guid);
      toggleTool(tool.guid, !isCurrentlyActive);
    }
  }));

  // Register all tool commands as a group
  registerCommandGroup({
    id: 'tools-list',
    name: 'Available Tools',
    priority: 75, // Just below the main tools group
    commands: toolCommands
  });
}