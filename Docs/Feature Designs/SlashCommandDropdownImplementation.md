# Slash Command Dropdown Implementation

## Overview

This feature adds a dropdown menu that appears when a user types '/' followed by text in an input field. The dropdown shows matching items that can be selected to insert text into the input field. The dropdown updates in real-time as the user types, narrowing down the options based on the input.

## Requirements

### Trigger Mechanism
- User types '/' followed by partial item name
- System immediately shows dropdown with matching items
- Dropdown updates in real-time as user continues typing

### Selection Process
- Keyboard Navigation: Arrow keys to navigate, Enter to select
- Mouse Selection: Click on desired item
- Tab Completion: Tab key selects top match

### Cancellation Methods
- Explicit: Escape key dismisses dropdown
- Implicit: Space after /someentry (with no selection) treats it as literal text

### Action
- When an item is selected, associated code is run which returns text
- Text is appended to the current user input

## Architecture

### Core Components

#### 1. SlashItemRegistry

A service that maintains a registry of all available slash items from different sources.

```typescript
// src/services/slashItemRegistry.ts
export interface SlashItem {
  id: string;
  name: string;
  description?: string;
  category?: string;
  getTextToInsert: () => string | Promise<string>;
}

export interface SlashItemProvider {
  getItems: () => SlashItem[] | Promise<SlashItem[]>;
}

class SlashItemRegistryService {
  private providers: SlashItemProvider[] = [];
  private cachedItems: SlashItem[] = [];
  private lastUpdateTime = 0;
  
  registerProvider(provider: SlashItemProvider) {
    this.providers.push(provider);
    this.invalidateCache();
  }
  
  async getItems(): Promise<SlashItem[]> {
    if (Date.now() - this.lastUpdateTime > 5000) {
      await this.refreshCache();
    }
    return this.cachedItems;
  }
  
  async getFilteredItems(query: string): Promise<SlashItem[]> {
    const items = await this.getItems();
    if (!query) return items;
    
    const lowerQuery = query.toLowerCase();
    return items.filter(item => 
      item.name.toLowerCase().includes(lowerQuery) || 
      item.description?.toLowerCase().includes(lowerQuery)
    );
  }
  
  private invalidateCache() {
    this.lastUpdateTime = 0;
  }
  
  private async refreshCache() {
    const allItems: SlashItem[] = [];
    for (const provider of this.providers) {
      try {
        const items = await provider.getItems();
        allItems.push(...items);
      } catch (error) {
        console.error('Error fetching items from provider:', error);
      }
    }
    this.cachedItems = allItems;
    this.lastUpdateTime = Date.now();
    return allItems;
  }
}

export const slashItemRegistry = new SlashItemRegistryService();
```

#### 2. SlashItemProviders

Implementations of the `SlashItemProvider` interface for different sources of items.

```typescript
// src/services/providers/userPromptProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';
import { userPromptService } from '../userPromptService';

export class UserPromptProvider implements SlashItemProvider {
  async getItems(): Promise<SlashItem[]> {
    const userPrompts = await userPromptService.getUserPrompts();
    
    return userPrompts.map(prompt => ({
      id: `prompt-${prompt.id}`,
      name: prompt.name,
      description: prompt.description || 'User prompt',
      category: 'Prompts',
      getTextToInsert: () => prompt.text
    }));
  }
}

// src/services/providers/fileNameProvider.ts
import { SlashItem, SlashItemProvider } from '../slashItemRegistry';
import { fileService } from '../fileService';

export class FileNameProvider implements SlashItemProvider {
  async getItems(): Promise<SlashItem[]> {
    const files = await fileService.getRecentFiles();
    
    return files.map(file => ({
      id: `file-${file.path}`,
      name: file.name,
      description: `File: ${file.path}`,
      category: 'Files',
      getTextToInsert: () => file.path
    }));
  }
}
```

#### 3. SlashDropdown Component

A React component that displays the dropdown and handles selection.

```typescript
// src/components/SlashDropdown.tsx
import React, { useEffect, useState, useRef } from 'react';
import { SlashItem, slashItemRegistry } from '../services/slashItemRegistry';

interface SlashDropdownProps {
  query: string;
  onSelect: (text: string) => void;
  onCancel: () => void;
  position: { top: number; left: number };
}

export const SlashDropdown: React.FC<SlashDropdownProps> = ({
  query,
  onSelect,
  onCancel,
  position
}) => {
  const [items, setItems] = useState<SlashItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const dropdownRef = useRef<HTMLDivElement>(null);
  
  useEffect(() => {
    const fetchItems = async () => {
      const filteredItems = await slashItemRegistry.getFilteredItems(query);
      setItems(filteredItems);
      setSelectedIndex(0);
    };
    
    fetchItems();
  }, [query]);
  
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowDown':
          e.preventDefault();
          setSelectedIndex(prev => (prev + 1) % items.length);
          break;
        case 'ArrowUp':
          e.preventDefault();
          setSelectedIndex(prev => (prev - 1 + items.length) % items.length);
          break;
        case 'Enter':
          e.preventDefault();
          if (items[selectedIndex]) {
            selectItem(items[selectedIndex]);
          }
          break;
        case 'Tab':
          e.preventDefault();
          if (items[selectedIndex]) {
            selectItem(items[selectedIndex]);
          }
          break;
        case 'Escape':
          e.preventDefault();
          onCancel();
          break;
      }
    };
    
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [items, selectedIndex, onCancel]);
  
  const selectItem = async (item: SlashItem) => {
    try {
      const text = await item.getTextToInsert();
      onSelect(text);
    } catch (error) {
      console.error('Error getting text to insert:', error);
    }
  };
  
  if (items.length === 0) return null;
  
  return (
    <div 
      ref={dropdownRef}
      className="slash-dropdown"
      style={{
        position: 'absolute',
        top: position.top,
        left: position.left,
        zIndex: 1000,
        maxHeight: '300px',
        overflowY: 'auto',
        backgroundColor: 'var(--background)',
        border: '1px solid var(--border)',
        borderRadius: '4px',
        boxShadow: '0 2px 8px rgba(0, 0, 0, 0.15)',
        width: '300px'
      }}
    >
      {items.map((item, index) => (
        <div
          key={item.id}
          className={`slash-item ${index === selectedIndex ? 'selected' : ''}`}
          onClick={() => selectItem(item)}
          style={{
            padding: '8px 12px',
            cursor: 'pointer',
            backgroundColor: index === selectedIndex ? 'var(--selection-background)' : 'transparent',
            color: index === selectedIndex ? 'var(--selection-text)' : 'var(--text)',
          }}
        >
          <div className="slash-item-name">{item.name}</div>
          {item.description && (
            <div className="slash-item-description" style={{ fontSize: '0.8em', opacity: 0.7 }}>
              {item.description}
            </div>
          )}
        </div>
      ))}
    </div>
  );
};
```

#### 4. InputBar Integration

Modifications to the existing InputBar component to integrate the slash dropdown.

```typescript
// Modification to existing InputBar component
import { SlashDropdown } from './SlashDropdown';
import { slashItemRegistry } from '../services/slashItemRegistry';

// Inside InputBar component
const [showSlashDropdown, setShowSlashDropdown] = useState(false);
const [slashQuery, setSlashQuery] = useState('');
const [dropdownPosition, setDropdownPosition] = useState({ top: 0, left: 0 });
const inputRef = useRef<HTMLTextAreaElement>(null);

// Handle input changes
const handleInputChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
  const value = e.target.value;
  setValue(value);
  
  // Check for slash command trigger
  const match = /(?:^|\s)\/([^\s]*)$/.exec(value);
  if (match) {
    const query = match[1];
    setSlashQuery(query);
    setShowSlashDropdown(true);
    
    // Calculate dropdown position based on cursor position
    if (inputRef.current) {
      const cursorPosition = getCursorPosition(inputRef.current);
      setDropdownPosition({
        top: cursorPosition.top + 20, // Adjust as needed
        left: cursorPosition.left
      });
    }
  } else {
    setShowSlashDropdown(false);
  }
};

// Handle selection from dropdown
const handleSlashItemSelect = (text: string) => {
  // Replace the slash command with the selected text
  const value = inputRef.current?.value || '';
  const newValue = value.replace(/(?:^|\s)\/[^\s]*$/, (match) => {
    // If the slash command starts with a space, preserve it
    return match.startsWith(' ') ? ' ' + text : text;
  });
  
  setValue(newValue);
  setShowSlashDropdown(false);
  
  // Focus back on input
  inputRef.current?.focus();
};

// Handle cancellation
const handleSlashDropdownCancel = () => {
  setShowSlashDropdown(false);
};

// In the render function
return (
  <div className="input-bar-container">
    <textarea
      ref={inputRef}
      value={value}
      onChange={handleInputChange}
      // Other props...
    />
    
    {showSlashDropdown && (
      <SlashDropdown
        query={slashQuery}
        onSelect={handleSlashItemSelect}
        onCancel={handleSlashDropdownCancel}
        position={dropdownPosition}
      />
    )}
    
    {/* Rest of InputBar component */}
  </div>
);
```

#### 5. Provider Registration

Registration of providers during application initialization.

```typescript
// src/App.tsx or appropriate initialization file
import { slashItemRegistry } from './services/slashItemRegistry';
import { UserPromptProvider } from './services/providers/userPromptProvider';
import { FileNameProvider } from './services/providers/fileNameProvider';

// Register providers during app initialization
slashItemRegistry.registerProvider(new UserPromptProvider());
slashItemRegistry.registerProvider(new FileNameProvider());
// Register other providers as needed
```

### Utility Functions

#### Cursor Position Calculation

```typescript
// src/utils/textAreaUtils.ts
export function getCursorPosition(textarea: HTMLTextAreaElement) {
  const { selectionStart, value } = textarea;
  const textBeforeCursor = value.substring(0, selectionStart);
  
  // Create a temporary element to measure text dimensions
  const temp = document.createElement('div');
  temp.style.position = 'absolute';
  temp.style.visibility = 'hidden';
  temp.style.whiteSpace = 'pre-wrap';
  temp.style.wordWrap = 'break-word';
  temp.style.width = getComputedStyle(textarea).width;
  temp.style.fontSize = getComputedStyle(textarea).fontSize;
  temp.style.lineHeight = getComputedStyle(textarea).lineHeight;
  temp.style.fontFamily = getComputedStyle(textarea).fontFamily;
  temp.style.padding = getComputedStyle(textarea).padding;
  
  // Add a span at the cursor position
  temp.innerHTML = textBeforeCursor.replace(/\n/g, '<br>') + '<span id="cursor"></span>';
  document.body.appendChild(temp);
  
  // Get position of the cursor span
  const cursorSpan = document.getElementById('cursor');
  const rect = cursorSpan!.getBoundingClientRect();
  const textareaRect = textarea.getBoundingClientRect();
  
  // Clean up
  document.body.removeChild(temp);
  
  // Return position relative to textarea
  return {
    top: rect.top - textareaRect.top + textarea.scrollTop,
    left: rect.left - textareaRect.left + textarea.scrollLeft
  };
}
```

## Implementation Plan

1. **Phase 1: Core Infrastructure**
   - Create SlashItemRegistry service
   - Define SlashItem and SlashItemProvider interfaces
   - Implement utility functions for cursor position calculation

2. **Phase 2: UI Components**
   - Create SlashDropdown component
   - Implement keyboard navigation and selection
   - Style the dropdown to match application theme

3. **Phase 3: InputBar Integration**
   - Modify InputBar to detect slash commands
   - Add logic to show/hide dropdown
   - Implement text replacement on selection

4. **Phase 4: Item Providers**
   - Implement UserPromptProvider
   - Implement FileNameProvider
   - Add registration of providers

5. **Phase 5: Testing & Refinement**
   - Test with various input scenarios
   - Optimize performance for large numbers of items
   - Add accessibility improvements

## Considerations

### Performance

- For large numbers of items, consider pagination or virtualization in the dropdown
- Cache items with periodic refreshes to avoid frequent backend calls
- Debounce input handling to prevent excessive filtering operations

### Accessibility

- Ensure keyboard navigation works properly
- Add appropriate ARIA attributes to the dropdown
- Provide visual feedback for selected items

### Extensibility

- Design the system to easily add new item providers
- Consider adding a way to categorize items in the dropdown
- Allow for customization of item appearance and behavior

### Error Handling

- Gracefully handle provider errors
- Provide fallbacks when items can't be loaded
- Log errors for debugging

## Future Enhancements

1. **Categories & Grouping**: Group items by category in the dropdown
2. **Item Icons**: Add support for icons or visual indicators for different item types
3. **Rich Text Insertion**: Support for inserting formatted text or templates
4. **History & Favorites**: Track frequently used items and prioritize them
5. **Custom Styling**: Allow for theming and customization of the dropdown appearance