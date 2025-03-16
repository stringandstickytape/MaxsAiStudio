
import { useCommandStore } from '@/stores/useCommandStore';
import { Mic } from 'lucide-react';
import React, { useState, useEffect, useCallback } from 'react';

let voiceInputOpen = false;
let setVoiceInputOpen: ((isOpen: boolean) => void) | null = null;
let setVoiceInputCallback: ((callback: (text: string) => void) => void) | null = null;

export function initializeVoiceInputCommand() {
  useCommandStore.getState().registerCommand({
    id: 'start-voice-input',
    name: 'Voice Input',
    description: 'Use your microphone to speak instead of typing',
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
      if (setVoiceInputOpen) {
        voiceInputOpen = true;
        setVoiceInputOpen(true);
      }
    },
  });
}

export function useVoiceInputState(inputCallback: (text: string) => void) {
  const [isOpen, setIsOpen] = useState(voiceInputOpen);

  useEffect(() => {
    setVoiceInputOpen = setIsOpen;
    setVoiceInputCallback = (callback) => {
      inputCallback = callback;
    };

    return () => {
      setVoiceInputOpen = setVoiceInputOpen === setIsOpen ? null : setVoiceInputOpen;
      setVoiceInputCallback = null;
    };
  }, [inputCallback]);

  const handleTranscript = useCallback(
    (text: string) => {
      inputCallback(text);
    },
    [inputCallback],
  );

  return {
    isVoiceInputOpen: isOpen,
    setVoiceInputOpen: setIsOpen,
    handleTranscript,
  };
}

export function setupVoiceInputKeyboardShortcut() {
  const handleKeyDown = (e: KeyboardEvent) => {
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

