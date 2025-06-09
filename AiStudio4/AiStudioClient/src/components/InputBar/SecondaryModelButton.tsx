// AiStudioClient/src/components/InputBar/SecondaryModelButton.tsx
import React, { useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

// Custom comparison function for SecondaryModelButton memoization
const areSecondaryModelButtonPropsEqual = () => {
  // This component has no props, so it only needs to re-render when the selectedSecondaryModel changes
  // The useModelManagement hook will handle that internally
  return true;
};

export const SecondaryModelButton = React.memo(() => {
    const { selectedSecondaryModel } = useModelManagement();
    
    const handleSecondaryModelClick = useCallback(() => {
        windowEventService.emit(WindowEvents.SELECT_SECONDARY_MODEL);
    }, []);
    
    return (
        <TooltipProvider>
            <Tooltip>
                <TooltipTrigger asChild>
                    <Button
                        variant="ghost"
                        onClick={handleSecondaryModelClick}
                        className="h-5 px-2 py-0 text-xs rounded-full bg-purple-600/10 hover:bg-purple-600/30 hover:text-purple-100 transition-colors"
                        style={{
                            color: 'var(--global-text-color)',
                            borderColor: 'var(--global-secondary-color, rgba(147, 51, 234, 0.2))',
                            border: '0px solid'
                        }}
                    >
                        <span className="truncate max-w-[160px]">{selectedSecondaryModel !== 'Select Model' ? selectedSecondaryModel : 'Secondary Model'}</span>
                    </Button>
                </TooltipTrigger>
                <TooltipContent>
                    <p>Secondary model for summaries & short tasks</p>
                </TooltipContent>
            </Tooltip>
        </TooltipProvider>
    );
}, areSecondaryModelButtonPropsEqual);