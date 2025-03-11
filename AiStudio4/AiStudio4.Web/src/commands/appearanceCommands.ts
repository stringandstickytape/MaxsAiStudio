// src/commands/appearanceCommands.ts
import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { ZoomIn, ZoomOut, RotateCcw, Settings } from 'lucide-react';
import { useAppearanceStore } from '@/stores/useAppearanceStore';
import { commandEvents } from '@/commands/settingsCommands';

interface AppearanceCommandsConfig {
  openAppearanceSettings: () => void;
}

export function initializeAppearanceCommands(config: AppearanceCommandsConfig) {
  const mac = navigator.platform.indexOf('Mac') !== -1;
  const shortcut = (key: string) => mac ? `⌘+${key}` : `Ctrl+${key}`;

  const { registerGroup } = useCommandStore.getState();
  
  registerGroup({
    id: 'appearance',
    name: 'Appearance',
    priority: 80,
    commands: [
      {
        id: 'open-appearance-settings',
        name: 'Appearance Settings',
        description: 'Open appearance and theme settings',
        shortcut: shortcut('A'),
        keywords: ['appearance', 'font', 'size', 'theme', 'dark', 'light', 'settings', 'customize'],
        section: 'appearance',
        icon: React.createElement(Settings, { size: 16 }),
        execute: () => {
          // Switch to appearance tab and open settings
          commandEvents.emit('settings-tab', 'appearance');
          config.openAppearanceSettings();
        }
      },
      {
        id: 'increase-font-size',
        name: 'Increase Font Size',
        description: 'Make the text larger',
        shortcut: shortcut('+'),
        keywords: ['font', 'size', 'larger', 'bigger', 'increase', 'zoom', 'in'],
        section: 'appearance',
        icon: React.createElement(ZoomIn, { size: 16 }),
        execute: () => {
          const { increaseFontSize, saveAppearanceSettings } = useAppearanceStore.getState();
          increaseFontSize();
          // Save the change after a delay
          setTimeout(() => {
            saveAppearanceSettings();
          }, 500);
        }
      },
      {
        id: 'decrease-font-size',
        name: 'Decrease Font Size',
        description: 'Make the text smaller',
        shortcut: shortcut('-'),
        keywords: ['font', 'size', 'smaller', 'decrease', 'zoom', 'out'],
        section: 'appearance',
        icon: React.createElement(ZoomOut, { size: 16 }),
        execute: () => {
          const { decreaseFontSize, saveAppearanceSettings } = useAppearanceStore.getState();
          decreaseFontSize();
          // Save the change after a delay
          setTimeout(() => {
            saveAppearanceSettings();
          }, 500);
        }
      },
      {
        id: 'reset-font-size',
        name: 'Reset Font Size',
        description: 'Reset to default font size',
        keywords: ['font', 'size', 'reset', 'default', 'normal', 'medium'],
        section: 'appearance',
        icon: React.createElement(RotateCcw, { size: 16 }),
        execute: () => {
          const { setFontSize, saveAppearanceSettings } = useAppearanceStore.getState();
          setFontSize(16); // Default size
          // Save the change after a delay
          setTimeout(() => {
            saveAppearanceSettings();
          }, 500);
        }
      }
    ]
  });
}