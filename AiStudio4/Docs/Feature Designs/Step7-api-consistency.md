// AiStudio4/Docs/Feature Designs/Step7-api-consistency.md

# Step 7: API Call Consistency Design

## Background

The codebase previously used a mix of direct `fetch`, a custom `apiClient` (Axios-based), and a `createApiRequest` utility for backend API calls. This inconsistency led to:
- Duplicated error handling logic
- Inconsistent request/response formats
- Harder debugging and maintenance
- Difficulty in applying cross-cutting concerns (e.g., auth headers, logging)

## Goals
- **Standardize all API calls** on a single, robust pattern
- Centralize error handling, logging, and request transformation
- Make API usage more predictable and testable
- Facilitate future enhancements (e.g., retries, auth, streaming)

## Migration Progress (as of 2025-04)

- **createApiRequest**: Now used in the majority of hooks and components (e.g., `main.tsx`, `useResourceFactory.ts`, `useThemeManagement.ts`, `useChatManagement.ts`, `useResourceManagement.ts`, `useSystemPromptSelection.ts`, `useThemeManagement.ts`, `useToolsManagement.ts`, `useUserPromptManagement.ts`, `useMcpServerStore.ts`).
- **apiClient**: Used in stores and services (e.g., `useAppearanceStore.ts`, `useHistoricalConvsStore.ts`, `apiClient.ts`).
- **Direct fetch**: Still present in a few locations: `coreCommands.ts`, `FileAttachment.tsx`, `usePinnedCommandsStore.ts`. These are candidates for further migration.

## Current Patterns Observed

- **Direct fetch**: Remaining in a few files (see above).
- **apiClient**: Used in stores and services.
- **createApiRequest**: Used in hooks and most components.

## Preferred Pattern

**All API calls should use the `createApiRequest` utility, which wraps the shared `apiClient` instance.**

- `apiClient` (Axios) handles base config, interceptors, and headers
- `createApiRequest` provides a typed, ergonomic, and consistent interface for making requests
- Direct `fetch` should be eliminated except for extremely low-level or browser-specific cases

### Example

```ts
// Good (preferred):
const saveSettings = createApiRequest('/api/saveSettings', 'POST');
const result = await saveSettings({ theme: 'dark' });

// Acceptable (for shared logic):
import { apiClient } from '@/services/api/apiClient';
const resp = await apiClient.post('/api/endpoint', params);

// Not allowed:
const resp = await fetch('/api/endpoint', ...);
```

## Migration Steps

1. **Inventory** all usages of `fetch`, direct Axios, and legacy API utilities (**Done**; see below for remaining exceptions)
2. **Replace** each with `createApiRequest` (preferred) or `apiClient` (if utility function is needed) (**Mostly complete**; a few fetch calls remain)
3. **Update error handling** to use the centralized error normalization in `apiUtils.ts` (**Complete in migrated code**)
4. **Refactor stores and hooks** to expose API actions using the new pattern (**Complete for all major stores and hooks**)
5. **Test** all affected flows for regressions (**Ongoing; no major regressions found**)

### Remaining Direct `fetch` Usages (as of 2025-04)
- `AiStudio4.Web/src/commands/coreCommands.ts`
- `AiStudio4.Web/src/components/FileAttachment.tsx`
- `AiStudio4.Web/src/stores/usePinnedCommandsStore.ts`

These should be migrated to `createApiRequest` or `apiClient` unless there is a technical reason not to.

## Special Cases and Exceptions

- **Streaming APIs**: WebSocket-based streaming is handled via `WebSocketService`. No raw `fetch` is used for streaming REST endpoints.
- **File uploads/downloads**: Some direct `fetch` or custom logic may remain for file upload/download endpoints, but these should be migrated to use `apiClient` with appropriate Axios config for consistency and error handling.
- **Third-party APIs**: If calling external APIs, wrap in a similar utility for consistency.
- **Low-level browser APIs**: In rare cases (e.g., clipboard, browser-specific endpoints), direct `fetch` may be justified, but should be documented inline.

## Error Handling
- Always use `normalizeError` from `apiUtils.ts` for error reporting
- Prefer `useApiCallState` for loading/error state in React components
- All new API code should use these patterns; legacy error handling should be refactored as encountered.

## Lessons Learned & Best Practices
- **Centralize all API logic**: Use `createApiRequest` for all standard REST calls; use `apiClient` for advanced/utility cases.
- **Avoid direct fetch**: Only use direct `fetch` for browser APIs or when absolutely necessary, and document the reason.
- **Consistent error handling**: Always normalize errors and surface them via React state or store.
- **Test incrementally**: Migrate and test in small batches to avoid regressions.
- **Document exceptions**: Any remaining direct `fetch` or nonstandard calls should be documented inline and in this file.

## Benefits
- Consistent, predictable API usage
- Centralized error/logging/interceptor logic
- Easier onboarding for new contributors
- Simplifies future refactoring (e.g., adding auth, retries)

## Risks
- Large-scale refactor may introduce regressions; mitigate with incremental migration and thorough testing
- Some edge cases (e.g., streaming, file uploads) may need special handling
- Legacy code may still contain direct `fetch` or inconsistent patterns; ongoing vigilance required

## References
- [`apiClient.ts`](../../AiStudio4.Web/src/services/api/apiClient.ts)
- [`apiUtils.ts`](../../AiStudio4.Web/src/utils/apiUtils.ts)
- [`createApiRequest` usage examples](../../AiStudio4.Web/src/hooks/)

---

**Action Items:**
- [x] Inventory all API call sites
- [x] Replace with `createApiRequest` or `apiClient` (all direct `fetch` calls migrated as of 2025-04-29)
- [x] Refactor error handling
- [x] Document any exceptions
- [x] Complete migration of remaining direct `fetch` calls

---

**Step 7 Status: Complete**

All direct `fetch` calls have been migrated to use `createApiRequest` or, where appropriate, `apiClient`. The codebase now follows the intended API call consistency pattern:
- `createApiRequest` is used in hooks and components for all standard REST calls.
- `apiClient` is used in shared logic and utility functions (e.g., for dynamic imports or advanced cases).
- No legacy or direct `fetch` patterns remain in the codebase.

If new API endpoints are added, follow the patterns described above. Any exceptions should be documented inline and in this file.

*This design is part of the TidyUp refactor plan, Step 7: API Call Consistency.*