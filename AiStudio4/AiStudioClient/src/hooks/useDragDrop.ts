
import { useState, useCallback, useRef } from 'react';

interface DragData {
  id: string;
  index: number;
}

export function useDragDrop<T extends { id: string }>(items: T[], onReorder?: (newItems: T[]) => void) {
  const [isDragging, setIsDragging] = useState(false);
  const [draggedItem, setDraggedItem] = useState<string | null>(null);
  const draggedIndexRef = useRef<number>(-1);

  const handleDragStart = useCallback((e: React.DragEvent, item: T, index: number) => {
    setIsDragging(true);
    setDraggedItem(item.id);
    draggedIndexRef.current = index;
    
    const dragData: DragData = { id: item.id, index };
    e.dataTransfer.setData('application/json', JSON.stringify(dragData));
    e.dataTransfer.effectAllowed = 'move';
  }, []);

  const handleDragEnd = useCallback(() => {
    setIsDragging(false);
    setDraggedItem(null);
    draggedIndexRef.current = -1;
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
  }, []);

  const handleDrop = useCallback(
    (e: React.DragEvent, targetIndex: number) => {
      e.preventDefault();
      
      try {
        const dragDataStr = e.dataTransfer.getData('application/json');
        if (!dragDataStr) return;
        
        const dragData: DragData = JSON.parse(dragDataStr);
        const sourceIndex = dragData.index;
        
        if (sourceIndex === targetIndex) return;

        const reorderedItems = Array.from(items);
        const [movedItem] = reorderedItems.splice(sourceIndex, 1);
        reorderedItems.splice(targetIndex, 0, movedItem);

        if (onReorder) {
          onReorder(reorderedItems);
        }
      } catch (error) {
        console.error('Error handling drop:', error);
      }
    },
    [items, onReorder],
  );

  return {
    isDragging,
    draggedItem,
    handleDragStart,
    handleDragEnd,
    handleDragOver,
    handleDrop,
  };
}

