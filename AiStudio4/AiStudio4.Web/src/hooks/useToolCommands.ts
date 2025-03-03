// src/hooks/useToolCommands.ts
import { useRef, useEffect, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { initializeToolCommands, registerToolsAsCommands } from '@/commands/toolCommands';
import { addActiveTool, removeActiveTool } from '@/store/toolSlice';
import { useGetToolsQuery } from '@/services/api/toolsApi';

export function useToolCommands({
    openToolPanel,
    createNewTool,
    importTools,
    exportTools,
}: {
    openToolPanel: () => void;
    createNewTool: () => void;
    importTools: () => void;
    exportTools: () => void;
}) {
    const dispatch = useDispatch();
    const { activeTools } = useSelector((state: any) => state.tools);

    // Use RTK Query to get tools
    const { data: tools = [] } = useGetToolsQuery();

    // Function to toggle a tool's active state
    const toggleTool = useCallback((toolId: string, activate: boolean) => {
        if (activate) {
            dispatch(addActiveTool(toolId));
        } else {
            dispatch(removeActiveTool(toolId));
        }
    }, [dispatch]);

    useEffect(() => {
        // Only initialize commands once tools are loaded
        if (tools.length > 0) {
            initializeToolCommands({
                openToolPanel,
                createNewTool,
                importTools,
                exportTools
            });

            // Register each individual tool as a command
            registerToolsAsCommands(tools, activeTools, toggleTool);
        }
    }, [tools, activeTools, openToolPanel, createNewTool, importTools, exportTools, toggleTool]);

    return { toggleTool };
}