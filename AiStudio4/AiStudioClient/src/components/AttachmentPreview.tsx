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
    const iconStyle = { color: 'var(--global-secondary-text-color, #999999)' };
    switch (iconType) {
        case 'Image': return <Image className="h-5 w-5" style={iconStyle} />;
        case 'FileText': return <FileText className="h-5 w-5" style={iconStyle} />;
        default: return <File className="h-5 w-5" style={iconStyle} />;
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
                <div className="aspect-square w-10 h-10 rounded-md overflow-hidden flex items-center justify-center"
                    style={{
                        backgroundColor: 'var(--global-background-color, #1f2937)',
                        borderColor: 'var(--global-border-color, #4a5568)',
                        borderWidth: '1px',
                        borderStyle: 'solid'
                    }}
                >
                    {isImage && attachment.previewUrl ? (
                        <img
                            src={attachment.previewUrl}
                            alt={attachment.name}
                            className="w-full h-full object-cover"
                        />
                    ) : isPdf && attachment.previewUrl ? (
                        <div className="flex items-center justify-center h-full w-full"
                            style={{ backgroundColor: 'var(--global-secondary-color, #374151)' }}
                        >
                            <FileText className="h-5 w-5" style={{ color: 'var(--global-text-color, #ffffff)' }} />
                        </div>
                    ) : (
                        getIconComponent(attachment.type)
                    )}
                </div>
                <div className="absolute top-1 right-1 flex flex-col gap-1 opacity-0 group-hover:opacity-100">
                    <button
                        onClick={() => downloadAttachment(attachment)}
                        className="h-5 w-5 rounded-full flex items-center justify-center transition-colors mb-0.5"
                        style={{
                            backgroundColor: 'var(--global-secondary-color, #374151)',
                            color: 'var(--global-secondary-text-color, #999999)'
                        }}
                        onMouseEnter={(e) => {
                            e.currentTarget.style.backgroundColor = 'var(--global-primary-color, #3b82f6)';
                            e.currentTarget.style.color = 'var(--global-text-color, #ffffff)';
                        }}
                        onMouseLeave={(e) => {
                            e.currentTarget.style.backgroundColor = 'var(--global-secondary-color, #374151)';
                            e.currentTarget.style.color = 'var(--global-secondary-text-color, #999999)';
                        }}
                        title={`Download ${attachment.name}`}
                    >
                        <Download className="h-3 w-3" />
                    </button>
                    <button
                        onClick={() => onRemove(attachment.id)}
                        className="h-5 w-5 rounded-full flex items-center justify-center transition-colors"
                        style={{
                            backgroundColor: 'var(--global-secondary-color, #374151)',
                            color: 'var(--global-secondary-text-color, #999999)'
                        }}
                        onMouseEnter={(e) => {
                            e.currentTarget.style.backgroundColor = '#dc2626';
                            e.currentTarget.style.color = 'var(--global-text-color, #ffffff)';
                        }}
                        onMouseLeave={(e) => {
                            e.currentTarget.style.backgroundColor = 'var(--global-secondary-color, #374151)';
                            e.currentTarget.style.color = 'var(--global-secondary-text-color, #999999)';
                        }}
                        title={`Remove ${attachment.name}`}
                    >
                        <X className="h-3 w-3" />
                    </button>
                </div>
            </div>
        );
    }

    // Get file extension for file type label
    const getFileType = () => {
        const extension = attachment.name.split('.').pop()?.toUpperCase() || '';
        if (extension.length > 4) {
            // For longer extensions, use the mime type
            const mimeType = attachment.type.split('/')[1]?.toUpperCase() || 'FILE';
            return mimeType.length > 4 ? mimeType.substring(0, 4) : mimeType;
        }
        return extension || 'FILE';
    };

    // Regular view - 140px x 64px
    return (
        <div
            className={cn(
                'relative w-[140px] h-[56px] rounded group overflow-hidden flex',
                className
            )}
            style={{
                backgroundColor: 'var(--global-background-color, #1f2937)',
                borderColor: 'var(--global-border-color, #4a5568)',
                borderWidth: '1px',
                borderStyle: 'solid'
            }}
        >
            {/* Left side - icon */}
            <div className="w-12 flex items-center justify-center flex-shrink-0">
                {isImage && attachment.previewUrl ? (
                    <img
                        src={attachment.previewUrl}
                        alt={attachment.name}
                        className="h-10 w-10 object-cover rounded"
                    />
                ) : isPdf && attachment.previewUrl ? (
                    <FileText className="h-6 w-6" style={{ color: 'var(--global-secondary-text-color, #999999)' }} />
                ) : (
                    getIconComponent(attachment.type)
                )}
            </div>

            {/* Right side - all text info */}
            <div className="flex-1 flex flex-col justify-center min-w-0">
                <div 
                    className="text-xs font-medium text-gray-200 truncate"
                    title={attachment.name}
                    style={{ 
                        color: 'var(--global-text-color, #ffffff)',
                        fontSize: '11px'
                    }}
                >
                    {attachment.name}
                </div>
                <div className="flex items-center gap-1 mt-0.5">
                    <span 
                        className="inline-block px-1 py-0.5 text-xs font-bold rounded"
                        style={{
                            backgroundColor: 'var(--global-background-color, #1f2937)',
                            color: 'var(--global-text-color, #ffffff)',
                            borderColor: 'var(--global-border-color, #4a5568)',
                            borderWidth: '1px',
                            borderStyle: 'solid',
                            fontSize: '9px'
                        }}
                    >
                        {getFileType()}
                    </span>
                    <div 
                        className="text-xs text-gray-400"
                        style={{ 
                            color: 'var(--global-secondary-text-color, #999999)',
                            fontSize: '10px'
                        }}
                    >
                        {fileSize}
                    </div>
                </div>
            </div>
            
            {/* Action buttons - top right */}
            <div className="absolute top-1 right-2 z-10 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                {/* Download button */}
                <button
                    type="button"
                    onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        downloadAttachment(attachment);
                    }}
                    className="p-1 cursor-pointer transition-all rounded border-none"
                    style={{
                        backgroundColor: 'var(--global-secondary-color, #374151)',
                        color: 'var(--global-secondary-text-color, #999999)'
                    }}
                    onMouseEnter={(e) => {
                        e.currentTarget.style.backgroundColor = 'var(--global-primary-color, #3b82f6)';
                        e.currentTarget.style.color = 'var(--global-text-color, #ffffff)';
                    }}
                    onMouseLeave={(e) => {
                        e.currentTarget.style.backgroundColor = 'var(--global-secondary-color, #374151)';
                        e.currentTarget.style.color = 'var(--global-secondary-text-color, #999999)';
                    }}
                    title={`Download ${attachment.name}`}
                >
                    <Download className="h-3 w-3" />
                </button>
                
                {/* Remove button */}
                <button
                    type="button"
                    onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        onRemove(attachment.id);
                    }}
                    className="p-1 cursor-pointer transition-all rounded border-none"
                    style={{
                        backgroundColor: 'var(--global-secondary-color, #374151)',
                        color: 'var(--global-secondary-text-color, #999999)'
                    }}
                    onMouseEnter={(e) => {
                        e.currentTarget.style.backgroundColor = '#dc2626';
                        e.currentTarget.style.color = 'var(--global-text-color, #ffffff)';
                    }}
                    onMouseLeave={(e) => {
                        e.currentTarget.style.backgroundColor = 'var(--global-secondary-color, #374151)';
                        e.currentTarget.style.color = 'var(--global-secondary-text-color, #999999)';
                    }}
                    title={`Remove ${attachment.name}`}
                >
                    <X className="h-3 w-3" />
                </button>
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

    // Regular view - horizontal layout for input bar
    return (
        <div className={cn('flex gap-2 overflow-x-auto h-full', className)}>
            {attachments.map((attachment) => (
                <AttachmentPreview
                    key={attachment.id}
                    attachment={attachment}
                    onRemove={onRemove}
                    className="flex-shrink-0"
                    iconsOnly={iconsOnly}
                />
            ))}
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
            className={cn('btn-ghost icon-btn', className)}
            style={{
                backgroundColor: 'var(--global-background-color, #1f2937)',
                borderColor: 'var(--global-border-color, #4a5568)',
                color: 'var(--global-text-color, #ffffff)'
            }}
            onMouseEnter={(e) => {
                e.currentTarget.style.color = 'var(--global-primary-color, #3b82f6)';
            }}
            onMouseLeave={(e) => {
                e.currentTarget.style.color = 'var(--global-text-color, #ffffff)';
            }}
            aria-label="Attach file"
        >
            <FilePlus className="h-5 w-5" />
        </Button>
    );
};