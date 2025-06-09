// AiStudioClient\src\components\categoryUtils.tsx
import React from 'react';
import * as LucideIcons from 'lucide-react';
import { PinnedCommand } from '@/stores/usePinnedCommandsStore';

/**
 * Category metadata for display names and icons
 */
export interface CategoryInfo {
  id: string;
  displayName: string;
  icon: React.ComponentType<{ className?: string }>;
  description?: string;
}

/**
 * Default category metadata with fallbacks for unknown categories
 */
const CATEGORY_METADATA: Record<string, Omit<CategoryInfo, 'id'>> = {
  conv: {
    displayName: 'Conversation',
    icon: LucideIcons.MessageSquare,
    description: 'Chat and conversation management'
  },
  model: {
    displayName: 'Models',
    icon: LucideIcons.Cpu,
    description: 'AI model selection and configuration'
  },
  view: {
    displayName: 'View',
    icon: LucideIcons.Eye,
    description: 'Display and layout options'
  },
  settings: {
    displayName: 'Settings',
    icon: LucideIcons.Settings,
    description: 'Application configuration'
  },
  utility: {
    displayName: 'Utility',
    icon: LucideIcons.Wrench,
    description: 'Helper tools and utilities'
  },
  appearance: {
    displayName: 'Appearance',
    icon: LucideIcons.Palette,
    description: 'Theme and visual customization'
  },
  tools: {
    displayName: 'Tools',
    icon: LucideIcons.Tool,
    description: 'Development and analysis tools'
  },
  files: {
    displayName: 'Files',
    icon: LucideIcons.FileText,
    description: 'File management and operations'
  }
};

/**
 * Gets category information with fallback for unknown categories
 */
export const getCategoryInfo = (categoryId: string): CategoryInfo => {
  const metadata = CATEGORY_METADATA[categoryId];
  
  if (metadata) {
    return {
      id: categoryId,
      ...metadata
    };
  }
  
  // Fallback for unknown categories
  return {
    id: categoryId,
    displayName: categoryId.charAt(0).toUpperCase() + categoryId.slice(1),
    icon: LucideIcons.Folder,
    description: `${categoryId} commands`
  };
};

/**
 * Extracts unique categories from pinned commands
 */
export const extractCategories = (pinnedCommands: PinnedCommand[]): string[] => {
  const categories = new Set<string>();
  
  pinnedCommands.forEach(command => {
    if (command.section) {
      categories.add(command.section);
    }
  });
  
  return Array.from(categories);
};

/**
 * Groups commands by category and sorts them by position
 */
export const groupCommandsByCategory = (pinnedCommands: PinnedCommand[]): Record<string, PinnedCommand[]> => {
  const grouped: Record<string, PinnedCommand[]> = {};
  
  pinnedCommands.forEach(command => {
    const category = command.section || 'uncategorized';
    
    if (!grouped[category]) {
      grouped[category] = [];
    }
    
    grouped[category].push(command);
  });
  
  // Sort commands within each category by position
  Object.keys(grouped).forEach(category => {
    grouped[category].sort((a, b) => (a.position || 0) - (b.position || 0));
  });
  
  return grouped;
};

/**
 * Sorts categories according to the specified order, with unknown categories at the end
 */
export const sortCategories = (categories: string[], categoryOrder: string[]): string[] => {
  const ordered: string[] = [];
  const unordered: string[] = [];
  
  // Add categories in the specified order
  categoryOrder.forEach(categoryId => {
    if (categories.includes(categoryId)) {
      ordered.push(categoryId);
    }
  });
  
  // Add any remaining categories not in the order
  categories.forEach(categoryId => {
    if (!categoryOrder.includes(categoryId)) {
      unordered.push(categoryId);
    }
  });
  
  return [...ordered, ...unordered];
};

/**
 * Gets the background color for a category pill
 */
export const getCategoryPillBackground = (categoryId: string): string => {
  const colors: Record<string, string> = {
    conv: 'bg-blue-600/80 hover:bg-blue-600/90',
    model: 'bg-purple-600/80 hover:bg-purple-600/90',
    view: 'bg-green-600/80 hover:bg-green-600/90',
    settings: 'bg-yellow-600/80 hover:bg-yellow-600/90',
    utility: 'bg-orange-600/80 hover:bg-orange-600/90',
    appearance: 'bg-pink-600/80 hover:bg-pink-600/90',
    tools: 'bg-indigo-600/80 hover:bg-indigo-600/90',
    files: 'bg-gray-600/80 hover:bg-gray-600/90'
  };
  
  return colors[categoryId] || 'bg-slate-600/80 hover:bg-slate-600/90';
};

/**
 * Gets the border color for a category pill
 */
export const getCategoryPillBorder = (categoryId: string): string => {
  const colors: Record<string, string> = {
    conv: 'border-blue-400/50',
    model: 'border-purple-400/50',
    view: 'border-green-400/50',
    settings: 'border-yellow-400/50',
    utility: 'border-orange-400/50',
    appearance: 'border-pink-400/50',
    tools: 'border-indigo-400/50',
    files: 'border-gray-400/50'
  };
  
  return colors[categoryId] || 'border-slate-400/50';
};