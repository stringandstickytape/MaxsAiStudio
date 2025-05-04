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
    
    
    // Helper function to check if a string matches a pattern with wildcards
    const matchesWildcard = (text: string, pattern: string): boolean => {
      // Convert strings to lowercase for case-insensitive matching
      const lowerText = text.toLowerCase();
      const lowerPattern = pattern.toLowerCase();
      
      
      // For patterns without wildcards, use simple includes
      if (!lowerPattern.includes('*') && !lowerPattern.includes('?')) {
        const result = lowerText.includes(lowerPattern);
        return result;
      }
      
      // Unified wildcard matching approach for both * and ? wildcards
      try {
        // If pattern has no wildcards, use simple includes
        if (!lowerPattern.includes('*') && !lowerPattern.includes('?')) {
          const result = lowerText.includes(lowerPattern);
          return result;
        }
        
        // If pattern is just * or **, match everything
        if (lowerPattern.replace(/\*/g, '').length === 0) {
          return true;
        }
        
        // First handle ? wildcards by converting them to regex dots
        // but only for parts between * characters
        const parts = lowerPattern.split('*');
        
        // Convert ? to regex dots in each part
        const processedParts = parts.map(part => {
          if (part.includes('?')) {
            // Escape regex special chars except ? which we'll replace
            return part.replace(/[.*+^${}()|[\]\\]/g, '\\$&')
                      .replace(/\?/g, '.');
          }
          return part;
        });
        
        // Filter out empty parts (adjacent * characters)
        const nonEmptyParts = processedParts.filter(part => part.length > 0);
        
        // If no non-empty parts, match everything
        if (nonEmptyParts.length === 0) {
          return true;
        }
        
        // Check if all parts appear in sequence
        let currentPos = 0;
        for (const part of nonEmptyParts) {
          // If part contains regex dots (from ? wildcards), use regex to find it
          if (part.includes('.')) {
            const regex = new RegExp(part);
            // Find the next position where this regex matches
            let found = false;
            while (currentPos <= lowerText.length) {
              const substring = lowerText.substring(currentPos);
              const match = substring.match(regex);
              if (match && match.index !== undefined) {
                currentPos += match.index + match[0].length;
                found = true;
                break;
              }
              currentPos++;
            }
            if (!found) {
              return false;
            }
          } else {
            // For parts without ? wildcards, use simple indexOf
            const pos = lowerText.indexOf(part, currentPos);
            if (pos === -1) {
              return false;
            }
            currentPos = pos + part.length;
          }
        }
        
        return true;
      } catch (error) {
        console.error('Error in wildcard matching:', error);
      }
      
      // Fallback to simple includes if all else fails
      const fallbackResult = lowerText.includes(lowerPattern.replace(/[*?]/g, ''));
      return fallbackResult;
    };
    
    const filteredItems = items.filter(item => {
      // Ensure item and item.name are defined before matching
      if (!item || typeof item.name !== 'string') return false;
      
      const nameMatch = matchesWildcard(item.name, query);
      const descriptionMatch = item.description && 
        typeof item.description === 'string' && 
        matchesWildcard(item.description, query);
      
      const result = nameMatch || descriptionMatch;
      return result;
    });
    
    return filteredItems;
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
        // Filter out any invalid items
        const validItems = items.filter(item => 
          item && typeof item.id === 'string' && 
          typeof item.name === 'string' && 
          typeof item.getTextToInsert === 'function'
        );
        allItems.push(...validItems);
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