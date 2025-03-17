import React, { useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Paperclip, FilePlus, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Attachment } from '@/types/attachment';
import { AttachmentButton, AttachmentPreviewBar } from './AttachmentPreview';
import { useAttachmentManager } from '@/hooks/useAttachmentManager';

interface FileAttachmentProps {
  onAttachmentChange?: (attachments: Attachment[]) => void;
  disabled?: boolean;
  className?: string;
  maxFiles?: number;
  acceptedTypes?: string;
  maxFileSize?: number; // in bytes
  showPreview?: boolean;
}

export const FileAttachment: React.FC<FileAttachmentProps> = ({
  onAttachmentChange,
  disabled = false,
  className,
  maxFiles = 5,
  acceptedTypes = ".jpg,.jpeg,.png,.gif,.pdf,.txt,.md,.js,.jsx,.ts,.tsx,.py,.html,.css,.json,.csv",
  maxFileSize = 10 * 1024 * 1024, // 10MB
  showPreview = true
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const {
    attachments,
    error,
    addAttachment,
    addAttachments,
    removeAttachment,
    clearAttachments
  } = useAttachmentManager({
    maxCount: maxFiles,
    maxSize: maxFileSize
  });

  // Notify parent component when attachments change
  React.useEffect(() => {
    onAttachmentChange?.(attachments);
  }, [attachments, onAttachmentChange]);

  const handleButtonClick = () => {
    if (localStorage.getItem('isVisualStudio') === 'true') {
      window.chrome?.webview?.postMessage({
        type: 'send',
        content: 'test',
        selectedTools: '',
        addEmbeddings: 'false',
      });

      if (inputRef.current) {
        inputRef.current.value = '';
      }
      return;
    }

    if (inputRef.current) {
      inputRef.current.click();
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files || files.length === 0) return;

    await addAttachments(files);

    // Reset input value to allow selecting the same file again
    if (inputRef.current) {
      inputRef.current.value = '';
    }
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDrop = async (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    if (!e.dataTransfer.files || e.dataTransfer.files.length === 0) return;
    await addAttachments(e.dataTransfer.files);
  };

  return (
    <div 
      className={cn(
        'flex flex-col relative',
        className
      )}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <input
        ref={inputRef}
        type="file"
        accept={acceptedTypes}
        onChange={handleFileChange}
        className="hidden"
        disabled={disabled}
        multiple
      />
      
      {/* Drag overlay */}
      {isDragging && (
        <div className="absolute inset-0 bg-blue-500/20 border-2 border-dashed border-blue-500 rounded-lg flex items-center justify-center z-10">
          <p className="text-blue-500 font-medium">Drop files here</p>
        </div>
      )}
      
      {/* Attachment button */}
      <Button
        variant="outline"
        size="icon"
        type="button"
        onClick={handleButtonClick}
        disabled={disabled || attachments.length >= maxFiles}
        className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
        aria-label="Attach file"
        title={attachments.length >= maxFiles ? `Maximum of ${maxFiles} files allowed` : 'Attach file'}
      >
        <Paperclip className="h-5 w-5" />
      </Button>
      
      {/* Error message */}
      {error && (
        <div className="mt-2 text-xs text-red-400">{error}</div>
      )}
    </div>
  );
};

interface AttachedFileProps {
  filename: string;
  onRemove: () => void;
}

export const AttachedFileDisplay: React.FC<AttachedFileProps> = ({ filename, onRemove }) => {
  return (
    <div className="flex items-center gap-2 py-1 px-2 bg-gray-800 rounded border border-gray-700 text-sm text-gray-300">
      <span className="truncate max-w-[150px]">{filename}</span>
      <Button
        variant="ghost"
        size="icon"
        onClick={onRemove}
        className="h-5 w-5 p-0 text-gray-400 hover:text-gray-100 hover:bg-gray-700/50"
      >
        <X className="h-3 w-3" />
      </Button>
    </div>
  );
};
