import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { useDebugStore } from '@/stores/useDebugStore';
import { ContentBlockRendererProps } from './contentBlockRendererRegistry';
import { Button } from '../ui/button';
import { RefreshCw } from 'lucide-react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useModalStore } from '@/stores/useModalStore';

export const ToolResponseContentRenderer: React.FC<ContentBlockRendererProps> = ({ block }) => {
  const showDevContentView = useDebugStore((state) => state.showDevContentView);
  const { isLoading, executeApiCall } = useApiCallState();
  const { openModal } = useModalStore();

  // Parse the tool response JSON to get formatting information
  let toolData: any = {};
  let formattedContent = block.content;
  
  try {
    toolData = JSON.parse(block.content);
    const { result, statusMessage, outputFileType } = toolData;
    
    // Format the result based on outputFileType
    let resultContent = result || '';
    
    if (outputFileType && outputFileType.trim() !== '') {
      // Map unrecognized languages to appropriate ones for syntax highlighting
      let mappedLanguage = outputFileType;
      
      // Map custom tool output types to recognized languages (only for tools without rich renderers)
      const languageMap: Record<string, string> = {
        'stopprocessing': 'text',
        'recordmistake': 'text',
        // Add other custom output types as needed (but NOT ones that have rich renderers)
        // NOTE: modifyfiles, gitcommit, and codediff are NOT included here because they have rich renderers
      };
      
      if (languageMap[outputFileType]) {
        mappedLanguage = languageMap[outputFileType];
      }
      
      // Wrap in code block with mapped language
      resultContent = `\`\`\`${mappedLanguage}\n${result || ''}\n\`\`\``;
    } else {
      // Plain text display
      resultContent = result || '';
    }
    
    // Add status message if present
    if (statusMessage && statusMessage.trim() !== '') {
      if (resultContent) {
        formattedContent = `${resultContent}\n\n${statusMessage}`;
      } else {
        formattedContent = statusMessage;
      }
    } else {
      formattedContent = resultContent;
    }
  } catch (e) {
    // If JSON parsing fails, fall back to showing as JSON
    formattedContent = `\`\`\`json\n${block.content}\n\`\`\``;
  }

  const handleReapply = async () => {
    try {
      // Use the already parsed toolData if available
      const toolName = toolData.toolName;

      if (!toolName) {
          throw new Error("Tool content JSON is missing the 'toolName' property.");
      }

      const result = await executeApiCall(async () => {
        const reapplyRequest = createApiRequest('/api/reapplyTool', 'POST');
        return await reapplyRequest( toolParameters );
      });

      if (result?.success && result.result) {
        openModal('info', {
          title: `Re-applied: ${toolName}`,
          content: result.result.resultMessage || "Operation completed with no output."
        });
      } else {
        openModal('info', {
          title: 'Re-apply Failed',
          content: `An error occurred: ${result?.result?.resultMessage || result?.error || 'Unknown error'}`
        });
      }
    } catch (e) {
      console.error("Failed to parse tool content for re-application:", e);
      openModal('info', {
        title: 'Re-apply Error',
        content: `Could not process the re-apply command. The tool output may not be valid JSON.`
      });
    }
  };

  return (
    <div className="my-2 border-2 border-dashed border-blue-500/50 bg-blue-900/10 p-3 rounded-lg opacity-80 group">
      <div className="flex justify-between items-center mb-1">
        <div className="text-xs font-bold text-blue-400">
          Tool Result
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={handleReapply}
          disabled={isLoading}
          className="h-7 px-2 opacity-0 group-hover:opacity-100 transition-opacity text-blue-300 hover:text-blue-100 hover:bg-blue-500/20"
        >
          <RefreshCw className={`h-3 w-3 mr-1 ${isLoading ? 'animate-spin' : ''}`} />
          Reapply
        </Button>
      </div>
      <MarkdownPane message={formattedContent} />
    </div>
  );
};