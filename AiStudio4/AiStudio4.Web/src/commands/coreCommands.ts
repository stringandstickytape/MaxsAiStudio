// src/commands/coreCommands.ts
import { store } from '@/store/store';
import { v4 as uuidv4 } from 'uuid';
import { createConversation } from '@/store/conversationSlice';
import { registerCommandGroup } from './commandRegistry';
import { Plus, RefreshCw, Settings, GitBranch, ExternalLink } from 'lucide-react';
import React from 'react';

// Initialize core commands
export function initializeCoreCommands(
    handlers: {
        toggleSidebar: () => void,
        toggleConversationTree: () => void,
        toggleSettings: () => void,
        openNewWindow: () => void,
    }
) {
    // Conversation commands
    registerCommandGroup({
        id: 'conversation',
        name: 'Conversation',
        priority: 100,
        commands: [
            {
                id: 'new-conversation',
                name: 'New Conversation',
                description: 'Start a new chat conversation',
                shortcut: navigator.platform.indexOf('Mac') !== -1 ? '⌘+N' : 'Ctrl+N',
                keywords: ['new', 'chat', 'conversation', 'start', 'begin', 'create', 'fresh', 'initiate', 'launch', 'commence', 'discuss', 'dialogue', 'interact', 'message', 'converse'],
                section: 'conversation',
                icon: React.createElement(Plus, { size: 16 }),
                execute: () => {
                    const conversationId = `conv_${uuidv4()}`;
                    store.dispatch(createConversation({
                        id: conversationId,
                        rootMessage: {
                            id: `msg_${Date.now()}`,
                            content: '',
                            source: 'system',
                            timestamp: Date.now()
                        }
                    }));
                }
            },
            {
                id: 'clear-conversation',
                name: 'Clear Current Conversation',
                description: 'Clear all messages in the current conversation',
                keywords: ['clear', 'reset', 'empty', 'delete', 'clean', 'wipe', 'erase', 'remove', 'purge', 'refresh', 'restart', 'flush', 'discard', 'renew', 'start-over'],
                section: 'conversation',
                icon: React.createElement(RefreshCw, { size: 16 }),
                execute: () => {
                    const state = store.getState();
                    const activeConversationId = state.conversations.activeConversationId;

                    if (activeConversationId) {
                        // Create a new conversation to replace the current one
                        const conversationId = `conv_${uuidv4()}`;
                        store.dispatch(createConversation({
                            id: conversationId,
                            rootMessage: {
                                id: `msg_${Date.now()}`,
                                content: '',
                                source: 'system',
                                timestamp: Date.now()
                            }
                        }));
                    }
                }
            }
        ]
    });

    // View commands
    registerCommandGroup({
        id: 'view',
        name: 'View',
        priority: 90,
        commands: [
            {
                id: 'toggle-sidebar',
                name: 'Toggle Sidebar',
                shortcut: navigator.platform.indexOf('Mac') !== -1 ? '⌘+B' : 'Ctrl+B',
                keywords: ['sidebar', 'menu', 'conversations', 'history', 'panel', 'navigation', 'side', 'drawer', 'toggle', 'hide', 'show', 'expand', 'collapse', 'fold', 'unfold'],
                section: 'view',
                icon: React.createElement(Plus, { size: 16 }),
                execute: handlers.toggleSidebar
            },
            {
                id: 'toggle-conversation-tree',
                name: 'Toggle Conversation Tree',
                shortcut: navigator.platform.indexOf('Mac') !== -1 ? '⌘+T' : 'Ctrl+T',
                keywords: ['tree', 'structure', 'map', 'messages', 'branch', 'hierarchy', 'conversation', 'flow', 'thread', 'graph', 'layout', 'diagram', 'organization', 'view', 'visualize'],
                section: 'view',
                icon: React.createElement(GitBranch, { size: 16 }),
                execute: handlers.toggleConversationTree
            },
            {
                id: 'toggle-settings',
                name: 'Toggle Settings Panel',
                shortcut: navigator.platform.indexOf('Mac') !== -1 ? '⌘+,' : 'Ctrl+,',
                keywords: ['settings', 'options', 'preferences', 'configure', 'setup', 'customize', 'adjust', 'modify', 'tweak', 'parameters', 'configuration', 'control', 'personalize', 'tune', 'manage'],
                section: 'view',
                icon: React.createElement(Settings, { size: 16 }),
                execute: handlers.toggleSettings
            },
            {
                id: 'open-new-window',
                name: 'Open New Window',
                keywords: ['window', 'open', 'new', 'external', 'launch', 'spawn', 'create', 'instance', 'tab', 'separate', 'additional', 'fresh', 'popup', 'session', 'view'],
                section: 'view',
                icon: React.createElement(ExternalLink, { size: 16 }),
                execute: handlers.openNewWindow
            }
        ]
    });

    // You can add more command groups here
}