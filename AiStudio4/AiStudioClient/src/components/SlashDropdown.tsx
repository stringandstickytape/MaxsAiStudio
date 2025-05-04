// AiStudioClient/src/components/SlashDropdown.tsx
import React, { useEffect, useState, useRef } from 'react';
import { SlashItem, slashItemRegistry } from '../services/slashItemRegistry';

interface SlashDropdownProps {
  query: string;
  onSelect: (text: string) => void;
  onCancel: () => void;
  position: { top: number; left: number };
}

/**
 * Dropdown component for slash commands
 * Shows a list of items that match the query and allows selection
 */
export const SlashDropdown: React.FC<SlashDropdownProps> = ({
  query,
  onSelect,
  onCancel,
  position
}) => {
  const [items, setItems] = useState<SlashItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const dropdownRef = useRef<HTMLDivElement>(null);
  
  // Fetch and filter items when query changes
  useEffect(() => {
    const fetchItems = async () => {
      console.log('Fetching items for query:', query);
      try {
        // Add a hardcoded item for testing
        const hardcodedItems: SlashItem[] = [
          {
            id: 'hardcoded-1',
            name: 'roflcopters',
            description: 'Inserts the word "sausages"',
            category: 'Test',
            getTextToInsert: () => 'sausages'
          },
          {
            id: 'hardcoded-2',
            name: 'test-item',
            description: 'A test item',
            category: 'Test',
            getTextToInsert: () => 'This is a test item'
          }
        ];
        
        // Get items from registry
        const registryItems = await slashItemRegistry.getFilteredItems(query);
        console.log('Items from registry:', registryItems);
        
        // Combine items
        const allItems = [...hardcodedItems, ...registryItems];
        console.log('All items:', allItems);
        
        // Filter items based on query
        const filteredItems = query ? 
          allItems.filter(item => 
            item.name.toLowerCase().includes(query.toLowerCase()) ||
            (item.description && item.description.toLowerCase().includes(query.toLowerCase()))
          ) : 
          allItems;
        
        console.log('Filtered items:', filteredItems);
        setItems(filteredItems);
        setSelectedIndex(0);
      } catch (error) {
        console.error('Error fetching slash items:', error);
        // Fallback to hardcoded items on error
        setItems([
          {
            id: 'hardcoded-1',
            name: 'roflcopters',
            description: 'Inserts the word "sausages"',
            category: 'Test',
            getTextToInsert: () => 'sausages'
          }
        ]);
      }
    };
    
    fetchItems();
  }, [query]);
  
  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (items.length === 0) return;
      
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
        case ' ':
          // Only prevent default if we're showing the dropdown
          // Otherwise let the space be typed normally
          if (items.length > 0) {
            e.preventDefault();
            onCancel();
          }
          break;
      }
    };
    
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [items, selectedIndex, onCancel]);
  
  // Handle item selection
  const selectItem = async (item: SlashItem) => {
    try {
      console.log('Selecting item:', item);
      const text = await item.getTextToInsert();
      console.log('Text to insert:', text);
      onSelect(text);
    } catch (error) {
      console.error('Error getting text to insert:', error);
    }
  };
  
  // Debug: Log when component renders or doesn't render
  useEffect(() => {
    console.log('SlashDropdown render state:', { 
      itemsLength: items.length, 
      shouldRender: items.length > 0,
      position
    });
  }, [items.length, position]);
  
  // Don't render if no items
  if (items.length === 0) {
    console.log('No items to display, not rendering dropdown');
    return null;
  }
  
  // Fix position if it's negative or invalid
  const fixedPosition = {
    top: position.top < 0 ? 40 : position.top, // Default to 40px from top if negative
    left: position.left < 0 ? 20 : position.left // Default to 20px from left if negative
  };
  
  console.log('Using fixed position:', fixedPosition);
  
  return (
    <div 
      ref={dropdownRef}
      className="slash-dropdown"
      style={{
        position: 'absolute',
        top: fixedPosition.top,
        left: fixedPosition.left,
        zIndex: 1000,
        maxHeight: '300px',
        overflowY: 'auto',
        backgroundColor: 'var(--background, #1f2937)',
        border: '1px solid var(--border, #4a5568)',
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
            backgroundColor: index === selectedIndex ? 'var(--selection-background, #2d3748)' : 'transparent',
            color: index === selectedIndex ? 'var(--selection-text, #e2e8f0)' : 'var(--text, #e2e8f0)',
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