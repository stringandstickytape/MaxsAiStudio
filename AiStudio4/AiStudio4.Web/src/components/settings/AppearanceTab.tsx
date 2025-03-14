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
  
  const {
    fontSize,
    isDarkMode,
    isLoading,
    error,
    setFontSize,
    saveSettings,
    resetToDefaults
  } = useAppearanceManagement();
  
  // Show success message briefly
  const showSuccessMessage = (message: string) => {
    setSuccessMessage(message);
    setTimeout(() => setSuccessMessage(null), 3000);
  };
  
  // Handle save
  const handleSave = async () => {
    const success = await saveSettings();
    if (success) {
      showSuccessMessage('Appearance settings saved successfully');
    }
  };
  
  // Handle reset to defaults
  const handleReset = async () => {
    await resetToDefaults();
    setResetDialogOpen(false);
    showSuccessMessage('Settings reset to defaults');
  };
  
  return (
    <div className="space-y-4">
      {/* Font Size Card */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader className="pb-2">
          <CardTitle className="text-lg text-gray-100">Font Size</CardTitle>
        </CardHeader>
        <CardContent>
          <FontSizeControl 
            onChange={() => {
              // Clear success message when changes are made
              setSuccessMessage(null);
            }}
            onSave={() => {
              showSuccessMessage('Font size saved successfully');
            }}
          />
        </CardContent>
      </Card>
      
      {/* Theme Card */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader className="pb-2">
          <CardTitle className="text-lg text-gray-100">Theme</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="p-3 bg-gray-700/50 rounded-md border border-gray-600/50">
            <p className="text-gray-300 mb-2">Theme settings coming soon</p>
            <p className="text-xs text-gray-400">Additional theme options will be available in a future update.</p>
          </div>
        </CardContent>
      </Card>
      
      {/* Reset Settings Button */}
      <div className="flex justify-between mt-6">
        <div className="flex-1">
          {successMessage && (
            <div className="bg-green-900/30 text-green-400 py-2 px-3 rounded-md text-sm">
              {successMessage}
            </div>
          )}
          {error && (
            <div className="bg-red-900/30 text-red-400 py-2 px-3 rounded-md text-sm">
              {error}
            </div>
          )}
        </div>
        <Button
          variant="outline"
          onClick={() => setResetDialogOpen(true)}
          className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600 flex items-center gap-1"
        >
          <RefreshCw className="h-4 w-4" />
          <span>Reset to Defaults</span>
        </Button>
      </div>
      
      {/* Reset Confirmation Dialog */}
      <AlertDialog open={resetDialogOpen} onOpenChange={setResetDialogOpen}>
        <AlertDialogContent className="bg-gray-800 border-gray-700 text-gray-100">
          <AlertDialogHeader>
            <AlertDialogTitle>Reset Appearance Settings?</AlertDialogTitle>
            <AlertDialogDescription className="text-gray-400">
              This will reset all appearance settings to their default values. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600">
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleReset}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              Reset
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}