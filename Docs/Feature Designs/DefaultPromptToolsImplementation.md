# Default Prompt Tools Implementation

## Problem

When the application starts or when a new chat is created, the default system prompt is loaded correctly, but the tools associated with that prompt are not activated. This creates an inconsistent experience where manually selecting the same prompt after startup works correctly (activating the associated tools), but the initial state doesn't have the tools enabled.

## Solution

Implement two key changes to ensure tools associated with the default system prompt are properly activated:

1. **On Application Startup**: Load the default system prompt and activate its associated tools when the application first loads.

2. **On New Chat Creation**: When a new conversation is created, apply the default system prompt's associated tools to that conversation.

## Implementation Details

### 1. Application Startup

Added code to `main.tsx` to load the default system prompt and its associated tools during application initialization:

```typescript
// Function to load the default system prompt and its associated tools
async function loadDefaultSystemPromptAndTools() {
  try {
    // Get the default system prompt
    const defaultPromptResp = await createApiRequest('/api/getDefaultSystemPrompt', 'POST')({});
    if (defaultPromptResp.success && defaultPromptResp.prompt) {
      const defaultPrompt = defaultPromptResp.prompt;
      console.log('Loaded default system prompt:', defaultPrompt.title);
      
      // Set the default prompt in the store
      useSystemPromptStore.getState().setDefaultPromptId(defaultPrompt.guid);
      
      // Apply associated tools if available
      if (defaultPrompt.associatedTools && defaultPrompt.associatedTools.length > 0) {
        useToolStore.getState().setActiveTools(defaultPrompt.associatedTools);
        console.log(`Applied ${defaultPrompt.associatedTools.length} tools from default system prompt`);
      }
    }
  } catch (err) {
    console.error('Failed to load default system prompt and tools at startup:', err);
  }
}
```

This function is called during the application initialization process, before the React app is rendered.

### 2. New Chat Creation

Modified `CommandInitializationPlugin.tsx` to apply the default system prompt's tools when a new conversation is created:

```typescript
// Apply default system prompt tools when a new conversation is created
useEffect(() => {
  if (activeConvId) {
    // Get the default system prompt
    const applyDefaultPromptTools = async () => {
      try {
        const response = await createApiRequest('/api/getDefaultSystemPrompt', 'POST')({});
        if (response.success && response.prompt) {
          const defaultPrompt = response.prompt;
          
          // Apply associated tools if available
          if (defaultPrompt.associatedTools && defaultPrompt.associatedTools.length > 0) {
            setActiveTools(defaultPrompt.associatedTools);
            console.log(`Applied ${defaultPrompt.associatedTools.length} tools from default system prompt for new conversation`);
          }
        }
      } catch (err) {
        console.error('Failed to apply default prompt tools for new conversation:', err);
      }
    };
    
    applyDefaultPromptTools();
  }
}, [activeConvId, setActiveTools]);
```

This effect runs whenever the active conversation ID changes, ensuring that when a new conversation is created, the default prompt's tools are applied.

## Benefits

- Consistent user experience: Tools associated with the default system prompt are always activated, whether at startup, when creating a new chat, or when manually selecting a prompt.
- Improved workflow: Users don't need to manually select tools that should be available by default.
- Maintains the existing system prompt association feature while fixing edge cases.

## Testing

1. **Startup Test**: Launch the application and verify that the default system prompt's tools are activated.
2. **New Chat Test**: Create a new chat and verify that the default system prompt's tools are activated.
3. **Manual Selection Test**: Verify that manually selecting a system prompt still correctly activates its associated tools.