// AiStudioClient/src/stores/usePinnedCommandsStore.ts
import { create } from 'zustand';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { createApiRequest } from '@/utils/apiUtils';
import { arrayMove } from '@dnd-kit/sortable';

export interface PinnedCommand {
  id: string;
  name: string;
  iconName?: string;
  iconSet?: 'lucide' | 'lobehub';
  section: string;
}

export interface PinnedCommandsStore {
  pinnedCommands: PinnedCommand[];
  loading: boolean;
  error: string | null;
  isDragging: boolean;
  isModified: boolean;
  categoryOrder: string[];

  setPinnedCommands: (commands: PinnedCommand[]) => void;
  addPinnedCommand: (command: PinnedCommand) => void;
  removePinnedCommand: (commandId: string) => void;
  reorderPinnedCommands: (activeId: string, overId: string) => void;
  setCategoryOrder: (categoryIds: string[]) => void;
  fetchPinnedCommands: () => Promise<void>;
  savePinnedCommands: () => Promise<void>;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setIsDragging: (isDragging: boolean) => void;
  setIsModified: (isModified: boolean) => void;
}

export const usePinnedCommandsStore = create<PinnedCommandsStore>((set, get) => ({
  pinnedCommands: [],
  loading: false,
  error: null,
  isDragging: false,
  isModified: false,
  categoryOrder: [],

  setPinnedCommands: (commands) => {
    const orderedCategories = commands
      .map(cmd => cmd.section || 'Uncategorized')
      .filter((value, index, self) => self.indexOf(value) === index);
    set({ pinnedCommands: commands, categoryOrder: orderedCategories, isModified: false });
  },

  addPinnedCommand: (command) =>
    set((state) => {
      if (state.pinnedCommands.some((cmd) => cmd.id === command.id)) {
        return state;
      }
      // Add new category if not present
      const newCategories = [...state.categoryOrder];
      if (command.section && !newCategories.includes(command.section)) {
        newCategories.push(command.section);
      }
      return { pinnedCommands: [...state.pinnedCommands, command], isModified: true, categoryOrder: newCategories };
    }),

  removePinnedCommand: (commandId) =>
    set((state) => {
      const filtered = state.pinnedCommands.filter((cmd) => cmd.id !== commandId);
      const orderedCategories = filtered
        .map(cmd => cmd.section || 'Uncategorized')
        .filter((value, index, self) => self.indexOf(value) === index);
      return { pinnedCommands: filtered, isModified: true, categoryOrder: orderedCategories };
    }),

  setIsModified: (isModified) => set({ isModified }),

  reorderPinnedCommands: (activeId, overId) =>
    set((state) => {
      const oldIndex = state.pinnedCommands.findIndex((cmd) => cmd.id === activeId);
      const newIndex = state.pinnedCommands.findIndex((cmd) => cmd.id === overId);
      if (oldIndex !== -1 && newIndex !== -1) {
        return { pinnedCommands: arrayMove(state.pinnedCommands, oldIndex, newIndex), isModified: true };
      }
      return state;
    }),

  setCategoryOrder: (categoryIds) => set({ categoryOrder: categoryIds, isModified: true }),

  fetchPinnedCommands: async () => {
    const { setLoading, setError, setPinnedCommands } = get();
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
      const commands = data.pinnedCommands || [];
      const orderedCategories = commands
        .map(cmd => cmd.section || 'Uncategorized')
        .filter((value, index, self) => self.indexOf(value) === index);
      set({ pinnedCommands: commands, categoryOrder: orderedCategories, isModified: false });
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      console.error('Error fetching pinned commands:', error);
    } finally {
      setLoading(false);
    }
  },

  savePinnedCommands: async () => {
    const { pinnedCommands, setLoading, setError, setIsModified } = get();
    setLoading(true);
    setError(null);
    try {
      const pinnedCommandsSave = createApiRequest('/api/pinnedCommands/save', 'POST');
      const data = await pinnedCommandsSave({ pinnedCommands }, {
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