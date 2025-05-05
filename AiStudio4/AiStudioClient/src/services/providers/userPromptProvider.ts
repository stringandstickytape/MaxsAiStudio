// AiStudioClient/src/services/providers/userPromptProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';
import { useUserPromptStore } from '@/stores/useUserPromptStore';

/**
 * Provider for user prompts as slash command items
 */
export class UserPromptProvider implements SlashItemProvider {
  async getItems(): Promise<SlashItem[]> {
    try {
      // Get user prompts from the store
      const userPrompts = useUserPromptStore.getState().prompts || [];
      
      // Add a default item if no prompts are available
      if (userPrompts.length === 0) {
        return [{
          id: 'default-prompt',
          name: 'Example Prompt',
          description: 'This is an example prompt',
          category: 'Prompts',
          getTextToInsert: () => 'This is an example prompt that would be inserted.'
        }];
      }
      
      return userPrompts.map(prompt => ({
        id: `prompt-${prompt.guid}`,
        name: (prompt.title || 'Unnamed') + ' (User Prompt)',
        description: prompt.tags || 'User prompt',
        category: 'Prompts',
        getTextToInsert: () => prompt.content || ''
      }));
    } catch (error) {
      console.error('Error getting user prompts:', error);
      return [];
    }
  }
}