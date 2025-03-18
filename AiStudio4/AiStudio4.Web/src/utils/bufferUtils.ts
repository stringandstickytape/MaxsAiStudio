/**
 * Utility functions for working with ArrayBuffers, Base64 strings, and binary data
 */
import { Attachment } from '@/types/attachment';
import {
    isTextFile,
    formatTextAttachments as formatTextAttachmentsUtil,
    cleanupAttachmentUrls as cleanupAttachmentUrlsUtil
} from './attachmentUtils';

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
 * Processes attachments by converting Base64 content to ArrayBuffer and creating preview URLs
 * This is the centralized attachment processing function to be used across the application
 * @param attachments Array of attachments to process
 * @returns Processed attachments with consistent format
 */
export function processAttachments(attachments: any[]): any[] {
    if (!attachments || !Array.isArray(attachments)) return [];

    return attachments.map(att => {
        
        if (typeof att.content === 'string') {
            const buffer = base64ToArrayBuffer(att.content);

            
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
 * Prepares attachments for API transmission
 * Converts ArrayBuffer content to Base64 strings for JSON serialization
 * @param attachments Array of attachments to prepare
 * @returns Attachments ready for API transmission with Base64 encoded content
 */
export function prepareAttachmentsForTransmission(attachments: Attachment[]): any[] {
    if (!attachments || !Array.isArray(attachments) || attachments.length === 0) return [];

    return attachments.map(attachment => {
        
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
    return formatTextAttachmentsUtil(textAttachments);
}

/**
 * Cleans up attachment preview URLs to prevent memory leaks
 * @param attachments Array of attachments to clean up
 */
export function cleanupAttachmentUrls(attachments: Attachment[]): void {
    cleanupAttachmentUrlsUtil(attachments);
}

/**
 * Checks if a file is a text file based on its MIME type
 * @param mimeType MIME type of the file
 * @returns boolean indicating if it's a text file
 */
export { isTextFile } from './attachmentUtils';