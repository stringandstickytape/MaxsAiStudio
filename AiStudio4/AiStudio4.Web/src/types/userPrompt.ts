// src/types/userPrompt.ts

export interface UserPrompt {
  guid: string;
  title: string;
  content: string;
  description: string;
  isFavorite: boolean;
  createdDate: string;
  modifiedDate: string;
  tags: string[];
  shortcut?: string;
}

export interface UserPromptFormValues {
  title: string;
  content: string;
  description: string;
  tags: string[];
  shortcut?: string;
  isFavorite?: boolean;
}
