// src/commands/toolCommands.ts
import React from 'react';
import { commandRegistry } from './commandRegistry';

import { registerCommandGroup } from './commandRegistry';
import { ToolSelector } from '@/components/tools/ToolSelector';

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
        id: 'toggle-tool-panel',
        name: 'Toggle Tool Panel',
        description: 'Show or hide the tool management panel',
        shortcut: 'Ctrl+T',
        keywords: ['tools', 'panel', 'manage', 'show', 'hide'],
        execute: () => {
          // Implementation: Use the passed in config to toggle the panel
          config.openToolPanel();
        }
      },
      {
        id: 'create-new-tool',
        name: 'Create New Tool',
        description: 'Open the editor to create a new tool',
        shortcut: 'Ctrl+Shift+T',
        keywords: ['tool', 'new', 'create', 'editor'],
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
        execute: () => {
          config.exportTools();
        }
      }
    ]
  });
}

// Function to register each individual tool as a command
export function registerToolsAsCommands(tools: any[], activeTools: string[], toggleTool: (toolId: string, activate: boolean) => void) {
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