// AiStudioClient/src/components/PinnedShortcuts/ShortcutItem.tsx
import React from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { PinnedCommand } from '@/types/command';
import { getIconForCommand } from '../pinnedShortcutsUtils'; // Assuming icon logic is moved to a utility
import { GripVertical } from 'lucide-react';
import { useCommandStore } from '@/stores/useCommandStore';
import { cn } from '@/lib/utils';

interface ShortcutItemProps {
  command: PinnedCommand;
}

export function ShortcutItem({ command }: ShortcutItemProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: command.id });
  const executeCommand = useCommandStore(state => state.executeCommand);

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    color: 'var(--global-text-color)',
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "flex items-center gap-2 p-1.5 rounded w-full cursor-pointer",
        "hover:bg-[color:var(--global-primary-color)] hover:bg-opacity-20",
        isDragging && "opacity-50 shadow-lg bg-[color:var(--global-primary-color)] bg-opacity-30"
      )}
      onClick={() => executeCommand(command.id)}
    >
      <div
        {...attributes}
        {...listeners}
        className="p-1 cursor-grab"
        style={{ color: 'var(--global-secondary-text-color)' }}
        onClick={(e) => e.stopPropagation()}
      >
        <GripVertical size={16} />
      </div>
      <div className="flex-shrink-0">
        {getIconForCommand(command.id, command.iconName, command.iconSet)}
      </div>
      <div className="flex-grow truncate text-sm">
        {command.name}
      </div>
    </div>
  );
}