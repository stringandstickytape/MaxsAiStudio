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
        return lowerText.includes(lowerPattern);
      }
      
      // Check if any substring of the text matches the wildcard pattern
      for (let i = 0; i <= lowerText.length; i++) {
        if (matchFromPosition(lowerText, lowerPattern, i)) {
          return true;
        }
      }
      
      return false;
    };
    
    // Helper function to check if text matches pattern starting from a specific position
    const matchFromPosition = (text: string, pattern: string, startPos: number): boolean => {
      let textIndex = startPos;
      let patternIndex = 0;
      
      while (patternIndex < pattern.length && textIndex <= text.length) {
        // Handle * wildcard
        if (pattern[patternIndex] === '*') {
          // Last character in pattern is *, it matches everything remaining
          if (patternIndex === pattern.length - 1) {
            return true;
          }
          
          // Try to match the rest of the pattern with different positions in text
          const restPattern = pattern.substring(patternIndex + 1);
          for (let i = textIndex; i <= text.length; i++) {
            if (matchFromPosition(text, restPattern, i)) {
              return true;
            }
          }
          return false;
        }
        
        // End of text but not end of pattern
        if (textIndex === text.length) {
          return false;
        }
        
        // Handle ? wildcard or exact character match
        if (pattern[patternIndex] === '?' || pattern[patternIndex] === text[textIndex]) {
          textIndex++;
          patternIndex++;
        } else {
          return false;
        }
      }
      
      // Check if we've consumed the entire pattern
      return patternIndex === pattern.length;
    };
    
    return items.filter(item => {
      // Ensure item and item.name are defined before matching
      if (!item || typeof item.name !== 'string') return false;
      
      const nameMatch = matchesWildcard(item.name, query);
      const descriptionMatch = item.description && 
        typeof item.description === 'string' && 
        matchesWildcard(item.description, query);
      
      return nameMatch || descriptionMatch;
    });
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