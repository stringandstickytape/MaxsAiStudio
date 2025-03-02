// src/commands/toolCommands.ts
import React from 'react';

import { registerCommand, registerCommandGroup } from './commandRegistry';
import { ToolSelector } from '@/components/tools/ToolSelector';

// Define some basic tool-related commands
interface ToolCommandsConfig {
  openToolPanel: () => void;
  createNewTool: () => void;
  importTools: () => void;
  exportTools: () => void;
}

export function initializeToolCommands(config: ToolCommandsConfig) {

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