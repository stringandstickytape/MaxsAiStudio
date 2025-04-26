// AiStudio4.Web\src\components\IconSelector.tsx
import React, { useState, useMemo } from 'react';
import * as lucide from 'lucide-react';
import { Command as CommandUI, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Button } from '@/components/ui/button';

// Define icon set types
export type IconSet = 'lucide';

interface IconSelectorProps {
  onSelect: (iconName: string, iconSet: IconSet) => void;
  selectedIconName?: string;
  selectedIconSet?: IconSet;
  onClose?: () => void;
}

const IconSelector: React.FC<IconSelectorProps> = ({ 
  onSelect, 
  selectedIconName, 
  selectedIconSet = 'lucide',
  onClose
}) => {
  const [searchTerm, setSearchTerm] = useState('');

  // Filter and process Lucide icons
  const filteredIcons = useMemo(() => {
    return Object.entries(lucide)
      .filter(([name, component]: [string, any]) => {
        // Filter for components that have a render method (actual icons)
        return name.includes('Icon') && !!component.render;
      })
      .filter(([name]) => {
        // Filter by search term if one exists
        return name.toLowerCase().includes(searchTerm.toLowerCase());
      })
      // Limit to 100 icons to prevent performance issues
      .slice(0, 100);
  }, [searchTerm]);

  // Handle icon selection
  const handleSelectIcon = (iconName: string) => {
    // Remove the "Icon" suffix if present
    const cleanName = iconName.replace(/Icon$/, '');
    onSelect(cleanName, 'lucide');
    if (onClose) onClose();
  };

  return (
    <div className="w-full">
      <CommandUI className="rounded-lg border shadow-md">
        <CommandInput 
          placeholder="Search icons..." 
          value={searchTerm}
          onValueChange={setSearchTerm}
        />
        <CommandList>
          <CommandEmpty>
            <div className="p-4 text-center">
              <p>No icons match your search.</p>
              <p className="text-xs text-gray-500 mt-1">Try a different search term or clear the search.</p>
            </div>
          </CommandEmpty>
          <CommandGroup heading={`Lucide Icons (${filteredIcons.length})`}>
            <ScrollArea className="h-[300px]">
              {filteredIcons.length === 0 && !searchTerm && (
                <div className="p-4 text-center">
                  <p>Loading icons...</p>
                </div>
              )}
              
              {filteredIcons.length > 0 && (
                <div className="grid grid-cols-5 gap-2 p-2">
                  {filteredIcons.map(([name, Icon]) => {
                    try {
                      // Remove "Icon" suffix for display
                      const displayName = name.replace(/Icon$/, '');
                      const isSelected = displayName === selectedIconName && selectedIconSet === 'lucide';
                      
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
                          <span className="text-xs text-center truncate w-full">{displayName}</span>
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
      
      {onClose && (
        <div className="flex justify-end mt-4">
          <Button variant="outline" onClick={onClose} className="mr-2">Cancel</Button>
          <Button onClick={() => onClose()}>Done</Button>
        </div>
      )}
    </div>
  );
};

export default IconSelector;