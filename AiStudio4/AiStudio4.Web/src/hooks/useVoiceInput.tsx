// src/hooks/useVoiceInput.tsx
import { useState, useCallback, useEffect, useRef } from 'react';

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

  
  const recognitionRef = useRef<SpeechRecognition | null>(null);

  
  const finalTranscriptRef = useRef('');
  const interimTranscriptRef = useRef('');

  
  useEffect(() => {
    setIsSupported(!!SpeechRecognitionAPI);

    return () => {
      
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (err) {
          
        }
      }
    };
  }, []);

  
  const startListening = useCallback(() => {
    if (!isSupported || isListening) return;

    try {
      
      finalTranscriptRef.current = '';
      interimTranscriptRef.current = '';
      setTranscript('');

      
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (err) {
          
        }
      }

      
      const recognition = new SpeechRecognitionAPI() as SpeechRecognition;
      recognitionRef.current = recognition;

      
      recognition.continuous = false; 
      recognition.interimResults = true;
      recognition.lang = 'en-US';

      
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

        
        setTranscript((finalTranscriptRef.current + interimTranscriptRef.current).trim());
      };

      recognition.onend = () => {
        
        if (interimTranscriptRef.current) {
          finalTranscriptRef.current += interimTranscriptRef.current;
          interimTranscriptRef.current = '';
        }

        
        setTranscript(finalTranscriptRef.current.trim());
        setIsListening(false);
      };

      recognition.onerror = (event) => {
        if (event.error !== 'no-speech' && event.error !== 'aborted') {
          console.warn('Recognition error:', event.error);
          setError(`Speech recognition error: ${event.error}`);
        }
      };

      
      recognition.start();
    } catch (err) {
      console.error('Failed to start speech recognition:', err);
      setError('Failed to start speech recognition');
      setIsListening(false);
    }
  }, [isSupported, isListening]);

  
  const continueListening = useCallback(() => {
    if (!isSupported || isListening) return;

    try {
      
      interimTranscriptRef.current = '';

      
      if (recognitionRef.current) {
        try {
          recognitionRef.current.stop();
        } catch (err) {
          
        }
      }

      
      const recognition = new SpeechRecognitionAPI() as SpeechRecognition;
      recognitionRef.current = recognition;

      
      recognition.continuous = false;
      recognition.interimResults = true;
      recognition.lang = 'en-US';

      
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

        
        setTranscript((finalTranscriptRef.current + interimTranscriptRef.current).trim());
      };

      recognition.onend = () => {
        
        if (interimTranscriptRef.current) {
          finalTranscriptRef.current += interimTranscriptRef.current;
          interimTranscriptRef.current = '';
        }

        
        setTranscript(finalTranscriptRef.current.trim());
        setIsListening(false);
      };

      recognition.onerror = (event) => {
        if (event.error !== 'no-speech' && event.error !== 'aborted') {
          console.warn('Recognition error:', event.error);
          setError(`Speech recognition error: ${event.error}`);
        }
      };

      
      recognition.start();
    } catch (err) {
      console.error('Failed to continue speech recognition:', err);
      setError('Failed to continue speech recognition');
      setIsListening(false);
    }
  }, [isSupported, isListening]);

  
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

