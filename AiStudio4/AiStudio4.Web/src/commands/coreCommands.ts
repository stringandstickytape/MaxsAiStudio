
import { v4 as uuidv4 } from 'uuid';
import { useCommandStore } from '@/stores/useCommandStore';
import { Plus, RefreshCw, Settings, GitBranch, ExternalLink, Terminal } from 'lucide-react';
import React from 'react';
import { useConvStore } from '@/stores/useConvStore';

export function initializeCoreCommands(handlers: {
  toggleSidebar: () => void;
  toggleConvTree: () => void;
  toggleSettings: () => void;
  openNewWindow: () => void;
}) {
  
  const { createConv } = useConvStore.getState();

  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  const { registerGroup } = useCommandStore.getState();

  registerGroup({
    id: 'conv',
    name: 'Conv',
    priority: 100,
    commands: [
      [
        'new-conv',
        'New Conv',
        'Start a new chat conv',
        shortcut('N'),
        ['new', 'chat', 'conv', 'start', 'create', 'fresh', 'initiate', 'discuss', 'message'],
        React.createElement(Plus, { size: 16 }),
        () => {
          const convId = `conv_${uuidv4()}`;
          const messageId = `msg_${Date.now()}`;
          createConv({
            id: convId,
            rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() },
          });
        },
      ],
      [
        'clear-conv',
        'Clear Current Conv',
        'Clear all messages in the current conv',
        '',
        ['clear', 'reset', 'empty', 'delete', 'clean', 'refresh', 'restart', 'discard', 'renew'],
        React.createElement(RefreshCw, { size: 16 }),
        () => {
          const { activeConvId, createConv } = useConvStore.getState();
          if (activeConvId) {
            const convId = `conv_${uuidv4()}`;
            const messageId = `msg_${Date.now()}`;
            createConv({
              id: convId,
              rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() },
            });
          }
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
            const response = await fetch('/api/exitApplication', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json'
              },
              body: JSON.stringify({ clientId: window.localStorage.getItem('clientId') })
            });
            const data = await response.json();
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

  registerGroup({
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
      [
        'toggle-settings',
        'Settings Panel Toggle',
        shortcut(','),
        ['settings', 'options', 'preferences', 'configure', 'setup', 'customize', 'adjust', 'toggle'],
        React.createElement(Settings, { size: 16 }),
        handlers.toggleSettings,
      ],
      [
        'open-new-window',
        'Open New Window',
        '',
        ['window', 'open', 'new', 'external', 'launch', 'create', 'instance'],
        React.createElement(ExternalLink, { size: 16 }),
        handlers.openNewWindow,
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

