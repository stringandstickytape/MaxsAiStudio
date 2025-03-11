// src/components/AppHeader.tsx
import { Command } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { HeaderPromptComponent } from '@/components/SystemPrompt/HeaderPromptComponent';
import { PinnedShortcuts } from '@/components/PinnedShortcuts';
import { ModelStatusBar } from '@/components/ModelStatusBar';

interface AppHeaderProps {
    onToggleSystemPrompts?: () => void;
    onExecuteCommand?: (command: string) => void;
    isCommandBarOpen?: boolean;
    setIsCommandBarOpen?: (open: boolean) => void;
    CommandBarComponent?: React.ReactNode;
    sidebarPinned?: boolean;
    rightSidebarPinned?: boolean;
    activeConvId?: string | null;
}

export function AppHeader({
    onToggleSystemPrompts,
    onExecuteCommand = () => { },
    isCommandBarOpen = false,
    setIsCommandBarOpen = () => { },
    CommandBarComponent,
    sidebarPinned = false,
    rightSidebarPinned = false,
    activeConvId = null,
}: AppHeaderProps) {
    const [commandText, setCommandText] = useState('');

    const handleCommandSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (commandText.trim()) {
            onExecuteCommand(commandText);
            setCommandText('');
            setIsCommandBarOpen(false);
        }
    };

    useEffect(() => {
        const inputElement = document.getElementById('command-input');
        if (isCommandBarOpen && inputElement) {
            inputElement.focus();
        }
    }, [isCommandBarOpen]);

    // Handle model selection via commands
    const handlePrimaryModelClick = () => {
        setIsCommandBarOpen(true);
        setTimeout(() => {
            const element = document.getElementById('command-input') as HTMLInputElement;
            if (element) {
                element.focus();
                // Pre-populate with the command to select primary model
                element.value = "select primary model";

                // Trigger input event manually to update filtered commands
                const inputEvent = new Event('input', { bubbles: true });
                element.dispatchEvent(inputEvent);

                // Also update the state
                setCommandText("select primary model");
            }
        }, 100);
    };

    const handleSecondaryModelClick = () => {
        setIsCommandBarOpen(true);
        setTimeout(() => {
            const element = document.getElementById('command-input') as HTMLInputElement;
            if (element) {
                element.focus();
                // Pre-populate with the command to select secondary model
                element.value = "select secondary model";

                // Trigger input event manually to update filtered commands
                const inputEvent = new Event('input', { bubbles: true });
                element.dispatchEvent(inputEvent);

                // Also update the state
                setCommandText("select secondary model");
            }
        }, 100);
    };

    return (
        <div className={cn(
            "bg-gradient-to-r from-gray-900 to-gray-800 border-b border-gray-700/50 shadow-xl backdrop-blur-sm p-4 z-20 h-full flex flex-col",
            sidebarPinned ? "left-80" : "left-0",
            rightSidebarPinned ? "right-80" : "right-0"
        )}>
            {/* Main header content */}
            <div className="flex flex-1">
                {/* Left side - Vertical Model Selectors */}
                <div className="flex flex-col justify-center pr-2 border-r border-gray-700/30">
                    <ModelStatusBar
                        onPrimaryClick={handlePrimaryModelClick}
                        onSecondaryClick={handleSecondaryModelClick}
                        orientation="vertical"
                    />
                </div>

                {/* Middle - Command Bar and System Prompt */}
                <div className="flex-1 flex flex-col justify-center px-0">
                    <div className="flex items-center justify-center">
                        {CommandBarComponent || (
                            <form onSubmit={handleCommandSubmit} className="relative w-full max-w-2xl mb-2">
                                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <Command className="h-4 w-4 text-gray-400" />
                                </div>
                                <Input
                                    id="command-input"
                                    type="text"
                                    placeholder="Type a command (/ for suggestions)..."
                                    value={commandText}
                                    onChange={(e) => setCommandText(e.target.value)}
                                    className="w-full shadow-inner transition-all duration-200 input-ghost input-with-icon"
                                    onBlur={() => setIsCommandBarOpen(false)}
                                />
                                <kbd
                                    className="absolute right-3 top-1/2 transform -translate-y-1/2 pointer-events-none hidden sm:inline-flex items-center gap-1 px-2 py-0.5 text-xs font-mono text-gray-400 bg-gray-800 rounded border border-gray-700"
                                    onClick={() => setIsCommandBarOpen(true)}
                                >
                                    {navigator.platform.indexOf('Mac') !== -1 ? '⌘ + K' : 'Ctrl + K'}
                                </kbd>
                            </form>
                        )}
                    </div>

                    <div className="w-full max-w-2xl mx-auto">
                        <HeaderPromptComponent
                            convId={activeConvId || undefined}
                            onOpenLibrary={onToggleSystemPrompts}
                        />
                    </div>
                </div>

                {/* Right side - Empty space for layout balance */}
                <div className="flex flex-col justify-center pl-4 border-l border-gray-700/30 w-[130px]">
                    {/* Intentionally left empty for balance */}
                </div>
            </div>

            {/* Pinned commands bar - positioned below the main header content */}
            <div className="mt-1 border-t border-gray-700/30 pt-1 flex justify-center">
                <div className="w-full">
                    <PinnedShortcuts orientation="horizontal" maxShown={15} className="overflow-x-auto" />
                </div>
            </div>
        </div>
    );
}