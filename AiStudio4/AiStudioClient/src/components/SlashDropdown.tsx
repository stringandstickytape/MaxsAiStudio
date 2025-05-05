// AiStudioClient/src/components/SlashDropdown.tsx
import React, { useEffect, useState, useRef } from 'react';
import { createPortal } from 'react-dom';
import { SlashItem, slashItemRegistry } from '../services/slashItemRegistry';
import { createApiRequest } from '@/utils/apiUtils';
import { base64ToArrayBuffer } from '@/utils/attachmentUtils';

// Interface for dropdown dimensions
interface DropdownDimensions {
  width: number;
  maxWidth: number;
}

interface SlashDropdownProps {
  query: string;
  onSelect: (text: string) => void;
  onCancel: () => void;
  anchorElement: HTMLTextAreaElement | null;
  onAttachFile?: (file: File) => void; // New prop for handling file attachments
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
  anchorElement,
  onAttachFile
}) => {
  const [items, setItems] = useState<SlashItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const [dimensions, setDimensions] = useState<DropdownDimensions>({ width: 0, maxWidth: 800 });
  
  // Calculate position and dimensions based on the input bar
  useEffect(() => {
    if (anchorElement) {
      const updatePositionAndDimensions = () => {
        // Find the InputBar container (parent of the textarea's parent)
        let inputBarElement = anchorElement.closest('.InputBar');
        if (!inputBarElement) {
          // Fallback to textarea's position if InputBar not found
          const rect = anchorElement.getBoundingClientRect();
          setPosition({
            top: rect.top,
            left: rect.left + (rect.width / 2)
          });
          setDimensions({
            width: rect.width * 0.7, // 70% of textarea width
            maxWidth: 800
          });
          return;
        }
        
        const rect = inputBarElement.getBoundingClientRect();
        const MARGIN = 8; // 8px margin above the input bar
        
        // Calculate position centered above the input bar
        setPosition({
          top: rect.top - MARGIN, // Position will be at the bottom of the dropdown
          left: rect.left + (rect.width / 2) // Center horizontally
        });
        
        // Set dimensions to 70% of input bar width
        setDimensions({
          width: rect.width * 0.7,
          maxWidth: 800
        });
      };
      
      // Update position and dimensions immediately and on resize
      updatePositionAndDimensions();
      
      // Set up resize observer to track input bar size changes
      const resizeObserver = new ResizeObserver(() => {
        updatePositionAndDimensions();
      });
      
      // Find the InputBar element to observe
      const inputBarElement = anchorElement.closest('.InputBar');
      if (inputBarElement) {
        resizeObserver.observe(inputBarElement);
      }
      
      // Also listen for window resize and scroll events
      window.addEventListener('resize', updatePositionAndDimensions);
      window.addEventListener('scroll', updatePositionAndDimensions, true);
      
      return () => {
        resizeObserver.disconnect();
        window.removeEventListener('resize', updatePositionAndDimensions);
        window.removeEventListener('scroll', updatePositionAndDimensions, true);
      };
    }
  }, [anchorElement]);
  
  // Helper function to filter items with wildcard support
  const filterItemsWithWildcards = (items: SlashItem[], query: string): SlashItem[] => {
    if (!query) return items;
    
    // Helper function to check if a string matches a pattern with wildcards
    const matchesWildcard = (text: string, pattern: string): boolean => {
      // Convert strings to lowercase for case-insensitive matching
      const lowerText = text.toLowerCase();
      const lowerPattern = pattern.toLowerCase();
      
      
      // For patterns without wildcards, use simple includes
      if (!lowerPattern.includes('*') && !lowerPattern.includes('?')) {
        const result = lowerText.includes(lowerPattern);
        return result;
      }
      
      // Unified wildcard matching approach for both * and ? wildcards
      try {
        // If pattern has no wildcards, use simple includes
        if (!lowerPattern.includes('*') && !lowerPattern.includes('?')) {
          const result = lowerText.includes(lowerPattern);
          return result;
        }
        
        // If pattern is just * or **, match everything
        if (lowerPattern.replace(/\*/g, '').length === 0) {
          return true;
        }
        
        // First handle ? wildcards by converting them to regex dots
        // but only for parts between * characters
        const parts = lowerPattern.split('*');
        
        // Convert ? to regex dots in each part
        const processedParts = parts.map(part => {
          if (part.includes('?')) {
            // Escape regex special chars except ? which we'll replace
            return part.replace(/[.*+^${}()|[\]\\]/g, '\\$&')
                      .replace(/\?/g, '.');
          }
          return part;
        });
        
        
        // Filter out empty parts (adjacent * characters)
        const nonEmptyParts = processedParts.filter(part => part.length > 0);
        
        // If no non-empty parts, match everything
        if (nonEmptyParts.length === 0) {
          return true;
        }
        
        // Check if all parts appear in sequence
        let currentPos = 0;
        for (const part of nonEmptyParts) {
          // If part contains regex dots (from ? wildcards), use regex to find it
          if (part.includes('.')) {
            const regex = new RegExp(part);
            // Find the next position where this regex matches
            let found = false;
            while (currentPos <= lowerText.length) {
              const substring = lowerText.substring(currentPos);
              const match = substring.match(regex);
              if (match && match.index !== undefined) {
                currentPos += match.index + match[0].length;
                found = true;
                break;
              }
              currentPos++;
            }
            if (!found) {
              return false;
            }
          } else {
            // For parts without ? wildcards, use simple indexOf
            const pos = lowerText.indexOf(part, currentPos);
            if (pos === -1) {
              return false;
            }
            currentPos = pos + part.length;
          }
        }
        
        return true;
      } catch (error) {
        console.error('Error in wildcard matching:', error);
      }
      
      // Fallback to simple includes if all else fails
      const fallbackResult = lowerText.includes(lowerPattern.replace(/[*?]/g, ''));
      return fallbackResult;
    };
    
    return items.filter(item => {
      if (!item || typeof item.name !== 'string') return false;
      
      const nameMatch = matchesWildcard(item.name, query);
      const descriptionMatch = item.description && 
        typeof item.description === 'string' && 
        matchesWildcard(item.description, query);
      
      return nameMatch || descriptionMatch;
    });
  };
  
  // Fetch and filter items when query changes
  useEffect(() => {
    const fetchItems = async () => {
      try {
        // Get items from registry - these are already filtered with wildcard support
        const registryItems = await slashItemRegistry.getFilteredItems(query);
        
        // Don't filter again - the registry items are already filtered with wildcard support
        // Just filter the hardcoded items with wildcard support
        
        // Combine the filtered hardcoded items with the already filtered registry items
        
          setItems(registryItems);
        setSelectedIndex(0);
      } catch (error) {
        console.error('Error fetching slash items:', error);
        // Fallback to hardcoded items on error
        setItems([

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
            // Check if shift key is pressed for file attachment
            if (e.shiftKey && onAttachFile) {
              handleFileAttachment(items[selectedIndex]);
            } else {
              selectItem(items[selectedIndex]);
            }
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
          // When space is pressed, just cancel the dropdown
          // The space character will be handled by the MessageInputArea component
          e.preventDefault();
          onCancel();
          break;
      }
    };
    
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [items, selectedIndex, onCancel, onAttachFile]);
  
  // Handle item selection
  const selectItem = async (item: SlashItem) => {
    try {
      const text = await item.getTextToInsert();
      // Add a space after the inserted text
      onSelect(text + ' ');
    } catch (error) {
      console.error('Error getting text to insert:', error);
    }
  };

  // Handle file attachment (when shift+selecting an item)
  const handleFileAttachment = async (item: SlashItem) => {
    try {
      if (!onAttachFile) return;
      
      // Get the file path from the item
      const filePath = await item.getTextToInsert();
      
      // Call the API to get the file content
      const fileContentRequest = createApiRequest('/api/getFileContent', 'POST');
      const response = await fileContentRequest({ filePath });
      
      if (response.success && response.attachment) {
        // Convert base64 to ArrayBuffer for content
        const arrBuf = base64ToArrayBuffer(response.attachment.content);
        
        // Create a File object from the response
        const file = new File(
          [arrBuf],
          response.attachment.name,
          {
            type: response.attachment.type,
            lastModified: response.attachment.lastModified
          }
        );
        
        // Call the callback to add the file as an attachment
        onAttachFile(file);
        
        // Close the dropdown
        onCancel();
      } else {
        console.error('Failed to get file content:', response.error);
      }
    } catch (error) {
      console.error('Error attaching file:', error);
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
        backgroundColor: 'var(--global-background-color, #1f2937)',
        color: 'var(--global-text-color, #e2e8f0)',
        border: '1px solid var(--global-border-color, #4a5568)',
        borderRadius: 'var(--global-border-radius, 4px)',
        boxShadow: 'var(--global-box-shadow, 0 2px 8px rgba(0, 0, 0, 0.15))',
        fontFamily: 'var(--global-font-family, inherit)',
        fontSize: 'var(--global-font-size, inherit)',
        width: `${dimensions.width}px`, // Dynamic width based on input bar
        maxWidth: `${dimensions.maxWidth}px`, // Prevent it from getting too wide
        ...(window?.theme?.SlashDropdown?.style || {})
      }}
    >
      {items.map((item, index) => (
        <div
          key={item.id}
          className={`slash-item ${index === selectedIndex ? 'selected' : ''}`}
          onClick={(e) => {
            // Handle shift+click for file attachment
            if (e.shiftKey && onAttachFile) {
              handleFileAttachment(item);
            } else {
              selectItem(item);
            }
          }}
          style={{
            padding: '8px 12px',
            cursor: 'pointer',
            backgroundColor: index === selectedIndex ? 'var(--global-primary-color, #2d3748)' : 'transparent',
            color: index === selectedIndex ? 'var(--global-text-color, #e2e8f0)' : 'var(--global-text-color, #e2e8f0)',
            wordWrap: 'break-word',
            overflowWrap: 'break-word',
            whiteSpace: 'normal'
          }}
        >
          <div className="slash-item-name" style={{ wordWrap: 'break-word', overflowWrap: 'break-word' }}>{item.name}</div>
          {item.description && (
            <div className="slash-item-description" style={{ fontSize: '0.8em', opacity: 0.7, wordWrap: 'break-word', overflowWrap: 'break-word' }}>
              {item.description}
            </div>
          )}
        </div>
      ))}
    </div>,
    document.body
  );
};

// Define themeable properties for the SlashDropdown component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--global-background-color',
    description: 'Background color for the dropdown',
    default: '#1f2937',
  },
  textColor: {
    cssVar: '--global-text-color',
    description: 'Text color for dropdown items',
    default: '#e2e8f0',
  },
  borderColor: {
    cssVar: '--global-border-color',
    description: 'Border color for the dropdown',
    default: '#4a5568',
  },
  borderRadius: {
    cssVar: '--global-border-radius',
    description: 'Border radius for the dropdown',
    default: '4px',
  },
  boxShadow: {
    cssVar: '--global-box-shadow',
    description: 'Box shadow for the dropdown',
    default: '0 2px 8px rgba(0, 0, 0, 0.15)',
  },
  fontFamily: {
    cssVar: '--global-font-family',
    description: 'Font family for dropdown text',
    default: 'inherit',
  },
  fontSize: {
    cssVar: '--global-font-size',
    description: 'Font size for dropdown text',
    default: 'inherit',
  },
  style: {
    description: 'Arbitrary CSS style for the dropdown container',
    default: {},
  },
};