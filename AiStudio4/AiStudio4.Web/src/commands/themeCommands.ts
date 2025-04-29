// AiStudio4.Web/src/commands/themeCommands.ts
import { useThemeStore } from '@/stores/useThemeStore';
import { Palette } from 'lucide-react';
import React from 'react';
import ThemeManager from '@/lib/ThemeManager';
import { commandRegistry } from '@/services/commandRegistry';

// The selectTheme function should setActiveThemeId and (optionally) persist to backend.
// The command's execute will also apply the theme visually.
export function registerThemeCommands(selectTheme: (themeGuid: string) => void) {
  try {
    commandRegistry.unregisterGroup('theme-selection');
  } catch (e) {}

  const { themes, activeThemeId } = useThemeStore.getState();
  console.log('[themeCommands] Registering theme commands. themes:', themes, 'activeThemeId:', activeThemeId);
  if (!themes || themes.length === 0) {
    console.warn('[themeCommands] No themes available to register commands for.');
  }
  const themeCommands = themes.map((theme) => ({
    id: `select-theme-${theme.guid}`,
    name: `${theme.name}` + (activeThemeId === theme.guid ? ' [Active]' : ''),
    description: `Switch to theme: ${theme.name}`,
    keywords: ['theme', 'appearance', 'color', 'switch', 'set', (theme.name || '').toLowerCase()],
    section: 'appearance',
    icon: React.createElement(Palette, { size: 16 }),
    execute: () => {
      selectTheme(theme.guid);
      ThemeManager.applyLLMTheme(theme.themeJson);
    },
    disabled: activeThemeId === theme.guid,
  }));

  console.log('[themeCommands] themeCommands array:', themeCommands);

  commandRegistry.registerGroup({
    id: 'theme-selection',
    name: 'Theme Selection',
    priority: 79,
    commands: themeCommands,
  });
  console.log('[themeCommands] Registered theme-selection group with', themeCommands.length, 'commands');
}