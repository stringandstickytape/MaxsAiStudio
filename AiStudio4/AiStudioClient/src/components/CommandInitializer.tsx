// AiStudio4/AiStudioClient/src/components/CommandInitializer.tsx
import { useEffect } from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { useToolStore } from '@/stores/useToolStore';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { useFileSystemStore } from '@/stores/useFileSystemStore';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { registerMcpServersAsCommands, initializeMcpServerManagementCommand } from '@/commands/mcpServerCommands';

export function CommandInitializer() {
  // Fetch stores and actions
  const { fetchPinnedCommands } = usePinnedCommandsStore();
  const { fetchTools, fetchToolCategories } = useToolStore();
  const { fetchUserPrompts } = useUserPromptStore();
  const { fetchFileSystem } = useFileSystemStore();
  const {
    servers: mcpServers,
    setServerEnabled: toggleMcpServerEnabled,
    fetchServers: fetchMcpServers,
  } = useMcpServerStore();

  // Initial data load
  useEffect(() => {
    const loadInitialData = async () => {
      try {
        await Promise.all([
          fetchPinnedCommands(),
          fetchTools(),
          fetchToolCategories(),
          fetchUserPrompts(),
          fetchFileSystem(),
          fetchMcpServers(),
        ]);
      } catch (error) {
        console.error('Error loading initial data:', error);
      }
    };
    loadInitialData();
  }, [fetchPinnedCommands, fetchTools, fetchToolCategories, fetchUserPrompts, fetchFileSystem, fetchMcpServers]);

  // Register MCP server commands when servers change
  useEffect(() => {
    
    if (mcpServers && mcpServers.length > 0) {
      registerMcpServersAsCommands(mcpServers, toggleMcpServerEnabled);
    } else if (mcpServers && mcpServers.length === 0) {
      try {
        // Unregister group if no servers
        const { commandRegistry } = require('@/services/commandRegistry');
        commandRegistry.unregisterGroup('mcp-servers-list');
      } catch (e) {}
    }
  }, [mcpServers, toggleMcpServerEnabled]);

  // Register static MCP server management command
  useEffect(() => {
    initializeMcpServerManagementCommand();
  }, []);

  return null;
}