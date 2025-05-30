﻿// AiStudioClient\src\components\FileAttachment.tsx
import React, { useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Paperclip, X, Upload, ClipboardPen } from 'lucide-react';
import { cn } from '@/lib/utils';
import { DEFAULT_ATTACHMENT_OPTIONS, base64ToArrayBuffer } from '@/utils/attachmentUtils';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { createApiRequest } from '@/utils/apiUtils';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

interface FileAttachmentProps {
    onFilesSelected?: (files: FileList | File[]) => void; // Make optional since we'll use the store directly
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
    const [isLoading, setIsLoading] = useState(false);
    const addStagedAttachments = useAttachmentStore(state => state.addStagedAttachments);

    const handleAttachFileClick = async () => {
        // If in Visual Studio, use the VS-specific approach
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

        // For non-VS environments, make an API call to open a file dialog on the server
        try {
            setIsLoading(true);
            const attachFileRequest = createApiRequest('/api/attachFile', 'POST');
            const data = await attachFileRequest({});
            
            if (data.success && data.attachments && data.attachments.length > 0) {
                // Process each attachment
                const files = data.attachments.map((attachment: any) => {
                    // Convert base64 to ArrayBuffer for content
                    const arrBuf = base64ToArrayBuffer(attachment.content);
                    return new File(
                        [arrBuf],
                        attachment.name,
                        {
                            type: attachment.type || 'application/octet-stream',
                            lastModified: attachment.lastModified || Date.now()
                        }
                    );
                });
                
                // Use the store directly and call the callback if provided
                addStagedAttachments(files);
                if (onFilesSelected) {
                    onFilesSelected(files);
                }
            } else if (data.error) {
                console.error('Error attaching file:', data.error);
            }
        } catch (err) {
            console.error('Failed to attach file:', err);
        } finally {
            setIsLoading(false);
        }
    };

    const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const files = e.target.files;
        if (!files || files.length === 0) return;

        // Use the store directly and call the callback if provided
        addStagedAttachments(files);
        if (onFilesSelected) {
            onFilesSelected(files);
        }

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
        // Use the store directly and call the callback if provided
        addStagedAttachments(e.dataTransfer.files);
        if (onFilesSelected) {
            onFilesSelected(e.dataTransfer.files);
        }
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
            {/* Hidden file input for future use if needed */}
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
                        disabled={disabled || isLoading}
                        className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
                        aria-label="Attachment options"
                        title="Attachment options"
                        style={style}
                    >
                        <Paperclip className={cn("h-5 w-5", isLoading && "animate-pulse")} />
                    </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                    <DropdownMenuItem 
                        onClick={handleAttachFileClick}
                        disabled={isLoading}
                    >
                        <Upload className="mr-2 h-4 w-4" />
                        <span>{isLoading ? "Attaching..." : "Attach file"}</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                        onClick={async () => {
                            // Show loading indicator
                            setIsLoading(true);
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
                                    // Use the store directly and call the callback if provided
                                    addStagedAttachments([file]);
                                    if (onFilesSelected) {
                                        onFilesSelected([file]);
                                    }
                                } else {
                                    alert(data.error || 'No image found in clipboard.');
                                }
                            } catch (err) {
                                alert('Failed to get image from clipboard.');
                            } finally {
                                setIsLoading(false);
                            }
                        }}
                        disabled={isLoading}
                    >
                        <ClipboardPen className="mr-2 h-4 w-4" />
                        <span>Image from Clipboard</span>
                    </DropdownMenuItem>
                    <DropdownMenuItem 
                        onClick={async () => {
                            // Git Diff option
                            setIsLoading(true);
                            try {
                                const { fetchGitDiffAsFile } = await import('@/utils/attachmentUtils');
                                const file = await fetchGitDiffAsFile();
                                
                                if (file) {
                                    // Use the store directly and call the callback if provided
                                    addStagedAttachments([file]);
                                    if (onFilesSelected) {
                                        onFilesSelected([file]);
                                    }
                                } else {
                                    alert('Failed to get git diff.');
                                }
                            } catch (err) {
                                alert('Failed to get git diff.');
                            } finally {
                                setIsLoading(false);
                            }
                        }}
                        disabled={isLoading}
                    >
                        <Paperclip className="mr-2 h-4 w-4" />
                        <span>Git Diff</span>
                    </DropdownMenuItem>
                </DropdownMenuContent>
            </DropdownMenu>
        </div>
    );
};