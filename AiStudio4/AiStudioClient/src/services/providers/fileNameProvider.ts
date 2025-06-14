// AiStudioClient/src/services/providers/fileNameProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';
import { useFileSystemStore } from '../../stores/useFileSystemStore';

/**
 * Provider for file names as slash command items
 */
export class FileNameProvider implements SlashItemProvider {
  
  // Project files from the file system store
  private projectFiles: { path: string; name: string }[] = [];
  
  // Unsubscribe function for store subscription
  private unsubscribe: (() => void) | null = null;
  
  constructor() {
    // Subscribe to the file system store
    this.subscribeToFileSystemStore();
  }
  
  /**
   * Subscribe to the file system store to get updates when files change
   */
  private subscribeToFileSystemStore() {
    // Initial load of project files
    this.updateProjectFiles();
    
    // Subscribe to changes in the file system store
    this.unsubscribe = useFileSystemStore.subscribe(
      (state) => {
        // Only update if files have changed
        this.updateProjectFiles();
      }
    );
  }
  
  /**
   * Update project files from the file system store
   */
  private updateProjectFiles() {
    const { files } = useFileSystemStore.getState();
    
    // Convert file paths to the format needed for slash items
    this.projectFiles = files.map(filePath => {
      // Extract the file name from the path
      const name = filePath.split('/').pop() || filePath;
      return { path: filePath, name };
    });
  }
  
  /**
   * Clean up subscription when provider is no longer needed
   */
  public dispose() {
    if (this.unsubscribe) {
      this.unsubscribe();
      this.unsubscribe = null;
    }
  }
  
  /**
   * Get all items from both hardcoded examples and project files
   */
  async getItems(): Promise<SlashItem[]> {
    
    // Convert to slash items
      return this.projectFiles.map(file => ({
      id: `file-${file.path}`,
      name: file.name,
      description: `File: ${file.path}`,
      category: 'Files',
      getTextToInsert: () => file.path
    }));
  }
}