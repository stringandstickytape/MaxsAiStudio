// src/hooks/useToolCommands.ts
import { useEffect, useCallback } from 'react';
import { initializeToolCommands, registerToolsAsCommands } from '@/commands/toolCommands';
import { useToolStore } from '@/stores/useToolStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';

export function useToolCommands({
    openToolPanel,
    createNewTool,
    exportTools,
}: {
    openToolPanel: () => void; 
    createNewTool: () => void;
    exportTools: () => void;
}) {
    // Use Zustand store instead of Redux
    const { activeTools, addActiveTool, removeActiveTool } = useToolStore();
    
    // Use the new management hook to get tools
    const { tools } = useToolsManagement();

    // Function to toggle a tool's active state
    const toggleTool = useCallback((toolId: string, activate: boolean) => {
        if (activate) {
            addActiveTool(toolId);
        } else {
            removeActiveTool(toolId);
        }
    }, [addActiveTool, removeActiveTool]);

    useEffect(() => {
        // Only initialize commands once tools are loaded
        if (tools.length > 0) {
            
            initializeToolCommands({
                openToolPanel,
                createNewTool,
                exportTools
            });

            // Register each individual tool as a command
            registerToolsAsCommands(tools, activeTools, toggleTool);
        }
    }, [tools, activeTools, openToolPanel, createNewTool, exportTools, toggleTool]);

    return { toggleTool };
}