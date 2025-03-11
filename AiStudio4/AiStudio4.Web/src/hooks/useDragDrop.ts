// src/hooks/useDragDrop.ts
import { useState, useCallback } from 'react';
import { DropResult } from 'react-beautiful-dnd';

/**
 * Custom hook to handle drag and drop operations
 */
export function useDragDrop<T extends { id: string }>(items: T[], onReorder?: (newItems: T[]) => void) {
  const [isDragging, setIsDragging] = useState(false);

  const handleDragStart = useCallback(() => {
    setIsDragging(true);
  }, []);

  const handleDragEnd = useCallback(
    (result: DropResult) => {
      setIsDragging(false);

      if (!result.destination) return;

      if (result.destination.index === result.source.index) return;

      const reorderedItems = Array.from(items);
      const [movedItem] = reorderedItems.splice(result.source.index, 1);
      reorderedItems.splice(result.destination.index, 0, movedItem);

      if (onReorder) {
        onReorder(reorderedItems);
      }
    },
    [items, onReorder],
  );

  return {
    isDragging,
    handleDragStart,
    handleDragEnd,
  };
}
