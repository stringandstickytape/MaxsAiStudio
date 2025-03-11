// src/stores/useCommandStore.ts
import { create } from 'zustand';
import { Command, CommandGroup, CommandSection } from '@/commands/types';

interface CommandState {
  // State
  commands: Map<string, Command>;
  groups: CommandGroup[];
  lastExecutedCommand: string | null;
  searchCache: Record<string, Command[]>;

  // Actions
  registerCommand: (command: Command) => void;
  registerGroup: (group: CommandGroup) => void;
  unregisterCommand: (commandId: string) => void;
  unregisterGroup: (groupId: string) => void;
  executeCommand: (commandId: string, args?: any) => boolean;
  searchCommands: (searchTerm: string) => Command[];
  clearSearchCache: () => void;

  // Selectors
  getCommandById: (id: string) => Command | undefined;
  getCommandsBySection: (section: CommandSection) => Command[];
  getAllCommands: () => Command[];
  getAllGroups: () => CommandGroup[];
}

export const useCommandStore = create<CommandState>((set, get) => ({
  // Initial state
  commands: new Map<string, Command>(),
  groups: [],
  lastExecutedCommand: null,
  searchCache: {},

  // Actions
  registerCommand: (command) =>
    set((state) => {
      const newCommands = new Map(state.commands);
      newCommands.set(command.id, command);

      // Also update any group this command belongs to
      const updatedGroups = [...state.groups];
      for (const group of updatedGroups) {
        const commandIndex = group.commands.findIndex((cmd) => cmd.id === command.id);
        if (commandIndex !== -1) {
          group.commands[commandIndex] = command;
        }
      }

      return {
        commands: newCommands,
        groups: updatedGroups,
        searchCache: {}, // Clear search cache when commands change
      };
    }),

  registerGroup: (group) =>
    set((state) => {
      // Check if group exists - update if it does, add if it doesn't
      const existingGroupIndex = state.groups.findIndex((g) => g.id === group.id);
      let updatedGroups = [...state.groups];

      if (existingGroupIndex !== -1) {
        // Update existing group
        const existingGroup = state.groups[existingGroupIndex];
        updatedGroups[existingGroupIndex] = {
          ...existingGroup,
          ...group,
          commands: [...group.commands],
        };
      } else {
        // Add new group
        updatedGroups.push({ ...group, commands: [...group.commands] });
      }

      // Sort groups by priority
      updatedGroups.sort((a, b) => (b.priority || 0) - (a.priority || 0));

      // Also update the commands map with all commands in the group
      const newCommands = new Map(state.commands);
      for (const command of group.commands) {
        newCommands.set(command.id, command);
      }

      return {
        groups: updatedGroups,
        commands: newCommands,
        searchCache: {}, // Clear search cache when groups change
      };
    }),

  unregisterCommand: (commandId) =>
    set((state) => {
      const newCommands = new Map(state.commands);
      newCommands.delete(commandId);

      // Also remove from any groups
      const updatedGroups = state.groups.map((group) => ({
        ...group,
        commands: group.commands.filter((cmd) => cmd.id !== commandId),
      }));

      return {
        commands: newCommands,
        groups: updatedGroups,
        searchCache: {}, // Clear search cache when commands change
      };
    }),

  unregisterGroup: (groupId) =>
    set((state) => {
      // Find the group to remove
      const groupToRemove = state.groups.find((g) => g.id === groupId);
      if (!groupToRemove) return state;

      // Get all command IDs from this group
      const commandIdsToRemove = groupToRemove.commands.map((cmd) => cmd.id);

      // Remove group
      const updatedGroups = state.groups.filter((g) => g.id !== groupId);

      // Remove commands that were only in this group
      const newCommands = new Map(state.commands);

      // Check if commands exist in other groups before removing
      for (const commandId of commandIdsToRemove) {
        let existsInOtherGroups = false;
        for (const group of updatedGroups) {
          if (group.commands.some((cmd) => cmd.id === commandId)) {
            existsInOtherGroups = true;
            break;
          }
        }

        if (!existsInOtherGroups) {
          newCommands.delete(commandId);
        }
      }

      return {
        commands: newCommands,
        groups: updatedGroups,
        searchCache: {}, // Clear search cache when groups change
      };
    }),

  executeCommand: (commandId, args) => {
    const command = get().getCommandById(commandId);
    if (!command || command.disabled) return false;

    try {
      command.execute(args);
      set({ lastExecutedCommand: commandId });
      return true;
    } catch (error) {
      console.error(`Error executing "${commandId}":`, error);
      return false;
    }
  },

  searchCommands: (searchTerm) => {
    // Check cache first
    const { searchCache, commands } = get();
    if (searchCache[searchTerm]) {
      return searchCache[searchTerm];
    }

    if (!searchTerm) {
      const allCommands = Array.from(commands.values());
      set((state) => ({
        searchCache: {
          ...state.searchCache,
          '': allCommands,
        },
      }));
      return allCommands;
    }

    const tokens = searchTerm
      .toLowerCase()
      .split(/\s+/)
      .filter((token) => token.length > 0);

    const results = Array.from(commands.values()).filter((command) =>
      tokens.every(
        (token) =>
          command.name.toLowerCase().includes(token) ||
          command.id.toLowerCase().includes(token) ||
          command.keywords.some((keyword) => keyword.toLowerCase().includes(token)) ||
          (command.description?.toLowerCase().includes(token) ?? false),
      ),
    );

    // Update cache with results
    set((state) => ({
      searchCache: {
        ...state.searchCache,
        [searchTerm]: results,
      },
    }));

    return results;
  },

  clearSearchCache: () => set({ searchCache: {} }),

  // Selectors
  getCommandById: (id) => get().commands.get(id),

  getCommandsBySection: (section) => {
    return Array.from(get().commands.values()).filter((command) => command.section === section);
  },

  getAllCommands: () => Array.from(get().commands.values()),

  getAllGroups: () => get().groups,
}));

// For backwards compatibility with existing code
export const commandRegistry = {
  registerCommand: (command: Command) => useCommandStore.getState().registerCommand(command),
  registerGroup: (group: CommandGroup) => useCommandStore.getState().registerGroup(group),
  unregisterCommandGroup: (groupId: string) => useCommandStore.getState().unregisterGroup(groupId),
  executeCommand: (commandId: string, args?: any) => useCommandStore.getState().executeCommand(commandId, args),
  getCommandById: (id: string) => useCommandStore.getState().getCommandById(id),
  searchCommands: (searchTerm: string) => useCommandStore.getState().searchCommands(searchTerm),
  getAllCommands: () => useCommandStore.getState().getAllCommands(),
  getAllCommandGroups: () => useCommandStore.getState().getAllGroups(),
  subscribe: (listener: () => void) => {
    return useCommandStore.subscribe(listener);
  },
};

// Debug helper
export const debugCommandStore = () => {
  const state = useCommandStore.getState();
  console.group('Command Store Debug');
  console.log('Command Count:', state.commands.size);
  console.log('Group Count:', state.groups.length);
  console.log('Last Executed Command:', state.lastExecutedCommand);
  console.log('Search Cache Size:', Object.keys(state.searchCache).length);
  console.log('All Groups:', state.groups);
  console.groupEnd();
  return state;
};

// For console debugging
(window as any).debugCommandStore = debugCommandStore;
