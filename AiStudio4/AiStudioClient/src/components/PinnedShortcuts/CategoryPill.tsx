// AiStudioClient/src/components/PinnedShortcuts/CategoryPill.tsx
import React from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Button } from '@/components/ui/button';
import { useFloating, useClick, useInteractions, FloatingPortal } from '@floating-ui/react';

interface CategoryPillProps {
  id: string;
  categoryName: string;
  isActive: boolean;
  onClick: () => void;
  children: React.ReactNode;
}

export function CategoryPill({ id, categoryName, isActive, onClick, children }: CategoryPillProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id });
  
  const { refs, context } = useFloating({
    open: isActive,
    onOpenChange: (open) => !open && onClick(),
  });

  const click = useClick(context);
  const { getReferenceProps } = useInteractions([click]);

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    backgroundColor: isActive ? 'var(--global-primary-color)' : 'var(--global-background-color)',
    color: isActive ? 'white' : 'var(--global-text-color)',
    borderColor: 'var(--global-border-color)',
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div ref={setNodeRef} style={style} {...attributes}>
      <Button
        ref={refs.setReference}
        variant="outline"
        size="sm"
        className="flex items-center gap-2 cursor-pointer h-7"
        {...listeners}
        {...getReferenceProps({ onClick })}
      >
        {categoryName}
      </Button>
      <FloatingPortal>{isActive && React.cloneElement(children as React.ReactElement, { floatingContext: context, floatingRefs: refs })}</FloatingPortal>
    </div>
  );
}