// src/services/SystemPromptService.ts
import { SystemPrompt } from '@/types/systemPrompt';

export class SystemPromptService {
    /**
     * Normalizes prompt data to ensure consistent property naming
     */
    private static normalizePrompt(prompt: any): SystemPrompt {
        if (!prompt) return null;

        return {
            guid: prompt.guid || prompt.Guid,
            title: prompt.title || prompt.Title,
            content: prompt.content || prompt.Content,
            description: prompt.description || prompt.Description,
            isDefault: prompt.isDefault || prompt.IsDefault,
            createdDate: prompt.createdDate || prompt.CreatedDate,
            modifiedDate: prompt.modifiedDate || prompt.ModifiedDate,
            tags: prompt.tags || prompt.Tags || [],
        };
    }

    /**
     * Fetches all system prompts
     */
    static async getSystemPrompts(): Promise<SystemPrompt[]> {
        try {
            const response = await fetch('/api/getSystemPrompts', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({}),
            });

            if (!response.ok) {
                throw new Error(`Error fetching system prompts: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to fetch system prompts');
            }

            // Normalize all prompts
            return Array.isArray(data.prompts)
                ? data.prompts.map(this.normalizePrompt)
                : [];
        } catch (error) {
            console.error('Error fetching system prompts:', error);
            throw error;
        }
    }

    /**
     * Fetches a system prompt by ID
     */
    static async getSystemPromptById(promptId: string): Promise<SystemPrompt> {
        try {
            const response = await fetch('/api/getSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({ promptId }),
            });

            if (!response.ok) {
                throw new Error(`Error fetching system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to fetch system prompt');
            }

            return this.normalizePrompt(data.prompt);
        } catch (error) {
            console.error('Error fetching system prompt:', error);
            throw error;
        }
    }

    /**
     * Creates a new system prompt
     */
    static async createSystemPrompt(promptData: Omit<SystemPrompt, 'guid' | 'createdDate' | 'modifiedDate'>): Promise<SystemPrompt> {
        try {
            console.log('Creating system prompt with data:', promptData);

            const response = await fetch('/api/createSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify(promptData),
            });

            if (!response.ok) {
                throw new Error(`Error creating system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to create system prompt');
            }

            const normalizedPrompt = this.normalizePrompt(data.prompt);
            console.log('Created system prompt, normalized response:', normalizedPrompt);
            return normalizedPrompt;
        } catch (error) {
            console.error('Error creating system prompt:', error);
            throw error;
        }
    }

    /**
     * Updates an existing system prompt
     */
    static async updateSystemPrompt(promptData: SystemPrompt): Promise<SystemPrompt> {
        try {
            // Ensure we have a guid
            const promptId = promptData.guid || promptData['Guid'];
            if (!promptId) {
                throw new Error('Prompt ID is required for updating');
            }

            const response = await fetch('/api/updateSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({
                    ...promptData,
                    // Ensure both casing variants are included for backend compatibility
                    guid: promptId,
                    Guid: promptId
                }),
            });

            if (!response.ok) {
                throw new Error(`Error updating system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to update system prompt');
            }

            return this.normalizePrompt(data.prompt);
        } catch (error) {
            console.error('Error updating system prompt:', error);
            throw error;
        }
    }

    /**
     * Deletes a system prompt
     */
    static async deleteSystemPrompt(promptId: string): Promise<boolean> {
        try {
            const response = await fetch('/api/deleteSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({ promptId }),
            });

            if (!response.ok) {
                throw new Error(`Error deleting system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to delete system prompt');
            }

            return true;
        } catch (error) {
            console.error('Error deleting system prompt:', error);
            throw error;
        }
    }

    /**
     * Sets a system prompt as the default
     */
    static async setDefaultSystemPrompt(promptId: string): Promise<boolean> {
        try {
            if (!promptId) {
                throw new Error('Prompt ID is required');
            }

            const response = await fetch('/api/setDefaultSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({ promptId }),
            });

            if (!response.ok) {
                throw new Error(`Error setting default system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to set default system prompt');
            }

            return true;
        } catch (error) {
            console.error('Error setting default system prompt:', error);
            throw error;
        }
    }

    /**
     * Gets the system prompt associated with a conversation
     */
    static async getConversationSystemPrompt(conversationId: string): Promise<SystemPrompt | null> {
        try {
            if (!conversationId) {
                throw new Error('Conversation ID is required');
            }

            const response = await fetch('/api/getConversationSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({ conversationId }),
            });

            if (!response.ok) {
                throw new Error(`Error fetching conversation system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to fetch conversation system prompt');
            }

            return data.prompt ? this.normalizePrompt(data.prompt) : null;
        } catch (error) {
            console.error('Error fetching conversation system prompt:', error);
            throw error;
        }
    }

    /**
     * Sets a system prompt for a specific conversation
     */
    // src/services/SystemPromptService.ts
    // Update the setConversationSystemPrompt method

    static async setConversationSystemPrompt(conversationId: string, promptId: string): Promise<boolean> {
        console.log(`Setting conversation system prompt: conversationId=${conversationId}, promptId=${promptId}`);

        try {
            // Add client ID if needed for your API
            const clientId = localStorage.getItem('clientId') || '';

            const response = await fetch('/api/setConversationSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': clientId, // Add this if your API requires it
                },
                body: JSON.stringify({ conversationId, promptId }),
            });

            // Log the full response for debugging
            const responseText = await response.text();
            console.log("Raw server response:", responseText);

            // Parse the response as JSON
            let data;
            try {
                data = JSON.parse(responseText);
            } catch (e) {
                console.error("Failed to parse response as JSON:", e);
                throw new Error(`Invalid response format: ${responseText}`);
            }

            if (!response.ok) {
                console.error("Server returned error status:", response.status, data);
                throw new Error(data.error || `Server error: ${response.statusText}`);
            }

            if (!data.success) {
                console.error("API reported failure:", data);
                throw new Error(data.error || 'Failed to set conversation system prompt');
            }

            console.log("Successfully set conversation system prompt");
            return true;
        } catch (error) {
            console.error('Error setting conversation system prompt:', error);
            throw error;
        }
    }

    /**
     * Clears the system prompt for a conversation
     */
    static async clearConversationSystemPrompt(conversationId: string): Promise<boolean> {
        try {
            if (!conversationId) {
                throw new Error('Conversation ID is required');
            }

            const response = await fetch('/api/clearConversationSystemPrompt', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': localStorage.getItem('clientId') || '',
                },
                body: JSON.stringify({ conversationId }),
            });

            if (!response.ok) {
                throw new Error(`Error clearing conversation system prompt: ${response.statusText}`);
            }

            const data = await response.json();
            if (!data.success) {
                throw new Error(data.error || 'Failed to clear conversation system prompt');
            }

            return true;
        } catch (error) {
            console.error('Error clearing conversation system prompt:', error);
            throw error;
        }
    }
}