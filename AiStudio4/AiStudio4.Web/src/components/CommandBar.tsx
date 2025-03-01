// src/components/CommandBar.tsx
import React, { useState, useEffect, useRef } from 'react';
import { Command } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { commandRegistry } from '@/commands/commandRegistry';
import { Command as CommandType } from '@/commands/types';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { cn } from '@/lib/utils';

interface CommandBarProps {
    isOpen: boolean;
    setIsOpen: (open: boolean) => void;
}

export function CommandBar({ isOpen, setIsOpen }: CommandBarProps) {
    const [searchTerm, setSearchTerm] = useState('');
    const [filteredCommands, setFilteredCommands] = useState<CommandType[]>([]);
    const [selectedIndex, setSelectedIndex] = useState(0);
    const inputRef = useRef<HTMLInputElement>(null);

    // Update filtered commands when search term changes
    useEffect(() => {
        setFilteredCommands(commandRegistry.searchCommands(searchTerm));
        setSelectedIndex(0);
        
        // If user starts typing, ensure the dropdown is visible
        if (searchTerm.length > 0 && !isOpen) {
            setIsOpen(true);
        }
    }, [searchTerm, isOpen]);

    // Subscribe to command registry changes
    useEffect(() => {
        const unsubscribe = commandRegistry.subscribe(() => {
            setFilteredCommands(commandRegistry.searchCommands(searchTerm));
        });

        return unsubscribe;
    }, [searchTerm]);

    // Focus input when opened
    useEffect(() => {
        if (isOpen && inputRef.current) {
            inputRef.current.focus();
        }
    }, [isOpen]);

    const handleCommandSubmit = (e: React.FormEvent) => {
        e.preventDefault();

        if (filteredCommands.length > 0 && selectedIndex >= 0) {
            const selectedCommand = filteredCommands[selectedIndex];
            commandRegistry.executeCommand(selectedCommand.id);
            setSearchTerm('');
            setIsOpen(false);
        }
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            setSelectedIndex(prev =>
                prev < filteredCommands.length - 1 ? prev + 1 : prev
            );
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setSelectedIndex(prev => prev > 0 ? prev - 1 : 0);
        } else if (e.key === 'Escape') {
            setIsOpen(false);
        }
    };

    const handleCommandClick = (commandId: string) => {
        commandRegistry.executeCommand(commandId);
        setSearchTerm('');
        setIsOpen(false);
    };

    // Group commands by section
    const groupedCommands = filteredCommands.reduce((acc, command) => {
        if (!acc[command.section]) {
            acc[command.section] = [];
        }
        acc[command.section].push(command);
        return acc;
    }, {} as Record<string, CommandType[]>);

    return (
        <div className="relative w-full max-w-2xl">
            <form onSubmit={handleCommandSubmit}>
                <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <Command className="h-4 w-4 text-gray-400" />
                    </div>
                    <Input
                        ref={inputRef}
                        id="command-input"
                        type="text"
                        placeholder="Type a command or search..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        onKeyDown={handleKeyDown}
                        className="w-full pl-10 pr-4 py-2 bg-gray-800/60 border border-gray-700/50 text-gray-100 rounded-lg shadow-inner focus:ring-2 focus:ring-indigo-500/40 focus:border-transparent transition-all duration-200 placeholder:text-gray-400"
                        onBlur={() => setTimeout(() => setIsOpen(false), 200)}
                    />
                    <kbd
                        className="absolute right-3 top-1/2 transform -translate-y-1/2 pointer-events-none hidden sm:inline-flex items-center gap-1 px-2 py-0.5 text-xs font-mono text-gray-400 bg-gray-800 rounded border border-gray-700"
                    >
                        {navigator.platform.indexOf('Mac') !== -1 ? '⌘ + K' : 'Ctrl + K'}
                    </kbd>
                </div>
            </form>

            {isOpen && (searchTerm.length > 0 || filteredCommands.length > 0) && (
                <div className="absolute top-full left-0 right-0 mt-2 bg-gray-800 border border-gray-700 rounded-lg shadow-lg overflow-hidden z-50 max-h-96 overflow-y-auto">
                    {Object.entries(groupedCommands).map(([section, commands]) => (
                        <div key={section} className="border-t border-gray-700 first:border-t-0">
                            <div className="px-3 py-2 text-xs font-semibold text-gray-400 bg-gray-900/70">
                                {section.charAt(0).toUpperCase() + section.slice(1)}
                            </div>
                            <div>
                                {commands.map((command, index) => {
                                    const isSelected = filteredCommands.indexOf(command) === selectedIndex;
                                    return (
                                        <div
                                            key={command.id}
                                            className={cn(
                                                "px-4 py-2 flex items-center justify-between cursor-pointer hover:bg-gray-700/50",
                                                isSelected && "bg-gray-700/70"
                                            )}
                                            onClick={() => handleCommandClick(command.id)}
                                        >
                                            <div className="flex items-center gap-3">
                                                {command.icon && <div className="text-gray-400">{command.icon}</div>}
                                                <div>
                                                    <div className="font-medium text-gray-200">{command.name}</div>
                                                    {command.description && (
                                                        <div className="text-xs text-gray-400">{command.description}</div>
                                                    )}
                                                </div>
                                            </div>
                                            {command.shortcut && (
                                                <kbd className="px-2 py-0.5 text-xs font-mono text-gray-400 bg-gray-800 rounded border border-gray-700">
                                                    {command.shortcut}
                                                </kbd>
                                            )}
                                        </div>
                                    );
                                })}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}