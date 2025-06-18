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

  return (
      <MarkdownPane message={formattedContent} />
  );
};