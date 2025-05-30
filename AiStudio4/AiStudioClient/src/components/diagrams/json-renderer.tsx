import { CodeBlockRenderer } from './types';
import { useState } from 'react';
import { cn } from '@/lib/utils';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { ChevronRight, ChevronDown } from 'lucide-react';

const JsonNode = ({ data, level = 0 }: { data: any; level?: number }) => {
    // Handle undefined/null data
    if (data === undefined || data === null) {
        return (
            <span className="text-gray-400">
                {JSON.stringify(data)}
            </span>
        );
    }

    const [isOpen, setIsOpen] = useState(level < 2);
    const type = Array.isArray(data) ? 'array' : typeof data;
    const isExpandable = type === 'object' || type === 'array';

    // Safely handle children property
    if (data?.children && !Array.isArray(data.children)) {
        try {
            data.children = [data.children];
        } catch (error) {
            console.error("Error converting children to array:", error);
        }
    }

    if (!isExpandable) {
        return (
            <span
                className={cn(
                    type === 'string' && 'text-green-400',
                    type === 'number' && 'text-blue-400',
                    type === 'boolean' && 'text-yellow-400',
                    data === null && 'text-gray-400',
                )}
            >
                {JSON.stringify(data)}
            </span>
        );
    }

    // Safely get items with error handling
    let items = [];
    try {
        items = Object.entries(data);
    } catch (error) {
        console.error("Error getting object entries:", error);
        return <span className="text-red-400">Error: Unable to process data</span>;
    }

    return (
        <Collapsible open={isOpen} onOpenChange={setIsOpen}>
            <div className="flex items-center gap-1">
                <CollapsibleTrigger className="hover:bg-accent p-1 rounded">
                    {isOpen ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                </CollapsibleTrigger>
                <span className="text-gray-400">{Array.isArray(data) ? '[' : '{'}</span>
                {!isOpen && (
                    <span className="text-gray-500">
                        {Array.isArray(data) ? `${data.length} items` : `${items.length} keys`}
                    </span>
                )}
            </div>
            <CollapsibleContent>
                <div className="ml-4 border-l border-gray-800">
                    {items.map(([key, value], index) => (
                        <div key={key || index} className="pl-4 py-1">
                            <span className="text-purple-400">{JSON.stringify(key)}</span>
                            <span className="text-gray-400">: </span>
                            <JsonNode data={value} level={level + 1} />
                            {index < items.length - 1 && <span className="text-gray-400">,</span>}
                        </div>
                    ))}
                </div>
            </CollapsibleContent>
            <div className="text-gray-400 ml-4">{isOpen && (Array.isArray(data) ? ']' : '}')}</div>
        </Collapsible>
    );
};

export const JsonRenderer: CodeBlockRenderer = {
    type: ['json'],
    initialize: () => { },
    render: async () => { },
    Component: ({ content, className }) => {
        if (content === undefined || content === null) {
            return <div className={`${className} text-yellow-500 p-4`}>No JSON content provided</div>;
        }

        try {
            const jsonData = typeof content === 'string' ? JSON.parse(content) : content;

            return (
                <div className={`${className || ''} p-4 font-mono text-sm`} style={{ whiteSpace: 'break-spaces' }}>
                    <JsonNode data={jsonData} />
                </div>
            );
        } catch (error) {
            console.error("JSON rendering error:", error);
            return (
                <div className={`${className || ''} text-red-500 p-4`}>
                    Invalid JSON content: {error instanceof Error ? error.message : String(error)}
                </div>
            );
        }
    },
};