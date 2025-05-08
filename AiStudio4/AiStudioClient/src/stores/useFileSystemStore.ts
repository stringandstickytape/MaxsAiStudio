// AiStudioClient/src/stores/useFileSystemStore.ts

import { create } from 'zustand';

interface FileSystemState {
  // State
  directories: string[];
  files: string[];
  loading: boolean;
  error: string | null;

  // Actions
  setDirectories: (directories: string[]) => void;
  setFiles: (files: string[]) => void;
  updateFileSystem: (directories: string[], files: string[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export const useFileSystemStore = create<FileSystemState>((set) => ({
  // Initial state
  directories: [],
  files: [],
  loading: false,
  error: null,

  // Actions
  setDirectories: (directories) => set({ directories }),
  setFiles: (files) => set({ files }),
  updateFileSystem: (directories, files) => set({ directories, files }),
  setLoading: (loading) => set({ loading }),
  setError: (error) => set({ error }),
}));

// Helper function for debugging
export const debugFileSystemStore = () => {
  const state = useFileSystemStore.getState();
  console.group('File System Store Debug');
  console.log('Directories:', state.directories);
  console.log('Files:', state.files);
  console.log('Loading:', state.loading);
  console.log('Error:', state.error);
  console.groupEnd();
  return state;
};

// Expose to window for debugging
(window as any).debugFileSystemStore = debugFileSystemStore;