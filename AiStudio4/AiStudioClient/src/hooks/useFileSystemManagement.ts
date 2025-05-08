// AiStudioClient/src/hooks/useFileSystemManagement.ts
import { useFileSystemStore } from '@/stores/useFileSystemStore';
import { createResourceHook } from './useResourceFactory';

// Define the resource hook for file system data
const useFileSystemResource = createResourceHook<{ directories: string[], files: string[] }>({ 
  endpoints: {
    fetch: '/api/getFileSystem',
  },
  storeActions: {
    setItems: (data) => {
      if (data && data.length > 0 && data[0]) {
        const fileSystemData = data[0];
        useFileSystemStore.getState().updateFileSystem(
          fileSystemData.directories || [],
          fileSystemData.files || []
        );
      }
    },
  },
  options: {
    transformFetchResponse: (data) => {
      // Return as an array with a single item to match the resource hook pattern
      return [{
        directories: data.directories || [],
        files: data.files || []
      }];
    },
  },
});

/**
 * Hook for managing file system data
 */
export function useFileSystemManagement() {
  const {
    isLoading,
    error,
    fetchItems: fetchFileSystem,
    clearError,
  } = useFileSystemResource();

  const { directories, files } = useFileSystemStore();

  return {
    directories,
    files,
    isLoading,
    error,
    fetchFileSystem,
    clearError,
  };
}