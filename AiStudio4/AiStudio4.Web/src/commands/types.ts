// src/commands/types.ts
export type CommandSection = 'conv' | 'model' | 'view' | 'settings' | 'utility' | 'appearance';

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
}

export interface CommandGroup {
    id: string;
    name: string;
    description?: string;
    commands: Command[];
    priority?: number; // Higher numbers appear first
}