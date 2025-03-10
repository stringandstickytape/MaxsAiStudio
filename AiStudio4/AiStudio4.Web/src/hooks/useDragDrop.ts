// src/hooks/useDragDrop.ts
import { useState, useCallback } from 'react';
import { DropResult } from 'react-beautiful-dnd';

/**
 * Custom hook to handle drag and drop operations
 * @param items The array of items to manage
 * @param onReorder Optional callback for when items are reordered
 * @returns The handlers and state for drag and drop
 */
export function useDragDrop<T extends { id: string }>(items: T[], onReorder?: (newItems: T[]) => void) {
  const [isDragging, setIsDragging] = useState(false);

  const handleDragStart = useCallback(() => {
    setIsDragging(true);
  }, []);

  const handleDragEnd = useCallback((result: DropResult) => {
    setIsDragging(false);

    // If the item was dropped outside the droppable area
    if (!result.destination) return;

    // If it was dropped in the same position
    if (result.destination.index === result.source.index) return;

    // Reorder the array
    const reorderedItems = Array.from(items);
    const [movedItem] = reorderedItems.splice(result.source.index, 1);
    reorderedItems.splice(result.destination.index, 0, movedItem);

    // Call the onReorder callback with the new array
    if (onReorder) {
      onReorder(reorderedItems);
    }
  }, [items, onReorder]);

  return {
    isDragging,
    handleDragStart,
    handleDragEnd
  };
}