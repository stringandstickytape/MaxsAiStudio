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
    );
};