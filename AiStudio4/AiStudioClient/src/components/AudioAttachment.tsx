// AiStudio4/AiStudioClient/src/components/AudioAttachment.tsx
import React, { useState, useEffect } from 'react';
import { Attachment } from '@/types/attachment';
import { Button } from '@/components/ui/button';
import { Download, Play, Pause } from 'lucide-react';

interface AudioAttachmentProps {
    attachment: Attachment;
    className?: string;
}

export const AudioAttachment: React.FC<AudioAttachmentProps> = ({ attachment, className }) => {
    const [audioSrc, setAudioSrc] = useState<string | null>(null);
    const [isPlaying, setIsPlaying] = useState(false);
    const [audioElement, setAudioElement] = useState<HTMLAudioElement | null>(null);

    useEffect(() => {
        // Content should be an ArrayBuffer due to processAttachments
        if (attachment.content instanceof ArrayBuffer) {
            try {
                const blob = new Blob([attachment.content], { type: attachment.type });
                const url = URL.createObjectURL(blob);
                setAudioSrc(url);

                const audio = new Audio(url);
                audio.addEventListener('ended', () => setIsPlaying(false));
                setAudioElement(audio);

                return () => {
                    audio.pause();
                    audio.src = ''; // Release resource
                    URL.revokeObjectURL(url); // Cleanup object URL
                };
            } catch (e) {
                console.error("Error processing ArrayBuffer audio:", e);
                setAudioSrc(null);
            }
        } else if (typeof attachment.content === 'string') {
            // Fallback for cases where content might still be a base64 string (e.g., if processAttachments was skipped)
            console.warn("AudioAttachment received string content, expected ArrayBuffer. Attempting base64 decode.");
            try {
                const byteCharacters = atob(attachment.content);
                const byteNumbers = new Array(byteCharacters.length);
                for (let i = 0; i < byteCharacters.length; i++) {
                    byteNumbers[i] = byteCharacters.charCodeAt(i);
                }
                const byteArray = new Uint8Array(byteNumbers);
                const blob = new Blob([byteArray], { type: attachment.type });
                const url = URL.createObjectURL(blob);
                setAudioSrc(url);

                const audio = new Audio(url);
                audio.addEventListener('ended', () => setIsPlaying(false));
                setAudioElement(audio);

                return () => {
                    audio.pause();
                    audio.src = '';
                    URL.revokeObjectURL(url);
                };
            } catch (e) {
                console.error("Error processing base64 audio string (fallback):", e);
                setAudioSrc(null);
            }
        } else {
            console.error("AudioAttachment received unexpected content type:", typeof attachment.content, "Value:", attachment.content);
            setAudioSrc(null); // Cannot process if not ArrayBuffer or string
        }
    }, [attachment.content, attachment.type]);

    const togglePlayPause = () => {
        if (audioElement) {
            if (isPlaying) {
                audioElement.pause();
            } else {
                audioElement.play();
            }
            setIsPlaying(!isPlaying);
        }
    };

    const downloadAudio = () => {
        // Use the original ArrayBuffer content for download to ensure no re-encoding
        if (attachment.content instanceof ArrayBuffer) {
            const blob = new Blob([attachment.content], { type: attachment.type });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = attachment.name;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url); // Clean up the object URL
        } else if (audioSrc) { // Fallback if content wasn't ArrayBuffer but audioSrc was created
            const a = document.createElement('a');
            a.href = audioSrc;
            a.download = attachment.name;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        } else {
            console.error("Cannot download: audio content is not available in a downloadable format.");
        }
    };

    if (!audioSrc && !(attachment.content instanceof ArrayBuffer)) { // Show error only if no processable content
        return <div className="text-xs text-red-400 p-2">Could not load audio: {attachment.name} (Invalid content)</div>;
    }

    // If audioSrc could not be created but we have the ArrayBuffer, still allow download
    if (!audioSrc && (attachment.content instanceof ArrayBuffer)) {
        return (
            <div className={`audio-attachment p-3 my-1 bg-gray-800/70 rounded border border-gray-700/50 ${className || ''}`}>
                <div className="flex items-center justify-between mb-1">
                    <p className="text-xs text-gray-300 truncate flex-1">{attachment.name}</p>
                    <Button
                        variant="ghost"
                        size="icon"
                        className="h-6 w-6 text-gray-400 hover:text-gray-100 hover:bg-gray-700"
                        onClick={downloadAudio}
                        title="Download"
                    >
                        <Download className="h-3 w-3" />
                    </Button>
                </div>
                <p className="text-xs text-yellow-400">Preview not available. Download to listen.</p>
            </div>
        );
    }

    // If audioSrc is created, render the player and download button
    return (
        <div className={`audio-attachment p-3 my-1 bg-gray-800/70 rounded border border-gray-700/50 ${className || ''}`}>
            <div className="flex items-center justify-between mb-1">
                <p className="text-xs text-gray-300 truncate flex-1">{attachment.name}</p>
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-6 w-6 text-gray-400 hover:text-gray-100 hover:bg-gray-700"
                    onClick={downloadAudio}
                    title="Download"
                >
                    <Download className="h-3 w-3" />
                </Button>
            </div>
            <div className="flex items-center gap-2">
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 rounded-full bg-gray-700 hover:bg-gray-600 text-gray-200"
                    onClick={togglePlayPause}
                >
                    {isPlaying ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
                </Button>
                <div className="flex-1">
                    {/* The native <audio> tag might still fail to play certain PCM formats depending on browser support.
                        However, the download functionality should work independently. */}
                    <audio
                        controls
                        src={audioSrc || ''} // Ensure src is not null
                        className="w-full h-8"
                        onPlay={() => setIsPlaying(true)}
                        onPause={() => setIsPlaying(false)}
                        onError={(e) => console.warn("HTML Audio Element Error:", e)}
                    >
                        Your browser does not support the audio element.
                    </audio>
                </div>
            </div>
        </div>
    );
};