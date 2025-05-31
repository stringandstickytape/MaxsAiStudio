// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\AiStudioClient\src\commands\voiceInputCommand.ts
import { useCommandStore } from '@/stores/useCommandStore';
import { useVoiceInputStore } from '@/stores/useVoiceInputStore'; // Added
import { Mic } from 'lucide-react';
import React from 'react'; // React import is needed for JSX in icon

// Removed old state variables: voiceInputOpen, setVoiceInputOpen, setVoiceInputCallback

export function initializeVoiceInputCommand() {
  useCommandStore.getState().registerCommand({
    id: 'start-voice-input',
    name: 'Voice Input',
    description: 'Use your microphone to speak instead of typing',
    // Shortcut remains, but its direct effect is now through the store
    shortcut: navigator.platform.indexOf('Mac') !== -1 ? '?+V' : 'Alt+V',
    keywords: [
      'voice',
      'speech',
      'microphone',
      'dictate',
      'speak',
      'mic',
      'audio',
      'talk',
      'record',
      'sound',
      'recognition',
      'dictation',
      'transcribe',
      'hands-free',
      'accessibility',
    ],
    section: 'utility',
    icon: React.createElement(Mic, { size: 16 }),
    execute: () => {
      // New execution: simply call the store's action
      useVoiceInputStore.getState().startListening();
    },
  });
}

// Removed setupVoiceInputKeyboardShortcut function.
// The keyboard shortcut is defined in the command registration.
// Global event listeners for shortcuts are typically handled by a dedicated command execution system or a global keydown listener that dispatches commands.
// If the shortcut defined in `registerCommand` is not automatically handled by a global system,
// a separate global shortcut handler might be needed, but it would dispatch the command 'start-voice-input'
// rather than directly manipulating state as the old setupVoiceInputKeyboardShortcut did.
// For now, assuming the command system handles its registered shortcuts.