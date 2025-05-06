// MarkdownPane/CodeBlockHeader.tsx
import React from 'react';
import { ExternalLink, Clipboard, ChevronDown, ChevronUp } from 'lucide-react';

export interface CodeBlockHeaderProps {
    language: string;
    content: string;
    isCollapsed: boolean;
    isRawView: boolean;
    isVisualStudio: boolean;
    onToggleRaw: () => void;
    onToggleCollapse: () => void;
    launchHtml?: () => void;
}

export const CodeBlockHeader: React.FC<CodeBlockHeaderProps> = ({
    language,
    content,
    isCollapsed,
    isRawView,
    isVisualStudio,
    onToggleRaw,
    onToggleCollapse,
    launchHtml,
}) => {
    return (
        <div
            className={`MarkdownPane flex items-center justify-between px-2 py-1 rounded-t-xl border-b text-sm`}
            style={{
                background: 'var(--global-background-color, #181c20)',
                color: 'var(--global-text-color, #bfc7d5)',
                borderColor: 'var(--global-border-color, #283040)',
                borderStyle: 'solid',
                borderBottomWidth: '1px',
                borderTopWidth: 0,
            }}
        >
            <div className="flex items-center space-x-2">
                <button
                    onClick={onToggleCollapse}
                    className="transition-colors p-1"
                    style={{ color: 'var(--global-primary-color, #4f8cff)' }}
                >
                    {isCollapsed ? <ChevronDown size={16} /> : <ChevronUp size={16} />}
                </button>
                <div className="font-medium">{language}</div>
            </div>
            <div className="flex space-x-2">
                {isVisualStudio && (
                    <button
                        onClick={() => window.chrome.webview.postMessage({ type: 'applyNewDiff', content: content.trim() })}
                        className="px-2 py-1 rounded transition-colors"
                        style={{
                            background: 'var(--global-primary-color, #4f8cff)',
                            color: 'var(--global-background-color, #181c20)',
                        }}
                    >
                        Apply Diff
                    </button>
                )}
                {launchHtml && (
                    <button
                        onClick={launchHtml}
                        className="text-small-gray-400 bg-gray-800 px-2 py-1 rounded hover:bg-gray-700 transition-colors flex items-center gap-1"
                    >
                        <ExternalLink className="h-3 w-3" />
                        Launch
                    </button>
                )}
                {language === 'theme' && (
                    <>
                        <button
                            onClick={async () => {
                                try {
                                    // Parse the theme content
                                    const parsedContent = JSON.parse(content);
                                    
                                    // Apply theme visually
                                    window.applyLLMTheme?.(parsedContent);
                                    
                                    // Get theme name from the theme-name property if available, otherwise use timestamp
                                    const themeName = parsedContent['theme-name'] || `Theme from code block ${new Date().toLocaleTimeString()}`;
                                    
                                    // Add theme to zustand store
                                    if (window.addThemeToStore) {
                                        window.addThemeToStore({
                                            name: themeName,
                                            description: 'Theme applied from markdown code block',
                                            themeJson: parsedContent
                                        });
                                    }
                                } catch (e) {
                                    console.error('[Theme Debug] Error processing theme:', e);
                                }
                            }}
                            className="px-2 py-1 rounded transition-colors mr-2"
                            style={{
                                background: 'var(--global-primary-color, #4f8cff)',
                                color: 'var(--global-background-color, #181c20)',
                            }}
                        >
                            Use Theme
                        </button>
                        <button
                            onClick={async () => {
                                try {
                                    // Parse the theme content
                                    const parsedContent = JSON.parse(content);
                                    
                                    // Get theme name from the theme-name property if available, otherwise use timestamp
                                    const themeName = parsedContent['theme-name'] || `Theme from code block ${new Date().toLocaleTimeString()}`;
                                    
                                    // Install the theme to the library
                                    if (typeof window.createTheme === 'function') {
                                        await window.createTheme({
                                            name: themeName,
                                            description: 'Theme installed from markdown code block',
                                            themeJson: parsedContent,
                                            previewColors: Object.values(parsedContent)
                                                .filter(value => typeof value === 'string' && value.startsWith('#'))
                                                .slice(0, 5)
                                        });
                                    } else {
                                        console.error('createTheme function not available on window');
                                        // Try to import the function directly as a fallback
                                        try {
                                            const { useThemeManagement } = await import('@/hooks/useThemeManagement');
                                            const { createTheme } = useThemeManagement();
                                            
                                            if (typeof createTheme === 'function') {
                                                await createTheme({
                                                    name: themeName,
                                                    description: 'Theme installed from markdown code block',
                                                    themeJson: parsedContent,
                                                    previewColors: Object.values(parsedContent)
                                                        .filter(value => typeof value === 'string' && value.startsWith('#'))
                                                        .slice(0, 5)
                                                });
                                            } else {
                                                throw new Error('createTheme function not available');
                                            }
                                        } catch (importError) {
                                            console.error('Failed to import useThemeManagement:', importError);
                                        }
                                    }
                                } catch (e) {
                                    console.error('[Theme Debug] Error installing theme:', e);
                                }
                            }}
                            className="px-2 py-1 rounded transition-colors"
                            style={{
                                background: 'var(--global-primary-color, #4f8cff)',
                                color: 'var(--global-background-color, #181c20)',
                            }}
                        >
                            Install Theme
                        </button>
                    </>
                )}
                <button
                    onClick={() => navigator.clipboard.writeText(content)}
                    className="px-2 py-1 rounded transition-colors"
                    style={{
                        background: 'var(--global-primary-color, #4f8cff)',
                        color: 'var(--global-background-color, #181c20)',
                    }}
                >
                    Copy
                </button>
                <button
                    onClick={onToggleRaw}
                    className="px-2 py-1 rounded transition-colors"
                    style={{
                        background: 'var(--global-primary-color, #4f8cff)',
                        color: 'var(--global-background-color, #181c20)',
                    }}
                >
                    {isRawView ? 'Show Rendered' : 'Show Raw'}
                </button>
            </div>
        </div>
    );
};
