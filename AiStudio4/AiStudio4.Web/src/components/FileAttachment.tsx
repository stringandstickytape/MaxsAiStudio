import React, { useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Paperclip, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { DEFAULT_ATTACHMENT_OPTIONS } from '@/utils/attachmentUtils';

interface FileAttachmentProps {
    onFilesSelected: (files: FileList | File[]) => void;
    disabled?: boolean;
    className?: string;
    maxFiles?: number;
    acceptedTypes?: string;
}

export const FileAttachment: React.FC<FileAttachmentProps> = ({
    onFilesSelected,
    disabled = false,
    className,
    maxFiles = DEFAULT_ATTACHMENT_OPTIONS.maxCount,
    acceptedTypes = ".jpg,.jpeg,.png,.gif,.pdf,.txt,.md,.js,.jsx,.ts,.tsx,.py,.html,.css,.json,.csv"
}) => {
    const inputRef = useRef<HTMLInputElement>(null);
    const [isDragging, setIsDragging] = useState(false);

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

        onFilesSelected(files);

        // Reset the input so the same file can be selected again
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

            {/* Attachment button */}
            <Button
                variant="outline"
                size="icon"
                type="button"
                onClick={handleButtonClick}
                disabled={disabled}
                className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
                aria-label="Attach file"
                title="Attach file"
            >
                <Paperclip className="h-5 w-5" />
            </Button>
        </div>
    );
};