import { cn } from '@/lib/utils';
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
import { useModelManagement } from '@/hooks/useResourceManagement';
import { useModalStore } from '@/stores/useModalStore';
import { useConfirmationDialog } from '@/hooks/useConfirmationDialog';

interface ToolPanelProps {
  isOpen?: boolean;
  onClose?: () => void;
  isModal?: boolean;
  onToolSelect?: (toolId: string) => void;
}

// Define themeable properties for the ToolPanel component
export const themeableProps = {};

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
  
  const { models } = useModelManagement();

  
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
  const { openModal, openNestedModal } = useModalStore();
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
    openNestedModal('toolEditor', {
      tool: null,
      categories,
      models,
      onClose: () => {}
    });
  };

  const handleEditTool = (tool: Tool) => {
    openNestedModal('toolEditor', {
      tool,
      categories,
      models,
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
    <div className="p-4" style={{
      backgroundColor: 'var(--global-background-color)',
      color: 'var(--global-text-color)',
      fontFamily: 'var(--global-font-family)',
      fontSize: 'var(--global-font-size)',
      // Removed overflow handling as it's now handled by the parent container
    }}>
      <div className="flex justify-between items-center mb-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
        <div className="flex items-center gap-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
          {/* Removed Close button from here */}
          <h2 className="text-title" style={{ color: 'var(--global-text-color)' }}>Tool Library</h2>
        </div>
        <div className="flex space-x-2 pr-16" style={{ backgroundColor: 'var(--global-background-color)' }}>
          {/* Moved Buttons Start */}
          <Button
            variant="outline"
            size="sm"
            className="btn-primary flex items-center space-x-1"
            onClick={handleSelectAll}
            disabled={isLoading || filteredTools.length === 0 || filteredTools.length === activeTools.length}
            style={{
              backgroundColor: 'var(--global-background-color)',
              borderColor: 'var(--global-border-color)',
              color: 'var(--global-text-color)'
            }}
          >
            Select All Visible
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="btn-primary flex items-center space-x-1"
            onClick={handleSelectNone}
            disabled={isLoading || activeTools.length === 0}
            style={{
              backgroundColor: 'var(--global-background-color)',
              borderColor: 'var(--global-border-color)',
              color: 'var(--global-text-color)'
            }}
          >
            Select None
          </Button>
          <Button 
            variant="outline" 
            size="sm"
            className="btn-primary flex items-center space-x-1"
            onClick={handleExportTools} 
            disabled={isExporting}
            style={{
              backgroundColor: 'var(--global-background-color)',
              borderColor: 'var(--global-border-color)',
              color: 'var(--global-text-color)'
            }}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export'}
          </Button>
          {/* Moved Buttons End */}
          <Button
            variant="outline"
            size="sm"
            onClick={handleAddTool}
            className="btn-primary flex items-center space-x-1"
            style={{
              backgroundColor: 'var(--global-primary-color)',
              color: '#ffffff'
            }}
          >
            <PlusCircle className="h-4 w-4 mr-1" />
            <span>New Tool</span>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
        <div className="md:col-span-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
          <div className="text-sm mb-2" style={{ color: 'var(--global-secondary-color)' }}>
              <span className="mr-2">Currently selected: {activeTools.length} tool{activeTools.length !== 1 ? 's' : ''}</span>
            </div>
          <Card className="card-base" style={{ backgroundColor: 'var(--global-background-color)', borderColor: 'var(--global-border-color)', borderRadius: 'var(--global-border-radius)', boxShadow: 'var(--global-box-shadow)' }}>
            <CardHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
              <CardTitle className="text-sm font-medium" style={{ color: 'var(--global-text-color)' }}>Categories</CardTitle>
              <p className="text-xs mt-1" style={{ color: 'var(--global-secondary-color)' }}>(Double-click to select entire category)</p> 
            </CardHeader>
            <CardContent className="p-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <div className="space-y-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full justify-start"
                  onClick={() => setSelectedCategory(null)}
                  onDoubleClick={() => handleCategoryDoubleClick(null)} // Add double-click handler
                  style={{
                    backgroundColor: selectedCategory === null ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                    borderColor: 'var(--global-border-color)',
                    color: selectedCategory === null ? '#ffffff' : 'var(--global-text-color)'
                  }}
                >
                  All Tools
                </Button>
                {categories.map((category) => (
                    <Button
                        key={category.id}
                        variant="outline"
                        size="sm"
                        className="w-full justify-start"
                        onClick={() => setSelectedCategory(category.id)}
                        onDoubleClick={() => handleCategoryDoubleClick(category.id)} // Add double-click handler
                        style={{
                          backgroundColor: selectedCategory === category.id ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                          borderColor: 'var(--global-border-color)',
                          color: selectedCategory === category.id ? '#ffffff' : 'var(--global-text-color)'
                        }}
                    >
                        {category.name}
                    </Button>
                ))}
                          </div>
                      </CardContent>
                  </Card>
        </div>

        <div className="md:col-span-3" style={{ backgroundColor: 'var(--global-background-color)' }}>
          <div className="mb-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
            <div className="relative" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <Search className="absolute left-2 top-2.5 h-4 w-4" style={{ color: 'var(--global-secondary-color)' }} />
              <Input
                placeholder="Search tools..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-8 input-base"
                style={{
                  backgroundColor: 'var(--global-background-color)',
                  borderColor: 'var(--global-border-color)',
                  color: 'var(--global-text-color)'
                }}
              />
            </div>
          </div>

          {isLoading ? (
            <div className="flex-center h-32" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <div className="loading-spinner h-8 w-8"></div>
            </div>
          ) : filteredTools.length === 0 ? (
            <div className="text-center p-8 text-subtitle" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-secondary-color)' }}>
              {searchTerm ? 'No tools match your search' : 'No tools available'}
            </div>
          ) : (
            <div className="space-y-3" style={{ backgroundColor: 'var(--global-background-color)' }}>
              {filteredTools.map((tool) => {
                const isActive = activeTools.includes(tool.guid);
                return (
                <Card 
                  key={tool.guid} 
                  className="card-base"
                  style={{
                    backgroundColor: 'var(--global-background-color)',
                    color: 'var(--global-text-color)',
                    borderRadius: 'var(--global-border-radius)',
                    boxShadow: 'var(--global-box-shadow)',
                    border: isActive ? `2px solid var(--global-primary-color)` : `1px solid var(--global-border-color)`
                  }}
                >
                  <CardContent className="p-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
                    <div className="flex justify-between items-start" style={{ backgroundColor: 'var(--global-background-color)' }}>
                      <div style={{ backgroundColor: 'var(--global-background-color)' }}>
                        <div className="flex items-center gap-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                          <h3 className="text-title text-lg" style={{ color: 'var(--global-text-color)' }}>{tool.name}</h3>
                          {tool.isBuiltIn && (
                            <span className="text-xs px-2 py-1 rounded-full" style={{ backgroundColor: 'var(--global-primary-color)', color: '#ffffff', borderRadius: 'var(--global-border-radius)' }}>Built-in</span>
                          )}
                        </div>
                        <p className="text-body" style={{ color: 'var(--global-text-color)' }}>{tool.description}</p>
                        <div className="flex flex-wrap gap-1 mt-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                          {(tool.categories || []).map((catId) => {
                            const category = categories.find((c) => c.id === catId);
                            return category ? (
                              <span key={catId} className="text-xs px-2 py-1 rounded-full" style={{ backgroundColor: 'var(--global-secondary-color)', color: '#ffffff', borderRadius: 'var(--global-border-radius)' }}>
                                {category.name}
                              </span>
                            ) : null;
                          })}
                          <span className="text-xs px-2 py-1 rounded-full" style={{ backgroundColor: 'var(--global-background-color)', borderColor: 'var(--global-border-color)', color: 'var(--global-text-color)', borderRadius: 'var(--global-border-radius)' }}>{tool.schemaType}</span>
                          {tool.filetype && (
                            <span className="text-xs px-2 py-1 rounded-full" style={{ backgroundColor: 'var(--global-background-color)', borderColor: 'var(--global-border-color)', color: 'var(--global-text-color)', borderRadius: 'var(--global-border-radius)' }}>
                              filetype: {tool.filetype}
                            </span>
                          )}
                        </div>
                      </div>
                      <div className="flex flex-col gap-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
                        <div className="flex space-x-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleEditTool(tool)}
                            className="h-8 w-8"
                            title={tool.isBuiltIn ? 'Edit extra properties for built-in tools' : 'Edit Tool'}
                            style={{
                              backgroundColor: 'var(--global-background-color)',
                              color: 'var(--global-text-color)'
                            }}
                            // No longer disabled for built-in tools
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button 
                            variant="ghost" 
                            size="icon" 
                            className="h-8 w-8"
                            title="Copy Tool"
                            style={{
                              backgroundColor: 'var(--global-background-color)',
                              color: 'var(--global-text-color)'
                            }}
                          >
                            <Copy className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => handleDeleteTool(tool.guid)}
                            className="h-8 w-8"
                            disabled={isDeleting || tool.isBuiltIn}
                            title={tool.isBuiltIn ? 'Built-in tools cannot be deleted' : 'Delete Tool'}
                            style={{
                              backgroundColor: 'var(--global-background-color)',
                              color: tool.isBuiltIn ? 'var(--global-secondary-color)' : 'var(--global-destructive-color, var(--global-primary-color))',
                              cursor: tool.isBuiltIn ? 'not-allowed' : 'pointer'
                            }}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                        <Button
                          variant={isActive ? "default" : "outline"}
                          size="sm"
                          onClick={() => handleSelectTool(tool.guid)}
                          className="w-full"
                          style={{
                            backgroundColor: isActive ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                            borderColor: 'var(--global-border-color)',
                            color: isActive ? '#ffffff' : 'var(--global-text-color)'
                          }}
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

      <div className="flex justify-between items-center mt-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
        {/* Removed 'Currently selected' label from here */}
        {/* Buttons moved to the top */} 
      {/* Tool Editor Dialog removed - now handled by ToolEditorModal */}
          </div>
    </div>
  );
}