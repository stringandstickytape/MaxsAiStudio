// AiStudioClient/src/components/InputBar/ModelPrimary.tsx
import React from 'react';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

export function ModelPrimary() {
    const handlePrimaryModelClick = () =>
        windowEventService.emit(WindowEvents.SELECT_PRIMARY_MODEL);
    return (
        <div className="flex items-center mr-2">
            <ModelStatusBar onPrimaryClick={handlePrimaryModelClick} />
        </div>
    );
}