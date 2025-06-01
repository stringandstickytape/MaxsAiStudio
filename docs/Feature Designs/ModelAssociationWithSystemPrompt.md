# Model Association with System Prompt

## Overview
This feature allows users to associate primary and secondary models with system prompts. When a system prompt is activated, the associated models are automatically selected.

## Implementation Details

### Data Model Changes
- Added `PrimaryModelGuid` and `SecondaryModelGuid` properties to the `SystemPrompt` class in C#
- Updated the TypeScript interface for `SystemPrompt` to include these properties
- Updated the `SystemPromptFormValues` interface to include model selection fields

### UI Changes
- Modified the `SystemPromptEditor` component to include model selection dropdowns
- Used 'none' as the default value for model selections to avoid empty string issues with the Select component

### Logic Changes
- Updated the system prompt selection logic to set the selected models when a system prompt is activated
- Added checks to ensure 'none' values are properly handled
- Modified the server-side request handlers to convert 'none' values to empty strings

### API Changes
- No new API endpoints were needed, as we reused existing model selection endpoints

## Usage
1. When editing a system prompt, users can select primary and secondary models from dropdowns
2. When a system prompt is activated, any associated models are automatically selected
3. The model selections are persisted with the system prompt

## Technical Notes
- We use 'none' as a special value in the UI to represent no selection, which is converted to empty strings in the backend
- The model selection is handled both in the React hook and in the standalone function for non-React contexts