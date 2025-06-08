// AiStudioClient\src\components\PinnedShortcutButton.tsx
import React from 'react';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { getIconForCommand, getCategoryBackgroundColor, getCategoryBorderColor, PinnedCommand } from './pinnedShortcutsUtils';

interface PinnedShortcutButtonProps {
    command: PinnedCommand;
    index: number;
    orientation: 'horizontal' | 'vertical';
    onCommandClick: (commandId: string) => void;
    onPinCommand: (commandId: string, isCurrentlyPinned: boolean) => void;
    onRenameCommand: (command: PinnedCommand) => void;
    isButtonRef?: boolean;
    buttonRef?: React.RefObject<HTMLButtonElement>;
    onDragStart?: (e: React.DragEvent, command: PinnedCommand, index: number) => void;
    onDragEnd?: () => void;
    onDragOver?: (e: React.DragEvent) => void;
    onDrop?: (e: React.DragEvent, index: number) => void;
    isDragging?: boolean;
    draggedItem?: string | null;
}

export function PinnedShortcutButton({
    command,
    index,
    orientation,
    onCommandClick,
    onPinCommand,
    onRenameCommand,
    isButtonRef = false,
    buttonRef,
    onDragStart,
    onDragEnd,
    onDragOver,
    onDrop,
    isDragging = false,
    draggedItem
}: PinnedShortcutButtonProps) {
    const isBeingDragged = draggedItem === command.id;
    
    return (
        <div
            draggable
            onDragStart={(e) => onDragStart?.(e, command, index)}
            onDragEnd={onDragEnd}
            onDragOver={onDragOver}
            onDrop={(e) => onDrop?.(e, index)}
            className={cn(
                'flex flex-col items-center group',
                isBeingDragged && 'opacity-70 z-50',
                isDragging && !isBeingDragged && 'opacity-50'
            )}
        >
            <div
                className="cursor-grab h-2 w-[38px] text-gray-500 hover:text-gray-300 flex items-center justify-center rounded hover:bg-gray-700 transition-colors opacity-0 group-hover:opacity-100"
            >
                <div className="w-12 flex items-center justify-center">
                    <div className="h-[3px] w-8 bg-current rounded-full"></div>
                </div>
            </div>
                    <Tooltip key={command.id} delayDuration={300}>
                        <TooltipTrigger asChild>
                            <Button
                                ref={isButtonRef ? buttonRef : null}
                                variant="ghost"
                                onClick={() => onCommandClick(command.id)}
                                onContextMenu={(e) => {
                                    e.preventDefault();
                                    onPinCommand(command.id, true);
                                }}
                                onAuxClick={(e) => {
                                    // Handle middle-click (button 1)
                                    if (e.button === 1) {
                                        e.preventDefault();
                                        onRenameCommand(command);
                                    }
                                }}
                                className={`PinnedShortcuts ${orientation === 'horizontal' ? 'h-[36px] w-[100px]' : 'h-[72px] w-[80px]'} ${orientation === 'horizontal' ? 'px-0 py-0' : 'px-0.5 py-0.5'} rounded-md ${getCategoryBackgroundColor(command.section)} hover:bg-opacity-30 flex flex-row items-center justify-center relative`}
                                style={{
                                    '--hover-text-color': 'var(--global-text-color, #f9fafb)',
                                    border: `var(--pinnedshortcuts-border-width, 1px) var(--pinnedshortcuts-border-style, solid) var(--pinnedshortcuts-border-color, ${getCategoryBorderColor(command.section).replace('border-', '')})`,
                                    color: 'var(--global-text-color, #e5e7eb)',
                                    fontWeight: 'var(--pinnedshortcuts-font-weight, 500)',
                                    fontFamily: 'var(--pinnedshortcuts-font-family, inherit)',
                                    ...(window?.theme?.PinnedShortcuts?.buttonStyle || {})
                                }}
                            >
                                {command.iconName && (
                                    <span className={orientation === 'horizontal' ? 'm-0.5 flex-shrink-0' : 'mr-0.5 flex-shrink-0'}>
                                        {getIconForCommand(command.id, command.iconName, command.iconSet)}
                                    </span>
                                )}
                                <span className="text-xs flex-1 text-left leading-tight break-words whitespace-normal overflow-hidden line-clamp-2"
                                      style={{
                                          fontWeight: 'var(--pinnedshortcuts-font-weight, 500)',
                                      }}>
                                    {command.name}
                                </span>
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                            <p>{command.name}</p>
                            <p className="text-xs text-gray-400">
                                Category: {command.section || 'Unknown'}
                            </p>
                            <p className="text-xs text-gray-400">
                                Drag handle above button to reorder · Right-click to unpin · Middle-click to rename
                            </p>
                        </TooltipContent>
                    </Tooltip>
        </div>
    );
}