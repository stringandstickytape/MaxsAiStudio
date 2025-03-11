// src/hooks/useVoiceInput.tsx
import { useState, useCallback, useEffect, useRef } from 'react';

// Types for Web Speech API
interface SpeechRecognition extends EventTarget {
  continuous: boolean;
  interimResults: boolean;
  lang: string;
  start: () => void;
  stop: () => void;
  abort: () => void;
  onresult: (event: SpeechRecognitionEvent) => void;
  onerror: (event: SpeechRecognitionError) => void;
  onend: () => void;
  onstart: () => void;
  onspeechend?: () => void;
}

interface SpeechRecognitionEvent {
  resultIndex: number;
  results: SpeechRecognitionResultList;
}

interface SpeechRecognitionResultList {
  length: number;
  item(index: number): SpeechRecognitionResult;
  [index: number]: SpeechRecognitionResult;
}

interface SpeechRecognitionResult {
  isFinal: boolean;
  length: number;
  item(index: number): SpeechRecognitionAlternative;
  [index: number]: SpeechRecognitionResult;
}

interface SpeechRecognitionAlternative {
  transcript: string;
  confidence: number;
}

interface SpeechRecognitionError extends Event {
  error: string;
  message: string;
}

// Initialize SpeechRecognition with browser prefixes
const SpeechRecognitionAPI =
  typeof window !== 'undefined'
    ? window.SpeechRecognition ||
      (window as any).webkitSpeechRecognition ||
      (window as any).mozSpeechRecognition ||
      (window as any).msSpeechRecognition
    : null;

export function useVoiceInput() {
  const [isListening, setIsListening] = useState(false);
  const [transcript, setTranscript] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSupported, setIsSupported] = useState(false);

  // Reference to the recognition instance
  const recognitionRef = useRef<SpeechRecognition | null>(null);

  // Refs to track final and interim results
  const finalTranscriptRef = useRef('');
  const interimTranscriptRef = useRef('');

  // Initialize on mount
  useEffect(() => {
    setIsSupported(!!SpeechRecognitionAPI);

    return () => {
      // Cleanup on unmount
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (err) {
          // Ignore errors on cleanup
        }
      }
    };
  }, []);

  // Start listening
  const startListening = useCallback(() => {
    if (!isSupported || isListening) return;

    try {
      // Reset transcript
      finalTranscriptRef.current = '';
      interimTranscriptRef.current = '';
      setTranscript('');

      // Cleanup any existing instance
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (err) {
          // Ignore
        }
      }

      // Create a new instance
      const recognition = new SpeechRecognitionAPI() as SpeechRecognition;
      recognitionRef.current = recognition;

      // Configure
      recognition.continuous = false; // Set to false to prevent auto-restart
      recognition.interimResults = true;
      recognition.lang = 'en-US';

      // Set up handlers
      recognition.onstart = () => {
        setIsListening(true);
        setError(null);
      };

      recognition.onresult = (event) => {
        interimTranscriptRef.current = '';

        for (let i = event.resultIndex; i < event.results.length; i++) {
          const result = event.results[i];
          const text = result[0].transcript;

          if (result.isFinal) {
            finalTranscriptRef.current += text + ' ';
          } else {
            interimTranscriptRef.current += text + ' ';
          }
        }

        // Update the displayed transcript
        setTranscript((finalTranscriptRef.current + interimTranscriptRef.current).trim());
      };

      recognition.onend = () => {
        // Add any remaining interim text to final
        if (interimTranscriptRef.current) {
          finalTranscriptRef.current += interimTranscriptRef.current;
          interimTranscriptRef.current = '';
        }

        // Update the final transcript display
        setTranscript(finalTranscriptRef.current.trim());
        setIsListening(false);
      };

      recognition.onerror = (event) => {
        if (event.error !== 'no-speech' && event.error !== 'aborted') {
          console.warn('Recognition error:', event.error);
          setError(`Speech recognition error: ${event.error}`);
        }
      };

      // Start recognition
      recognition.start();
    } catch (err) {
      console.error('Failed to start speech recognition:', err);
      setError('Failed to start speech recognition');
      setIsListening(false);
    }
  }, [isSupported, isListening]);

  // Continue listening (keeps existing transcript)
  const continueListening = useCallback(() => {
    if (!isSupported || isListening) return;

    try {
      // Keep existing final transcript
      interimTranscriptRef.current = '';

      // Cleanup any existing instance
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (err) {
          // Ignore
        }
      }

      // Create a new instance
      const recognition = new SpeechRecognitionAPI() as SpeechRecognition;
      recognitionRef.current = recognition;

      // Configure
      recognition.continuous = false;
      recognition.interimResults = true;
      recognition.lang = 'en-US';

      // Set up handlers
      recognition.onstart = () => {
        setIsListening(true);
        setError(null);
      };

      recognition.onresult = (event) => {
        interimTranscriptRef.current = '';

        for (let i = event.resultIndex; i < event.results.length; i++) {
          const result = event.results[i];
          const text = result[0].transcript;

          if (result.isFinal) {
            finalTranscriptRef.current += text + ' ';
          } else {
            interimTranscriptRef.current += text + ' ';
          }
        }

        // Update the displayed transcript
        setTranscript((finalTranscriptRef.current + interimTranscriptRef.current).trim());
      };

      recognition.onend = () => {
        // Add any remaining interim text to final
        if (interimTranscriptRef.current) {
          finalTranscriptRef.current += interimTranscriptRef.current;
          interimTranscriptRef.current = '';
        }

        // Update the final transcript display
        setTranscript(finalTranscriptRef.current.trim());
        setIsListening(false);
      };

      recognition.onerror = (event) => {
        if (event.error !== 'no-speech' && event.error !== 'aborted') {
          console.warn('Recognition error:', event.error);
          setError(`Speech recognition error: ${event.error}`);
        }
      };

      // Start recognition
      recognition.start();
    } catch (err) {
      console.error('Failed to continue speech recognition:', err);
      setError('Failed to continue speech recognition');
      setIsListening(false);
    }
  }, [isSupported, isListening]);

  // Stop listening
  const stopListening = useCallback(() => {
    if (recognitionRef.current) {
      try {
        recognitionRef.current.stop();
      } catch (err) {
        console.error('Error stopping recognition:', err);
      }
    }
    setIsListening(false);
  }, []);

  // Reset transcript
  const resetTranscript = useCallback(() => {
    finalTranscriptRef.current = '';
    interimTranscriptRef.current = '';
    setTranscript('');
  }, []);

  return {
    isListening,
    transcript,
    error,
    startListening,
    stopListening,
    resetTranscript,
    continueListening,
    isSupported,
  };
}
