// src/components/settings/AppearanceTab.tsx
import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { FontSizeControl } from './FontSizeControl';
import { useAppearanceManagement } from '@/hooks/useAppearanceManagement';
import { RefreshCw } from 'lucide-react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';

export function AppearanceTab() {
  const [resetDialogOpen, setResetDialogOpen] = useState(false);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const { error, resetToDefaults } = useAppearanceManagement();

  const showSuccessMessage = (message: string) => {
    setSuccessMessage(message);
    setTimeout(() => setSuccessMessage(null), 3000);
  };

  const handleReset = async () => {
    await resetToDefaults();
    setResetDialogOpen(false);
    showSuccessMessage('Settings reset to defaults');
  };

  return (
    <div className="space-y-4">
      
      <Card className="card-base">
        <CardHeader className="pb-2">
          <CardTitle className="text-title text-lg">Font Size</CardTitle>
        </CardHeader>
        <CardContent>
          <FontSizeControl
            onChange={() => {
              
              setSuccessMessage(null);
            }}
            onSave={() => {
              showSuccessMessage('Font size saved successfully');
            }}
          />
        </CardContent>
      </Card>

      
      <Card className="card-base">
        <CardHeader className="pb-2">
          <CardTitle className="text-title text-lg">Theme</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="p-3 bg-gray-700/50 rounded-md border border-gray-600/50">
            <p className="text-body mb-2">Theme settings coming soon</p>
            <p className="text-subtitle">Additional theme options will be available in a future update.</p>
          </div>
        </CardContent>
      </Card>

      
      <div className="flex justify-between mt-6">
        <div className="flex-1">
          {successMessage && (
            <div className="bg-green-900/30 text-green-400 py-2 px-3 rounded-md text-sm">{successMessage}</div>
          )}
          {error && <div className="bg-red-900/30 text-red-400 py-2 px-3 rounded-md text-sm">{error}</div>}
        </div>
        <Button
          variant="outline"
          onClick={() => setResetDialogOpen(true)}
          className="btn-secondary flex items-center gap-1"
        >
          <RefreshCw className="h-4 w-4" />
          <span>Reset to Defaults</span>
        </Button>
      </div>

      
      <AlertDialog open={resetDialogOpen} onOpenChange={setResetDialogOpen}>
        <AlertDialogContent className="bg-gray-800 border-gray-700 text-gray-100">
          <AlertDialogHeader>
            <AlertDialogTitle>Reset Appearance Settings?</AlertDialogTitle>
            <AlertDialogDescription className="text-gray-400">
              This will reset all appearance settings to their default values. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="btn-secondary">Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleReset} className="btn-danger">
              Reset
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

