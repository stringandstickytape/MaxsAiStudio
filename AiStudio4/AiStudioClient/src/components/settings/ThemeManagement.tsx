// AiStudioClient/src/components/settings/ThemeManagement.tsx

import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { ThemeForm } from './ThemeForm';
import { UnifiedModalDialog, UnifiedModalContent, UnifiedModalHeader } from '@/components/ui/unified-modal-dialog';
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

// Define themeable properties for the ThemeManagement component
export const themeableProps = {};

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
      
      return;
    }

    
    setIsProcessing(true);
    setLocalError(null);
    try {
      
      const result = await deleteTheme(themeToDelete.guid);
      
      setDeleteOpen(false);
    } catch (err: any) {
      
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
      <div className="flex-between mb-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
        <div style={{ backgroundColor: 'var(--global-background-color)' }}>
          <h2 className="text-title" style={{ color: 'var(--global-text-color)' }}>Themes</h2>
          <div className="text-sm mt-1" style={{ color: 'var(--global-secondary-color)' }}>Current theme: <span style={{ color: 'var(--global-primary-color)' }}>{currentThemeName}</span></div>
        </div>
        <div className="flex gap-2" style={{ backgroundColor: 'var(--global-background-color)' }}>
          <Button
            variant="ghost"
            size="icon"
            onClick={handleRefresh}
            disabled={refreshing || isLoading}
            className="h-8 w-8"
            style={{
              backgroundColor: 'var(--global-background-color)',
              color: 'var(--global-text-color)'
            }}
          >
            <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
          </Button>
          <Button 
            onClick={() => setAddOpen(true)} 
            className="flex items-center gap-2"
            style={{
              backgroundColor: 'var(--global-primary-color)',
              color: '#ffffff'
            }}
          >
            <PlusCircle className="h-4 w-4" /> Add Theme
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="flex-center py-8" style={{ backgroundColor: 'var(--global-background-color)' }}>
          <div className="loading-spinner h-8 w-8"></div>
        </div>
      ) : themes.length === 0 ? (
        <Card className="card-base" style={{ backgroundColor: 'var(--global-background-color)', borderColor: 'var(--global-border-color)', borderRadius: 'var(--global-border-radius)', boxShadow: 'var(--global-box-shadow)' }}>
          <CardContent className="pt-6 text-center" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-secondary-color)' }}>
            <div className="flex flex-col flex-center py-8" style={{ backgroundColor: 'var(--global-background-color)' }}>
              <AlertCircle className="h-12 w-12 mb-4 opacity-50" style={{ color: 'var(--global-secondary-color)' }} />
              <p style={{ color: 'var(--global-text-color)' }}>No themes configured yet.</p>
              <p className="mb-4" style={{ color: 'var(--global-text-color)' }}>Add your first theme to get started.</p>
              <Button
                onClick={() => setAddOpen(true)}
                variant="outline"
                style={{
                  backgroundColor: 'var(--global-background-color)',
                  borderColor: 'var(--global-border-color)',
                  color: 'var(--global-text-color)'
                }}
              >
                Add Theme
              </Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4" style={{ backgroundColor: 'var(--global-background-color)' }}>
          {themes.map((theme) => (
            <Card
              key={theme.guid}
              className="card-base card-hover backdrop-blur-sm group flex flex-col relative overflow-hidden"
              style={{
                backgroundColor: 'var(--global-background-color)',
                color: 'var(--global-text-color)',
                borderRadius: 'var(--global-border-radius)',
                boxShadow: 'var(--global-box-shadow)',
                borderColor: 'var(--global-border-color)'
              }}
            >
              <div
                className="h-2 bg-gradient-to-r from-opacity-80 to-opacity-100 animate-hover group-hover:h-3"
                style={{
                  backgroundColor: theme.previewColors[0] || '#4f46e5',
                  from: `${theme.previewColors[0]}80` || '#4f46e580',
                  to: theme.previewColors[0] || '#4f46e5',
                }}
              />
              <div className="p-3 flex flex-col flex-1" style={{ backgroundColor: 'var(--global-background-color)' }}>
                <div className="flex items-start justify-between" style={{ backgroundColor: 'var(--global-background-color)' }}>
                  <h3 className="text-lg font-medium truncate" style={{ color: 'var(--global-text-color)' }}>{theme.name}</h3>
                  {activeThemeId === theme.guid && (
                    <span className="text-xs px-2 py-0.5 rounded-full" style={{ backgroundColor: 'var(--global-primary-color)', color: '#ffffff', borderRadius: 'var(--global-border-radius)' }}>Active</span>
                  )}
                </div>

                {theme.author && (
                  <div className="text-xs mt-1" style={{ color: 'var(--global-secondary-color)' }}>by {theme.author}</div>
                )}

                <div className="text-sm mt-2 line-clamp-2" style={{ color: 'var(--global-text-color)' }}>{theme.description}</div>

                <div className="mt-auto" style={{ backgroundColor: 'var(--global-background-color)' }}>
                  {theme.previewColors && theme.previewColors.length > 0 && renderColorSwatches(theme.previewColors)}
                  <Button
                    variant="outline"
                    size="sm"
                    className="w-full mt-2"
                    onClick={() => handleActivateTheme(theme.guid)}
                    disabled={isProcessing || activeThemeId === theme.guid}
                    style={{
                      backgroundColor: activeThemeId === theme.guid ? 'var(--global-primary-color)' : 'var(--global-background-color)',
                      borderColor: 'var(--global-border-color)',
                      color: activeThemeId === theme.guid ? '#ffffff' : 'var(--global-text-color)'
                    }}
                  >
                    {activeThemeId === theme.guid ? 'Current Theme' : 'Apply Theme'}
                  </Button>
                </div>

                {/* Vertical stacked buttons in the bottom-right corner */}
                <div className="absolute bottom-1 right-1 flex flex-col space-y-1" style={{ backgroundColor: 'transparent' }}>
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="animate-hover h-6 w-6 p-0"
                          onClick={() => handleActivateTheme(theme.guid)}
                          disabled={isProcessing}
                          style={{
                            backgroundColor: 'transparent',
                            color: activeThemeId === theme.guid ? 'var(--global-primary-color)' : 'var(--global-secondary-color)'
                          }}
                        >
                          <Star className={`h-3.5 w-3.5 ${activeThemeId === theme.guid ? 'fill-current' : ''}`} />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="text-xs" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)', borderColor: 'var(--global-border-color)' }}>
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
                          className="h-6 w-6 p-0"
                          onClick={() => {
                            setEditingTheme(theme);
                            setEditOpen(true);
                          }}
                          disabled={isProcessing}
                          style={{
                            backgroundColor: 'transparent',
                            color: 'var(--global-text-color)'
                          }}
                        >
                          <Pencil className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="text-xs" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)', borderColor: 'var(--global-border-color)' }}>
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
                          className="h-6 w-6 p-0"
                          onClick={() => {
                            setThemeToDelete(theme);
                            setDeleteOpen(true);
                          }}
                          disabled={isProcessing}
                          style={{
                            backgroundColor: 'transparent',
                            color: 'var(--global-destructive-color, var(--global-primary-color))'
                          }}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="left" className="text-xs" style={{ backgroundColor: 'var(--global-background-color)', color: 'var(--global-text-color)', borderColor: 'var(--global-border-color)' }}>
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
      <UnifiedModalDialog open={addOpen} onOpenChange={setAddOpen} size="xl" variant="form" style={{ backgroundColor: 'var(--global-background-color)' }}>
        <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
          <h2 className="text-lg font-semibold" style={{ color: 'var(--global-text-color)' }}>Add New Theme</h2>
        </UnifiedModalHeader>
        <UnifiedModalContent style={{ backgroundColor: 'var(--global-background-color)' }}>
          {displayError && (
            <div className="p-3 rounded-md mb-4" style={{ backgroundColor: 'var(--global-destructive-color-translucent, rgba(220, 38, 38, 0.1))', color: 'var(--global-destructive-color, var(--global-primary-color))', borderColor: 'var(--global-destructive-color-muted, rgba(220, 38, 38, 0.3))' }}>
              {displayError}
            </div>
          )}
          <ThemeForm onSubmit={handleAddTheme} isProcessing={isProcessing} />
        </UnifiedModalContent>
      </UnifiedModalDialog>

      {/* Edit Theme Dialog */}
      <UnifiedModalDialog open={editOpen} onOpenChange={setEditOpen} size="xl" variant="form" style={{ backgroundColor: 'var(--global-background-color)' }}>
        <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
          <h2 className="text-lg font-semibold" style={{ color: 'var(--global-text-color)' }}>Edit Theme</h2>
        </UnifiedModalHeader>
        <UnifiedModalContent style={{ backgroundColor: 'var(--global-background-color)' }}>
          {displayError && (
            <div className="p-3 rounded-md mb-4" style={{ backgroundColor: 'var(--global-destructive-color-translucent, rgba(220, 38, 38, 0.1))', color: 'var(--global-destructive-color, var(--global-primary-color))', borderColor: 'var(--global-destructive-color-muted, rgba(220, 38, 38, 0.3))' }}>
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
        </UnifiedModalContent>
      </UnifiedModalDialog>

      {/* Delete Theme Dialog */}
      <UnifiedModalDialog open={deleteOpen} onOpenChange={setDeleteOpen} size="md" variant="confirmation" style={{ backgroundColor: 'var(--global-background-color)' }}>
        <UnifiedModalHeader style={{ backgroundColor: 'var(--global-background-color)' }}>
          <h2 className="text-lg font-semibold" style={{ color: 'var(--global-text-color)' }}>Confirm Deletion</h2>
        </UnifiedModalHeader>
        <UnifiedModalContent style={{ backgroundColor: 'var(--global-background-color)' }}>
          <div className="py-4" style={{ color: 'var(--global-text-color)' }}>
            Are you sure you want to delete the theme <strong>{themeToDelete?.name}</strong>? This action cannot
            be undone.
          </div>
          {displayError && (
            <div className="p-3 rounded-md mb-4" style={{ backgroundColor: 'var(--global-destructive-color-translucent, rgba(220, 38, 38, 0.1))', color: 'var(--global-destructive-color, var(--global-primary-color))', borderColor: 'var(--global-destructive-color-muted, rgba(220, 38, 38, 0.3))' }}>
              {displayError}
            </div>
          )}
          <div className="flex justify-end space-x-2">
            <Button
              variant="outline"
              onClick={() => setDeleteOpen(false)}
              disabled={isProcessing}
              style={{
                backgroundColor: 'var(--global-background-color)',
                borderColor: 'var(--global-border-color)',
                color: 'var(--global-text-color)'
              }}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteThemeConfirm}
              disabled={isProcessing}
              style={{
                backgroundColor: 'var(--global-destructive-color, var(--global-primary-color))',
                color: '#ffffff'
              }}
            >
              {isProcessing ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </UnifiedModalContent>
      </UnifiedModalDialog>
    </>
  );
};