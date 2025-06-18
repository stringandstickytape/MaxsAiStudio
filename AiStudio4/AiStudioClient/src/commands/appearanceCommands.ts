import React from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { ZoomIn, ZoomOut, RotateCcw, Settings, Palette, ArrowDown, ArrowDownCircle } from 'lucide-react';
import { useAppearanceStore, fontSizeUtils } from '@/stores/useAppearanceStore';
import { useModalStore } from '@/stores/useModalStore';

export function initializeAppearanceCommands() {
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
          useModalStore.getState().openModal('appearance', {});
        },
      ],
      [
        'open-theme-library',
        'Theme Library',
        'Browse and manage application themes',
        shortcut('T'),
        ['theme', 'library', 'color', 'palette', 'style', 'appearance', 'customize'],
        React.createElement(Palette, { size: 16 }),
        () => {
          useModalStore.getState().openModal('theme');
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
          fontSizeUtils.increase();
          setTimeout(() => fontSizeUtils.saveSettings(), 500);
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
          fontSizeUtils.decrease();
          setTimeout(() => fontSizeUtils.saveSettings(), 500);
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
          fontSizeUtils.set(16);
          setTimeout(() => fontSizeUtils.saveSettings(), 500);
        },
      ],
      [
        'toggle-stick-to-bottom',
        'Toggle Auto-Scroll to Bottom',
        'Toggle automatic scrolling to bottom during conversations',
        '',
        ['stick', 'bottom', 'scroll', 'auto', 'toggle', 'chat', 'conversation'],
        React.createElement(ArrowDownCircle, { size: 16 }),
        () => {
          const { toggleStickToBottom, saveAppearanceSettings } = useAppearanceStore.getState();
          toggleStickToBottom();
          setTimeout(() => saveAppearanceSettings().catch(console.error), 500);
        },
      ],
      [
        'enable-stick-to-bottom',
        'Enable Auto-Scroll to Bottom',
        'Enable automatic scrolling to bottom during conversations',
        '',
        ['stick', 'bottom', 'scroll', 'auto', 'enable', 'on', 'chat', 'conversation'],
        React.createElement(ArrowDown, { size: 16 }),
        () => {
          const { setStickToBottom, saveAppearanceSettings } = useAppearanceStore.getState();
          setStickToBottom(true);
          setTimeout(() => saveAppearanceSettings().catch(console.error), 500);
        },
      ],
      [
        'disable-stick-to-bottom',
        'Disable Auto-Scroll to Bottom',
        'Disable automatic scrolling to bottom during conversations',
        '',
        ['stick', 'bottom', 'scroll', 'auto', 'disable', 'off', 'chat', 'conversation'],
        React.createElement(ArrowDown, { size: 16 }),
        () => {
          const { setStickToBottom, saveAppearanceSettings } = useAppearanceStore.getState();
          setStickToBottom(false);
          setTimeout(() => saveAppearanceSettings().catch(console.error), 500);
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
