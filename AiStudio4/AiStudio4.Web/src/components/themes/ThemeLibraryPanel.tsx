// AiStudio4.Web/src/components/themes/ThemeLibraryPanel.tsx

import { useState, useEffect } from 'react';
import { Theme } from '@/types/theme';
import { useThemeManagement } from '@/hooks/useThemeManagement';
import { ThemeCard } from './ThemeCard';
import { ThemePreview } from './ThemePreview';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Search, Download, Upload, CheckSquare, Square } from 'lucide-react';

interface ThemeLibraryPanelProps {
  isOpen?: boolean;
  onClose?: () => void;
  onApplyTheme: (theme: Theme) => void;
}

export function ThemeLibraryPanel({ isOpen = true, onClose, onApplyTheme }: ThemeLibraryPanelProps) {
  const {
    themes,
    selectedThemeIds,
    isLoading,
    error,
    fetchThemes,
    deleteTheme,
    importThemes,
    exportThemes,
    applyTheme,
    toggleThemeSelection,
    clearSelectedThemes,
    selectAllThemes,
  } = useThemeManagement();

  const [searchTerm, setSearchTerm] = useState('');
  const [isImportDialogOpen, setIsImportDialogOpen] = useState(false);
  const [importJson, setImportJson] = useState('');
  const [isImporting, setIsImporting] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [previewTheme, setPreviewTheme] = useState<Theme | null>(null);

  useEffect(() => {
    if (isOpen) {
      fetchThemes();
    }
  }, [isOpen, fetchThemes]);

  const handleApplyTheme = (theme: Theme) => {
    applyTheme(theme);
    onApplyTheme(theme);
  };

  const handleDeleteTheme = async (themeId: string) => {
    await deleteTheme(themeId);
  };

  const handleImportThemes = async () => {
    if (!importJson.trim()) return;
    
    try {
      setIsImporting(true);
      await importThemes(importJson);
      setIsImportDialogOpen(false);
      setImportJson('');
    } catch (error) {
      console.error('Failed to import themes:', error);
      alert(`Failed to import themes: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsImporting(false);
    }
  };

  const handleExportThemes = async () => {
    try {
      setIsExporting(true);
      const themeIds = selectedThemeIds.length > 0 ? selectedThemeIds : undefined;
      const json = await exportThemes(themeIds);
      
      // Create and download a JSON file
      const blob = new Blob([json], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'themes-export.json';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error exporting themes:', error);
      alert(`Failed to export themes: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsExporting(false);
    }
  };

  const filteredThemes = Array.isArray(themes) ? themes.filter((theme) => {
    return (
      searchTerm === '' ||
      theme.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      (theme.description && theme.description.toLowerCase().includes(searchTerm.toLowerCase())) ||
      (theme.author && theme.author.toLowerCase().includes(searchTerm.toLowerCase()))
    );
  }) : [];

  return (
    <div className="p-4 overflow-y-auto h-full bg-gray-900 text-gray-100">
      <div className="flex justify-between items-center mb-4">
        <div className="flex items-center gap-4">
          <h2 className="text-title">Theme Library</h2>
        </div>
        <div className="flex space-x-2">
          <Button
            variant="outline"
            size="sm"
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
            onClick={selectAllThemes}
            disabled={isLoading || filteredThemes.length === 0 || filteredThemes.length === selectedThemeIds.length}
          >
            <CheckSquare className="h-4 w-4 mr-1" />
            Select All
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
            onClick={clearSelectedThemes}
            disabled={isLoading || selectedThemeIds.length === 0}
          >
            <Square className="h-4 w-4 mr-1" />
            Select None
          </Button>
          <Button 
            variant="outline" 
            size="sm"
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
            onClick={handleExportThemes} 
            disabled={isExporting || (selectedThemeIds.length === 0 && themes.length === 0)}
          >
            <Download className="h-4 w-4 mr-1" />
            {isExporting ? 'Exporting...' : 'Export'}
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsImportDialogOpen(true)}
            className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
          >
            <Upload className="h-4 w-4 mr-1" />
            Import
          </Button>
        </div>
      </div>

      <div className="mb-4">
        <div className="relative">
          <Search className="absolute left-2 top-2.5 h-4 w-4 text-gray-500" />
          <Input
            placeholder="Search themes..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="pl-8 input-base"
          />
        </div>
      </div>

      <div className="text-sm text-gray-400 mb-2">
        <span className="mr-2">Currently selected: {selectedThemeIds.length} theme{selectedThemeIds.length !== 1 ? 's' : ''}</span>
      </div>

      {isLoading ? (
        <div className="flex-center h-32">
          <div className="loading-spinner h-8 w-8"></div>
        </div>
      ) : filteredThemes.length === 0 ? (
        <div className="text-center p-8 text-subtitle">
          {searchTerm ? 'No themes match your search' : 'No themes available'}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredThemes.map((theme) => (
            <ThemeCard
              key={theme.guid}
              theme={theme}
              isSelected={selectedThemeIds.includes(theme.guid)}
              onSelect={toggleThemeSelection}
              onDelete={handleDeleteTheme}
              onApply={handleApplyTheme}
            />
          ))}
        </div>
      )}

      {/* Import Dialog */}
      <Dialog open={isImportDialogOpen} onOpenChange={setIsImportDialogOpen}>
        <DialogContent className="bg-gray-900 border-gray-700 text-gray-100">
          <DialogHeader>
            <DialogTitle>Import Themes</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <p className="text-sm text-gray-400">
              Paste the JSON export of themes below to import them into your library.
            </p>
            <textarea
              className="w-full h-64 p-2 bg-gray-800 border border-gray-700 rounded-md text-gray-100"
              value={importJson}
              onChange={(e) => setImportJson(e.target.value)}
              placeholder='{"themes": [...]}'
            />
            <div className="flex justify-end space-x-2">
              <Button
                variant="outline"
                onClick={() => setIsImportDialogOpen(false)}
                disabled={isImporting}
              >
                Cancel
              </Button>
              <Button
                onClick={handleImportThemes}
                disabled={isImporting || !importJson.trim()}
              >
                {isImporting ? 'Importing...' : 'Import'}
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Theme Preview Dialog */}
      {previewTheme && (
        <Dialog open={!!previewTheme} onOpenChange={() => setPreviewTheme(null)}>
          <DialogContent className="bg-gray-900 border-gray-700 text-gray-100">
            <DialogHeader>
              <DialogTitle>Theme Preview</DialogTitle>
            </DialogHeader>
            <ThemePreview theme={previewTheme} />
            <div className="flex justify-end space-x-2 mt-4">
              <Button
                variant="outline"
                onClick={() => setPreviewTheme(null)}
              >
                Close
              </Button>
              <Button
                onClick={() => {
                  handleApplyTheme(previewTheme);
                  setPreviewTheme(null);
                }}
              >
                Apply Theme
              </Button>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}