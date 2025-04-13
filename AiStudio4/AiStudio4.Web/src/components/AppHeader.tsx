import { Command } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { PinnedShortcuts } from '@/components/PinnedShortcuts';

interface AppHeaderProps {
    onExecuteCommand?: (command: string) => void;
    isCommandBarOpen?: boolean;
    setIsCommandBarOpen?: (open: boolean) => void;
    CommandBarComponent?: React.ReactNode;
    sidebarOpen?: boolean;
    rightSidebarOpen?: boolean;
    activeConvId?: string | null;
}

export function AppHeader({
    onExecuteCommand = () => { },
    isCommandBarOpen = false,
    setIsCommandBarOpen = () => { },
    CommandBarComponent,
    sidebarOpen = false,
    rightSidebarOpen = false,
    activeConvId = null,
}: AppHeaderProps) {
    const [commandText, setCommandText] = useState('');

    const handleCommandSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (commandText.trim()) {
            onExecuteCommand(commandText);
            setCommandText('');
            setIsCommandBarOpen(false);
        }
    };

    useEffect(() => {
        const inputElement = document.getElementById('command-input');
        if (isCommandBarOpen && inputElement) {
            inputElement.focus();
        }
    }, [isCommandBarOpen]);


    return (
        <div className="app-container">
            <div className={cn(
                'AppHeader header-section relative z-2 border-b border-gray-700/50 shadow-xl backdrop-blur-sm p-0 h-full flex flex-col transition-all duration-300',
            )}
            style={{
                backgroundColor: 'var(--appheader-bg, #1a1f2c)', // Default dark blue-gray
                color: 'var(--appheader-text-color, #e0e0e0)',
                borderColor: 'var(--appheader-border-color, #3a3f4c)',
                fontFamily: 'var(--appheader-font-family, inherit)',
                fontSize: 'var(--appheader-font-size, 0.875rem)',
                boxShadow: '0 2px 4px rgba(0,0,0,0.2)',
                ...(window?.theme?.AppHeader?.style || {})
            }}
            >
                
                <div className="flex flex-1 justify-center justify-items-center ">
                <div 
                    className={cn("w-full justify-items-center ", 
                        sidebarOpen || rightSidebarOpen ? "max-w-[calc(100%-40px)]" : "max-w-2xl",
                        sidebarOpen && rightSidebarOpen ? "max-w-full" : ""
                    )} 
                    style={{
                        margin: '0 auto'
                    }}
                >
                        
                        {CommandBarComponent || (
                            <form onSubmit={handleCommandSubmit} className="relative w-full mb-2">
                                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                                    <Command className="h-4 w-4" />
                                </div>
                                <Input
                                    id="command-input"
                                    type="text"
                                    placeholder="Type a command (/ for suggestions)..."
                                    value={commandText}
                                    onChange={(e) => setCommandText(e.target.value)}
                                    className="w-full shadow-inner transition-all duration-200 input-ghost input-with-icon"
                                    onBlur={() => setIsCommandBarOpen(false)}
                                />
                                <kbd
                                    className="absolute right-3 top-1/2 transform -translate-y-1/2 pointer-events-none hidden sm:inline-flex items-center gap-1 px-2 py-0.5 text-xs font-mono text-gray-400 bg-gray-800 rounded border border-gray-700"
                                    onClick={() => setIsCommandBarOpen(true)}
                                >
                                    {navigator.platform.indexOf('Mac') !== -1 ? '⌘ + K' : 'Ctrl + K'}
                                </kbd>
                            </form>
                        )}
                    </div>
                </div>

                
                <div className="mt-1 border-t border-gray-700/30 pt-1 flex flex-col gap-0.5">
                    <div 
                        className="w-full pb-1"
                    >
                        <PinnedShortcuts orientation="horizontal" maxShown={15} className="overflow-x-auto" maxRows={3} />
                    </div>
                </div>
            </div>
        </div>
    );
}

// Expose themeable properties for ThemeManager
export const themeableProps = {
  backgroundColor: {
    cssVar: '--appheader-bg',
    description: 'App header background color',
    default: '#1a1f2c', // Default dark blue-gray
  },
  textColor: {
    cssVar: '--appheader-text-color',
    description: 'App header text color',
    default: '#e0e0e0',
  },
  borderColor: {
    cssVar: '--appheader-border-color',
    description: 'App header border color (used for bottom border)',
    default: '#3a3f4c',
  },
  accentColor: {
    cssVar: '--appheader-accent-color',
    description: 'App header accent color for highlights and active elements',
    default: '#3b82f6',
  },
  style: {
    description: 'Arbitrary CSS style for the root AppHeader container',
    default: {},
  },
  // Keeping font properties as they're essential for readability
  fontFamily: {
    cssVar: '--appheader-font-family',
    description: 'App header font family',
    default: 'inherit',
  },
  fontSize: {
    cssVar: '--appheader-font-size',
    description: 'App header base font size',
    default: '0.875rem',
  }
};