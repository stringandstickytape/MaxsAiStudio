// AiStudio4.Web/src/components/settings/ThemeManagement.tsx

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ThemeForm } from './ThemeForm';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Pencil, Trash2, Star, PlusCircle, AlertCircle, RefreshCw } from 'lucide-react';
import { Theme } from '@/types/theme';
import { useThemeManagement } from '@/hooks/useThemeManagement';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface ThemeManagementProps {
  themeToEdit?: Theme | null;
  setThemeToEdit?: React.Dispatch<React.SetStateAction<Theme | null>>;
  editDialogOpen?: boolean;
  setEditDialogOpen?: React.Dispatch<React.SetStateAction<boolean>>;
}

export const ThemeManagement: React.FC<ThemeManagementProps> = ({
  themeToEdit: externalThemeToEdit,
  setThemeToEdit: externalSetThemeToEdit,
  editDialogOpen: externalEditOpen,
  setEditDialogOpen: externalSetEditOpen,
}) => {
  const {
    themes,
    activeThemeId,
    isLoading,
    error: storeError,
    createTheme,
    updateTheme,
    deleteTheme,
    activateTheme,
    applyTheme,
    refreshThemes,
    clearError
  } = useThemeManagement();

  // Handle internal/external state for editing
  const [internalEditingTheme, setInternalEditingTheme] = useState<Theme | null>(null);
  const [internalEditOpen, setInternalEditOpen] = useState(false);
  const [currentThemeName, setCurrentThemeName] = useState<string>('Default');

  // Use external state if provided, otherwise use internal state
  const editingTheme = externalThemeToEdit !== undefined ? externalThemeToEdit : internalEditingTheme;
  const setEditingTheme = externalSetThemeToEdit || setInternalEditingTheme;
  const editOpen = externalEditOpen !== undefined ? externalEditOpen : internalEditOpen;
  const setEditOpen = externalSetEditOpen || setInternalEditOpen;

  const [addOpen, setAddOpen] = useState(false);
  const [themeToDelete, setThemeToDelete] = useState<Theme | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [error, setLocalError] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  // Combined error from local state or store
  const displayError = error || storeError;

  // Get current theme name from ThemeManager
  useEffect(() => {
    if (window.getCurrentThemeName) {
      setCurrentThemeName(window.getCurrentThemeName());
    }
  }, []);

  // Listen for theme changes
  useEffect(() => {
    const handleThemeChange = (event: CustomEvent) => {
      if (event.detail && event.detail.themeName) {
        setCurrentThemeName(event.detail.themeName);
      }
    };

    window.addEventListener('themechange', handleThemeChange as EventListener);
    return () => {
      window.removeEventListener('themechange', handleThemeChange as EventListener);
    };
  }, []);

  // Clear errors when dialogs close
  useEffect(() => {
    if (!addOpen && !editOpen && !deleteOpen) {
      setLocalError(null);
      clearError();
    }
  }, [addOpen, editOpen, deleteOpen, clearError]);

  const handleAddTheme = async (themeData: Omit<Theme, 'guid'>) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await createTheme(themeData);
      setAddOpen(false);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to add theme');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleUpdateTheme = async (themeData: Theme) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await updateTheme(themeData);
      setEditOpen(false);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to update theme');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteThemeConfirm = async () => {
    if (!themeToDelete) {
      console.log('[ThemeManagement] No theme to delete');
      return;
    }

    console.log('[ThemeManagement] Deleting theme:', themeToDelete.guid, themeToDelete.name);
    setIsProcessing(true);
    setLocalError(null);
    try {
      console.log('[ThemeManagement] Calling deleteTheme with ID:', themeToDelete.guid);
      const result = await deleteTheme(themeToDelete.guid);
      console.log('[ThemeManagement] Delete theme result:', result);
      setDeleteOpen(false);
    } catch (err: any) {
      console.error('[ThemeManagement] Error deleting theme:', err);
      setLocalError(err?.message || 'Failed to delete theme');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleActivateTheme = async (themeId: string) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      await activateTheme(themeId);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to activate theme');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleRefresh = async () => {
    setRefreshing(true);
    try {
      await refreshThemes();
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to refresh themes');
    } finally {
      setRefreshing(false);
    }
  };

  const handleApplyTheme = async (themeId: string) => {
    setIsProcessing(true);
    setLocalError(null);
    try {
      applyTheme(themeId);
    } catch (err: any) {
      setLocalError(err?.message || 'Failed to apply theme');
    } finally {
      setIsProcessing(false);
    }
  };

  // Function to render color preview swatches
  const renderColorSwatches = (colors: string[]) => {
    return (
      <div className="flex space-x-1 mt-2">
        {colors.map((color, index) => (
          <div
            key={index}
            className="w-4 h-4 rounded-full border border-gray-700"
            style={{ backgroundColor: color }}
          />
        ))}
      </div>
    );
  };

  return (
    <>
      <div className="flex-between mb-4">
        <div>
          <h2 className="text-title">Themes</h2>
          <div className="text-sm text-gray-400 mt-1">Current theme: <span className="text-blue-400">{currentThemeName}</span></div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={handleRefresh}
            disabled={refreshing || isLoading}
            className="h-8 w-8 text-gray-400 hover:text-gray-100"
          >
            <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
          </Button>
          <Button onClick={() => setAddOpen(true)} className="flex items-center gap-2 btn-primary">
            <PlusCircle className="h-4 w-4" /> Add Theme
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex-center py-8">
          <div className="loading-spinner h-8 w-8"></div>
        </div>
      ) : themes.length === 0 ? (
        <Card className="card-base">
          <CardContent className="pt-6 text-center text-gray-400">
            <div className="flex flex-col flex-center py-8">
              <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
              <p>No themes configured yet.</p>
              <p className="mb-4">Add your first theme to get started.</p>
              <Button
                onClick={() => setAddOpen(true)}
                variant="outline"
                className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
              >
                Add Theme
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {themes.map((theme) => (
            <Card
              key={theme.guid}
              className="card-base card-hover backdrop-blur-sm group flex flex-col relative overflow-hidden"
            >
              <div
                className="h-2 bg-gradient-to-r from-opacity-80 to-opacity-100 animate-hover group-hover:h-3"
                style={{
                  backgroundColor: theme.previewColors[0] || '#4f46e5',
                  from: `${theme.previewColors[0]}80` || '#4f46e580',
                  to: theme.previewColors[0] || '#4f46e5',
                }}
              />
              <div className="p-3 flex flex-col flex-1">
                <div className="flex items-start justify-between">
                  <h3 className="text-gray-100 text-lg font-medium truncate">{theme.name}</h3>
                  {activeThemeId === theme.guid && (
                    <span className="text-xs bg-blue-500/20 text-blue-300 px-2 py-0.5 rounded-full">Active</span>
                  )}
                </div>

                {theme.author && (
                  <div className="text-gray-400 text-xs mt-1">by {theme.author}</div>
                )}

                <div className="text-gray-300 text-sm mt-2 line-clamp-2">{theme.description}</div>

                <div className="mt-auto">
                  {theme.previewColors && theme.previewColors.length > 0 && renderColorSwatches(theme.previewColors)}
                  <Button
                    variant="outline"
                    size="sm"
                    className="w-full mt-2"
                    onClick={() => handleActivateTheme(theme.guid)}
                    disabled={isProcessing || activeThemeId === theme.guid}
                  >
                    {activeThemeId === theme.guid ? 'Current Theme' : 'Apply Theme'}
                  </Button>
                </div>

                {/* Vertical stacked buttons in the bottom-right corner */}
                <div className="absolute bottom-1 right-1 flex flex-col space-y-1">
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className={`${activeThemeId === theme.guid ? 'text-blue-400 hover:text-blue-300' : 'text-gray-400 hover:text-blue-400'} hover:bg-gray-700 animate-hover h-6 w-6 p-0`}
                          onClick={() => handleActivateTheme(theme.guid)}
                          disabled={isProcessing}
                        >
                          <Star className={`h-3.5 w-3.5 ${activeThemeId === theme.guid ? 'fill-blue-400' : ''}`} />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        {activeThemeId === theme.guid ? 'Current active theme' : 'Activate theme'}
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>

                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="btn-ghost icon-btn h-6 w-6 p-0"
                          onClick={() => {
                            setEditingTheme(theme);
                            setEditOpen(true);
                          }}
                          disabled={isProcessing}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        Edit theme
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>

                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="btn-danger icon-btn h-6 w-6 p-0"
                          onClick={() => {
                            setThemeToDelete(theme);
                            setDeleteOpen(true);
                          }}
                          disabled={isProcessing}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="bg-gray-900 text-gray-100 text-xs border-gray-700">
                        Delete theme
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Add Theme Dialog */}
      <Dialog open={addOpen} onOpenChange={setAddOpen}>
        <DialogContent className="max-w-xl bg-gray-800 border-gray-700 text-gray-100" description="Add a new theme">
          <DialogHeader>
            <DialogTitle className="text-gray-100">Add New Theme</DialogTitle>
          </DialogHeader>
          {displayError && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {displayError}
            </div>
          )}
          <ThemeForm onSubmit={handleAddTheme} isProcessing={isProcessing} />
        </DialogContent>
      </Dialog>

      {/* Edit Theme Dialog */}
      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent
          className="max-w-xl bg-gray-800 border-gray-700 text-gray-100"
          description="Edit an existing theme"
        >
          <DialogHeader>
            <DialogTitle className="text-gray-100">Edit Theme</DialogTitle>
          </DialogHeader>
          {displayError && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {displayError}
            </div>
          )}
          {editingTheme && (
            <ThemeForm
              key={`edit-theme-form-${editingTheme.guid}`}
              onSubmit={handleUpdateTheme}
              isProcessing={isProcessing}
              initialValues={editingTheme}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete Theme Dialog */}
      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent
          className="bg-gray-800 border-gray-700 text-gray-100"
          description="Confirm deletion of a theme"
        >
          <DialogHeader>
            <DialogTitle className="text-gray-100">Confirm Deletion</DialogTitle>
          </DialogHeader>
          <div className="py-4 text-gray-200">
            Are you sure you want to delete the theme <strong>{themeToDelete?.name}</strong>? This action cannot
            be undone.
          </div>
          {displayError && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {displayError}
            </div>
          )}
          <div className="flex justify-end space-x-2">
            <Button
              variant="outline"
              onClick={() => setDeleteOpen(false)}
              disabled={isProcessing}
              className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteThemeConfirm}
              disabled={isProcessing}
              className="bg-red-700 hover:bg-red-800 text-white border-red-900"
            >
              {isProcessing ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
};