// AiStudioClient\src\components\FileAttachment.tsx
import React, { useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Paperclip, X, Upload, ClipboardPen } from 'lucide-react';
import { cn } from '@/lib/utils';
import { DEFAULT_ATTACHMENT_OPTIONS, base64ToArrayBuffer } from '@/utils/attachmentUtils';
import { createApiRequest } from '@/utils/apiUtils';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

interface FileAttachmentProps {
    onFilesSelected: (files: FileList | File[]) => void;
    disabled?: boolean;
    className?: string;
    maxFiles?: number;
    acceptedTypes?: string;
    style?: React.CSSProperties; 

}

export const FileAttachment: React.FC<FileAttachmentProps> = ({
    onFilesSelected,
    disabled = false,
    className,
    maxFiles = DEFAULT_ATTACHMENT_OPTIONS.maxCount,
    acceptedTypes = ".jpg,.jpeg,.png,.gif,.pdf,.txt,.md,.js,.jsx,.ts,.tsx,.py,.html,.css,.json,.csv,application/pdf",
    style
}) => {
    const inputRef = useRef<HTMLInputElement>(null);
    const [isDragging, setIsDragging] = useState(false);

    const handleAttachFileClick = () => {
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

        onFilesSelected(files);

        
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
        onFilesSelected(e.dataTransfer.files);
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

            {/* Attachment dropdown menu */}
            <DropdownMenu>
                <DropdownMenuTrigger asChild>
                    <Button
                        variant="outline"
                        size="icon"
                        type="button"
                        disabled={disabled}
                        className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
                        aria-label="Attachment options"
                        title="Attachment options"
                        style={style}
                    >
                        <Paperclip className="h-5 w-5" />
                    </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                    <DropdownMenuItem onClick={handleAttachFileClick}>
                        <Upload className="mr-2 h-4 w-4" />
                        <span>Attach file</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={async () => {
                        // Show loading indicator (optional)
                        try {
                            const clipboardImageRequest = createApiRequest('/api/clipboardImage', 'POST');
                            const data = await clipboardImageRequest({});
                            if (data.success && data.attachment) {
                                // Convert base64 to ArrayBuffer for content
                                const arrBuf = base64ToArrayBuffer(data.attachment.content);
                                const file = new File([
                                    arrBuf
                                ], data.attachment.name || 'clipboard-image.png', {
                                    type: data.attachment.type || 'image/png',
                                    lastModified: data.attachment.lastModified || Date.now()
                                });
                                // Pass as array for addAttachments
                                onFilesSelected([file]);
                            } else {
                                alert(data.error || 'No image found in clipboard.');
                            }
                        } catch (err) {
                            alert('Failed to get image from clipboard.');
                        }
                    }}>
                        <ClipboardPen className="mr-2 h-4 w-4" />
                        <span>Image from Clipboard</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={async () => {
                        // Git Diff option
                        try {
                            // Optionally show loading indicator here
                            const gitDiffRequest = createApiRequest('/api/gitDiff', 'POST');
                            const data = await gitDiffRequest({ data: '{}' });
                            if (data.success && data.attachment) {
                                // Convert base64 to ArrayBuffer for content
                                const arrBuf = base64ToArrayBuffer(data.attachment.content);
                                const file = new File([
                                    arrBuf
                                ], data.attachment.name || 'git-diff.txt', {
                                    type: data.attachment.type || 'text/plain',
                                    lastModified: data.attachment.lastModified || Date.now()
                                });
                                // Pass as array for addAttachments
                                onFilesSelected([file]);
                            } else {
                                alert(data.error || 'Failed to get git diff.');
                            }
                        } catch (err) {
                            alert('Failed to get git diff.');
                        }
                    }}>
                        <Paperclip className="mr-2 h-4 w-4" />
                        <span>Git Diff</span>
                    </DropdownMenuItem>
                </DropdownMenuContent>
            </DropdownMenu>
        </div>
    );
};