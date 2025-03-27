import { create } from 'zustand';

interface AutoReplyStore {
  enabled: boolean;
  pendingReply: { convId: string; messageId: string } | null;
  setEnabled: (enabled: boolean) => void;
  setPendingReply: (reply: { convId: string; messageId: string } | null) => void;
}

export const useAutoReplyStore = create<AutoReplyStore>((set) => ({
  enabled: false,
  pendingReply: null,
  setEnabled: (enabled) => set({ enabled }),
  setPendingReply: (reply) => set({ pendingReply: reply }),
}));