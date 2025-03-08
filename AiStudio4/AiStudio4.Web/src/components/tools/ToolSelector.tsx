// src/components/tools/ToolSelector.tsx
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Plus, Wrench } from 'lucide-react';
import { useToolStore } from '@/stores/useToolStore';
import { useGetToolsQuery } from '@/services/api/toolsApi';
import { commandRegistry } from '@/commands/commandRegistry';

interface ToolSelectorProps {
    onManageTools?: () => void;
}

export function ToolSelector({ onManageTools }: ToolSelectorProps) {
    // Use Zustand store instead of Redux
    const { activeTools, addActiveTool, removeActiveTool } = useToolStore();
    const { data: tools = [] } = useGetToolsQuery();
    const [open, setOpen] = useState(false);

    const handleToolToggle = (toolId: string, checked: boolean) => {
        if (checked) {
            addActiveTool(toolId);
        } else {
            removeActiveTool(toolId);
        }
    };

    const handleManageTools = () => {
        // Close the popover first
        setOpen(false);

        const commandSuccess = commandRegistry.executeCommand('manage-tools');
    };

    return (
        <div className="flex items-center space-x-2">
            <div className="text-sm text-gray-400">Active Tools:</div>
            <div className="flex flex-wrap gap-2">
                {activeTools.map(toolId => {
                    const tool = tools.find(t => t.guid === toolId);
                    if (!tool) return null;

                    return (
                        <Button
                            key={tool.guid}
                            variant="outline"
                            size="sm"
                            className="bg-gray-800/60 border-gray-700 text-gray-300 hover:bg-gray-700 hover:text-gray-100"
                            onClick={() => handleToolToggle(tool.guid, false)}
                        >
                            {tool.name}
                        </Button>
                    );
                })}

                <Popover open={open} onOpenChange={setOpen}>
                    <PopoverTrigger asChild>
                        <Button
                            variant="outline"
                            size="sm"
                            className="bg-gray-800/60 border-gray-700 text-gray-300 hover:bg-gray-700 hover:text-gray-100"
                        >
                            <Plus className="h-4 w-4 mr-1" />
                        </Button>
                    </PopoverTrigger>
                    <PopoverContent className="w-64 p-2 bg-gray-800 border-gray-700 text-gray-100">
                        <div className="space-y-2 max-h-60 overflow-y-auto">
                            {tools.map(tool => (
                                <div key={tool.guid} className="flex items-center space-x-2">
                                    <Checkbox
                                        id={`tool-${tool.guid}`}
                                        checked={activeTools.includes(tool.guid)}
                                        onCheckedChange={(checked) => handleToolToggle(tool.guid, checked as boolean)}
                                    />
                                    <Label htmlFor={`tool-${tool.guid}`} className="text-sm cursor-pointer">
                                        {tool.name}
                                    </Label>
                                </div>
                            ))}
                        </div>

                        <div className="mt-4 pt-2 border-t border-gray-700">
                            <Button
                                variant="outline"
                                size="sm"
                                className="w-full mt-1 bg-gray-700/60 border-gray-600 text-gray-200 hover:bg-gray-600 hover:text-white flex items-center justify-center"
                                onClick={handleManageTools}
                            >
                                <span className="mr-1">Manage Tools</span>
                                <Wrench className="h-3.5 w-3.5" />
                            </Button>
                        </div>
                    </PopoverContent>
                </Popover>
            </div>
        </div>
    );
}