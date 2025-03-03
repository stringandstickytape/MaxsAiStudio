﻿// src/components/PinnedShortcuts.tsx
import { useSelector } from 'react-redux';
import { RootState } from '@/store/store';
import { Button } from '@/components/ui/button';
import { commandRegistry } from '@/commands/commandRegistry';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { Pin, Command, ChevronRight, Plus, Settings, RefreshCw, GitBranch, Mic } from 'lucide-react';

interface PinnedShortcutsProps {
    orientation?: 'horizontal' | 'vertical';
    maxShown?: number;
    className?: string;
}

// Common icon mapping for command names
const getIconForCommand = (commandId: string, iconName?: string) => {
    // If we have a specific icon name from the stored command, use it
    if (iconName) {
        switch (iconName) {
            case 'Plus': return <Plus className="h-3.5 w-3.5" />;
            case 'Settings': return <Settings className="h-3.5 w-3.5" />;
            case 'RefreshCw': return <RefreshCw className="h-3.5 w-3.5" />;
            case 'GitBranch': return <GitBranch className="h-3.5 w-3.5" />;
            case 'Mic': return <Mic className="h-3.5 w-3.5" />;
            default: return <Command className="h-3.5 w-3.5" />;
        }
    }

    // Fallback to inferring based on command ID
    if (commandId.includes('new')) return <Plus className="h-3.5 w-3.5" />;
    if (commandId.includes('settings')) return <Settings className="h-3.5 w-3.5" />;
    if (commandId.includes('clear') || commandId.includes('reset')) return <RefreshCw className="h-3.5 w-3.5" />;
    if (commandId.includes('tree')) return <GitBranch className="h-3.5 w-3.5" />;
    if (commandId.includes('voice')) return <Mic className="h-3.5 w-3.5" />;

    return <Command className="h-3.5 w-3.5" />;
};

export function PinnedShortcuts({
    orientation = 'horizontal',
    maxShown = 12,
    className
}: PinnedShortcutsProps) {
    const pinnedCommands = useSelector((state: RootState) => state.pinnedCommands.pinnedCommands);

    // Limit the number of displayed commands
    const visibleCommands = pinnedCommands.slice(0, maxShown);
    const hasMoreCommands = pinnedCommands.length > maxShown;

    const handleCommandClick = (commandId: string) => {
        commandRegistry.executeCommand(commandId);
    };

    // Empty state - no pinned commands
    if (pinnedCommands.length === 0) {
        return (
            <div className={cn(
                "flex items-center justify-center",
                orientation === 'vertical' ? "flex-col h-full" : "h-8",
                className
            )}>
                <div className="text-xs text-gray-500 flex items-center gap-1.5">
                    <Pin className="h-3 w-3" />
                    <span className="hidden sm:inline">Pin commands for quick access</span>
                </div>
            </div>
        );
    }

    return (
        <div className={cn(
            "flex items-center gap-1 overflow-x-auto py-1 px-2",
            orientation === 'vertical' ? "flex-col" : "flex-row w-full",
            className
        )}>
            <TooltipProvider>
                {visibleCommands.map(command => (
                    <Tooltip key={command.id} delayDuration={300}>
                        <TooltipTrigger asChild>
                            <Button
                                variant="ghost"
                                onClick={() => handleCommandClick(command.id)}
                                className="h-6 px-1.5 rounded-md bg-gray-800/60 hover:bg-gray-700 border border-gray-700/50 text-gray-300 hover:text-gray-100 flex items-center gap-1"
                            >
                                {getIconForCommand(command.id, command.iconName)}
                                <span className="text-xs font-medium max-w-[40px] truncate">
                                    {command.name.substring(0, 5)}
                                </span>
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                            <p>{command.name}</p>
                        </TooltipContent>
                    </Tooltip>
                ))}

                {/* "More" button if there are additional commands */}
                {hasMoreCommands && (
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => commandRegistry.executeCommand('open-command-bar')}
                                className="h-6 w-6 p-0 rounded-md bg-gray-800/60 hover:bg-gray-700 border border-gray-700/50 text-gray-300 hover:text-gray-100"
                            >
                                <ChevronRight className="h-3 w-3" />
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                            <p>More commands ({pinnedCommands.length - maxShown})</p>
                        </TooltipContent>
                    </Tooltip>
                )}
            </TooltipProvider>
        </div>
    );
}