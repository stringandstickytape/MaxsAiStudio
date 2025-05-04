// AiStudioClient/src/services/providers/userPromptProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';
import { useUserPromptStore } from '@/stores/useUserPromptStore';

/**
 * Provider for user prompts as slash command items
 */
export class UserPromptProvider implements SlashItemProvider {
  async getItems(): Promise<SlashItem[]> {
    // Get user prompts from the store
    const userPrompts = useUserPromptStore.getState().prompts;
    
    return userPrompts.map(prompt => ({
      id: `prompt-${prompt.guid}`,
      name: prompt.name,
      description: prompt.description || 'User prompt',
      category: 'Prompts',
      getTextToInsert: () => prompt.content
    }));
  }
}