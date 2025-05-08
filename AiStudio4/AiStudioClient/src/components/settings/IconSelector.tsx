// AiStudioClient/src/components/settings/IconSelector.tsx
import React, { useState, useEffect, useMemo } from 'react';
import * as LobehubIcons from '@lobehub/icons';
import { Input } from '@/components/ui/input';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Search } from 'lucide-react';

interface IconSelectorProps {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}

export const IconSelector: React.FC<IconSelectorProps> = ({ value, onChange, disabled = false }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedIcon, setSelectedIcon] = useState(value || '');

  // Extract all available icons from LobehubIcons
  const allIcons = useMemo(() => {
    const iconNames: string[] = [];
    
    // Filter out non-icon entries and get only those with Avatar property
    Object.entries(LobehubIcons).forEach(([name, icon]) => {
      if (typeof icon === 'object' && icon !== null && 'Avatar' in icon) {
        iconNames.push(name);
      }
    });
    
    return iconNames.sort();
  }, []);

  // Filter icons based on search term
  const filteredIcons = useMemo(() => {
    if (!searchTerm) return allIcons;
    
    return allIcons.filter(name => 
      name.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }, [allIcons, searchTerm]);

  // Update the selected icon when the external value changes
  useEffect(() => {
    setSelectedIcon(value || '');
  }, [value]);

  // Handle icon selection
  const handleSelectIcon = (iconName: string) => {
    setSelectedIcon(iconName);
    onChange(iconName);
  };

  return (
    <div className="flex flex-col space-y-2">
      {/* Search input */}
      <div className="relative">
        <Search className="absolute left-2 top-2.5 h-4 w-4 text-gray-400" />
        <Input
          type="text"
          placeholder="Search icons..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="pl-8 bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500"
          disabled={disabled}
        />
      </div>
      
      {/* Selected icon preview */}
      {selectedIcon && LobehubIcons[selectedIcon] && (
        <div className="flex items-center gap-2 p-2 bg-gray-800 rounded-md border border-gray-700">
          <div className="flex items-center justify-center w-8 h-8 bg-gray-700 rounded-md">
            {React.createElement(LobehubIcons[selectedIcon].Avatar, { size: 24 })}
          </div>
          <span className="text-gray-200">{selectedIcon}</span>
        </div>
      )}
      
      {/* Icons grid */}
      <ScrollArea className="h-60 rounded-md border border-gray-700 bg-gray-800">
        <div className="grid grid-cols-4 gap-2 p-2">
          {filteredIcons.map((iconName) => {
            const IconComponent = LobehubIcons[iconName].Avatar;
            const isSelected = selectedIcon === iconName;
            
            return (
              <div
                key={iconName}
                className={`flex flex-col items-center justify-center p-2 rounded-md cursor-pointer transition-colors ${isSelected ? 'bg-blue-600' : 'bg-gray-700 hover:bg-gray-600'} ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
                onClick={() => !disabled && handleSelectIcon(iconName)}
                title={iconName}
              >
                <div className="flex items-center justify-center w-8 h-8">
                  <IconComponent size={24} />
                </div>
                <span className="text-xs text-center mt-1 text-gray-200 truncate w-full">
                  {iconName}
                </span>
              </div>
            );
          })}
        </div>
      </ScrollArea>
      
      {filteredIcons.length === 0 && (
        <div className="text-center py-4 text-gray-400">
          No icons found matching "{searchTerm}"
        </div>
      )}
    </div>
  );
};