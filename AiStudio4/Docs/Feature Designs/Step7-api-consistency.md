// AiStudio4/Docs/Feature Designs/Step7-api-consistency.md

# Step 7: API Call Consistency Design

## Background

The codebase currently uses a mix of direct `fetch`, a custom `apiClient` (Axios-based), and a `createApiRequest` utility for backend API calls. This inconsistency leads to:
- Duplicated error handling logic
- Inconsistent request/response formats
- Harder debugging and maintenance
- Difficulty in applying cross-cutting concerns (e.g., auth headers, logging)

## Goals
- **Standardize all API calls** on a single, robust pattern
- Centralize error handling, logging, and request transformation
- Make API usage more predictable and testable
- Facilitate future enhancements (e.g., retries, auth, streaming)

## Current Patterns Observed

- **Direct fetch**: Used in e.g. `coreCommands.ts`, `FileAttachment.tsx`, `usePinnedCommandsStore.ts`
- **apiClient**: Used in stores and services (e.g., `useAppearanceStore.ts`, `apiClient.ts`)
- **createApiRequest**: Used in hooks and some components (e.g., `main.tsx`, `useResourceFactory.ts`, `useThemeManagement.ts`)

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

1. **Inventory** all usages of `fetch`, direct Axios, and legacy API utilities
2. **Replace** each with `createApiRequest` (preferred) or `apiClient` (if utility function is needed)
3. **Update error handling** to use the centralized error normalization in `apiUtils.ts`
4. **Refactor stores and hooks** to expose API actions using the new pattern
5. **Test** all affected flows for regressions

## Special Cases

- **Streaming APIs**: If streaming is needed (e.g., SSE, WebSockets), document and wrap in a dedicated utility, but avoid raw `fetch` for standard REST calls
- **File uploads/downloads**: Use `apiClient` with appropriate config (see Axios docs)
- **Third-party APIs**: If calling external APIs, wrap in a similar utility for consistency

## Error Handling
- Always use `normalizeError` from `apiUtils.ts` for error reporting
- Prefer `useApiCallState` for loading/error state in React components

## Benefits
- Consistent, predictable API usage
- Centralized error/logging/interceptor logic
- Easier onboarding for new contributors
- Simplifies future refactoring (e.g., adding auth, retries)

## Risks
- Large-scale refactor may introduce regressions; mitigate with incremental migration and thorough testing
- Some edge cases (e.g., streaming, file uploads) may need special handling

## References
- [`apiClient.ts`](../../AiStudio4.Web/src/services/api/apiClient.ts)
- [`apiUtils.ts`](../../AiStudio4.Web/src/utils/apiUtils.ts)
- [`createApiRequest` usage examples](../../AiStudio4.Web/src/hooks/)

---

**Action Items:**
- [ ] Inventory all API call sites
- [ ] Replace with `createApiRequest` or `apiClient`
- [ ] Refactor error handling
- [ ] Document any exceptions

---

*This design is part of the TidyUp refactor plan, Step 7: API Call Consistency.*