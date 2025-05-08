# Tool Association with System Prompt

## Overview
This feature allows associating tools with a system prompt. When a system prompt is selected, the associated tools are automatically activated.

## Implementation Details

### Data Model
- `SystemPrompt` model has an `associatedTools` property that stores an array of tool GUIDs

### Frontend Implementation
- When a system prompt is selected, the associated tools are automatically activated
- This is implemented in the `CommandInitializationPlugin.tsx` file

### Event Flow
1. User selects a system prompt
2. `system-prompt-selected` event is dispatched with the prompt ID
3. Event listener in `CommandInitializationPlugin` handles the event
4. If the system prompt has associated tools, they're activated

## Usage
When starting a new conversation, if there is a default system prompt with associated tools, those tools will be automatically activated.