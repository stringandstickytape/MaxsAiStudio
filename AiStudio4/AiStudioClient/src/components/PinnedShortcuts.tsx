// AiStudioClient/src/components/PinnedShortcuts.tsx
import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { usePinnedCommandsStore, PinnedCommand } from '@/stores/usePinnedCommandsStore';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors, DragEndEvent } from '@dnd-kit/core';
import { SortableContext, horizontalListSortingStrategy, arrayMove } from '@dnd-kit/sortable';
import { CategoryPill } from './PinnedShortcuts/CategoryPill';
import { ShortcutPopup } from './PinnedShortcuts/ShortcutPopup';
import { Pin } from 'lucide-react';

export function PinnedShortcuts() {
  const { pinnedCommands, categoryOrder, setCategoryOrder, savePinnedCommands, fetchPinnedCommands } = usePinnedCommandsStore();
  const [activeCategoryId, setActiveCategoryId] = useState<string | null>(null);

  useEffect(() => {
    fetchPinnedCommands();
  }, [fetchPinnedCommands]);

  const sensors = useSensors(useSensor(PointerSensor), useSensor(KeyboardSensor));

  const groupedCommands = useMemo(() => {
    const groups: Record<string, PinnedCommand[]> = {};
    pinnedCommands.forEach(cmd => {
      const category = cmd.section || 'Uncategorized';
      if (!groups[category]) groups[category] = [];
      groups[category].push(cmd);
    });
    return groups;
  }, [pinnedCommands]);

  const orderedCategories = useMemo(() => {
    const existingCategories = new Set(Object.keys(groupedCommands));
    return categoryOrder.filter(cat => existingCategories.has(cat));
  }, [categoryOrder, groupedCommands]);

  const handleCategoryDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      const oldIndex = orderedCategories.indexOf(active.id as string);
      const newIndex = orderedCategories.indexOf(over.id as string);
      const newOrder = arrayMove(orderedCategories, oldIndex, newIndex);
      setCategoryOrder(newOrder);
      savePinnedCommands();
    }
  }, [orderedCategories, setCategoryOrder, savePinnedCommands]);

  const togglePopup = (categoryId: string) => {
    setActiveCategoryId(prev => (prev === categoryId ? null : categoryId));
  };

  if (pinnedCommands.length === 0) {
    return (
      <div className="flex items-center justify-center h-10 text-xs gap-1.5" style={{ color: 'var(--global-secondary-text-color)' }}>
        <Pin className="h-3 w-3" />
        Pin commands from Command Bar (Ctrl+K)
      </div>
    );
  }

  return (
    <div className="flex justify-center w-full py-1">
      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleCategoryDragEnd}>
        <SortableContext items={orderedCategories} strategy={horizontalListSortingStrategy}>
          <div className="flex items-center gap-2">
            {orderedCategories.map(categoryName => (
              <CategoryPill
                key={categoryName}
                id={categoryName}
                categoryName={categoryName}
                isActive={activeCategoryId === categoryName}
                onClick={() => togglePopup(categoryName)}
              >
                {activeCategoryId === categoryName && (
                  <ShortcutPopup
                    categoryName={categoryName}
                    commands={groupedCommands[categoryName] || []}
                    onClose={() => setActiveCategoryId(null)}
                  />
                )}
              </CategoryPill>
            ))}
          </div>
        </SortableContext>
      </DndContext>
    </div>
  );
}