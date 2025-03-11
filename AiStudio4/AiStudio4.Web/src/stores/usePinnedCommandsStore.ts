// src/stores/usePinnedCommandsStore.ts
import { create } from 'zustand';

export interface PinnedCommand {
  id: string;
  name: string;
  iconName?: string;
  section: string;
}

interface PinnedCommandsStore {
  // State
  pinnedCommands: PinnedCommand[];
  loading: boolean;
  error: string | null;
  isDragging: boolean;

  // Actions
  setPinnedCommands: (commands: PinnedCommand[]) => void;
  addPinnedCommand: (command: PinnedCommand) => void;
  removePinnedCommand: (commandId: string) => void;
  reorderPinnedCommands: (commandIds: string[]) => void;
  fetchPinnedCommands: () => Promise<void>;
  savePinnedCommands: () => Promise<void>;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setIsDragging: (isDragging: boolean) => void;
}

export const usePinnedCommandsStore = create<PinnedCommandsStore>((set, get) => ({
  // Initial state
  pinnedCommands: [],
  loading: false,
  error: null,
  isDragging: false,

  // Actions
  setPinnedCommands: (commands) => set({ pinnedCommands: commands }),

  addPinnedCommand: (command) =>
    set((state) => {
      if (state.pinnedCommands.some((cmd) => cmd.id === command.id)) {
        return state;
      }
      return { pinnedCommands: [...state.pinnedCommands, command] };
    }),

  removePinnedCommand: (commandId) =>
    set((state) => ({
      pinnedCommands: state.pinnedCommands.filter((cmd) => cmd.id !== commandId),
    })),

  reorderPinnedCommands: (commandIds) =>
    set((state) => {
      const orderedCommands: PinnedCommand[] = [];
      commandIds.forEach((id) => {
        const command = state.pinnedCommands.find((cmd) => cmd.id === id);
        if (command) {
          orderedCommands.push(command);
        }
      });
      // Set user-modified flag to trigger save
      window.localStorage.setItem('pinnedCommands_modified', 'true');
      return { pinnedCommands: orderedCommands };
    }),

  fetchPinnedCommands: async () => {
    const { setLoading, setError, setPinnedCommands } = get();

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/pinnedCommands/get', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': localStorage.getItem('clientId') || '',
        },
        body: JSON.stringify({}),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to fetch pinned commands');
      }

      setPinnedCommands(data.pinnedCommands || []);
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      console.error('Error fetching pinned commands:', error);
    } finally {
      setLoading(false);
    }
  },

  savePinnedCommands: async () => {
    const { pinnedCommands, setLoading, setError } = get();

    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/pinnedCommands/save', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Id': localStorage.getItem('clientId') || '',
        },
        body: JSON.stringify({ pinnedCommands }),
      });

      const data = await response.json();

      if (!data.success) {
        throw new Error(data.error || 'Failed to save pinned commands');
      }
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Unknown error');
      console.error('Error saving pinned commands:', error);
    } finally {
      setLoading(false);
    }
  },

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error }),

  setIsDragging: (isDragging) => set({ isDragging }),
}));

// Debug helper for console
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

// Export for console access
(window as any).debugPinnedCommands = debugPinnedCommands;
