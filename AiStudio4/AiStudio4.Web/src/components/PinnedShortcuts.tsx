import { useEffect, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { useCommandStore } from '@/stores/useCommandStore';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { Pin, Command, ChevronDown, Plus, Settings, RefreshCw, GitBranch, Mic } from 'lucide-react';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuGroup,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { DragDropContext, Droppable, Draggable, DropResult } from 'react-beautiful-dnd';

interface PinnedShortcutsProps {
    orientation?: 'horizontal' | 'vertical';
    maxShown?: number;
    className?: string;
    autoFit?: boolean;
    maxRows?: number;
}

const getIconForCommand = (commandId: string, iconName?: string) => {
    const iconProps = { className: "h-3.5 w-3.5" };

    if (iconName) {
        const iconMap: Record<string, JSX.Element> = {
            'Plus': <Plus {...iconProps} />,
            'Settings': <Settings {...iconProps} />,
            'RefreshCw': <RefreshCw {...iconProps} />,
            'GitBranch': <GitBranch {...iconProps} />,
            'Mic': <Mic {...iconProps} />
        };
        return iconMap[iconName] || <Command {...iconProps} />;
    }

    return commandId.includes('new') ? <Plus {...iconProps} /> :
           commandId.includes('settings') ? <Settings {...iconProps} /> :
           commandId.includes('clear') || commandId.includes('reset') ? <RefreshCw {...iconProps} /> :
           commandId.includes('tree') ? <GitBranch {...iconProps} /> :
           commandId.includes('voice') ? <Mic {...iconProps} /> :
           <Command {...iconProps} />;
}

const getCategoryBorderColor = (section?: string) => {
        switch(section) {
            case 'conv': return 'border-blue-500/70';
            case 'model': return 'border-purple-500/70';
            case 'view': return 'border-green-500/70';
            case 'settings': return 'border-yellow-500/70';
            case 'utility': return 'border-orange-500/70';
            case 'appearance': return 'border-pink-500/70';
            default: return 'border-gray-700/40';
        }
    };

    const getCategoryBackgroundColor = (section?: string) => {
        switch(section) {
            case 'conv': return 'bg-blue-900/20';
            case 'model': return 'bg-purple-900/20';
            case 'view': return 'bg-green-900/20';
            case 'settings': return 'bg-yellow-900/20';
            case 'utility': return 'bg-orange-900/20';
            case 'appearance': return 'bg-pink-900/20';
            default: return 'bg-gray-800/60';
        }
    };

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

    useEffect(() => {
        if (clientId) {
            fetchPinnedCommands();
        }
    }, [fetchPinnedCommands, clientId]);

    useEffect(() => {
        const handleWebSocketConnected = () => {
            if (wsConnectedRef.current && initialLoadComplete) {
                console.log('WebSocket reconnected, refreshing pinned commands');
                fetchPinnedCommands();
            }
            wsConnectedRef.current = true;
        };

        window.addEventListener('ws-connected', handleWebSocketConnected);

        return () => {
            window.removeEventListener('ws-connected', handleWebSocketConnected);
        };
    }, [fetchPinnedCommands, initialLoadComplete]);

    useEffect(() => {
        if (!loading && pinnedCommands.length > 0) {
            const currentCommandsString = JSON.stringify(pinnedCommands);

            prevPinnedCommandsRef.current = currentCommandsString;

            if (!initialLoadComplete) {
                setInitialLoadComplete(true);
                console.log('Initial pinned commands load complete');
            }
        }
    }, [loading, pinnedCommands, initialLoadComplete]);

    const [userModified, setUserModified] = useState(false);

    const isModified = usePinnedCommandsStore(state => state.isModified);

    useEffect(() => {
        if (loading || !initialLoadComplete || !isModified) {
            return;
        }

        const saveTimer = setTimeout(() => {
            console.log('Saving modified pinned commands to server');
            savePinnedCommands();
        }, 1000);

        return () => clearTimeout(saveTimer);
    }, [pinnedCommands, savePinnedCommands, loading, initialLoadComplete, isModified]);

    useEffect(() => {
        if (!autoFit || orientation === 'vertical' || !containerRef.current || !buttonRef.current) {
            return;
        }

        const calculateVisibleButtons = () => {
            const containerWidth = containerRef.current?.clientWidth || 0;
            const buttonWidth = 160 + 8;
            const dropdownWidth = 30;

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

    const handleDragEnd = (result: DropResult) => {
        if (!result.destination) return;

        const items = Array.from(pinnedCommands);

        const [reorderedItem] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, reorderedItem);

        const reorderedIds = items.map((cmd) => cmd.id);

        reorderPinnedCommands(reorderedIds);

        setUserModified(true);


    };

    const handleCommandClick = (commandId: string) => {
        useCommandStore.getState().executeCommand(commandId);
    };

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
                    section: command.section,
                });
            }
        }
    };

    const effectiveVisibleCount = autoFit && orientation === 'horizontal' ? visibleCount : maxShown;

    const visibleCommands = pinnedCommands.slice(0, effectiveVisibleCount);
    const hiddenCommands = pinnedCommands.slice(effectiveVisibleCount);
    const hasMoreCommands = pinnedCommands.length > effectiveVisibleCount;


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
                    style={window?.theme?.PinnedShortcuts?.style || {}}
                >
                    {orientation === 'horizontal' && autoFit && rowCount > 1 ? (

                        commandRows.map((rowCommands, rowIndex) => (
                            <div key={`row-${rowIndex}`} className="flex flex-row items-center justify-center gap-1 w-full">
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
                                                    <Draggable key={command.id} draggableId={command.id} index={globalIndex}>
                                                        {(provided, snapshot) => (
                                                            <div
                                                                ref={provided.innerRef}
                                                                {...provided.draggableProps}
                                                                className={cn('flex flex-col items-center group', snapshot.isDragging && 'opacity-70 z-50')}
                                                            >
                                                                <div
                                                                    {...provided.dragHandleProps}
                                                                    className="cursor-grab h-2 w-[38px] text-gray-500 hover:text-gray-300 flex items-center justify-center rounded hover:bg-gray-700 transition-colors opacity-0 group-hover:opacity-100"
                                                                >
                                                                    <div className="w-12 flex items-center justify-center">
                                                                        <div className="h-[3px] w-8 bg-current rounded-full"></div>
                                                                    </div>
                                                                </div>
                                                                <Tooltip key={command.id} delayDuration={300}>
                                                                    <TooltipTrigger asChild>
                                                                        <Button
                                                                            ref={command.id === visibleCommands[0]?.id ? buttonRef : null}
                                                                            variant="ghost"
                                                                            onClick={() => handleCommandClick(command.id)}
                                                                            onContextMenu={(e) => {
                                                                                e.preventDefault();
                                                                                handlePinCommand(command.id, true);
                                                                            }}
                                                                            className={`PinnedShortcuts h-auto min-h-[20px] max-h-[36px] w-[160px] px-0 py-0 rounded-md ${getCategoryBackgroundColor(command.section)} hover:bg-opacity-30 flex flex-row items-center justify-center relative`}
                                                                            style={{
                                                                                '--hover-text-color': 'var(--pinnedshortcuts-text-color-hover, #f9fafb)',
                                                                                border: `var(--pinnedshortcuts-border-width, 1px) var(--pinnedshortcuts-border-style, solid) var(--pinnedshortcuts-border-color, ${getCategoryBorderColor(command.section).replace('border-', '')})`,
                                                                                color: 'var(--pinnedshortcuts-text-color, #e5e7eb)',
                                                                                fontWeight: 'var(--pinnedshortcuts-font-weight, 500)',
                                                                                fontFamily: 'var(--pinnedshortcuts-font-family, inherit)',
                                                                                ...(window?.theme?.PinnedShortcuts?.buttonStyle || {})
                                                                            }}
                                                                        >
                                                                            <span className="text-xs flex-1 text-center leading-tight break-words whitespace-nowrap overflow-hidden"
                                                                                  style={{
                                                                                      fontWeight: 'var(--pinnedshortcuts-font-weight, 500)',
                                                                                  }}>
                                                                                {command.name}
                                                                            </span>
                                                                        </Button>
                                                                    </TooltipTrigger>
                                                                    <TooltipContent side="bottom">
                                                                        <p>{command.name}</p>
                                                                        <p className="text-xs text-gray-400">
                                                                            Category: {command.section || 'Unknown'}
                                                                        </p>
                                                                        <p className="text-xs text-gray-400">
                                                                            Drag handle above button to reorder · Right-click to unpin
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </div>
                                                        )}
                                                    </Draggable>
                                                );
                                            })}
                                            {provided.placeholder}
                                        </div>
                                    )}
                                </Droppable>
                            </div>
                        ))
                    ) : (

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
                                        <Draggable key={command.id} draggableId={command.id} index={index}>
                                            {(provided, snapshot) => (
                                                <div
                                                    ref={provided.innerRef}
                                                    {...provided.draggableProps}
                                                    className={cn('flex flex-col items-center group', snapshot.isDragging && 'opacity-70 z-50')}
                                                >
                                                    <div
                                                        {...provided.dragHandleProps}
                                                        className="cursor-grab h-2 w-[38px] text-gray-500 hover:text-gray-300 flex items-center justify-center rounded hover:bg-gray-700 transition-colors opacity-0 group-hover:opacity-100"
                                                    >
                                                        <div className="w-12 flex items-center justify-center">
                                                            <div className="h-[3px] w-8 bg-current rounded-full"></div>
                                                        </div>
                                                    </div>
                                                    <Tooltip key={command.id} delayDuration={300}>
                                                        <TooltipTrigger asChild>
                                                            <Button
                                                                ref={command.id === visibleCommands[0]?.id ? buttonRef : null}
                                                                variant="ghost"
                                                                onClick={() => handleCommandClick(command.id)}
                                                                onContextMenu={(e) => {
                                                                    e.preventDefault();
                                                                    handlePinCommand(command.id, true);
                                                                }}
                                                                className={`PinnedShortcuts h-auto min-h-[32px] max-h-[36px] w-[160px] px-0.5 py-0.5 rounded-md ${getCategoryBackgroundColor(command.section)} hover:bg-opacity-30 flex flex-row items-center justify-center relative`}
                                                                style={{
                                                                    '--hover-text-color': 'var(--pinnedshortcuts-text-color-hover, #f9fafb)',
                                                                    border: `var(--pinnedshortcuts-border-width, 1px) var(--pinnedshortcuts-border-style, solid) var(--pinnedshortcuts-border-color, ${getCategoryBorderColor(command.section).replace('border-', '')})`,
                                                                    color: 'var(--pinnedshortcuts-text-color, #e5e7eb)',
                                                                    fontWeight: 'var(--pinnedshortcuts-font-weight, 500)',
                                                                    fontFamily: 'var(--pinnedshortcuts-font-family, inherit)',
                                                                    ...(window?.theme?.PinnedShortcuts?.buttonStyle || {})
                                                                }}
                                                            >
                                                                <span className="text-xs flex-1 text-center leading-tight break-words whitespace-nowrap overflow-hidden"
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
                                                                Drag handle above button to reorder · Right-click to unpin
                                                            </p>
                                                        </TooltipContent>
                                                    </Tooltip>
                                                </div>
                                            )}
                                        </Draggable>
                                    ))}
                                    {provided.placeholder}
                                </div>

                            )}
                        </Droppable>)}

                    {hasMoreCommands && (
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
                                    <p>More commands ({pinnedCommands.length - effectiveVisibleCount})</p>
                                </TooltipContent>
                            </Tooltip>
                            <DropdownMenuContent align="end" className="w-48 max-h-[50vh] overflow-y-auto">
                                <DropdownMenuGroup>
                                    {hiddenCommands.map((command) => (
                                        <DropdownMenuItem
                                            key={command.id}
                                            onClick={() => handleCommandClick(command.id)}
                                            onContextMenu={(e) => {
                                                e.preventDefault();
                                                handlePinCommand(command.id, true);
                                            }}
                                            className="flex items-center gap-1 text-xs whitespace-normal"
                                        >
                                            <span>{command.name}</span>
                                        </DropdownMenuItem>
                                    ))}
                                </DropdownMenuGroup>
                            </DropdownMenuContent>
                        </DropdownMenu>
                    )}
                </div>
            </DragDropContext>
        </TooltipProvider>
    );
}

// Expose themeable properties for ThemeManager
export const themeableProps = {
  borderColor: {
    cssVar: '--pinnedshortcuts-border-color',
    description: 'Border color for pinned shortcut buttons',
    default: '', // Default will use category-based colors
  },
  borderStyle: {
    cssVar: '--pinnedshortcuts-border-style',
    description: 'Border style for pinned shortcut buttons',
    default: 'solid',
  },
  borderWidth: {
    cssVar: '--pinnedshortcuts-border-width',
    description: 'Border width for pinned shortcut buttons',
    default: '1px',
  },
  textColor: {
    cssVar: '--pinnedshortcuts-text-color',
    description: 'Text color for pinned shortcut buttons',
    default: '#e5e7eb', // Equivalent to text-gray-300
  },
  textColorHover: {
    cssVar: '--pinnedshortcuts-text-color-hover',
    description: 'Text color for pinned shortcut buttons on hover',
    default: '#f9fafb', // Equivalent to text-gray-100
  },
  fontWeight: {
    cssVar: '--pinnedshortcuts-font-weight',
    description: 'Font weight for pinned shortcut buttons',
    default: '500', // Equivalent to font-medium
  },
  fontFamily: {
    cssVar: '--pinnedshortcuts-font-family',
    description: 'Font family for pinned shortcut buttons',
    default: 'inherit',
  },
  style: {
    description: 'Arbitrary CSS style for the root PinnedShortcuts container',
    default: {},
  },
  buttonStyle: {
    description: 'Arbitrary CSS style for individual shortcut buttons',
    default: {},
  },
};