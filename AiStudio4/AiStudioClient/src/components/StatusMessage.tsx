// AiStudioClient/src/components/StatusMessage.tsx
import { useStatusMessageStore } from '@/stores/useStatusMessageStore';
import { useEffect, useState } from 'react';

export function StatusMessage() {
    const { message, lastUpdated } = useStatusMessageStore();
    const [elapsedTime, setElapsedTime] = useState<string>('');

    useEffect(() => {
        if (!lastUpdated) {
            setElapsedTime('');
            return;
        }

        // Update the elapsed time every second
        const intervalId = setInterval(() => {
            const now = Date.now();
            const elapsedMs = now - lastUpdated;
            const seconds = Math.floor((elapsedMs / 1000) % 60);
            const minutes = Math.floor(elapsedMs / 1000 / 60);
            setElapsedTime(`${minutes}:${seconds.toString().padStart(2, '0')}`);
        }, 1000);

        // Initial calculation
        const now = Date.now();
        const elapsedMs = now - lastUpdated;
        const seconds = Math.floor((elapsedMs / 1000) % 60);
        const minutes = Math.floor(elapsedMs / 1000 / 60);
        setElapsedTime(`${minutes}:${seconds.toString().padStart(2, '0')}`);

        return () => clearInterval(intervalId);
    }, [lastUpdated]);

    // Hide component when message is empty
    if (!message) return null;

    return (
        <div className="status-message-container py-1 px-2 rounded-md animate-fade-in w-full"
             style={{
                 backgroundColor: 'var(--global-background-color, #1f2937)',
                 color: 'var(--global-text-color, #e2e8f0)',
                 border: '0px'
             }}>
            <div className="flex items-start gap-2">
                <span className="animate-pulse flex-shrink-0 mt-0.5">⚙️</span>
                <div className="min-w-0 flex-1">
                    <div className="flex items-start gap-2 flex-wrap">
                        {elapsedTime && (
                            <span className="opacity-70 text-xs leading-tight flex-shrink-0">{elapsedTime}</span>
                        )}
                        <div className="break-words leading-tight text-xs flex-1 min-w-0">{message}</div>
                    </div>
                </div>
            </div>
        </div>
    );
}