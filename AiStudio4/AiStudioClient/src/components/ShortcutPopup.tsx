// AiStudioClient\src\components\ShortcutPopup.tsx
import React, { useEffect, useRef, useState, useCallback } from 'react';
import { createPortal } from 'react-dom';
import { cn } from '@/lib/utils';
import { X, GripVertical } from 'lucide-react';
import { getCategoryInfo } from './categoryUtils';
import { getIconForCommand } from './pinnedShortcutsUtils';
import { PinnedCommand } from '@/stores/usePinnedCommandsStore';

interface ShortcutPopupProps {
  isOpen: boolean;
  categoryId: string;
  commands: PinnedCommand[];
  anchorElement: HTMLElement | null;
  onClose: () => void;
  onCommandClick: (commandId: string) => void;
  onCommandsReorder: (categoryId: string, commandIds: string[]) => void;
  onRemoveCommand: (commandId: string) => void;
}

export const ShortcutPopup: React.FC<ShortcutPopupProps> = ({
  isOpen,
  categoryId,
  commands,
  anchorElement,
  onClose,
  onCommandClick,
  onCommandsReorder,
  onRemoveCommand
}) => {
  const popupRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState({ top: 0, left: 0 });
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null);

  const categoryInfo = getCategoryInfo(categoryId);

  // Calculate popup position
  useEffect(() => {
    if (!isOpen || !anchorElement) return;

    const updatePosition = () => {
      const anchorRect = anchorElement.getBoundingClientRect();
      const viewportWidth = window.innerWidth;
      const viewportHeight = window.innerHeight;
      
      // Default position below the anchor
      let top = anchorRect.bottom + 8;
      let left = anchorRect.left;

      // Adjust if popup would go off-screen
      const popupWidth = 280; // Estimated popup width
      const popupHeight = Math.min(400, commands.length * 60 + 100); // Estimated height

      // Adjust horizontal position
      if (left + popupWidth > viewportWidth) {
        left = viewportWidth - popupWidth - 16;
      }
      if (left < 16) {
        left = 16;
      }

      // Adjust vertical position
      if (top + popupHeight > viewportHeight) {
        top = anchorRect.top - popupHeight - 8;
        if (top < 16) {
          top = 16;
        }
      }

      setPosition({ top, left });
    };

    updatePosition();
    window.addEventListener('resize', updatePosition);
    window.addEventListener('scroll', updatePosition);

    return () => {
      window.removeEventListener('resize', updatePosition);
      window.removeEventListener('scroll', updatePosition);
    };
  }, [isOpen, anchorElement, commands.length]);

  // Handle clicks outside popup
  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (popupRef.current && !popupRef.current.contains(event.target as Node)) {
        onClose();
      }
    };

    const handleEscapeKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('keydown', handleEscapeKey);

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('keydown', handleEscapeKey);
    };
  }, [isOpen, onClose]);

  // Drag and drop handlers
  const handleDragStart = useCallback((e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', commands[index].id);
  }, [commands]);

  const handleDragEnd = useCallback(() => {
    setDraggedIndex(null);
    setDragOverIndex(null);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent, index: number) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    setDragOverIndex(index);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent, dropIndex: number) => {
    e.preventDefault();
    
    if (draggedIndex === null || draggedIndex === dropIndex) {
      setDraggedIndex(null);
      setDragOverIndex(null);
      return;
    }

    const reorderedCommands = [...commands];
    const [draggedCommand] = reorderedCommands.splice(draggedIndex, 1);
    reorderedCommands.splice(dropIndex, 0, draggedCommand);

    onCommandsReorder(categoryId, reorderedCommands.map(cmd => cmd.id));
    
    setDraggedIndex(null);
    setDragOverIndex(null);
  }, [draggedIndex, commands, categoryId, onCommandsReorder]);

  const handleCommandClick = useCallback((commandId: string) => {
    onCommandClick(commandId);
    onClose();
  }, [onCommandClick, onClose]);

  const handleRemoveCommand = useCallback((e: React.MouseEvent, commandId: string) => {
    e.stopPropagation();
    onRemoveCommand(commandId);
  }, [onRemoveCommand]);

  if (!isOpen) return null;

  const IconComponent = categoryInfo.icon;

  return createPortal(
    <div className="fixed inset-0 z-50 pointer-events-none">
      {/* Semi-transparent backdrop */}
      <div className="absolute inset-0 bg-black/20 backdrop-blur-sm pointer-events-auto" />
      
      {/* Popup */}
      <div
        ref={popupRef}
        className="absolute pointer-events-auto"
        style={{
          top: `${position.top}px`,
          left: `${position.left}px`,
        }}
      >
        <div className={cn(
          'bg-gray-900/95 backdrop-blur-md border border-gray-700/50 rounded-lg shadow-2xl',
          'min-w-[280px] max-w-[320px] max-h-[400px] overflow-hidden',
          'animate-in fade-in-0 zoom-in-95 duration-200'
        )}>
          {/* Header */}
          <div className="flex items-center gap-3 p-4 border-b border-gray-700/50">
            <IconComponent className="h-5 w-5 text-gray-300" />
            <h3 className="font-semibold text-white flex-1">
              {categoryInfo.displayName}
            </h3>
            <span className="text-xs text-gray-400">
              {commands.length} {commands.length === 1 ? 'command' : 'commands'}
            </span>
            <button
              onClick={onClose}
              className="p-1 hover:bg-gray-700/50 rounded transition-colors"
            >
              <X className="h-4 w-4 text-gray-400" />
            </button>
          </div>

          {/* Commands list */}
          <div className="max-h-[300px] overflow-y-auto">
            {commands.length === 0 ? (
              <div className="p-4 text-center text-gray-400">
                No commands in this category
              </div>
            ) : (
              <div className="p-2">
                {commands.map((command, index) => (
                  <div
                    key={command.id}
                    className={cn(
                      'flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-all',
                      'hover:bg-gray-700/50 group',
                      draggedIndex === index && 'opacity-50',
                      dragOverIndex === index && 'bg-gray-700/30 border-t-2 border-blue-400'
                    )}
                    draggable
                    onDragStart={(e) => handleDragStart(e, index)}
                    onDragEnd={handleDragEnd}
                    onDragOver={(e) => handleDragOver(e, index)}
                    onDrop={(e) => handleDrop(e, index)}
                    onClick={() => handleCommandClick(command.id)}
                  >
                    {/* Drag handle */}
                    <GripVertical className="h-4 w-4 text-gray-500 opacity-0 group-hover:opacity-100 transition-opacity cursor-grab" />
                    
                    {/* Command icon */}
                    <div className="flex-shrink-0">
                      {getIconForCommand(command.id, command.iconName, command.iconSet)}
                    </div>
                    
                    {/* Command name */}
                    <span className="flex-1 text-sm text-white truncate">
                      {command.name}
                    </span>
                    
                    {/* Remove button */}
                    <button
                      onClick={(e) => handleRemoveCommand(e, command.id)}
                      className="p-1 hover:bg-red-500/20 rounded opacity-0 group-hover:opacity-100 transition-all"
                      title="Remove from pinned"
                    >
                      <X className="h-3 w-3 text-red-400" />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>,
    document.body
  );
};