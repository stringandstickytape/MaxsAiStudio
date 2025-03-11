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
  filetype: string; 
}

export interface ToolCategory {
  id: string;
  name: string;
  priority: number;
}

