// AiStudio4.Web\src\components\InputBar\SystemPromptSection.tsx
import React from 'react';
import { ArrowDownToLine } from 'lucide-react';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { SystemPromptComponent } from '@/components/SystemPrompt/SystemPromptComponent';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';
import { windowEventService, WindowEvents } from '@/services/windowEvents';

interface SystemPromptSectionProps {
    activeConvId: string | null;
}

export function SystemPromptSection({ activeConvId }: SystemPromptSectionProps) {
    return (
        <div className="mb-2 rounded-lg flex-shrink-0 flex justify-between items-center">
            <SystemPromptComponent
                convId={activeConvId || undefined}
                onOpenLibrary={() => windowEventService.emit(WindowEvents.OPEN_SYSTEM_PROMPT_LIBRARY)}
            />
            <div className="flex items-center gap-2">
                <TooltipProvider>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <div 
                                className="flex items-center cursor-pointer" 
                                onClick={() => {
                                    const newState = !useJumpToEndStore.getState().jumpToEndEnabled;
                                    console.log(`[JumpToEnd] Button clicked, setting to: ${newState}`);
                                    useJumpToEndStore.getState().setJumpToEndEnabled(newState);
                                }}
                            >
                                <ArrowDownToLine 
                                    size={16} 
                                    className={`ml-2 ${useJumpToEndStore(state => {
                                        return state.jumpToEndEnabled;
                                    }) ? 'text-blue-400' : 'text-gray-300'}`} 
                                />
                            </div>
                        </TooltipTrigger>
                        <TooltipContent side="bottom">
                            <p>Auto-scroll to end</p>
                        </TooltipContent>
                    </Tooltip>
                </TooltipProvider>
            </div>
        </div>
    );
}