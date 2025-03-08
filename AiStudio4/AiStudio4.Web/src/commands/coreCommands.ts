// src/commands/coreCommands.ts
import { v4 as uuidv4 } from 'uuid';
import { useCommandStore } from '@/stores/useCommandStore';
import { Plus, RefreshCw, Settings, GitBranch, ExternalLink } from 'lucide-react';
import React from 'react';
import { useConversationStore } from '@/stores/useConversationStore';

export function initializeCoreCommands(
    handlers: {
        toggleSidebar: () => void,
        toggleConversationTree: () => void,
        toggleSettings: () => void,
        openNewWindow: () => void,
    }
) {
    // Get Zustand store actions
    const { createConversation } = useConversationStore.getState();

    const mac = navigator.platform.indexOf('Mac') !== -1;
    const shortcut = (key: string) => mac ? `⌘+${key}` : `Ctrl+${key}`;

    const { registerGroup } = useCommandStore.getState();
    
    registerGroup({
        id: 'conversation',
        name: 'Conversation',
        priority: 100,
        commands: [
            {
                id: 'new-conversation',
                name: 'New Conversation',
                description: 'Start a new chat conversation',
                shortcut: shortcut('N'),
                keywords: ['new', 'chat', 'conversation', 'start', 'create', 'fresh', 'initiate', 'discuss', 'message'],
                section: 'conversation',
                icon: React.createElement(Plus, { size: 16 }),
                execute: () => {
                    const conversationId = `conv_${uuidv4()}`;
                    const messageId = `msg_${Date.now()}`;
                    createConversation({
                        id: conversationId,
                        rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() }
                    });
                }
            },
            {
                id: 'clear-conversation',
                name: 'Clear Current Conversation',
                description: 'Clear all messages in the current conversation',
                keywords: ['clear', 'reset', 'empty', 'delete', 'clean', 'refresh', 'restart', 'discard', 'renew'],
                section: 'conversation',
                icon: React.createElement(RefreshCw, { size: 16 }),
                execute: () => {
                    // Use the current store's state to get active conversation ID
                    const { activeConversationId, createConversation, conversations } = useConversationStore.getState();
                    
                    if (activeConversationId) {
                        const conversationId = `conv_${uuidv4()}`;
                        const messageId = `msg_${Date.now()}`;

                        createConversation({
                            id: conversationId,
                            rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() }
                        });
                    }
                }
            }
        ]
    });

    registerGroup({
        id: 'view',
        name: 'View',
        priority: 90,
        commands: [
            {
                id: 'toggle-sidebar',
                name: 'Conversation History Toggle',
                shortcut: shortcut('B'),
                keywords: ['sidebar', 'menu', 'conversations', 'history', 'panel', 'navigation', 'toggle', 'hide', 'show', 'toggle'],
                section: 'view',
                icon: React.createElement(Plus, { size: 16 }),
                execute: handlers.toggleSidebar
            },
            {
                id: 'toggle-conversation-tree',
                name: 'Conversation Tree Toggle',
                shortcut: shortcut('T'),
                keywords: ['tree', 'structure', 'map', 'messages', 'branch', 'hierarchy', 'conversation', 'flow', 'thread', 'graph', 'toggle'],
                section: 'view',
                icon: React.createElement(GitBranch, { size: 16 }),
                execute: handlers.toggleConversationTree
            },
            {
                id: 'toggle-settings',
                name: 'Settings Panel Toggle',
                shortcut: shortcut(','),
                keywords: ['settings', 'options', 'preferences', 'configure', 'setup', 'customize', 'adjust', 'toggle'],
                section: 'view',
                icon: React.createElement(Settings, { size: 16 }),
                execute: handlers.toggleSettings
            },
            {
                id: 'open-new-window',
                name: 'Open New Window',
                keywords: ['window', 'open', 'new', 'external', 'launch', 'create', 'instance'],
                section: 'view',
                icon: React.createElement(ExternalLink, { size: 16 }),
                execute: handlers.openNewWindow
            }
        ]
    });
}