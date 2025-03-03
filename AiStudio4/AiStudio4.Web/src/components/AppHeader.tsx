// src/components/AppHeader.tsx
import { Button } from '@/components/ui/button';
import { Menu, Settings, GitBranch, Command, Wrench as ToolIcon, MessageSquare } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { HeaderPromptComponent } from '@/components/SystemPrompt/HeaderPromptComponent';
import { PinnedShortcuts } from '@/components/PinnedShortcuts';
import { ModelStatusBar } from '@/components/ModelStatusBar';
import { commandRegistry } from '@/commands/commandRegistry';

interface AppHeaderProps {
    isMobile: boolean;
    selectedModel: string;
    secondaryModel: string;
    models: string[];
    onToggleSidebar: () => void;
    onModelSelect: (model: string) => void;
    onSecondaryModelSelect: (model: string) => void;
    onToggleConversationTree: () => void;
    onToggleSettings: () => void;
    onToggleToolPanel?: () => void;
    onToggleSystemPrompts?: () => void;
    onManageTools?: () => void;
    onExecuteCommand?: (command: string) => void;
    isCommandBarOpen?: boolean;
    setIsCommandBarOpen?: (open: boolean) => void;
    CommandBarComponent?: React.ReactNode;
    sidebarPinned?: boolean;
    rightSidebarPinned?: boolean;
    activeConversationId?: string | null;
}

export function AppHeader({
    isMobile,
    selectedModel,
    secondaryModel,
    models,
    onToggleSidebar,
    onModelSelect,
    onSecondaryModelSelect,
    onToggleConversationTree,
    onToggleSettings,
    onToggleToolPanel,
    onToggleSystemPrompts,
    onManageTools,
    onExecuteCommand = () => { },
    isCommandBarOpen = false,
    setIsCommandBarOpen = () => { },
    CommandBarComponent,
    sidebarPinned = false,
    rightSidebarPinned = false,
    activeConversationId = null,
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
            const element = document.getElementById('command-input');
            if (element) {
                element.focus();
                // Pre-populate with the command to select primary model
                (element as HTMLInputElement).value = "select primary model";
                // Simulate an input event to trigger filtering
                element.dispatchEvent(new Event('input', { bubbles: true }));
            }
        }, 100);
    };

    const handleSecondaryModelClick = () => {
        setIsCommandBarOpen(true);
        setTimeout(() => {
            const element = document.getElementById('command-input');
            if (element) {
                element.focus();
                // Pre-populate with the command to select secondary model
                (element as HTMLInputElement).value = "select secondary model";
                // Simulate an input event to trigger filtering
                element.dispatchEvent(new Event('input', { bubbles: true }));
            }
        }, 100);
    };

    return (
        <div className={cn(
            "bg-gradient-to-r from-gray-900 to-gray-800 border-b border-gray-700/50 shadow-xl backdrop-blur-sm p-4 z-20 flex items-center gap-2 h-full",
            sidebarPinned ? "left-80" : "left-0",
            rightSidebarPinned ? "right-80" : "right-0"
        )}>
            <div className="flex-1 flex flex-col justify-center items-center gap-2">
                {CommandBarComponent || (
                    <form onSubmit={handleCommandSubmit} className="relative w-full max-w-2xl">
                        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                            <Command className="h-4 w-4 text-gray-400" />
                        </div>
                        <Input
                            id="command-input"
                            type="text"
                            placeholder="Type a command (/ for suggestions)..."
                            value={commandText}
                            onChange={(e) => setCommandText(e.target.value)}
                            className="w-full pl-10 pr-4 py-2 bg-gray-800/60 border border-gray-700/50 text-gray-100 rounded-lg shadow-inner focus:ring-2 focus:ring-indigo-500/40 focus:border-transparent transition-all duration-200"
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

                <div className="flex flex-col gap-2 w-full py-1">
                    {/* Replace ModelSelector components with ModelStatusBar */}
                    <ModelStatusBar
                        primaryModel={selectedModel}
                        secondaryModel={secondaryModel}
                        onPrimaryClick={handlePrimaryModelClick}
                        onSecondaryClick={handleSecondaryModelClick}
                    />

                    <div className="flex items-center justify-between w-full">
                        <HeaderPromptComponent
                            conversationId={activeConversationId || undefined}
                            onOpenLibrary={onToggleSystemPrompts}
                        />

                        <PinnedShortcuts className="ml-2" />
                    </div>
                </div>
            </div>
        </div>
    );
}