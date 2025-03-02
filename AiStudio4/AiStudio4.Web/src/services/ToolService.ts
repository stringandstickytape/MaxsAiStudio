// src/services/ToolService.ts
import { Tool, ToolCategory, ToolResponse } from '@/types/toolTypes';
import { webSocketService } from './websocket/WebSocketService';

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

  // Helper function to normalize property casing from API response
  private static normalizePropertyNames<T>(obj: any): T {
    if (!obj) return obj;
    
    if (Array.isArray(obj)) {
      return obj.map(item => this.normalizePropertyNames<any>(item)) as unknown as T;
    }
    
    if (typeof obj === 'object') {
      const normalized: any = {};
      Object.keys(obj).forEach(key => {
        // Convert the first character to lowercase
        const normalizedKey = key.charAt(0).toLowerCase() + key.slice(1);
        normalized[normalizedKey] = this.normalizePropertyNames(obj[key]);
      });
      return normalized as T;
    }
    
    return obj as T;
  }

  // Get all tools
  static async getTools(): Promise<Tool[]> {
    const clientId = webSocketService.getClientId();
    if (!clientId) {
      throw new Error('Client ID not found');
    }

    const data = await this.apiRequest('/api/getTools', clientId, {});
    if (!data.success) {
      throw new Error(data.error || 'Failed to fetch tools');
    }

    // Normalize property names in the response
    return this.normalizePropertyNames<Tool[]>(data.tools);
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

    return this.normalizePropertyNames<Tool>(data.tool);
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
      return this.normalizePropertyNames<Tool>(data.tool);
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
      return this.normalizePropertyNames<Tool>(data.tool);
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

    return this.normalizePropertyNames<ToolCategory[]>(data.categories);
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

    return this.normalizePropertyNames<ToolCategory>(data.category);
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

    return this.normalizePropertyNames<ToolCategory>(data.category);
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

    return this.normalizePropertyNames<Tool[]>(data.tools);
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