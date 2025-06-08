// AiStudioClient\src\components\PinnedShortcutRow.tsx
import React from 'react';
import { PinnedShortcutButton } from './PinnedShortcutButton';
import { PinnedCommand } from './pinnedShortcutsUtils';

interface PinnedShortcutRowProps {
    rowCommands: PinnedCommand[];
    rowIndex: number;
    itemsPerRow: number;
    orientation: 'horizontal' | 'vertical';
    onCommandClick: (commandId: string) => void;
    onPinCommand: (commandId: string, isCurrentlyPinned: boolean) => void;
    onRenameCommand: (command: PinnedCommand) => void;
    buttonRef?: React.RefObject<HTMLButtonElement>;
    onDragStart?: (e: React.DragEvent, command: PinnedCommand, index: number) => void;
    onDragEnd?: () => void;
    onDragOver?: (e: React.DragEvent) => void;
    onDrop?: (e: React.DragEvent, index: number) => void;
    isDragging?: boolean;
    draggedItem?: string | null;
}

export function PinnedShortcutRow({
    rowCommands,
    rowIndex,
    itemsPerRow,
    orientation,
    onCommandClick,
    onPinCommand,
    onRenameCommand,
    buttonRef,
    onDragStart,
    onDragEnd,
    onDragOver,
    onDrop,
    isDragging,
    draggedItem
}: PinnedShortcutRowProps) {
    return (
        <div className="flex flex-row items-center justify-center gap-1 w-full">
            <div
                className="flex items-center gap-2 flex-row"
                onDragOver={onDragOver}
            >
                {rowCommands.map((command, index) => {
                    const globalIndex = rowIndex * itemsPerRow + index;
                    return (
                        <PinnedShortcutButton
                            key={command.id}
                            command={command}
                            index={globalIndex}
                            orientation={orientation}
                            onCommandClick={onCommandClick}
                            onPinCommand={onPinCommand}
                            onRenameCommand={onRenameCommand}
                            isButtonRef={command.id === rowCommands[0]?.id && rowIndex === 0}
                            buttonRef={buttonRef}
                            onDragStart={onDragStart}
                            onDragEnd={onDragEnd}
                            onDragOver={onDragOver}
                            onDrop={onDrop}
                            isDragging={isDragging}
                            draggedItem={draggedItem}
                        />
                    );
                })}
            </div>
        </div>
    );
}