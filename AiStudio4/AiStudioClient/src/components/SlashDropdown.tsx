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
      const filteredItems = await slashItemRegistry.getFilteredItems(query);
      setItems(filteredItems);
      setSelectedIndex(0);
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
      const text = await item.getTextToInsert();
      onSelect(text);
    } catch (error) {
      console.error('Error getting text to insert:', error);
    }
  };
  
  // Don't render if no items
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