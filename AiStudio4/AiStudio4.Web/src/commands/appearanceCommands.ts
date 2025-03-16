
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
  const shortcut = (key: string) => (mac ? `⌘+${key}` : `Ctrl+${key}`);

  const { registerGroup } = useCommandStore.getState();

  registerGroup({
    id: 'appearance',
    name: 'Appearance',
    priority: 80,
    commands: [
      [
        'open-appearance-settings',
        'Appearance Settings',
        'Open appearance and theme settings',
        shortcut('A'),
        ['appearance', 'font', 'size', 'theme', 'dark', 'light', 'settings', 'customize'],
        React.createElement(Settings, { size: 16 }),
        () => {
          commandEvents.emit('settings-tab', 'appearance');
          config.openAppearanceSettings();
        },
      ],
      [
        'increase-font-size',
        'Increase Font Size',
        'Make the text larger',
        shortcut('+'),
        ['font', 'size', 'larger', 'bigger', 'increase', 'zoom', 'in'],
        React.createElement(ZoomIn, { size: 16 }),
        () => {
          const { increaseFontSize, saveAppearanceSettings } = useAppearanceStore.getState();
          increaseFontSize();
          setTimeout(() => saveAppearanceSettings(), 500);
        },
      ],
      [
        'decrease-font-size',
        'Decrease Font Size',
        'Make the text smaller',
        shortcut('-'),
        ['font', 'size', 'smaller', 'decrease', 'zoom', 'out'],
        React.createElement(ZoomOut, { size: 16 }),
        () => {
          const { decreaseFontSize, saveAppearanceSettings } = useAppearanceStore.getState();
          decreaseFontSize();
          setTimeout(() => saveAppearanceSettings(), 500);
        },
      ],
      [
        'reset-font-size',
        'Reset Font Size',
        'Reset to default font size',
        '',
        ['font', 'size', 'reset', 'default', 'normal', 'medium'],
        React.createElement(RotateCcw, { size: 16 }),
        () => {
          const { setFontSize, saveAppearanceSettings } = useAppearanceStore.getState();
          setFontSize(16);
          setTimeout(() => saveAppearanceSettings(), 500);
        },
      ],
    ].map(([id, name, description, shortcut, keywords, icon, fn]) => ({
      id,
      name,
      description,
      shortcut,
      keywords,
      section: 'appearance',
      icon,
      execute: fn,
    })),
  });
}
