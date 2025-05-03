// AiStudioClient\src\components\ConvTreeView\ConvTreeView.tsx
import React, { useState, useEffect, useRef, useCallback, memo } from 'react';
import { cn } from '@/lib/utils';
import { Message } from '@/types/conv';
import { useConvStore } from '@/stores/useConvStore';
import { useThemeStore } from '@/stores/useThemeStore';
import { TreeViewProps } from './types';
import { useMessageTree } from './useMessageTree';
import { useTreeVisualization } from './useTreeVisualization';
import { EmptyTreeView } from './EmptyTreeView';
import { TreeControls } from './TreeControls';

// Use React.memo to prevent unnecessary re-renders
const ConvTreeViewComponent: React.FC<TreeViewProps> = ({ convId, messages }) => {
    const [updateKey, setUpdateKey] = useState(0);
    const { setActiveConv, convs, slctdMsgId } = useConvStore();
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
    // But NOT when only slctdMsgId changes
    useEffect(() => {
        setUpdateKey((prev) => prev + 1);
    }, [convId, activeThemeId]);

    // Convert messages to hierarchical tree data - memoized in useMessageTree
    const hierarchicalData = useMessageTree(messages);

    // Handle node click to set active conversation and message
    const handleNodeClick = useCallback((nodeId: string, nodeSource: string, nodeContent: string) => {
        const conv = convs[convId];
        
        // Set the prompt content when clicking user nodes
        if (conv && nodeSource === 'user') {
            const message = conv.messages.find(msg => msg.id === nodeId);
            if (message) {
                // Set the prompt to the user message content
                if (window.setPrompt) {
                    window.setPrompt(message.content);
                }
                
                // If it has a parent, select the parent (AI message)
                if (message.parentId) {
                    setActiveConv({
                        convId: convId,
                        slctdMsgId: message.parentId,
                    });
                    // Scroll to the last message after a brief delay
                    setTimeout(() => scrollToMessage(), 100);
                    return;
                }
            }
        }
        
        setActiveConv({
            convId: convId, slctdMsgId: nodeId,
        });
        // Scroll to the last message after a brief delay
        setTimeout(() => scrollToMessage(), 100);
    }, [convId, convs, setActiveConv]);

    // Handle middle-click to delete a message and its descendants
    const handleNodeMiddleClick = useCallback((event: any, nodeId: string) => {
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
    }, [convId]);

    // Use the tree visualization hook
    const {
        zoomRef,
        handleZoomIn,
        handleZoomOut,
        handleCenter,
        handleFocusOnLatest,
        updateSelectedNode
    } = useTreeVisualization({
        svgRef,
        containerRef,
        hierarchicalData,
        onNodeClick: handleNodeClick,
        onNodeMiddleClick: handleNodeMiddleClick,
        updateKey,
        selectedMessageId: slctdMsgId
    });
    
    // Update selected node when slctdMsgId changes without full re-initialization
    useEffect(() => {
        if (svgRef.current && slctdMsgId) {
            updateSelectedNode(slctdMsgId);
        }
    }, [slctdMsgId, updateSelectedNode]);

    // Create a memoized version of handleFocusOnLatest that uses the current messages
    const focusOnLatest = useCallback(() => {
        handleFocusOnLatest(messages);
    }, [handleFocusOnLatest, messages]);

    // Render empty state if no messages
    if (!messages.length) {
        return <EmptyTreeView />;
    }

    return (
        <div className="ConvTreeView flex flex-col h-full w-full"
            style={{
                backgroundColor: 'var(--convtree-bg, var(--global-background-color, #111827))',
                color: 'var(--convtree-text-color, var(--global-text-color, #e5e7eb))',
                fontFamily: 'var(--convtree-font-family, var(--global-font-family, inherit))',
                fontSize: 'var(--convtree-font-size, var(--global-font-size, 0.875rem))',
                borderRadius: 'var(--convtree-border-radius, var(--global-border-radius, 0))',
                boxShadow: 'var(--convtree-box-shadow, var(--global-box-shadow, none))',
                borderColor: 'var(--convtree-border-color, var(--global-border-color, #1f2937))',
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
                        backgroundColor: 'var(--convtree-bg, var(--global-background-color, #111827))'
                    }}
                />

                {/* Tree controls */
                    <TreeControls
                        onFocusLatest={focusOnLatest}
                        onCenter={handleCenter}
                        onZoomIn={handleZoomIn}
                        onZoomOut={handleZoomOut}
                    />
                }
            </div>
        </div>
    );
};

// Export the memoized component
export const ConvTreeView = memo(ConvTreeViewComponent);

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
    borderRadius: {
        cssVar: '--convtree-border-radius',
        description: 'Conversation tree border radius',
        default: '0',
    },
    boxShadow: {
        cssVar: '--convtree-box-shadow',
        description: 'Conversation tree box shadow',
        default: 'none',
    },
    fontFamily: {
        cssVar: '--convtree-font-family',
        description: 'Conversation tree font family',
        default: 'inherit',
    },
    fontSize: {
        cssVar: '--convtree-font-size',
        description: 'Conversation tree font size',
        default: '0.875rem',
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