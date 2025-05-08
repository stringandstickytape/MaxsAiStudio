// AiStudioClient\src\components\PinnedShortcutDropdown.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { ChevronDown } from 'lucide-react';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuGroup,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { PinnedCommand } from './pinnedShortcutsUtils';

interface PinnedShortcutDropdownProps {
    hiddenCommands: PinnedCommand[];
    orientation: 'horizontal' | 'vertical';
    onCommandClick: (commandId: string) => void;
    onPinCommand: (commandId: string, isCurrentlyPinned: boolean) => void;
    visibleCount: number;
    totalCount: number;
}

export function PinnedShortcutDropdown({
    hiddenCommands,
    orientation,
    onCommandClick,
    onPinCommand,
    visibleCount,
    totalCount
}: PinnedShortcutDropdownProps) {
    if (hiddenCommands.length === 0) return null;
    
    return (
        <DropdownMenu>
            <Tooltip>
                <TooltipTrigger asChild>
                    <DropdownMenuTrigger asChild>
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-6 w-6 p-0 rounded-md bg-gray-800/60 hover:bg-gray-700 border border-gray-700/50 text-gray-300 hover:text-gray-100"
                        >
                            <ChevronDown className="h-3 w-3" />
                        </Button>
                    </DropdownMenuTrigger>
                </TooltipTrigger>
                <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                    <p>More commands ({totalCount - visibleCount})</p>
                </TooltipContent>
            </Tooltip>
            <DropdownMenuContent align="end" className="w-48 max-h-[50vh] overflow-y-auto">
                <DropdownMenuGroup>
                    {hiddenCommands.map((command) => (
                        <DropdownMenuItem
                            key={command.id}
                            onClick={() => onCommandClick(command.id)}
                            onContextMenu={(e) => {
                                e.preventDefault();
                                onPinCommand(command.id, true);
                            }}
                            className="flex items-center gap-1 text-xs whitespace-normal"
                        >
                            <span>{command.name}</span>
                        </DropdownMenuItem>
                    ))}
                </DropdownMenuGroup>
            </DropdownMenuContent>
        </DropdownMenu>
    );
}