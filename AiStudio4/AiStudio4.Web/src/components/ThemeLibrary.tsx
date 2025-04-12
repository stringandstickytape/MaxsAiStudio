// AiStudio4.Web/src/components/ThemeLibrary.tsx

import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter } from '@/components/ui/dialog';
import { Card, CardContent, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useThemeStore } from '@/stores/useThemeStore';
import { useThemeManagement } from '@/hooks/useThemeManagement';
import { Theme } from '@/types/theme';
import { AlertCircle, Edit2, RefreshCw, Trash2 } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface ThemeLibraryProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

interface ThemeNameEditDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  theme: Theme | null;
  onSave: (themeId: string, name: string) => void;
}

interface DeleteConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  theme: Theme | null;
  onConfirm: (themeId: string) => void;
}

const DeleteConfirmationDialog: React.FC<DeleteConfirmationDialogProps> = ({
  open,
  onOpenChange,
  theme,
  onConfirm
}) => {
  const handleConfirm = () => {
    if (theme) {
      onConfirm(theme.guid);
      onOpenChange(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-gray-800 border-gray-700 text-gray-100">
        <DialogHeader>
          <DialogTitle className="text-gray-100">Delete Theme</DialogTitle>
          <DialogDescription className="text-gray-400">
            Are you sure you want to delete the theme "{theme?.name}"? This action cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter className="flex justify-end space-x-2 mt-4">
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button variant="destructive" onClick={handleConfirm}>Delete</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

const ThemeNameEditDialog: React.FC<ThemeNameEditDialogProps> = ({ 
  open, 
  onOpenChange, 
  theme, 
  onSave 
}) => {
  const [name, setName] = useState(theme?.name || '');

  React.useEffect(() => {
    if (theme) {
      setName(theme.name);
    }
  }, [theme]);

  const handleSave = () => {
    if (theme && name.trim()) {
      onSave(theme.guid, name.trim());
      onOpenChange(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-gray-800 border-gray-700 text-gray-100">
        <DialogHeader>
          <DialogTitle className="text-gray-100">Edit Theme Name</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-2">
            <Label htmlFor="theme-name">Theme Name</Label>
            <Input
              id="theme-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="bg-gray-700 border-gray-600 text-gray-100"
              autoFocus
            />
          </div>
          <div className="flex justify-end space-x-2">
            <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
            <Button onClick={handleSave}>Save</Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
};

export const ThemeLibrary: React.FC<ThemeLibraryProps> = ({ open, onOpenChange }) => {
  const { themes, isLoading, error, applyTheme } = useThemeStore();
  const { updateThemeName, refreshThemes, deleteTheme } = useThemeManagement();
  
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [themeToEdit, setThemeToEdit] = useState<Theme | null>(null);
  const [themeToDelete, setThemeToDelete] = useState<Theme | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  
  const handleEditThemeName = (theme: Theme) => {
    setThemeToEdit(theme);
    setEditDialogOpen(true);
  };
  
  const handleDeleteTheme = (theme: Theme) => {
    setThemeToDelete(theme);
    setDeleteDialogOpen(true);
  };
  
  const handleSaveThemeName = (themeId: string, name: string) => {
    updateThemeName(themeId, name);
  };
  
  const handleConfirmDelete = (themeId: string) => {
    deleteTheme(themeId);
  };
  
  const handleRefresh = async () => {
    setRefreshing(true);
    try {
      await refreshThemes();
    } finally {
      setRefreshing(false);
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
      <ThemeNameEditDialog
        open={editDialogOpen}
        onOpenChange={setEditDialogOpen}
        theme={themeToEdit}
        onSave={handleSaveThemeName}
      />
      <DeleteConfirmationDialog
        open={deleteDialogOpen}
        onOpenChange={setDeleteDialogOpen}
        theme={themeToDelete}
        onConfirm={handleConfirmDelete}
      />
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-4xl bg-gray-800 border-gray-700 text-gray-100">
          <DialogHeader className="flex flex-row items-center justify-between">
            <DialogTitle className="text-gray-100">Theme Library</DialogTitle>
            <Button 
              variant="ghost" 
              size="icon" 
              onClick={handleRefresh} 
              disabled={refreshing || isLoading}
              className="h-8 w-8 text-gray-400 hover:text-gray-100"
            >
              <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            </Button>
          </DialogHeader>

          {error && (
            <div className="bg-red-950/30 text-red-400 p-3 rounded-md mb-4 border border-red-800/50">
              {error}
            </div>
          )}

          {isLoading ? (
            <div className="flex-center py-8">
              <div className="loading-spinner h-8 w-8"></div>
            </div>
          ) : themes.length === 0 ? (
            <Card className="card-base">
              <CardContent className="pt-6 text-center text-gray-400">
                <div className="flex flex-col flex-center py-8">
                  <AlertCircle className="h-12 w-12 mb-4 opacity-50" />
                  <p>No themes available.</p>
                  <p className="mb-4">Add your first theme to get started.</p>
                </div>
              </CardContent>
            </Card>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 max-h-[70vh] overflow-y-auto p-1">
              {themes.map((theme: Theme) => (
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
                    <div className="flex space-x-1">
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        className="h-8 w-8 text-gray-400 hover:text-gray-100"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleEditThemeName(theme);
                        }}
                      >
                        <Edit2 className="h-4 w-4" />
                      </Button>
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        className="h-8 w-8 text-gray-400 hover:text-red-400"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleDeleteTheme(theme);
                        }}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>

                  {theme.author && (
                    <div className="text-gray-400 text-xs mt-1">by {theme.author}</div>
                  )}

                  <div className="text-gray-300 text-sm mt-2 line-clamp-2">{theme.description}</div>

                  <div className="mt-auto">
                    {theme.previewColors && theme.previewColors.length > 0 && renderColorSwatches(theme.previewColors)}
                  </div>
                </div>
                <CardFooter className="p-3 pt-0">
                  <Button 
                    variant="outline" 
                    size="sm" 
                    className="w-full mt-2" 
                    onClick={() => applyTheme(theme.guid)}
                  >
                    Apply
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
    </>
  );
};