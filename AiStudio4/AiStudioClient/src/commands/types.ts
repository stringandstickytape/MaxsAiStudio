// AiStudio4/AiStudioClient/src/commands/types.ts

// Add 'mcpServers' to the CommandSection type
export type CommandSection = 
  | 'conv'
  | 'model'
  | 'view'
  | 'settings'
  | 'utility'
  | 'appearance'
  | 'tools' // Assuming 'tools' section already exists for tool commands
  | 'mcpServers'; // New section for MCP Servers

export interface Command {
  id: string;
  name: string;
  description?: string;
  shortcut?: string;
  keywords: string[];
  section: CommandSection;
  execute: (args?: any) => void;
  icon?: React.ReactNode;
  disabled?: boolean;
  active?: boolean; // Add this if not already present
}

export interface CommandGroup {
  id: string;
  name: string;
  description?: string;
  commands: Command[];
  priority?: number;
}