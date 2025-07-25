﻿// AiStudioClient\src\components\InputBar\ToolsSection.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { Wrench, Server } from 'lucide-react';
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { useToolStore } from '@/stores/useToolStore';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface ToolsSectionProps {
    activeTools: string[];
    removeActiveTool: (toolId: string) => void;
    disabled: boolean;
}

export function ToolsSection({
    activeTools,
    removeActiveTool,
    disabled
}: ToolsSectionProps) {
    const { enabledCount } = useMcpServerStore();
    
    return (
        <div className="flex items-center gap-4 mr-2">
            <div className="flex flex-col gap-1">
                <TooltipProvider delayDuration={300}>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => windowEventService.emit(WindowEvents.OPEN_TOOL_LIBRARY)}
                                onMouseDown={(e) => {
                                    if (e.button === 1) { // Middle mouse button
                                        e.preventDefault(); // Prevent default middle-click behavior
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
                <TooltipProvider delayDuration={300}>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => windowEventService.emit(WindowEvents.OPEN_SERVER_LIST)}
                                onMouseDown={(e) => {
                                    if (e.button === 1) { // Middle mouse button
                                        e.preventDefault(); // Prevent default middle-click behavior
                                        useMcpServerStore.getState().setEnabledServers([]);
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
                                <Server className="h-3 w-3 mr-1"
                                />
                                <span>MCP Servers</span>
                                {enabledCount > 0 && (
                                    <span className="ml-1 inline-flex items-center justify-center px-1.5 py-0.5 text-xs font-bold leading-none text-white bg-blue-500 rounded-full" title="Middle-click to clear all">
                                        {enabledCount}
                                    </span>
                                )}
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent side="top" align="center">
                            Middle-click to clear all enabled MCP servers
                        </TooltipContent>
                    </Tooltip>
                </TooltipProvider>
            </div>

        </div>
    );
}