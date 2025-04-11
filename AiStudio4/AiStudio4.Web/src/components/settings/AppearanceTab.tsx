import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { FontSizeControl } from './FontSizeControl';
import { useAppearanceManagement } from '@/hooks/useAppearanceManagement';
import { RefreshCw } from 'lucide-react';
import { fontSizeUtils } from '@/stores/useAppearanceStore';
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

    const { error } = useAppearanceManagement();

    const showSuccessMessage = (message: string) => {
        setSuccessMessage(message);
        setTimeout(() => setSuccessMessage(null), 3000);
    };

    const handleReset = async () => {
        
        await fontSizeUtils.set(16);
        await fontSizeUtils.saveSettings();
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
                            
                            fontSizeUtils.saveSettings();
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
                    <div className="space-y-3">
                        <p className="text-body">Manage and apply themes to customize the appearance of the application.</p>
                        
                        <div className="flex flex-col sm:flex-row gap-2">
                            <Button 
                                onClick={() => {
                                    window.dispatchEvent(new CustomEvent('open-theme-library'));
                                }}
                                className="btn-primary bg-blue-600/30 hover:bg-blue-500/30 border-blue-500/50 flex items-center space-x-1"
                            >
                                Browse Theme Library
                            </Button>
                            
                            <Button 
                                onClick={() => {
                                    // Open theme library with install flag set to true
                                    const modalStore = window.useModalStore || window.useModalStore;
                                    if (modalStore) {
                                        modalStore.getState().openModal('theme', { installTheme: true });
                                    } else {
                                        window.dispatchEvent(new CustomEvent('open-theme-library'));
                                    }
                                }}
                                variant="outline"
                                className="btn-secondary flex items-center space-x-1"
                            >
                                Install New Theme
                            </Button>
                        </div>
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