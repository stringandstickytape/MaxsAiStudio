// AiStudioClient/src/commands/coreCommands.ts
import { v4 as uuidv4 } from 'uuid';
import { Plus, RefreshCw, Settings, GitBranch, ExternalLink, Terminal, Bug } from 'lucide-react';
import React from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { useDebugStore } from '@/stores/useDebugStore';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { createApiRequest } from '@/utils/apiUtils';
import { commandRegistry } from '@/services/commandRegistry';

export function initializeCoreCommands(handlers: {
  toggleSidebar: () => void;
  toggleSidebarCollapse: () => void;
  toggleConvTree: () => void;
  toggleSettings: () => void;
  openNewWindow: () => void;
}) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  commandRegistry.registerGroup({
    id: 'conv',
    name: 'Conv',
    priority: 100,
    commands: [
      [
        'new-conv',
        'New Conversation',
        'Start a new chat conversation',
        shortcut('N'),
            ['new', 'chat', 'conversation', 'start', 'create', 'fresh', 'initiate', 'discuss', 'message', 'clear', 'reset', 'empty', 'delete', 'clean', 'refresh', 'restart', 'discard', 'renew'],
        React.createElement(Plus, { size: 16 }),
        () => {
          const convId = `conv_${uuidv4()}`;
          const messageId = `msg_${Date.now()}`;
          useConvStore.getState().createConv({
            id: convId,
            rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() },
          });
        },
      ],
      [
        'exit-app',
        'Exit the application',
        'Closes the application on the Windows desktop',
        '',
        ['close', 'quit', 'exit', 'end'],
        React.createElement(Terminal, { size: 16 }),
        async () => {
          try {
            const exitApplication = createApiRequest('/api/exitApplication', 'POST');
            const data = await exitApplication({ clientId: window.localStorage.getItem('clientId') });
            if (!data.success) {
              console.error('Failed to exit application:', data.error);
            }
          } catch (error) {
            console.error('Error sending exit application request:', error);
          }
        },
      ],
    ].map(([id, name, description, shortcut, keywords, icon, fn]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'conv',
      icon,
      execute: fn,
    })),
  });

  const createCommand = (id: string, name: string, keywords: string[], fn: () => void) => {
    return [
      id,
      name,
      '',
      keywords,
      React.createElement(Plus, { size: 16 }),
      fn,
    ];
  };

  commandRegistry.registerGroup({
    id: 'view',
    name: 'View',
    priority: 90,
    commands: [
      [
        'toggle-sidebar',
        'Conversation History Toggle',
        shortcut('B'),
        ['sidebar', 'menu', 'conversations','chats', 'history', 'panel', 'navigation', 'toggle', 'hide', 'show', 'toggle'],
        React.createElement(Plus, { size: 16 }),
        handlers.toggleSidebar,
      ],
      // We're using a single command for sidebar toggling now
      // createCommand(
      //   'toggle-sidebar-collapse',
      //   'Collapse or expand the sidebar',
      //   ['sidebar', 'collapse', 'expand', 'toggle', 'narrow', 'wide'],
      //   handlers.toggleSidebarCollapse
      // ),
      [
        'open-new-window',
        'Open New Window',
        '',
        ['window', 'open', 'new', 'external', 'launch', 'create', 'instance'],
        React.createElement(ExternalLink, { size: 16 }),
        handlers.openNewWindow,
      ],
      [
        'toggle-dev-view',
        'Toggle Developer Content View',
        '',
        ['dev', 'developer', 'debug', 'content', 'view', 'toggle', 'hidden', 'ai', 'context', 'tool', 'blocks'],
        React.createElement(Bug, { size: 16 }),
        () => {
          useDebugStore.getState().toggleDevContentView();
          const isVisible = useDebugStore.getState().showDevContentView;
          console.log(`Developer Content View: ${isVisible ? 'ON' : 'OFF'}`);
        },
      ],
    ].map(([id, name, shortcut, keywords, icon, fn]) => ({
      id,
      name,
      shortcut,
      keywords,
      section: 'view',
      icon,
      execute: fn,
    })),
  });
}