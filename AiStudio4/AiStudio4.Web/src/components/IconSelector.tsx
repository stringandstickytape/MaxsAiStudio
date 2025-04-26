// AiStudio4.Web\src\components\IconSelector.tsx
import React, { useState, useEffect, useMemo } from 'react';
import * as LucideIcons from 'lucide-react';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
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
  
  // Get all Lucide icons
  const lucideIcons = useMemo(() => {
    return Object.entries(LucideIcons)
      .filter(([name]) => 
        name !== 'createLucideIcon' && 
        name !== 'default' && 
        !name.startsWith('__')
      )
      .map(([name, Icon]) => ({ name, Icon }));
  }, []);
  
  // Filter icons based on search query
  const filteredIcons = useMemo(() => {
    return lucideIcons.filter(({ name }) =>
      name.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [lucideIcons, searchQuery]);

  // Handle icon selection
  const handleSelectIcon = (iconName: string) => {
    onSelect(iconName, 'lucide');
  };

  return (
    <div className="w-full">
      <Command className="rounded-lg border shadow-md">
        <CommandInput 
          placeholder="Search icons..." 
          value={searchQuery}
          onValueChange={setSearchQuery}
        />
        <CommandList>
          <CommandEmpty>No icons found.</CommandEmpty>
          <CommandGroup heading="Lucide Icons">
            <ScrollArea className="h-[200px]">
              <div className="grid grid-cols-4 gap-2 p-2">
                {filteredIcons.map(({ name, Icon }) => {
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
                })}
              </div>
            </ScrollArea>
          </CommandGroup>
        </CommandList>
      </Command>
    </div>
  );
};

export default IconSelector;