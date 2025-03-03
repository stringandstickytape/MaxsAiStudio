// src/types/systemPrompt.ts

export interface SystemPrompt {
  guid: string;
  title: string;
  content: string;
  description: string;
  isDefault: boolean;
  createdDate: string;
  modifiedDate: string;
  tags: string[];
}

export interface SystemPromptState {
  prompts: SystemPrompt[];
  defaultPromptId: string | null;
  conversationPrompts: Record<string, string>; // conversationId -> promptId
  loading: boolean;
  error: string | null;
  currentPrompt: SystemPrompt | null; // For editing/viewing
  isLibraryOpen: boolean;
}

export interface SystemPromptFormValues {
  title: string;
  content: string;
  description: string;
  tags: string[];
  isDefault?: boolean;
}
// src/types/systemPrompt.ts

export interface SystemPrompt {
  guid: string;
  title: string;
  content: string;
  description: string;
  isDefault: boolean;
  createdDate: string;
  modifiedDate: string;
  tags: string[];
}

export interface SystemPromptState {
  prompts: SystemPrompt[];
  defaultPromptId: string | null;
  conversationPrompts: Record<string, string>; // Maps conversationId to promptId
  activePrompt: SystemPrompt | null;
  loading: boolean;
  error: string | null;
}
