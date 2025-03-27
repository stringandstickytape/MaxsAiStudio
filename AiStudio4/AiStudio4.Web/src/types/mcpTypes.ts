export interface McpServerDefinition {
  id: string;
  name: string;
  command: string;
  arguments: string;
  isEnabled: boolean;
  description: string;
  lastModified: string;
  env?: Record<string, string>;
}

export interface McpTool {
  name: string;
  description: string;
  parameters: any; // This could be refined based on actual parameter structure
}