﻿// AiStudioClient\src\components\InputBar\ModelStatusSection.tsx
import React from 'react';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

export function ModelStatusSection() {
    const handlePrimaryModelClick = () =>
        windowEventService.emit(WindowEvents.SELECT_PRIMARY_MODEL);

    const handleSecondaryModelClick = () =>
        windowEventService.emit(WindowEvents.SELECT_SECONDARY_MODEL);
        
    return (
        <div className="flex items-center mr-2">
            <ModelStatusBar
                onPrimaryClick={handlePrimaryModelClick}
                onSecondaryClick={handleSecondaryModelClick}
            />
        </div>
    );
}