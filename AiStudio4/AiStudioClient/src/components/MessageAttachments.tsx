import React from 'react';
import { File, FileText, Image, Download, Music } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { Attachment } from '@/types/attachment';
import { getIconForFileType, formatFileSize } from '@/utils/attachmentUtils';
import { AudioAttachment } from '@/components/AudioAttachment';

interface MessageAttachmentProps {
    attachment: Attachment;
    className?: string;
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

export const MessageAttachment: React.FC<MessageAttachmentProps> = ({ attachment, className }) => {
    const isImage = attachment.type.startsWith('image/');
    const isPdf = attachment.type === 'application/pdf';

    return (
        <div
            className={cn(
                'relative group overflow-hidden rounded border border-gray-700/50 bg-gray-800/70',
                isImage || isPdf ? 'hover:border-blue-500/50 w-[120px]' : '',
                className
            )}
        >
            {isImage && attachment.previewUrl ? (
                <div className="relative aspect-square overflow-hidden bg-gray-900/50">
                    <img
                        src={attachment.previewUrl}
                        alt={attachment.name}
                        className="w-full h-full object-contain max-w-full"
                    />
                    <div className="absolute inset-0 bg-gradient-to-b from-transparent to-black/30 opacity-0 group-hover:opacity-100 transition-opacity duration-200"></div>
                    <div className="absolute bottom-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 rounded-full bg-gray-800/80 hover:bg-gray-700 text-gray-200"
                            onClick={() => downloadAttachment(attachment)}
                            title="Download"
                        >
                            <Download className="h-4 w-4" />
                        </Button>
                    </div>
                </div>
            ) : isPdf && attachment.previewUrl ? (
                <div className="relative aspect-square overflow-hidden bg-gray-900/50 flex items-center justify-center">
                    <div className="flex flex-col items-center justify-center">
                        <FileText className="h-8 w-8 text-gray-300 mb-1" />
                        <div className="text-xs text-gray-300">PDF Document</div>
                    </div>
                    <div className="absolute inset-0 bg-gradient-to-b from-transparent to-black/30 opacity-0 group-hover:opacity-100 transition-opacity duration-200"></div>
                    <div className="absolute bottom-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 rounded-full bg-gray-800/80 hover:bg-gray-700 text-gray-200"
                            onClick={() => downloadAttachment(attachment)}
                            title="Download"
                        >
                            <Download className="h-4 w-4" />
                        </Button>
                    </div>
                </div>
            ) : (
                <div className="p-3 flex items-center gap-3">
                    <div className="w-10 h-10 bg-gray-700 rounded flex items-center justify-center flex-shrink-0">
                        {getIconComponent(attachment.type)}
                    </div>
                    <div className="flex-1 min-w-0">
                        <div className="truncate text-sm font-medium text-gray-200">{attachment.name}</div>
                        <div className="text-xs text-gray-400">{formatFileSize(attachment.size)}</div>
                    </div>
                    <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-gray-400 hover:text-gray-100 hover:bg-gray-700"
                        onClick={() => downloadAttachment(attachment)}
                        title="Download"
                    >
                        <Download className="h-4 w-4" />
                    </Button>
                </div>
            )}
        </div>
    );
};

interface MessageAttachmentsProps {
    attachments: Attachment[];
    className?: string;
}

export const MessageAttachments: React.FC<MessageAttachmentsProps> = ({ attachments, className }) => {
    if (!attachments || attachments.length === 0) return null;

    const previews = attachments.filter(att => att.type.startsWith('image/') || att.type === 'application/pdf');
    const audioFiles = attachments.filter(att => att.type.startsWith('audio/'));
    const files = attachments.filter(att => 
        !att.type.startsWith('image/') && 
        !att.type.startsWith('audio/') && 
        att.type !== 'application/pdf');

    return (
        <div className={cn('space-y-2', className)}>
            {previews.length > 0 && (
                <div className="flex flex-wrap gap-2">
                    {previews.map(attachment => (
                        <MessageAttachment key={attachment.id} attachment={attachment} className="flex-shrink-0" />
                    ))}
                </div>
            )}
            {audioFiles.length > 0 && (
                <div className="flex flex-col space-y-2">
                    {audioFiles.map(attachment => (
                        <AudioAttachment key={attachment.id} attachment={attachment} />
                    ))}
                </div>
            )}
            {files.length > 0 && (
                <div className="flex flex-col space-y-2">
                    {files.map(attachment => (
                        <MessageAttachment key={attachment.id} attachment={attachment} />
                    ))}
                </div>
            )}
        </div>
    );
};