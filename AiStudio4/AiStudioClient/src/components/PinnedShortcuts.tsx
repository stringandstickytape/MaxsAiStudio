// AiStudioClient\src\components\PinnedShortcuts.tsx
import React, { useEffect, useRef, useState } from 'react';
import { Pin } from 'lucide-react';
import { cn } from '@/lib/utils';
import { TooltipProvider } from '@/components/ui/tooltip';
import { useCommandStore } from '@/stores/useCommandStore';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { DragDropContext, Droppable, DropResult } from 'react-beautiful-dnd';
import { PinnedShortcutButton } from './PinnedShortcutButton';
import { PinnedShortcutRow } from './PinnedShortcutRow';
import { PinnedShortcutDropdown } from './PinnedShortcutDropdown';
import { RenameShortcutDialog } from './RenameShortcutDialog';
import { PinnedCommand } from './pinnedShortcutsUtils';
import { IconSet } from './IconSelector';

interface PinnedShortcutsProps {
    orientation?: 'horizontal' | 'vertical';
    maxShown?: number;
    className?: string;
    autoFit?: boolean;
    maxRows?: number;
}

export function PinnedShortcuts({
    orientation = 'horizontal',
    maxShown = 10,
    className,
    autoFit = true,
    maxRows = 3,
}: PinnedShortcutsProps) {
    const {
        pinnedCommands,
        loading,
        error,
        fetchPinnedCommands,
        addPinnedCommand,
        removePinnedCommand,
        reorderPinnedCommands,
        savePinnedCommands,
        setPinnedCommands,
        setIsModified,
    } = usePinnedCommandsStore();

    const clientId = localStorage.getItem('clientId');

    const [initialLoadComplete, setInitialLoadComplete] = useState(false);
    const prevPinnedCommandsRef = useRef<string>('');
    const wsConnectedRef = useRef<boolean>(false);
    const containerRef = useRef<HTMLDivElement>(null);
    const buttonRef = useRef<HTMLButtonElement>(null);

    const [visibleCount, setVisibleCount] = useState(maxShown);
    const [rowCount, setRowCount] = useState(1);
    const [itemsPerRow, setItemsPerRow] = useState(maxShown);

    // Dialog state
    const [renameDialogOpen, setRenameDialogOpen] = useState(false);
    const [commandToRename, setCommandToRename] = useState<PinnedCommand | null>(null);
    const [newCommandName, setNewCommandName] = useState('');
    const [selectedIconName, setSelectedIconName] = useState<string | undefined>();
    const [selectedIconSet, setSelectedIconSet] = useState<IconSet>('lucide');
    
    // Track user modifications
    const [userModified, setUserModified] = useState(false);
    const isModified = usePinnedCommandsStore(state => state.isModified);

    // Initial load of pinned commands
    useEffect(() => {
        if (clientId) {
            fetchPinnedCommands();
        }
    }, [fetchPinnedCommands, clientId]);

    // WebSocket reconnection handler
    useEffect(() => {
        const handleWebSocketConnected = () => {
            if (wsConnectedRef.current && initialLoadComplete) {
                
                fetchPinnedCommands();
            }
            wsConnectedRef.current = true;
        };

        window.addEventListener('ws-connected', handleWebSocketConnected);

        return () => {
            window.removeEventListener('ws-connected', handleWebSocketConnected);
        };
    }, [fetchPinnedCommands, initialLoadComplete]);

    // Track initial load completion
    useEffect(() => {
        if (!loading && pinnedCommands.length > 0) {
            const currentCommandsString = JSON.stringify(pinnedCommands);
            prevPinnedCommandsRef.current = currentCommandsString;

            if (!initialLoadComplete) {
                setInitialLoadComplete(true);
                
            }
        }
    }, [loading, pinnedCommands, initialLoadComplete]);

    // Auto-save changes
    useEffect(() => {
        if (loading || !initialLoadComplete || !isModified) {
            return;
        }

        const saveTimer = setTimeout(() => {
            
            savePinnedCommands();
        }, 1000);

        return () => clearTimeout(saveTimer);
    }, [pinnedCommands, savePinnedCommands, loading, initialLoadComplete, isModified]);

    // Calculate visible buttons based on container width
    useEffect(() => {
        if (!autoFit || orientation === 'vertical' || !containerRef.current || !buttonRef.current) {
            return;
        }

        const calculateVisibleButtons = () => {
            const containerWidth = containerRef.current?.clientWidth || 0;
            const buttonWidth = 100 + 8; // Button width + gap

            const availableWidth = containerWidth;
            const maxButtonsPerRow = Math.floor(availableWidth / buttonWidth);

            const effectiveRows = Math.min(maxRows, Math.ceil(pinnedCommands.length / maxButtonsPerRow));
            const newItemsPerRow = maxButtonsPerRow;
            const newRowCount = Math.min(effectiveRows, Math.ceil(pinnedCommands.length / newItemsPerRow));
            const newVisibleCount = Math.min(newItemsPerRow * newRowCount, pinnedCommands.length);

            if (newItemsPerRow !== itemsPerRow) {
                setItemsPerRow(newItemsPerRow);
            }

            if (newRowCount !== rowCount) {
                setRowCount(newRowCount);
            }

            if (newVisibleCount !== visibleCount) {
                setVisibleCount(newVisibleCount);
            }
        };

        calculateVisibleButtons();

        const resizeObserver = new ResizeObserver(calculateVisibleButtons);
        resizeObserver.observe(containerRef.current);

        return () => {
            if (containerRef.current) {
                resizeObserver.unobserve(containerRef.current);
            }
            resizeObserver.disconnect();
        };
    }, [autoFit, orientation, pinnedCommands.length, visibleCount, itemsPerRow, rowCount, maxRows]);

    // Handle drag and drop reordering
    const handleDragEnd = (result: DropResult) => {
        if (!result.destination) return;

        const items = Array.from(pinnedCommands);
        const [reorderedItem] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, reorderedItem);

        const reorderedIds = items.map((cmd) => cmd.id);
        reorderPinnedCommands(reorderedIds);
        setUserModified(true);
    };

    // Handle command click
    const handleCommandClick = (commandId: string) => {
        useCommandStore.getState().executeCommand(commandId);
    };

    // Handle pin/unpin command
    const handlePinCommand = (commandId: string, isCurrentlyPinned: boolean) => {
        setUserModified(true);

        if (isCurrentlyPinned) {
            removePinnedCommand(commandId);
            savePinnedCommands().catch(err => console.error('Error saving pinned commands after removal:', err));
        } else {
            const command = useCommandStore.getState().getCommandById(commandId);
            if (command) {
                let iconName = undefined;
                if (command.icon && typeof command.icon === 'object') {
                    const iconType = command.icon.type?.name || command.icon.type?.displayName;
                    if (iconType) {
                        iconName = iconType;
                    }
                }

                addPinnedCommand({
                    id: command.id,
                    name: command.name,
                    iconName,
                    iconSet: 'lucide',
                    section: command.section,
                });
            }
        }
    };
    
    // Handle rename command
    const handleRenameCommand = (command: PinnedCommand) => {
        setCommandToRename(command);
        setNewCommandName(command.name);
        setSelectedIconName(command.iconName);
        // Ensure we always have a valid icon set, defaulting to 'lucide' if undefined
        setSelectedIconSet((command.iconSet as IconSet) || 'lucide');
        setRenameDialogOpen(true);
    };
    
    // Handle rename confirmation
    const handleRenameConfirm = () => {
        if (!commandToRename || !newCommandName.trim()) {
            setRenameDialogOpen(false);
            return;
        }
        
        // Update the command name and icon in the store
        const updatedCommands = pinnedCommands.map(cmd => {
            if (cmd.id === commandToRename.id) {
                return { 
                    ...cmd, 
                    name: newCommandName.trim(),
                    iconName: selectedIconName,
                    iconSet: selectedIconSet
                };
            }
            return cmd;
        });
        
        // Set the updated commands and mark as modified
        setPinnedCommands(updatedCommands);
        setIsModified(true);
        
        // Save the changes to the server
        savePinnedCommands().catch(err => console.error('Error saving renamed command:', err));
        
        // Close the dialog
        setRenameDialogOpen(false);
    };
    
    // Handle rename cancellation
    const handleRenameCancel = () => {
        setRenameDialogOpen(false);
    };
    
    // Handle icon selection
    const handleIconSelect = (iconName: string, iconSet: IconSet) => {
        setSelectedIconName(iconName);
        setSelectedIconSet(iconSet);
    };

    // Calculate visible and hidden commands
    const effectiveVisibleCount = autoFit && orientation === 'horizontal' ? visibleCount : maxShown;
    const visibleCommands = pinnedCommands.slice(0, effectiveVisibleCount);
    const hiddenCommands = pinnedCommands.slice(effectiveVisibleCount);
    const hasMoreCommands = pinnedCommands.length > effectiveVisibleCount;

    // Create command rows for multi-row layout
    const commandRows: typeof pinnedCommands[] = [];
    if (orientation === 'horizontal' && autoFit && rowCount > 1) {
        for (let i = 0; i < rowCount; i++) {
            const startIdx = i * itemsPerRow;
            const endIdx = Math.min(startIdx + itemsPerRow, visibleCommands.length);
            commandRows.push(visibleCommands.slice(startIdx, endIdx));
        }
    } else {
        commandRows.push(visibleCommands);
    }

    // Empty state
    if (pinnedCommands.length === 0) {
        return (
            <div
                className={cn(
                    'flex items-center justify-center',
                    orientation === 'vertical' ? 'flex-col h-full' : 'h-8',
                    className,
                )}
            >
                <div className="text-xs text-gray-500 flex items-center gap-1.5">
                    <Pin className="h-3 w-3" />
                    <span className="hidden sm:inline">Pin commands for quick access</span>
                </div>
            </div>
        );
    }

    // Log errors
    if (error) {
        console.error('Error with pinned commands:', error);
    }

    return (
        <TooltipProvider>
            <DragDropContext onDragEnd={handleDragEnd}>
                <div
                    ref={containerRef}
                    className={cn(
                        'PinnedShortcuts flex justify-center gap-1 overflow-x-auto',
                        orientation === 'vertical' ? 'flex-col items-center' : 'flex-col w-full',
                        className,
                    )}
                    style={{
                        fontFamily: 'var(--global-font-family, inherit)',
                        fontSize: 'var(--global-font-size, inherit)',
                        color: 'var(--global-text-color, #e5e7eb)',
                        ...(window?.theme?.PinnedShortcuts?.style || {})
                    }}
                >
                    {/* Multi-row layout */}
                    {orientation === 'horizontal' && autoFit && rowCount > 1 ? (
                        commandRows.map((rowCommands, rowIndex) => (
                            <PinnedShortcutRow
                                key={`row-${rowIndex}`}
                                rowCommands={rowCommands}
                                rowIndex={rowIndex}
                                itemsPerRow={itemsPerRow}
                                orientation={orientation}
                                onCommandClick={handleCommandClick}
                                onPinCommand={handlePinCommand}
                                onRenameCommand={handleRenameCommand}
                                buttonRef={buttonRef}
                            />
                        ))
                    ) : (
                        /* Single row/column layout */
                        <Droppable
                            droppableId="pinned-commands"
                            direction={orientation === 'vertical' ? 'vertical' : 'horizontal'}
                            isCombineEnabled={false}
                            isDropDisabled={false}
                            ignoreContainerClipping={false}
                        >
                            {(provided) => (
                                <div
                                    {...provided.droppableProps}
                                    ref={provided.innerRef}
                                    className={cn('flex items-center justify-center gap-1', orientation === 'vertical' ? 'flex-col' : 'flex-row w-full')}
                                >
                                    {visibleCommands.map((command, index) => (
                                        <PinnedShortcutButton
                                            key={command.id}
                                            command={command}
                                            index={index}
                                            orientation={orientation}
                                            onCommandClick={handleCommandClick}
                                            onPinCommand={handlePinCommand}
                                            onRenameCommand={handleRenameCommand}
                                            isButtonRef={command.id === visibleCommands[0]?.id}
                                            buttonRef={buttonRef}
                                        />
                                    ))}
                                    {provided.placeholder}
                                </div>
                            )}
                        </Droppable>
                    )}

                    {/* Dropdown for overflow commands */}
                    {hasMoreCommands && (
                        <PinnedShortcutDropdown
                            hiddenCommands={hiddenCommands}
                            orientation={orientation}
                            onCommandClick={handleCommandClick}
                            onPinCommand={handlePinCommand}
                            visibleCount={effectiveVisibleCount}
                            totalCount={pinnedCommands.length}
                        />
                    )}
                </div>
                
                {/* Rename Dialog */}
                <RenameShortcutDialog
                    open={renameDialogOpen}
                    onOpenChange={setRenameDialogOpen}
                    commandToRename={commandToRename}
                    newCommandName={newCommandName}
                    setNewCommandName={setNewCommandName}
                    selectedIconName={selectedIconName}
                    selectedIconSet={selectedIconSet}
                    onIconSelect={handleIconSelect}
                    onConfirm={handleRenameConfirm}
                    onCancel={handleRenameCancel}
                />
            </DragDropContext>
        </TooltipProvider>
    );
}

// Expose themeable properties for ThemeManager
export const themeableProps = {
};
