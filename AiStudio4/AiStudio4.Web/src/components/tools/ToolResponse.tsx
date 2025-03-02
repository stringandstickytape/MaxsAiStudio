// src/components/tools/ToolResponse.tsx
import React from 'react';
import { Card } from '@/components/ui/card';

interface ToolResponseProps {
  toolName: string;
  functionName: string;
  parameters: Record<string, any>;
  result: any;
}

export function ToolResponse({ toolName, functionName, parameters, result }: ToolResponseProps) {
  const formatJson = (obj: any) => {
    try {
      return JSON.stringify(obj, null, 2);
    } catch (e) {
      return String(obj);
    }
  };

  return (
    <div className="my-2 px-1">
      <Card className="bg-gray-800/60 border border-gray-700/70 overflow-hidden">
        <div className="px-3 py-2 bg-gray-700/50 border-b border-gray-600/50 flex items-center">
          <div className="text-sm font-semibold text-gray-200 mr-1">TOOL USE:</div>
          <div className="text-sm font-medium text-blue-300">{toolName}</div>
        </div>
        <div className="p-3 space-y-2">
          <div className="flex">
            <div className="text-xs font-medium text-gray-400 w-24">Function:</div>
            <div className="text-xs font-mono text-gray-200">{functionName}</div>
          </div>
          <div className="flex">
            <div className="text-xs font-medium text-gray-400 w-24">Parameters:</div>
            <div className="text-xs font-mono text-gray-200 whitespace-pre-wrap overflow-x-auto max-w-full">
              {formatJson(parameters)}
            </div>
          </div>
          <div className="border-t border-gray-700/50 my-1 pt-1"></div>
          <div className="flex">
            <div className="text-xs font-medium text-gray-400 w-24">Result:</div>
            <div className="text-xs font-mono text-gray-200 whitespace-pre-wrap overflow-x-auto max-w-full">
              {formatJson(result)}
            </div>
          </div>
        </div>
      </Card>
    </div>
  );
}