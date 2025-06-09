// AiStudioClient\src\stores\usePinnedCommandsStore.ts
import { create } from 'zustand';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { createApiRequest } from '@/utils/apiUtils';

export interface PinnedCommand {
  id: string;
  name: string;
  iconName?: string;
  iconSet?: 'lucide' | 'lobehub';
  section: string;
  position?: number;
}

interface PinnedCommandsStore {
  
  pinnedCommands: PinnedCommand[];
  categoryOrder: string[];
  loading: boolean;
  error: string | null;
  isDragging: boolean;
  isModified: boolean;

  
  setPinnedCommands: (commands: PinnedCommand[]) => void;
  setCategoryOrder: (order: string[]) => void;
  addPinnedCommand: (command: PinnedCommand) => void;
  removePinnedCommand: (commandId: string) => void;
  reorderPinnedCommands: (commandIds: string[]) => void;
  reorderCategories: (categoryIds: string[]) => void;
  reorderCommandsInCategory: (category: string, commandIds: string[]) => void;
  fetchPinnedCommands: () => Promise<void>;
  savePinnedCommands: () => Promise<void>;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setIsDragging: (isDragging: boolean) => void;
  setIsModified: (isModified: boolean) => void;
}

export const usePinnedCommandsStore = create<PinnedCommandsStore>((set, get) => ({
  
  pinnedCommands: [],
  categoryOrder: [],
  loading: false,
  error: null,
  isDragging: false,
  isModified: false,

  
  setPinnedCommands: (commands) => set({ pinnedCommands: commands, isModified: false }),

  setCategoryOrder: (order) => set({ categoryOrder: order, isModified: true }),

  addPinnedCommand: (command) =>
    set((state) => {
      if (state.pinnedCommands.some((cmd) => cmd.id === command.id)) {
        return state;
      }
      
      // Set position for new command
      const categoryCommands = state.pinnedCommands.filter(cmd => cmd.section === command.section);
      const newCommand = { ...command, position: categoryCommands.length };
      
      // Add category to order if not present
      const newCategoryOrder = [...state.categoryOrder];
      if (!newCategoryOrder.includes(command.section)) {
        newCategoryOrder.push(command.section);
      }
      
      return { 
        pinnedCommands: [...state.pinnedCommands, newCommand], 
        categoryOrder: newCategoryOrder,
        isModified: true 
      };
    }),

  removePinnedCommand: (commandId) =>
    set((state) => ({
      pinnedCommands: state.pinnedCommands.filter((cmd) => cmd.id !== commandId),
      isModified: true,
    })),
    
  setIsModified: (isModified) => set({ isModified }),

  reorderPinnedCommands: (commandIds) =>
    set((state) => {
      const orderedCommands: PinnedCommand[] = [];
      commandIds.forEach((id) => {
        const command = state.pinnedCommands.find((cmd) => cmd.id === id);
        if (command) {
          orderedCommands.push(command);
        }
      });
      
      return { pinnedCommands: orderedCommands, isModified: true };
    }),

  reorderCategories: (categoryIds) =>
    set({ categoryOrder: categoryIds, isModified: true }),

  reorderCommandsInCategory: (category, commandIds) =>
    set((state) => {
      const updatedCommands = state.pinnedCommands.map(cmd => {
        if (cmd.section === category) {
          const newPosition = commandIds.indexOf(cmd.id);
          return { ...cmd, position: newPosition >= 0 ? newPosition : cmd.position || 0 };
        }
        return cmd;
      });
      
      return { pinnedCommands: updatedCommands, isModified: true };
    }),

  fetchPinnedCommands: async () => {
    const { setLoading, setError, setPinnedCommands, setCategoryOrder } = get();

    setLoading(true);
    setError(null);

    try {
      const pinnedCommandsGet = createApiRequest('/api/pinnedCommands/get', 'POST');
      const data = await pinnedCommandsGet({}, {
        headers: {
          'X-Client-Id': webSocketService.getClientId() || '',
        },
      });

      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch pinned commands');
      }

      setPinnedCommands(data.pinnedCommands || []);
      set({ categoryOrder: data.categoryOrder || [], isModified: false });
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      console.error('Error fetching pinned commands:', error);
    } finally {
      setLoading(false);
    }
  },

  savePinnedCommands: async () => {
    const { pinnedCommands, categoryOrder, setLoading, setError, setIsModified } = get();

    setLoading(true);
    setError(null);

    try {
      const pinnedCommandsSave = createApiRequest('/api/pinnedCommands/save', 'POST');
      const data = await pinnedCommandsSave({ 
        pinnedCommands, 
        categoryOrder 
      }, {
        headers: {
          'X-Client-Id': webSocketService.getClientId() || '',
        },
      });

      if (!data.success) {
        throw new Error(data.error || 'Failed to save pinned commands');
      }
      
      
      setIsModified(false);
      return true;
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      console.error('Error saving pinned commands:', error);
      return false;
    } finally {
      setLoading(false);
    }
  },

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),

  setIsDragging: (isDragging) => set({ isDragging }),
}));


export const debugPinnedCommands = () => {
  const state = usePinnedCommandsStore.getState();
  console.group('Pinned Commands Debug');
  console.log('Count:', state.pinnedCommands.length);
  console.log('Commands:', state.pinnedCommands);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.log('Dragging:', state.isDragging);
  console.groupEnd();
  return state;
};


(window as any).debugPinnedCommands = debugPinnedCommands;