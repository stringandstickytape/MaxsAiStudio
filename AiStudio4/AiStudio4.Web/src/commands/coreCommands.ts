// src/commands/coreCommands.ts
import { v4 as uuidv4 } from 'uuid';
import { useCommandStore } from '@/stores/useCommandStore';
import { Plus, RefreshCw, Settings, GitBranch, ExternalLink } from 'lucide-react';
import React from 'react';
import { useConvStore } from '@/stores/useConvStore';

export function initializeCoreCommands(
    handlers: {
        toggleSidebar: () => void,
        toggleConvTree: () => void,
        toggleSettings: () => void,
        openNewWindow: () => void,
    }
) {
    // Get Zustand store actions
    const { createConv } = useConvStore.getState();

    const mac = navigator.platform.indexOf('Mac') !== -1;
    const shortcut = (key: string) => mac ? `⌘+${key}` : `Ctrl+${key}`;

    const { registerGroup } = useCommandStore.getState();
    
    registerGroup({
        id: 'conv',
        name: 'Conv',
        priority: 100,
        commands: [
            {
                id: 'new-conv',
                name: 'New Conv',
                description: 'Start a new chat conv',
                shortcut: shortcut('N'),
                keywords: ['new', 'chat', 'conv', 'start', 'create', 'fresh', 'initiate', 'discuss', 'message'],
                section: 'conv',
                icon: React.createElement(Plus, { size: 16 }),
                execute: () => {
                    const convId = `conv_${uuidv4()}`;
                    const messageId = `msg_${Date.now()}`;
                    createConv({
                        id: convId,
                        rootMessage: { id: messageId, content: '', source: 'system', timestamp: Date.now() }
                    });
                }
            },
            {
                id: 'clear-conv',
                name: 'Clear Current Conv',
                description: 'Clear all messages in the current conv',
                keywords: ['clear', 'reset', 'empty', 'delete', 'clean', 'refresh', 'restart', 'discard', 'renew'],
                section: 'conv',
                icon: React.createElement(RefreshCw, { size: 16 }),
                execute: () => {
                    // Use the current store's state to get active conv ID
                    const { activeConvId, createConv, convs } = useConvStore.getState();
                    
                    if (activeConvId) {
                        const convId = `conv_${uuidv4()}`;
                        const messageId = `msg_${Date.now()}`;

                        createConv({
                            id: convId,
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
                name: 'Conv History Toggle',
                shortcut: shortcut('B'),
                keywords: ['sidebar', 'menu', 'convs', 'history', 'panel', 'navigation', 'toggle', 'hide', 'show', 'toggle'],
                section: 'view',
                icon: React.createElement(Plus, { size: 16 }),
                execute: handlers.toggleSidebar
            },
            {
                id: 'toggle-conv-tree',
                name: 'Conv Tree Toggle',
                shortcut: shortcut('T'),
                keywords: ['tree', 'structure', 'map', 'messages', 'branch', 'hierarchy', 'conv', 'flow', 'thread', 'graph', 'toggle'],
                section: 'view',
                icon: React.createElement(GitBranch, { size: 16 }),
                execute: handlers.toggleConvTree
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