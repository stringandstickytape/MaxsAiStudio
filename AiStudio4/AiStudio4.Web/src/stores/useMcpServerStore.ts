import { create } from 'zustand';
import { McpServerDefinition } from '../types/mcpTypes';
import { 
  getAllMcpServers, 
  addMcpServer, 
  updateMcpServer, 
  deleteMcpServer,
  getMcpServerTools
} from '../services/api/apiClient';

interface McpServerState {
  servers: McpServerDefinition[];
  loading: boolean;
  error: string | null;
  fetchServers: () => Promise<void>;
  addServer: (server: Omit<McpServerDefinition, 'id' | 'lastModified'>) => Promise<McpServerDefinition>;
  updateServer: (server: McpServerDefinition) => Promise<McpServerDefinition>;
  deleteServer: (id: string) => Promise<boolean>;
}

export const useMcpServerStore = create<McpServerState>((set) => ({
  servers: [],
  loading: false,
  error: null,
  
  fetchServers: async () => {
    set({ loading: true, error: null });
    try {
      const servers = await getAllMcpServers();
      set({ servers, loading: false });
      return servers;
    } catch (error: any) {
      set({ 
        error: error.message || 'Failed to fetch MCP servers', 
        loading: false 
      });
      throw error;
    }
  },
  
  addServer: async (serverData) => {
    set({ loading: true, error: null });
    try {
      const newServer = await addMcpServer(serverData);
      set((state) => ({ 
        servers: [...state.servers, newServer],
        loading: false 
      }));
      return newServer;
    } catch (error: any) {
      set({ 
        error: error.message || 'Failed to add MCP server', 
        loading: false 
      });
      throw error;
    }
  },
  
  updateServer: async (server) => {
    set({ loading: true, error: null });
    try {
      const updatedServer = await updateMcpServer(server);
      set((state) => ({
        servers: state.servers.map(s => s.id === updatedServer.id ? updatedServer : s),
        loading: false
      }));
      return updatedServer;
    } catch (error: any) {
      set({ 
        error: error.message || 'Failed to update MCP server', 
        loading: false 
      });
      throw error;
    }
  },
  
  deleteServer: async (id) => {
    set({ loading: true, error: null });
    try {
      const success = await deleteMcpServer(id);
      if (success) {
        set((state) => ({
          servers: state.servers.filter(s => s.id !== id),
          loading: false
        }));
      }
      return success;
    } catch (error: any) {
      set({ 
        error: error.message || 'Failed to delete MCP server', 
        loading: false 
      });
      throw error;
    }
  },
}));