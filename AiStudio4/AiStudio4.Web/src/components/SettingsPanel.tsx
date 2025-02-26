import React from 'react';
import { Button } from '@/components/ui/button';
import { ChevronRight } from 'lucide-react';

interface SettingsPanelProps {
    isOpen: boolean;
    onClose: () => void;
}

export const SettingsPanel: React.FC<SettingsPanelProps> = ({ isOpen, onClose }) => {
    if (!isOpen) return null;
    
    return (
        <div className="fixed right-0 top-0 h-screen w-[300px] bg-[#1f2937] border-l border-gray-700 shadow-lg transform transition-transform duration-300 z-50">
            <div className="p-4 border-b border-gray-700 flex items-center justify-between">
                <h2 className="text-gray-100 text-lg font-semibold">Settings</h2>
                <Button
                    variant="ghost"
                    size="icon"
                    onClick={onClose}
                    className="text-gray-400 hover:text-gray-100"
                >
                    <ChevronRight className="h-5 w-5" />
                </Button>
            </div>
            <div className="p-4">
                <h3 className="text-md font-medium mb-2">Appearance</h3>
                <div className="space-y-2 mb-6">
                    {/* Placeholder for theme settings */}
                    <div className="p-3 bg-gray-800 rounded-md">Theme settings will go here</div>
                </div>
                
                <h3 className="text-md font-medium mb-2">API Settings</h3>
                <div className="space-y-2 mb-6">
                    {/* Placeholder for API settings */}
                    <div className="p-3 bg-gray-800 rounded-md">API configuration will go here</div>
                </div>
                
                <h3 className="text-md font-medium mb-2">About</h3>
                <div className="p-3 bg-gray-800 rounded-md">
                    <p className="text-sm text-gray-300">Version: 1.0.0</p>
                    <p className="text-sm text-gray-300">Build Date: 2023-05-28</p>
                </div>
            </div>
        </div>
    );
};
