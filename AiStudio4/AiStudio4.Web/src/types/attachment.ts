export interface Attachment {
  id: string;               // Unique identifier
  type: string;             // MIME type
  name: string;             // Original filename
  size: number;             // File size in bytes
  content: ArrayBuffer;     // Binary content
  textContent?: string;     // Text content for text files
  previewUrl?: string;      // For display (created with URL.createObjectURL)
  thumbnailUrl?: string;    // Optional thumbnail for large images
  metadata?: {
    width?: number;         // For images
    height?: number;        // For images
    lastModified?: number;  // From original file
  };
}
