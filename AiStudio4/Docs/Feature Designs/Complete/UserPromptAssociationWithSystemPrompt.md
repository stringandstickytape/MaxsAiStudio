# User Prompt Association with System Prompt

## Overview
This feature allows associating a user prompt with a system prompt. When a system prompt is selected, the associated user prompt is automatically applied.

## Implementation Details

### Data Model
- `SystemPrompt` model has an `associatedUserPromptId` property that stores the GUID of the associated user prompt

### Frontend Implementation
- When a system prompt is selected, the associated user prompt is automatically applied
- The user prompt is pre-filled in the input field
- This is implemented in the `CommandInitializationPlugin.tsx` file

### Event Flow
1. User selects a system prompt
2. `system-prompt-selected` event is dispatched with the prompt ID
3. Event listener in `CommandInitializationPlugin` handles the event
4. If the system prompt has an associated user prompt, it's applied

## Usage
When starting a new conversation, if there is a default system prompt with an associated user prompt, the user prompt will be automatically applied.