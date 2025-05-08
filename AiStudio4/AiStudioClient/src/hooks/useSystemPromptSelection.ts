// AiStudioClient\src\hooks\useSystemPromptSelection.ts
import { useCallback } from 'react';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useToolStore } from '@/stores/useToolStore';
import { useModelStore } from '@/stores/useModelStore';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
import { SystemPrompt } from '@/types/systemPrompt';
import { useConvStore } from '@/stores/useConvStore';
import { createApiRequest } from '@/utils/apiUtils';

/**
 * A hook that provides a unified way to select system prompts across the application.
 * Handles all side effects of selecting a system prompt:
 * - Sets the conversation's system prompt
 * - Sets the prompt as default (if requested)
 * - Loads associated tools
 * - Loads associated user prompt
 * - Sets associated models (primary and secondary)
 * - Activates associated MCP servers
 */
export function useSystemPromptSelection() {
  const { activeConvId } = useConvStore();
  const { setConvPrompt } = useSystemPromptStore();
  const { setActiveTools } = useToolStore.getState();
  const { selectPrimaryModel, selectSecondaryModel } = useModelStore();
  const { setEnabledServers } = useMcpServerStore();
  const { prompts: userPrompts, insertUserPrompt } = useUserPromptManagement();
  const { setConvSystemPrompt, setDefaultSystemPrompt } = useSystemPromptManagement();

  /**
   * Select a system prompt and handle all associated actions
   * @param prompt The system prompt to select
   * @param options Configuration options
   * @param options.setAsDefault Whether to also set this prompt as the default
   * @param options.convId Optional conversation ID (uses active conversation if not provided)
   */
  const selectSystemPrompt = useCallback(
    async (prompt: SystemPrompt, options?: { setAsDefault?: boolean; convId?: string }) => {
      const { setAsDefault = false, convId = activeConvId } = options || {};
      
      if (!convId) return false; // Cannot set without a conversation

      try {
        // 1. Set the conversation's system prompt
        await setConvSystemPrompt({ convId, promptId: prompt.guid });
        setConvPrompt(convId, prompt.guid); // Update Zustand store immediately

        // 2. Set as default if requested
        if (setAsDefault) {
          await setDefaultSystemPrompt(prompt.guid);
        }

        // 3. Dispatch event for system prompt selection
        window.dispatchEvent(
          new CustomEvent('system-prompt-selected', {
            detail: { promptId: prompt.guid }
          })
        );

        // 4. Synchronize active tools
        setActiveTools(Array.isArray(prompt.associatedTools) ? prompt.associatedTools : []);

        // 5. Activate associated MCP servers or clear them if none are associated
        setEnabledServers(Array.isArray(prompt.associatedMcpServers) ? prompt.associatedMcpServers : []);

        // 6. Handle associated user prompt if one exists
        if (prompt.associatedUserPromptId && prompt.associatedUserPromptId !== 'none') {
          // Find the user prompt in the local store instead of making an API call
          const userPrompt = userPrompts.find(up => up.guid === prompt.associatedUserPromptId);
          if (userPrompt) {
            insertUserPrompt(userPrompt);
          } else {
            console.warn('Associated user prompt not found in local store:', prompt.associatedUserPromptId);
          }
        }

        // 7. Set associated models if they exist
        if (prompt.primaryModelGuid && prompt.primaryModelGuid !== 'none') {
          selectPrimaryModel(prompt.primaryModelGuid);
          await createApiRequest('/api/setDefaultModel', 'POST')({ modelGuid: prompt.primaryModelGuid });
        }

        if (prompt.secondaryModelGuid && prompt.secondaryModelGuid !== 'none') {
          selectSecondaryModel(prompt.secondaryModelGuid);
          await createApiRequest('/api/setSecondaryModel', 'POST')({ modelGuid: prompt.secondaryModelGuid });
        }

        return true;
      } catch (error) {
        console.error(`Failed to select system prompt ${prompt.guid}:`, error);
        return false;
      }
    },
    [activeConvId, setConvSystemPrompt, setConvPrompt, setDefaultSystemPrompt, userPrompts, insertUserPrompt, selectPrimaryModel, selectSecondaryModel, setEnabledServers]
  );

  return { selectSystemPrompt };
}

/**
 * Standalone utility function for selecting a system prompt that can be used outside of React components.
 * This is useful for command handlers and other non-React contexts.
 * 
 * @param prompt The system prompt to select
 * @param options Configuration options
 */
export async function selectSystemPromptStandalone(prompt: SystemPrompt, options?: { 
  setAsDefault?: boolean; 
  convId?: string 
}) {
  const { setAsDefault = false, convId } = options || {};
  
  if (!convId) {
    console.error('Cannot select system prompt: No conversation ID provided');
    return false;
  }

  try {
    // 1. Set the conversation's system prompt via API
    await createApiRequest('/api/setConvSystemPrompt', 'POST')({
      convId,
      promptId: prompt.guid
    });
    
    // Update the store directly
    useSystemPromptStore.getState().setConvPrompt(convId, prompt.guid);

    // 2. Set as default if requested
    if (setAsDefault) {
      await createApiRequest('/api/setDefaultSystemPrompt', 'POST')({
        promptId: prompt.guid
      });
      useSystemPromptStore.getState().setDefaultPromptId(prompt.guid);
    }

    // 3. Dispatch event for system prompt selection
    window.dispatchEvent(
      new CustomEvent('system-prompt-selected', {
        detail: { promptId: prompt.guid }
      })
    );

    // 4. Synchronize active tools
    useToolStore.getState().setActiveTools(
      Array.isArray(prompt.associatedTools) ? prompt.associatedTools : []
    );

    // 5. Activate associated MCP servers or clear them if none are associated
    useMcpServerStore.getState().setEnabledServers(
      Array.isArray(prompt.associatedMcpServers) ? prompt.associatedMcpServers : []
    );

    // 6. Handle associated user prompt if one exists
    if (prompt.associatedUserPromptId && prompt.associatedUserPromptId !== 'none') {
      // We can't use the hook here, so we'll just dispatch an event that can be listened for
      window.dispatchEvent(
        new CustomEvent('load-associated-user-prompt', {
          detail: { userPromptId: prompt.associatedUserPromptId }
        })
      );
    }

    // 7. Set associated models if they exist
    if (prompt.primaryModelGuid && prompt.primaryModelGuid !== 'none') {
      useModelStore.getState().selectPrimaryModel(prompt.primaryModelGuid);
      await createApiRequest('/api/setDefaultModel', 'POST')({ modelGuid: prompt.primaryModelGuid });
    }

    if (prompt.secondaryModelGuid && prompt.secondaryModelGuid !== 'none') {
      useModelStore.getState().selectSecondaryModel(prompt.secondaryModelGuid);
      await createApiRequest('/api/setSecondaryModel', 'POST')({ modelGuid: prompt.secondaryModelGuid });
    }

    return true;
  } catch (error) {
    console.error(`Failed to select system prompt ${prompt.guid}:`, error);
    return false;
  }
}