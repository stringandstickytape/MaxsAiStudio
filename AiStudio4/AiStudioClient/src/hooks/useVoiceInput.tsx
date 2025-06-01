// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\AiStudioClient\src\hooks\useVoiceInput.tsx
import { useState, useCallback, useEffect, useRef } from 'react';
import { useVoiceInputStore } from '../stores/useVoiceInputStore';

// Types based on the Web Speech API, might need adjustments based on browser specifics
interface SpeechRecognitionErrorEvent extends Event {
  error: string;
  message: string;
}

interface SpeechRecognitionAlternative {
  transcript: string;
  confidence: number;
}

interface SpeechRecognitionResult {
  isFinal: boolean;
  readonly length: number;
  item(index: number): SpeechRecognitionAlternative;
  [index: number]: SpeechRecognitionAlternative; // Make it directly indexable
}

interface SpeechRecognitionResultList {
  readonly length: number;
  item(index: number): SpeechRecognitionResult;
  [index: number]: SpeechRecognitionResult;
}

interface SpeechRecognitionEvent extends Event {
  readonly resultIndex: number;
  readonly results: SpeechRecognitionResultList;
}

interface SpeechRecognition extends EventTarget {
  continuous: boolean;
  interimResults: boolean;
  lang: string;
  start: () => void;
  stop: () => void;
  abort: () => void;
  onresult: ((this: SpeechRecognition, ev: SpeechRecognitionEvent) => any) | null;
  onerror: ((this: SpeechRecognition, ev: SpeechRecognitionErrorEvent) => any) | null; // Adjusted type
  onend: ((this: SpeechRecognition, ev: Event) => any) | null;
  onstart: ((this: SpeechRecognition, ev: Event) => any) | null;
  // onspeechend?: () => void; // Optional, not used in this design
}

const SpeechRecognitionAPI = typeof window !== 'undefined' ? 
  (window.SpeechRecognition || 
  (window as any).webkitSpeechRecognition || 
  (window as any).mozSpeechRecognition || 
  (window as any).msSpeechRecognition) as { new(): SpeechRecognition } | null // Added constructor type
  : null;

interface UseVoiceInputProps {
  onTranscriptFinalized: (text: string) => void;
}

export function useVoiceInput(props: UseVoiceInputProps) {
  const [isSupported, setIsSupported] = useState(false);
  const recognitionRef = useRef<SpeechRecognition | null>(null);
  // finalTranscriptRef is used to accumulate parts of a single utterance if it comes in multiple 'final' events (though rare for continuous=false)
  // or more importantly, to be cleared by resetTranscript.
  const finalTranscriptRef = useRef(''); 

  useEffect(() => {
    setIsSupported(!!SpeechRecognitionAPI);
    return () => {
      if (recognitionRef.current) {
        try {
          recognitionRef.current.abort(); // Use abort for cleanup
        } catch (e) {
          console.warn("Error aborting speech recognition on unmount:", e);
        }
        recognitionRef.current = null;
      }
    };
  }, []);

  const startMicCapture = useCallback(() => {
    if (!isSupported || !SpeechRecognitionAPI) {
      useVoiceInputStore.getState().setError("Speech recognition not supported by your browser.");
      useVoiceInputStore.getState()._setIsListening(false); // Ensure store reflects not listening
      return;
    }

    // finalTranscriptRef.current should be cleared by resetTranscript before this is called by InputBar
    // finalTranscriptRef.current = ''; 

    try {
      // Ensure any existing recognition is stopped before starting a new one.
      if (recognitionRef.current) {
        recognitionRef.current.stop();
      }
    } catch (e) {
      console.warn("Error stopping previous recognition instance:", e);
      // If stopping fails, nullify ref to allow new instance creation
      recognitionRef.current = null;
    }
    
    const recognition = new SpeechRecognitionAPI();
    recognitionRef.current = recognition;
    
    recognition.continuous = false; // Per design: "capture one phrase then stop"
    recognition.interimResults = true; // Can be true, doesn't affect final result logic
    recognition.lang = 'en-US'; // Consider making this configurable later
    
    recognition.onstart = () => {
      useVoiceInputStore.getState()._setIsListening(true);
      useVoiceInputStore.getState().setError(null); // Clear previous errors
    };

    recognition.onresult = (event: SpeechRecognitionEvent) => {
      let finalizedTextSegment = '';
      for (let i = event.resultIndex; i < event.results.length; ++i) {
        if (event.results[i].isFinal) {
          finalizedTextSegment += event.results[i][0].transcript;
        }
      }
      
      if (finalizedTextSegment) {
        props.onTranscriptFinalized(finalizedTextSegment.trim());
        // Automatically stop after a final transcript is processed.
        // This internal stop will trigger recognition.onend(), which then updates the store.
        if (recognitionRef.current) {
          try {
            recognitionRef.current.stop();
          } catch (e) {
            console.error("Error stopping recognition in onresult:", e);
            // Fallback to ensure store state is correct if .stop() fails to trigger onend
            useVoiceInputStore.getState()._setIsListening(false);
          }
        }
        finalTranscriptRef.current = ''; // Clear after processing, per design
      }
    };

    recognition.onend = () => {
      useVoiceInputStore.getState()._setIsListening(false);
      // finalTranscriptRef.current = ''; // Ensure clean, though onresult should handle it for its flow.
                                      // resetTranscript is the primary clearer for external calls.
    };

    recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
      // Don't report 'no-speech' or 'aborted' as errors unless desired
      if (event.error !== 'no-speech' && event.error !== 'aborted') {
        console.warn('Speech recognition error:', event.error, event.message);
        useVoiceInputStore.getState().setError(`Speech error: ${event.error} - ${event.message}`);
      }
      useVoiceInputStore.getState()._setIsListening(false); // Critical: ensure listening is set to false
    };
    
    try {
      recognition.start();
    } catch (err: any) { // Catching potential errors during start
      console.error("Failed to start speech recognition:", err);
      useVoiceInputStore.getState().setError(`Failed to start speech recognition: ${err.message || err}`);
      useVoiceInputStore.getState()._setIsListening(false);
      if (recognitionRef.current) {
        try { recognitionRef.current.abort(); } catch {} // Abort if start failed badly
        recognitionRef.current = null;
      }
    }
  }, [isSupported, props.onTranscriptFinalized]);

  const stopMicCapture = useCallback(() => {
    if (recognitionRef.current) {
      try {
        recognitionRef.current.stop();
      } catch (err) {
        console.error('Error stopping recognition via stopMicCapture:', err);
        // If .stop() fails, manually ensure the store state is updated as a fallback.
        useVoiceInputStore.getState()._setIsListening(false);
      }
    }
    // Note: recognition.onend() should handle setting _setIsListening(false) in the store.
    // This function just initiates the stop.
  }, []);
  
  const resetTranscript = useCallback(() => {
    finalTranscriptRef.current = '';
    // No need to manage interimTranscriptRef or setTranscript state here anymore
  }, []);

  return {
    isSupported,
    // error is now managed in the store, accessed by consumer if needed
    // isListening is now managed in the store, accessed by consumer
    // transcript is no longer managed by this hook directly
    startMicCapture,
    stopMicCapture,
    resetTranscript,
    // continueListening removed as per new design
  };
}
