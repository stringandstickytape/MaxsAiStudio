// AiStudioClient/src/stores/useAttachmentStore.ts
import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Attachment } from '@/types/attachment';
import {
  validateAttachment,
  createAttachmentPreviewUrl,
  extractTextFromFile,
  isTextFile,
  cleanupAttachmentUrls,
  DEFAULT_ATTACHMENT_OPTIONS
} from '@/utils/attachmentUtils';

interface AttachmentState {
  // All attachments currently staged/managed, keyed by a unique ID (e.g., message ID or a temporary "staging" ID)
  attachmentsById: Record<string, Attachment[]>;
  // Tracks attachments specifically being prepared in the input bar before sending
  stagedAttachments: Attachment[];
  // Tracks validation errors related to attachments
  attachmentErrors: Record<string, string | null>; // Keyed by attachment ID or a general key
  // Loading state for async operations like validation or processing
  isLoading: boolean;
  // Maximum allowed attachments and size (could be fetched from config)
  maxCount: number;
  maxSize: number;
}

interface AttachmentActions {
  // --- Staging Area Actions (Input Bar) ---
  addStagedAttachment: (file: File) => Promise<boolean>;
  addStagedAttachments: (files: FileList | File[]) => Promise<boolean>;
  removeStagedAttachment: (id: string) => void;
  clearStagedAttachments: () => void;
  getStagedAttachments: () => Attachment[]; // Action to get current staged attachments before sending

  // --- General Attachment Management ---
  // Adds attachments associated with a specific ID (e.g., a sent message ID)
  addAttachmentsForId: (id: string, attachments: Attachment[]) => void;
  // Removes all attachments associated with a specific ID
  removeAttachmentsForId: (id: string) => void;

  // --- Utility Actions ---
  setLoading: (loading: boolean) => void;
  setError: (attachmentId: string, error: string | null) => void;
  clearErrors: () => void;
}

type AttachmentStore = AttachmentState & AttachmentActions;

// Initial state
const initialState: AttachmentState = {
  attachmentsById: {},
  stagedAttachments: [],
  attachmentErrors: {},
  isLoading: false,
  maxCount: DEFAULT_ATTACHMENT_OPTIONS.maxCount,
  maxSize: DEFAULT_ATTACHMENT_OPTIONS.maxSize,
};

export const useAttachmentStore = create<AttachmentStore>((set, get) => ({
  ...initialState,

  // --- Staging Area Actions ---
  addStagedAttachment: async (file: File) => {
    set({ isLoading: true });
    try {
      const { stagedAttachments, maxCount, maxSize } = get();
      
      // Validate the attachment
      const error = validateAttachment(file, { maxCount, maxSize }, stagedAttachments.length);
      if (error) {
        set(state => ({
          attachmentErrors: { ...state.attachmentErrors, general: error },
          isLoading: false
        }));
        return false;
      }

      // Process the file
      const buffer = await file.arrayBuffer();
      
      // Extract text content if it's a text file
      let textContent: string | null = null;
      if (isTextFile(file.type)) {
        textContent = await extractTextFromFile(file);
      }

      // Create the attachment object
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

      // Get image dimensions if it's an image
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

      // Update state with the new attachment
      set(state => ({
        stagedAttachments: [...state.stagedAttachments, attachment],
        attachmentErrors: { ...state.attachmentErrors, general: null },
        isLoading: false
      }));
      
      return true;
    } catch (err) {
      console.error('Error creating attachment:', err);
      set(state => ({
        attachmentErrors: { 
          ...state.attachmentErrors, 
          general: err instanceof Error ? err.message : 'Failed to process attachment' 
        },
        isLoading: false
      }));
      return false;
    }
  },

  addStagedAttachments: async (files: FileList | File[]) => {
    set({ isLoading: true });
    try {
      const { stagedAttachments, maxCount } = get();
      const fileArray = Array.from(files);
      const availableSlots = maxCount - stagedAttachments.length;

      if (availableSlots <= 0) {
        set(state => ({
          attachmentErrors: { ...state.attachmentErrors, general: `Maximum of ${maxCount} attachments allowed` },
          isLoading: false
        }));
        return false;
      }

      const filesToProcess = fileArray.slice(0, availableSlots);
      let success = false;

      for (const file of filesToProcess) {
        const result = await get().addStagedAttachment(file);
        if (result) success = true;
      }

      set({ isLoading: false });
      return success;
    } catch (err) {
      console.error('Error adding attachments:', err);
      set(state => ({
        attachmentErrors: { 
          ...state.attachmentErrors, 
          general: err instanceof Error ? err.message : 'Failed to process attachments' 
        },
        isLoading: false
      }));
      return false;
    }
  },

  removeStagedAttachment: (id: string) => {
    const { stagedAttachments } = get();
    const attachmentToRemove = stagedAttachments.find(a => a.id === id);
    
    if (attachmentToRemove && attachmentToRemove.previewUrl) {
      URL.revokeObjectURL(attachmentToRemove.previewUrl);
    }
    
    set(state => ({
      stagedAttachments: state.stagedAttachments.filter(a => a.id !== id),
      attachmentErrors: { ...state.attachmentErrors, [id]: null }
    }));
  },

  clearStagedAttachments: () => {
    const { stagedAttachments } = get();
    cleanupAttachmentUrls(stagedAttachments);
    
    set(state => ({
      stagedAttachments: [],
      attachmentErrors: {}
    }));
  },

  getStagedAttachments: () => {
    const { stagedAttachments } = get();
    const attachments = [...stagedAttachments];
    // Clear staged attachments after retrieving them
    get().clearStagedAttachments();
    return attachments;
  },

  // --- General Attachment Management ---
    addAttachmentsForId: (id: string, attachments: Attachment[]) => {
        console.log("add attachment...");
    set(state => ({
      attachmentsById: {
        ...state.attachmentsById,
        [id]: attachments
      }
    }));
  },

  removeAttachmentsForId: (id: string) => {
    const { attachmentsById } = get();
    const attachmentsToRemove = attachmentsById[id];
    
    if (attachmentsToRemove) {
      cleanupAttachmentUrls(attachmentsToRemove);
    }
    
    set(state => {
      const newAttachmentsById = { ...state.attachmentsById };
      delete newAttachmentsById[id];
      
      return { attachmentsById: newAttachmentsById };
    });
  },

  // --- Utility Actions ---
  setLoading: (loading: boolean) => set({ isLoading: loading }),
  
  setError: (attachmentId: string, error: string | null) => {
    set(state => ({
      attachmentErrors: { ...state.attachmentErrors, [attachmentId]: error }
    }));
  },
  
  clearErrors: () => set({ attachmentErrors: {} })
}));