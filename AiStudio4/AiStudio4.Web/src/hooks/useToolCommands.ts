// src/hooks/useToolCommands.ts
import { useRef, useEffect, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { initializeToolCommands, registerToolsAsCommands } from '@/commands/toolCommands';
import { ToolSelectorMethods } from '@/components/tools/ToolSelector';
import { fetchTools, addActiveTool, removeActiveTool } from '@/store/toolSlice';
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
    const toolSelectorRef = useRef<ToolSelectorMethods>(null);
    const { tools, activeTools } = useSelector((state: RootState) => state.tools);
    
    // Function to toggle a tool's active state
    const toggleTool = useCallback((toolId: string, activate: boolean) => {
        if (activate) {
            dispatch(addActiveTool(toolId));
        } else {
            dispatch(removeActiveTool(toolId));
        }
    }, [dispatch]);

    useEffect(() => {
        // First make sure tools are loaded
        dispatch(fetchTools());
    }, [dispatch]);

    useEffect(() => {
        // Only initialize commands once tools are loaded
        if (tools.length > 0) {
            initializeToolCommands({
                openToolPanel,
                createNewTool,
                importTools,
                exportTools,
                toggleTool: (toolId: string) => {
                    if (toolSelectorRef.current) {
                        toolSelectorRef.current.toggleTool(toolId);
                    }
                },
                getAvailableTools: async () => {
                    if (toolSelectorRef.current) {
                        return await toolSelectorRef.current.getAvailableTools();
                    }
                    return [];
                },
            });
            
            // Register each individual tool as a command
            registerToolsAsCommands(tools, activeTools, toggleTool);
        }
    }, [tools, activeTools, openToolPanel, createNewTool, importTools, exportTools, toggleTool]);

    return { toolSelectorRef };
}