﻿import { cn } from '@/lib/utils';
import { useState, useEffect } from 'react';
import { useToolStore } from '@/stores/useToolStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
// Removed Dialog import
import { Plus, Search, Edit, Trash2, Copy, Download, X, Check, CheckSquare, Square, PlusCircle } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// Removed ToolEditor import as it's now used in ToolEditorModal
import { Tool } from '@/types/toolTypes';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useModalStore } from '@/stores/useModalStore';
import { useConfirmationDialog } from '@/hooks/useConfirmationDialog';

interface ToolPanelProps {
  isOpen?: boolean;
  onClose?: () => void;
  isModal?: boolean;
  onToolSelect?: (toolId: string) => void;
}

export function ToolPanel({ isOpen = true, isModal = true, onClose, onToolSelect }: ToolPanelProps) {
  
  const {
    tools,
    categories,
    isLoading: toolsLoading,
    fetchTools,
    fetchToolCategories,
    deleteTool,
    exportTools: exportToolsFn,
    clearActiveTools, // Added
  } = useToolsManagement();

  
  const { setTools, setCategories, activeTools, addActiveTool, removeActiveTool, setActiveTools } = useToolStore(); // Added setActiveTools here

  
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

  
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const { openModal } = useModalStore();
  const { confirm } = useConfirmationDialog();

  useEffect(() => {
    if (isOpen) {
      fetchTools();
      fetchToolCategories();
    }
  }, [isOpen, fetchTools, fetchToolCategories]);

  
  useEffect(() => {
    const pendingAction = window.localStorage.getItem('toolPanel_action');
    if (pendingAction) {
      if (pendingAction === 'create') {
        handleAddTool();
      } else if (pendingAction === 'export') {
        
        handleExportTools();
      }
      
      window.localStorage.removeItem('toolPanel_action');
    }
  }, [isOpen]);

  const handleAddTool = () => {
    openModal('toolEditor', {
      tool: null,
      categories,
      onClose: () => {}
    });
  };

  const handleEditTool = (tool: Tool) => {
    openModal('toolEditor', {
      tool,
      categories,
      onClose: () => {}
    });
  };

  const handleDeleteTool = async (toolId: string) => {
    const confirmed = await confirm({
      title: 'Delete Tool',
      description: 'Are you sure you want to delete this tool? This action cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      danger: true
    });
    
    if (confirmed) {
      try {
        setIsDeleting(true);
        await deleteTool(toolId);
      } catch (error) {
        console.error('Failed to delete tool:', error);
        alert(`Failed to delete tool: ${error instanceof Error ? error.message : 'Unknown error'}`);
      } finally {
        setIsDeleting(false);
      }
    }
  };

  const handleExportTools = async () => {
    try {
      setIsExporting(true);
      const json = await exportToolsFn();
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
    } finally {
      setIsExporting(false);
    }
  };

  const filteredTools = tools.filter((tool) => {
    const matchesSearch =
      searchTerm === '' ||
      tool.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      tool.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (tool.filetype && tool.filetype.toLowerCase().includes(searchTerm.toLowerCase()));

    const matchesCategory = selectedCategory === null || tool.categories.includes(selectedCategory);

    return matchesSearch && matchesCategory;
  });

  
  const handleSelectTool = (toolId: string) => {
    const isActive = activeTools.includes(toolId);
    
    if (isActive) {
      removeActiveTool(toolId);
    } else {
      addActiveTool(toolId);
    }
    
    
    if (onToolSelect) {
      onToolSelect(toolId);
    }
  };

    const handleSelectAll = () => {
        const filteredToolIds = filteredTools.map(tool => tool.guid);
        setActiveTools(filteredToolIds);
    };
    const handleSelectNone = () => {
        clearActiveTools();
    };

    // New handler for double-clicking category buttons
    const handleCategoryDoubleClick = (categoryId: string | null) => {
      let toolsToSelect: Tool[] = [];
      if (categoryId === null) {
        // Select all tools if "All Tools" is double-clicked
        toolsToSelect = tools;
      } else {
        // Select tools belonging to the specific category
        toolsToSelect = tools.filter(tool => tool.categories.includes(categoryId));
      }
      const toolIdsToSelect = toolsToSelect.map(tool => tool.guid);
      setActiveTools(toolIdsToSelect);
      onClose?.(); // Close the dialog
    };


  const isLoading = toolsLoading || isDeleting || isExporting;

  return (
    <div className="p-4 overflow-y-auto h-full bg-gray-900 text-gray-100"> {/* Removed pt-12 */}
      <div className="flex justify-between items-center mb-4">
        <div className="flex items-center gap-4">
          {/* Removed Close button from here */}
          <h2 className="text-title">Tool Library</h2>
        </div>
        <div className="flex space-x-2 pr-16"> {/* Added pr-16 */}
          {/* Moved Buttons Start */}
          <Button
            variant="outline"
            size="sm"
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
            onClick={handleSelectAll}
            disabled={isLoading || filteredTools.length === 0 || filteredTools.length === activeTools.length}
          >
            Select All Visible
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
            onClick={handleSelectNone}
            disabled={isLoading || activeTools.length === 0}
          >
            Select None
          </Button>
          <Button 
            variant="outline" 
            size="sm"
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
            onClick={handleExportTools} 
            disabled={isExporting}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export'}
          </Button>
          {/* Moved Buttons End */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleAddTool}
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
          >
            <PlusCircle className="h-4 w-4 mr-1" />
            <span>New Tool</span>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
        <div className="md:col-span-1">
          <div className="text-sm text-gray-400 mb-2">
              <span className="mr-2">Currently selected: {activeTools.length} tool{activeTools.length !== 1 ? 's' : ''}</span>
            </div>
          <Card className="card-base">
            <CardHeader>
              <CardTitle className="text-sm font-medium text-gray-300">Categories</CardTitle>
              <p className="text-xs text-gray-400 mt-1">(Double-click to select all)</p> 
            </CardHeader>
            <CardContent className="p-2">
              <div className="space-y-1">
                <Button
                  variant="outline"
                  size="sm"
                  className={cn(
                    "w-full justify-start",
                    selectedCategory === null 
                      ? "bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 text-white" 
                      : "bg-blue-900/20 hover:bg-blue-500/30 border-blue-800/30 text-gray-300 hover:text-white"
                  )}
                  onClick={() => setSelectedCategory(null)}
                  onDoubleClick={() => handleCategoryDoubleClick(null)} // Add double-click handler
                >
                  All Tools
                </Button>
                {categories.map((category) => (
                    <Button
                        key={category.id}
                        variant="outline"
                        size="sm"
                        className={cn(
                          "w-full justify-start", 
                          selectedCategory === category.id 
                            ? "bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 text-white" 
                            : "bg-blue-900/20 hover:bg-blue-500/30 border-blue-800/30 text-gray-300 hover:text-white"
                          )}
                        onClick={() => setSelectedCategory(category.id)}
                        onDoubleClick={() => handleCategoryDoubleClick(category.id)} // Add double-click handler
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
                className="pl-8 input-base"
              />
            </div>
          </div>

          {isLoading ? (
            <div className="flex-center h-32">
              <div className="loading-spinner h-8 w-8"></div>
            </div>
          ) : filteredTools.length === 0 ? (
            <div className="text-center p-8 text-subtitle">
              {searchTerm ? 'No tools match your search' : 'No tools available'}
            </div>
          ) : (
            <div className="space-y-3">
              {filteredTools.map((tool) => {
                const isActive = activeTools.includes(tool.guid);
                return (
                <Card 
                  key={tool.guid} 
                  className={`card-base ${isActive ? 'border-blue-500 border-2' : ''}`}
                >
                  <CardContent className="p-4">
                    <div className="flex justify-between items-start">
                      <div>
                        <div className="flex items-center gap-2">
                          <h3 className="text-title text-lg">{tool.name}</h3>
                          {tool.isBuiltIn && (
                            <span className="text-xs px-2 py-1 bg-blue-700 rounded-full">Built-in</span>
                          )}
                        </div>
                        <p className="text-body">{tool.description}</p>
                        <div className="flex flex-wrap gap-1 mt-2">
                          {(tool.categories || []).map((catId) => {
                            const category = categories.find((c) => c.id === catId);
                            return category ? (
                              <span key={catId} className="text-xs px-2 py-1 bg-gray-700 rounded-full">
                                {category.name}
                              </span>
                            ) : null;
                          })}
                          <span className="text-xs px-2 py-1 bg-gray-700 rounded-full">{tool.schemaType}</span>
                          {tool.filetype && (
                            <span className="text-xs px-2 py-1 bg-gray-700 rounded-full">
                              filetype: {tool.filetype}
                            </span>
                          )}
                        </div>
                      </div>
                      <div className="flex flex-col gap-2">
                        <div className="flex space-x-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleEditTool(tool)}
                            className={`h-8 w-8 ${tool.isBuiltIn ? 'text-blue-400 hover:text-blue-600' : 'text-gray-400 hover:text-gray-100'}`}
                            title={tool.isBuiltIn ? 'Edit extra properties for built-in tools' : 'Edit Tool'}
                            // No longer disabled for built-in tools
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button 
                            variant="ghost" 
                            size="icon" 
                            className="h-8 w-8 text-gray-400 hover:text-gray-100"
                            title="Copy Tool"
                          >
                            <Copy className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleDeleteTool(tool.guid)}
                            className={`h-8 w-8 ${tool.isBuiltIn ? 'text-gray-600 cursor-not-allowed' : 'text-gray-400 hover:text-red-400'}`}
                            disabled={isDeleting || tool.isBuiltIn}
                            title={tool.isBuiltIn ? 'Built-in tools cannot be deleted' : 'Delete Tool'}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                        <Button
                          variant={isActive ? "default" : "outline"}
                          size="sm"
                          onClick={() => handleSelectTool(tool.guid)}
                          className={`w-full ${isActive ? 
                            'bg-blue-600 hover:bg-blue-700 text-white' : 
                            'bg-gray-700 hover:bg-gray-600 border-gray-600'
                          }`}
                        >
                          {isActive ? (
                            <>
                              <Check className="h-4 w-4 mr-1" />
                              <span>Selected</span>
                            </>
                          ) : (
                            <>
                              <Plus className="h-4 w-4 mr-1" />
                              <span>Select Tool</span>
                            </>
                          )}
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )})}
            </div>
          )}
        </div>
      </div>

      <div className="flex justify-between items-center mt-4">
        {/* Removed 'Currently selected' label from here */}
        {/* Buttons moved to the top */} 
      {/* Tool Editor Dialog removed - now handled by ToolEditorModal */}
          </div>
    </div>
  );
}