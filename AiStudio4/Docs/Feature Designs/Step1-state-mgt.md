# State Management Improvement Plan

## 1. Current State Analysis

After analyzing the codebase, we've identified several inconsistencies in how state is managed across the application:

### 1.1 Direct `getState()` Usage

Many files directly access Zustand store state using `Store.getState()` instead of React hooks:

- Command files: `coreCommands.ts`, `settingsCommands.ts`, `systemPromptCommands.ts`, `themeCommands.ts`, etc.
- Components: `CommandInitializer.tsx`, `App.tsx`, `InputBar.tsx`, etc.

Example from `coreCommands.ts`:
```typescript
const { createConv } = useConvStore.getState();
const { registerGroup } = useCommandStore.getState();
```

### 1.2 Global State via Window/localStorage

The application uses global state through `window` and `localStorage` for cross-component communication:

- `window.dispatchEvent` and custom events in `settingsCommands.ts`, `systemPromptCommands.ts`
- Global functions like `window.setPrompt` in `userPromptCommands.ts`
- Direct localStorage access in multiple components

### 1.3 Current Store Structure

The application has numerous Zustand stores:

- `useCommandStore`
- `useConvStore`
- `useModalStore`
- `useSystemPromptStore`
- `useUserPromptStore`
- `useToolStore`
- `useThemeStore`
- `usePanelStore`
- `useWebSocketStore`
- And others...

## 2. Best Practices for Zustand in React

### 2.1 Hook-based Access in Components

Zustand is designed to be used with hooks in React components:

```typescript
// Preferred approach
function MyComponent() {
  const { someState, someAction } = useMyStore();
  // Use state and actions directly
}
```

### 2.2 Accessing Store Outside React

For non-React contexts (like utility functions, command handlers), Zustand provides `getState()` and a subscription API:

```typescript
// For one-time state access
const currentState = useMyStore.getState();

// For reactive state outside React
const unsubscribe = useMyStore.subscribe(
  state => state.someValue,
  newValue => console.log(newValue)
);
```

### 2.3 Store Organization

Zustand best practices include:

- Separating state and actions clearly
- Using selectors for derived state
- Keeping stores focused on specific domains
- Using middleware for complex state logic (immer, persist, etc.)

## 3. Refactoring Recommendations

### 3.1 Command Handlers Refactoring

Command files are a primary source of direct `getState()` calls. We should refactor these to:

1. Create a command registry service that doesn't rely on direct store access
2. Move command execution logic into store actions where possible
3. Use a middleware pattern for commands that need to interact with multiple stores

### 3.2 Component Refactoring

For React components:

1. Replace all direct `getState()` calls with hook usage
2. Use selectors to optimize rendering
3. Split complex components to minimize state dependencies

### 3.3 Global State Elimination

To eliminate global state:

1. Replace `window.dispatchEvent` with store actions
2. Create dedicated stores for cross-cutting concerns
3. Use React Context for deeply nested component communication
4. Move `localStorage` persistence into Zustand middleware

## 4. Implementation Plan

### 4.1 Phase 1: Command Handler Refactoring

#### Step 1: Create Command Registry Service

Create a dedicated service for command registration that doesn't rely on direct store access:

```typescript
// src/services/commandRegistry.ts
import { useCommandStore } from '@/stores/useCommandStore';
import type { Command, CommandGroup } from '@/commands/types';

export const commandRegistry = {
  registerCommand: (command: Command) => {
    useCommandStore.getState().registerCommand(command);
  },
  registerGroup: (group: CommandGroup) => {
    useCommandStore.getState().registerGroup(group);
  },
  // Other methods...
};
```

#### Step 2: Refactor Command Files

Refactor command files to use the registry service:

```typescript
// Before
import { useCommandStore } from '@/stores/useCommandStore';

export function initializeCoreCommands() {
  const { registerGroup } = useCommandStore.getState();
  // ...
}

// After
import { commandRegistry } from '@/services/commandRegistry';

export function initializeCoreCommands() {
  const { registerGroup } = commandRegistry;
  // ...
}
```

#### Step 3: Move Command Logic to Store Actions

Move command execution logic into store actions:

```typescript
// In useConvStore.ts
export const useConvStore = create<ConvState>((set, get) => ({
  // ... existing state
  
  // Add new action
  createNewConversation: () => {
    const convId = `conv_${uuidv4()}`;
    const messageId = `msg_${Date.now()}`;
    set(state => ({
      // Create new conversation logic
    }));
    return convId;
  },
}));

// In command file
import { commandRegistry } from '@/services/commandRegistry';
import { useConvStore } from '@/stores/useConvStore';

export function initializeCoreCommands() {
  commandRegistry.registerCommand({
    id: 'new-conv',
    // ...
    execute: () => {
      useConvStore.getState().createNewConversation();
    },
  });
}
```

### 4.2 Phase 2: Component Refactoring

#### Step 1: Identify Components Using Direct getState()

Components to refactor include:
- `App.tsx`
- `CommandInitializationPlugin.tsx`
- `InputBar.tsx`
- `ConvTreeView.tsx`
- And others...

#### Step 2: Refactor Components

Refactor each component to use hooks:

```typescript
// Before
function MyComponent() {
  const handleAction = () => {
    useModalStore.getState().openModal('systemPrompt');
  };
  // ...
}

// After
function MyComponent() {
  const { openModal } = useModalStore();
  
  const handleAction = () => {
    openModal('systemPrompt');
  };
  // ...
}
```

#### Step 3: Create Custom Hooks for Complex State Logic

For components with complex state interactions, create custom hooks:

```typescript
// src/hooks/useSystemPromptActions.ts
export function useSystemPromptActions() {
  const { prompts, defaultPromptId, convPrompts } = useSystemPromptStore();
  const { activeConvId } = useConvStore();
  
  const getCurrentPrompt = useCallback(() => {
    if (!activeConvId) return null;
    const promptId = convPrompts[activeConvId];
    return promptId ? prompts.find(p => p.guid === promptId) : null;
  }, [prompts, convPrompts, activeConvId]);
  
  // Other actions...
  
  return { getCurrentPrompt };
}
```

### 4.3 Phase 3: Global State Elimination

#### Step 1: Create Event Bus Store

Replace window events with a dedicated event bus store:

```typescript
// src/stores/useEventBusStore.ts
import { create } from 'zustand';

type EventHandler = (data: any) => void;

interface EventBusState {
  listeners: Record<string, EventHandler[]>;
  emit: (event: string, data: any) => void;
  on: (event: string, handler: EventHandler) => () => void;
  off: (event: string, handler: EventHandler) => void;
}

export const useEventBusStore = create<EventBusState>((set, get) => ({
  listeners: {},
  
  emit: (event, data) => {
    const { listeners } = get();
    if (listeners[event]) {
      listeners[event].forEach(handler => handler(data));
    }
  },
  
  on: (event, handler) => {
    set(state => ({
      listeners: {
        ...state.listeners,
        [event]: [...(state.listeners[event] || []), handler],
      },
    }));
    
    return () => get().off(event, handler);
  },
  
  off: (event, handler) => {
    set(state => ({
      listeners: {
        ...state.listeners,
        [event]: (state.listeners[event] || []).filter(h => h !== handler),
      },
    }));
  },
}));
```

#### Step 2: Replace Window Events

Replace window events with the event bus:

```typescript
// Before
window.dispatchEvent(new CustomEvent('open-system-prompt-library'));
window.addEventListener('system-prompt-selected', handleSystemPromptSelected);

// After
import { useEventBusStore } from '@/stores/useEventBusStore';

useEventBusStore.getState().emit('open-system-prompt-library', {});

const unsubscribe = useEventBusStore.getState().on(
  'system-prompt-selected', 
  handleSystemPromptSelected
);
// Remember to call unsubscribe when component unmounts
```

#### Step 3: Add Persistence Middleware

Replace direct localStorage access with Zustand middleware:

```typescript
// src/stores/useAppearanceStore.ts
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export const useAppearanceStore = create(
  persist(
    (set) => ({
      fontSize: 14,
      setFontSize: (size: number) => set({ fontSize: size }),
      // Other state and actions...
    }),
    {
      name: 'appearance-storage',
      getStorage: () => localStorage,
    }
  )
);
```

## 5. Testing Strategy

### 5.1 Unit Tests

- Create unit tests for each refactored store
- Test store actions in isolation
- Mock dependencies for pure unit testing

### 5.2 Integration Tests

- Test command registration and execution
- Verify event bus communication
- Test persistence and hydration

### 5.3 Component Tests

- Test components with refactored state access
- Verify correct rendering based on state changes
- Test interactions between components and stores

## 6. Migration Strategy

To minimize disruption, we'll implement these changes incrementally:

1. Create the command registry service first
2. Refactor one command file at a time
3. Create the event bus store
4. Refactor one component at a time
5. Add persistence middleware to stores
6. Remove global state access

This approach allows us to validate each change before moving to the next, reducing the risk of regressions.

## 7. Expected Benefits

- **Improved Testability**: Components and logic will be easier to test in isolation
- **Better Performance**: Optimized rendering with proper hook usage
- **Enhanced Maintainability**: Consistent patterns make the codebase easier to understand
- **Reduced Bugs**: Eliminating global state reduces the chance of unexpected interactions
- **Easier Onboarding**: New developers will find the codebase more approachable

## 8. Conclusion

This refactoring plan addresses the inconsistent state management in the AiStudio4 web application. By implementing these changes, we'll create a more maintainable, testable, and performant application that follows React and Zustand best practices.

The plan is designed to be implemented incrementally, allowing for validation at each step and minimizing disruption to ongoing development.