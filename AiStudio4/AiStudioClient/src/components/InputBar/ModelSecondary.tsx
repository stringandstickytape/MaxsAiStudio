// AiStudioClient/src/components/InputBar/ModelSecondary.tsx
import React from 'react';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

export function ModelSecondary() {
    const handleSecondaryModelClick = () =>
        windowEventService.emit(WindowEvents.SELECT_SECONDARY_MODEL);
    return (
        <div className="flex items-center mr-2">
            <ModelStatusBar onSecondaryClick={handleSecondaryModelClick} />
        </div>
    );
}