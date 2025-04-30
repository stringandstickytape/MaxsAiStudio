// AiStudio4.Web\src\components\ConvTreeView\ConvTreeView.tsx
import React, { useState, useEffect, useRef, useCallback } from 'react';
import { cn } from '@/lib/utils';
import { Message } from '@/types/conv';
import { useConvStore } from '@/stores/useConvStore';
import { useThemeStore } from '@/stores/useThemeStore';
import { TreeViewProps } from './types';
import { useMessageTree } from './useMessageTree';
import { useTreeVisualization } from './useTreeVisualization';
import { EmptyTreeView } from './EmptyTreeView';
import { TreeControls } from './TreeControls';

export const ConvTreeView: React.FC<TreeViewProps> = ({ convId, messages }) => {
    const [updateKey, setUpdateKey] = useState(0);
    const { setActiveConv, convs } = useConvStore();
    const svgRef = useRef<SVGSVGElement>(null);
    const containerRef = useRef<HTMLDivElement>(null);
    
    // Helper function to scroll to the last message
    const scrollToMessage = () => {
        // Find all message containers in the chat view
        const messageElements = document.querySelectorAll('.message-container');

        // Check if there are any message elements
        if (messageElements.length > 0) {
            // Get the last message element
            const lastMessageElement = messageElements[messageElements.length - 1];

            // Jump the top of the last message element into view
            lastMessageElement.scrollIntoView({ block: 'start' });
        }
    };

    // Subscribe to activeThemeId to trigger redraw on theme change
    const activeThemeId = useThemeStore(state => state.activeThemeId);

    // Update the key when convId or theme changes to force re-render
    useEffect(() => {
        setUpdateKey((prev) => prev + 1);
    }, [convId, activeThemeId]);

    // Convert messages to hierarchical tree data
    const hierarchicalData = useMessageTree(messages);

    // Handle node click to set active conversation and message
    const handleNodeClick = (nodeId: string, nodeSource: string, nodeContent: string) => {
        if (nodeSource === 'user') {
            window.setPrompt(nodeContent);
            const conv = convs[convId];
            if (conv) {
                const message = conv.messages.find(msg => msg.id === nodeId);
                if (message && message.parentId) {
                    setActiveConv({
                        convId: convId,
                        slctdMsgId: message.parentId,
                    });
                    // Scroll to the last message after a brief delay
                    setTimeout(() => scrollToMessage(), 100);
                    return;
                }
            }
        } else {
            window.setPrompt("");
        }
        setActiveConv({
            convId: convId, slctdMsgId: nodeId,
        });
        // Scroll to the last message after a brief delay
        setTimeout(() => scrollToMessage(), 100);
    };

    // Handle middle-click to delete a message and its descendants
    const handleNodeMiddleClick = (event: any, nodeId: string) => {
        // Middle mouse button is button 1
        if (event.button === 1) {
            event.preventDefault();

            // Show confirmation dialog
            if (window.confirm('Delete this message and all its descendants?')) {
                // Delete message and its descendants locally
                useConvStore.getState().deleteMessage({ convId, messageId: nodeId });

                // Delete on the server
                import('@/services/api/apiClient').then(({ deleteMessageWithDescendants }) => {
                    deleteMessageWithDescendants({ convId, messageId: nodeId })
                        .catch(e => console.error('Failed to delete message on server:', e));
                });
            }
        }
    };

    // Use the tree visualization hook
    const {
        zoomRef,
        handleZoomIn,
        handleZoomOut,
        handleCenter,
        handleFocusOnLatest
    } = useTreeVisualization({
        svgRef,
        containerRef,
        hierarchicalData,
        onNodeClick: handleNodeClick,
        onNodeMiddleClick: handleNodeMiddleClick,
        updateKey
    });

    // Create a memoized version of handleFocusOnLatest that uses the current messages
    const focusOnLatest = useCallback(() => {
        handleFocusOnLatest(messages);
    }, [handleFocusOnLatest, messages]);

    // Auto-focus on latest message when a new message is added
    useEffect(() => {
        // We need a slight delay to let the tree render after message changes
        const timer = setTimeout(() => {
            if (messages.length > 0) {
                focusOnLatest();
            }
        }, 300);

        return () => clearTimeout(timer);
    }, [messages.length, focusOnLatest]);

    // Render empty state if no messages
    if (!messages.length) {
        return <EmptyTreeView />;
    }

    return (
        <div className="ConvTreeView flex flex-col h-full w-full"
            style={{
                ...(window?.theme?.ConvTreeView?.style || {})
            }}
        >
            <div
                className={cn('ConvTreeView flex-1 relative', !messages.length && 'flex items-center justify-center')}
                ref={containerRef}
                style={{
                    height: '100%'
                }}
            >
                <svg
                    ref={svgRef}
                    className="ConvTreeView w-full h-full"
                    key={`tree-${convId}-${updateKey}`}
                    style={{
                        backgroundColor: 'var(--convtree-bg, #111827)'
                    }}
                />

                {/* Info tooltip about middle-click functionality */}
                <div className="ConvTreeView absolute top-2 left-2 text-xs px-2 py-1 rounded-md backdrop-blur-sm"
                    style={{
                        backgroundColor: 'var(--convtree-bg, rgba(31, 41, 55, 0.7))',
                        color: 'var(--convtree-text-color, #9ca3af)',
                        backdropFilter: 'blur(4px)'
                    }}
                >
                    <span>Middle-click to delete</span>
                </div>

                {/* Tree controls */}
                <TreeControls 
                    onFocusLatest={focusOnLatest}
                    onCenter={handleCenter}
                    onZoomIn={handleZoomIn}
                    onZoomOut={handleZoomOut}
                />
            </div>
        </div>
    );
};

// Export themeable properties for ThemeManager
export const themeableProps = {
    backgroundColor: {
        cssVar: '--convtree-bg',
        description: 'Conversation tree background color',
        default: '#111827',
    },
    textColor: {
        cssVar: '--convtree-text-color',
        description: 'Conversation tree text color',
        default: '#e5e7eb',
    },
    borderColor: {
        cssVar: '--convtree-border-color',
        description: 'Conversation tree border color',
        default: '#1f2937',
    },
    accentColor: {
        cssVar: '--convtree-accent-color',
        description: 'Conversation tree accent color for highlights and active elements',
        default: '#4f46e5',
    },
    style: {
        description: 'Arbitrary CSS style for ConvTreeView root',
        default: {},
    },
    // Keeping node colors as they're essential for the tree visualization
    userNodeColor: {
        cssVar: '--convtree-user-node-color',
        description: 'User message node background color',
        default: '#1e40af',
    },
    systemNodeColor: {
        cssVar: '--convtree-system-node-color',
        description: 'System message node background color',
        default: '#4B5563',
    },
    aiNodeColor: {
        cssVar: '--convtree-ai-node-color',
        description: 'AI message node background color',
        default: '#4f46e5',
    },
    linkColor: {
        cssVar: '--convtree-link-color',
        description: 'Tree connection line color',
        default: '#6b7280',
    },
    // Add message styling properties from ConvView for consistency
    userMessageBackground: {
        cssVar: '--user-message-background',
        description: 'Background for user messages (supports gradients, images, etc.)',
        default: '#1e40af', // blue-800
    },
    aiMessageBackground: {
        cssVar: '--ai-message-background',
        description: 'Background for assistant messages (supports gradients, images, etc.)',
        default: '#1f2937', // gray-800
    },
    userMessageBorderColor: {
        cssVar: '--user-message-border-color',
        description: 'Border color for user messages',
        default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
    },
    aiMessageBorderColor: {
        cssVar: '--ai-message-border-color',
        description: 'Border color for assistant messages',
        default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
    },
    userMessageBorderWidth: {
        cssVar: '--user-message-border-width',
        description: 'Border width for user messages',
        default: '0px',
    },
    aiMessageBorderWidth: {
        cssVar: '--ai-message-border-width',
        description: 'Border width for assistant messages',
        default: '0px',
    },
    userMessageBorderStyle: {
        cssVar: '--user-message-border-style',
        description: 'Border style for user messages',
        default: 'solid',
    },
    aiMessageBorderStyle: {
        cssVar: '--ai-message-border-style',
        description: 'Border style for assistant messages',
        default: 'solid',
    }
};