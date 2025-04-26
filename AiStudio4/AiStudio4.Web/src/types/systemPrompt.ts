export interface SystemPrompt {
  guid: string;
  title: string;
  content: string;
  description: string;
  isDefault: boolean;
  createdDate: string;
  modifiedDate: string;
  tags: string[];
  associatedTools: string[]; // Tool GUIDs
}

export interface SystemPromptFormValues {
  title: string;
  content: string;
  description: string;
  tags: string[];
  isDefault?: boolean;
  associatedTools: string[]; // Tool GUIDs
}