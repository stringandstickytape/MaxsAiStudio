

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
  // Extra dynamic properties (string key-value pairs)
  extraProperties: Record<string, string>;
}

export interface ToolCategory {
  id: string;
  name: string;
  priority: number;
}