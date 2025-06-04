// AiStudioClient/src/components/InputBar/ToolsButton.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { Wrench } from 'lucide-react';
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface ToolsButtonProps {
    activeTools: string[];
    removeActiveTool: (toolId: string) => void;
    disabled: boolean;
}

export function ToolsButton({ activeTools, removeActiveTool, disabled }: ToolsButtonProps) {
    return (
        <TooltipProvider delayDuration={300}>
            <Tooltip>
                <TooltipTrigger asChild>
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => windowEventService.emit(WindowEvents.OPEN_TOOL_LIBRARY)}
                        onMouseDown={(e) => {
                            if (e.button === 1) {
                                e.preventDefault();
                                activeTools.forEach(toolId => removeActiveTool(toolId));
                            }
                        }}
                        className="h-5 px-2 py-0 text-xs rounded-full bg-gray-600/10 border border-gray-700/20 text-gray-300 hover:bg-gray-600/30 hover:text-gray-100 transition-colors flex-shrink-0 relative"
                        disabled={disabled}
                        style={{
                            color: 'var(--global-text-color)',
                            borderColor: 'var(--global-secondary-color, rgba(147, 51, 234, 0.2))',
                            border: '0px solid'
                        }}
                    >
                        <Wrench className="h-3 w-3 mr-1" />
                        <span>Tools</span>
                        {activeTools.length > 0 && (
                            <span className="ml-1 inline-flex items-center justify-center px-1.5 py-0.5 text-xs font-bold leading-none text-white bg-blue-500 rounded-full" title="Middle-click to clear all">
                                {activeTools.length}
                            </span>
                        )}
                    </Button>
                </TooltipTrigger>
                <TooltipContent side="top" align="center">
                    Middle-click to clear all active tools
                </TooltipContent>
            </Tooltip>
        </TooltipProvider>
    );
}