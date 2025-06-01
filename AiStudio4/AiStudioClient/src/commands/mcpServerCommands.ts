// AiStudio4/AiStudioClient/src/commands/mcpServerCommands.ts
import React from 'react';
import { Server } from 'lucide-react';
import { McpServerDefinition } from '@/stores/useMcpServerStore';
import { commandRegistry } from '@/services/commandRegistry';
import { Command } from './types';
import { useModalStore } from '@/stores/useModalStore';

/**
 * Registers individual MCP servers as toggleable commands.
 */
export function registerMcpServersAsCommands(
  servers: McpServerDefinition[],
  toggleServerEnabledCallback: (serverId: string, enable: boolean) => Promise<void>
) {
  try {
    commandRegistry.unregisterGroup('mcp-servers-list');
  } catch (e) {
    // Group might not exist on first run, this is fine.
  }

  const mcpServerCommands: Command[] = servers.map((server) => ({
    id: `mcp-server-toggle-${server.id}`,
    name: `${server.name} [MCP Server]`,
    description: server.description || `Enable/Disable the '${server.name}' MCP server.`,
    keywords: ['mcp', 'server', 'model context protocol', 'toggle', ...server.name.toLowerCase().split(' ')],
    section: 'mcpServers',
    icon: React.createElement(Server, { size: 16 }),
    active: server.isEnabled,
    execute: async () => {
      await toggleServerEnabledCallback(server.id, !server.isEnabled);
      // The store update should trigger a re-render of CommandInitializer,
      // which will re-register commands with the new 'active' state.
    },
  }));

  commandRegistry.registerGroup({
    id: 'mcp-servers-list',
    name: 'Available MCP Servers',
    priority: 70, // Adjust priority as needed (e.g., slightly lower than tools)
    commands: mcpServerCommands,
  });
}

/**
 * Initializes the command to open the MCP Server management UI.
 */
export function initializeMcpServerManagementCommand() {
  commandRegistry.registerCommand({
    id: 'manage-mcp-servers',
    name: 'Manage MCP Servers',
    description: 'Open the MCP Server management panel/modal',
    keywords: ['mcp', 'server', 'manage', 'settings', 'configure', 'list'],
    section: 'settings', // Or 'mcpServers' if you prefer to group it there
    icon: React.createElement(Server, { size: 16, className: 'text-blue-500' }),
    execute: () => {
      useModalStore.getState().openModal('server', {});
    },
  });
}