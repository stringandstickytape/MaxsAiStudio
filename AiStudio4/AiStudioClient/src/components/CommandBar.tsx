export const themeableProps = {
};

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
        
        const dropdownMenu = document.querySelector('.command-dropdown-menu');
        if (dropdownMenu && dropdownMenu.contains(event.target as Node)) {
          
          return;
        }
        
        
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

  const scrollSelectedItemIntoView = () => {
    if (selectedIndex < 0) return;
    
    const dropdown = document.querySelector('.command-dropdown-menu .overflow-y-auto');
    if (!dropdown) return;
    
    // Find all command items
    const allItems = Array.from(dropdown.querySelectorAll('[class*="bg-gray-700"]'));
    
    // If we have items and the selectedIndex is valid
    if (allItems.length > 0 && selectedIndex < filteredCommands.length) {
      // Find the correct item - we need to count only items that are command entries
      let currentIndex = 0;
      let targetItem = null;
      
      // Find all command containers
      const commandContainers = dropdown.querySelectorAll('.px-4.py-2.flex.items-center.justify-between');
      
      // Loop through and find the one at our selected index
      if (commandContainers && commandContainers.length > 0) {
        if (selectedIndex < commandContainers.length) {
          targetItem = commandContainers[selectedIndex];
        }
      }
      
      if (targetItem) {
        targetItem.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
      }
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((prev) => Math.min(prev + 1, filteredCommands.length - 1));
      scrollSelectedItemIntoView();
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((prev) => Math.max(prev - 1, 0));
      scrollSelectedItemIntoView();
    } else if (e.key === 'Escape') {
      setIsOpen(false);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (filteredCommands.length && selectedIndex >= 0) {
        executeCommand(filteredCommands[selectedIndex].id);
        setSearchTerm('');
        setIsOpen(false);
      }
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
    <div
      ref={containerRef}
      className="relative w-full max-w-2xl CommandBar"
      style={{
        backgroundColor: 'var(--global-background-color, #222)',
        color: 'var(--global-text-color, #eee)',
        borderColor: 'var(--global-border-color, #555)',
        borderRadius: 'var(--global-border-radius, 8px)',
        fontFamily: 'var(--global-font-family, inherit)',
        fontSize: 'var(--global-font-size, 0.875rem)',
        boxShadow: 'var(--global-box-shadow, 0 4px 12px rgba(0,0,0,0.3))',
        ...(window?.theme?.CommandBar?.style || {})
      }}
    >
      <form onSubmit={handleCommandSubmit} onKeyDown={handleKeyDown}>
        <div className="relative">
         <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
        <Command className="h-4 w-4" />
        </div>
        <Input
            ref={inputRef}
            id="command-input"
            autoComplete="off"
            type="text"
            placeholder="Type a command or search..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            style={{
              backgroundColor: 'var(--global-background-color, #444)',
              color: 'var(--global-text-color, #eee)',
              fontFamily: 'var(--global-font-family, inherit)',
              fontSize: 'var(--global-font-size, 0.875rem)'
            }}
            className="w-full shadow-inner transition-all duration-200 placeholder:text-gray-400 input-ghost input-with-icon pl-9"
        />
          {isOpen ? (
            <button
              type="button"
              onClick={() => {
                setSearchTerm('');
                setIsOpen(false);
              }}
              style={{
                backgroundColor: 'var(--global-background-color, #555)',
                color: 'var(--global-text-color, #eee)'
              }}
              className="absolute right-3 top-1/2 transform -translate-y-1/2 flex items-center gap-1.5 px-3 py-1 hover:text-white bg-gray-700/60 hover:bg-gray-700/90 rounded border border-gray-600/50 hover:border-gray-500 animate-hover text-xs font-medium"
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
                          <kbd className="absolute right-3 top-1/2 transform -translate-y-1/2 pointer-events-none hidden sm:inline-flex items-center gap-1 px-2 py-0.5 text-xs font-mono bg-gray-800 rounded border border-gray-700"
                              style={{
                                  backgroundColor: 'var(--global-background-color, #111827)',
                                  borderColor: 'var(--global-border-color, #1f2937)',
                                  color: 'var(--global-text-color, #e5e7eb)',
                                  fontFamily: 'var(--global-font-family, inherit)',
                                  fontSize: 'calc(var(--global-font-size, 0.875rem) * 0.75)',
                                  borderRadius: 'var(--global-border-radius, 0)',
                                  boxShadow: 'var(--global-box-shadow, none)'
                              }}
                          >
              {navigator.platform.indexOf('Mac') !== -1 ? '⌘ + K' : 'Ctrl + K'}
            </kbd>
          )}
        </div>
      </form>
          {isOpen && (searchTerm || filteredCommands.length) && createPortal(
              <div
                className="fixed z-50 command-dropdown-menu CommandBar"
                style={{
                  top: inputRef.current ? inputRef.current.getBoundingClientRect().bottom + 8 : 0,
                  left: inputRef.current ? inputRef.current.getBoundingClientRect().left : 0,
                  width: inputRef.current ? inputRef.current.getBoundingClientRect().width : 'auto',
                  maxWidth: '100vw'
                }}
              >
        <div
          className="absolute top-full left-0 right-0 mt-1 bg-gray-800 border border-gray-700 rounded-lg shadow-lg overflow-hidden z-50 max-h-[60vh] overflow-y-auto"
          style={{
            backgroundColor: 'var(--global-background-color, #333)',
            borderColor: 'var(--global-border-color, #555)',
            boxShadow: 'var(--global-box-shadow, 0 4px 12px rgba(0,0,0,0.3))'
          }}
        >
          {Object.entries(groupedCommands).map(([section, commands]) => (
            <div key={section} className="border-t border-gray-700 first:border-t-0">
              <div
                className="px-2 py-2 text-sm font-bold"
                style={{
                  backgroundColor: 'var(--global-background-color, #111)',
                  color: 'var(--global-text-color, #aaa)'
                }}
              >
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
                              'px-2 py-1 flex items-center justify-between cursor-pointer hover:bg-gray-700/50',
                              isSelected && 'bg-gray-700/70'
                          )}
                          style={{
                            backgroundColor: isSelected
                              ? 'var(--global-primary-color, #555)'
                              : index % 2 === 1
                                ? 'color-mix(in srgb, var(--global-background-color, #333) 95%, white)'
                                : 'transparent',
                            color: 'var(--global-text-color, #eee)'
                          }}
                          onMouseDown={(e) => {
                              e.preventDefault(); 
                          }}
                          onMouseUp={(e) => {
                              e.stopPropagation(); 
                              handleCommandClick(command.id);
                          }}
                      >
                      <div className="flex items-center gap-1.5">
                        {command.icon && <div className="">{typeof command.icon === 'function' ? command.icon() : command.icon}</div>}
                      <div className="max-w-md overflow-hidden text-sm">
                          <div className="font-medium">{command.name}</div>
                          {command.description && (
                            <div className="whitespace-pre-line line-clamp-2 text-xs leading-tight opacity-80">
                              {command.description}
                            </div>
                          )}
                      </div>
                      </div>
                      <div className="flex items-center gap-1">
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
                                style={{
                                  color: 'var(--global-text-color, #888)'
                                }}
                                className={cn(
                                  'p-0.5 rounded hover:bg-gray-600/50 cursor-pointer',
                                  isPinned ? 'text-blue-400' : 'text-gray-500 hover:text-gray-300',
                                )}
                              >
                                <Pin className="h-3 w-3" fill={isPinned ? 'currentColor' : 'none'}  data-testid={`pin-button-${command.id}`}/>
                              </div>
                            </TooltipTrigger>
                            <TooltipContent side="top">
                              <p>{isPinned ? 'Unpin command' : 'Pin to shortcuts'}</p>
                            </TooltipContent>
                          </Tooltip>
                        </TooltipProvider>
                        {command.shortcut && (
                          <kbd className="px-1.5 py-0.5 text-xs font-mono bg-gray-800 rounded border border-gray-700">
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