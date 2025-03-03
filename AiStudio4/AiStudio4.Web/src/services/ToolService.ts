// src/services/ToolService.ts
import { Tool, ToolCategory, ToolResponse } from '@/types/toolTypes';
import { webSocketService } from './websocket/WebSocketService';

/* 

localStorage.getItem('clientId')

*/

export class ToolService {
  private static async apiRequest(endpoint: string, clientId: string, data: any) {
    const response = await fetch(endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Client-Id': clientId,
      },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  // Removed normalizePropertyNames as JsonConvert handles serialization/deserialization

  // Get all tools
  static async getTools(): Promise<Tool[]> {
    const clientId = localStorage.getItem('clientId');
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/getTools', clientId, {});
    if (!data.success) {
      throw new Error(data.error || 'Failed to fetch tools');
    }

    return data.tools;
  }

  // Get a specific tool by ID
  static async getTool(toolId: string): Promise<Tool> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/getTool', clientId, { toolId });
    if (!data.success) {
      throw new Error(data.error || 'Failed to fetch tool');
    }

    return data.tool;
  }

  // Add a new tool
  static async addTool(tool: Omit<Tool, 'guid'>): Promise<Tool> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    try {
      const data = await this.apiRequest('/api/addTool', clientId, tool);
      if (!data.success) {
        throw new Error(data.error || 'Failed to add tool');
      }
      return data.tool;
    } catch (error) {
      console.error('Error in addTool:', error);
      throw error;
    }
  }

  // Update an existing tool
  static async updateTool(tool: Tool): Promise<Tool> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    try {
      const data = await this.apiRequest('/api/updateTool', clientId, tool);
      if (!data.success) {
        throw new Error(data.error || 'Failed to update tool');
      }
      return data.tool;
    } catch (error) {
      console.error('Error in updateTool:', error);
      throw error;
    }
  }

  // Delete a tool
  static async deleteTool(toolId: string): Promise<boolean> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/deleteTool', clientId, { toolId });
    if (!data.success) {
      throw new Error(data.error || 'Failed to delete tool');
    }

    return true;
  }

  // Get all tool categories
  static async getToolCategories(): Promise<ToolCategory[]> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/getToolCategories', clientId, {});
    if (!data.success) {
      throw new Error(data.error || 'Failed to fetch tool categories');
    }

    return data.categories;
  }

  // Add a new tool category
  static async addToolCategory(category: Omit<ToolCategory, 'id'>): Promise<ToolCategory> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/addToolCategory', clientId, category);
    if (!data.success) {
      throw new Error(data.error || 'Failed to add tool category');
    }

    return data.category;
  }

  // Update an existing tool category
  static async updateToolCategory(category: ToolCategory): Promise<ToolCategory> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/updateToolCategory', clientId, category);
    if (!data.success) {
      throw new Error(data.error || 'Failed to update tool category');
    }

    return data.category;
  }

  // Delete a tool category
  static async deleteToolCategory(categoryId: string): Promise<boolean> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/deleteToolCategory', clientId, { categoryId });
    if (!data.success) {
      throw new Error(data.error || 'Failed to delete tool category');
    }

    return true;
  }

  // Validate a tool schema
  static async validateToolSchema(schema: string): Promise<boolean> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/validateToolSchema', clientId, { schema });
    if (!data.success) {
      throw new Error(data.error || 'Failed to validate tool schema');
    }

    return data.isValid;
  }

  // Import tools from JSON
  static async importTools(json: string): Promise<Tool[]> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/importTools', clientId, { json });
    if (!data.success) {
      throw new Error(data.error || 'Failed to import tools');
    }

    return data.tools;
  }

  // Export tools to JSON
  static async exportTools(toolIds?: string[]): Promise<string> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/exportTools', clientId, { toolIds });
    if (!data.success) {
      throw new Error(data.error || 'Failed to export tools');
    }

    return data.json;
  }
}