// src/commands/commandRegistry.ts
// This file now serves as an adapter to maintain backward compatibility
// with existing code that uses the commandRegistry directly

import { useCommandStore, commandRegistry as storeCommandRegistry } from '@/stores/useCommandStore';
import { Command, CommandGroup } from './types';

// Re-export the adapter from the store
export const commandRegistry = storeCommandRegistry;

// For backward compatibility, export these functions directly
export function registerCommand(command: Command): void {
  commandRegistry.registerCommand(command);
}

export function registerCommandGroup(group: CommandGroup): void {
  commandRegistry.registerGroup(group);
}

// This lets us gradually migrate without breaking existing code
export default commandRegistry;