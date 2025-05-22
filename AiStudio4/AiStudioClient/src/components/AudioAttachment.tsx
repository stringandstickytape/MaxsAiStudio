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
        if (typeof attachment.content === 'string') { // Assuming content is base64 string
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

                // Create audio element
                const audio = new Audio(url);
                audio.addEventListener('ended', () => setIsPlaying(false));
                setAudioElement(audio);

                return () => {
                    audio.pause();
                    audio.src = '';
                    URL.revokeObjectURL(url); // Cleanup
                };
            } catch (e) {
                console.error("Error processing base64 audio:", e);
                setAudioSrc(null);
            }
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
        if (audioSrc) {
            const a = document.createElement('a');
            a.href = audioSrc;
            a.download = attachment.name;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        }
    };

    if (!audioSrc) {
        return <div className="text-xs text-red-400 p-2">Could not load audio: {attachment.name}</div>;
    }

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
                    <audio
                        controls
                        src={audioSrc}
                        className="w-full h-8"
                        onPlay={() => setIsPlaying(true)}
                        onPause={() => setIsPlaying(false)}
                    >
                        Your browser does not support the audio element.
                    </audio>
                </div>
            </div>
        </div>
    );
};