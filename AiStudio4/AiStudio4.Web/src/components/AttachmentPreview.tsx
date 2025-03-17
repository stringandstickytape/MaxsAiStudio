import React from 'react';
import { X, File, FileText, Image, FilePlus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { Attachment } from '@/types/attachment';

interface AttachmentPreviewProps {
  attachment: Attachment;
  onRemove: (id: string) => void;
  className?: string;
}

const getIconForType = (type: string) => {
  if (type.startsWith('image/')) {
    return <Image className="h-5 w-5" />;
  }
  if (type.startsWith('text/')) {
    return <FileText className="h-5 w-5" />;
  }
  return <File className="h-5 w-5" />;
};

const formatFileSize = (bytes: number) => {
  if (bytes < 1024) {
    return `${bytes} B`;
  } else if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  } else {
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
};

export const AttachmentPreview: React.FC<AttachmentPreviewProps> = ({ attachment, onRemove, className }) => {
  const isImage = attachment.type.startsWith('image/');

  return (
    <div
      className={cn(
        'flex items-center gap-2 p-2 bg-gray-800 rounded border border-gray-700 group',
        className
      )}
    >
      {isImage && attachment.previewUrl ? (
        <div className="relative w-12 h-12 bg-gray-700 rounded overflow-hidden flex-shrink-0">
          <img
            src={attachment.previewUrl}
            alt={attachment.name}
            className="w-full h-full object-cover"
          />
        </div>
      ) : (
        <div className="w-12 h-12 bg-gray-700 rounded flex items-center justify-center flex-shrink-0">
          {getIconForType(attachment.type)}
        </div>
      )}

      <div className="flex-1 min-w-0">
        <div className="truncate text-sm font-medium text-gray-200">{attachment.name}</div>
        <div className="text-xs text-gray-400">{formatFileSize(attachment.size)}</div>
      </div>

      <Button
        variant="ghost"
        size="icon"
        className="h-6 w-6 p-0 text-gray-400 hover:text-gray-100 hover:bg-gray-700 opacity-0 group-hover:opacity-100 transition-opacity"
        onClick={() => onRemove(attachment.id)}
      >
        <X className="h-4 w-4" />
      </Button>
    </div>
  );
};

interface AttachmentPreviewBarProps {
  attachments: Attachment[];
  onRemove: (id: string) => void;
  onClear?: () => void;
  className?: string;
}

export const AttachmentPreviewBar: React.FC<AttachmentPreviewBarProps> = ({
  attachments,
  onRemove,
  onClear,
  className,
}) => {
  if (attachments.length === 0) return null;

  return (
    <div className={cn('p-2 bg-gray-800/50 rounded border border-gray-700/50', className)}>
      <div className="flex items-center justify-between mb-2">
        <div className="text-sm font-medium text-gray-300">Attachments ({attachments.length})</div>
        {onClear && attachments.length > 1 && (
          <Button variant="ghost" size="sm" onClick={onClear} className="h-6 px-2 py-0 text-xs">
            Clear All
          </Button>
        )}
      </div>
      <div className="flex flex-wrap gap-2">
        {attachments.map((attachment) => (
          <AttachmentPreview
            key={attachment.id}
            attachment={attachment}
            onRemove={onRemove}
            className="w-full sm:w-auto"
          />
        ))}
      </div>
    </div>
  );
};

interface AttachmentButtonProps {
  onClick: () => void;
  disabled?: boolean;
  className?: string;
}

export const AttachmentButton: React.FC<AttachmentButtonProps> = ({ onClick, disabled, className }) => {
  return (
    <Button
      variant="outline"
      size="icon"
      onClick={onClick}
      disabled={disabled}
      className={cn('btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400', className)}
      aria-label="Attach file"
    >
      <FilePlus className="h-5 w-5" />
    </Button>
  );
};
