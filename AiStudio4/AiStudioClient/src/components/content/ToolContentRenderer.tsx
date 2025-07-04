import React from 'react';
import { MarkdownPane } from '@/components/MarkdownPane';
import { useDebugStore } from '@/stores/useDebugStore';
import { ContentBlockRendererProps } from './contentBlockRendererRegistry';
import { Button } from '../ui/button';
import { RefreshCw } from 'lucide-react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useModalStore } from '@/stores/useModalStore';

export const ToolContentRenderer: React.FC<ContentBlockRendererProps> = ({ block, messageId }) => {
  const showDevContentView = useDebugStore((state) => state.showDevContentView);
  const { isLoading, executeApiCall } = useApiCallState();
  const { openModal } = useModalStore();

  if (!showDevContentView) {
    return null;
  }

  const handleReapply = async () => {
    try {
      // The tool content is already JSON, but wrapped in a markdown block.
      // We need to extract the raw JSON from the `block.content` property.
      const toolParameters = block.content;
      const toolData = JSON.parse(toolParameters);
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
          Tool Use
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
      <MarkdownPane message={`\`\`\`json\n${block.content}\n\`\`\``} messageId={messageId} />
    </div>
  );
};