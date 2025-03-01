// src/components/AppHeader.tsx
import { Button } from '@/components/ui/button';
import { Menu, Settings, GitBranch, Command } from 'lucide-react';
import { cn } from '@/lib/utils';
import { ModelSelector } from '@/components/ModelSelector';
import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';

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
    onExecuteCommand?: (command: string) => void;
    isCommandBarOpen?: boolean;
    setIsCommandBarOpen?: (open: boolean) => void;
    CommandBarComponent?: React.ReactNode;
    sidebarPinned?: boolean;
    rightSidebarPinned?: boolean;
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
    onExecuteCommand = () => { },
    isCommandBarOpen = false,
    setIsCommandBarOpen = () => { },
    CommandBarComponent,
    sidebarPinned = false,
    rightSidebarPinned = false,
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

    return (
        <div className={cn(
            "bg-gradient-to-r from-gray-900 to-gray-800 border-b border-gray-700/50 shadow-xl backdrop-blur-sm p-4 z-20 flex items-center gap-2 h-full",
            sidebarPinned ? "left-80" : "left-0",
            rightSidebarPinned ? "right-80" : "right-0"
        )}>
            {!sidebarPinned && (
                <div className="absolute left-4">
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onToggleSidebar}
                        className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                    >
                        <Menu className="h-6 w-6" />
                    </Button>
                </div>
            )}

            <div className="flex-1 flex flex-col justify-center items-center gap-2 pl-12 pr-20">
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

                <div className="flex gap-3 items-center w-full justify-center py-1">
                    <ModelSelector
                        label="Primary AI"
                        selectedModel={selectedModel}
                        models={models}
                        modelType="primary"
                        onModelSelect={onModelSelect}
                    />

                    <ModelSelector
                        label="Secondary AI"
                        selectedModel={secondaryModel}
                        models={models}
                        modelType="secondary"
                        onModelSelect={onSecondaryModelSelect}
                    />
                </div>
            </div>

            <div className={cn(`absolute right-4 flex items-center space-x-2 transition-all duration-300 z-10`)}>
                {onToggleConversationTree && (
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onToggleConversationTree}
                        className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                    >
                        <GitBranch className="h-5 w-5" />
                    </Button>
                )}
                {onToggleSettings && (
                    <Button
                        variant="ghost"
                        size="icon"
                        onClick={onToggleSettings}
                        className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5"
                    >
                        <Settings className="h-5 w-5" />
                    </Button>
                )}
            </div>
        </div>
    );
}