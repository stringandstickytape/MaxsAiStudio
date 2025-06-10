// AiStudioClient\src\components\IconSelector.tsx
import React, { useState, useMemo } from 'react';
import * as lucide from 'lucide-react';
import * as LobehubIcons from '@lobehub/icons';
import { Command as CommandUI, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Button } from '@/components/ui/button';

// Define themeable properties for the component (currently none)
export const themeableProps = {};

// Define icon set types
export type IconSet = 'lucide' | 'lobehub';

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
  const filteredLucideIcons = useMemo(() => {
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
  
  // Filter and process Lobehub icons
  const filteredLobehubIcons = useMemo(() => {
    return Object.entries(LobehubIcons)
      .filter(([name, icon]: [string, any]) => {
        // Filter for components that have an Avatar property (actual icons)
        return typeof icon === 'object' && icon !== null && 'Avatar' in icon;
      })
      .filter(([name]) => {
        // Filter by search term if one exists
        return name.toLowerCase().includes(searchTerm.toLowerCase());
      })
      // Limit to 100 icons to prevent performance issues
      .slice(0, 100);
  }, [searchTerm]);

  // Handle icon selection
  const handleSelectIcon = (iconName: string, iconSet: IconSet) => {
    // For Lucide icons, remove the "Icon" suffix if present
    const cleanName = iconSet === 'lucide' ? iconName.replace(/Icon$/, '') : iconName;
    onSelect(cleanName, iconSet);
    if (onClose) onClose();
  };

  return (
    <div className="w-full">
      <CommandUI
        className="rounded-lg border shadow-md"
        style={{
          backgroundColor: 'var(--global-background-color)',
          color: 'var(--global-text-color)',
          borderColor: 'var(--global-border-color)',
          fontFamily: 'var(--global-font-family)',
          fontSize: 'var(--global-font-size)'
        }}>
        <CommandInput 
          style={{
            backgroundColor: 'var(--global-background-color)',
            color: 'var(--global-text-color)',
            borderColor: 'var(--global-border-color)'
          }} 
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
          <CommandGroup heading={`Lucide Icons (${filteredLucideIcons.length})`}>
            <ScrollArea className="h-[300px]">
              {filteredLucideIcons.length === 0 && !searchTerm && (
                <div className="p-4 text-center">
                  <p>Loading icons...</p>
                </div>
              )}
              
              {filteredLucideIcons.length > 0 && (
                <div className="grid grid-cols-5 gap-2 p-2">
                  {filteredLucideIcons.map(([name, Icon]) => {
                    try {
                      // Remove "Icon" suffix for display
                      const displayName = name.replace(/Icon$/, '');
                      const isSelected = displayName === selectedIconName && selectedIconSet === 'lucide';
                      
                      return (
                        <CommandItem
                          key={name}
                          value={name}
                          onSelect={() => handleSelectIcon(name, 'lucide')}
                          className="flex flex-col items-center justify-center p-2 cursor-pointer transition-colors hover:bg-white/10"
                          style={{
                            color: isSelected ? 'var(--global-primary-color)' : 'var(--global-text-color)',
                            border: isSelected ? '2px solid var(--global-primary-color)' : '2px solid transparent',
                            borderRadius: 'var(--global-border-radius)'
                          }}
                        >
                          <div className="flex items-center justify-center w-8 h-8 mb-1">
                            <Icon 
                              className="h-6 w-6"
                              style={{ color: isSelected ? 'var(--global-primary-color)' : 'var(--global-text-color)' }}
                            />
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
          
          <CommandGroup heading={`Lobehub Icons (${filteredLobehubIcons.length})`}>
            <ScrollArea className="h-[300px]">
              {filteredLobehubIcons.length === 0 && !searchTerm && (
                <div className="p-4 text-center">
                  <p>Loading icons...</p>
                </div>
              )}
              
              {filteredLobehubIcons.length > 0 && (
                <div className="grid grid-cols-5 gap-2 p-2">
                  {filteredLobehubIcons.map(([name, icon]) => {
                    try {
                      const isSelected = name === selectedIconName && selectedIconSet === 'lobehub';
                      
                      return (
                        <CommandItem
                          key={name}
                          value={name}
                          onSelect={() => handleSelectIcon(name, 'lobehub')}
                          className="flex flex-col items-center justify-center p-2 cursor-pointer transition-colors hover:bg-white/10"
                          style={{
                            color: isSelected ? 'var(--global-primary-color)' : 'var(--global-text-color)',
                            border: isSelected ? '2px solid var(--global-primary-color)' : '2px solid transparent',
                            borderRadius: 'var(--global-border-radius)'
                          }}
                        >
                          <div className="flex items-center justify-center w-8 h-8 mb-1">
                            {React.createElement(icon.Avatar, { size: 24, style: { color: isSelected ? 'var(--global-primary-color)' : 'var(--global-text-color)' } })}
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
      
      {onClose && (
        <div className="flex justify-end mt-4">
          <Button 
            variant="outline" 
            onClick={onClose} 
            className="mr-2"
            style={{
              backgroundColor: 'var(--global-background-color)',
              borderColor: 'var(--global-border-color)',
              color: 'var(--global-text-color)'
            }}
          >
            Cancel
          </Button>
          <Button 
            onClick={() => onClose()}
            style={{
              backgroundColor: 'var(--global-background-color)',
              borderColor: 'var(--global-border-color)',
              color: 'var(--global-text-color)'
            }}
          >
            Done
          </Button>
        </div>
      )}
    </div>
  );
};

export default IconSelector;