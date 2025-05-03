// AiStudioClient/src/stores/useMcpServerStore.ts
import { create } from 'zustand';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';

export interface McpServerDefinition {
  id: string;
  name: string;
  description?: string;
  isEnabled: boolean;
  command: string;
  arguments?: string;
  stdIo?: boolean;
  env?: Record<string, string>;
  categories?: string[];
}

export interface McpTool {
  name: string;
  description?: string;
  parameters?: any;
}

interface McpServerStoreState {
  servers: McpServerDefinition[];
  enabledCount: number;
  selectedServer: McpServerDefinition | null;
  serverTools: McpTool[];
  isLoadingTools: boolean;
  fetchServers: () => Promise<void>;
  setServerEnabled: (id: string, enabled: boolean) => Promise<void>;
  addServer: (server: McpServerDefinition) => Promise<McpServerDefinition | null>;
  updateServer: (server: McpServerDefinition) => Promise<McpServerDefinition | null>;
  deleteServer: (id: string) => Promise<boolean>;
  fetchServerTools: (id: string) => Promise<McpTool[]>;
  setSelectedServer: (server: McpServerDefinition | null) => void;
}

export const useMcpServerStore = create<McpServerStoreState>((set, get) => ({
  servers: [],
  enabledCount: 0,
  selectedServer: null,
  serverTools: [],
  isLoadingTools: false,

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

  async addServer(server) {
    try {
      const response = await createApiRequest('/api/mcpServers/add', 'POST')(server);
      if (response?.success) {
        const newServer = response.server as McpServerDefinition;
        const servers = [...get().servers, newServer];
        set({
          servers,
          enabledCount: servers.filter((s) => s.isEnabled).length,
        });
        return newServer;
      }
      return null;
    } catch (err) {
      console.error('Failed to add MCP server', err);
      return null;
    }
  },

  async updateServer(server) {
    try {
      const response = await createApiRequest('/api/mcpServers/update', 'POST')(server);
      if (response?.success) {
        const updatedServer = response.server as McpServerDefinition;
        const servers = get().servers.map((s) => 
          s.id === updatedServer.id ? updatedServer : s
        );
        set({
          servers,
          enabledCount: servers.filter((s) => s.isEnabled).length,
        });
        return updatedServer;
      }
      return null;
    } catch (err) {
      console.error('Failed to update MCP server', err);
      return null;
    }
  },

  async deleteServer(id) {
    try {
      const response = await createApiRequest('/api/mcpServers/delete', 'POST')({
        serverId: id,
      });
      if (response?.success) {
        const servers = get().servers.filter((s) => s.id !== id);
        set({
          servers,
          enabledCount: servers.filter((s) => s.isEnabled).length,
        });
        return true;
      }
      return false;
    } catch (err) {
      console.error('Failed to delete MCP server', err);
      return false;
    }
  },

  async fetchServerTools(id) {
    try {
      set({ isLoadingTools: true });
      const response = await createApiRequest('/api/mcpServers/getTools', 'POST')({
        serverId: id,
      });
      if (response?.success) {
        const tools = response.tools as McpTool[];
        set({
          serverTools: tools,
          isLoadingTools: false,
        });
        return tools;
      }
      set({ isLoadingTools: false });
      return [];
    } catch (err) {
      console.error('Failed to fetch MCP server tools', err);
      set({ isLoadingTools: false });
      return [];
    }
  },

  setSelectedServer(server) {
    set({ selectedServer: server });
  },
}));