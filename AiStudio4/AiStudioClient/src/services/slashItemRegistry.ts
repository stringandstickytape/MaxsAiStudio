// AiStudioClient/src/services/slashItemRegistry.ts

/**
 * Registry for slash command items that can be inserted into the input field
 * This service manages the registration and retrieval of items that appear
 * when a user types '/' in the input field.
 */

export interface SlashItem {
  id: string;
  name: string;
  description?: string;
  category?: string;
  getTextToInsert: () => string | Promise<string>;
}

export interface SlashItemProvider {
  getItems: () => SlashItem[] | Promise<SlashItem[]>;
}

class SlashItemRegistryService {
  private providers: SlashItemProvider[] = [];
  private cachedItems: SlashItem[] = [];
  private lastUpdateTime = 0;
  
  /**
   * Register a provider of slash items
   * @param provider The provider to register
   */
  registerProvider(provider: SlashItemProvider) {
    this.providers.push(provider);
    this.invalidateCache();
  }
  
  /**
   * Get all available slash items
   * @returns A promise that resolves to an array of slash items
   */
  async getItems(): Promise<SlashItem[]> {
    if (Date.now() - this.lastUpdateTime > 5000) {
      await this.refreshCache();
    }
    return this.cachedItems;
  }
  
  /**
   * Get slash items filtered by a query string
   * @param query The query string to filter by
   * @returns A promise that resolves to an array of filtered slash items
   */
  async getFilteredItems(query: string): Promise<SlashItem[]> {
    const items = await this.getItems();
    if (!query) return items;
    
    const lowerQuery = query.toLowerCase();
    return items.filter(item => 
      item.name.toLowerCase().includes(lowerQuery) || 
      item.description?.toLowerCase().includes(lowerQuery)
    );
  }
  
  /**
   * Invalidate the cache of slash items
   * This will force a refresh on the next getItems call
   */
  private invalidateCache() {
    this.lastUpdateTime = 0;
  }
  
  /**
   * Refresh the cache of slash items
   * @returns A promise that resolves to an array of slash items
   */
  private async refreshCache() {
    const allItems: SlashItem[] = [];
    for (const provider of this.providers) {
      try {
        const items = await provider.getItems();
        allItems.push(...items);
      } catch (error) {
        console.error('Error fetching items from provider:', error);
      }
    }
    this.cachedItems = allItems;
    this.lastUpdateTime = Date.now();
    return allItems;
  }
}

export const slashItemRegistry = new SlashItemRegistryService();