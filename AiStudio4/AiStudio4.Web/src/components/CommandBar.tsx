// src/components/CommandBar.tsx
import React, { useState, useEffect, useRef } from 'react';
import { createPortal } from 'react-dom';
import { Command, Pin } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { useCommandStore } from '@/stores/useCommandStore';
import { Command as CommandType } from '@/commands/types';
import { cn } from '@/lib/utils';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';

interface CommandBarProps {
  isOpen: boolean;
  setIsOpen: (open: boolean) => void;
}

export function CommandBar({ isOpen, setIsOpen }: CommandBarProps) {
  
  const { searchCommands, executeCommand } = useCommandStore();

  
  const { pinnedCommands, addPinnedCommand, removePinnedCommand, savePinnedCommands } = usePinnedCommandsStore();

  const [searchTerm, setSearchTerm] = useState('');
  const [filteredCommands, setFilteredCommands] = useState<CommandType[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  
  useEffect(() => {
    
    const isShortcutSearch = searchTerm.startsWith('/') && !searchTerm.includes(' ');
    
    let newCommands;
    if (isShortcutSearch) {
      
      const allCommands = searchCommands('');
      newCommands = allCommands.filter(cmd => {
        
        return cmd.name.includes('[/') || 
               cmd.name.includes('[ /') || 
               cmd.keywords.some(k => k === searchTerm.substring(1)) ||
               cmd.section === 'utility'; 
      });
      
      
      if (newCommands.length <= 2) {
        newCommands = searchCommands(searchTerm);
      }
    } else {
      newCommands = searchCommands(searchTerm);
    }
    
    setFilteredCommands(newCommands);
    setSelectedIndex(0);
    if (searchTerm && !isOpen) setIsOpen(true);
  }, [searchTerm, isOpen, setIsOpen, searchCommands]);

  
  useEffect(() => {
    const unsubscribe = useCommandStore.subscribe(() => {
      setFilteredCommands(searchCommands(searchTerm));
    });

    return () => unsubscribe();
  }, [searchTerm, searchCommands]);

  
  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();

      
      
      if (searchTerm) {
        setFilteredCommands(searchCommands(searchTerm));
      }
    }
  }, [isOpen, searchCommands, searchTerm]);

  
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (isOpen && containerRef.current && !containerRef.current.contains(event.target as Node)) {
        // Check if the click is on the dropdown menu (portal)
        const dropdownMenu = document.querySelector('.command-dropdown-menu');
        if (dropdownMenu && dropdownMenu.contains(event.target as Node)) {
          // Click is on the dropdown menu, don't close
          return;
        }
        
        // Otherwise close the command bar
        setIsOpen(false);
        setSearchTerm('');
      }
    };

    
    document.addEventListener('mouseup', handleClickOutside);

    return () => {
      document.removeEventListener('mouseup', handleClickOutside);
    };
  }, [isOpen, setIsOpen]);

  
  useEffect(() => {
    const handleInput = () => {
      if (inputRef.current) {
        
        const inputValue = inputRef.current.value;
        if (inputValue !== searchTerm) {
          setSearchTerm(inputValue);
          const newCommands = searchCommands(inputValue);
          setFilteredCommands(newCommands);
          setSelectedIndex(0);
        }
      }
    };

    const inputElement = inputRef.current;
    if (inputElement) {
      inputElement.addEventListener('input', handleInput);
    }

    return () => {
      if (inputElement) {
        inputElement.removeEventListener('input', handleInput);
      }
    };
  }, [searchTerm, searchCommands]);

  const handleCommandSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (filteredCommands.length && selectedIndex >= 0) {
      executeCommand(filteredCommands[selectedIndex].id);
      setSearchTerm('');
      setIsOpen(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((prev) => Math.min(prev + 1, filteredCommands.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((prev) => Math.max(prev - 1, 0));
    } else if (e.key === 'Escape') {
      setIsOpen(false);
    }
  };

  const handleCommandClick = (commandId: string) => {

    executeCommand(commandId);
    setSearchTerm('');
    setIsOpen(false);
  };

  const handlePinCommand = async (e: React.MouseEvent, command: CommandType) => {
    e.stopPropagation();

    const isPinned = pinnedCommands.some((cmd) => cmd.id === command.id);

    if (isPinned) {
      removePinnedCommand(command.id);
    } else {
      
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

    
    await savePinnedCommands();
  };

  
  const groupedCommands = filteredCommands.reduce(
    (acc, command) => {
      acc[command.section] = acc[command.section] || [];
      acc[command.section].push(command);
      return acc;
    },
    {} as Record<string, CommandType[]>,
  );

  return (
    <div ref={containerRef} className="relative w-full max-w-2xl">
      <form onSubmit={handleCommandSubmit}>
        <div className="relative">
         <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
        <Command className="h-4 w-4 text-gray-400" />
        </div>
        <Input
            ref={inputRef}
            id="command-input"
            autoComplete="off"
            type="text"
            placeholder="Type a command or search..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            onKeyDown={handleKeyDown}
            className="w-full shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost input-with-icon pl-9"
        />
          {isOpen ? (
            <button
              type="button"
              onClick={() => {
                setSearchTerm('');
                setIsOpen(false);
              }}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 flex items-center gap-1.5 px-3 py-1 text-gray-300 hover:text-white bg-gray-700/60 hover:bg-gray-700/90 rounded border border-gray-600/50 hover:border-gray-500 animate-hover text-xs font-medium"
              aria-label="Clear and close"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="12"
                height="12"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <line x1="18" y1="6" x2="6" y2="18"></line>
                <line x1="6" y1="6" x2="18" y2="18"></line>
              </svg>
              <span>Close</span>
            </button>
          ) : (
            <kbd className="absolute right-3 top-1/2 transform -translate-y-1/2 pointer-events-none hidden sm:inline-flex items-center gap-1 px-2 py-0.5 text-xs font-mono text-gray-400 bg-gray-800 rounded border border-gray-700">
              {navigator.platform.indexOf('Mac') !== -1 ? '⌘ + K' : 'Ctrl + K'}
            </kbd>
          )}
        </div>
      </form>
          {isOpen && (searchTerm || filteredCommands.length) && createPortal(
              <div className="fixed z-50 command-dropdown-menu" style={{
                  top: inputRef.current ? inputRef.current.getBoundingClientRect().bottom + 8 : 0,
                  left: inputRef.current ? inputRef.current.getBoundingClientRect().left : 0,
                  width: inputRef.current ? inputRef.current.getBoundingClientRect().width : 'auto',
                  maxWidth: '100vw'
              }}>
        <div className="absolute top-full left-0 right-0 mt-2 bg-gray-800 border border-gray-700 rounded-lg shadow-lg overflow-hidden z-50 max-h-[70vh] overflow-y-auto">
          {Object.entries(groupedCommands).map(([section, commands]) => (
            <div key={section} className="border-t border-gray-700 first:border-t-0">
              <div className="px-3 py-2 text-xs font-semibold text-gray-400 bg-gray-900/70">
                {section.charAt(0).toUpperCase() + section.slice(1)}
              </div>
              <div>
                {commands.map((command, index) => {
                  const isSelected = filteredCommands.indexOf(command) === selectedIndex;
                  const isPinned = pinnedCommands.some((cmd) => cmd.id === command.id);

                    return (
                        <div
                            key={command.id}
                            className={cn(
                                'px-4 py-2 flex items-center justify-between cursor-pointer hover:bg-gray-700/50',
                                isSelected && 'bg-gray-700/70',
                            )}
                            onMouseDown={(e) => {
                                e.preventDefault(); 
                            }}
                            onMouseUp={(e) => {
                                e.stopPropagation(); 
                                handleCommandClick(command.id);
                            }}
                        >
                      <div className="flex items-center gap-3">
                        {command.icon && <div className="text-gray-400">{command.icon}</div>}
                      <div className="max-w-md overflow-hidden">
                          <div className="font-medium text-gray-200">{command.name}</div>
                          {command.description && (
                            <div className="text-small-gray-400 whitespace-pre-line line-clamp-3">
                              {command.description}
                            </div>
                          )}
                      </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <TooltipProvider>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <div
                                onMouseDown={(e) => {
                                  e.stopPropagation();
                                  e.preventDefault();
                                }}
                                onMouseUp={(e) => {
                                  e.stopPropagation();
                                  handlePinCommand(e, command);
                                }}
                                className={cn(
                                  'p-1 rounded hover:bg-gray-600/50 cursor-pointer',
                                  isPinned ? 'text-blue-400' : 'text-gray-500 hover:text-gray-300',
                                )}
                              >
                                <Pin className="h-3.5 w-3.5" fill={isPinned ? 'currentColor' : 'none'} />
                              </div>
                            </TooltipTrigger>
                            <TooltipContent side="top">
                              <p>{isPinned ? 'Unpin command' : 'Pin to shortcuts'}</p>
                            </TooltipContent>
                          </Tooltip>
                        </TooltipProvider>
                        {command.shortcut && (
                          <kbd className="px-2 py-0.5 text-xs font-mono text-gray-400 bg-gray-800 rounded border border-gray-700">
                            {command.shortcut}
                          </kbd>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          ))}
                  </div>
              </div>,
              document.body
      )}
    </div>
  );
}

