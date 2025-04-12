// AiStudio4.Web/src/commands/themeCommands.ts

import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { Palette, PaintBucket } from 'lucide-react';
import { useModalStore } from '@/stores/useModalStore';
import { Theme } from '@/types/theme';
import themeManagerInstance from '@/lib/ThemeManager';
// Simple event emitter implementation for browser
class SimpleEventEmitter {
  private events: Record<string, Function[]> = {};

  on(event: string, listener: Function): void {
    if (!this.events[event]) {
      this.events[event] = [];
    }
    this.events[event].push(listener);
  }

  off(event: string, listener: Function): void {
    if (!this.events[event]) return;
    this.events[event] = this.events[event].filter(l => l !== listener);
  }

  emit(event: string, ...args: any[]): void {
    if (!this.events[event]) return;
    this.events[event].forEach(listener => listener(...args));
  }
}

// Event emitter for theme-related events
export const themeEvents = new SimpleEventEmitter();

interface ThemeCommandsConfig {
  openThemeLibrary: () => void;
  availableThemes?: Theme[];
}

export function initializeThemeCommands(config: ThemeCommandsConfig) {
  const { registerGroup, registerCommand, unregisterCommand } = useCommandStore.getState();

  // Register the theme commands group
  registerGroup({
    id: 'themes',
    name: 'Themes',
    priority: 75,
    commands: [
      [
        'open-theme-library',
        'Theme Library',
        'Browse and manage themes',
        '',
        ['theme', 'appearance', 'customize', 'style', 'color', 'library'],
        React.createElement(Palette, { size: 16 }),
        () => {
          config.openThemeLibrary();
        },
      ],
      [
        'install-theme',
        'Install New Theme',
        'Add a new theme to your library',
        '',
        ['theme', 'add', 'import', 'install', 'new'],
        React.createElement(PaintBucket, { size: 16 }),
        () => {
          useModalStore.getState().openModal('theme', { installTheme: true });
        },
      ],
    ].map(([id, name, description, shortcut, keywords, icon, fn]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'themes',
      icon,
      execute: fn,
    })),
  });

  // Register dynamic theme commands based on available themes
  if (config.availableThemes && Array.isArray(config.availableThemes) && config.availableThemes.length > 0) {
    config.availableThemes.forEach(theme => {
      registerCommand({
        id: `apply-theme-${theme.guid}`,
        name: `Apply Theme: ${theme.name}`,
        description: theme.description || `Apply the ${theme.name} theme`,
        shortcut: '',
        keywords: ['theme', 'apply', 'style', theme.name.toLowerCase()],
        section: 'themes',
        icon: React.createElement(PaintBucket, { size: 16 }),
        execute: async () => {
          try {
            // Import both theme API and hook
            const themeApi = await import('@/api/themeApi');
            const { useThemeManagement } = await import('@/hooks/useThemeManagement');
            const { applyTheme } = useThemeManagement();
            
            // Use the hook's applyTheme function which handles both visual application
            // and saving to library
            await applyTheme(theme);
            console.log(`Theme "${theme.name}" added to library and set as default`);
          } catch (error) {
            console.error('Error applying theme:', error);
          }
        },
      });
    });
  }

  // Listen for theme library changes to update commands
  themeEvents.on('themes-updated', (themes: Theme[]) => {
    // First, remove all existing theme commands
    const commands = useCommandStore.getState().commands;
    Array.from(commands.values())
      .filter(cmd => cmd.id.startsWith('apply-theme-'))
      .forEach(cmd => {
        unregisterCommand(cmd.id);
      });

    // Then register new commands for each theme
    if (Array.isArray(themes)) {
      themes.forEach(theme => {
        registerCommand({
          id: `apply-theme-${theme.guid}`,
          name: `Apply Theme: ${theme.name}`,
          description: theme.description || `Apply the ${theme.name} theme`,
          shortcut: '',
          keywords: ['theme', 'apply', 'style', theme.name.toLowerCase()],
          section: 'themes',
          icon: React.createElement(PaintBucket, { size: 16 }),
          execute: async () => {
            try {
              // Import both theme API and hook
              const themeApi = await import('@/api/themeApi');
              const { useThemeManagement } = await import('@/hooks/useThemeManagement');
              const { applyTheme } = useThemeManagement();
              
              // Use the hook's applyTheme function which handles both visual application
              // and saving to library
              await applyTheme(theme);
              console.log(`Theme "${theme.name}" added to library and set as default`);
            } catch (error) {
              console.error('Error applying theme:', error);
            }
          },
        });
      });
    }
  });
}