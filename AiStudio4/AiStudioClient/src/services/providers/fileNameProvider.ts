// AiStudioClient/src/services/providers/fileNameProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';
import { useFileSystemStore } from '../../stores/useFileSystemStore';

/**
 * Provider for file names as slash command items
 * Combines hardcoded examples with actual project files from the file system store
 */
export class FileNameProvider implements SlashItemProvider {
  // Hardcoded examples that will remain alongside real project files
  private hardcodedFiles: { path: string; name: string }[] = [
    { path: 'C:/project/src/main.ts', name: 'main.ts' },
    { path: 'C:/project/src/app.tsx', name: 'app.tsx' },
    { path: 'C:/project/package.json', name: 'package.json' },
    // Fun example item
    { path: 'sausages', name: 'roflcopters' }
  ];
  
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
    // Combine hardcoded files and project files
    const allFiles = [...this.hardcodedFiles, ...this.projectFiles];
    
    // Convert to slash items
    return allFiles.map(file => ({
      id: `file-${file.path}`,
      name: file.name,
      description: file.name === 'roflcopters' ? 'Inserts the word "sausages"' : `File: ${file.path}`,
      category: 'Files',
      getTextToInsert: () => file.path
    }));
  }
}