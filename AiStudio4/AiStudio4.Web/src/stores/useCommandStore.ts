
import { create } from 'zustand';
import { Command, CommandGroup, CommandSection } from '@/commands/types';

interface CommandState {
  
  commands: Map<string, Command>;
  groups: CommandGroup[];
  lastExecutedCommand: string | null;
  searchCache: Record<string, Command[]>;

  
  registerCommand: (command: Command) => void;
  registerGroup: (group: CommandGroup) => void;
  unregisterCommand: (commandId: string) => void;
  unregisterGroup: (groupId: string) => void;
  executeCommand: (commandId: string, args?: any) => boolean;
  searchCommands: (searchTerm: string) => Command[];
  clearSearchCache: () => void;

  
  getCommandById: (id: string) => Command | undefined;
  getCommandsBySection: (section: CommandSection) => Command[];
  getAllCommands: () => Command[];
  getAllGroups: () => CommandGroup[];
}

export const useCommandStore = create<CommandState>((set, get) => ({
  
  commands: new Map<string, Command>(),
  groups: [],
  lastExecutedCommand: null,
  searchCache: {},

  
  registerCommand: (command) =>
    set((state) => {
      const newCommands = new Map(state.commands);
      newCommands.set(command.id, command);

      
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
        searchCache: {}, 
      };
    }),

  registerGroup: (group) =>
    set((state) => {
      
      const existingGroupIndex = state.groups.findIndex((g) => g.id === group.id);
      let updatedGroups = [...state.groups];

      if (existingGroupIndex !== -1) {
        
        const existingGroup = state.groups[existingGroupIndex];
        updatedGroups[existingGroupIndex] = {
          ...existingGroup,
          ...group,
          commands: [...group.commands],
        };
      } else {
        
        updatedGroups.push({ ...group, commands: [...group.commands] });
      }

      
      updatedGroups.sort((a, b) => (b.priority || 0) - (a.priority || 0));

      
      const newCommands = new Map(state.commands);
      for (const command of group.commands) {
        newCommands.set(command.id, command);
      }

      return {
        groups: updatedGroups,
        commands: newCommands,
        searchCache: {}, 
      };
    }),

  unregisterCommand: (commandId) =>
    set((state) => {
      const newCommands = new Map(state.commands);
      newCommands.delete(commandId);

      
      const updatedGroups = state.groups.map((group) => ({
        ...group,
        commands: group.commands.filter((cmd) => cmd.id !== commandId),
      }));

      return {
        commands: newCommands,
        groups: updatedGroups,
        searchCache: {}, 
      };
    }),

  unregisterGroup: (groupId) =>
    set((state) => {
      
      const groupToRemove = state.groups.find((g) => g.id === groupId);
      if (!groupToRemove) return state;

      
      const commandIdsToRemove = groupToRemove.commands.map((cmd) => cmd.id);

      
      const updatedGroups = state.groups.filter((g) => g.id !== groupId);

      
      const newCommands = new Map(state.commands);

      
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
        searchCache: {}, 
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

    
    set((state) => ({
      searchCache: {
        ...state.searchCache,
        [searchTerm]: results,
      },
    }));

    return results;
  },

  clearSearchCache: () => set({ searchCache: {} }),

  
  getCommandById: (id) => get().commands.get(id),

  getCommandsBySection: (section) => {
    return Array.from(get().commands.values()).filter((command) => command.section === section);
  },

  getAllCommands: () => Array.from(get().commands.values()),

  getAllGroups: () => get().groups,
}));


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


(window as any).debugCommandStore = debugCommandStore;

