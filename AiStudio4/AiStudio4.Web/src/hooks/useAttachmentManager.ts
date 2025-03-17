import { useState, useCallback, useEffect } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { Attachment } from '@/types/attachment';

interface UseAttachmentManagerOptions {
  maxSize?: number; // Maximum file size in bytes
  maxCount?: number; // Maximum number of attachments
  allowedTypes?: string[]; // Array of allowed MIME types
}

const DEFAULT_OPTIONS: UseAttachmentManagerOptions = {
  maxSize: 10 * 1024 * 1024, // 10MB
  maxCount: 5,
  allowedTypes: [
    'image/jpeg',
    'image/png',
    'image/gif',
    'image/webp',
    'application/pdf',
    'text/plain',
    'text/csv',
    'application/json',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
  ]
};

export function useAttachmentManager(options: UseAttachmentManagerOptions = {}) {
  const config = { ...DEFAULT_OPTIONS, ...options };
  const [attachments, setAttachments] = useState<Attachment[]>([]);
  const [error, setError] = useState<string | null>(null);

  // Clean up object URLs when component unmounts
  useEffect(() => {
    return () => {
      attachments.forEach(attachment => {
        if (attachment.previewUrl) {
          URL.revokeObjectURL(attachment.previewUrl);
        }
        if (attachment.thumbnailUrl) {
          URL.revokeObjectURL(attachment.thumbnailUrl);
        }
      });
    };
  }, [attachments]);

  const validateFile = useCallback((file: File): string | null => {
    if (config.maxSize && file.size > config.maxSize) {
      return `File ${file.name} exceeds maximum size of ${config.maxSize / (1024 * 1024)}MB`;
    }

    if (config.allowedTypes && !config.allowedTypes.includes(file.type)) {
      return `File type ${file.type} is not allowed`;
    }

    if (config.maxCount && attachments.length >= config.maxCount) {
      return `Maximum of ${config.maxCount} attachments allowed`;
    }

    return null;
  }, [attachments.length, config.allowedTypes, config.maxCount, config.maxSize]);

  const createAttachment = useCallback(async (file: File): Promise<Attachment | null> => {
    try {
      const error = validateFile(file);
      if (error) {
        setError(error);
        return null;
      }

      const buffer = await file.arrayBuffer();
      const attachment: Attachment = {
        id: uuidv4(),
        type: file.type,
        name: file.name,
        size: file.size,
        content: buffer,
        previewUrl: URL.createObjectURL(file),
        metadata: {
          lastModified: file.lastModified
        }
      };

      // Generate additional metadata for images
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
  }, [validateFile]);

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
      if (attachmentToRemove) {
        if (attachmentToRemove.previewUrl) {
          URL.revokeObjectURL(attachmentToRemove.previewUrl);
        }
        if (attachmentToRemove.thumbnailUrl) {
          URL.revokeObjectURL(attachmentToRemove.thumbnailUrl);
        }
      }
      return prev.filter(a => a.id !== id);
    });
    setError(null);
  }, []);

  const clearAttachments = useCallback(() => {
    attachments.forEach(attachment => {
      if (attachment.previewUrl) {
        URL.revokeObjectURL(attachment.previewUrl);
      }
      if (attachment.thumbnailUrl) {
        URL.revokeObjectURL(attachment.thumbnailUrl);
      }
    });
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
