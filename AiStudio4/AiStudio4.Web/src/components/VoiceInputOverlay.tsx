// src/components/VoiceInputOverlay.tsx
import React, { useEffect, useRef } from 'react';
import { Mic, MicOff, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useVoiceInput } from '@/hooks/useVoiceInput';

interface VoiceInputOverlayProps {
    isOpen: boolean;
    onClose: () => void;
    onTranscript: (text: string) => void;
}

export const VoiceInputOverlay: React.FC<VoiceInputOverlayProps> = ({
    isOpen,
    onClose,
    onTranscript
}) => {
    const { isListening, transcript, error, startListening, stopListening, isSupported } = useVoiceInput();
    const hasStartedRef = useRef(false);

    // Start listening when opened
    useEffect(() => {
        if (isOpen && isSupported && !hasStartedRef.current) {
            hasStartedRef.current = true;
            startListening();
        }

        // Clean up on unmount or close
        return () => {
            if (isListening) {
                stopListening();
            }
            if (!isOpen) {
                hasStartedRef.current = false;
            }
        };
    }, [isOpen, isSupported, startListening, stopListening, isListening]);

    // Submit transcript when done
    const handleDone = () => {
        if (transcript.trim()) {
            onTranscript(transcript);
        }
        stopListening();
        onClose();
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center">
            <div className="bg-gray-900 border border-gray-700 rounded-lg p-6 shadow-2xl max-w-md w-full mx-4">
                <div className="flex justify-between items-center mb-4">
                    <h3 className="text-xl font-medium text-gray-100">Voice Input</h3>
                    <Button variant="ghost" size="icon" onClick={onClose} className="text-gray-400 hover:text-gray-100">
                        <X className="h-5 w-5" />
                    </Button>
                </div>

                {!isSupported ? (
                    <div className="text-center py-6">
                        <MicOff className="h-12 w-12 mx-auto text-red-500 mb-2" />
                        <p className="text-red-400">Voice input is not supported in this browser.</p>
                        <p className="text-gray-400 text-sm mt-2">
                            Try using Chrome, Edge, or Safari on a mobile device.
                        </p>
                    </div>
                ) : (
                    <>
                        <div className="flex justify-center mb-6">
                            <div
                                className={cn(
                                    "w-20 h-20 rounded-full border-4 flex items-center justify-center transition-all duration-200",
                                    isListening
                                        ? "border-blue-500 bg-blue-500/20 animate-pulse"
                                        : "border-gray-700 bg-gray-800"
                                )}
                            >
                                <Mic className={cn(
                                    "h-8 w-8 transition-colors",
                                    isListening ? "text-blue-500" : "text-gray-400"
                                )} />
                            </div>
                        </div>

                        <div className="bg-gray-800 border border-gray-700 p-4 rounded-lg min-h-20 mb-4">
                            {transcript ? (
                                <p className="text-gray-200">{transcript}</p>
                            ) : (
                                <p className="text-gray-400 italic">
                                    {isListening ? "Listening..." : "Click the mic to start speaking"}
                                </p>
                            )}
                        </div>

                        {error && (
                            <p className="text-red-400 mb-4 text-sm">{error}</p>
                        )}

                        <div className="flex justify-between">
                            <Button
                                variant="outline"
                                onClick={isListening ? stopListening : startListening}
                                className={cn(
                                    "bg-gray-800 hover:bg-gray-700 text-gray-100",
                                    isListening && "border-blue-500 text-blue-400"
                                )}
                            >
                                {isListening ? "Stop Listening" : "Start Listening"}
                            </Button>
                            <Button
                                onClick={handleDone}
                                disabled={!transcript.trim()}
                                className="bg-blue-600 hover:bg-blue-700 text-white"
                            >
                                Done
                            </Button>
                        </div>
                    </>
                )}
            </div>
        </div>
    );
};