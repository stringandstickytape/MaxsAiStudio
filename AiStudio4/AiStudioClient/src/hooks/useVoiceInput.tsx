import { useCallback, useEffect, useRef } from 'react';
import SpeechRecognition, { useSpeechRecognition } from 'react-speech-recognition';
import { useVoiceInputStore } from '../stores/useVoiceInputStore';

interface UseVoiceInputProps {
  onTranscriptUpdate: (text: string) => void;
}

export function useVoiceInput(props: UseVoiceInputProps) {
  const {
    transcript,
    listening,
    resetTranscript,
    browserSupportsSpeechRecognition,
    isMicrophoneAvailable
  } = useSpeechRecognition();
  
  const { isListening, setBaseText } = useVoiceInputStore();
  const onTranscriptUpdateRef = useRef(props.onTranscriptUpdate);
  const previousTranscriptRef = useRef('');
  
  // Keep ref updated
  useEffect(() => {
    onTranscriptUpdateRef.current = props.onTranscriptUpdate;
  }, [props.onTranscriptUpdate]);

  // Update the input with the current transcript - simple approach like the demo
  useEffect(() => {
    if (listening && transcript !== previousTranscriptRef.current) {
      // Just pass the transcript directly, let the component handle how to display it
      onTranscriptUpdateRef.current(transcript);
      previousTranscriptRef.current = transcript;
    }
  }, [transcript, listening]);

  const startMicCapture = useCallback(() => {
    if (!browserSupportsSpeechRecognition) {
      useVoiceInputStore.getState().setError("Speech recognition not supported by your browser.");
      useVoiceInputStore.getState()._setIsListening(false);
      return;
    }

    if (!isMicrophoneAvailable) {
      useVoiceInputStore.getState().setError("Microphone access denied. Please allow microphone access and try again.");
      useVoiceInputStore.getState()._setIsListening(false);
      return;
    }

    resetTranscript();
    previousTranscriptRef.current = '';
    
    SpeechRecognition.startListening({
      continuous: true,
      language: 'en-US',
      interimResults: true
    });
    
    useVoiceInputStore.getState()._setIsListening(true);
    useVoiceInputStore.getState().setError(null);
  }, [browserSupportsSpeechRecognition, resetTranscript, isMicrophoneAvailable]);

  const stopMicCapture = useCallback(() => {
    SpeechRecognition.stopListening();
    useVoiceInputStore.getState()._setIsListening(false);
    setBaseText('');
  }, [setBaseText]);

  // Sync library listening state with our store
  useEffect(() => {
    if (!listening && isListening) {
      useVoiceInputStore.getState()._setIsListening(false);
    }
  }, [listening, isListening]);

  const resetTranscriptWrapper = useCallback(() => {
    resetTranscript();
    previousTranscriptRef.current = '';
    setBaseText('');
  }, [resetTranscript, setBaseText]);

  return {
    isSupported: browserSupportsSpeechRecognition,
    startMicCapture,
    stopMicCapture,
    resetTranscript: resetTranscriptWrapper,
  };
}