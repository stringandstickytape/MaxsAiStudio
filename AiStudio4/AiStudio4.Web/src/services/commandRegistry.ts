// AiStudio4.Web/src/services/commandRegistry.ts
import { useCommandStore } from '@/stores/useCommandStore';
import type { Command, CommandGroup, CommandSection } from '@/commands/types';

/**
 * Command Registry Service
 * 
 * A centralized service for command registration and management that abstracts
 * direct store access. This service provides methods for registering, unregistering,
 * and executing commands, as well as searching and retrieving commands.
 */
export const commandRegistry = {
  /**
   * Register a single command
   */
  registerCommand: (command: Command) => {
    useCommandStore.getState().registerCommand(command);
  },

  /**
   * Register a group of commands
   */
  registerGroup: (group: CommandGroup) => {
    useCommandStore.getState().registerGroup(group);
  },

  /**
   * Unregister a command group
   */
  unregisterGroup: (groupId: string) => {
    useCommandStore.getState().unregisterGroup(groupId);
  },

  /**
   * Unregister a single command
   */
  unregisterCommand: (commandId: string) => {
    useCommandStore.getState().unregisterCommand(commandId);
  },

  /**
   * Execute a command by ID
   */
  executeCommand: (commandId: string, args?: any) => {
    return useCommandStore.getState().executeCommand(commandId, args);
  },

  /**
   * Search for commands matching a search term
   */
  searchCommands: (searchTerm: string) => {
    return useCommandStore.getState().searchCommands(searchTerm);
  },

  /**
   * Get a command by ID
   */
  getCommandById: (id: string) => {
    return useCommandStore.getState().getCommandById(id);
  },

  /**
   * Get all commands in a specific section
   */
  getCommandsBySection: (section: CommandSection) => {
    return useCommandStore.getState().getCommandsBySection(section);
  },

  /**
   * Get all registered commands
   */
  getAllCommands: () => {
    return useCommandStore.getState().getAllCommands();
  },

  /**
   * Get all command groups
   */
  getAllGroups: () => {
    return useCommandStore.getState().getAllGroups();
  },

  /**
   * Subscribe to command store changes
   */
  subscribe: (listener: () => void) => {
    return useCommandStore.subscribe(listener);
  },
};