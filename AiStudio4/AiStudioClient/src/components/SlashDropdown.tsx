// AiStudioClient/src/components/SlashDropdown.tsx
import React, { useEffect, useState, useRef } from 'react';
import { createPortal } from 'react-dom';
import { SlashItem, slashItemRegistry } from '../services/slashItemRegistry';

interface SlashDropdownProps {
  query: string;
  onSelect: (text: string) => void;
  onCancel: () => void;
  anchorElement: HTMLTextAreaElement | null;
}

/**
 * Dropdown component for slash commands
 * Shows a list of items that match the query and allows selection
 * Positioned centered above the input bar
 */
export const SlashDropdown: React.FC<SlashDropdownProps> = ({
  query,
  onSelect,
  onCancel,
  anchorElement
}) => {
  const [items, setItems] = useState<SlashItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  
  // Calculate position centered above the input bar
  useEffect(() => {
    if (anchorElement) {
      const updatePosition = () => {
        const rect = anchorElement.getBoundingClientRect();
        const MARGIN = 8; // 8px margin above the input bar
        
        // Calculate position centered above the input bar
        setPosition({
          top: rect.top - MARGIN, // Position will be at the bottom of the dropdown
          left: rect.left + (rect.width / 2) // Center horizontally
        });
      };
      
      // Update position immediately and on resize
      updatePosition();
      window.addEventListener('resize', updatePosition);
      return () => window.removeEventListener('resize', updatePosition);
    }
  }, [anchorElement]);
  
  // Fetch and filter items when query changes
  useEffect(() => {
    const fetchItems = async () => {
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
        
        // Combine items
        const allItems = [...hardcodedItems, ...registryItems];
        
        // Filter items based on query
        const filteredItems = query ? 
          allItems.filter(item => 
            item.name.toLowerCase().includes(query.toLowerCase()) ||
            (item.description && item.description.toLowerCase().includes(query.toLowerCase()))
          ) : 
          allItems;
        
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
      const text = await item.getTextToInsert();
      onSelect(text);
    } catch (error) {
      console.error('Error getting text to insert:', error);
    }
  };
  
  // Don't render if no items or no anchor element
  if (items.length === 0 || !anchorElement) {
    return null;
  }
  
  // Use createPortal to render the dropdown to the document body
  return createPortal(
    <div 
      ref={dropdownRef}
      className="slash-dropdown"
      style={{
        position: 'fixed',
        bottom: `calc(100vh - ${position.top}px)`, // Position bottom at 8px above input bar
        left: position.left,
        transform: 'translateX(-50%)', // Center horizontally
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
    </div>,
    document.body
  );
};