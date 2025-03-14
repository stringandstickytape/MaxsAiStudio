// src/types/toolTypes.ts

export interface Tool {
  guid: string;
  name: string;
  description: string;
  schema: string;
  schemaType: 'function' | 'custom' | 'template';
  categories: string[];
  lastModified: string;
  isBuiltIn: boolean;
  filetype: string; // Added filetype property
}

export interface ToolCategory {
  id: string;
  name: string;
  priority: number;
}

export interface ToolSelectionRequest {
  toolIds: string[];
}

export interface ToolResponse {
  success: boolean;
  message?: string;
  tool?: Tool;
  tools?: Tool[];
}

export interface ToolLibrary {
  tools: Tool[];
  categories: ToolCategory[];
}