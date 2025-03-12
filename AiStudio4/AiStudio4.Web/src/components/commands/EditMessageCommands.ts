import { CommandSection } from '@/commands/types';
import { commandRegistry } from '@/stores/useCommandStore';
import { useConvStore } from '@/stores/useConvStore';

export function registerEditMessageCommands() {
  commandRegistry.registerGroup({
    id: 'edit-message-commands',
    title: 'Message Editing',
    commands: [
      {
        id: 'edit-raw-message',
        name: 'Edit Raw Message',
        description: 'Edit the raw content of the selected message',
        keywords: ['edit', 'message', 'raw', 'content', 'modify'],
        section: 'conv',
        execute: () => {
          const { slctdMsgId, editMessage } = useConvStore.getState();
          if (slctdMsgId) {
            editMessage(slctdMsgId);
          }
        },
      },
      {
        id: 'cancel-edit-message',
        name: 'Cancel Edit Message',
        description: 'Cancel editing the current message',
        keywords: ['cancel', 'edit', 'message'],
          section: 'conv',
        execute: () => {
          const { cancelEditMessage } = useConvStore.getState();
          cancelEditMessage();
        },
      },
    ],
  });
}
