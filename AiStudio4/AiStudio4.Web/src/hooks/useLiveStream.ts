import { useStreamTokens } from './useStreamTokens';

// This is a compatibility wrapper to maintain backward compatibility
// with code that uses the old useLiveStream hook
export function useLiveStream() {
    return useStreamTokens();
}