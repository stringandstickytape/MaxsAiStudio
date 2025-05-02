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
  associatedUserPromptId?: string; // Associated User Prompt GUID
  primaryModelGuid?: string; // Associated primary model GUID
  secondaryModelGuid?: string; // Associated secondary model GUID
}

export interface SystemPromptFormValues {
  title: string;
  content: string;
  description: string;
  tags: string[];
  isDefault?: boolean;
  associatedTools: string[]; // Tool GUIDs
  associatedUserPromptId?: string; // Associated User Prompt GUID
  primaryModelGuid?: string; // Associated primary model GUID
  secondaryModelGuid?: string; // Associated secondary model GUID
}