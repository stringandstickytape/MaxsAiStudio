// AiStudio4.Web/src/components/ThemeLibrary.tsx

import React from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { useThemeStore } from '@/stores/useThemeStore';
import { Theme } from '@/types/theme';
import { AlertCircle } from 'lucide-react';

interface ThemeLibraryProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export const ThemeLibrary: React.FC<ThemeLibraryProps> = ({ open, onOpenChange }) => {
  const { themes, isLoading, error } = useThemeStore();

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
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl bg-gray-800 border-gray-700 text-gray-100">
        <DialogHeader>
          <DialogTitle className="text-gray-100">Theme Library</DialogTitle>
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
                  <div className="flex items-start">
                    <h3 className="text-gray-100 text-lg font-medium truncate">{theme.name}</h3>
                  </div>

                  {theme.author && (
                    <div className="text-gray-400 text-xs mt-1">by {theme.author}</div>
                  )}

                  <div className="text-gray-300 text-sm mt-2 line-clamp-2">{theme.description}</div>

                  <div className="mt-auto">
                    {theme.previewColors && theme.previewColors.length > 0 && renderColorSwatches(theme.previewColors)}
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
};