
import axios from 'axios';
import { create } from 'zustand';
import { webSocketService } from '../websocket/WebSocketService';
import { McpServerDefinition, McpTool } from '@/types/mcpTypes';


export const apiClient = axios.create({
  baseURL: '/',
  headers: {
    'Content-Type': 'application/json',
  },
});


apiClient.interceptors.request.use((config) => {
    const clientId = webSocketService.getClientId();
    if (clientId) {
        config.headers['X-Client-Id'] = clientId;
    }
  return config;
});


apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    
    const errorResponse = {
      message: error.message || 'An unknown error occurred',
      status: error.response?.status,
      data: error.response?.data,
    };

    
    console.error('API Error:', errorResponse);

    
    return Promise.reject(errorResponse);
  },
);



export async function updateMessage(params: { convId: string; messageId: string; content: string }) {
  try {
    const response = await apiClient.post('/api/updateMessage', params);
    return response.data;
  } catch (error) {
    console.error('Error updating message:', error);
    throw error;
  }
}

export async function cancelRequest(params: { convId: string; messageId: string }) {
  try {
    const response = await apiClient.post('/api/cancelRequest', params);
    return response.data;
  } catch (error) {
    console.error('Error cancelling request:', error);
    throw error;
  }
}

// MCP Server API Functions
export async function getAllMcpServers(): Promise<McpServerDefinition[]> {
  try {
    const response = await apiClient.post('/api/mcpServers/getAll', {});
    return response.data.servers;
  } catch (error) {
    console.error('Error fetching MCP servers:', error);
    throw error;
  }
}

export async function getMcpServerById(id: string): Promise<McpServerDefinition> {
  try {
    const response = await apiClient.post('/api/mcpServers/getById', { serverId: id });
    return response.data.server;
  } catch (error) {
    console.error(`Error fetching MCP server ${id}:`, error);
    throw error;
  }
}

export async function addMcpServer(serverDef: Omit<McpServerDefinition, 'id' | 'lastModified'>): Promise<McpServerDefinition> {
  try {
    const response = await apiClient.post('/api/mcpServers/add', serverDef);
    return response.data.server;
  } catch (error) {
    console.error('Error adding MCP server:', error);
    throw error;
  }
}

export async function updateMcpServer(serverDef: McpServerDefinition): Promise<McpServerDefinition> {
  try {
    const response = await apiClient.post('/api/mcpServers/update', serverDef);
    return response.data.server;
  } catch (error) {
    console.error(`Error updating MCP server ${serverDef.id}:`, error);
    throw error;
  }
}

export async function deleteMcpServer(id: string): Promise<boolean> {
  try {
    const response = await apiClient.post('/api/mcpServers/delete', { serverId: id });
    return response.data.success;
  } catch (error) {
    console.error(`Error deleting MCP server ${id}:`, error);
    throw error;
  }
}

export async function getMcpServerTools(id: string): Promise<McpTool[]> {
  try {
    const response = await apiClient.post('/api/mcpServers/getTools', { serverId: id });
    return response.data.tools;
  } catch (error) {
    console.error(`Error fetching tools for MCP server ${id}:`, error);
    throw error;
  }
}
