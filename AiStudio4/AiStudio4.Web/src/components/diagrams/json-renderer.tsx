import { DiagramRenderer } from './types';
import { useState } from 'react';
import { cn } from '@/lib/utils';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { ChevronRight, ChevronDown } from 'lucide-react';

const JsonNode = ({ data, level = 0 }: { data: any; level?: number }) => {
    const [isOpen, setIsOpen] = useState(level < 2);
    const type = Array.isArray(data) ? 'array' : typeof data;
    const isExpandable = type === 'object' || type === 'array';

    // Normalize children to always be an array if present
    if (data?.children && !Array.isArray(data.children)) {
        data.children = [data.children];
    }
    if (!isExpandable) {
        return (
            <span className={cn(
                type === 'string' && 'text-green-400',
                type === 'number' && 'text-blue-400',
                type === 'boolean' && 'text-yellow-400',
                data === null && 'text-gray-400'
            )}>
                {JSON.stringify(data)}
            </span>
        );
    }

    const items = Object.entries(data);

    return (
        <Collapsible open={isOpen} onOpenChange={setIsOpen}>
            <div className="flex items-center gap-1">
                <CollapsibleTrigger className="hover:bg-accent p-1 rounded">
                    {isOpen ? (
                        <ChevronDown className="h-4 w-4" />
                    ) : (
                        <ChevronRight className="h-4 w-4" />
                    )}
                </CollapsibleTrigger>
                <span className="text-gray-400">
                    {Array.isArray(data) ? '[' : '{'}
                </span>
                {!isOpen && (
                    <span className="text-gray-500">
                        {Array.isArray(data) ? `${data.length} items` : `${items.length} keys`}
                    </span>
                )}
            </div>
            <CollapsibleContent>
                <div className="ml-4 border-l border-gray-800">
                    {items.map(([key, value], index) => (
                        <div key={key} className="pl-4 py-1">
                            <span className="text-purple-400">{JSON.stringify(key)}</span>
                            <span className="text-gray-400">: </span>
                            <JsonNode data={value} level={level + 1} />
                            {index < items.length - 1 && <span className="text-gray-400">,</span>}
                        </div>
                    ))}
                </div>
            </CollapsibleContent>
            <div className="text-gray-400 ml-4">
                {isOpen && (Array.isArray(data) ? ']' : '}')}
            </div>
        </Collapsible>
    );
};

export const JsonRenderer: DiagramRenderer = {
    type: 'json',
    initialize: () => {},
    render: async () => {},
    Component: ({ content, className }) => {
        try {
            const jsonData = typeof content === 'string' ? JSON.parse(content) : content;
            
            return (
                <div className={`${className} p-4 font-mono text-sm`}>
                    <JsonNode data={jsonData} />
                </div>
            );
        } catch (error) {
            return (
                <div className={`${className} text-red-500 p-4`}>
                    Invalid JSON content
                </div>
            );
        }
    }
};