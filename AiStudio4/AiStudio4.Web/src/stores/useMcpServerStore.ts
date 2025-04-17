// AiStudio4.Web/src/stores/useMcpServerStore.ts
import { create } from 'zustand';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';

export interface McpServerDefinition {
  id: string;
  name: string;
  description?: string;
  isEnabled: boolean;
}

interface McpServerStoreState {
  servers: McpServerDefinition[];
  enabledCount: number;
  fetchServers: () => Promise<void>;
  setServerEnabled: (id: string, enabled: boolean) => Promise<void>;
}

export const useMcpServerStore = create<McpServerStoreState>((set, get) => ({
  servers: [],
  enabledCount: 0,

  async fetchServers() {
    try {
      const response = await createApiRequest('/api/mcpServers/getAll', 'POST')({});
      if (response?.success) {
        const servers: McpServerDefinition[] = response.servers ?? [];
        set({
          servers,
          enabledCount: servers.filter((s) => s.isEnabled).length,
        });
      }
    } catch (err) {
      console.error('Failed to fetch MCP servers', err);
    }
  },

  async setServerEnabled(id, enabled) {
    try {
      const response = await createApiRequest('/api/mcpServers/setEnabled', 'POST')({
        serverId: id,
        isEnabled: enabled,
      });
      if (response?.success) {
        // Update local store list with new definition
        const updated = response.server as McpServerDefinition;
        const servers = get().servers.map((s) => (s.id === updated.id ? updated : s));
        set({
          servers,
          enabledCount: servers.filter((s) => s.isEnabled).length,
        });
      }
    } catch (err) {
      console.error('Failed to toggle MCP server', err);
    }
  },
}));