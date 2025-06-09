// AiStudioClient\src\components\CategoryPill.tsx
import React, { forwardRef } from 'react';
import { cn } from '@/lib/utils';
import { getCategoryInfo, getCategoryPillBackground, getCategoryPillBorder } from './categoryUtils';
import { PinnedCommand } from '@/stores/usePinnedCommandsStore';

interface CategoryPillProps {
  categoryId: string;
  commandCount: number;
  commands: PinnedCommand[];
  isActive?: boolean;
  onClick: () => void;
  onDragStart?: (e: React.DragEvent, categoryId: string) => void;
  onDragEnd?: (e: React.DragEvent) => void;
  onDragOver?: (e: React.DragEvent) => void;
  onDrop?: (e: React.DragEvent, categoryId: string) => void;
  isDragging?: boolean;
  className?: string;
}

export const CategoryPill = forwardRef<HTMLButtonElement, CategoryPillProps>(({
  categoryId,
  commandCount,
  commands,
  isActive = false,
  onClick,
  onDragStart,
  onDragEnd,
  onDragOver,
  onDrop,
  isDragging = false,
  className
}, ref) => {
  const categoryInfo = getCategoryInfo(categoryId);
  const IconComponent = categoryInfo.icon;

  const handleDragStart = (e: React.DragEvent) => {
    onDragStart?.(e, categoryId);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    onDragOver?.(e);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    onDrop?.(e, categoryId);
  };

  return (
    <button
      ref={ref}
      className={cn(
        'CategoryPill flex items-center gap-2 px-4 py-2 rounded-lg border transition-all duration-200',
        'cursor-pointer select-none',
        getCategoryPillBackground(categoryId),
        getCategoryPillBorder(categoryId),
        isActive && 'ring-2 ring-white/30',
        isDragging && 'opacity-50 scale-95',
        'hover:scale-105 hover:shadow-lg',
        className
      )}
      onClick={onClick}
      draggable
      onDragStart={handleDragStart}
      onDragEnd={onDragEnd}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
      title={`${categoryInfo.displayName} (${commandCount} ${commandCount === 1 ? 'command' : 'commands'})`}
      style={{
        fontFamily: 'var(--global-font-family, inherit)',
        fontSize: 'var(--global-font-size, inherit)',
        color: 'var(--global-text-color, #ffffff)',
      }}
    >
      <IconComponent className="h-5 w-5 flex-shrink-0" />
      <span className="font-medium text-sm whitespace-nowrap">
        {categoryInfo.displayName}
      </span>
      <span className="bg-white/20 text-xs px-2 py-0.5 rounded-full min-w-[1.5rem] text-center">
        {commandCount}
      </span>
    </button>
  );
});

CategoryPill.displayName = 'CategoryPill';