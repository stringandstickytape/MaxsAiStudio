import { useState, useCallback, useEffect } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { Attachment } from '@/types/attachment';
import {
    cleanupAttachmentUrls,
    createAttachmentPreviewUrl
} from '@/utils/bufferUtils';
import {
    DEFAULT_ATTACHMENT_OPTIONS,
    validateAttachment,
    extractTextFromFile,
    isTextFile
} from '@/utils/attachmentUtils';

interface UseAttachmentManagerOptions {
    maxSize?: number;
    maxCount?: number;
    allowedTypes?: string[];
}

export function useAttachmentManager(options: UseAttachmentManagerOptions = {}) {
    const config = { ...DEFAULT_ATTACHMENT_OPTIONS, ...options };
    const [attachments, setAttachments] = useState<Attachment[]>([]);
    const [error, setError] = useState<string | null>(null);

    // Clean up URLs on unmount to prevent memory leaks
    useEffect(() => {
        return () => {
            cleanupAttachmentUrls(attachments);
        };
    }, [attachments]);

    const createAttachment = useCallback(async (file: File): Promise<Attachment | null> => {
        try {
            const error = validateAttachment(file, config, attachments.length);
            if (error) {
                setError(error);
                return null;
            }

            const buffer = await file.arrayBuffer();

            // Extract text content if it's a text file
            let textContent: string | null = null;
            if (isTextFile(file.type)) {
                textContent = await extractTextFromFile(file);
            }

            const attachment: Attachment = {
                id: uuidv4(),
                type: file.type,
                name: file.name,
                size: file.size,
                content: buffer,
                previewUrl: URL.createObjectURL(file),
                textContent,
                metadata: {
                    lastModified: file.lastModified
                }
            };

            // Add dimensions for images
            if (file.type.startsWith('image/')) {
                await new Promise<void>((resolve) => {
                    const img = new Image();
                    img.onload = () => {
                        if (attachment.metadata) {
                            attachment.metadata.width = img.width;
                            attachment.metadata.height = img.height;
                        }
                        resolve();
                    };
                    img.onerror = () => resolve();
                    img.src = attachment.previewUrl || '';
                });
            }

            return attachment;
        } catch (err) {
            console.error('Error creating attachment:', err);
            setError(err instanceof Error ? err.message : 'Failed to process attachment');
            return null;
        }
    }, [attachments.length, config]);

    const addAttachment = useCallback(async (file: File) => {
        const newAttachment = await createAttachment(file);
        if (newAttachment) {
            setAttachments(prev => [...prev, newAttachment]);
            setError(null);
            return true;
        }
        return false;
    }, [createAttachment]);

    const addAttachments = useCallback(async (files: FileList | File[]) => {
        const fileArray = Array.from(files);
        const availableSlots = config.maxCount ? config.maxCount - attachments.length : fileArray.length;

        if (availableSlots <= 0) {
            setError(`Maximum of ${config.maxCount} attachments allowed`);
            return false;
        }

        const filesToProcess = fileArray.slice(0, availableSlots);
        const results = await Promise.all(filesToProcess.map(createAttachment));
        const validAttachments = results.filter(Boolean) as Attachment[];

        if (validAttachments.length > 0) {
            setAttachments(prev => [...prev, ...validAttachments]);
            if (validAttachments.length < filesToProcess.length) {
                setError(`${validAttachments.length} of ${filesToProcess.length} files were added. Others failed validation.`);
            } else {
                setError(null);
            }
            return true;
        }

        return false;
    }, [attachments.length, config.maxCount, createAttachment]);

    const removeAttachment = useCallback((id: string) => {
        setAttachments(prev => {
            const attachmentToRemove = prev.find(a => a.id === id);
            if (attachmentToRemove && attachmentToRemove.previewUrl) {
                URL.revokeObjectURL(attachmentToRemove.previewUrl);
            }
            return prev.filter(a => a.id !== id);
        });
        setError(null);
    }, []);

    const clearAttachments = useCallback(() => {
        cleanupAttachmentUrls(attachments);
        setAttachments([]);
        setError(null);
    }, [attachments]);

    return {
        attachments,
        error,
        addAttachment,
        addAttachments,
        removeAttachment,
        clearAttachments,
        setError
    };
}