// src/commands/commandRegistry.ts
import { Command, CommandGroup } from './types';

class CommandRegistryService {
    private commandGroups: CommandGroup[] = [];
    private commandsById: Map<string, Command> = new Map();
    private commandListeners: Set<() => void> = new Set();

    constructor() {
        // Initialize with core command groups
        this.registerGroup({
            id: 'core',
            name: 'Core Actions',
            priority: 100,
            commands: []
        });
    }

    // Register an individual command
    registerCommand(command: Command): void {
        if (this.commandsById.has(command.id)) {
            console.warn(`Command with id "${command.id}" is already registered. Overwriting.`);
        }

        // Find the group or use 'utility' as default
        const sectionId = command.section || 'utility';
        let group = this.commandGroups.find(g => g.id === sectionId);

        if (!group) {
            // Create the group if it doesn't exist
            group = {
                id: sectionId,
                name: this.formatGroupName(sectionId),
                commands: []
            };
            this.commandGroups.push(group);
        }

        // Add to group
        group.commands.push(command);
        // Add to lookup map
        this.commandsById.set(command.id, command);

        // Notify listeners
        this.notifyListeners();
    }

    // Register a group of commands
    registerGroup(group: CommandGroup): void {
        const existingGroupIndex = this.commandGroups.findIndex(g => g.id === group.id);

        if (existingGroupIndex !== -1) {
            // Merge with existing group
            this.commandGroups[existingGroupIndex] = {
                ...this.commandGroups[existingGroupIndex],
                ...group,
                commands: [
                    ...this.commandGroups[existingGroupIndex].commands,
                    ...group.commands
                ]
            };
        } else {
            // Add as new group
            this.commandGroups.push(group);
        }

        // Add all commands to the lookup map
        group.commands.forEach(command => {
            this.commandsById.set(command.id, command);
        });

        // Notify listeners
        this.notifyListeners();
    }

    // Get all commands, organized by groups
    getAllCommandGroups(): CommandGroup[] {
        // Sort groups by priority
        return [...this.commandGroups].sort((a, b) =>
            (b.priority || 0) - (a.priority || 0)
        );
    }

    // Get a flattened list of all commands
    getAllCommands(): Command[] {
        return Array.from(this.commandsById.values());
    }

    // Get a command by ID
    getCommandById(id: string): Command | undefined {
        return this.commandsById.get(id);
    }

    // Execute a command by ID
    executeCommand(id: string, args?: any): boolean {
        const command = this.getCommandById(id);
        if (!command || command.disabled) {
            return false;
        }

        try {
            command.execute(args);
            return true;
        } catch (error) {
            console.error(`Error executing command "${id}":`, error);
            return false;
        }
    }

    // Filter commands by search term
    // Filter commands by search term
    searchCommands(searchTerm: string): Command[] {
        if (!searchTerm) {
            return this.getAllCommands();
        }

        // Split the search term into tokens and filter out empty strings
        const searchTokens = searchTerm.toLowerCase()
            .split(/\s+/)
            .filter(token => token.length > 0);

        return this.getAllCommands().filter(command => {
            // Check if all tokens are present in any of the searchable fields
            return searchTokens.every(token => {
                // Match by name
                if (command.name.toLowerCase().includes(token)) {
                    return true;
                }

                // Match by id
                if (command.id.toLowerCase().includes(token)) {
                    return true;
                }

                // Match by keywords
                if (command.keywords.some(keyword =>
                    keyword.toLowerCase().includes(token)
                )) {
                    return true;
                }

                // Match by description
                if (command.description?.toLowerCase().includes(token)) {
                    return true;
                }

                return false;
            });
        });
    }

    // Subscribe to command registry changes
    subscribe(listener: () => void): () => void {
        this.commandListeners.add(listener);
        return () => {
            this.commandListeners.delete(listener);
        };
    }

    // Notify all listeners of changes
    private notifyListeners(): void {
        this.commandListeners.forEach(listener => listener());
    }

    // Format a group ID into a readable name
    private formatGroupName(id: string): string {
        return id
            .split('-')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }
}

// Create a singleton instance
export const commandRegistry = new CommandRegistryService();

// Utility function for plugin registration
export function registerCommand(command: Command): void {
    commandRegistry.registerCommand(command);
}

// Utility function for group registration
export function registerCommandGroup(group: CommandGroup): void {
    commandRegistry.registerGroup(group);
}