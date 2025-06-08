// AiStudioClient/src/components/PinnedShortcuts/ShortcutPopup.tsx
import React from 'react';
import { FloatingFocusManager, useDismiss, useInteractions } from '@floating-ui/react';
import { DndContext, closestCenter, KeyboardSensor, PointerSensor, useSensor, useSensors, DragEndEvent } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { usePinnedCommandsStore, PinnedCommand } from '@/stores/usePinnedCommandsStore';
import { ShortcutItem } from './ShortcutItem';

interface ShortcutPopupProps {
  categoryName: string;
  commands: PinnedCommand[];
  onClose: () => void;
  floatingContext: any; // from @floating-ui/react
  floatingRefs: any;    // from @floating-ui/react
}

export function ShortcutPopup({ categoryName, commands, onClose, floatingContext, floatingRefs }: ShortcutPopupProps) {
  const { reorderPinnedCommands, savePinnedCommands } = usePinnedCommandsStore();
  const sensors = useSensors(useSensor(PointerSensor), useSensor(KeyboardSensor));

  const dismiss = useDismiss(floatingContext);
  const { getFloatingProps } = useInteractions([dismiss]);

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      reorderPinnedCommands(active.id as string, over.id as string);
      savePinnedCommands();
    }
  };

  return (
    <FloatingFocusManager context={floatingContext} modal={false}>
      <div
        ref={floatingRefs.setFloating}
        style={{
          ...floatingContext.floatingStyles,
          backgroundColor: 'rgba(31, 41, 55, 0.8)', // semi-translucent dark background
          backdropFilter: 'blur(8px)',
          border: '1px solid var(--global-border-color)',
          borderRadius: 'var(--global-border-radius)',
          boxShadow: 'var(--global-box-shadow)',
          padding: '0.5rem',
          zIndex: 1000,
        }}
        className="w-64"
        {...getFloatingProps()}
      >
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
          <SortableContext items={commands.map(c => c.id)} strategy={verticalListSortingStrategy}>
            <div className="flex flex-col gap-1">
              {commands.map(command => (
                <ShortcutItem key={command.id} command={command} />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      </div>
    </FloatingFocusManager>
  );
}