// src/commands/voiceInputCommand.ts
import { registerCommand } from './commandRegistry';
import { Mic } from 'lucide-react';
import React from 'react';

// State management for the voice input overlay
let voiceInputOpen = false;
let setVoiceInputOpen: ((isOpen: boolean) => void) | null = null;
let setVoiceInputCallback: ((callback: (text: string) => void) => void) | null = null;

export function initializeVoiceInputCommand() {
    registerCommand({
        id: 'start-voice-input',
        name: 'Voice Input',
        description: 'Use your microphone to speak instead of typing',
        shortcut: navigator.platform.indexOf('Mac') !== -1 ? '⌥+V' : 'Alt+V',
        keywords: ['voice', 'speech', 'microphone', 'dictate', 'speak', 'mic', 'audio', 'talk', 'record', 'sound', 'recognition', 'dictation', 'transcribe', 'hands-free', 'accessibility'],
        section: 'utility',
        icon: React.createElement(Mic, { size: 16 }),
        execute: () => {
            if (setVoiceInputOpen) {
                // Open the voice input overlay
                voiceInputOpen = true;
                setVoiceInputOpen(true);
            }
        }
    });
}

// Hook into the voice input state from the component
export function useVoiceInputState(
    inputCallback: (text: string) => void
) {
    const [isOpen, setIsOpen] = React.useState(voiceInputOpen);

    React.useEffect(() => {
        setVoiceInputOpen = setIsOpen;
        setVoiceInputCallback = (callback) => {
            inputCallback = callback;
        };

        return () => {
            if (setVoiceInputOpen === setIsOpen) {
                setVoiceInputOpen = null;
            }
            if (setVoiceInputCallback) {
                setVoiceInputCallback = null;
            }
        };
    }, [inputCallback]);

    const handleTranscript = React.useCallback((text: string) => {
        inputCallback(text);
    }, [inputCallback]);

    return {
        isVoiceInputOpen: isOpen,
        setVoiceInputOpen: setIsOpen,
        handleTranscript
    };
}

// Also set up a keyboard shortcut listener
export function setupVoiceInputKeyboardShortcut() {
    const handleKeyDown = (e: KeyboardEvent) => {
        // Alt+V / Option+V shortcut
        if (e.altKey && e.key.toLowerCase() === 'v') {
            e.preventDefault();
            if (setVoiceInputOpen) {
                voiceInputOpen = true;
                setVoiceInputOpen(true);
            }
        }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
}