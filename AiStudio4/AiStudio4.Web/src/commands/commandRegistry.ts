// src/commands/commandRegistry.ts
import { Command, CommandGroup } from './types';

class CommandRegistryService {
    private groups: CommandGroup[] = [];
    private commands: Map<string, Command> = new Map();
    private listeners: Set<() => void> = new Set();

    constructor() {
        this.registerGroup({ id: 'core', name: 'Core Actions', priority: 100, commands: [] });
    }

    registerCommand(command: Command): void {
        if (this.commands.has(command.id)) {
            console.warn(`Command "${command.id}" already registered. Overwriting.`);
        }

        const sectionId = command.section || 'utility';
        let group = this.groups.find(g => g.id === sectionId);

        if (!group) {
            group = { id: sectionId, name: this.formatGroupName(sectionId), commands: [] };
            this.groups.push(group);
        }

        group.commands.push(command);
        this.commands.set(command.id, command);
        this.notifyListeners();
    }

    registerGroup(group: CommandGroup): void {
        const existingIndex = this.groups.findIndex(g => g.id === group.id);

        if (existingIndex !== -1) {
            const existingGroup = this.groups[existingIndex];
            this.groups[existingIndex] = {
                ...existingGroup,
                ...group,
                commands: [...existingGroup.commands, ...group.commands]
            };
        } else {
            this.groups.push(group);
        }

        group.commands.forEach(command => this.commands.set(command.id, command));
        this.notifyListeners();
    }

    getAllCommandGroups(): CommandGroup[] {
        return [...this.groups].sort((a, b) => (b.priority || 0) - (a.priority || 0));
    }

    getAllCommands(): Command[] {
        return Array.from(this.commands.values());
    }

    getCommandById(id: string): Command | undefined {
        return this.commands.get(id);
    }

    executeCommand(id: string, args?: any): boolean {
        const command = this.getCommandById(id);
        if (!command || command.disabled) return false;

        try {
            command.execute(args);
            return true;
        } catch (error) {
            console.error(`Error executing "${id}":`, error);
            return false;
        }
    }

    searchCommands(searchTerm: string): Command[] {
        if (!searchTerm) return this.getAllCommands();

        const tokens = searchTerm.toLowerCase().split(/\s+/).filter(token => token.length > 0);

        return this.getAllCommands().filter(command =>
            tokens.every(token =>
                command.name.toLowerCase().includes(token) ||
                command.id.toLowerCase().includes(token) ||
                command.keywords.some(keyword => keyword.toLowerCase().includes(token)) ||
                (command.description?.toLowerCase().includes(token) ?? false)
            )
        );
    }

    subscribe(listener: () => void): () => void {
        this.listeners.add(listener);
        return () => this.listeners.delete(listener);
    }

    private notifyListeners(): void {
        this.listeners.forEach(listener => listener());
    }

    private formatGroupName(id: string): string {
        return id.split('-').map(word => word.charAt(0).toUpperCase() + word.slice(1)).join(' ');
    }
}

export const commandRegistry = new CommandRegistryService();

export function registerCommand(command: Command): void {
    commandRegistry.registerCommand(command);
}

export function registerCommandGroup(group: CommandGroup): void {
    commandRegistry.registerGroup(group);
}