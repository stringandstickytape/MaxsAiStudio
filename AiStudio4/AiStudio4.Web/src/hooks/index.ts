// src/hooks/index.ts

// Resource factories
export { createResourceHook } from './useResourceFactory';
export { createEnhancedResourceHook, type EnhancedResourceFactoryOptions, ResourceOperation } from './useEnhancedResourceFactory';

// Resource hooks
export { useModelManagement } from './useModelManagement';
export { useSystemPromptManagement } from './useSystemPromptManagement';
export { useToolsManagement } from './useToolsManagement';
export { useChatManagement } from './useChatManagement';

// Other hooks
export { useApiCallState } from '@/utils/apiUtils';
export { useInitialization, useInitializeIfEmpty, useAfterInitialization } from '@/utils/hookUtils';
export { useResourceInitialization, useAsyncAction } from '@/utils/hookLifecycle';
export { useWebSocket } from './useWebSocket';
export { useMediaQuery } from './use-media-query';
export { useMessageGraph } from './useMessageGraph';
export { useVoiceInput } from './useVoiceInput';
export { useStreamTokens } from './useStreamTokens';
export { useToolCommands } from './useToolCommands';

// Hook types
export type { HookState } from '@/utils/hookLifecycle';
