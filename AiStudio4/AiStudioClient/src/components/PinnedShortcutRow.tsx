// AiStudioClient\src\components\PinnedShortcutRow.tsx
import React from 'react';
import { Droppable } from 'react-beautiful-dnd';
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
}

export function PinnedShortcutRow({
    rowCommands,
    rowIndex,
    itemsPerRow,
    orientation,
    onCommandClick,
    onPinCommand,
    onRenameCommand,
    buttonRef
}: PinnedShortcutRowProps) {
    return (
        <div className="flex flex-row items-center justify-center gap-1 w-full">
            <Droppable
                droppableId={`pinned-commands-row-${rowIndex}`}
                direction="horizontal"
                isCombineEnabled={false}
                isDropDisabled={false}
                ignoreContainerClipping={false}
            >
                {(provided) => (
                    <div
                        {...provided.droppableProps}
                        ref={provided.innerRef}
                        className="flex items-center gap-2 flex-row"
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
                                />
                            );
                        })}
                        {provided.placeholder}
                    </div>
                )}
            </Droppable>
        </div>
    );
}