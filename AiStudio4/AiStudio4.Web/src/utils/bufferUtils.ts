/**
 * Utility functions for working with ArrayBuffers, Base64 strings, and binary data
 */
import { Attachment } from '@/types/attachment';

/**
 * Converts an ArrayBuffer to a Base64 string
 * @param buffer The ArrayBuffer to convert
 * @returns A Base64 encoded string
 */
export function arrayBufferToBase64(buffer: ArrayBuffer): string {
  let binary = '';
  const bytes = new Uint8Array(buffer);
  const len = bytes.byteLength;
  for (let i = 0; i < len; i++) {
    binary += String.fromCharCode(bytes[i]);
  }
  return window.btoa(binary);
}

/**
 * Converts a Base64 string back to an ArrayBuffer
 * @param base64 The Base64 string to convert
 * @returns An ArrayBuffer containing the decoded data
 */
export function base64ToArrayBuffer(base64: string): ArrayBuffer {
  const binaryString = window.atob(base64);
  const len = binaryString.length;
  const bytes = new Uint8Array(len);
  for (let i = 0; i < len; i++) {
    bytes[i] = binaryString.charCodeAt(i);
  }
  return bytes.buffer;
}

/**
 * Creates a preview URL for attachments (primarily for images)
 * @param attachment The attachment object with type and content
 * @returns A URL that can be used for previewing the attachment, or undefined if not applicable
 */
export function createAttachmentPreviewUrl(attachment: { type: string, content: ArrayBuffer }): string | undefined {
  if (!attachment.type.startsWith('image/')) return undefined;

  try {
    const blob = new Blob([attachment.content], { type: attachment.type });
    return URL.createObjectURL(blob);
  } catch (error) {
    console.error('Failed to create preview URL:', error);
    return undefined;
  }
}

/**
 * Processes attachments by converting Base64 content to ArrayBuffer and creating preview URLs
 * This is the centralized attachment processing function to be used across the application
 * @param attachments Array of attachments to process
 * @returns Processed attachments with consistent format
 */
export function processAttachments(attachments: any[]): any[] {
  if (!attachments || !Array.isArray(attachments)) return [];
  
  return attachments.map(att => {
    // If content is a Base64 string, convert to ArrayBuffer
    if (typeof att.content === 'string') {
      const buffer = base64ToArrayBuffer(att.content);
      
      // Create preview URL for images - only if one doesn't already exist
      const previewUrl = !att.previewUrl && att.type.startsWith('image/') 
        ? createAttachmentPreviewUrl({ type: att.type, content: buffer })
        : att.previewUrl;
      
      return {
        ...att,
        content: buffer,
        previewUrl
      };
    }
    return att;
  });
}

/**
 * Prepares attachments for API transmission
 * Converts ArrayBuffer content to Base64 strings for JSON serialization
 * @param attachments Array of attachments to prepare
 * @returns Attachments ready for API transmission with Base64 encoded content
 */
export function prepareAttachmentsForTransmission(attachments: Attachment[]): any[] {
  if (!attachments || !Array.isArray(attachments) || attachments.length === 0) return [];
  
  return attachments.map(attachment => {
    // If attachment has ArrayBuffer content, convert to base64 for JSON transmission
    if (attachment.content instanceof ArrayBuffer) {
      return {
        ...attachment,
        content: arrayBufferToBase64(attachment.content)
      };
    }
    return attachment;
  });
}

/**
 * Extracts text from text file attachments and formats it for inclusion in messages
 * @param textAttachments Array of text file attachments
 * @returns Formatted string with text content
 */
export function formatTextAttachments(textAttachments: any[]): string {
  if (!textAttachments || textAttachments.length === 0) return '';

  let formattedContent = '';

  textAttachments.forEach(attachment => {
    if (attachment.textContent) {
      // Determine language based on file extension
      const fileExt = attachment.name.split('.').pop()?.toLowerCase() || '';
      let language = '';

      // Map common extensions to languages
      switch (fileExt) {
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

      formattedContent += `\n\n**File: ${attachment.name}**\n\`\`\`${language}\n${attachment.textContent}\n\`\`\`\n`;
    }
  });

  return formattedContent;
}

/**
 * Cleans up attachment preview URLs to prevent memory leaks
 * @param attachments Array of attachments to clean up
 */
export function cleanupAttachmentUrls(attachments: Attachment[]): void {
  if (!attachments || !Array.isArray(attachments)) return;
  
  attachments.forEach(attachment => {
    if (attachment.previewUrl) {
      URL.revokeObjectURL(attachment.previewUrl);
    }
    if (attachment.thumbnailUrl) {
      URL.revokeObjectURL(attachment.thumbnailUrl);
    }
  });
}

/**
 * Checks if a file is a text file based on its MIME type
 * @param mimeType MIME type of the file
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