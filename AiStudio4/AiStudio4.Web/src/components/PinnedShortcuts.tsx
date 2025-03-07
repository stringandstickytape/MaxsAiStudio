// src/components/PinnedShortcuts.tsx
import { useEffect, useRef, useState } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { RootState } from '@/store/store';
import { Button } from '@/components/ui/button';
import { commandRegistry } from '@/commands/commandRegistry';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { Pin, Command, ChevronRight, Plus, Settings, RefreshCw, GitBranch, Mic } from 'lucide-react';
import { fetchPinnedCommands, savePinnedCommands } from '@/store/pinnedCommandsSlice';

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
    const dispatch = useDispatch();
    const { pinnedCommands, loading, error } = useSelector((state: RootState) => state.pinnedCommands);
    const clientId = localStorage.getItem('clientId');

    // Track if we've initialized from the server
    const [initialLoadComplete, setInitialLoadComplete] = useState(false);

    // Use a ref to track the previous pinnedCommands state for comparison
    const prevPinnedCommandsRef = useRef<string>('');

    // Track WebSocket connection state to handle refetching
    const wsConnectedRef = useRef<boolean>(false);

    // Load pinned commands from server when component mounts or clientId changes
    useEffect(() => {
        if (clientId) {
            dispatch(fetchPinnedCommands());
        }
    }, [dispatch, clientId]);

    // Add a listener for WebSocket connection changes
    useEffect(() => {
        const handleWebSocketConnected = () => {
            // Only refetch if we've already loaded once (prevents double-loading on startup)
            if (wsConnectedRef.current && initialLoadComplete) {
                console.log('WebSocket reconnected, refreshing pinned commands');
                dispatch(fetchPinnedCommands());
            }
            wsConnectedRef.current = true;
        };

        // Listen for WebSocket open events
        window.addEventListener('ws-connected', handleWebSocketConnected);

        return () => {
            window.removeEventListener('ws-connected', handleWebSocketConnected);
        };
    }, [dispatch, initialLoadComplete]);

    // Set initialLoadComplete flag and update reference when commands change
    useEffect(() => {
        // Only care about non-empty command lists after loading
        if (!loading && pinnedCommands.length > 0) {
            const currentCommandsString = JSON.stringify(pinnedCommands);

            // Always update the reference to latest server value
            prevPinnedCommandsRef.current = currentCommandsString;

            // Mark initial load as complete if not already done
            if (!initialLoadComplete) {
                setInitialLoadComplete(true);
                console.log('Initial pinned commands load complete');
            }
        }
    }, [loading, pinnedCommands, initialLoadComplete]);

    // Flag to indicate changes originating from user actions vs. server loads
    const [userModified, setUserModified] = useState(false);

    // Save pinned commands to server when they change
    useEffect(() => {
        // Skip if loading, initial load is not complete, or not user-modified
        if (loading || !initialLoadComplete || !userModified) {
            return;
        }

        // Clear user modified flag
        setUserModified(false);

        // Debounce to prevent excessive API calls
        const saveTimer = setTimeout(() => {
            console.log('Saving user-modified pinned commands');
            dispatch(savePinnedCommands(pinnedCommands));
        }, 1000);

        return () => clearTimeout(saveTimer);
    }, [pinnedCommands, dispatch, loading, initialLoadComplete, userModified]);

    // Override addPinnedCommand and removePinnedCommand to set userModified flag
    const handleCommandClick = (commandId: string) => {
        commandRegistry.executeCommand(commandId);
    };

    // Wrap original dispatch functions to track user modifications
    const handlePinCommand = (commandId: string, isCurrentlyPinned: boolean) => {
        // Set user modified flag
        setUserModified(true);

        // Then perform the actual dispatch
        if (isCurrentlyPinned) {
            dispatch(removePinnedCommand(commandId));
        } else {
            // Get command details from registry
            const command = commandRegistry.getCommandById(commandId);
            if (command) {
                // Extract icon name from the command if it exists
                let iconName = undefined;
                if (command.icon && typeof command.icon === 'object') {
                    const iconType = command.icon.type?.name || command.icon.type?.displayName;
                    if (iconType) {
                        iconName = iconType;
                    }
                }

                dispatch(addPinnedCommand({
                    id: command.id,
                    name: command.name,
                    iconName,
                    section: command.section
                }));
            }
        }
    };

    // Limit the number of displayed commands
    const visibleCommands = pinnedCommands.slice(0, maxShown);
    const hasMoreCommands = pinnedCommands.length > maxShown;

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

    // Error state
    if (error) {
        console.error("Error with pinned commands:", error);
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
                                onContextMenu={(e) => {
                                    e.preventDefault();
                                    // Remove pin on right-click
                                    handlePinCommand(command.id, true);
                                }}
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