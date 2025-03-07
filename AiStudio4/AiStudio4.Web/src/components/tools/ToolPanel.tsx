// src/components/tools/ToolPanel.tsx
import { useState, useEffect } from 'react';
import { useToolStore } from '@/stores/useToolStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Plus, Search, Edit, Trash2, Copy, Import, Download } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ToolEditor } from './ToolEditor';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { 
  useGetToolsQuery, 
  useGetToolCategoriesQuery, 
  useDeleteToolMutation,
  useImportToolsMutation,
  useExportToolsMutation
} from '@/services/api/toolsApi';

interface ToolPanelProps {
  isOpen: boolean;
  onClose?: () => void;
}

export function ToolPanel({ isOpen, onClose }: ToolPanelProps) {
  // RTK Query hooks
  const { data: tools = [], isLoading: toolsLoading, refetch: refetchTools } = useGetToolsQuery(undefined, {
    skip: !isOpen // Only fetch when panel is open
  });
  const { data: categories = [], refetch: refetchCategories } = useGetToolCategoriesQuery(undefined, {
    skip: !isOpen // Only fetch when panel is open
  });
  const [deleteTool, { isLoading: isDeleting }] = useDeleteToolMutation();
  const [importTools, { isLoading: isImporting }] = useImportToolsMutation();
  const [exportTools, { isLoading: isExporting }] = useExportToolsMutation();
  
  // Use Zustand store to sync tools and categories
  const { setTools, setCategories } = useToolStore();

  // Sync tools and categories with Zustand store when they change
  useEffect(() => {
    if (tools.length > 0) {
      setTools(tools);
    }
  }, [tools, setTools]);

  useEffect(() => {
    if (categories.length > 0) {
      setCategories(categories);
    }
  }, [categories, setCategories]);
  
  // Local state
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [currentTool, setCurrentTool] = useState<Tool | null>(null);
  
  useEffect(() => {
    if (isOpen) {
      refetchTools();
      refetchCategories();
    }
  }, [isOpen, refetchTools, refetchCategories]);

  useEffect(() => {
    const pendingAction = window.localStorage.getItem('toolPanel_action');
    if (pendingAction) {
      if (pendingAction === 'create') {
        handleAddTool();
      } else if (pendingAction === 'import') {
        // Trigger file input click for import
        const input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';
        input.click();
      } else if (pendingAction === 'export') {
        // Handle export action
        handleExportTools();
      }
      // Clear the action after processing
      window.localStorage.removeItem('toolPanel_action');
    }
  }, [isOpen]);

  const handleAddTool = () => {
    setCurrentTool(null);
    setIsEditorOpen(true);
  };

  const handleEditTool = (tool: Tool) => {
    setCurrentTool(tool);
    setIsEditorOpen(true);
  };

  const handleDeleteTool = async (toolId: string) => {
    if (window.confirm('Are you sure you want to delete this tool?')) {
      try {
        await deleteTool(toolId).unwrap();
      } catch (error) {
        console.error('Failed to delete tool:', error);
        alert(`Failed to delete tool: ${error instanceof Error ? error.message : 'Unknown error'}`);
      }
    }
  };

  const handleImportTools = () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = async (e) => {
      const file = (e.target as HTMLInputElement).files?.[0];
      if (file) {
        const reader = new FileReader();
        reader.onload = async (event) => {
          const json = event.target?.result as string;
          try {
            await importTools(json).unwrap();
            // Tools list will auto-update due to RTK Query cache invalidation
          } catch (error) {
            console.error('Error importing tools:', error);
            alert(`Failed to import tools: ${error instanceof Error ? error.message : 'Unknown error'}`);
          }
        };
        reader.readAsText(file);
      }
    };
    input.click();
  };

  const handleExportTools = async () => {
    try {
      const json = await exportTools(undefined).unwrap();
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'tools-export.json';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error exporting tools:', error);
      alert(`Failed to export tools: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  const filteredTools = tools.filter(tool => {
    const matchesSearch = searchTerm === '' || 
      tool.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      tool.description.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesCategory = selectedCategory === null || 
      tool.categories.includes(selectedCategory);
    
    return matchesSearch && matchesCategory;
  });

  if (!isOpen) return null;

  // Determine loading state
  const isLoading = toolsLoading || isDeleting || isImporting || isExporting;

  return (
    <div className="p-4 overflow-y-auto h-full bg-gray-900 text-gray-100">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-xl font-semibold">Tool Library</h2>
        <div className="flex space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleAddTool}
            className="flex items-center space-x-1 bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50"
          >
            <Plus className="h-4 w-4" />
            <span>New Tool</span>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
        <div className="md:col-span-1">
          <Card className="bg-gray-800 border-gray-700">
            <CardHeader>
              <CardTitle className="text-sm font-medium">Categories</CardTitle>
            </CardHeader>
            <CardContent className="p-2">
              <div className="space-y-1">
                <Button
                  variant={selectedCategory === null ? "default" : "ghost"}
                  size="sm"
                  className="w-full justify-start"
                  onClick={() => setSelectedCategory(null)}
                >
                  All Tools
                </Button>
                {categories.map(category => (
                  <Button
                    key={category.id}
                    variant={selectedCategory === category.id ? "default" : "ghost"}
                    size="sm"
                    className="w-full justify-start"
                    onClick={() => setSelectedCategory(category.id)}
                  >
                    {category.name}
                  </Button>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        <div className="md:col-span-3">
          <div className="mb-4">
            <div className="relative">
              <Search className="absolute left-2 top-2.5 h-4 w-4 text-gray-500" />
              <Input
                placeholder="Search tools..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-8 bg-gray-800 border-gray-700 text-gray-100"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="flex items-center justify-center h-32">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            </div>
          ) : filteredTools.length === 0 ? (
            <div className="text-center p-8 text-gray-400">
              {searchTerm ? 'No tools match your search' : 'No tools available'}
            </div>
          ) : (
            <div className="space-y-3">
              {filteredTools.map(tool => (
                <Card key={tool.guid} className="bg-gray-800 border-gray-700">
                  <CardContent className="p-4">
                    <div className="flex justify-between items-start">
                      <div>
                        <h3 className="text-lg font-medium text-gray-100">{tool.name}</h3>
                        <p className="text-sm text-gray-400">{tool.description}</p>
                        <div className="flex flex-wrap gap-1 mt-2">
                          {(tool.categories || []).map(catId => {
                            const category = categories.find(c => c.id === catId);
                            return category ? (
                              <span key={catId} className="text-xs px-2 py-1 bg-gray-700 rounded-full">
                                {category.name}
                              </span>
                            ) : null;
                          })}
                          <span className="text-xs px-2 py-1 bg-gray-700 rounded-full">
                            {tool.schemaType}
                          </span>
                        </div>
                      </div>
                      <div className="flex space-x-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleEditTool(tool)}
                          className="h-8 w-8 text-gray-400 hover:text-gray-100"
                        >
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-gray-400 hover:text-gray-100"
                        >
                          <Copy className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDeleteTool(tool.guid)}
                          className="h-8 w-8 text-gray-400 hover:text-red-400"
                          disabled={isDeleting}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="flex space-x-2 mt-4">
        <Button 
          variant="outline" 
          className="bg-gray-800 border-gray-700"
          onClick={handleImportTools}
          disabled={isImporting}
        >
          <Import className="h-4 w-4 mr-1" />
          {isImporting ? 'Importing...' : 'Import'}
        </Button>
        <Button 
          variant="outline" 
          className="bg-gray-800 border-gray-700"
          onClick={handleExportTools}
          disabled={isExporting}
        >
          <Download className="h-4 w-4 mr-1" />
          {isExporting ? 'Exporting...' : 'Export'}
        </Button>
      </div>

      <Dialog open={isEditorOpen} onOpenChange={setIsEditorOpen}>
        <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-2xl">
          <DialogHeader>
            <DialogTitle>{currentTool ? 'Edit Tool' : 'Create Tool'}</DialogTitle>
          </DialogHeader>
          <ToolEditor 
            tool={currentTool} 
            onClose={() => setIsEditorOpen(false)} 
            categories={categories} 
          />
        </DialogContent>
      </Dialog>
    </div>
  );
}