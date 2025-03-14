// src/hooks/useToolCommands.ts
import { useEffect, useCallback } from 'react';
import { initializeToolCommands, registerToolsAsCommands } from '@/commands/toolCommands';
import { useToolStore } from '@/stores/useToolStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';

export function useToolCommands({
  openToolLibrary,
  createNewTool,
  exportTools,
}: {
  openToolLibrary: () => void;
  createNewTool: () => void;
  exportTools: () => void;
}) {
  
  const { activeTools, addActiveTool, removeActiveTool } = useToolStore();

  
  const { tools } = useToolsManagement();

  
  const toggleTool = useCallback(
    (toolId: string, activate: boolean) => {
      if (activate) {
        addActiveTool(toolId);
      } else {
        removeActiveTool(toolId);
      }
    },
    [addActiveTool, removeActiveTool],
  );

  useEffect(() => {
    
    if (tools.length > 0) {
      initializeToolCommands({
        openToolLibrary,
        createNewTool,
        exportTools,
      });

      
      registerToolsAsCommands(tools, activeTools, toggleTool);
    }
  }, [tools, activeTools, openToolLibrary, createNewTool, exportTools, toggleTool]);

  return { toggleTool };
}

