import { useState, useCallback, useEffect, useRef } from 'react';

type SpeechRecognition = EventTarget & {
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

type SpeechRecognitionEvent = {
  resultIndex: number;
  results: SpeechRecognitionResultList;
}

type SpeechRecognitionResultList = {
  length: number;
  item(index: number): SpeechRecognitionResult;
  [index: number]: SpeechRecognitionResult;
}

type SpeechRecognitionResult = {
  isFinal: boolean;
  length: number;
  item(index: number): SpeechRecognitionAlternative;
  [index: number]: SpeechRecognitionResult;
}

type SpeechRecognitionAlternative = {
  transcript: string;
  confidence: number;
}

type SpeechRecognitionError = Event & {
  error: string;
  message: string;
}

const SpeechRecognitionAPI = typeof window !== 'undefined' ? 
  window.SpeechRecognition || 
  (window as any).webkitSpeechRecognition || 
  (window as any).mozSpeechRecognition || 
  (window as any).msSpeechRecognition : null;

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
    return () => recognitionRef.current?.stop?.();
  }, []);

  const setupRecognition = useCallback((isContinuing = false) => {
    if (!isSupported || isListening) return;

    try {
      if (!isContinuing) {
        finalTranscriptRef.current = '';
        setTranscript('');
      }
      interimTranscriptRef.current = '';
      
      try { recognitionRef.current?.stop(); } catch {}
      
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
          
          result.isFinal ? 
            (finalTranscriptRef.current += text + ' ') : 
            (interimTranscriptRef.current += text + ' ');
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
      console.error(`Failed to ${isContinuing ? 'continue' : 'start'} speech recognition:`, err);
      setError(`Failed to ${isContinuing ? 'continue' : 'start'} speech recognition`);
      setIsListening(false);
    }
  }, [isSupported, isListening]);

  const startListening = useCallback(() => setupRecognition(false), [setupRecognition]);
  const continueListening = useCallback(() => setupRecognition(true), [setupRecognition]);
  
  const stopListening = useCallback(() => {
    try { recognitionRef.current?.stop(); } catch (err) { console.error('Error stopping recognition:', err); }
    setIsListening(false);
  }, []);
  
  const resetTranscript = useCallback(() => {
    finalTranscriptRef.current = interimTranscriptRef.current = '';
    setTranscript('');
  }, []);

  return {
    isListening, transcript, error, isSupported,
    startListening, stopListening, resetTranscript, continueListening
  };
}
