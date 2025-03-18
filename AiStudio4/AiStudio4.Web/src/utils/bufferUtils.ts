/**
 * Utility functions for working with ArrayBuffers, Base64 strings, and binary data
 */

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
 * @param attachments Array of attachments to process
 * @returns Processed attachments
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
        : undefined;
      
      return {
        ...att,
        content: buffer,
        previewUrl: att.previewUrl || previewUrl
      };
    }
    return att;
  });
}