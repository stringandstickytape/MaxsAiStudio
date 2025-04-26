// AiStudio4.Web\src\components\pinnedShortcutsUtils.tsx
import React from 'react';
import * as LucideIcons from 'lucide-react';
import * as LobehubIcons from '@lobehub/icons';
import { Command, Plus, Settings, RefreshCw, GitBranch, Mic } from 'lucide-react';

/**
 * Gets the appropriate icon component for a command
 */
export const getIconForCommand = (commandId: string, iconName?: string, iconSet?: string) => {
    const iconProps = { className: "h-7 w-7" };

    if (iconName) {
        try {
            if (iconSet !== 'lucide' && iconSet !== 'lobehub') {
                // default icon
                iconSet = 'lucide';
                iconName = 'Pin';
            }

            // If we have a specific icon name, try to use it
            if (iconSet === 'lucide') {
                // For Lucide icons, we can dynamically import them
                if (LucideIcons && typeof LucideIcons === 'object') {
                    const LucideIcon = LucideIcons[iconName as keyof typeof LucideIcons];
                    if (LucideIcon) {
                        // Add slightly bigger left margin for lucide icons
                        return <LucideIcon {...iconProps} className="h-7 w-7 ml-1" />;
                    }
                }
            } else if (iconSet === 'lobehub') {
                // For Lobehub icons
                if (LobehubIcons && typeof LobehubIcons === 'object') {
                    const icon = LobehubIcons[iconName as keyof typeof LobehubIcons];
                    if (icon && icon.Avatar) {
                        return React.createElement(icon.Avatar, { size: 24 });
                    }
                }
            }
            
        } catch (error) {
            console.error('Error rendering icon:', error);
            return <Command {...iconProps} />;
        }
    }

    // Fallback to inferring icon from command ID
    return commandId.includes('new') ? <Plus {...iconProps} /> :
           commandId.includes('settings') ? <Settings {...iconProps} /> :
           commandId.includes('clear') || commandId.includes('reset') ? <RefreshCw {...iconProps} /> :
           commandId.includes('tree') ? <GitBranch {...iconProps} /> :
           commandId.includes('voice') ? <Mic {...iconProps} /> :
           <Command {...iconProps} />;
}

/**
 * Gets the border color class based on the command category
 */
export const getCategoryBorderColor = (section?: string) => {
    switch(section) {
        case 'conv': return 'border-blue-500/70';
        case 'model': return 'border-purple-500/70';
        case 'view': return 'border-green-500/70';
        case 'settings': return 'border-yellow-500/70';
        case 'utility': return 'border-orange-500/70';
        case 'appearance': return 'border-pink-500/70';
        default: return 'border-gray-700/40';
    }
};

/**
 * Gets the background color class based on the command category
 */
export const getCategoryBackgroundColor = (section?: string) => {
    switch(section) {
        case 'conv': return 'bg-blue-900/20';
        case 'model': return 'bg-purple-900/20';
        case 'view': return 'bg-green-900/20';
        case 'settings': return 'bg-yellow-900/20';
        case 'utility': return 'bg-orange-900/20';
        case 'appearance': return 'bg-pink-900/20';
        default: return 'bg-gray-800/60';
    }
};

/**
 * Types for pinned commands
 */
export interface PinnedCommand {
    id: string;
    name: string;
    iconName?: string;
    iconSet?: string;
    section?: string;
}