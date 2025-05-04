// AiStudioClient/src/services/providers/fileNameProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';

/**
 * Provider for recent file names as slash command items
 * This is a placeholder implementation that would need to be connected
 * to an actual file service in the application.
 */
export class FileNameProvider implements SlashItemProvider {
  // In a real implementation, this would be populated from a file service
  private recentFiles: { path: string; name: string }[] = [];
  
  constructor() {
    // This would be replaced with actual file service integration
    this.loadRecentFiles();
  }
  
  private loadRecentFiles() {
    // This is a placeholder. In a real implementation, this would
    // fetch recent files from a service or API
    // For now, we'll use some dummy data
    this.recentFiles = [
      { path: 'C:/project/src/main.ts', name: 'main.ts' },
      { path: 'C:/project/src/app.tsx', name: 'app.tsx' },
      { path: 'C:/project/package.json', name: 'package.json' },
      // Add a fun example item as requested
      { path: 'sausages', name: 'roflcopters' }
    ];
  }
  
  async getItems(): Promise<SlashItem[]> {
    return this.recentFiles.map(file => ({
      id: `file-${file.path}`,
      name: file.name,
      description: file.name === 'roflcopters' ? 'Inserts the word "sausages"' : `File: ${file.path}`,
      category: 'Files',
      getTextToInsert: () => file.path
    }));
  }
}