// AiStudioClient/src/components/InputBar/PrimaryModelButton.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

export function PrimaryModelButton() {
    const { selectedPrimaryModel } = useModelManagement();
    const handlePrimaryModelClick = () =>
        windowEventService.emit(WindowEvents.SELECT_PRIMARY_MODEL);
    return (
        <TooltipProvider>
            <Tooltip>
                <TooltipTrigger asChild>
                    <Button
                        variant="ghost"
                        onClick={handlePrimaryModelClick}
                        className="h-5 px-2 py-0 text-xs rounded-full bg-blue-600/10 hover:bg-blue-600/30 hover:text-blue-100 transition-colors"
                        style={{
                            color: 'var(--global-text-color)',
                            borderColor: 'var(--global-primary-color, rgba(37, 99, 235, 0.2))',
                            border: '0px solid'
                        }}
                    >
                        <span className="truncate max-w-[160px]">{selectedPrimaryModel !== 'Select Model' ? selectedPrimaryModel : 'Primary Model'}</span>
                    </Button>
                </TooltipTrigger>
                <TooltipContent>
                    <p>Primary model for chat responses</p>
                </TooltipContent>
            </Tooltip>
        </TooltipProvider>
    );
}