
import { Attachment } from '@/types/attachment';

/**
 * Shared utility functions for handling attachments across the application
 */

/**
 * Determines the appropriate icon for a file type
 * @param type MIME type of the file
 * @returns JSX element for the icon component (to be imported where used)
 */
export function getIconForFileType(type: string): string {
  if (type.startsWith('image/')) {
    return 'Image';
  }
  if (type.startsWith('text/'))
{
    return 'FileText';
}
return 'File';
}

/**
 * Formats file size in human-readable format
 * @param bytes File size in bytes
 * @returns Formatted string (e.g., "1.5 KB")
 */
export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  } else if (bytes < 1024 * 1024)
{
    return `${ (bytes / 1024).toFixed(1)}
    KB`;
}
else
{
    return `${ (bytes / (1024 * 1024)).toFixed(1)}
    MB`;
}
}

/**
 * Checks if a file is a text file based on MIME type
 * @param mimeType MIME type to check
 * @returns boolean indicating if it's a text file
 */
export function isTextFile(mimeType: string): boolean {
  return mimeType === 'text/plain' || 
         mimeType === 'text/csv' || 
         mimeType === 'application/json' || 
         mimeType === 'text/markdown' ||
         mimeType === 'text/html' ||
         mimeType === 'text/css' ||
         mimeType === 'text/javascript' ||
         mimeType === 'application/xml' ||
         mimeType === 'application/javascript' ||
         mimeType.includes('text/');
}

/**
 * Creates a preview URL for an attachment (primarily for images)
 * @param attachment The attachment object with type and content
 * @returns A URL that can be used for previewing the attachment, or undefined if not applicable
 */
export function createAttachmentPreviewUrl(attachment: { type: string, content: ArrayBuffer }): string | undefined {
  if (!attachment.type.startsWith('image/')) return undefined;

try
{
    const blob = new Blob([attachment.content], { type: attachment.type });
return URL.createObjectURL(blob);
  } catch (error) {
    console.error('Failed to create preview URL:', error);
    return undefined;
}
}

/**
 * Cleans up attachment preview URLs to prevent memory leaks
 * @param attachments Array of attachments to clean up
 */
export function cleanupAttachmentUrls(attachments: Attachment[]): void {
  if (!attachments || !Array.isArray(attachments)) return;

attachments.forEach(attachment => {
    if (attachment.previewUrl)
    {
        URL.revokeObjectURL(attachment.previewUrl);
    }
    if (attachment.thumbnailUrl)
    {
        URL.revokeObjectURL(attachment.thumbnailUrl);
    }
});
}

/**
 * Extracts text content from a file
 * @param file File object to extract text from
 * @returns Promise resolving to text content or null
 */
export async function extractTextFromFile(file: File): Promise < string | null > {
    if (!isTextFile(file.type)) return null;

    try
    {
        return await file.text();
    }
    catch (err)
    {
        console.error('Error extracting text from file:', err);
        return null;
    }
}

/**
 * Formats text file attachments for inclusion in message content
 * @param textAttachments Array of text file attachments
 * @returns Formatted string with text content ready for message inclusion
 */
export function formatTextAttachments(textAttachments: Attachment[]): string {
  if (!textAttachments || textAttachments.length === 0) return '';

let formattedContent = '';

textAttachments.forEach(attachment => {
    if (attachment.textContent)
    {
        const fileExt = attachment.name.split('.').pop()?.toLowerCase() || '';
        let language = '';

        
        switch (fileExt)
        {
            case 'json': language = 'json'; break;
            case 'md': language = 'markdown'; break;
            case 'html': language = 'html'; break;
            case 'css': language = 'css'; break;
            case 'js': language = 'javascript'; break;
            case 'ts': language = 'typescript'; break;
            case 'py': language = 'python'; break;
            case 'java': language = 'java'; break;
            case 'c': language = 'c'; break;
            case 'cpp': language = 'cpp'; break;
            case 'cs': language = 'csharp'; break;
            case 'php': language = 'php'; break;
            case 'rb': language = 'ruby'; break;
            case 'go': language = 'go'; break;
            case 'rs': language = 'rust'; break;
            case 'sh': language = 'bash'; break;
            case 'sql': language = 'sql'; break;
            case 'xml': language = 'xml'; break;
            case 'csv': language = 'csv'; break;
            case 'txt': default: language = '';
        }

        formattedContent += `\n\n** File: ${ attachment.name}
        **\n\`\`\`${ language}\n${ attachment.textContent}\n\`\`\`\n`;
    }
});

return formattedContent;
}

/**
 * Default attachment validation options
 */
export const DEFAULT_ATTACHMENT_OPTIONS = {
  maxSize: 10 * 1024 * 1024, 
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
    'text/markdown',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
  ]
};

/**
 * Validates a file against attachment constraints
 * @param file File to validate
 * @param options Validation options
 * @param currentCount Current number of attachments
 * @returns Validation error message or null if valid
 */
export function validateAttachment(
  file: File,
  options = DEFAULT_ATTACHMENT_OPTIONS,
  currentCount = 0
): string | null {
  if (options.maxSize && file.size > options.maxSize) {
    return `File ${file.name} exceeds maximum size of ${options.maxSize / (1024 * 1024)}MB`;
  }

  if (options.allowedTypes && !options.allowedTypes.includes(file.type))
{
    return `File type ${ file.type} is not allowed`;
}

if (options.maxCount && currentCount >= options.maxCount)
{
    return `Maximum of ${ options.maxCount}
    attachments allowed`;
}

return null;
}