import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { FilePlus, X, File, FileText, Image, Download } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Attachment } from '@/types/attachment';
import { getIconForFileType, formatFileSize } from '@/utils/attachmentUtils';

interface AttachmentPreviewProps {
    attachment: Attachment;
    onRemove: (id: string) => void;
    className?: string;
    iconsOnly?: boolean;
    compact?: boolean;
}

const getIconComponent = (type: string) => {
    const iconType = getIconForFileType(type);
    switch (iconType) {
        case 'Image': return <Image className="h-5 w-5" />;
        case 'FileText': return <FileText className="h-5 w-5" />;
        default: return <File className="h-5 w-5" />;
    }
};

const downloadAttachment = (attachment: Attachment) => {
    const blob = new Blob([attachment.content]);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = attachment.name;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

export const AttachmentPreview: React.FC<AttachmentPreviewProps> = ({
    attachment,
    onRemove,
    className,
    iconsOnly = false,
    compact = false
}) => {
    const isImage = attachment.type.startsWith('image/');
    const isPdf = attachment.type === 'application/pdf';
    const fileSize = formatFileSize(attachment.size);

    // Compact mode view
    if (compact) {
        return (
            <div
                className={cn(
                    'relative group',
                    className
                )}
                title={`${attachment.name} (${fileSize})`}
            >
                <div className="aspect-square w-10 h-10 bg-gray-800 rounded-md border border-gray-700 overflow-hidden flex items-center justify-center">
                    {isImage && attachment.previewUrl ? (
                        <img
                            src={attachment.previewUrl}
                            alt={attachment.name}
                            className="w-full h-full object-cover"
                        />
                    ) : isPdf && attachment.previewUrl ? (
                        <div className="flex items-center justify-center h-full w-full bg-gray-700">
                            <FileText className="h-5 w-5 text-gray-300" />
                        </div>
                    ) : (
                        getIconComponent(attachment.type)
                    )}
                </div>
                <div className="absolute top-1 right-1 flex flex-col gap-1 opacity-0 group-hover:opacity-100">
                    <button
                        onClick={() => downloadAttachment(attachment)}
                        className="h-5 w-5 bg-gray-700 rounded-full flex items-center justify-center text-gray-300 hover:text-white hover:bg-blue-600 transition-colors mb-0.5"
                        title={`Download ${attachment.name}`}
                    >
                        <Download className="h-3 w-3" />
                    </button>
                    <button
                        onClick={() => onRemove(attachment.id)}
                        className="h-5 w-5 bg-gray-700 rounded-full flex items-center justify-center text-gray-300 hover:text-white hover:bg-red-600 transition-colors"
                        title={`Remove ${attachment.name}`}
                    >
                        <X className="h-3 w-3" />
                    </button>
                </div>
            </div>
        );
    }

    // Regular view
    return (
        <div
            className={cn(
                'flex items-center gap-2 p-2 bg-gray-800 rounded border border-gray-700 group',
                className
            )}
        >
            {isImage && attachment.previewUrl ? (
                <div className="relative w-10 h-10 bg-gray-700 rounded overflow-hidden flex-shrink-0">
                    <img
                        src={attachment.previewUrl}
                        alt={attachment.name}
                        className="w-full h-full object-cover"
                    />
                </div>
            ) : isPdf && attachment.previewUrl ? (
                <div className="relative w-10 h-10 bg-gray-700 rounded overflow-hidden flex-shrink-0 flex items-center justify-center">
                    <FileText className="h-6 w-6 text-gray-300" />
                </div>
            ) : (
                <div className="w-10 h-10 bg-gray-700 rounded flex items-center justify-center flex-shrink-0">
                    {getIconComponent(attachment.type)}
                </div>
            )}

            {!iconsOnly && (
                <div className="flex-1 min-w-0">
                    <div className="truncate text-sm font-medium text-gray-200">{attachment.name}</div>
                    <div className="text-xs text-gray-400">{fileSize}</div>
                </div>
            )}

            <div className="flex flex-col gap-1">
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => downloadAttachment(attachment)}
                    className="h-6 w-6 p-0 text-gray-400 hover:text-blue-400 hover:bg-gray-700/50 opacity-0 group-hover:opacity-100 transition-opacity"
                    title={`Download ${attachment.name}`}
                >
                    <Download className="h-3 w-3" />
                </Button>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => onRemove(attachment.id)}
                    className="h-6 w-6 p-0 text-gray-400 hover:text-gray-100 hover:bg-gray-700/50 opacity-0 group-hover:opacity-100 transition-opacity"
                    title={`Remove ${attachment.name}`}
                >
                    <X className="h-3 w-3" />
                </Button>
            </div>
        </div>
    );
};

interface AttachmentPreviewBarProps {
    attachments: Attachment[];
    onRemove: (id: string) => void;
    onClear?: () => void;
    className?: string;
    iconsOnly?: boolean;
    compact?: boolean;
}

export const AttachmentPreviewBar: React.FC<AttachmentPreviewBarProps> = ({
    attachments,
    onRemove,
    onClear,
    className,
    iconsOnly: propIconsOnly,
    compact: propCompact,
}) => {
    const [viewportWidth, setViewportWidth] = useState(window.innerWidth);
    const [iconsOnly, setIconsOnly] = useState(propIconsOnly || false);
    const [compact, setCompact] = useState(propCompact || false);

    // Responsive behavior
    useEffect(() => {
        const handleResize = () => {
            setViewportWidth(window.innerWidth);

            if (propIconsOnly === undefined) {
                setIconsOnly(window.innerWidth < 768); // MD breakpoint
            }

            // Set compact view for small screens
            if (propCompact === undefined) {
                setCompact(window.innerWidth < 640); // SM breakpoint
            }
        };

        window.addEventListener('resize', handleResize);
        handleResize(); // Initial call

        return () => window.removeEventListener('resize', handleResize);
    }, [propIconsOnly, propCompact]);

    // Prop overrides
    useEffect(() => {
        if (propIconsOnly !== undefined) {
            setIconsOnly(propIconsOnly);
        }
        if (propCompact !== undefined) {
            setCompact(propCompact);
        }
    }, [propIconsOnly, propCompact]);

    if (attachments.length === 0) return null;

    // Compact view
    if (compact) {
        return (
            <div className={cn(
                'h-full overflow-auto p-1',
                className
            )}>
                <div className="flex flex-col items-center gap-1">
                    {attachments.length > 1 && onClear && (
                        <button
                            onClick={onClear}
                            className="h-6 flex items-center justify-center mb-1 px-1.5 text-xs text-gray-400 hover:text-gray-200 bg-gray-800/50 rounded-md mx-auto"
                            title="Clear all attachments"
                        >
                            <X className="h-3 w-3" />
                            <span className="sr-only">Clear all attachments</span>
                        </button>
                    )}

                    {attachments.map((attachment) => (
                        <AttachmentPreview
                            key={attachment.id}
                            attachment={attachment}
                            onRemove={onRemove}
                            compact={true}
                        />
                    ))}
                </div>
            </div>
        );
    }

    // Regular view
    return (
        <div className={cn('p-2 bg-gray-800/50 rounded border border-gray-700/50 h-full overflow-auto', className)}>
            <div className="flex items-center justify-between mb-2">
                <div className="text-sm font-medium text-gray-300">
                    {!iconsOnly ? `Attachments (${attachments.length})` : ''}
                </div>
                {onClear && attachments.length > 1 && (
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={onClear}
                        className="h-6 px-2 py-0 text-xs"
                        title="Clear all attachments"
                    >
                        {iconsOnly ? <X className="h-3 w-3" /> : "Clear All"}
                    </Button>
                )}
            </div>
            <div className="flex flex-col gap-2">
                {attachments.map((attachment) => (
                    <AttachmentPreview
                        key={attachment.id}
                        attachment={attachment}
                        onRemove={onRemove}
                        className="w-full"
                        iconsOnly={iconsOnly}
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