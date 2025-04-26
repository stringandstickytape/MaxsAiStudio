// AiStudio4.Web\src\components\IconSelector.tsx
import React, { useState, useEffect, useMemo } from 'react';
import * as LucideIcons from 'lucide-react';
// Import some common icons directly to ensure we have at least these available
import { Command, Plus, Settings, RefreshCw, GitBranch, Mic, Check, X, Search, Home, User, Mail, Calendar } from 'lucide-react';
import { Command as CommandUI, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { ScrollArea } from '@/components/ui/scroll-area';

// Define icon set types
export type IconSet = 'lucide';

interface IconSelectorProps {
  onSelect: (iconName: string, iconSet: IconSet) => void;
  selectedIconName?: string;
  selectedIconSet?: IconSet;
}

const IconSelector: React.FC<IconSelectorProps> = ({ 
  onSelect, 
  selectedIconName, 
  selectedIconSet = 'lucide' 
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [displayedIcons, setDisplayedIcons] = useState<Array<{name: string, Icon: any}>>([]);
  
  // Get all Lucide icons with error handling
  const lucideIcons = useMemo(() => {
    try {
      if (!LucideIcons || typeof LucideIcons !== 'object') {
        console.error('LucideIcons is not available or not an object');
        return [];
      }
      
      // Get all icon names that are valid components
      const iconEntries = Object.entries(LucideIcons).filter(([name, component]) => {
        return (
          name !== 'createLucideIcon' && 
          name !== 'default' && 
          !name.startsWith('__') &&
          typeof component === 'function'
        );
      });
      
      console.log('Valid Lucide icons found:', iconEntries.length);
      
      // Map to the format we need
      return iconEntries.map(([name, Icon]) => ({ name, Icon }));
    } catch (error) {
      console.error('Error processing Lucide icons:', error);
      return [];
    }
  }, []);
  
  // Create a fallback set of icons if the main import fails
  const fallbackIcons = useMemo(() => {
    return [
      { name: 'Command', Icon: Command },
      { name: 'Plus', Icon: Plus },
      { name: 'Settings', Icon: Settings },
      { name: 'RefreshCw', Icon: RefreshCw },
      { name: 'GitBranch', Icon: GitBranch },
      { name: 'Mic', Icon: Mic },
      { name: 'Check', Icon: Check },
      { name: 'X', Icon: X },
      { name: 'Search', Icon: Search },
      { name: 'Home', Icon: Home },
      { name: 'User', Icon: User },
      { name: 'Mail', Icon: Mail },
      { name: 'Calendar', Icon: Calendar },
    ];
  }, []);
  
  // Update displayed icons when search query changes
  useEffect(() => {
    try {
      // Use lucideIcons if available, otherwise use fallbackIcons
      
      const sourceIcons = lucideIcons.length > 0 ? lucideIcons : fallbackIcons;
      
      // Filter based on search query
      const filtered = sourceIcons.filter(({ name }) =>
        name.toLowerCase().includes(searchQuery.toLowerCase())
      );
      
      // Limit to first 100 icons to prevent performance issues
      const limited = filtered.slice(0, 100);
      console.log(`Displaying ${limited.length} icons out of ${filtered.length} matches`);
      
      setDisplayedIcons(limited);
    } catch (error) {
      console.error('Error filtering icons:', error);
      setDisplayedIcons(fallbackIcons);
    }
  }, [lucideIcons, fallbackIcons, searchQuery]);

  // Handle icon selection
  const handleSelectIcon = (iconName: string) => {
    onSelect(iconName, 'lucide');
  };

  // If we have no icons at all, show a simplified version
  if (lucideIcons.length === 0 && fallbackIcons.length === 0) {
    return (
      <div className="w-full p-4 text-center border rounded-lg">
        <p>Icon selector is not available at this moment.</p>
        <p className="text-sm text-gray-500 mt-2">You can still save the shortcut without changing the icon.</p>
      </div>
    );
  }

  return (
    <div className="w-full">
      <CommandUI className="rounded-lg border shadow-md">
        <CommandInput 
          placeholder="Search icons..." 
          value={searchQuery}
          onValueChange={setSearchQuery}
        />
        <CommandList>
          <CommandEmpty>
            <div className="p-4 text-center">
              <p>No icons match your search.</p>
              <p className="text-xs text-gray-500 mt-1">Try a different search term or clear the search.</p>
            </div>
          </CommandEmpty>
          <CommandGroup heading={`Lucide Icons ${lucideIcons.length ? `(${lucideIcons.length})` : ''}`}>
            <ScrollArea className="h-[200px]">
              {displayedIcons.length === 0 && !searchQuery && (
                <div className="p-4 text-center">
                  <p>Loading icons...</p>
                </div>
              )}
              
              {displayedIcons.length > 0 && (
                <div className="grid grid-cols-4 gap-2 p-2">
                  {displayedIcons.map(({ name, Icon }) => {
                    try {
                      const isSelected = name === selectedIconName && selectedIconSet === 'lucide';
                      return (
                        <CommandItem
                          key={name}
                          value={name}
                          onSelect={() => handleSelectIcon(name)}
                          className={`flex flex-col items-center justify-center p-2 cursor-pointer ${isSelected ? 'bg-accent text-accent-foreground' : ''}`}
                        >
                          <div className="flex items-center justify-center w-8 h-8 mb-1">
                            <Icon className="h-6 w-6" />
                          </div>
                          <span className="text-xs text-center truncate w-full">{name}</span>
                        </CommandItem>
                      );
                    } catch (error) {
                      console.error(`Error rendering icon ${name}:`, error);
                      return null;
                    }
                  })}
                </div>
              )}
            </ScrollArea>
          </CommandGroup>
        </CommandList>
      </CommandUI>
    </div>
  );
};

export default IconSelector;