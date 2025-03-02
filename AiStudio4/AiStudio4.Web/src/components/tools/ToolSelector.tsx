// src/components/tools/ToolSelector.tsx
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '@/store/store';
import { Button } from '@/components/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Plus, Tool } from 'lucide-react';
import { fetchTools, setActiveTools, addActiveTool, removeActiveTool } from '@/store/toolSlice';

interface ToolSelectorProps {
  onManageTools?: () => void;
}

export function ToolSelector({ onManageTools }: ToolSelectorProps) {
  const dispatch = useDispatch();
  const { tools, activeTools } = useSelector((state: RootState) => state.tools);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    dispatch(fetchTools());
  }, [dispatch]);

  const handleToolToggle = (toolId: string, checked: boolean) => {
    console.log(`Tool ${toolId} toggled: ${checked ? 'activated' : 'deactivated'}`);
    if (checked) {
      dispatch(addActiveTool(toolId));
    } else {
      dispatch(removeActiveTool(toolId));
    }
    console.log('Active tools after toggle:', [...activeTools, checked ? toolId : null].filter(Boolean));
    if (checked) {
      dispatch(addActiveTool(toolId));
    } else {
      dispatch(removeActiveTool(toolId));
    }
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
                variant="link" 
                size="sm" 
                className="text-blue-400 hover:text-blue-300 p-0"
                onClick={onManageTools}
              >
                Manage Tools...
              </Button>
            </div>
          </PopoverContent>
        </Popover>
      </div>
    </div>
  );
}